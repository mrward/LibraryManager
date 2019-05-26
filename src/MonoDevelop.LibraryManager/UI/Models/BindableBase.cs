// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MonoDevelop.LibraryManager.UI.Models
{
    internal class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool Set<T>(ref T value, T newValue, IEqualityComparer<T> comparer = null, [CallerMemberName] string propertyName = null)
        {
            IEqualityComparer<T> comparerToUse = comparer ?? EqualityComparer<T>.Default;

            if (!comparerToUse.Equals(value, newValue))
            {
                value = newValue;
                OnPropertyChangedCore(propertyName);
                return true;
            }

            return false;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChangedCore(propertyName);
        }

        void OnPropertyChangedCore(string propertyName)
        {
            Debug.Assert(propertyName != null, "Property name cannot be null");
            Core.Runtime.RunInMainThread (() =>
            {
                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs(propertyName));
            });
        }
    }
}