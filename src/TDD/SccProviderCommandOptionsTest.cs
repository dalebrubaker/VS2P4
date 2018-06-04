/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using BruSoft.VS2P4;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VsSDK.UnitTestLibrary;

namespace Microsoft.Samples.VisualStudio.SourceControlIntegration.SccProvider.UnitTests
{
    /// <summary>
    ///This is a test class for Microsoft.Samples.VisualStudio.SourceControlIntegration.SccProvider.SccProviderOptions and is intended
    ///to contain all Microsoft.Samples.VisualStudio.SourceControlIntegration.SccProvider.SccProviderOptions Unit Tests
    ///</summary>
    [TestClass()]
    public class SccProviderCommandOptionsTest
    {
        /// <summary>
        ///A test for OnActivate (CancelEventArgs)
        ///</summary>
        [TestMethod()]
        public void OnActivateTest()
        {
            SccProviderCommandOptions target = new SccProviderCommandOptions();

            MethodInfo method = typeof(SccProviderCommandOptions).GetMethod("OnActivate", BindingFlags.NonPublic | BindingFlags.Instance);
            CancelEventArgs e = new CancelEventArgs();
            method.Invoke(target, new object[] { e });
        }

        ///// <summary>
        /////A test for OnApply (PageApplyEventArgs)
        /////</summary>
        //[TestMethod()]
        //public void OnApplyTest()
        //{
        //    SccProviderOptions target = new SccProviderOptions();

        //    // Create a basic service provider
        //    using (OleServiceProvider serviceProvider = OleServiceProvider.CreateOleServiceProviderWithBasicServices())
        //    {
        //        // Mock the UIShell service to answer Cancel to the dialog invocation
        //        BaseMock mockUIShell = MockUiShellProvider.GetShowMessageBoxCancel();
        //        serviceProvider.AddService(typeof(IVsUIShell), mockUIShell, true);

        //        // Create an ISite wrapper over the service provider
        //        SiteWrappedServiceProvider wrappedProvider = new SiteWrappedServiceProvider(serviceProvider);
        //        target.Site = wrappedProvider;

        //        Assembly shell = typeof(Microsoft.VisualStudio.Shell.DialogPage).Assembly;
        //        Type argtype = shell.GetType("Microsoft.VisualStudio.Shell.DialogPage+PageApplyEventArgs", true);

        //        MethodInfo method = typeof(SccProviderOptions).GetMethod("OnApply", BindingFlags.NonPublic | BindingFlags.Instance);
        //        object eventargs = shell.CreateInstance(argtype.FullName);

        //        method.Invoke(target, new object[] { eventargs });
        //    }
        //}

        /// <summary>
        ///A test for OnClosed (EventArgs)
        ///</summary>
        [TestMethod()]
        public void OnClosedTest()
        {
            SccProviderCommandOptions target = new SccProviderCommandOptions();

            MethodInfo method = typeof(SccProviderCommandOptions).GetMethod("OnClosed", BindingFlags.NonPublic | BindingFlags.Instance);
            EventArgs e = new EventArgs();
            method.Invoke(target, new object[] { e });
        }

        /// <summary>
        ///A test for OnDeactivate (CancelEventArgs)
        ///</summary>
        [TestMethod()]
        public void OnDeactivateTest()
        {
            SccProviderCommandOptions target = new SccProviderCommandOptions();

            MethodInfo method = typeof(SccProviderCommandOptions).GetMethod("OnDeactivate", BindingFlags.NonPublic | BindingFlags.Instance);
            CancelEventArgs e = new CancelEventArgs();
            method.Invoke(target, new object[] { e });
        }

        /// <summary>
        ///A test for Window
        ///</summary>
        [TestMethod()]
        public void WindowTest()
        {
            SccProviderCommandOptions target = new SccProviderCommandOptions();

            PropertyInfo property = typeof(SccProviderCommandOptions).GetProperty("Window", BindingFlags.NonPublic | BindingFlags.Instance);
            IWin32Window val = property.GetValue(target, null) as IWin32Window;
            Assert.IsNotNull(val, "The property page control was not created for SccProviderCommandOptions");
        }
    }
}
