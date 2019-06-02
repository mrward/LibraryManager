//
// RestoreLibrariesHandler.cs
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Vsix;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.LibraryManager.Vsix;

namespace MonoDevelop.LibraryManager.Commands
{
    class RestoreLibrariesHandler : CommandHandler
    {
        protected override async Task UpdateAsync(CommandInfo info, CancellationToken cancelToken)
        {
            var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
            if (solution == null)
                return;

            if (await VsHelpers.SolutionContainsManifestFileAsync(solution))
            {
                info.Visible = true;
                info.Enabled =
                    IdeApp.ProjectOperations.CurrentBuildOperation.IsCompleted &&
                    IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted &&
                    !LibraryManagerService.LibraryCommandService.IsOperationInProgress;
            }
            else
            {
                info.Visible = false;
            }
        }

        protected override void Run()
        {
            RunAsync().Ignore();
        }

        async Task RunAsync()
        {
            LibraryManagerService.Initialize();
            var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
            if (solution == null)
                return;

            var configFiles = new List<string> ();

            foreach (var project in solution.GetAllProjects())
            {
                if (await VsHelpers.ProjectContainsManifestFileAsync(project))
                {
                    string rootPath = await project.GetRootFolderAsync();
                    string configFilePath = Path.Combine(rootPath, Constants.ConfigFileName);
                    configFiles.Add(configFilePath);
                }
            }

            await LibraryManagerService.LibraryCommandService.RestoreAsync(configFiles, CancellationToken.None);
        }
    }
}
