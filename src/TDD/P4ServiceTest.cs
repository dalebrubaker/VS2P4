using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Collections.Generic;

namespace BruSoft.VS2P4.UnitTests
{
    /// <summary>
    /// This is a test class for P4ServiceTest and is intended
    ///     to contain all P4ServiceTest Unit Tests.
    /// Note that these connect to a live Perforce server installation.
    ///</summary>
    [TestClass()]
    public class P4ServiceTest
    {
        private static PersistedP4OptionSettings settings;

        internal string PASSWORD = "";

        private Map _map;

        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            // For these provider tests, we're going to use a settings group specifically named
            // SccProviderServiceTest.  Note: If tests are ever added that specifically need a 
            // streams depot, those tests will have to use a different settings group.
            P4OptionsDefaultsProvider defaults = new P4OptionsDefaultsProvider(new List<string> { "P4ServiceTest", "UnitTestDefaults" });
            settings = new PersistedP4OptionSettings(defaults);
            UserSettings.UserSettingsXML = null;
        }

        //Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize()
        {
            _map = new Map(false);
        }


        [TestMethod]
        [ExpectedException(typeof(Perforce.P4.P4Exception))]
        public void ConnectWithBadPortTest()
        {
            var target = new P4Service("A_BAD_PORT", settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                target.Disconnect();
                target.Dispose();
            }
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConnectWithBadWorkspaceTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, "A_BAD_WORKSPACE", false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                target.Disconnect();
                target.Dispose();
            }
        }

        //[TestMethod]
        //[ExpectedException(typeof(ArgumentException))]
        //public void ConnectWithPortEmptyStringTest()
        //{
        //    var target =  new P4Service("", settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, new Map());
        //    try
        //    {
        //        target.Connect();
        //    }
        //    finally
        //    {
        //        target.Disconnect();
        //        target.Dispose();
        //    }
        //}

        [TestMethod]
        public void ConnectDisconnectTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "P4Service cannot connect");
                target.Disconnect();
                Assert.IsFalse(target.IsConnected, "P4Service cannot disconnect");
                target.Dispose();
            }
        }

        /// <summary>
        ///A test for AddFile
        ///</summary>
        [TestMethod()]
        public void AddFileTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();
                string message;
                bool result = target.AddFile(filePath, out message);
                Assert.IsTrue(result, "AddFileTest error: " + message);
                target.Disconnect();
                target.Dispose();
            }
        }


        /// <summary>
        /// Create an empty file with a random file name in the TestPath,
        ///     which points to our test Perforce workspace.
        /// </summary>
        /// <returns></returns>
        private string GetTempFileName()
        {
            return GetTempFileName(settings);
        }

             /// <summary>
        /// Create an empty file with a random file name in the TestPath,
        ///     which points to our test Perforce workspace.
        /// </summary>
        /// <returns></returns>
        internal string GetTempFileName(PersistedP4OptionSettings settings)
        {
            if (!Directory.Exists(settings.WorkspacePath))
            {
                Directory.CreateDirectory(settings.WorkspacePath);
            }

            string fileName = Path.GetRandomFileName();
            string filePath = Path.Combine(settings.WorkspacePath, fileName);
            var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
            stream.Close();

            // register the filePath with the map so we can Send the command to Perforce
            string warning;
            if (_map == null)
            {
                _map = new Map(false);
            }
            _map.GetLocalFileName(filePath, out warning);
            Assert.IsTrue(String.IsNullOrEmpty(warning), warning);

            return filePath;
        }


        /// <summary>
        ///A test for DeleteFile
        ///</summary>
        [TestMethod()]
        public void DeleteFileTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                string message;
                bool result = target.DeleteFile(filePath, out message);
                Assert.IsTrue(result && !string.IsNullOrEmpty(message), "DeleteFile should have succeeded with warning message: " + message);
                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        ///A test for Deleting a file we are editing
        ///</summary>
        [TestMethod()]
        public void DeleteEditedFileTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddAndSubmitFile(filePath, out message);
                Assert.IsTrue(result, "AddAndSubmitFile error: " + message);

                result = target.EditFile(filePath, out message);
                Assert.IsTrue(result, "EditFile should have succeeded: " + message);

                result = target.DeleteFile(filePath, out message);
                Assert.IsFalse(result, "DeleteFile should have failed: " + message);

                //var state = target.GetP4FileState(filePath, out message);
                //Assert.IsTrue(state == FileState.DeletedAtHeadRevision, "P4 File State should be DeletedAtHeadRevision");

                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        ///A test for RevertFile
        ///</summary>
        [TestMethod()]
        public void RevertFileNotExistingTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                string message;
                bool result = target.RevertFile(filePath, out message);
                Assert.IsTrue(result, "RevertFile should have succeeded with warning. message: " + message);
                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        ///A test for RevertFile
        ///</summary>
        [TestMethod()]
        public void RevertFileAddedTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddFile(filePath, out message);
                Assert.IsTrue(result, "AddFileTest error: " + message);

                result = target.RevertFile(filePath, out message);
                Assert.IsTrue(result, "RevertFile should have succeeded with warning. message: " + message);
                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        ///A test for AddAndSubmitFile
        ///</summary>
        [TestMethod()]
        public void AddAndSubmitFileTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddAndSubmitFile(filePath, out message);
                Assert.IsTrue(result, "AddAndSubmitFileTest error: " + message);
                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        ///A test for RevertFile
        ///</summary>
        [TestMethod()]
        public void RevertFileDeletedTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddFile(filePath, out message);
                Assert.IsTrue(result, "AddFileTest error: " + message);

                result = target.RevertFile(filePath, out message);
                Assert.IsTrue(result, "RevertFile should have succeeded with warning. message: " + message);
                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        ///A test for EditFile
        ///</summary>
        [TestMethod()]
        public void EditFileTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddAndSubmitFile(filePath, out message);
                Assert.IsTrue(result, "AddAndSubmitFile error: " + message);

                result = target.EditFile(filePath, out message);
                Assert.IsTrue(result, "EditFile should have succeeded: " + message);
                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        ///A test for EditFile
        ///</summary>
        [TestMethod()]
        public void RevertIfUnchangedFileTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddAndSubmitFile(filePath, out message);
                Assert.IsTrue(result, "AddAndSubmitFileTest error: " + message);

                result = target.EditFile(filePath, out message);
                Assert.IsTrue(result, "EditFile should have succeeded: " + message);

                result = target.RevertIfUnchangedFile(filePath, out message);
                Assert.IsTrue(result, "RevertIfUnchangedFile should have succeeded: " + message);
                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        ///A test for LockFile and UnlockFile
        ///</summary>
        [TestMethod()]
        public void LockAndUnlockFileTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddAndSubmitFile(filePath, out message);
                Assert.IsTrue(result, "AddAndSubmitFile error: " + message);

                result = target.EditFile(filePath, out message);
                Assert.IsTrue(result, "EditFile should have succeeded: " + message);

                result = target.LockFile(filePath, out message);
                Assert.IsTrue(result, "LockFile should have succeeded: " + message);

                result = target.UnlockFile(filePath, out message);
                Assert.IsTrue(result, "UnlockFile should have succeeded: " + message);
                
                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        ///A test for MoveFile
        ///</summary>
        [TestMethod()]
        public void MoveFileTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddAndSubmitFile(filePath, out message);
                Assert.IsTrue(result, "AddAndSubmitFile error: " + message);

                result = target.EditFile(filePath, out message);
                Assert.IsTrue(result, "EditFile should have succeeded: " + message);

                string fileName = Path.GetRandomFileName();
                string newPath = Path.Combine(settings.WorkspacePath, fileName);

                result = target.MoveFile(filePath, newPath, out message);
                Assert.IsTrue(result, "MoveFile should have succeeded: " + message);
                target.Disconnect();
                target.Dispose();
            }
        }

        ///// <summary>
        /////A test for DiffFile
        ///// P4.Net doesn't support diff without -s flag
        /////</summary>
        //[TestMethod()]
        //public void DiffFileTest()
        //{
        //    var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null);
        //    try
        //    {
        //        target.Connect();
        //    }
        //    finally
        //    {
        //        Assert.IsTrue(target.IsConnected, "Unable to connect");
        //        string filePath = GetTempFileName(TESTPATH);

        //        string message;
        //        bool result = target.AddAndSubmitFile(filePath, out message);
        //        Assert.IsTrue(result, "AddAndSubmitFile error: " + message);

        //        result = target.EditFile(filePath, out message);
        //        Assert.IsTrue(result, "EditFile error: " + message);

        //        var writer = new StreamWriter(filePath);
        //        writer.WriteLine("Test 1 line in opened file.");
        //        writer.Close();

        //        result = target.DiffFile(filePath, out message);
        //        Assert.IsTrue(result, "DiffFile should have succeeded: " + message);

        //        target.Disconnect();
        //        target.Dispose();
        //    }
        //}

        /// <summary>
        /// Test the NotInPerforce file state
        ///</summary>
        [TestMethod()]
        public void IsNotInPerforceTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                FileState state = target.GetVsFileState(filePath, out message);
                Assert.AreEqual(state, FileState.NotInPerforce, "Should have been NotInPerforce: " + message);

                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        /// Test the OpenForAdd file state
        ///</summary>
        [TestMethod()]
        public void IsOpenForEditTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddAndSubmitFile(filePath, out message);
                Assert.IsTrue(result, "AddAndSubmitFile error: " + message);

                result = target.EditFile(filePath, out message);
                Assert.IsTrue(result, "EditFile should have succeeded: " + message);

                FileState state = target.GetVsFileState(filePath, out message);
                Assert.AreEqual(state, FileState.OpenForEdit, "Should have been OpenForEdit: " + message);

                target.Disconnect();
                target.Dispose();
           }
        }

        /// <summary>
        /// Test the OpenForAdd file state
        ///</summary>
        [TestMethod()]
        public void IsOpenForAddTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();


                string message;
                bool result = target.AddFile(filePath, out message);
                Assert.IsTrue(result, "AddFileTest error: " + message);

                FileState state = target.GetVsFileState(filePath, out message);
                Assert.AreEqual(state, FileState.OpenForAdd, "Should have been OpenForAdd: " + message);

                target.Disconnect();
                target.Dispose();
            }

        }

        ///// <summary>
        ///// Manual tests of FileState.
        ///// Commented out when finished with manual testing.
        /////</summary>
        //[TestMethod()]
        //public void ManualFileStateTest()
        //{
        //    var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, null);
        //    try
        //    {
        //        target.Connect();
        //    }
        //    finally
        //    {
        //        Assert.IsTrue(target.IsConnected, "Unable to connect");
        //        string filePath = @"d:\Sandbox\Packages\VS2P4\Test\0bwro110.ytg";
        //        //string filePath = @"d:\Sandbox\Packages\VS2P4\Test\0bwro110.ytg2";
        //        //string filePath = @"d:\Sandbox\Packages\VS2P4\Test\2c4kgvbf.2xp";
        //        //string filePath = @"d:\Sandbox\Packages\VS2P4\Test\3abll2fh.us3";

        //        string message;

        //        FileState state = target.GetVsFileState(filePath, out message);

        //        target.Disconnect();
        //        target.Dispose();
        //    }
        //}

        /// <summary>
        /// Test the Locked file state
        ///</summary>
        [TestMethod()]
        public void IsLockedTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddAndSubmitFile(filePath, out message);
                Assert.IsTrue(result, "AddAndSubmitFile error: " + message);

                result = target.EditFile(filePath, out message);
                Assert.IsTrue(result, "EditFile should have succeeded: " + message);

                result = target.LockFile(filePath, out message);
                Assert.IsTrue(result, "LockFile should have succeeded: " + message);

                FileState state = target.GetVsFileState(filePath, out message);
                Assert.AreEqual(state, FileState.Locked, "Should have been Locked: " + message);

                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        /// Test the OpenForDelete file state
        ///</summary>
        [TestMethod()]
        public void IsOpenForDeleteTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddAndSubmitFile(filePath, out message);
                Assert.IsTrue(result, "AddAndSubmitFile error: " + message);

                result = target.DeleteFile(filePath, out message);
                Assert.IsTrue(result, "DeleteFile should have succeeded: " + message);

                FileState state = target.GetVsFileState(filePath, out message);
                Assert.AreEqual(state, FileState.OpenForDelete, "Should have been OpenForDelete: " + message);

                target.Disconnect();
                target.Dispose();
            }
        }

        /// <summary>
        /// Test the CheckedInHeadRevision file state
        ///</summary>
        [TestMethod()]
        public void IsCheckedInHeadRevisionTest()
        {
            var target = new P4Service(settings.PerforceServer, settings.PerforceUser, PASSWORD, settings.PerforceWorkspace, false, null, _map);
            try
            {
                target.Connect();
            }
            finally
            {
                Assert.IsTrue(target.IsConnected, "Unable to connect");
                string filePath = GetTempFileName();

                string message;
                bool result = target.AddAndSubmitFile(filePath, out message);
                Assert.IsTrue(result, "AddAndSubmitFile error: " + message);

                FileState state = target.GetVsFileState(filePath, out message);
                Assert.AreEqual(state, FileState.CheckedInHeadRevision, "Should have been CheckedInHeadRevision: " + message);

                target.Disconnect();
                target.Dispose();
            }
        }

    
    }
}
