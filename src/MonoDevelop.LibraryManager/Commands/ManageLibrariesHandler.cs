//
// ManageLibrariesHandler.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager;
using Microsoft.Web.LibraryManager.Vsix;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.LibraryManager.Commands
{
    public class ManageLibrariesHandler : CommandHandler
    {
        protected override void Run()
        {
            RunAsync().Ignore();
        }

        async Task RunAsync()
        {
            Project project = IdeApp.ProjectOperations.CurrentSelectedProject;

            if (project == null)
                return;

            string rootFolder = await project.GetRootFolderAsync();

            string configFilePath = Path.Combine(rootFolder, Constants.ConfigFileName);

            if (File.Exists(configFilePath))
            {
                 await VsHelpers.OpenFileAsync(configFilePath);
            }
            else
            {
                var dependencies = Dependencies.FromConfigFile(configFilePath);
                Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, CancellationToken.None);
                manifest.DefaultProvider = "cdnjs";
                manifest.Version = Manifest.SupportedVersions.Max().ToString();

                await manifest.SaveAsync(configFilePath, CancellationToken.None);

                if (project.GetProjectFile(configFilePath) == null)
                {
                    project.AddFile(configFilePath);
                    await project.SaveAsync(new ProgressMonitor());
                }

                await VsHelpers.OpenFileAsync(configFilePath);
           }
         }
    }
}
