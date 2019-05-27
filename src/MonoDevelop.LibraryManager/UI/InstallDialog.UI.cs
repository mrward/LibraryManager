//
// InstallDialog.UI.cs
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

using Xwt;
using MonoDevelop.Core;
using MonoDevelop.LibraryManager.UI.Models;
using InformationPopoverWidget = MonoDevelop.Components.InformationPopoverWidget;

namespace MonoDevelop.LibraryManager.UI
{
    partial class InstallDialog : Dialog
    {
        Label providerLabel;
        Label libraryLabel;
        ComboBox providerComboBox;
        SearchTextEntryWithCodeCompletion libraryTextEntry;
        RadioButtonGroup radioButtonGroup;
        RadioButton includeAllLibraryFilesRadioButton;
        RadioButton chooseSpecificFilesRadioButton;
        TreeView libraryFilesTreeView;
        TreeStore libraryFilesTreeStore;
        DataField<bool> libraryFileCheckedDataField = new DataField<bool>();
        DataField<bool> libraryFileCheckedEditableDataField = new DataField<bool> ();
        DataField<string> libraryFileDescriptionDataField = new DataField<string>();
        CheckBoxCellView libraryFilesCheckBoxCellView;
        FrameBox chooseLibraryFilesToInstallFrame;
        Label chooseLibraryFilesToInstallLabel;
        SearchTextEntryWithCodeCompletion targetLocationTextEntry;
        DialogButton installButton;
        DialogButton cancelButton;
        InformationPopoverWidget infoPopover;

        void Build()
        {
            Title = GettextCatalog.GetString("Add Client-Side Library");
            Height = 350;
            Width = 500;

            var mainVBox = new VBox();
            mainVBox.Margin = 12;

            var providerHBox = new HBox();
            mainVBox.PackStart(providerHBox);

            providerLabel = new Label();
            providerLabel.Text = GettextCatalog.GetString("Provider:");
            providerHBox.PackStart(providerLabel);

            providerComboBox = new ComboBox();
            providerHBox.PackStart(providerComboBox);

            var libraryHBox = new HBox();
            mainVBox.PackStart(libraryHBox);

            libraryLabel = new Label();
            libraryLabel.Text = GettextCatalog.GetString("Library:");
            libraryHBox.PackStart(libraryLabel);

            libraryTextEntry = new SearchTextEntryWithCodeCompletion();
            libraryTextEntry.PlaceholderText = GettextCatalog.GetString("Type to search");
            libraryHBox.PackStart(libraryTextEntry, true, true);

            infoPopover = new InformationPopoverWidget();
            libraryHBox.PackStart(infoPopover);

            var libraryRadioButtonVBox = new VBox();
            libraryRadioButtonVBox.Spacing = 0;
            mainVBox.PackStart(libraryRadioButtonVBox);

            radioButtonGroup = new RadioButtonGroup();
            includeAllLibraryFilesRadioButton = new RadioButton();
            includeAllLibraryFilesRadioButton.Group = radioButtonGroup;
            includeAllLibraryFilesRadioButton.Label = GettextCatalog.GetString("Include all library files");
            libraryRadioButtonVBox.PackStart(includeAllLibraryFilesRadioButton);

            chooseSpecificFilesRadioButton = new RadioButton();
            chooseSpecificFilesRadioButton.Group = radioButtonGroup;
            chooseSpecificFilesRadioButton.Label = GettextCatalog.GetString("Choose specific files");
            libraryRadioButtonVBox.PackStart(chooseSpecificFilesRadioButton);

            var libraryFilesVBox = new VBox();
            mainVBox.PackStart(libraryFilesVBox, true, true);

            libraryFilesTreeView = new TreeView();
            libraryFilesTreeView.HeadersVisible = false;
            libraryFilesTreeView.Sensitive = false;
            libraryFilesVBox.PackStart(libraryFilesTreeView, true, true);

            libraryFilesTreeStore = new TreeStore (libraryFileCheckedDataField, libraryFileCheckedEditableDataField, libraryFileDescriptionDataField);
            libraryFilesTreeView.DataSource = libraryFilesTreeStore;

            libraryFilesCheckBoxCellView = new CheckBoxCellView(libraryFileCheckedDataField);
            libraryFilesCheckBoxCellView.EditableField = libraryFileCheckedEditableDataField;
            libraryFilesTreeView.Columns.Add("Checked", libraryFilesCheckBoxCellView);
            libraryFilesTreeView.Columns.Add("Text", libraryFileDescriptionDataField);
            libraryFilesTreeView.Columns[1].Expands = true;

            var chooseLibraryFilesToInstallHBox = new HBox();
            chooseLibraryFilesToInstallHBox.HorizontalPlacement = WidgetPlacement.Center;

            chooseLibraryFilesToInstallLabel = new Label();
            chooseLibraryFilesToInstallLabel.Text = GettextCatalog.GetString("Choose a library to select files to install");
            chooseLibraryFilesToInstallHBox.PackEnd(chooseLibraryFilesToInstallLabel);

            chooseLibraryFilesToInstallFrame = new FrameBox();
            chooseLibraryFilesToInstallFrame.BackgroundColor = Ide.Gui.Styles.PrimaryBackgroundColor;
            chooseLibraryFilesToInstallFrame.Content = chooseLibraryFilesToInstallHBox;
            chooseLibraryFilesToInstallFrame.BorderWidth = new WidgetSpacing();
            libraryFilesVBox.PackStart(chooseLibraryFilesToInstallFrame, true, true);

            var targetLocationHBox = new HBox();
            mainVBox.PackStart(targetLocationHBox);

            var targetLocationLabel = new Label();
            targetLocationLabel.Text = GettextCatalog.GetString("Target Location:");
            targetLocationHBox.PackStart(targetLocationLabel);

            targetLocationTextEntry = new SearchTextEntryWithCodeCompletion();
            targetLocationHBox.PackStart(targetLocationTextEntry, true, true);

            cancelButton = new DialogButton(Command.Cancel);
            Buttons.Add(cancelButton);

            installButton = new DialogButton(GettextCatalog.GetString("Install"));
            Buttons.Add(installButton);

            DefaultCommand = installButton.Command;

            Content = mainVBox;
        }

        protected override void OnReallocate()
        {
            if (providerLabel.WindowBounds.Width > libraryLabel.WindowBounds.Width)
            {
                libraryLabel.WidthRequest = providerLabel.WindowBounds.Width;
            }
            base.OnReallocate();
        }
    }
}
