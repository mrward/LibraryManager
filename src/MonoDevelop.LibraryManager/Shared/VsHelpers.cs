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
                    project.AddFile(file, BuildAction.None);
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

        //    if (project.IsKind(Constants.WebsiteProject))
        //    {
        //        Command command = DTE.Commands.Item("SolutionExplorer.Refresh");

        //        if (command.IsAvailable)
        //        {
        //            DTE.ExecuteCommand(command.Name);
        //        }

        //        return;
        //    }

        //    var solutionService = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;

        //    IVsHierarchy hierarchy = null;
        //    if (solutionService != null && !ErrorHandler.Failed(solutionService.GetProjectOfUniqueName(project.UniqueName, out hierarchy)))
        //    {
        //        if (hierarchy == null)
        //        {
        //            return;
        //        }

        //        var vsProject = (IVsProject)hierarchy;

        //        await AddFilesToHierarchyAsync(hierarchy, files, logAction, cancellationToken);
        //    }
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
        //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        //    int batchSize = 10;

        //    try
        //    {
        //        IVsHierarchy hierarchy = GetHierarchy(project);
        //        IVsProjectBuildSystem bldSystem = hierarchy as IVsProjectBuildSystem;
        //        List<string> filesToRemove = filePaths.ToList();

        //        while (filesToRemove.Any())
        //        {
        //            List<string> nextBatch = filesToRemove.Take(batchSize).ToList();
        //            bool success = await DeleteProjectItemsInBatchAsync(hierarchy, nextBatch, logAction, cancellationToken);

        //            if (!success)
        //            {
        //                return false;
        //            }

        //            await System.Threading.Tasks.Task.Yield();

        //            int countToDelete = Math.Min(filesToRemove.Count(), batchSize);
        //            filesToRemove.RemoveRange(0, countToDelete);
        //        }

        //        return true;
        //    }
        //    catch
        //    {
                return false;
        //    }
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

        //public static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution, __VSENUMPROJFLAGS flags)
        //{
        //    if (solution == null)
        //    {
        //        yield break;
        //    }

        //    Guid guid = Guid.Empty;
        //    if (ErrorHandler.Failed(solution.GetProjectEnum((uint)flags, ref guid, out IEnumHierarchies enumHierarchies)) || enumHierarchies == null)
        //    {
        //        yield break;
        //    }

        //    IVsHierarchy[] hierarchy = new IVsHierarchy[1];
        //    while (ErrorHandler.Succeeded(enumHierarchies.Next(1, hierarchy, out uint fetched)) && fetched == 1)
        //    {
        //        if (hierarchy.Length > 0 && hierarchy[0] != null)
        //        {
        //            yield return hierarchy[0];
        //        }
        //    }
        //}

        //public static Project GetDTEProject(IVsHierarchy hierarchy)
        //{
        //    if (ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object obj)))
        //    {
        //        return obj as Project;
        //    }

        //    return null;
        //}

        public static bool IsCapabilityMatch(Project project, string capability)
        {
            return project.IsCapabilityMatch(capability);
        }

        //public static IVsHierarchy GetHierarchy(Project project)
        //{
        //    IVsSolution solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;

        //    if (ErrorHandler.Succeeded(solution.GetProjectOfUniqueName(project.FullName, out IVsHierarchy hierarchy)))
        //    {
        //        return hierarchy;
        //    }

        //    return null;
        //}

        public static Project GetDTEProjectFromConfig(string file)
        {
            try
            {
                //ProjectItem projectItem = DTE.Solution.FindProjectItem(file);
                //if (projectItem != null)
                //{
                //    return projectItem.ContainingProject;
                //}
            }
            catch (Exception ex)
            {
                Logger.LogEvent(ex.ToString(), LibraryManager.Contracts.LogLevel.Error);
                System.Diagnostics.Debug.Write(ex);
            }

            return null;
        }

        //private static async Task<bool> AddFilesToHierarchyAsync(IVsHierarchy hierarchy, IEnumerable<string> filePaths, Action<string, LogLevel> logAction, CancellationToken cancellationToken)
        //{
        //    int batchSize = 10;

        //    List<string> filesToAdd = filePaths.ToList();

        //    while (filesToAdd.Any())
        //    {
        //        List<string> nextBatch = filesToAdd.Take(batchSize).ToList();
        //        bool success = await AddProjectItemsInBatchAsync(hierarchy, nextBatch, logAction, cancellationToken);

        //        if (!success)
        //        {
        //            return false;
        //        }

        //        await System.Threading.Tasks.Task.Yield();

        //        int countToDelete = filesToAdd.Count() >= batchSize ? batchSize : filesToAdd.Count();
        //        filesToAdd.RemoveRange(0, countToDelete);
        //    }

        //    return true;
        //}

        //private static async Task<bool> AddProjectItemsInBatchAsync(IVsHierarchy vsHierarchy, List<string> filePaths, Action<string, LogLevel> logAction, CancellationToken cancellationToken)
        //{
        //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        //    IVsProjectBuildSystem bldSystem = vsHierarchy as IVsProjectBuildSystem;

        //    try
        //    {
        //        if (bldSystem != null)
        //        {
        //            bldSystem.StartBatchEdit();
        //        }

        //        cancellationToken.ThrowIfCancellationRequested();

        //        var vsProject = (IVsProject)vsHierarchy;
        //        VSADDRESULT[] result = new VSADDRESULT[filePaths.Count()];

        //        vsProject.AddItem(VSConstants.VSITEMID_ROOT,
        //                    VSADDITEMOPERATION.VSADDITEMOP_LINKTOFILE,
        //                    string.Empty,
        //                    (uint)filePaths.Count(),
        //                    filePaths.ToArray(),
        //                    IntPtr.Zero,
        //                    result);

        //        foreach (string filePath in filePaths)
        //        {
        //            logAction.Invoke(string.Format(Resources.Text.LibraryAddedToProject, filePath.Replace('\\', '/')), LogLevel.Operation);
        //        }
        //    }
        //    finally
        //    {
        //        if (bldSystem != null)
        //        {
        //            bldSystem.EndBatchEdit();
        //        }
        //    }

        //    return true;
        //}

        //private static async Task<bool> DeleteProjectItemsInBatchAsync(IVsHierarchy hierarchy, IEnumerable<string> filePaths, Action<string, LogLevel> logAction, CancellationToken cancellationToken)
        //{

        //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        //    IVsProjectBuildSystem bldSystem = hierarchy as IVsProjectBuildSystem;
        //    HashSet<ProjectItem> folders = new HashSet<ProjectItem>();

        //    try
        //    {
        //        if (bldSystem != null)
        //        {
        //            bldSystem.StartBatchEdit();
        //        }

        //        foreach (string filePath in filePaths)
        //        {
        //            cancellationToken.ThrowIfCancellationRequested();

        //            ProjectItem item = DTE.Solution.FindProjectItem(filePath);

        //            if (item != null)
        //            {
        //                ProjectItem parentFolder = item.Collection.Parent as ProjectItem;
        //                folders.Add(parentFolder);
        //                item.Delete();
        //                logAction.Invoke(string.Format(Resources.Text.LibraryDeletedFromProject, filePath.Replace('\\', '/')), LogLevel.Operation);
        //            }
        //        }

        //        DeleteEmptyFolders(folders);
        //    }
        //    finally
        //    {
        //        if (bldSystem != null)
        //        {
        //            bldSystem.EndBatchEdit();
        //        }
        //    }

        //    return true;
        //}

        //private static void DeleteEmptyFolders(HashSet<ProjectItem> folders)
        //{
        //    foreach (ProjectItem folder in folders)
        //    {
        //        if (folder.ProjectItems.Count == 0)
        //        {
        //            folder.Delete();
        //        }
        //    }
        //}
    }

}
