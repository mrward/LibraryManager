// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using Microsoft.Web.LibraryManager.Vsix;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.LibraryManager.UI.Models
{
    class InstallDialogViewModel : BindableBase
    {
        IDependencies dependencies;
        ILibraryCommandService libraryCommandService;
        string configFileName;
        string fullPath;
        string rootFolder;
        Project project;
        ILibraryCatalog catalog;
        IProvider activeProvider;
        List<IProvider> providers;
        string packageId = string.Empty;
        bool isInstalling;
        ILibrary selectedPackage;
        IReadOnlyList<PackageItem> displayRoots;
        FileSelectionType fileSelectionType;
        bool anyFileSelected;
        bool isTreeViewEmpty;
        string errorMessage;
        BindLibraryNameToTargetLocation libraryNameChange;

        public InstallDialogViewModel(
            IDependencies dependencies,
            ILibraryCommandService libraryCommandService,
            string configFileName,
            string fullPath,
            string rootFolder,
            Project project)
        {
            this.dependencies = dependencies;
            this.libraryCommandService = libraryCommandService;
            this.configFileName = configFileName;
            this.fullPath = fullPath;
            this.rootFolder = rootFolder;
            this.project = project;
            libraryNameChange = new BindLibraryNameToTargetLocation();

            UpdateDestinationFolder(fullPath);
            LoadProviders();
        }

        void LoadProviders ()
        {
            providers = new List<IProvider>();
            foreach (IProvider provider in dependencies.Providers.OrderBy(x => x.Id))
            {
                ILibraryCatalog currentCatalog = provider.GetCatalog();

                if (currentCatalog == null)
                {
                    continue;
                }

                if (catalog == null)
                {
                    activeProvider = provider;
                    catalog = currentCatalog;
                }

                providers.Add(provider);
            }
        }

        void UpdateDestinationFolder(string fullPath)
        {
            string destinationFolder = string.Empty;

            if (fullPath.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
            {
                destinationFolder = fullPath.Substring(rootFolder.Length);
            }

            destinationFolder = destinationFolder.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            destinationFolder = destinationFolder.Replace('\\', '/');

            InstallationFolder.DestinationFolder = destinationFolder.Replace('\\', '/');
        }

        public IReadOnlyList<PackageItem> DisplayRoots
        {
            get
            {
                IReadOnlyList<PackageItem> currentDisplayRoots = displayRoots;

                if (currentDisplayRoots != null && currentDisplayRoots.Any())
                {
                    currentDisplayRoots.ElementAt(0).Name = GettextCatalog.GetString("Files:");
                }

                return displayRoots;
            }

            set
            {
                if (string.IsNullOrEmpty (PackageId))
                {
                    Set(ref displayRoots, null);
                }
                else
                {
                    Set(ref displayRoots, value);
                }
            }
        }

        public bool IsTreeViewEmpty
        {
            get { return isTreeViewEmpty; }
            set
            {
                if (Set(ref isTreeViewEmpty, value))
                {
                    RefreshFileSelections();
                }
            }
        }

        public string PackageId
        {
            get { return packageId; }
            set
            {
                // If libraryId is null, then we need to clear the tree view for files and show warning message.
                if (string.IsNullOrEmpty(value))
                {
                    if (packageId != value)
                    {
                        packageId = value;
                        SelectedPackage = null;
                    }

                    AnyFileSelected = false;
                    DisplayRoots = null;
                }
                else
                {
                    if (packageId != value)
                    {
                        packageId = value;
                        RefreshFileSelections ();
                    }
                 }
            }
        }

        public IReadOnlyList<IProvider> Providers => providers;

        public HashSet<string> SelectedFiles { get; private set; }

        internal BindLibraryNameToTargetLocation LibraryNameChange
        {
            get { return libraryNameChange; }
            set { Set(ref libraryNameChange, value); }
        }

        public ILibrary SelectedPackage
        {
            get { return selectedPackage; }
            set
            {
                if (value == null)
                {
                    IsTreeViewEmpty = true;
                }

                if (Set(ref selectedPackage, value) && value != null)
                {
                    libraryNameChange.LibraryName = SelectedProvider.GetSuggestedDestination(SelectedPackage);
                    IsTreeViewEmpty = false;
                    bool canUpdateInstallStatusValue = false;
                    HashSet<string> selectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    Func<bool> canUpdateInstallStatus = () => canUpdateInstallStatusValue;
                    var root = new PackageItem(this, null, selectedFiles)
                    {
                        CanUpdateInstallStatus = canUpdateInstallStatus,
                        ItemType = PackageItemType.Folder,
                        Name = Path.GetFileName(fullPath.TrimEnd ('/', '\\')),
                        IsChecked = false
                    };

                    var packageItem = new PackageItem(this, root, selectedFiles)
                    {
                        CanUpdateInstallStatus = canUpdateInstallStatus,
                        Name = value.Name,
                        ItemType = PackageItemType.Folder,
                        IsChecked = false
                    };

                    //The node that children will be added to
                    PackageItem realParent = root;
                    //The node that will be set as the parent of the child nodes
                    PackageItem virtualParent = packageItem;

                    foreach (KeyValuePair<string, bool> file in value.Files)
                    {
                        string[] parts = file.Key.Split('/');
                        PackageItem currentRealParent = realParent;
                        PackageItem currentVirtualParent = virtualParent;

                        for (int i = 0; i < parts.Length; ++i)
                        {
                            bool isFolder = i != parts.Length - 1;

                            if (isFolder)
                            {
                                PackageItem next = currentRealParent.Children.FirstOrDefault(
                                    x => x.ItemType == PackageItemType.Folder &&
                                        string.Equals(x.Name, parts[i], StringComparison.OrdinalIgnoreCase));

                                if (next == null)
                                {
                                    next = new PackageItem(this, currentVirtualParent, selectedFiles)
                                    {
                                        CanUpdateInstallStatus = canUpdateInstallStatus,
                                        Name = parts[i],
                                        ItemType = PackageItemType.Folder,
                                        IsChecked = false
                                    };

                                    var children = new List<PackageItem>(currentRealParent.Children) { next };

                                    children.Sort((x, y) => x.ItemType == y.ItemType ?
                                        StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name) : y.ItemType == PackageItemType.Folder ? 1 : -1);

                                    currentRealParent.Children = children;

                                    if (currentVirtualParent != currentRealParent)
                                    {
                                        currentVirtualParent.Children = children;
                                    }
                                }

                                currentRealParent = next;
                                currentVirtualParent = next;
                            }
                            else
                            {
                                var next = new PackageItem(this, currentVirtualParent, selectedFiles)
                                {
                                    CanUpdateInstallStatus = canUpdateInstallStatus,
                                    FullPath = file.Key,
                                    Name = parts[i],
                                    ItemType = PackageItemType.File,
                                    IsChecked = file.Value,
                                };

                                if (next.IsChecked ?? false)
                                {
                                    selectedFiles.Add(next.FullPath);
                                }

                                var children = new List<PackageItem>(currentRealParent.Children) { next };
                                children.Sort ((x, y) => x.ItemType == y.ItemType ?
                                StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name) : y.ItemType == PackageItemType.Folder ? -1 : 1);

                                currentRealParent.Children = children;

                                if (currentVirtualParent != currentRealParent)
                                {
                                    currentVirtualParent.Children = children;
                                }
                            }
                        }
                    }

                    Runtime.RunInMainThread(() =>
                    {
                        canUpdateInstallStatusValue = true;
                        SetNodeOpenStates(root);
                        DisplayRoots = new[] { root };
                        SelectedFiles = selectedFiles;
                        //InstallPackageCommand.CanExecute(null);
                    }).Ignore();
                }
            }
        }

        void RefreshFileSelections()
        {
            (string name, string version) = LibraryIdToNameAndVersionConverter.Instance.GetLibraryNameAndVersion(packageId, SelectedProvider.Id);
            _ = SelectedProvider.GetCatalog()
                .GetLibraryAsync(name, version, CancellationToken.None)
                .ContinueWith(x =>
            {
                if (x.IsFaulted || x.IsCanceled)
                {
                    SelectedPackage = null;
                    return;
                }

                SelectedPackage = x.Result;
            }, TaskScheduler.Default);
        }

        public FileSelectionType LibraryFilesToInstall
        {
            get
            {
                return fileSelectionType;
            }
            set
            {
                fileSelectionType = value;
                FileSelection.InstallationType = value;
                RefreshFileSelections ();
            }
        }

        public IProvider SelectedProvider
        {
            get { return activeProvider; }
            set
            {
                if (value != activeProvider)
                {
                    activeProvider = value;
                    catalog = value.GetCatalog();
                //    Task t = LoadPackagesAsync();
                }
            }
        }

        static void SetNodeOpenStates(PackageItem item)
        {
            bool shouldBeOpen = false;

            foreach (PackageItem child in item.Children)
            {
                SetNodeOpenStates(child);
                shouldBeOpen |= child.IsChecked.GetValueOrDefault(true) || child.IsExpanded;
            }

            item.IsExpanded = shouldBeOpen;
        }

        public bool AnyFileSelected
        {
            get { return anyFileSelected; }
            set { Set (ref anyFileSelected, value); }
        }

        public string ErrorMessage
        {
            get { return errorMessage; }
            set { Set(ref errorMessage, value); }
        }

        public string SelectedProviderHintMessage
        {
            get
            {
                return activeProvider?.LibraryIdHintText ?? string.Empty;
            }
        }

        public async Task<bool> IsLibraryInstallationStateValidAsync()
        {
            (string name, string version) = LibraryIdToNameAndVersionConverter.Instance.GetLibraryNameAndVersion(PackageId, SelectedProvider.Id);
            var libraryInstallationState = new LibraryInstallationState
            {
                Name = name,
                Version = version,
                ProviderId = SelectedProvider.Id,
                DestinationPath = InstallationFolder.DestinationFolder,
                Files = SelectedFiles?.ToList()
            };

            ILibraryOperationResult libraryOperationResult = await libraryInstallationState.IsValidAsync(SelectedProvider).ConfigureAwait(false);
            IList<IError> errors = libraryOperationResult.Errors;

            ErrorMessage = string.Empty;

            if (errors != null && errors.Count > 0)
            {
                ErrorMessage = errors[0].Message;
                return false;
            }

            AnyFileSelected = IsAnyFileSelected(DisplayRoots);

            if (!AnyFileSelected)
            {
               ErrorMessage = GettextCatalog.GetString("No files have been selected");
               return false;
            }

            return true;
        }

        static bool IsAnyFileSelected(IReadOnlyList<PackageItem> children)
        {
            if (children != null)
            {
                List<PackageItem> toProcess = children.ToList();

                for (int i = 0; i < toProcess.Count; i++)
                {
                    PackageItem child = toProcess[i];

                    if (child.IsChecked.HasValue && child.IsChecked.Value)
                    {
                        return true;
                    }

                    toProcess.AddRange(child.Children);
                }
            }

            return false;
        }

        public async Task InstallPackageAsync()
        {
            try
            {
                bool isLibraryInstallationStateValid = await IsLibraryInstallationStateValidAsync().ConfigureAwait(false);

                if (isLibraryInstallationStateValid)
                {
                    ILibrary package = SelectedPackage;
                    Manifest manifest = await Manifest.FromFileAsync(configFileName, dependencies, CancellationToken.None).ConfigureAwait(false);
                    string targetPath = fullPath;

                    if (!string.IsNullOrEmpty(configFileName))
                    {
                        Uri configContainerUri = new Uri(configFileName, UriKind.Absolute);
                        Uri targetUri = new Uri(targetPath, UriKind.Absolute);
                        targetPath = configContainerUri.MakeRelativeUri(targetUri).ToString();
                    }

                    if (String.IsNullOrEmpty(manifest.Version))
                    {
                        manifest.Version = Manifest.SupportedVersions.Max().ToString();
                    }

                    (string name, string version) = LibraryIdToNameAndVersionConverter.Instance.GetLibraryNameAndVersion(PackageId, SelectedProvider.Id);
                    LibraryInstallationState libraryInstallationState = new LibraryInstallationState
                    {
                        Name = name,
                        Version = version,
                        ProviderId = package.ProviderId,
                        DestinationPath = InstallationFolder.DestinationFolder,
                    };

                    isInstalling = true;

                    // When "Include all files" option is checked, we don't want to write out the files to libman.json.
                    // We will only list the files when user chose to install specific files.
                    if (LibraryFilesToInstall == FileSelectionType.ChooseSpecificFilesToInstall)
                    {
                        libraryInstallationState.Files = SelectedFiles.ToList();
                    }

                    manifest.AddLibrary(libraryInstallationState);

                    if (!File.Exists(configFileName))
                    {
                        await manifest.SaveAsync(configFileName, CancellationToken.None);

                        if (project != null)
                        {
                            await project.AddFileToProjectAsync(configFileName);
                        }
                    }

                   // RunningDocumentTable rdt = new RunningDocumentTable(Shell.ServiceProvider.GlobalProvider);
                    string configFilePath = Path.GetFullPath(configFileName);

                    //IVsTextBuffer textBuffer = rdt.FindDocument(configFilePath) as IVsTextBuffer;

                    //_dispatcher.Invoke(() =>
                    //{
                    //    _closeDialog(true);
                    //});

                    // The file isn't open. So we'll write to disk directly
                    //if (textBuffer == null)
                    {
                        manifest.AddLibrary(libraryInstallationState);

                        await manifest.SaveAsync(configFileName, CancellationToken.None).ConfigureAwait(false);

                        await libraryCommandService.RestoreAsync(configFileName, CancellationToken.None).ConfigureAwait(false);
                    }
                    //else
                    //{
                        // libman.json file is open, so we will write to the textBuffer.
                        //InsertIntoTextBuffer(textBuffer, libraryInstallationState, manifest);

                        // Save manifest file so we can restore library files.
                    //    rdt.SaveFileIfDirty(configFilePath);
                    //}
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("InstallPackageAsync error", ex);
            }
        }
    }
}
