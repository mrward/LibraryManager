//
// CompletionWindowManagerExtensions.cs
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
using System.Reflection;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Core;

namespace MonoDevelop.LibraryManager.UI
{
    static class CompletionWindowManagerExtensions
    {
        internal static bool ShowWindow(
            CompletionTextEditorExtension ext,
            char firstChar,
            ICompletionDataList list,
            ICompletionWidget completionWidget,
            CodeCompletionContext completionContext)
        {
            Type type = typeof(CompletionWindowManager);
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var parameterTypes = new Type[]
            {
                typeof(CompletionTextEditorExtension),
                typeof(char),
                typeof(ICompletionDataList),
                typeof(ICompletionWidget),
                typeof(CodeCompletionContext)
            };

            var method = type.GetMethod("ShowWindow", flags, null, parameterTypes, null);
            if (method != null)
            {
                var parameters = new object[]
                {
                    ext,
                    firstChar,
                    list,
                    completionWidget,
                    completionContext
                };
                return (bool)method.Invoke(null, parameters);
            }
            else
            {
                LoggingService.LogInfo("Unable to find CompletionWindowManager.ShowWindow method");
            }
            return false;
        }
    }
}
