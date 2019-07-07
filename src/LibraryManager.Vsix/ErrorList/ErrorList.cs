// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.LibraryNaming;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Projects;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class ErrorList
    {
        public ErrorList(Project project, string configFileName)
        {
            Project = project;
            ProjectName = project?.Name ?? "";
            ConfigFileName = configFileName;
            Errors = new List<DisplayError>();
        }

        public string ProjectName { get; set; }
        public string ConfigFileName { get; set; }
        public List<DisplayError> Errors { get; }
        public Project Project { get; set; }

        public bool HandleErrors(IEnumerable<ILibraryOperationResult> results)
        {
            IEnumerable<string> json = File.Exists(ConfigFileName) ? File.ReadLines(ConfigFileName).ToArray() : Array.Empty<string>();

            foreach (ILibraryOperationResult result in results)
            {
                if (!result.Success)
                {
                    DisplayError[] displayErrors = result.Errors.Select(error => new DisplayError(error)).ToArray();
                    AddLineAndColumn(json, result.InstallationState, displayErrors);

                    Errors.AddRange(displayErrors);
                }
            }

            PushToErrorList();
            return Errors.Count > 0;
        }

        private static void AddLineAndColumn(IEnumerable<string> lines, ILibraryInstallationState state, DisplayError[] errors)
        {
            string libraryId = LibraryIdToNameAndVersionConverter.Instance.GetLibraryId(state?.Name, state?.Version, state?.ProviderId);

            if(string.IsNullOrEmpty(libraryId))
            {
                return;
            }

            foreach (DisplayError error in errors)
            {
                int index = 0;

                for (int i = 0; i < lines.Count(); i++)
                {
                    string line = lines.ElementAt(i);

                    if (line.Trim() == "{")
                        index = i;

                    if (line.Contains(libraryId))
                    {
                        error.Line = index > 0 ? index : i;
                        break;
                    }
                }
            }
        }

        private void PushToErrorList()
        {
            Runtime.RunInMainThread(() =>
            {
                IdeServices.TaskService.Errors.RemoveFileTasks(ConfigFileName);

                if (Errors.Count > 0)
                {
                    foreach (DisplayError error in Errors)
                    {
                        var buildError = new BuildError
                        {
                            FileName = ConfigFileName,

                            ErrorNumber = error.ErrorCode,
                            ErrorText = error.Description,

                            Column = error.Column + 1,
                            Line = error.Line + 1,

                            SourceTarget = Project
                        };

                        var task = new TaskListEntry(buildError);
                        task.DocumentationLink = error.HelpLink;

                        IdeServices.TaskService.Errors.Add(task);
                    }
                }
            }).Ignore();
        }

        public void ClearErrors()
        {
            Runtime.RunInMainThread(() =>
            {
                IdeServices.TaskService.Errors.RemoveFileTasks(ConfigFileName);
            }).Ignore();
        }
    }
}
