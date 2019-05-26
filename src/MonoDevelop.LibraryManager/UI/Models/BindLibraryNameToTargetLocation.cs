﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace MonoDevelop.LibraryManager.UI.Models
{
    /// <summary>
    /// It will fire an event whenever a valid library name is typed so as to update the target location.
    /// </summary>
    internal class BindLibraryNameToTargetLocation : BindableBase
    {
        private string _libraryName;

        internal string LibraryName
        {
            get { return _libraryName; }
            set { Set(ref _libraryName, value); }
        }

        internal BindLibraryNameToTargetLocation()
        {
            _libraryName = string.Empty;
        }
    }
}
