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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix;
using MonoDevelop.Ide;
using MonoDevelop.LibraryManager.UI.Models;
using MonoDevelop.Projects;

namespace MonoDevelop.LibraryManager.UI
{
    partial class InstallDialog
    {
        InstallDialogViewModel viewModel;
        readonly string configFileName;
        int previousLibraryTextEntryLength;

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
            targetLocationTextEntry.Changed += TargetLocationTextEntryChanged;
            installButton.Sensitive = !string.IsNullOrEmpty(targetLocationTextEntry.Text);

            libraryTextEntry.SetFocus();
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
    }
}
