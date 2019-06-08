//
// TaskHandler.cs
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
using Microsoft.VisualStudio.TaskStatusCenter;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.LibraryManager.UI;

namespace MonoDevelop.LibraryManager.Tasks
{
    class TaskHandler : ITaskHandler
    {
        Task task;
        ProgressMonitor monitor;

        public TaskHandler(string taskTitle)
        {
            var pad = IdeApp.Workbench.GetPad<LibraryManagerOutputPad>();
            monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor(
                taskTitle,
                Stock.StatusSolutionOperation,
                false,
                true,
                false,
                pad,
                true);
        }

        public CancellationToken UserCancellation
        {
            get { return monitor.CancellationToken; }
        }

        public void RegisterTask(Task task)
        {
            this.task = task;
            task.ContinueWith(t => OnTaskCompleted(t), TaskScheduler.Default)
                .Ignore();
        }

        void OnTaskCompleted(Task task)
        {
            try
            {
                if (task.Exception != null)
                {
                    monitor.ReportError(task.Exception.Message, task.Exception);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("TaskHandler error", ex);
            }
            finally
            {
                monitor.Dispose();
            }
        }
    }
}
