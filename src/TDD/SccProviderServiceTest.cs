/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

namespace BruSoft.VS2P4.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;

    using VS2P4;
    using Microsoft.Samples.VisualStudio.SourceControlIntegration.SccProvider.UnitTests;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VsSDK.UnitTestLibrary;

    /// <summary>
    ///This is a test class for Microsoft.Samples.VisualStudio.SourceControlIntegration.SccProvider.SccProviderService and is intended
    ///to contain all Microsoft.Samples.VisualStudio.SourceControlIntegration.SccProvider.SccProviderService Unit Tests
    ///</summary>
    [TestClass()]
    public class SccProviderServiceTest
    {
        private const int customSccGlyphBaseIndex = 0;

        private OleServiceProvider _serviceProvider;
        private MockSolution _solution;
        private VS2P4Package _sccProvider;
        private SccProviderService _sccProviderService;
        private Map _map;
        private P4ServiceTest _p4ServiceTest;
        private static PersistedP4OptionSettings settings;

        #region Additional test attributes

        //You can use the following additional attributes as you write your tests:

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            // For these provider tests, we're going to use a settings group specifically named
            // SccProviderServiceTest.  Note: If tests are ever added that specifically need a 
            // streams depot, those tests will have to use a different settings group.
            var defaults = new P4OptionsDefaultsProvider(new List<string> { "SccProviderServiceTest", "UnitTestDefaults" });
            settings = new PersistedP4OptionSettings(defaults);
            UserSettings.UserSettingsXML = null;
        }

        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}

        private TestContext _testContext;

        public TestContext TestContext
        {
            get
            {
                return _testContext;
            }

            set
            {
                _testContext = value;
            }
        }

        //Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize()
        {
            _map = new Map(false);
            var sccProviderService = GetSccProviderServiceInstance;
            _p4ServiceTest = new P4ServiceTest();
            sccProviderService.P4Service = new P4Service(settings.PerforceServer, settings.PerforceUser, _p4ServiceTest.PASSWORD, settings.PerforceWorkspace, false, null, _map);
            sccProviderService.Options = new P4Options(settings, sccProvider);
            sccProviderService.Options.Password = _p4ServiceTest.PASSWORD;
            sccProviderService.IsFirstP4CacheUpdateComplete = true; // fake it so we get to P4Service
        }

        [TestCleanup()]
        public void AfterTest()
        {
            Trace.Unindent();
            Trace.WriteLine(String.Format("Ending test {0}", _testContext.TestName));
        }

        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            Trace.Flush();
            Trace.Close();
        }

        #endregion

        /// <summary>
        /// The service provider
        ///</summary>
        OleServiceProvider serviceProvider
        {
            get
            {
                if (_serviceProvider == null)
                {
                    _serviceProvider = OleServiceProvider.CreateOleServiceProviderWithBasicServices();
                }

                return _serviceProvider;
            }
        }

        /// <summary>
        /// The solution
        ///</summary>
        MockSolution solution
        {
            get
            {
                if (_solution == null)
                {
                    _solution = new MockSolution();
                }

                return _solution;
            }
        }

        /// <summary>
        /// The provider
        ///</summary>
        VS2P4Package sccProvider
        {
            get
            {
                if (_sccProvider == null)
                {
                    // Create a provider package
                    _sccProvider = new VS2P4Package();
                }

                return _sccProvider;
            }
        }

        /// <summary>
        /// Creates a SccProviderService object
        ///</summary>
        public SccProviderService GetSccProviderServiceInstance
        {
            get
            {
                if (_sccProviderService == null)
                {
                    // Need to mock a service implementing IVsRegisterScciProvider, because the scc provider will register with it
                    IVsRegisterScciProvider registerScciProvider = MockRegisterScciProvider.GetBaseRegisterScciProvider();
                    serviceProvider.AddService(typeof(IVsRegisterScciProvider), registerScciProvider, true);

                    // Register solution events because the provider will try to subscribe to them
                    serviceProvider.AddService(typeof(SVsSolution), solution as IVsSolution, true);

                    // Register TPD service because the provider will try to subscribe to TPD
                    IVsTrackProjectDocuments2 tpd = MockTrackProjectDocumentsProvider.GetTrackProjectDocuments() as IVsTrackProjectDocuments2;
                    serviceProvider.AddService(typeof(SVsTrackProjectDocuments), tpd, true);

                    // Site the package
                    IVsPackage package = sccProvider;
                    package.SetSite(serviceProvider);

                    //  Get the source control provider service object
                    FieldInfo sccServiceMember = typeof(VS2P4Package).GetField("_sccService", BindingFlags.Instance | BindingFlags.NonPublic);
                    _sccProviderService = sccServiceMember.GetValue(sccProvider) as SccProviderService;

                    _sccProvider.IsUnitTesting = true;
                }
                return _sccProviderService;
            }
        }

        /// <summary>
        ///A test for menu command status
        ///</summary>
        void VerifyCommandStatus(OLECMDF expectedStatus, OLECMD[] command)
        {
            Guid guidCmdGroup = GuidList.guidVS2P4CmdSet;

            int result = _sccProvider.QueryStatus(ref guidCmdGroup, 1, command, IntPtr.Zero);
            Assert.AreEqual(VSConstants.S_OK, result);
            Debug.Assert((uint)(expectedStatus) == command[0].cmdf);
            Assert.AreEqual((uint)(expectedStatus), command[0].cmdf);
        }

        void VerifyCommandExecution(OLECMD[] command)
        {
            OleMenuCommandService mcs = sccProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            CommandID cmd = new CommandID(GuidList.guidVS2P4CmdSet, (int)command[0].cmdID);
            MenuCommand menuCmd = mcs.FindCommand(cmd);
            menuCmd.Invoke();
        }

        /// <summary>
        ///A test for SccProviderService creation and interfaces
        ///</summary>
        [TestMethod()]
        public void ConstructorTest()
        {
            SccProviderService target = GetSccProviderServiceInstance;

            Assert.AreNotEqual(null, target, "Could not create provider service");
            Assert.IsNotNull(target as IVsSccProvider, "The object does not implement IVsPackage");
        }

        /// <summary>
        ///A test for Active
        ///</summary>
        [TestMethod()]
        public void ActiveTest()
        {
            SccProviderService target = GetSccProviderServiceInstance;

            // After the object is created, the provider is inactive
            Assert.AreEqual(false, target.IsActive, "SccProviderService.Active was not reported correctly.");

            // Activate the provider and test the result
            target.SetActive();
            Assert.AreEqual(true, target.IsActive, "SccProviderService.Active was not reported correctly.");

            // Deactivate the provider and test the result
            target.SetInactive();
            Assert.AreEqual(false, target.IsActive, "SccProviderService.Active was not reported correctly.");
        }

        /// <summary>
        ///A test for AnyItemsUnderSourceControl (out int)
        ///</summary>
        [TestMethod()]
        public void AnyItemsUnderSourceControlTest()
        {
            SccProviderService target = GetSccProviderServiceInstance;

            int pfResult = 0;
            int actual = target.AnyItemsUnderSourceControl(out pfResult);

            // The method is not supposed to fail, and the basic provider cannot control any projects
            Assert.AreEqual(VSConstants.S_OK, pfResult, "pfResult_AnyItemsUnderSourceControl_expected was not set correctly.");
            Assert.AreEqual(0, actual, "SccProviderService.AnyItemsUnderSourceControl did not return the expected value.");
        }

        /// <summary>
        ///A test for SetActive ()
        ///</summary>
        [TestMethod()]
        public void SetActiveTest()
        {
            SccProviderService target = GetSccProviderServiceInstance;

            int actual = target.SetActive();
            Assert.AreEqual(VSConstants.S_OK, actual, "SccProviderService.SetActive failed.");
        }

        /// <summary>
        ///A test for SetInactive ()
        ///</summary>
        [TestMethod()]
        public void SetInactiveTest()
        {
            SccProviderService target = GetSccProviderServiceInstance;

            int actual = target.SetInactive();
            Assert.AreEqual(VSConstants.S_OK, actual, "SccProviderService.SetInactive failed.");
        }

        /// <summary>
        ///A test for QueryEditQuerySave interface. 
        /// Some basic tests for controlled and uncontrolled files.
        ///</summary>
        [TestMethod()]
        public void QueryEditQuerySaveTestBasic()
        {
            uint pfEditVerdict;
            uint prgfMoreInfo;
            uint pdwQSResult;
            int result;

            // Mock a solution with a project and a file
            IList<string> files;
            SccProviderService target;
            VsStateIcon[] rgsiGlyphs;
            uint[] rgdwSccStatus;
            VsStateIcon[] rgsiGlyphsFromStatus;
            MockIVsProject project = SetUpTestSolution(out files, out target, out rgsiGlyphs, out rgdwSccStatus, out rgsiGlyphsFromStatus);

            // check the functions that are not implemented
            Assert.AreEqual((int)VSConstants.S_OK, (int)target.BeginQuerySaveBatch());
            Assert.AreEqual((int)VSConstants.S_OK, (int)target.EndQuerySaveBatch());
            Assert.AreEqual((int)VSConstants.S_OK, (int)target.DeclareReloadableFile("", 0, null));
            Assert.AreEqual((int)VSConstants.S_OK, (int)target.DeclareUnreloadableFile("", 0, null));
            Assert.AreEqual((int)VSConstants.S_OK, (int)target.OnAfterSaveUnreloadableFile("", 0, null));
            Assert.AreEqual((int)VSConstants.S_OK, (int)target.IsReloadable("", out result));
            Assert.AreEqual(1, result, "Not the right return value from IsReloadable");

            // Create a basic service provider

            IVsShell shell = MockShellProvider.GetShellForCommandLine() as IVsShell;
            serviceProvider.AddService(typeof(IVsShell), shell, true);

            // Command line tests
            result = target.QueryEditFiles((uint)tagVSQueryEditFlags.QEF_ReportOnly, 1, new string[] { "Dummy.txt" }, null, null, out pfEditVerdict, out prgfMoreInfo);
            Assert.AreEqual(VSConstants.S_OK, result, "QueryEdit failed.");
            Assert.AreEqual((uint)tagVSQueryEditResult.QER_EditOK, pfEditVerdict, "QueryEdit failed.");
            Assert.AreEqual((uint)0, prgfMoreInfo, "QueryEdit failed.");

            result = target.QuerySaveFile("Dummy.txt", 0, null, out pdwQSResult);
            Assert.AreEqual(VSConstants.S_OK, result, "QuerySave failed.");
            Assert.AreEqual((uint)tagVSQuerySaveResult.QSR_SaveOK, pdwQSResult, "QueryEdit failed.");

            serviceProvider.RemoveService(typeof(SVsShell));

            // UI mode tests
            shell = MockShellProvider.GetShellForUI() as IVsShell;
            serviceProvider.AddService(typeof(SVsShell), shell, true);

            // Edit of an uncontrolled file that doesn't exist on disk
            result = target.QueryEditFiles((uint)tagVSQueryEditFlags.QEF_ReportOnly, 1, new string[] { "Dummy.txt" }, null, null, out pfEditVerdict, out prgfMoreInfo);
            Assert.AreEqual(VSConstants.S_OK, result, "QueryEdit failed.");
            Assert.AreEqual((uint)tagVSQueryEditResult.QER_EditOK, pfEditVerdict, "QueryEdit failed.");
            Assert.AreEqual((uint)0, prgfMoreInfo, "QueryEdit failed.");

            // Add only the project to source control.
            string message;
            bool success = target.P4Service.AddAndSubmitFile(project.ProjectFile, out message);
            Assert.IsTrue(success, "Expected AddAndSubmit to succeed.");
            Assert.IsTrue(target.GetFileState(project.ProjectFile) == FileState.CheckedInHeadRevision);

            // Check that solution file is not controlled
            Assert.AreEqual(FileState.NotInPerforce, target.GetFileState(solution.SolutionFile), "Incorrect status returned");

            serviceProvider.RemoveService(typeof(IVsUIShell));
        }

        /// <summary>
        ///A test for QueryEdit
        ///</summary>
        [TestMethod()]
        public void QueryEditTest()
        {
            uint pfEditVerdict;
            uint prgfMoreInfo;
            int result;

            // Mock a solution with a project and a file
            IList<string> files;
            SccProviderService target;
            VsStateIcon[] rgsiGlyphs;
            uint[] rgdwSccStatus;
            VsStateIcon[] rgsiGlyphsFromStatus;
            MockIVsProject project = SetUpTestSolution(out files, out target, out rgsiGlyphs, out rgdwSccStatus, out rgsiGlyphsFromStatus);

            // UI mode tests
            IVsShell shell = MockShellProvider.GetShellForUI() as IVsShell;
            serviceProvider.AddService(typeof(SVsShell), shell, true);

            // Make the solution read-only on disk
            File.SetAttributes(solution.SolutionFile, FileAttributes.ReadOnly);

            // QueryEdit in report mode for uncontrolled readonly file
            result = target.QueryEditFiles((uint)tagVSQueryEditFlags.QEF_ReportOnly, 1, new string[] { solution.SolutionFile }, null, null, out pfEditVerdict, out prgfMoreInfo);
            Assert.AreEqual(VSConstants.S_OK, result, "QueryEdit failed.");
            Assert.AreEqual((uint)tagVSQueryEditResult.QER_EditNotOK, pfEditVerdict, "QueryEdit failed.");
            Assert.AreEqual((uint)(tagVSQueryEditResultFlags.QER_EditNotPossible | tagVSQueryEditResultFlags.QER_ReadOnlyNotUnderScc), prgfMoreInfo, "QueryEdit failed.");

            // QueryEdit in silent mode for uncontrolled readonly file
            result = target.QueryEditFiles((uint)tagVSQueryEditFlags.QEF_SilentMode, 1, new string[] { solution.SolutionFile }, null, null, out pfEditVerdict, out prgfMoreInfo);
            Assert.AreEqual(VSConstants.S_OK, result, "QueryEdit failed.");
            Assert.AreEqual((uint)tagVSQueryEditResult.QER_EditNotOK, pfEditVerdict, "QueryEdit failed.");
            Assert.AreEqual((uint)(tagVSQueryEditResultFlags.QER_NoisyPromptRequired), (uint)(tagVSQueryEditResultFlags.QER_NoisyPromptRequired) & prgfMoreInfo, "QueryEdit failed.");

            // Mock the UIShell service to answer Yes to the dialog invocation
            BaseMock mockUIShell = MockUiShellProvider.GetShowMessageBoxYes();
            serviceProvider.AddService(typeof(IVsUIShell), mockUIShell, true);

            // QueryEdit for uncontrolled readonly file: allow the edit and make the file read-write
            //change to allow the edit and add to perforce if AutoAdd
            result = target.QueryEditFiles(0, 1, new string[] { solution.SolutionFile }, null, null, out pfEditVerdict, out prgfMoreInfo);
            Assert.AreEqual(VSConstants.S_OK, result, "QueryEdit failed.");
            Assert.AreEqual((uint)tagVSQueryEditResult.QER_EditOK, pfEditVerdict, "QueryEdit failed.");
            Assert.AreEqual((uint)0, prgfMoreInfo, "QueryEdit failed.");
            Assert.AreEqual<FileAttributes>(FileAttributes.Normal, File.GetAttributes(solution.SolutionFile), "File was not made writable");
            serviceProvider.RemoveService(typeof(IVsUIShell));

            // Add project file to Perforce
            string message;
            bool success = target.P4Service.AddAndSubmitFile(project.ProjectFile, out message);
            Assert.IsTrue(success, "Expected AddAndSubmit to succeed.");
            Assert.IsTrue(target.GetFileState(project.ProjectFile) == FileState.CheckedInHeadRevision);

            // QueryEdit in report mode for controlled readonly file
            result = target.QueryEditFiles((uint)tagVSQueryEditFlags.QEF_ReportOnly, 1, new string[] { project.ProjectFile }, null, null, out pfEditVerdict, out prgfMoreInfo);
            Assert.AreEqual(VSConstants.S_OK, result, "QueryEdit failed.");
            Assert.AreEqual((uint)tagVSQueryEditResult.QER_EditNotOK, pfEditVerdict, "QueryEdit failed.");
            Assert.AreEqual((uint)(tagVSQueryEditResultFlags.QER_EditNotPossible | tagVSQueryEditResultFlags.QER_ReadOnlyUnderScc), prgfMoreInfo, "QueryEdit failed.");

            // QueryEdit in silent mode for controlled readonly file: should allow the edit and make the file read-write
            result = target.QueryEditFiles((uint)tagVSQueryEditFlags.QEF_SilentMode, 1, new string[] { project.ProjectFile }, null, null, out pfEditVerdict, out prgfMoreInfo);
            Assert.AreEqual(VSConstants.S_OK, result, "QueryEdit failed.");
            Assert.AreEqual((uint)tagVSQueryEditResult.QER_EditOK, pfEditVerdict, "QueryEdit failed.");
            Assert.AreEqual((uint)tagVSQueryEditResultFlags.QER_MaybeCheckedout, prgfMoreInfo, "QueryEdit failed.");
            Assert.AreEqual<FileAttributes>(FileAttributes.Normal, File.GetAttributes(solution.SolutionFile), "File was not made writable");
            serviceProvider.RemoveService(typeof(IVsUIShell));
        }

        /// <summary>
        ///A test for GetVsFileState/GetSccGlyphs etc. for uncontrolled files
        ///</summary>
        [TestMethod()]
        public void TestFileStateUncontrolled()
        {
            IList<string> files;
            SccProviderService target;
            VsStateIcon[] rgsiGlyphs;
            uint[] rgdwSccStatus;
            VsStateIcon[] rgsiGlyphsFromStatus;
            MockIVsProject project = SetUpTestSolution(out files, out target, out rgsiGlyphs, out rgdwSccStatus, out rgsiGlyphsFromStatus);
            string strTooltip;
            foreach (string file in files)
            {
                int result = target.GetSccGlyph(1, new string[] { file }, rgsiGlyphs, rgdwSccStatus);
                Assert.AreEqual<int>(VSConstants.S_OK, result);
                Assert.AreEqual<VsStateIcon>(VsStateIcon.STATEICON_BLANK, rgsiGlyphs[0]);
                Assert.AreEqual<uint>((uint)__SccStatus.SCC_STATUS_NOTCONTROLLED, rgdwSccStatus[0]);

                result = target.GetSccGlyphFromStatus(rgdwSccStatus[0], rgsiGlyphsFromStatus);
                Assert.AreEqual<int>(VSConstants.S_OK, result);
                Assert.AreEqual<VsStateIcon>(rgsiGlyphs[0], rgsiGlyphsFromStatus[0]);
            }

            // Uncontrolled items should not have tooltips
            target.GetGlyphTipText(project as IVsHierarchy, VSConstants.VSITEMID_ROOT, out strTooltip);
            Assert.IsTrue(strTooltip.Length == 0);
        }

        /// <summary>
        ///A test for GetVsFileState/GetSccGlyphs etc. for checked in files
        ///</summary>
        [TestMethod()]
        public void TestFileStateCheckedIn()
        {
            IList<string> files;
            SccProviderService target;
            VsStateIcon[] rgsiGlyphs;
            uint[] rgdwSccStatus;
            VsStateIcon[] rgsiGlyphsFromStatus;
            MockIVsProject project = SetUpTestSolution(out files, out target, out rgsiGlyphs, out rgdwSccStatus, out rgsiGlyphsFromStatus);
            string strTooltip;

            foreach (string file in files)
            {
                string message;
                bool success = target.P4Service.AddAndSubmitFile(file, out message);
                Assert.IsTrue(success, "Failure adding and submitting file.");

                Assert.AreEqual(FileState.CheckedInHeadRevision, target.GetFileState(file), "Incorrect status returned");
                //Assert.AreEqual(SourceControlStatus.scsCheckedIn, target.GetFileStatus(file), "Incorrect status returned");

                int result = target.GetSccGlyph(1, new string[] { file }, rgsiGlyphs, rgdwSccStatus);
                Assert.AreEqual<int>(VSConstants.S_OK, result);
                const VsStateIcon expectedIcon = (VsStateIcon)(customSccGlyphBaseIndex + (uint)CustomSccGlyphs.CheckedIn);
                Assert.AreEqual<VsStateIcon>(expectedIcon, rgsiGlyphs[0]);
                Assert.AreEqual<uint>((uint)__SccStatus.SCC_STATUS_CONTROLLED, rgdwSccStatus[0]);

                result = target.GetSccGlyphFromStatus(rgdwSccStatus[0], rgsiGlyphsFromStatus);
                Assert.AreEqual<int>(VSConstants.S_OK, result);
                Assert.AreEqual<VsStateIcon>(rgsiGlyphs[0], rgsiGlyphsFromStatus[0]);
            }

            // Checked in items should have tooltips
            target.GetGlyphTipText(project as IVsHierarchy, VSConstants.VSITEMID_ROOT, out strTooltip);
            Assert.IsTrue(strTooltip.Length > 0);
        }


        /// <summary>
        ///A test for GetVsFileState/GetSccGlyphs etc. for files checked in, checked out, and added.
        ///</summary>
        [TestMethod()]
        public void TestFileStateCheckedOutAndAdd()
        {
            IList<string> files;
            SccProviderService target;
            VsStateIcon[] rgsiGlyphs;
            uint[] rgdwSccStatus;
            VsStateIcon[] rgsiGlyphsFromStatus;
            MockIVsProject project = SetUpTestSolution(out files, out target, out rgsiGlyphs, out rgdwSccStatus, out rgsiGlyphsFromStatus);
            string strTooltip;
            int result;

            foreach (string file in files)
            {
                string message;
                bool success = target.P4Service.AddAndSubmitFile(file, out message);
                Assert.IsTrue(success, "Failure adding and submitting file.");

                var cmdArgsCheckout = new SccProviderService.CommandArguments(file, 0);
                target.CheckoutFile(cmdArgsCheckout);
                Assert.AreEqual(FileState.OpenForEdit, target.GetFileState(file), "Incorrect status returned");

                result = target.GetSccGlyph(1, new string[] { file }, rgsiGlyphs, rgdwSccStatus);
                Assert.AreEqual<int>(VSConstants.S_OK, result);
                Assert.AreEqual<VsStateIcon>(VsStateIcon.STATEICON_CHECKEDOUT, rgsiGlyphs[0]);
                Assert.AreEqual<uint>((uint)__SccStatus.SCC_STATUS_CHECKEDOUT, rgdwSccStatus[0]);

                result = target.GetSccGlyphFromStatus(rgdwSccStatus[0], rgsiGlyphsFromStatus);
                Assert.AreEqual<int>(VSConstants.S_OK, result);
                Assert.AreEqual<VsStateIcon>(rgsiGlyphs[0], rgsiGlyphsFromStatus[0]);
            }

            // Checked out items should have tooltips, too
            target.GetGlyphTipText(project as IVsHierarchy, VSConstants.VSITEMID_ROOT, out strTooltip);
            Assert.IsTrue(strTooltip.Length > 0);

            VsStateIcon expectedIcon;

            foreach (string file in files)
            {
                var cmdArgsRevert = new SccProviderService.CommandArguments(file, 0);
                target.RevertFileIfUnchanged(cmdArgsRevert);
                Assert.AreEqual(FileState.CheckedInHeadRevision, target.GetFileState(file), "Incorrect status returned");

                result = target.GetSccGlyph(1, new string[] { file }, rgsiGlyphs, rgdwSccStatus);
                Assert.AreEqual<int>(VSConstants.S_OK, result);
                expectedIcon = (VsStateIcon)(customSccGlyphBaseIndex + (uint)CustomSccGlyphs.CheckedIn);
                Assert.AreEqual<VsStateIcon>(expectedIcon, rgsiGlyphs[0]);
                Assert.AreEqual<uint>((uint)__SccStatus.SCC_STATUS_CONTROLLED, rgdwSccStatus[0]);
            }

            // Add a new file to the project
            string pendingAddFile = GetTempFileName();

            project.AddItem(pendingAddFile);
            Assert.AreEqual(FileState.NotInPerforce, target.GetFileState(pendingAddFile), "Incorrect status returned");

            var cmdArgs = new SccProviderService.CommandArguments(pendingAddFile, 0);
            target.AddFile(cmdArgs);
            Assert.AreEqual(FileState.OpenForAdd, target.GetFileState(pendingAddFile), "Incorrect status returned");

            result = target.GetSccGlyph(1, new string[] { pendingAddFile }, rgsiGlyphs, rgdwSccStatus);
            Assert.AreEqual<int>(VSConstants.S_OK, result);
            expectedIcon = (VsStateIcon)(customSccGlyphBaseIndex + (uint)CustomSccGlyphs.Add);
            Assert.AreEqual<VsStateIcon>(expectedIcon, rgsiGlyphs[0]);
            Assert.AreEqual<uint>((uint)__SccStatus.SCC_STATUS_CHECKEDOUT, rgdwSccStatus[0]);

            // Pending add items should have tooltips, too
            target.GetGlyphTipText(project as IVsHierarchy, 1, out strTooltip);
            Assert.IsTrue(strTooltip.Length > 0);
        }

        private string GetTempFileName()
        {
            return _p4ServiceTest.GetTempFileName(settings);
        }

        private MockIVsProject SetUpTestSolution(out IList<string> files, out SccProviderService target, out VsStateIcon[] rgsiGlyphs, out uint[] rgdwSccStatus, out VsStateIcon[] rgsiGlyphsFromStatus)
        {
            target = GetSccProviderServiceInstance;
            solution.SolutionFile = GetTempFileName();
            string projectName = GetTempFileName();
            var project = new MockIVsProject(projectName);
            string fileName1 = GetTempFileName();
            project.AddItem(fileName1);
            solution.AddProject(project);
            target.IsSolutionLoaded = true;

            rgsiGlyphs = new VsStateIcon[1];
            rgsiGlyphsFromStatus = new VsStateIcon[1];
            rgdwSccStatus = new uint[1];

            // Check glyphs and statuses for uncontrolled items
            files = new string[] { solution.SolutionFile, project.ProjectFile, project.ProjectItems[0] };
            return project;
        }

        /// <summary>
        ///A test for TrackProjectDocuments for adding a new file
        ///</summary>
        [TestMethod()]
        public void TestTPDEventsAdd()
        {
            IList<string> files;
            SccProviderService target;
            VsStateIcon[] rgsiGlyphs;
            uint[] rgdwSccStatus;
            VsStateIcon[] rgsiGlyphsFromStatus;
            MockIVsProject project = SetUpTestSolution(out files, out target, out rgsiGlyphs, out rgdwSccStatus, out rgsiGlyphsFromStatus);
            target.Options.AutoAdd = true;

            // In real life, a QueryEdit call on the project file would be necessary to add/rename/delete items

            // Add a new item and fire the appropriate events
            string pendingAddFile = GetTempFileName();
            VSQUERYADDFILERESULTS[] pSummaryResultAdd = new VSQUERYADDFILERESULTS[1];
            VSQUERYADDFILERESULTS[] rgResultsAdd = new VSQUERYADDFILERESULTS[1];
            int result = target.OnQueryAddFiles(project as IVsProject, 1, new string[] { pendingAddFile }, null, pSummaryResultAdd, rgResultsAdd);
            Assert.AreEqual<int>(VSConstants.E_NOTIMPL, result);
            project.AddItem(pendingAddFile);
            result = target.OnAfterAddFilesEx(1, 1, new IVsProject[] { project as IVsProject }, new int[] { 0 }, new string[] { pendingAddFile }, null);
            Assert.AreEqual<int>(VSConstants.E_NOTIMPL, result);
            Assert.AreEqual(FileState.OpenForAdd, target.GetFileState(pendingAddFile), "Incorrect status returned");
        }

        /// <summary>
        ///A test for TrackProjectDocuments for deleting a checked-in file
        ///</summary>
        [TestMethod()]
        public void TestTPDEventsRemove()
        {
            IList<string> files;
            SccProviderService target;
            VsStateIcon[] rgsiGlyphs;
            uint[] rgdwSccStatus;
            VsStateIcon[] rgsiGlyphsFromStatus;
            MockIVsProject project = SetUpTestSolution(out files, out target, out rgsiGlyphs, out rgdwSccStatus, out rgsiGlyphsFromStatus);
            target.Options.AutoDelete = false;
            target.IsUnitTesting = true;

            foreach (string file in files)
            {
                string message;
                bool success = target.P4Service.AddAndSubmitFile(file, out message);
                Assert.IsTrue(success, "Failure adding and submitting file.");
            }

            string fileToDelete = files[files.Count - 1];

            // In real life, a QueryEdit call on the project file would be necessary to add/rename/delete items

            // Mock the UIShell service to answer Cancel to the dialog invocation
            BaseMock mockUIShell = MockUiShellProvider.GetShowMessageBoxCancel();
            serviceProvider.AddService(typeof(IVsUIShell), mockUIShell, true);

            // Try to delete the file from project; the delete should not be allowed
            var pSummaryResultDel = new VSQUERYREMOVEFILERESULTS[1];
            var rgResultsDel = new VSQUERYREMOVEFILERESULTS[1];
            int result = target.OnQueryRemoveFiles(project, 1, new string[] { fileToDelete }, null, pSummaryResultDel, rgResultsDel);
            Assert.AreEqual<int>(VSConstants.S_OK, result);
            Assert.AreEqual<VSQUERYREMOVEFILERESULTS>(VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveNotOK, pSummaryResultDel[0]);

            // Mock the UIShell service to answer Yes to the dialog invocation
            serviceProvider.RemoveService(typeof(IVsUIShell));
            mockUIShell = MockUiShellProvider.GetShowMessageBoxYes();
            serviceProvider.AddService(typeof(IVsUIShell), mockUIShell, true);

            // Try to remove the file from project; the remove should be allowed this time
            result = target.OnQueryRemoveFiles(project, 1, new string[] { fileToDelete }, null, pSummaryResultDel, rgResultsDel);
            Assert.AreEqual<int>(VSConstants.S_OK, result);
            Assert.AreEqual<VSQUERYREMOVEFILERESULTS>(VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveOK, pSummaryResultDel[0]);

            // Remove the file from project, but don't actually delete it (like Exclude From Project)
            project.RemoveItem(fileToDelete);
            result = target.OnAfterRemoveFiles(1, 1, new IVsProject[] { project as IVsProject }, new int[] { 0 }, new string[] { fileToDelete }, null);
            Assert.AreEqual<int>(VSConstants.S_OK, result);
            bool exists = File.Exists(fileToDelete);
            Assert.IsTrue(exists, "Expected file to exist.");
            FileState state = target.GetFileState(fileToDelete);
            Assert.AreEqual(FileState.CheckedInHeadRevision, state, "Incorrect status returned");

            // Try to remove the file from project; the remove should be allowed this time
            result = target.OnQueryRemoveFiles(project, 1, new string[] { fileToDelete }, null, pSummaryResultDel, rgResultsDel);
            Assert.AreEqual<int>(VSConstants.S_OK, result);
            Assert.AreEqual<VSQUERYREMOVEFILERESULTS>(VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveOK, pSummaryResultDel[0]);

            File.SetAttributes(fileToDelete, FileAttributes.Normal);
            File.Delete(fileToDelete);

            // Now actually mark it for delete in Perforce (like Cut or Delete)
            result = target.OnAfterRemoveFiles(1, 1, new IVsProject[] { project }, new int[] { 0 }, new string[] { fileToDelete }, null);
            Assert.AreEqual<int>(VSConstants.S_OK, result);
            state = target.GetFileState(fileToDelete);
            Assert.AreEqual(FileState.OpenForDelete, state, "Incorrect status returned");
        }

        /// <summary>
        ///A test for TrackProjectDocuments for Rename
        ///</summary>
        [TestMethod()]
        public void TestTPDEventsRename()
        {
            IList<string> files;
            SccProviderService target;
            VsStateIcon[] rgsiGlyphs;
            uint[] rgdwSccStatus;
            VsStateIcon[] rgsiGlyphsFromStatus;
            MockIVsProject project = SetUpTestSolution(out files, out target, out rgsiGlyphs, out rgdwSccStatus, out rgsiGlyphsFromStatus);
            int result;

            // In real life, a QueryEdit call on the project file would be necessary to add/rename/delete items

            // Add a checked-in file
            string fileName = GetTempFileName();
            string message;
            bool success = target.P4Service.AddAndSubmitFile(fileName, out message);
            Assert.IsTrue(success, "Failure adding and submitting file.");

            // Check out the file so we can rename it.
            var cmdArgsCheckout = new SccProviderService.CommandArguments(fileName, 0);
            target.CheckoutFile(cmdArgsCheckout);
            Assert.AreEqual(FileState.OpenForEdit, target.GetFileState(fileName), "Incorrect status returned");

            // Rename the item and verify the file remains controlled
            string newName = fileName + ".renamed";
            VSQUERYRENAMEFILERESULTS[] pSummaryResultRen = new VSQUERYRENAMEFILERESULTS[1];
            VSQUERYRENAMEFILERESULTS[] rgResultsRen = new VSQUERYRENAMEFILERESULTS[1];
            result = target.OnQueryRenameFiles(project as IVsProject, 1, new string[] { fileName }, new string[] { newName }, null, pSummaryResultRen, rgResultsRen);
            Assert.AreEqual<int>(VSConstants.S_OK, result);
            project.RenameItem(fileName, newName);
            result = target.OnAfterRenameFiles(1, 1, new IVsProject[] { project as IVsProject }, new int[] { 0 }, new string[] { fileName }, new string[] { newName }, new VSRENAMEFILEFLAGS[] { VSRENAMEFILEFLAGS.VSRENAMEFILEFLAGS_NoFlags });
            Assert.AreEqual<int>(VSConstants.S_OK, result);
            Assert.AreEqual(FileState.OpenForRenameSource, target.GetFileState(fileName), "Incorrect status returned");
            Assert.AreEqual(FileState.OpenForRenameTarget, target.GetFileState(newName), "Incorrect status returned");
        }


        /// <summary>
        ///A test for scc menu commands
        ///</summary>
        [TestMethod()]
        public void TestSccMenuCommands()
        {
            int result;
            Guid badGuid = new Guid();
            Guid guidCmdGroup = GuidList.guidVS2P4CmdSet;

            OLECMD[] cmdCheckout = new OLECMD[1];
            cmdCheckout[0].cmdID = CommandId.icmdCheckout;
            OLECMD[] cmdMarkForAdd = new OLECMD[1];
            cmdMarkForAdd[0].cmdID = CommandId.icmdMarkForAdd;
            OLECMD[] cmdRevertIfUnchanged = new OLECMD[1];
            cmdRevertIfUnchanged[0].cmdID = CommandId.icmdRevertIfUnchanged;
            OLECMD[] cmdRevert = new OLECMD[1];
            cmdRevert[0].cmdID = CommandId.icmdRevert;
            OLECMD[] cmdGetLatestRevision = new OLECMD[1];
            cmdGetLatestRevision[0].cmdID = CommandId.icmdGetLatestRevison;
            OLECMD[] cmdRevisionHistory = new OLECMD[1];
            cmdRevisionHistory[0].cmdID = CommandId.icmdRevisionHistory;
            OLECMD[] cmdDiff = new OLECMD[1];
            cmdDiff[0].cmdID = CommandId.icmdDiff;
            OLECMD[] cmdTimeLapse = new OLECMD[1];
            cmdTimeLapse[0].cmdID = CommandId.icmdTimeLapse;
            OLECMD[] cmdOpenInSwarm = new OLECMD[1];
            cmdOpenInSwarm[0].cmdID = CommandId.icmdOpenInSwarm;

            //OLECMD[] cmdViewToolWindow = new OLECMD[1];
            //cmdViewToolWindow[0].cmdID = CommandId.icmdViewToolWindow;
            //OLECMD[] cmdToolWindowToolbarCommand = new OLECMD[1];
            //cmdToolWindowToolbarCommand[0].cmdID = CommandId.icmdToolWindowToolbarCommand;

            OLECMD[] cmdUnsupported = new OLECMD[1];
            cmdUnsupported[0].cmdID = 0;

            // Initialize the provider, etc
            SccProviderService target = GetSccProviderServiceInstance;

            // Mock a service implementing IVsMonitorSelection
            BaseMock monitorSelection = MockIVsMonitorSelectionFactory.GetMonSel();
            serviceProvider.AddService(typeof(IVsMonitorSelection), monitorSelection, true);

            // Commands that don't belong to our package should not be supported
            result = _sccProvider.QueryStatus(ref badGuid, 1, cmdMarkForAdd, IntPtr.Zero);
            Assert.AreEqual((int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED, result);

            // The command should be invisible when there is no solution
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdMarkForAdd);

            // Activate the provider and test the result
            target.SetActive();
            Assert.AreEqual(true, target.IsActive, "SccProviderService.Active was not reported correctly.");

            // The commands should be invisible when there is no solution
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdMarkForAdd);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdRevertIfUnchanged);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdCheckout);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdRevert);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdGetLatestRevision);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdRevisionHistory);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdDiff);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdTimeLapse);

            // Commands that don't belong to our package should not be supported
            result = _sccProvider.QueryStatus(ref guidCmdGroup, 1, cmdUnsupported, IntPtr.Zero);
            Assert.AreEqual((int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED, result);

            // Deactivate the provider and test the result
            target.SetInactive();
            Assert.AreEqual(false, target.IsActive, "SccProviderService.Active was not reported correctly.");

            // Mock a solution with a project and a file
            IList<string> files;
            VsStateIcon[] rgsiGlyphs;
            uint[] rgdwSccStatus;
            VsStateIcon[] rgsiGlyphsFromStatus;
            MockIVsProject project = SetUpTestSolution(out files, out target, out rgsiGlyphs, out rgdwSccStatus, out rgsiGlyphsFromStatus);

            // The commands should be invisible when the provider is not active
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdMarkForAdd);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdRevertIfUnchanged);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdCheckout);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdRevert);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdGetLatestRevision);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdRevisionHistory);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdDiff);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdTimeLapse);

            //VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdViewToolWindow);
            //VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdToolWindowToolbarCommand);

            // Activate the provider and test the result
            target.SetActive();
            Assert.AreEqual(true, target.IsActive, "SccProviderService.Active was not reported correctly.");

            Thread.Sleep(2000);

            // Reset the test settings (were overwritten by SetActive()).
            ResetTestSettings(target);

            // The command should be visible but disabled now, except the toolwindow ones, depending on options
            target.Options.IsAddEnabled = false;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdMarkForAdd);
            target.Options.IsAddEnabled = true;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdMarkForAdd);

            target.Options.IsRevertIfUnchangedEnabled = false;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdRevertIfUnchanged);
            target.Options.IsRevertIfUnchangedEnabled = true;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevertIfUnchanged);

            target.Options.IsCheckoutEnabled = false;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdCheckout);
            target.Options.IsCheckoutEnabled = true;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdCheckout);

            target.Options.IsRevertEnabled = false;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdRevert);
            target.Options.IsRevertEnabled = true;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevert);

            target.Options.IsGetLatestRevisionEnabled = false;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdGetLatestRevision);
            target.Options.IsGetLatestRevisionEnabled = true;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdGetLatestRevision);

            target.Options.IsViewRevisionHistoryEnabled = false;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdRevisionHistory);
            target.Options.IsViewRevisionHistoryEnabled = true;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevisionHistory);

            target.Options.IsViewDiffEnabled = false;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdDiff);
            target.Options.IsViewDiffEnabled = true;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdDiff);

            target.Options.IsViewTimeLapseEnabled = false;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdTimeLapse);
            target.Options.IsViewTimeLapseEnabled = true;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdTimeLapse);

            target.Options.IsOpenInSwarmEnabled = false;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE, cmdOpenInSwarm);
            target.Options.IsOpenInSwarmEnabled = true;
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdOpenInSwarm);

            //VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED, cmdViewToolWindow);
            //VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED, cmdToolWindowToolbarCommand);

            // Set selection to solution node
            VSITEMSELECTION selSolutionRoot;
            selSolutionRoot.pHier = _solution as IVsHierarchy;
            selSolutionRoot.itemid = VSConstants.VSITEMID_ROOT;
            monitorSelection["Selection"] = new VSITEMSELECTION[] { selSolutionRoot };

            // The add command should be available, rest should be disabled
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED, cmdMarkForAdd);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevertIfUnchanged);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdCheckout);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevert);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdGetLatestRevision);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevisionHistory);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdDiff);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdTimeLapse);

            // Still solution hierarchy, but other way
            selSolutionRoot.pHier = null;
            selSolutionRoot.itemid = VSConstants.VSITEMID_ROOT;
            monitorSelection["Selection"] = new VSITEMSELECTION[] { selSolutionRoot };

            // The add command should be available, rest should be disabled
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED, cmdMarkForAdd);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevertIfUnchanged);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdCheckout);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevert);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdGetLatestRevision);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevisionHistory);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdDiff);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdTimeLapse);

            // Set selection to project node
            VSITEMSELECTION selProjectRoot;
            selProjectRoot.pHier = project as IVsHierarchy;
            selProjectRoot.itemid = VSConstants.VSITEMID_ROOT;
            monitorSelection["Selection"] = new VSITEMSELECTION[] { selProjectRoot };

            // The add command should be available, rest should be disabled
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED, cmdMarkForAdd);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevertIfUnchanged);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdCheckout);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevert);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdGetLatestRevision);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevisionHistory);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdDiff);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdTimeLapse);

            // Set selection to project item
            VSITEMSELECTION selProjectItem;
            selProjectItem.pHier = project as IVsHierarchy;
            selProjectItem.itemid = 0;
            monitorSelection["Selection"] = new VSITEMSELECTION[] { selProjectItem };

            // The add command should be available, rest should be disabled
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED, cmdMarkForAdd);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevertIfUnchanged);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdCheckout);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevert);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdGetLatestRevision);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevisionHistory);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdDiff);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdTimeLapse);

            // Set selection to project and item node and add project to scc
            monitorSelection["Selection"] = new VSITEMSELECTION[] { selProjectRoot, selProjectItem };
            VerifyCommandExecution(cmdMarkForAdd);
            // Wait for the cache to be current.
            target.IsCacheActive_WaitForUpdate();

            // The revert command should be enabled, all others disabled because of the Mark for Add in the project
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdMarkForAdd);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevertIfUnchanged);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdCheckout);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED, cmdRevert);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdGetLatestRevision);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevisionHistory);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdDiff);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdTimeLapse);

            // Checkout the project 
            VerifyCommandExecution(cmdCheckout);
            Thread.Sleep(2000);

            // The revert command should be enabled, all others disabled because of the Mark for Add in the project
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdMarkForAdd);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevertIfUnchanged);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdCheckout);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED, cmdRevert);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdGetLatestRevision);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdRevisionHistory);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdDiff);
            VerifyCommandStatus(OLECMDF.OLECMDF_SUPPORTED, cmdTimeLapse);
        }

        /// <summary>
        /// Reset all the test settings after they've been overwritten by calling certain methods of SccProviderService.
        /// Some methods will load the Perforce connection settings from persisted or set them assuming the operation is
        /// in the context of Visual Studio.
        /// </summary>
        /// <param name="target"></param>
        private void ResetTestSettings(SccProviderService target)
        {
            // Reset the provider's options to our test settings.
            target.Options = new P4Options(settings, sccProvider);
            target.Options.Password = _p4ServiceTest.PASSWORD;

            // Redefine the P4Service to use our test parameters.
            target.P4Service = new P4Service(settings.PerforceServer, settings.PerforceUser, _p4ServiceTest.PASSWORD, settings.PerforceWorkspace,
                                             false, settings.WorkspacePath, new Map(false));
        }

        /// <summary>
        /// Tests for eligibility of each command versus its state.
        /// We don't test for some states that are complex, like those that involve locks etc. by other users. Those have been done manually.
        ///</summary>
        [TestMethod()]
        public void CommandEligibilityTests()
        {
            SccProviderService target = GetSccProviderServiceInstance;

            // Check FileState.NotInPerforce
            var fileName = GetTempFileName();
            var state = target.GetFileState(fileName);
            Assert.AreEqual(state, FileState.NotSet);
            Assert.IsTrue(target.IsEligibleForAdd(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForCheckOut(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForGetLatestRevision(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsInPerforceAndIsEligibleForDelete(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsInPerforceAndIsEligibleForRename(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevisionHistory(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForDiff(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForTimeLapse(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevert(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevertIfUnchanged(fileName), "Eligibility error.");

            // Check FileState.OpenForAdd
            fileName = GetTempFileName();
            VsSelection vsSelection = new VsSelection(new List<string>(), sccProvider.GetSolutionNodes());
            vsSelection.FileNames.Add(fileName);
            target.IsSolutionLoaded = true;
            target.AddFiles(vsSelection);
            state = target.GetFileState(fileName);
            Assert.AreEqual(state, FileState.OpenForAdd);
            Assert.IsFalse(target.IsEligibleForAdd(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForCheckOut(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForGetLatestRevision(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsInPerforceAndIsEligibleForDelete(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsInPerforceAndIsEligibleForRename(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevisionHistory(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForDiff(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForTimeLapse(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForRevert(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevertIfUnchanged(fileName), "Eligibility error.");

            // Check FileState.CheckedInHeadRevision
            string message;
            fileName = GetTempFileName();
            bool success = target.P4Service.AddAndSubmitFile(fileName, out message);
            Assert.IsTrue(success, "Expected AddAndSubmit to succeed.");
            state = target.GetFileState(fileName);
            Assert.AreEqual(state, FileState.CheckedInHeadRevision);
            Assert.IsFalse(target.IsEligibleForAdd(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForCheckOut(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForGetLatestRevision(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsInPerforceAndIsEligibleForDelete(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsInPerforceAndIsEligibleForRename(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForRevisionHistory(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForDiff(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForTimeLapse(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevert(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevertIfUnchanged(fileName), "Eligibility error.");

            // Check FileState.OpenForDelete
            vsSelection = new VsSelection(new List<string>(), sccProvider.GetSolutionNodes());
            vsSelection.FileNames.Add(fileName);
            target.DeleteFiles(vsSelection);
            state = target.GetFileState(fileName);
            Assert.AreEqual(state, FileState.OpenForDelete);
            Assert.IsTrue(target.IsEligibleForAdd(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForCheckOut(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForGetLatestRevision(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsInPerforceAndIsEligibleForDelete(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsInPerforceAndIsEligibleForRename(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForRevisionHistory(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForDiff(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForTimeLapse(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForRevert(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevertIfUnchanged(fileName), "Eligibility error.");

            // Check FileState.OpenForEdit
            target.RevertFiles(vsSelection);
            state = target.GetFileState(fileName);
            Assert.AreEqual(state, FileState.CheckedInHeadRevision);
            target.CheckoutFiles(vsSelection);
            state = target.GetFileState(fileName);
            Assert.AreEqual(state, FileState.OpenForEdit);
            Assert.IsFalse(target.IsEligibleForAdd(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForCheckOut(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForGetLatestRevision(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsInPerforceAndIsEligibleForDelete(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsInPerforceAndIsEligibleForRename(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForRevisionHistory(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForDiff(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForTimeLapse(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForRevert(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForRevertIfUnchanged(fileName), "Eligibility error.");

            // Check FileState.Locked
            target.LockFiles(vsSelection);
            state = target.GetFileState(fileName);
            Assert.AreEqual(state, FileState.Locked);
            Assert.IsFalse(target.IsEligibleForAdd(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForCheckOut(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForGetLatestRevision(fileName), "Eligibility error.");
            Assert.IsFalse(target.IsInPerforceAndIsEligibleForDelete(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsInPerforceAndIsEligibleForRename(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForRevisionHistory(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForDiff(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForTimeLapse(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForRevert(fileName), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForRevertIfUnchanged(fileName), "Eligibility error.");

            // Check FileState.OpenForRenameSource and FileState.OpenForRenameTarget
            target.RevertFiles(vsSelection);
            state = target.GetFileState(fileName);
            Assert.AreEqual(state, FileState.CheckedInHeadRevision);
            target.CheckoutFiles(vsSelection);
            state = target.GetFileState(fileName);
            Assert.AreEqual(state, FileState.OpenForEdit);

            string sourceFile = fileName;
            string targetFile = sourceFile + ".renamed";
            string pipedFile = sourceFile + "|" + targetFile;
            VsSelection vsSelectionRename = new VsSelection(new List<string>(), sccProvider.GetSolutionNodes());
            vsSelectionRename.FileNames.Add(pipedFile);
            target.RenameFiles(vsSelectionRename);
            state = target.GetFileState(sourceFile);
            Assert.AreEqual(FileState.OpenForRenameSource, state);
            Assert.IsFalse(target.IsEligibleForAdd(sourceFile), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForCheckOut(sourceFile), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForGetLatestRevision(sourceFile), "Eligibility error.");
            Assert.IsFalse(target.IsInPerforceAndIsEligibleForDelete(sourceFile), "Eligibility error.");
            Assert.IsFalse(target.IsInPerforceAndIsEligibleForRename(sourceFile), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForRevisionHistory(sourceFile), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForDiff(sourceFile), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForTimeLapse(sourceFile), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevert(sourceFile), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevertIfUnchanged(sourceFile), "Eligibility error.");

            state = target.GetFileState(targetFile);
            Assert.AreEqual(state, FileState.OpenForRenameTarget);
            Assert.IsFalse(target.IsEligibleForAdd(targetFile), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForCheckOut(targetFile), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForGetLatestRevision(targetFile), "Eligibility error.");
            Assert.IsFalse(target.IsInPerforceAndIsEligibleForDelete(targetFile), "Eligibility error.");
            Assert.IsTrue(target.IsInPerforceAndIsEligibleForRename(targetFile), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevisionHistory(targetFile), "Eligibility error.");
            Assert.IsTrue(target.IsEligibleForDiff(targetFile), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForTimeLapse(targetFile), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevert(targetFile), "Eligibility error.");
            Assert.IsFalse(target.IsEligibleForRevertIfUnchanged(targetFile), "Eligibility error.");

            // Checked (Manually)
            // OpenForEditDiffers TESTED
            // DeletedAtHeadRevision TESTED
            // CheckedInPreviousRevision TESTED
            // NeedsResolved TESTED
            // OpenForBranch TESTED
            // OpenForIntegrate TESTED

            // Still to check (Manually)
            // OpenForEditOtherUser NOT TESTED YET
            // LockedByOtherUser NOT TESTED YET
            // OpenForDeleteOtherUser NOT TESTED YET
        }

        /// <summary>
        /// Some manual tests
        ///</summary>
        [TestMethod()]
        public void CommandEligibilityTestsManual()
        {
            //SccProviderService target = GetSccProviderServiceInstance;

            //// Check FileState.CheckedInPreviousRevision
            //string fileName = @"d:\Sandbox\Packages\TestSln\Test.txt";
            //FileState state = target.GetVsFileState(fileName);
            ////Assert.AreEqual(state, FileState.CheckedInPreviousRevision);
            ////Assert.IsFalse(target.IsEligibleForAdd(fileName), "Eligibility error.");
            ////Assert.IsTrue(target.IsEligibleForCheckOut(fileName), "Eligibility error.");
            ////Assert.IsTrue(target.IsEligibleForGetLatestRevision(fileName), "Eligibility error.");
            ////Assert.IsTrue(target.IsEligibleForRemove(fileName), "Eligibility error.");
            ////Assert.IsFalse(target.IsEligibleForRename(fileName), "Eligibility error.");
            ////Assert.IsTrue(target.IsEligibleForRevisionHistory(fileName), "Eligibility error.");
            ////Assert.IsTrue(target.IsEligibleForDiff(fileName), "Eligibility error.");
            ////Assert.IsTrue(target.IsEligibleForTimeLapse(fileName), "Eligibility error.");
            ////Assert.IsFalse(target.IsEligibleForRevert(fileName), "Eligibility error.");
            ////Assert.IsFalse(target.IsEligibleForRevertIfUnchanged(fileName), "Eligibility error.");

            //// Check FileState.OpenForBranch
            //fileName = @"d:\Sandbox\Packages\TestSln\DlgQueryEditCheckedInFileTest.cs";
            //state = target.GetVsFileState(fileName);
            //Assert.AreEqual(state, FileState.OpenForBranch);
            //Assert.IsFalse(target.IsEligibleForAdd(fileName), "Eligibility error.");
            //Assert.IsFalse(target.IsEligibleForCheckOut(fileName), "Eligibility error.");
            //Assert.IsFalse(target.IsEligibleForGetLatestRevision(fileName), "Eligibility error.");
            //Assert.IsFalse(target.IsEligibleForRemove(fileName), "Eligibility error.");
            //Assert.IsFalse(target.IsEligibleForRename(fileName), "Eligibility error.");
            //Assert.IsFalse(target.IsEligibleForRevisionHistory(fileName), "Eligibility error.");
            //Assert.IsFalse(target.IsEligibleForDiff(fileName), "Eligibility error.");
            //Assert.IsFalse(target.IsEligibleForTimeLapse(fileName), "Eligibility error.");
            //Assert.IsTrue(target.IsEligibleForRevert(fileName), "Eligibility error.");
            //Assert.IsFalse(target.IsEligibleForRevertIfUnchanged(fileName), "Eligibility error.");
        }

        /// <summary>
        /// A test for opening a solution
        ///</summary>
        [TestMethod()]
        public void SolutionOpenTest()
        {
            IList<string> files;
            SccProviderService target;
            VsStateIcon[] rgsiGlyphs;
            uint[] rgdwSccStatus;
            VsStateIcon[] rgsiGlyphsFromStatus;
            MockIVsProject project = SetUpTestSolution(out files, out target, out rgsiGlyphs, out rgdwSccStatus, out rgsiGlyphsFromStatus);
            foreach (string file in files)
            {
                string message;
                bool success = target.P4Service.AddAndSubmitFile(file, out message);
                Assert.IsTrue(success, "Failure adding and submitting file.");
            }

            target.IsActive = true;
            target.OnAfterOpenSolution(null, 0);
            ResetTestSettings(target);

            VsSelection selection = sccProvider.GetSolutionSelection();
            Assert.IsTrue(selection.FileNames.Count == files.Count);

            // Wait for the cache to finish updating by calling IsCacheActive_WaitForUpdate
            Assert.IsTrue(target.IsCacheActive_WaitForUpdate(), "Cache should be active.");
            foreach (string file in files)
            {
                FileState state = target.GetFileState(file);
                Assert.IsTrue(state == FileState.CheckedInHeadRevision, String.Format("{0}: Expected file state CheckedInHeadRevision after Perforce updated, file {1}", state, file));
            }
        }

    }
}
