using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Xml;

namespace BruSoft.VS2P4.UnitTests
{
    [TestClass]
    public class UserSettingsTests
    {
        /// <summary>
        /// Test a single group with only useP4Config specified.
        /// </summary>
        [TestMethod]
        public void OneGroupOnlyConfig()
        {
            string xml = "<DefaultUserSettings xmlns=\"http://tempuri.org/DefaultUserSettings.xsd\">"  +
                         "  <Group context=\"UnitTest\" name=\"TestConfig\">" +
                         "    <UseP4Config value=\"true\"/>" +
                         "  </Group>" +
                         "</DefaultUserSettings>";
            XmlReader xmlReader = XmlReader.Create(new StringReader(xml));
            UserSettings.UserSettingsXML = xmlReader;
            UserSettings testSettings = UserSettings.GetSettings("TestConfig");
            Assert.IsNotNull(testSettings.UseP4Config, "UseP4Config should not be null.");
            Assert.IsTrue(testSettings.UseP4Config.Value, "UseP4Config should be true.");
            Assert.IsNull(testSettings.PerforceServer, "PerforceServer should be null.");
            Assert.IsNull(testSettings.PerforceUser, "PerforceUser should be null.");
            Assert.IsNull(testSettings.PerforceWorkspace, "PerforceWorkspace should be null.");
        }

        /// <summary>
        /// Test a single group with all of the attributes specified.
        /// </summary>
        [TestMethod]
        public void OneGroupAllSettings()
        {
            string xml = "<DefaultUserSettings xmlns=\"http://tempuri.org/DefaultUserSettings.xsd\">" +
                         "  <Group context=\"UnitTest\" name=\"TestConfig\">" +
                         "    <UseP4Config value=\"true\"/>" +
                         "    <PerforceServer value=\"TestServer\"/>" +
                         "    <PerforceUser value=\"TestUser\"/>" +
                         "    <PerforceWorkspace value=\"TestWorkspace\"/>" +
                         "    <WorkspacePath value=\"TestWorkspacePath\"/>" +
                         "  </Group>" +
                         "</DefaultUserSettings>";
            XmlReader xmlReader = XmlReader.Create(new StringReader(xml));
            UserSettings.UserSettingsXML = xmlReader;
            UserSettings testSettings = UserSettings.GetSettings("TestConfig");
            Assert.IsNotNull(testSettings.UseP4Config, "UseP4Config should not be null.");
            Assert.IsTrue(testSettings.UseP4Config.Value, "UseP4Config should be true.");
            Assert.AreEqual(testSettings.PerforceServer, "TestServer", "Unexpected value for PerforceServer");
            Assert.AreEqual(testSettings.PerforceUser, "TestUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(testSettings.PerforceWorkspace, "TestWorkspace", "Unexpected value for PerforceWorkspace");
            Assert.AreEqual(testSettings.WorkspacePath, "TestWorkspacePath", "Unexpected value for WorkspacePath");
        }

        /// <summary>
        /// Test two groups with some of the settings specified, some not.
        /// </summary>
        [TestMethod]
        public void TwoGroupsSomeSettings()
        {
            string xml = "<DefaultUserSettings xmlns=\"http://tempuri.org/DefaultUserSettings.xsd\">" +
                         "  <Group context=\"UnitTest\" name=\"TestGroup1\">" +
                         "    <UseP4Config value=\"false\"/>" +
                         "    <PerforceServer value=\"TestServer1\"/>" +
                         "    <PerforceWorkspace value=\"AWorkspace\"/>" +
                         "    <WorkspacePath value=\"TestWorkspacePath\"/>" +
                         "  </Group>" +
                         "  <Group context=\"UnitTest\" name=\"TestGroup2\">" +
                         "    <PerforceServer value=\"AServer\"/>" +
                         "    <PerforceUser value=\"MyUser\"/>" +
                         "  </Group>" +
                         "</DefaultUserSettings>";
            XmlReader xmlReader = XmlReader.Create(new StringReader(xml));
            UserSettings.UserSettingsXML = xmlReader;
            // Group 1
            UserSettings testSettings = UserSettings.GetSettings("TestGroup1");
            Assert.IsNotNull(testSettings.UseP4Config, "UseP4Config should not be null.");
            Assert.IsFalse(testSettings.UseP4Config.Value, "UseP4Config should be false.");
            Assert.AreEqual(testSettings.PerforceServer, "TestServer1", "Unexpected value for PerforceServer");
            Assert.AreEqual(testSettings.PerforceUser, null, "Unexpected value for PerforceUser");
            Assert.AreEqual(testSettings.PerforceWorkspace, "AWorkspace", "Unexpected value for PerforceWorkspace");
            Assert.AreEqual(testSettings.WorkspacePath, "TestWorkspacePath", "Unexpected value for WorkspacePath");

            // Group 2
            testSettings = UserSettings.GetSettings("TestGroup2");
            Assert.IsNull(testSettings.UseP4Config, "UseP4Config should be null.");
            Assert.AreEqual(testSettings.PerforceServer, "AServer", "Unexpected value for PerforceServer");
            Assert.AreEqual(testSettings.PerforceUser, "MyUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(testSettings.PerforceWorkspace, null, "Unexpected value for PerforceWorkspace");
        }
    }
}
