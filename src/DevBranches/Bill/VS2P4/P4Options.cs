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

        public P4Options(ProvideSavedSettings settingsProvider)
        {
            Server = settingsProvider.PerforceServer;
            User = settingsProvider.PerforceUser;
            UseP4Config = settingsProvider.UseP4Config.Value;
            Workspace = settingsProvider.PerforceWorkspace;
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

        const string defaultPassword = "";
#if DEBUG
        const Log.Level defaultLogLevel = Log.Level.Debug;
#else
        const Log.Level defaultLogLevel = Log.Level.Information;
#endif
        private const bool defaultCommandsEnabled = true;

            /// <summary>
        /// Load all options and return the loaded instance of this class.
        /// </summary>
        public static P4Options Load(ProvideSavedSettings settingsProvider)
        {
            P4Options p4Options = new P4Options(settingsProvider);
            p4Options.LoadPersisted(settingsProvider);
            return p4Options;
        }

        private void LoadPersisted(ProvideSavedSettings settingsProvider)
        {
            Password = settingsProvider.GetVariableExists("Password") ? (string)settingsProvider["Password"] : defaultPassword;

            if (settingsProvider.GetVariableExists("LogLevel") )
            {
                string level = (string)settingsProvider["LogLevel"];
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

            IsCheckoutEnabled = LoadBoolean("IsCheckoutEnabled", settingsProvider, defaultCommandsEnabled);
            IsAddEnabled = LoadBoolean("IsAddEnabled", settingsProvider, defaultCommandsEnabled);
            IsRevertIfUnchangedEnabled = LoadBoolean("IsRevertIfUnchangedEnabled", settingsProvider, defaultCommandsEnabled);
            IsRevertEnabled = LoadBoolean("IsRevertEnabled", settingsProvider, defaultCommandsEnabled);
            PromptBeforeRevert = LoadBoolean("PromptBeforeRevert", settingsProvider, defaultCommandsEnabled);
            IsGetLatestRevisionEnabled = LoadBoolean("IsGetLatestRevisionEnabled", settingsProvider, defaultCommandsEnabled);
            IsViewRevisionHistoryEnabled = LoadBoolean("IsViewRevisionHistoryEnabled", settingsProvider, defaultCommandsEnabled);
            IsViewDiffEnabled = LoadBoolean("IsViewDiffEnabled", settingsProvider, defaultCommandsEnabled);
            IsViewTimeLapseEnabled = LoadBoolean("IsViewTimeLapseEnabled", settingsProvider, defaultCommandsEnabled);
            AutoCheckoutOnEdit = LoadBoolean("AutoCheckoutOnEdit", settingsProvider, defaultCommandsEnabled);
            AutoCheckoutOnSave = LoadBoolean("AutoCheckoutOnSave", settingsProvider, defaultCommandsEnabled);
            AutoAdd = LoadBoolean("AutoAdd", settingsProvider, defaultCommandsEnabled);
            AutoDelete = LoadBoolean("AutoDelete", settingsProvider, defaultCommandsEnabled);
        }

        private static bool LoadBoolean(string name, ProvideSavedSettings settingsProvider, bool defaultValue)
        {
            if (settingsProvider.GetVariableExists(name))
            {
                bool isEnabled;
                bool.TryParse((string)settingsProvider[name], out isEnabled);
                return isEnabled;
            }

            return defaultValue;
        }

        /// <summary>
        /// Save all options. (Persist between sessions.)
        /// </summary>
        public void Save(ProvideSavedSettings settingsProvider)
        {
            if (settingsProvider == null)
            {
                throw new InvalidOperationException("P4Options.Save called without a settings provider.");
            }
            Save("UseP4Config", UseP4Config.ToString(), settingsProvider);
            Save("Server", Server, settingsProvider);
            Save("User", User, settingsProvider);
            Save("Password", Password, settingsProvider);
            Save("Workspace", Workspace, settingsProvider);
            Save("LogLevel", LogLevel.ToString(), settingsProvider);
            Save("IsCheckoutEnabled", IsCheckoutEnabled.ToString(), settingsProvider);
            Save("IsAddEnabled", IsAddEnabled.ToString(), settingsProvider);
            Save("IsRevertIfUnchangedEnabled", IsRevertIfUnchangedEnabled.ToString(), settingsProvider);
            Save("IsRevertEnabled", IsRevertEnabled.ToString(), settingsProvider);
            Save("PromptBeforeRevert", PromptBeforeRevert.ToString(), settingsProvider);
            Save("IsGetLatestRevisionEnabled", IsGetLatestRevisionEnabled.ToString(), settingsProvider);
            Save("IsViewRevisionHistoryEnabled", IsViewRevisionHistoryEnabled.ToString(), settingsProvider);
            Save("IsViewDiffEnabled", IsViewDiffEnabled.ToString(), settingsProvider);
            Save("IsViewTimeLapseEnabled", IsViewTimeLapseEnabled.ToString(), settingsProvider);
            Save("AutoCheckoutOnEdit", AutoCheckoutOnEdit.ToString(), settingsProvider);
            Save("AutoCheckoutOnSave", AutoCheckoutOnSave.ToString(), settingsProvider);
            Save("AutoAdd", AutoAdd.ToString(), settingsProvider);
            Save("AutoDelete", AutoDelete.ToString(), settingsProvider);
        }

        private static void Save(string variableName, string variableValue, ProvideSavedSettings settingsProvider)
        {
            if (settingsProvider.GetVariableExists(variableName))
            {
                settingsProvider[variableName] = variableValue;
            }
            else
            {
                settingsProvider[variableName] = variableValue;
                settingsProvider.SetVariablePersists(variableName, true);
            }
        }

        public override string ToString()
        {
            var txt = String.Format("{0} {1} {2}", Server, User, Workspace);
            return txt;
        }
    }
}
