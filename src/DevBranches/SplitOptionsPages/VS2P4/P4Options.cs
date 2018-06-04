using System;

using EnvDTE;
using BruSoft.VS2P4.Properties;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// This class holds the options set by SccProviderOptionsControl and allows loading them and persisting them between sessions.
    /// </summary>
    public class P4Options
    {
        public P4Options()
        {
        }

        public P4Options(string server, string user, string password, string workspace, Log.Level logLevel)
        {
            Server = server;
            User = user;
            Password = password;
            Workspace = workspace;
            LogLevel = logLevel;
        }

        public bool UseP4Config { get; set; }
        public string Server { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Workspace { get; set; }
        public Log.Level LogLevel { get; set; }
        public bool IsCheckoutEnabled { get; set; }
        public bool IsAddEnabled { get; set; }
        public bool IsRevertIfUnchangedEnabled { get; set; }
        public bool IsRevertEnabled { get; set; }
        public bool PromptBeforeRevert { get; set; }
        public bool IsGetLatestRevisionEnabled { get; set; }
        public bool IsViewRevisionHistoryEnabled { get; set; }
        public bool IsViewDiffEnabled { get; set; }
        public bool IsViewTimeLapseEnabled { get; set; }
        public bool AutoCheckoutOnEdit { get; set; }
        public bool AutoCheckoutOnSave { get; set; }
        public bool AutoAdd { get; set; }
        public bool AutoDelete { get; set; }

        private bool defaultUseP4Config = Settings.Default.PerforceUseP4Config;
        string defaultServer = Settings.Default.PerforceServer;
        string defaultUser = Settings.Default.PerforceUser;
        const string defaultPassword = "";
        string defaultWorkspace = Settings.Default.PerforceWorkspace;
#if DEBUG
        const Log.Level defaultLogLevel = Log.Level.Debug;
#else
        const Log.Level defaultLogLevel = Log.Level.Information;
#endif
        private const bool defaultCommandsEnabled = true;

            /// <summary>
        /// Load all options and return the loaded instance of this class.
        /// </summary>
        public static P4Options Load(EnvDTE80.DTE2 dte2)
        {
            P4Options p4Options = new P4Options();
            if (dte2 == null)
            {
                // This is the case during unit testing
                p4Options.LoadDefaults();
            }
            else
            {
                Globals globals = dte2.Globals;
                p4Options.LoadPersisted(globals);
            }

                return p4Options;
        }

        private void LoadPersisted(Globals globals)
        {
            UseP4Config = LoadBoolean("UseP4Config", globals, defaultUseP4Config);
            Server = globals.VariableExists["Server"] ? (string)globals["Server"] : defaultServer;
            User = globals.VariableExists["User"] ? (string)globals["User"] : defaultUser;
            Password = globals.VariableExists["Password"] ? (string)globals["Password"] : defaultPassword;
            Workspace = globals.VariableExists["Workspace"] ? (string)globals["Workspace"] : defaultWorkspace;

            if (globals.VariableExists["LogLevel"] )
            {
                string level = (string)globals["LogLevel"];
                try
                {
                    LogLevel = (Log.Level)Enum.Parse(typeof(Log.Level), level);
                }
                catch (Exception)
                {
                    LogLevel = defaultLogLevel;
                }
            }
            else
            {
                LogLevel = defaultLogLevel;
            }

            UseP4Config = LoadBoolean("UseP4Config", globals, defaultUseP4Config);
            IsCheckoutEnabled = LoadBoolean("IsCheckoutEnabled", globals, defaultCommandsEnabled);
            IsAddEnabled = LoadBoolean("IsAddEnabled", globals, defaultCommandsEnabled);
            IsRevertIfUnchangedEnabled = LoadBoolean("IsRevertIfUnchangedEnabled", globals, defaultCommandsEnabled);
            IsRevertEnabled = LoadBoolean("IsRevertEnabled", globals, defaultCommandsEnabled);
            PromptBeforeRevert = LoadBoolean("PromptBeforeRevert", globals, defaultCommandsEnabled);
            IsGetLatestRevisionEnabled = LoadBoolean("IsGetLatestRevisionEnabled", globals, defaultCommandsEnabled);
            IsViewRevisionHistoryEnabled = LoadBoolean("IsViewRevisionHistoryEnabled", globals, defaultCommandsEnabled);
            IsViewDiffEnabled = LoadBoolean("IsViewDiffEnabled", globals, defaultCommandsEnabled);
            IsViewTimeLapseEnabled = LoadBoolean("IsViewTimeLapseEnabled", globals, defaultCommandsEnabled);
            AutoCheckoutOnEdit = LoadBoolean("AutoCheckoutOnEdit", globals, defaultCommandsEnabled);
            AutoCheckoutOnSave = LoadBoolean("AutoCheckoutOnSave", globals, defaultCommandsEnabled);
            AutoAdd = LoadBoolean("AutoAdd", globals, defaultCommandsEnabled);
            AutoDelete = LoadBoolean("AutoDelete", globals, defaultCommandsEnabled);
        }

        private static bool LoadBoolean(string name, Globals globals, bool defaultValue)
        {
            if (globals.VariableExists[name])
            {
                bool isEnabled;
                bool.TryParse((string)globals[name], out isEnabled);
                return isEnabled;
            }

            return defaultValue;
        }


        private void LoadDefaults()
        {
            UseP4Config = defaultUseP4Config;
            Server = defaultServer;
            User = defaultUser;
            Password = defaultPassword;
            Workspace = defaultWorkspace;
            LogLevel = defaultLogLevel;
            IsCheckoutEnabled = defaultCommandsEnabled;
            IsAddEnabled = defaultCommandsEnabled;
            IsRevertIfUnchangedEnabled = defaultCommandsEnabled;
            IsRevertEnabled = defaultCommandsEnabled;
            PromptBeforeRevert = defaultCommandsEnabled;
            IsGetLatestRevisionEnabled = defaultCommandsEnabled;
            IsViewRevisionHistoryEnabled = defaultCommandsEnabled;
            IsViewDiffEnabled = defaultCommandsEnabled;
            IsViewTimeLapseEnabled = defaultCommandsEnabled;
            AutoCheckoutOnEdit = defaultCommandsEnabled;
            AutoCheckoutOnSave = defaultCommandsEnabled;
            AutoAdd = defaultCommandsEnabled;
            AutoDelete = defaultCommandsEnabled;
        }

        /// <summary>
        /// Save all options. (Persist between sessions.)
        /// </summary>
        public void Save(EnvDTE80.DTE2 dte2)
        {
            if (dte2 == null)
            {
                return;
            }

            Globals globals = dte2.Globals;
            Save("UseP4Config", UseP4Config.ToString(), globals);
            Save("Server", Server, globals);
            Save("User", User, globals);
            Save("Password", Password, globals);
            Save("Workspace", Workspace, globals);
            Save("LogLevel", LogLevel.ToString(), globals);
            Save("IsCheckoutEnabled", IsCheckoutEnabled.ToString(), globals);
            Save("IsAddEnabled", IsAddEnabled.ToString(), globals);
            Save("IsRevertIfUnchangedEnabled", IsRevertIfUnchangedEnabled.ToString(), globals);
            Save("IsRevertEnabled", IsRevertEnabled.ToString(), globals);
            Save("PromptBeforeRevert", PromptBeforeRevert.ToString(), globals);
            Save("IsGetLatestRevisionEnabled", IsGetLatestRevisionEnabled.ToString(), globals);
            Save("IsViewRevisionHistoryEnabled", IsViewRevisionHistoryEnabled.ToString(), globals);
            Save("IsViewDiffEnabled", IsViewDiffEnabled.ToString(), globals);
            Save("IsViewTimeLapseEnabled", IsViewTimeLapseEnabled.ToString(), globals);
            Save("AutoCheckoutOnEdit", AutoCheckoutOnEdit.ToString(), globals);
            Save("AutoCheckoutOnSave", AutoCheckoutOnSave.ToString(), globals);
            Save("AutoAdd", AutoAdd.ToString(), globals);
            Save("AutoDelete", AutoDelete.ToString(), globals);
        }

        private static void Save(string variableName, string variableValue, Globals globals)
        {
            if (globals.VariableExists[variableName])
            {
                globals[variableName] = variableValue;
            }
            else
            {
                globals[variableName] = variableValue;
                globals.VariablePersists[variableName] = true;
            }
        }

        public override string ToString()
        {
            var txt = String.Format("{0} {1} {2}", Server, User, Workspace);
            return txt;
        }
    }
}
