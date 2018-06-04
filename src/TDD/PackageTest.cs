/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/


using BruSoft.VS2P4;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VsSDK.UnitTestLibrary;

namespace Microsoft.Samples.VisualStudio.SourceControlIntegration.SccProvider.UnitTests
{
	[TestClass()]
	public class PackageTest
	{
		[TestMethod()]
		public void CreateInstance()
		{
			VS2P4Package package = new VS2P4Package();
		}

        [TestMethod()]
		public void IsIVsPackage()
		{
			VS2P4Package package = new VS2P4Package();
            Assert.IsNotNull(package as IVsPackage, "The object does not implement IVsPackage");
		}

        [TestMethod()]
		public void SetSite()
		{
			// Create the package
			IVsPackage package = new VS2P4Package() as IVsPackage;
            Assert.IsNotNull(package, "The object does not implement IVsPackage");

            // Create a basic service provider
            OleServiceProvider serviceProvider = OleServiceProvider.CreateOleServiceProviderWithBasicServices();

            // Need to mock a service implementing IVsRegisterScciProvider, because the scc provider will register with it
            IVsRegisterScciProvider registerScciProvider = MockRegisterScciProvider.GetBaseRegisterScciProvider();
            serviceProvider.AddService(typeof(IVsRegisterScciProvider), registerScciProvider, true);

            // Register solution events because the provider will try to subscribe to them
            MockSolution solution = new MockSolution();
            serviceProvider.AddService(typeof(SVsSolution), solution as IVsSolution, true);

            // Register TPD service because the provider will try to subscribe to TPD
            IVsTrackProjectDocuments2 tpd = MockTrackProjectDocumentsProvider.GetTrackProjectDocuments() as IVsTrackProjectDocuments2;
            serviceProvider.AddService(typeof(SVsTrackProjectDocuments), tpd, true);

			// Site the package
            Assert.AreEqual(0, package.SetSite(serviceProvider), "SetSite did not return S_OK");

			// Unsite the package
            Assert.AreEqual(0, package.SetSite(null), "SetSite(null) did not return S_OK");
		}
	}
}
