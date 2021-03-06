﻿//
// ClearLibrariesInProjectHandler.cs
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Vsix;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.LibraryManager.Vsix;
using MonoDevelop.Projects;

namespace MonoDevelop.LibraryManager.Commands
{
    public class CleanLibrariesInProjectHandler : CommandHandler
    {
        protected override async Task UpdateAsync(CommandInfo info, CancellationToken cancelToken)
        {
            ProjectFile item = await VsHelpers.GetSelectedItemAsync<ProjectFile>();

            if (item != null && item.FilePath.FileName.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
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
            ProjectFile configProjectItem = await VsHelpers.GetSelectedItemAsync<ProjectFile>();

            if (configProjectItem != null)
            {
                LibraryManagerService.Initialize ();
                await LibraryManagerService.LibraryCommandService.CleanAsync(configProjectItem, CancellationToken.None);
            }
        }
    }
}
