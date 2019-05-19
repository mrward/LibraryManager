//
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
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix;
using MonoDevelop.LibraryManager.UI.Models;
using MonoDevelop.Projects;
using System.Threading.Tasks;
using MonoDevelop.Ide;

namespace MonoDevelop.LibraryManager.UI
{
    partial class InstallDialog
    {
        InstallDialogViewModel viewModel;
        int previousLibraryTextEntryLength;

        public InstallDialog (
            IDependencies dependencies,
            ILibraryCommandService libraryCommandService,
            string configFilePath,
            string fullPath,
            string rootFolder,
            Project project)
        {
            Build();

            viewModel = new InstallDialogViewModel(dependencies, libraryCommandService, configFilePath, fullPath, rootFolder, project);
            LoadDialog();
        }

        void LoadDialog()
        {
            cancelButton.Clicked += CancelButtonClicked;
            installButton.Clicked += InstallButtonClicked;
            libraryTextEntry.Changed += LibraryTextEntryChanged;
            libraryFilesTreeView.Visible = false;

            LoadProviders();
            providerComboBox.SelectionChanged += ProviderComboBoxSelectionChanged;

            targetLocationTextEntry.Text = viewModel.DestinationFolder;

            libraryTextEntry.SetFocus();
        }

        void CancelButtonClicked (object sender, EventArgs e)
        {
            Close();
        }

        void InstallButtonClicked(object sender, EventArgs e)
        {
            InstallAsync().Ignore();
        }

        void LibraryTextEntryChanged (object sender, EventArgs e)
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancelButton.Clicked -= CancelButtonClicked;
                installButton.Clicked -= InstallButtonClicked;
                libraryTextEntry.Changed -= LibraryTextEntryChanged;
                providerComboBox.SelectionChanged -= ProviderComboBoxSelectionChanged;
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
    }
}
