//
// InstallLibraryHandler.cs
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.LibraryManager.Vsix;
using MonoDevelop.Projects;
using Xwt;

namespace MonoDevelop.LibraryManager.Commands
{
    class InstallLibraryHandler : CommandHandler
    {
        ILibraryCommandService libraryCommandService = new LibraryCommandService();

        protected override void Run ()
        {
            LibraryManagerService.Initialize();
            RunAsync().Ignore();
        }

        async Task RunAsync()
        {
            ProjectFolder item = await VsHelpers.GetSelectedItemAsync().ConfigureAwait(false);
            Project project = await VsHelpers.GetProjectOfSelectedItemAsync().ConfigureAwait(false);

            if (project != null)
            {
                string rootFolder = await project.GetRootFolderAsync().ConfigureAwait(false);

                string configFilePath = Path.Combine(rootFolder, Constants.ConfigFileName);
                IDependencies dependencies = Dependencies.FromConfigFile(configFilePath);

                Manifest manifest = await GetManifestAsync(configFilePath, dependencies).ConfigureAwait(false);

                // If the manifest contains errors, we will not invoke the "Add Client-Side libraries" dialog
                // Instead we will display a message box indicating the syntax errors in manifest file.
                if (manifest == null)
                {
                    MessageService.ShowWarning(PredefinedErrors.ManifestMalformed().Message);
                    return;
                }

                string target = string.Empty;

                // Install command was invoked from a folder.
                // So the initial target location should be name of the folder from which
                // the command was invoked.
                if (item != null)
                {
                    target = item.Path.CanonicalPath + Path.DirectorySeparatorChar;
                }
                else
                {
                    // Install command was invoked from project scope.
                    // If wwwroot exists, initial target location should be - wwwroot/lib.
                    // Else, target location should be - lib
                    if (Directory.Exists(Path.Combine(rootFolder, "wwwroot")))
                    {
                        target = Path.Combine(rootFolder, "wwwroot", "lib") + Path.DirectorySeparatorChar;
                    }
                    else
                    {
                        target = Path.Combine(rootFolder, "lib") + Path.DirectorySeparatorChar;
                    }
                }

                var dialog = new UI.InstallDialog(dependencies, libraryCommandService, configFilePath, target, rootFolder, project);
                WindowFrame parent = Toolkit.CurrentEngine.WrapWindow(IdeApp.Workbench.RootWindow);
                dialog.Run(parent);
            }
        }

        async Task<Manifest> GetManifestAsync(string configFilePath, IDependencies dependencies)
        {
            var document = IdeApp.Workbench.GetDocument(configFilePath);
            Manifest manifest = null;

            // If the document is null, then libman.json is not open. In that case, we'll use the manifest as is.
            // If document is not null, then libman.json file is open and could be dirty. So we'll get the contents for the manifest from the buffer.
            if (document != null)
            {
                manifest = Manifest.FromJson(document.Editor.Text, dependencies);
            }
            else
            {
                manifest = await Manifest.FromFileAsync(configFilePath, dependencies, CancellationToken.None).ConfigureAwait(false);
            }

            return manifest;
        }
    }
}
