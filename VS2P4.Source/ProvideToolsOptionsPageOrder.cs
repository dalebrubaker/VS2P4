/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Globalization;
using MsVsShell = Microsoft.VisualStudio.Shell;

namespace BruSoft.VS2P4
{
	/// <summary>
	/// This attribute registers the sort order of a Tools/Options property page.
    /// This attribute relies on an undocumented registry key named Sort in the registry
    /// heirarchy for the Tools Options page.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ProvideToolsOptionsPageOrder : MsVsShell.RegistrationAttribute
	{
        private string _categoryName = null;
        private string _pageName = null;
        private uint sortOrder;
        
        /// <summary>
		/// </summary>
        public ProvideToolsOptionsPageOrder(string categoryName, string pageName, uint sortOrder)
		{
            _categoryName = categoryName;
            _pageName = pageName;
            this.sortOrder = sortOrder;
    	}

        /// <summary>
        /// The programmatic name for this category (non localized).
        /// </summary>
        public string CategoryName
        {
            get { return _categoryName; }
        }

        /// <summary>
        /// The programmatic name for this page (non localized).
        /// </summary>
        public string PageName
        {
            get { return _pageName; }
        }

        /// <summary>
        /// Get the sort order for the page.
        /// </summary>
        public uint SortOrder
        {
            get { return sortOrder; }
        }

        private string RegistryPath
        {
            get { return string.Format(CultureInfo.InvariantCulture, "ToolsOptionsPages\\{0}\\{1}", CategoryName, PageName); }
        }

        /// <summary>
		///     Called to register this attribute with the given context.  The context
		///     contains the location where the registration inforomation should be placed.
		///     It also contains other information such as the type being registered and path information.
		/// </summary>
        public override void Register(RegistrationContext context)
		{
            // Write to the context's log what we are about to do
            context.Log.WriteLine(String.Format(CultureInfo.CurrentCulture, "Opt.Page order:\t{0}\\{1}, {2}\n", CategoryName, PageName, sortOrder));

            // Create the sort key.
            using (Key childKey = context.CreateKey(RegistryPath))
            {
                // Set the value for the Sort sub-key.
                childKey.SetValue("Sort", sortOrder);
            }
		}

        /// <summary>
        /// Unregister this visibility entry.
        /// </summary>
        public override void Unregister(RegistrationContext context)
        {
            context.RemoveValue(RegistryPath, "Sort");
        }
    }
}
