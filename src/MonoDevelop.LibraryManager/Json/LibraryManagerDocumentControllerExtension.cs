//
// LibraryManagerDocumentControllerExtension.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Resources;
using Microsoft.Web.LibraryManager.Vsix;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.LibraryManager.Vsix;
using MonoDevelop.Projects;

namespace MonoDevelop.LibraryManager.Json
{
    public class LibraryManagerDocumentControllerExtension : DocumentControllerExtension
    {
        Manifest manifest;
        IDependencies dependencies;
        Project project;
        ErrorList errorList;
        string manifestPath;

        public override Task<bool> SupportsController(DocumentController controller)
        {
            bool supported = false;
            if (controller is FileDocumentController fileController)
            {
                supported = StringComparer.OrdinalIgnoreCase.Equals(Constants.ConfigFileName, fileController.FilePath.FileName);
            }
            return Task.FromResult(supported);
        }

        public override Task Initialize(Properties status)
        {
            var fileController = Controller as FileDocumentController;
            if (fileController != null)
            {
                return Initialize (fileController, status);
            }

            return base.Initialize(status);
        }

        async Task Initialize(FileDocumentController fileController, Properties status)
        {
            await base.Initialize(status).ConfigureAwait(true);

            dependencies = Dependencies.FromConfigFile(fileController.FilePath);
            manifest = await Manifest.FromFileAsync(fileController.FilePath, dependencies, CancellationToken.None).ConfigureAwait(false);
            manifestPath = fileController.FilePath;
            project = fileController.Owner as Project;

            Task.Run(async () =>
            {
                IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, dependencies, CancellationToken.None).ConfigureAwait (false);
                if (!results.All(r => r.Success))
                {
                    AddErrorsToList(results);
                }
            }).Ignore();
        }

        protected override void OnClosed()
        {
            base.OnClosed();
             errorList?.ClearErrors();
        }

        void AddErrorsToList(IEnumerable<ILibraryOperationResult> errors)
        {
            errorList = new ErrorList(project, manifestPath);
            errorList.HandleErrors(errors);
        }

        public override async Task OnSave()
        {
            await base.OnSave().ConfigureAwait(false);

            var fileController = Controller as FileDocumentController;
            if (fileController == null)
                return;

            if (LibraryManagerService.LibraryCommandService.IsOperationInProgress)
            {
                string message = GettextCatalog.GetString ("Library Manager operation already in progress.");
                Logger.LogEvent(message, LogLevel.Operation);
            }

            Task.Run(async () =>
            {
                try
                {
                    var newManifest = Manifest.FromJson(fileController.Document.TextBuffer.CurrentSnapshot.GetText(), dependencies);
                    IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(
                        newManifest,
                        dependencies,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                    if (!results.All(r => r.Success))
                    {
                        AddErrorsToList(results);
                        Logger.LogErrorsSummary(results, OperationType.Restore);
                    }
                    else
                    {
                        if (manifest == null || await manifest.RemoveUnwantedFilesAsync(newManifest, CancellationToken.None).ConfigureAwait (false))
                        {
                            manifest = newManifest;

                            await LibraryManagerService.LibraryCommandService.RestoreAsync(
                                fileController.FilePath,
                                manifest,
                                CancellationToken.None)
                            .ConfigureAwait (false);
                        }
                        else
                        {
                            string textMessage = string.Concat(Environment.NewLine, Text.Restore_OperationHasErrors, Environment.NewLine);
                            Logger.LogErrorsSummary (new[] { textMessage }, OperationType.Restore);
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    string textMessage = string.Concat(Environment.NewLine, Text.Restore_OperationCancelled, Environment.NewLine);

                    Logger.LogEvent(textMessage, LogLevel.Task);
                    Logger.LogEvent(ex.ToString(), LogLevel.Error);
                }
                catch (Exception ex)
                {
                    // TO DO: Restore to previous state
                    // and add a warning to the Error List

                    string textMessage = string.Concat(Environment.NewLine, Text.Restore_OperationHasErrors, Environment.NewLine);

                    Logger.LogEvent(textMessage, LogLevel.Task);
                    Logger.LogEvent(ex.ToString(), LogLevel.Error);
                }
            }).Ignore();
        }
    }
}
