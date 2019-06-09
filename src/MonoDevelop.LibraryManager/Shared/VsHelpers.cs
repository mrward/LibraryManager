// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class VsHelpers
    {
        public static bool IsConfigFile(this ProjectItem item)
        {
            if (item is ProjectFile file)
            {
                return file.Name.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public static Task CheckFileOutOfSourceControlAsync(string file)
        {
            return Task.CompletedTask;
        }

        internal static async Task OpenFileAsync(string configFilePath)
        {
            if (!string.IsNullOrEmpty(configFilePath))
            {
                await Runtime.RunInMainThread(async () =>
                {
                    var fileInfo = new FileOpenInformation(configFilePath);
                    await IdeApp.Workbench.OpenDocument(fileInfo).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }

        internal static Task<T> GetSelectedItemAsync<T>()
            where T : class
        {
            var pad = IdeApp.Workbench.Pads.FirstOrDefault(p => p.Id == "ProjectPad");
            var solutionPad = pad?.Content as SolutionPad;
            if (solutionPad == null)
            {
                return null;
            }

            T item = null;
            if (!solutionPad.TreeView.MultipleNodesSelected())
            {
                var node = solutionPad.TreeView.GetSelectedNode();
                item = node.DataItem as T;
            }

            return Task.FromResult(item);
        }

        public static Task<Project> GetProjectOfSelectedItemAsync()
        {
            return Task.FromResult(IdeApp.ProjectOperations.CurrentSelectedProject);
        }

        public static async Task AddFileToProjectAsync(this Project project, string file, string itemType = null)
        {
            if (IsCapabilityMatch(project, Constants.DotNetCoreWebCapability))
            {
                return;
            }

            try
            {
                await Runtime.RunInMainThread (async () =>
                {
                    project.AddFile(file);
                    await project.SaveAsync(new ProgressMonitor()).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogEvent(ex.ToString(), LogLevel.Error);
                System.Diagnostics.Debug.Write(ex);
            }
        }

        public static async Task AddFilesToProjectAsync(Project project, IEnumerable<string> files, Action<string, LogLevel> logAction, CancellationToken cancellationToken)
        {
            if (project == null || IsCapabilityMatch(project, Constants.DotNetCoreWebCapability))
            {
                return;
            }

            await Runtime.RunInMainThread(async () =>
            {
                project.AddFiles(files.Select(file => new FilePath(file)));
                await project.SaveAsync(new ProgressMonitor()).ConfigureAwait(false);

                foreach (string filePath in files)
                {
                    logAction.Invoke(GettextCatalog.GetString("Library {0} was successfully added to project", filePath.Replace('\\', '/')), LogLevel.Operation);
                }
            }).ConfigureAwait(false);
        }

        public static Task<string> GetRootFolderAsync(this Project project)
        {
            if (project == null)
            {
                return null;
            }

            string fullPath = project.FileName;

            if (File.Exists(fullPath))
            {
                string folder = Path.GetDirectoryName(fullPath);
                return Task.FromResult(folder);
            }

            return null;
        }

        //public static bool IsKind(this Project project, params string[] kindGuids)
        //{
        //    foreach (string guid in kindGuids)
        //    {
        //        if (project.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase))
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        public static Task<bool> IsDotNetCoreWebProjectAsync(Project project)
        {
            if (project == null || IsCapabilityMatch(project, Constants.DotNetCoreWebCapability))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public static async Task<bool> DeleteFilesFromProjectAsync(Project project, IEnumerable<string> filePaths, Action<string, LogLevel> logAction, CancellationToken cancellationToken)
        {
            try
            {
                await Runtime.RunInMainThread(async () =>
                {
                    var folders = new HashSet<string>();

                    foreach (string filePath in filePaths)
                    {
                        if (!Path.IsPathRooted (filePath))
                            continue;

                        project.Files.Remove(filePath);
                        FileService.DeleteFile(filePath);

                        folders.Add(Path.GetDirectoryName(filePath));

                        logAction.Invoke(GettextCatalog.GetString("Library {0} was successfully deleted from project", filePath.Replace ('\\', '/')), LogLevel.Operation);
                    }

                    await project.SaveAsync (new ProgressMonitor ()).ConfigureAwait (false);

                    FileHelpers.DeleteEmptyFoldersFromDisk(folders, project.BaseDirectory);
                }).ConfigureAwait (false);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void SatisfyImportsOnce(this object o)
        {
            //_compositionService = _compositionService ?? GetService<SComponentModel, IComponentModel>();

            //if (_compositionService != null)
            //{
            //    _compositionService.DefaultCompositionService.SatisfyImportsOnce(o);
            //}
        }

        public static async Task<bool> ProjectContainsManifestFileAsync(Project project)
        {
            string rootPath = await GetRootFolderAsync(project);
            string configFilePath = Path.Combine(rootPath, Constants.ConfigFileName);

            if (File.Exists(configFilePath))
            {
                return true;
            }

            return false;
        }

        public static async Task<bool> SolutionContainsManifestFileAsync(Solution solution)
        {
            if (solution == null)
                return false;

             foreach (Project project in solution.GetAllProjects())
             {
                 if (project != null && await ProjectContainsManifestFileAsync(project))
                 {
                     return true;
                 }
            }

            return false;
        }

        public static bool IsCapabilityMatch(Project project, string capability)
        {
            return project.IsCapabilityMatch(capability);
        }

        public static Project GetProjectFromConfig(string file)
        {
            try
            {
                foreach (Solution solution in IdeApp.Workspace.GetAllSolutions())
                {
                    return solution.GetProjectsContainingFile(file).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Logger.LogEvent(ex.ToString(), LibraryManager.Contracts.LogLevel.Error);
                System.Diagnostics.Debug.Write(ex);
            }

            return null;
        }
    }
}
