﻿//
// InstallDialog.cs
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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.LibraryManager.UI.Models;
using MonoDevelop.Projects;
using Xwt;

namespace MonoDevelop.LibraryManager.UI
{
    partial class InstallDialog
    {
        InstallDialogViewModel viewModel;
        readonly string configFileName;
        int previousLibraryTextEntryLength;
        string baseFolder;
        string lastSuggestedTargetLocation;

        public InstallDialog(
            IDependencies dependencies,
            ILibraryCommandService libraryCommandService,
            string configFilePath,
            string fullPath,
            string rootFolder,
            Project project)
        {
            this.configFileName = configFilePath;
            Build();

            viewModel = new InstallDialogViewModel(dependencies, libraryCommandService, configFilePath, fullPath, rootFolder, project);

            lastSuggestedTargetLocation = InstallationFolder.DestinationFolder;
            baseFolder = InstallationFolder.DestinationFolder;

            LoadDialog ();
        }

        void LoadDialog()
        {
            cancelButton.Clicked += CancelButtonClicked;
            installButton.Clicked += InstallButtonClicked;
            libraryTextEntry.Changed += LibraryTextEntryChanged;
            libraryFilesTreeView.Visible = false;

            LoadProviders();
            providerComboBox.SelectionChanged += ProviderComboBoxSelectionChanged;

            targetLocationTextEntry.Text = InstallationFolder.DestinationFolder;
            targetLocationTextEntry.Changed += TargetLocationTextEntryChanged;
            installButton.Sensitive = !string.IsNullOrEmpty(targetLocationTextEntry.Text);

            libraryTextEntry.SetFocus();

            includeAllLibraryFilesRadioButton.ActiveChanged += IncludeAllLibraryFilesRadioButtonActiveChanged;

            libraryFilesCheckBoxCellView.Toggled += LibraryFilesCheckBoxCellViewToggled;

            viewModel.LibraryNameChange.PropertyChanged += LibraryNameChanged;
            viewModel.PropertyChanged += ViewModelPropertyChanged;

            CompletionWindowManager.WindowShown += HandleCompletionWindowShown;
            CompletionWindowManager.WindowClosed += HandleCompletionWindowClosed;
        }

        void CancelButtonClicked(object sender, EventArgs e)
        {
            Close();
        }

        void InstallButtonClicked(object sender, EventArgs e)
        {
            InstallAsync().Ignore();
        }

        void LibraryTextEntryChanged(object sender, EventArgs e)
        {
            viewModel.PackageId = libraryTextEntry.Text;

            // Only search if text has been inserted not deleted.
            int length = libraryTextEntry.TextLength;
            if (previousLibraryTextEntryLength < length)
                PerformLibrarySearch().Ignore();

            previousLibraryTextEntryLength = length;
        }

        async Task PerformLibrarySearch()
        {
            CompletionSet completionSet = await PerformSearch(libraryTextEntry.Text, libraryTextEntry.CaretOffset);
            libraryTextEntry.UpdateCompletionItems(completionSet);
        }

        void TargetLocationTextEntryChanged(object sender, EventArgs e)
        {
            // Target location text box is pre populated with name of the folder from where the - Add Client-Side Library command was invoked.
            // If the user clears the field at any point, we should make sure the Install button is disabled till valid folder name is provided.
            installButton.Sensitive = !string.IsNullOrEmpty(targetLocationTextEntry.Text);

            InstallationFolder.DestinationFolder = targetLocationTextEntry.Text;

            CompletionSet completionSet = TargetLocationSearch(targetLocationTextEntry.Text, targetLocationTextEntry.CaretOffset);
            targetLocationTextEntry.UpdateCompletionItems(completionSet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancelButton.Clicked -= CancelButtonClicked;
                installButton.Clicked -= InstallButtonClicked;
                libraryTextEntry.Changed -= LibraryTextEntryChanged;
                providerComboBox.SelectionChanged -= ProviderComboBoxSelectionChanged;
                includeAllLibraryFilesRadioButton.ActiveChanged -= IncludeAllLibraryFilesRadioButtonActiveChanged;
                libraryFilesCheckBoxCellView.Toggled -= LibraryFilesCheckBoxCellViewToggled;
                viewModel.LibraryNameChange.PropertyChanged -= LibraryNameChanged;
                viewModel.PropertyChanged -= ViewModelPropertyChanged;

                CompletionWindowManager.WindowShown -= HandleCompletionWindowShown;
                CompletionWindowManager.WindowClosed -= HandleCompletionWindowClosed;

                CompletionWindowManager.HideWindow();
            }
            base.Dispose(disposing);
        }

        void LoadProviders()
        {
            foreach (IProvider provider in viewModel.Providers)
            {
                providerComboBox.Items.Add(provider, provider.Id);
            }

            providerComboBox.SelectedItem = viewModel.SelectedProvider;
            infoPopover.Message = viewModel.SelectedProviderHintMessage;
        }

        void ProviderComboBoxSelectionChanged(object sender, EventArgs e)
        {
            viewModel.SelectedProvider = (IProvider)providerComboBox.SelectedItem;
            infoPopover.Message = viewModel.SelectedProviderHintMessage;

            if (!viewModel.IsTreeViewEmpty)
            {
                includeAllLibraryFilesRadioButton.Active = true;
                libraryTextEntry.Text = string.Empty;
                viewModel.IsTreeViewEmpty = true;
                viewModel.PackageId = null;
                viewModel.AnyFileSelected = false;
            }
        }

        Task<CompletionSet> PerformSearch(string searchText, int caretPosition)
        {
            try
            {
                return viewModel.SelectedProvider.GetCatalog().GetLibraryCompletionSetAsync(searchText, caretPosition);
            }
            catch (InvalidLibraryException)
            {
                // Make the warning visible with ex.Message
                return Task.FromResult<CompletionSet>(default(CompletionSet));
            }
        }

        CompletionSet TargetLocationSearch(string searchText, int caretPosition)
        {
            Dependencies dependencies = Dependencies.FromConfigFile(configFileName);
            string cwd = dependencies?.GetHostInteractions().WorkingDirectory;

            IEnumerable<Tuple<string, string>> completions = GetCompletions(cwd, searchText, caretPosition, out Span textSpan);

            var completionSet = new CompletionSet
            {
                Start = 0,
                Length = searchText.Length
            };

            var completionItems = new List<CompletionItem>();

            foreach (Tuple<string, string> completion in completions)
            {
                string insertionText = completion.Item2;

                if (insertionText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var completionItem = new CompletionItem
                    {
                        DisplayText = completion.Item1,
                        InsertionText = insertionText,
                    };

                    completionItems.Add(completionItem);
                }
            }

            completionSet.Completions = completionItems.OrderBy(m => m.InsertionText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase));

            return completionSet;
        }

        IEnumerable<Tuple<string, string>> GetCompletions(string cwd, string value, int caretPosition, out Span span)
        {
            span = new Span(0, value.Length);
            var completions = new List<Tuple<string, string>> ();
            int index = 0;

            if (value.Contains("/"))
            {
                index = value.Length >= caretPosition - 1 ? value.LastIndexOf('/', Math.Max (caretPosition - 1, 0)) : value.Length;
            }

            string prefix = "";

            if (index > 0)
            {
                prefix = value.Substring(0, index + 1);
                cwd = Path.Combine(cwd, prefix);
                span = new Span(index + 1, value.Length - index - 1);
            }

            var directoryInfo = new DirectoryInfo(cwd);

            if (directoryInfo.Exists)
            {
                foreach (FileSystemInfo item in directoryInfo.EnumerateDirectories())
                {
                    completions.Add(Tuple.Create(item.Name + "/", prefix + item.Name + "/"));
                }
            }

            return completions;
        }

        async Task<bool> IsLibraryInstallationStateValidAsync()
        {
            bool isLibraryInstallationStateValid = await viewModel.IsLibraryInstallationStateValidAsync().ConfigureAwait(false);
            return isLibraryInstallationStateValid;
        }

        async Task InstallAsync ()
        {
            installButton.Sensitive = false;

            bool isLibraryInstallationStateValid = await IsLibraryInstallationStateValidAsync();
            if (isLibraryInstallationStateValid)
            {
                Close();
                await viewModel.InstallPackageAsync().ConfigureAwait(false);
            }
            else
            {
                MessageService.ShowError(this, viewModel.ErrorMessage);
                installButton.Sensitive = true;
            }
        }

        void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName) {
                case nameof (viewModel.IsTreeViewEmpty):
                    OnIsTreeViewEmptyChanged();
                    break;
                case nameof (viewModel.DisplayRoots):
                    OnDisplayRootsChanged();
                    break;
            }
        }

        void OnIsTreeViewEmptyChanged()
        {
            chooseLibraryFilesToInstallFrame.Visible = viewModel.IsTreeViewEmpty;
            libraryFilesTreeView.Visible = !viewModel.IsTreeViewEmpty;
        }

        void OnDisplayRootsChanged()
        {
            libraryFilesTreeStore.Clear();

            foreach (PackageItem item in viewModel.DisplayRoots)
            {
                AddLibraryFileNodes(null, item);
            }

            libraryFilesTreeView.ExpandAll();
        }

        void AddLibraryFileNodes(TreeNavigator navigator, PackageItem item)
        {
            if (navigator == null)
            {
                navigator = libraryFilesTreeStore.AddNode();
            }
            else
            {
                navigator.AddChild();
            }

            navigator.SetValue(libraryFileCheckedDataField, item.IsChecked.GetValueOrDefault());
            navigator.SetValue(libraryFileCheckedEditableDataField, item.IsChecked.HasValue);
            navigator.SetValue(libraryFileDescriptionDataField, item.Name);
            navigator.SetValue(libraryFilePackageItemDataField, item);

            foreach (PackageItem childItem in item.Children)
            {
                AddLibraryFileNodes(navigator, childItem);
                navigator.MoveToParent();
            }
        }

        void IncludeAllLibraryFilesRadioButtonActiveChanged(object sender, EventArgs e)
        {
            bool includeAllLibraryFiles = includeAllLibraryFilesRadioButton.Active;
            libraryFilesTreeView.Sensitive = !includeAllLibraryFiles;

            if (includeAllLibraryFiles)
            {
                viewModel.LibraryFilesToInstall = FileSelectionType.InstallAllLibraryFiles;
            }
            else
            {
                viewModel.LibraryFilesToInstall = FileSelectionType.ChooseSpecificFilesToInstall;
            }
        }

        void LibraryNameChanged(object sender, PropertyChangedEventArgs e)
        {
            string targetLibrary = viewModel.LibraryNameChange.LibraryName;

            if (!string.IsNullOrEmpty(targetLibrary))
            {
                if (targetLocationTextEntry.Text.Equals(lastSuggestedTargetLocation, StringComparison.Ordinal))
                {
                    if (targetLibrary.Length > 0 && targetLibrary[targetLibrary.Length - 1] == '/')
                    {
                        targetLibrary = targetLibrary.Substring(0, targetLibrary.Length - 1);
                    }

                    targetLocationTextEntry.Text = baseFolder + targetLibrary + '/';
                    InstallationFolder.DestinationFolder = targetLocationTextEntry.Text;
                    lastSuggestedTargetLocation = targetLocationTextEntry.Text;
                }
            }
        }

        void LibraryFilesCheckBoxCellViewToggled(object sender, WidgetEventArgs e)
        {
            TreePosition currentPosition = libraryFilesTreeView.CurrentEventRow;
            if (currentPosition == null)
                return;

            TreeNavigator navigator = libraryFilesTreeStore.GetNavigatorAt(currentPosition);
            if (navigator == null)
                return;

            PackageItem item = navigator.GetValue(libraryFilePackageItemDataField);
            bool libraryFileChecked = navigator.GetValue(libraryFileCheckedDataField);

            if (item != null)
            {
                // Note that the toggle event seems to happen before the check box changes
                // which is why we are using !libraryFileChecked.
                item.IsChecked = !libraryFileChecked;
            }
        }

        #region Modal and Dialog.Run workaround
        /*
         * If Dialog is ran with Dialog.Run and Modal=true it takes all events like mouse, keyboard... from other windows.
         * So when CodeCompletionList window appears mouse events don't work on it(except if CodeCompletionList.Modal=true, but then
         * events don't work on BreakpointPropertiesDialog(can't type rest of exception type name)).
         * So what this workaround does is disables Modal on BreakpointProperties so CodeCompletionList mouse events work fine. But if user
         * tries to access anything outside this two windows(e.g. MainWindow). CodeCompletionList loses focus and closes itself. Resulting
         * in BreakpointProperties.Modal = true and user can't do anything on MainWindow.
         * All this is done so fast(or in correct order) that user can't notice this Modal switching.
         */

        void HandleCompletionWindowClosed(object sender, EventArgs e)
        {
            UpdateTopWindowModal(true);
        }

        void UpdateTopWindowModal(bool modal)
        {
            var gtkWidget = Toolkit.CurrentEngine.GetNativeWidget (libraryLabel) as Gtk.Widget;//Any widget is fine
            if (gtkWidget != null)
            {
                var topWindow = gtkWidget.Toplevel as Gtk.Window;
                if (topWindow != null)
                {
                    topWindow.Modal = modal;
                }
            }
        }

        void HandleCompletionWindowShown(object sender, EventArgs e)
        {
            UpdateTopWindowModal(false);
        }

        #endregion
    }
}
