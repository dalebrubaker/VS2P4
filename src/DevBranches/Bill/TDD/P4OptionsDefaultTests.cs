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
    public class P4OptionsDefaultTests
    {
        /// <summary>
        /// Test a single group with all settings specified.
        /// </summary>
        [TestMethod]
        public void SingleGroupAllSettings()
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
            P4OptionsDefaultsProvider defaults = new P4OptionsDefaultsProvider(new List<string> { "TestConfig" });

            Assert.IsTrue(defaults.UseP4Config, "UseP4Config should be true.");
            Assert.AreEqual(defaults.PerforceServer, "TestServer", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "TestUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "TestWorkspace", "Unexpected value for PerforceWorkspace");
            Assert.AreEqual(defaults.WorkspacePath, "TestWorkspacePath", "Unexpected value for WorkspacePath");
        }

        /// <summary>
        /// Test two groups of settings, with each group providing only some of the values.  Test the groups
        /// in both possible orders to verify the priorities are correct.
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

            // Test using group 1 before group 2
            P4OptionsDefaultsProvider defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup1", "TestGroup2" });

            Assert.IsFalse(defaults.UseP4Config, "UseP4Config should be false");
            Assert.AreEqual(defaults.PerforceServer, "TestServer1", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "MyUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "AWorkspace", "Unexpected value for PerforceWorkspace");
            Assert.AreEqual(defaults.WorkspacePath, "TestWorkspacePath", "Unexpected value for WorkspacePath");

            // Test using group 2 before group 1
            defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup2", "TestGroup1" });

            Assert.IsFalse(defaults.UseP4Config, "UseP4Config should be false");
            Assert.AreEqual(defaults.PerforceServer, "AServer", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "MyUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "AWorkspace", "Unexpected value for PerforceWorkspace");
            Assert.AreEqual(defaults.WorkspacePath, "TestWorkspacePath", "Unexpected value for WorkspacePath");
        }

        /// <summary>
        /// Test 3 groups in various combinations, where each group contains 2 settings each.
        /// </summary>
        [TestMethod]
        public void ThreeGroupsTwoEach()
        {
            string xml = "<DefaultUserSettings xmlns=\"http://tempuri.org/DefaultUserSettings.xsd\">" +
                         "  <Group context=\"UnitTest\" name=\"TestGroup1\">" +
                         "    <PerforceServer value=\"TestServer1\"/>" +
                         "    <PerforceWorkspace value=\"AWorkspace\"/>" +
                         "  </Group>" +
                         "  <Group context=\"UnitTest\" name=\"TestGroup2\">" +
                         "    <PerforceServer value=\"AServer\"/>" +
                         "    <PerforceUser value=\"MyUser\"/>" +
                         "  </Group>" +
                         "  <Group context=\"UnitTest\" name=\"TestGroup3\">" +
                         "    <UseP4Config value=\"true\"/>" +
                         "    <PerforceUser value=\"SomeUser\"/>" +
                         "  </Group>" +
                         "</DefaultUserSettings>";
            XmlReader xmlReader = XmlReader.Create(new StringReader(xml));
            UserSettings.UserSettingsXML = xmlReader;

            // Test using group 1, 2, 3
            P4OptionsDefaultsProvider defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup1", "TestGroup2", "TestGroup3" });

            Assert.IsTrue(defaults.UseP4Config, "UseP4Config should be true");
            Assert.AreEqual(defaults.PerforceServer, "TestServer1", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "MyUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "AWorkspace", "Unexpected value for PerforceWorkspace");

            // Test using group 2, 3, 1
            defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup2", "TestGroup3", "TestGroup1" });

            Assert.IsTrue(defaults.UseP4Config, "UseP4Config should be true");
            Assert.AreEqual(defaults.PerforceServer, "AServer", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "MyUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "AWorkspace", "Unexpected value for PerforceWorkspace");

            // Test using group 3, 2, 1
            defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup3", "TestGroup2", "TestGroup1" });

            Assert.IsTrue(defaults.UseP4Config, "UseP4Config should be true");
            Assert.AreEqual(defaults.PerforceServer, "AServer", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "SomeUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "AWorkspace", "Unexpected value for PerforceWorkspace");

            // Test using group 2, 1
            defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup2", "TestGroup1" });

            Assert.IsFalse(defaults.UseP4Config, "UseP4Config should be false");
            Assert.AreEqual(defaults.PerforceServer, "AServer", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "MyUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "AWorkspace", "Unexpected value for PerforceWorkspace");

            // Test using group 1, 3
            defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup1", "TestGroup3" });

            Assert.IsTrue(defaults.UseP4Config, "UseP4Config should be true");
            Assert.AreEqual(defaults.PerforceServer, "TestServer1", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "SomeUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "AWorkspace", "Unexpected value for PerforceWorkspace");

            // Test using group 3, 2
            defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup3", "TestGroup2" });

            Assert.IsTrue(defaults.UseP4Config, "UseP4Config should be true");
            Assert.AreEqual(defaults.PerforceServer, "AServer", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "SomeUser", "Unexpected value for PerforceUser");
            Assert.IsNull(defaults.PerforceWorkspace, "Unexpected value for PerforceWorkspace");
        }

        /// <summary>
        /// Test 3 groups in various orders.
        /// </summary>
        [TestMethod]
        public void ThreeGroupsSomeSettings()
        {
            string xml = "<DefaultUserSettings xmlns=\"http://tempuri.org/DefaultUserSettings.xsd\">" +
                         "  <Group context=\"UnitTest\" name=\"TestGroup1\">" +
                         "    <UseP4Config value=\"false\"/>" +
                         "    <PerforceServer value=\"TestServer1\"/>" +
                         "    <PerforceWorkspace value=\"AWorkspace\"/>" +
                         "  </Group>" +
                         "  <Group context=\"UnitTest\" name=\"TestGroup2\">" +
                         "    <PerforceServer value=\"AServer\"/>" +
                         "    <PerforceUser value=\"MyUser\"/>" +
                         "  </Group>" +
                         "  <Group context=\"UnitTest\" name=\"TestGroup3\">" +
                         "    <UseP4Config value=\"true\"/>" +
                         "    <PerforceUser value=\"SomeUser\"/>" +
                         "    <PerforceWorkspace value=\"MyWorkspace\"/>" +
                         "    <WorkspacePath value=\"TestWorkspacePath\"/>" +
                         "  </Group>" +
                         "</DefaultUserSettings>";
            XmlReader xmlReader = XmlReader.Create(new StringReader(xml));
            UserSettings.UserSettingsXML = xmlReader;

            // Test using group 1, 2, 3
            P4OptionsDefaultsProvider defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup1", "TestGroup2", "TestGroup3" });

            Assert.IsFalse(defaults.UseP4Config, "UseP4Config should be false");
            Assert.AreEqual(defaults.PerforceServer, "TestServer1", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "MyUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "AWorkspace", "Unexpected value for PerforceWorkspace");
            Assert.AreEqual(defaults.WorkspacePath, "TestWorkspacePath", "Unexpected value for WorkspacePath");

            // Test using group 2, 3, 1
            defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup2", "TestGroup3", "TestGroup1" });

            Assert.IsTrue(defaults.UseP4Config, "UseP4Config should be true");
            Assert.AreEqual(defaults.PerforceServer, "AServer", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "MyUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "MyWorkspace", "Unexpected value for PerforceWorkspace");
            Assert.AreEqual(defaults.WorkspacePath, "TestWorkspacePath", "Unexpected value for WorkspacePath");

            // Test using group 3, 1
            defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup3", "TestGroup1" });

            Assert.IsTrue(defaults.UseP4Config, "UseP4Config should be true");
            Assert.AreEqual(defaults.PerforceServer, "TestServer1", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "SomeUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "MyWorkspace", "Unexpected value for PerforceWorkspace");
            Assert.AreEqual(defaults.WorkspacePath, "TestWorkspacePath", "Unexpected value for WorkspacePath");

            // Test using group 2, 1
            defaults = new P4OptionsDefaultsProvider(new List<string> { "TestGroup2", "TestGroup1" });

            Assert.IsFalse(defaults.UseP4Config, "UseP4Config should be false");
            Assert.AreEqual(defaults.PerforceServer, "AServer", "Unexpected value for PerforceServer");
            Assert.AreEqual(defaults.PerforceUser, "MyUser", "Unexpected value for PerforceUser");
            Assert.AreEqual(defaults.PerforceWorkspace, "AWorkspace", "Unexpected value for PerforceWorkspace");
            Assert.IsNull(defaults.WorkspacePath, "Unexpected value for WorkspacePath");
        }
    }
}
