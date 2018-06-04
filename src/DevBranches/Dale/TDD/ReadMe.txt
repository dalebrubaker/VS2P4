To set up your unit testing environment, use VS2P4\DefaultUserSettings.xsd to generate the xml file, or use the template below.
The file should be: %AppData%\VS2P4\user_settings.xml


<?xml version="1.0" encoding="utf-8"?>
<DefaultUserSettings xmlns="http://tempuri.org/DefaultUserSettings.xsd">
  <Group context="UnitTest" name="UnitTestDefaults">
    <PerforceServer value="perforce:1666"/>
    <PerforceWorkspace value="Dale_DaleBPC_Sandbox_Main"/>
    <PerforceUser value="Dale"/>
    <UseP4Config value="false"/>
    <WorkspacePath value="D:\Workspaces\Sandbox\main\Packages\Test"/>
  </Group>
  <Group context="VisualStudio" name="TestLargeSolution">
    <PerforceWorkspace value="vs2p4_test_nonstream"/>
  </Group>
  <Group context="VisualStudio" name="Default">
    <UseP4Config value="false"/>
    <PerforceServer value="localhost:1666"/>
    <PerforceUser value="Bill"/>
  </Group>
</DefaultUserSettings>