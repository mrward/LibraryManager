//
// InstallDialogViewModel.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
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
    class InstallDialogViewModel
    {
        IDependencies dependencies;
        ILibraryCommandService libraryCommandService;
        string configFileName;
        string fullPath;
        string rootFolder;
        Project project;
        string destinationFolder = string.Empty;
        ILibraryCatalog catalog;
        IProvider activeProvider;
        List<IProvider> providers;
        string packageId = string.Empty;
        bool isInstalling;
        ILibrary selectedPackage;

        public InstallDialogViewModel (
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

        void UpdateDestinationFolder (string fullPath)
        {
            if (fullPath.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
            {
                destinationFolder = fullPath.Substring(rootFolder.Length);
            }

            destinationFolder = destinationFolder.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            destinationFolder = destinationFolder.Replace('\\', '/');
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
                        //SelectedPackage = null;
                    }

                    //AnyFileSelected = false;
                    //DisplayRoots = null;
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

        public ILibrary SelectedPackage
        {
            get { return selectedPackage; }
            set
            {
                if (value == null)
                {
                    //IsTreeViewEmpty = true;
                }

                if (selectedPackage == value)
                {
                    return;
                }

                selectedPackage = value;

                //if (selectedPackage != null)
                //{
                    ////libraryNameChange.LibraryName = SelectedProvider.GetSuggestedDestination(SelectedPackage);
                    ////IsTreeViewEmpty = false;
                    //bool canUpdateInstallStatusValue = false;
                    //HashSet<string> selectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    //Func<bool> canUpdateInstallStatus = () => canUpdateInstallStatusValue;
                    //var root = new PackageItem(this, null, selectedFiles)
                    //{
                    //    CanUpdateInstallStatus = canUpdateInstallStatus,
                    //    ItemType = PackageItemType.Folder,
                    //    Name = Path.GetFileName(fullPath.TrimEnd('/', '\\')),
                    //    IsChecked = false
                    //};

                    //var packageItem = new PackageItem(this, root, selectedFiles)
                    //{
                    //    CanUpdateInstallStatus = canUpdateInstallStatus,
                    //    Name = value.Name,
                    //    ItemType = PackageItemType.Folder,
                    //    IsChecked = false
                    //};

                    ////The node that children will be added to
                    //PackageItem realParent = root;
                    ////The node that will be set as the parent of the child nodes
                    //PackageItem virtualParent = packageItem;

                    //foreach (KeyValuePair<string, bool> file in value.Files)
                    //{
                    //    string[] parts = file.Key.Split('/');
                    //    PackageItem currentRealParent = realParent;
                    //    PackageItem currentVirtualParent = virtualParent;

                    //    for (int i = 0; i < parts.Length; ++i)
                    //    {
                    //        bool isFolder = i != parts.Length - 1;

                    //        if (isFolder)
                    //        {
                    //            PackageItem next = currentRealParent.Children.FirstOrDefault(x => x.ItemType == PackageItemType.Folder && string.Equals(x.Name, parts[i], StringComparison.OrdinalIgnoreCase));

                    //            if (next == null)
                    //            {
                    //                next = new PackageItem(this, currentVirtualParent, selectedFiles)
                    //                {
                    //                    CanUpdateInstallStatus = canUpdateInstallStatus,
                    //                    Name = parts[i],
                    //                    ItemType = PackageItemType.Folder,
                    //                    IsChecked = false
                    //                };

                    //                var children = new List<PackageItem>(currentRealParent.Children) { next };

                    //                children.Sort((x, y) => x.ItemType == y.ItemType ? StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name) : y.ItemType == PackageItemType.Folder ? 1 : -1);

                    //                currentRealParent.Children = children;

                    //                if (currentVirtualParent != currentRealParent)
                    //                {
                    //                    currentVirtualParent.Children = children;
                    //                }
                    //            }

                    //            currentRealParent = next;
                    //            currentVirtualParent = next;
                    //        }
                    //        else
                    //        {
                    //            var next = new PackageItem(this, currentVirtualParent, selectedFiles)
                    //            {
                    //                CanUpdateInstallStatus = canUpdateInstallStatus,
                    //                FullPath = file.Key,
                    //                Name = parts[i],
                    //                ItemType = PackageItemType.File,
                    //                IsChecked = file.Value,
                    //            };

                    //            if (next.IsChecked ?? false)
                    //            {
                    //                selectedFiles.Add(next.FullPath);
                    //            }

                    //            var children = new List<PackageItem>(currentRealParent.Children) { next };
                    //            children.Sort((x, y) => x.ItemType == y.ItemType ? StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name) : y.ItemType == PackageItemType.Folder ? -1 : 1);

                    //            currentRealParent.Children = children;

                    //            if (currentVirtualParent != currentRealParent)
                    //            {
                    //                currentVirtualParent.Children = children;
                    //            }
                    //        }
                    //    }
                    //}

                    //_dispatcher.Invoke(() =>
                    //{
                    //    canUpdateInstallStatusValue = true;
                    //    SetNodeOpenStates(root);
                    //    DisplayRoots = new[] { root };
                    //    SelectedFiles = selectedFiles;
                    //    InstallPackageCommand.CanExecute(null);
                    //});
                //}
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

        public string ErrorMessage { get; set; }

        public string SelectedProviderHintMessage
        {
            get
            {
                return activeProvider?.LibraryIdHintText ?? string.Empty;
            }
        }

        public string DestinationFolder
        {
            get
            {
                return destinationFolder;
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
                DestinationPath = DestinationFolder,
                //Files = SelectedFiles?.ToList()
            };

            ILibraryOperationResult libraryOperationResult = await libraryInstallationState.IsValidAsync(SelectedProvider).ConfigureAwait(false);
            IList<IError> errors = libraryOperationResult.Errors;

            ErrorMessage = string.Empty;

            if (errors != null && errors.Count > 0)
            {
                ErrorMessage = errors[0].Message;
                return false;
            }

            //AnyFileSelected = IsAnyFileSelected(DisplayRoots);

            //if (!AnyFileSelected)
            //{
            //   ErrorMessage = Text.NoFilesSelected;
            //   return false;
            //}

            return true;
        }

        //static bool IsAnyFileSelected(IReadOnlyList<PackageItem> children)
        //{
        //    if (children != null)
        //    {
        //        List<PackageItem> toProcess = children.ToList();

        //        for (int i = 0; i < toProcess.Count; i++)
        //        {
        //            PackageItem child = toProcess[i];

        //            if (child.IsChecked.HasValue && child.IsChecked.Value)
        //            {
        //                return true;
        //            }

        //            toProcess.AddRange(child.Children);
        //        }
        //    }

        //    return false;
        //}

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
                        DestinationPath = DestinationFolder,
                    };

                    isInstalling = true;

                    // When "Include all files" option is checked, we don't want to write out the files to libman.json.
                    // We will only list the files when user chose to install specific files.
                    //if (LibraryFilesToInstall == FileSelectionType.ChooseSpecificFilesToInstall)
                    //{
                    //    libraryInstallationState.Files = SelectedFiles.ToList();
                    //}

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
