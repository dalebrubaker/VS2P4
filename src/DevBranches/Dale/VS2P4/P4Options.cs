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
        public P4Options(ProvideSavedSettings settingsProvider)
        {
            Server = settingsProvider.PerforceServer;
            User = settingsProvider.PerforceUser;
            UseP4Config = settingsProvider.UseP4Config == null ? true : settingsProvider.UseP4Config.Value;
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
        public bool IgnoreFilesNotUnderP4Root { get; set; }

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
            var p4Options = new P4Options(settingsProvider);
            p4Options.LoadPersisted(settingsProvider);
            return p4Options;
        }

        /// <summary>
        /// Load persisted options except the connection options, which are loaded in the ctor
        /// </summary>
        /// <param name="settingsProvider"></param>
        private void LoadPersisted(ProvideSavedSettings settingsProvider)
        {
            var passwordId = OptionName.OptionNameForLoad(OptionName.SettingIds.Password);
            Password = settingsProvider.GetVariableExists(passwordId) ? (string)settingsProvider[OptionName.SettingIds.Password] : defaultPassword;
            var logLevelId = OptionName.OptionNameForLoad(OptionName.SettingIds.LogLevel);
            if (settingsProvider.GetVariableExists(logLevelId))
            {
                string level = (string)settingsProvider[OptionName.SettingIds.LogLevel];
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

            IsCheckoutEnabled = LoadBoolean(OptionName.SettingIds.IsCheckoutEnabled, settingsProvider, defaultCommandsEnabled);
            IsAddEnabled = LoadBoolean(OptionName.SettingIds.IsAddEnabled, settingsProvider, defaultCommandsEnabled);
            IsRevertIfUnchangedEnabled = LoadBoolean(OptionName.SettingIds.IsRevertIfUnchangedEnabled, settingsProvider, defaultCommandsEnabled);
            IsRevertEnabled = LoadBoolean(OptionName.SettingIds.IsRevertEnabled, settingsProvider, defaultCommandsEnabled);
            PromptBeforeRevert = LoadBoolean(OptionName.SettingIds.PromptBeforeRevert, settingsProvider, defaultCommandsEnabled);
            IsGetLatestRevisionEnabled = LoadBoolean(OptionName.SettingIds.IsGetLatestRevisionEnabled, settingsProvider, defaultCommandsEnabled);
            IsViewRevisionHistoryEnabled = LoadBoolean(OptionName.SettingIds.IsViewRevisionHistoryEnabled, settingsProvider, defaultCommandsEnabled);
            IsViewDiffEnabled = LoadBoolean(OptionName.SettingIds.IsViewDiffEnabled, settingsProvider, defaultCommandsEnabled);
            IsViewTimeLapseEnabled = LoadBoolean(OptionName.SettingIds.IsViewTimeLapseEnabled, settingsProvider, defaultCommandsEnabled);
            AutoCheckoutOnEdit = LoadBoolean(OptionName.SettingIds.AutoCheckoutOnEdit, settingsProvider, defaultCommandsEnabled);
            AutoCheckoutOnSave = LoadBoolean(OptionName.SettingIds.AutoCheckoutOnSave, settingsProvider, defaultCommandsEnabled);
            AutoAdd = LoadBoolean(OptionName.SettingIds.AutoAdd, settingsProvider, defaultCommandsEnabled);
            AutoDelete = LoadBoolean(OptionName.SettingIds.AutoDelete, settingsProvider, defaultCommandsEnabled);
            IgnoreFilesNotUnderP4Root = LoadBoolean(OptionName.SettingIds.IgnoreFilesNotUnderP4Root2, settingsProvider, false);
            OptionName.IsFirstLoadDone = true;

            if (!settingsProvider.GetVariableExists(OptionName.OptionNameForSave(OptionName.SettingIds.Version180OrAfter)))
            {
                // This is the first time we've loaded starting with Release 1.80. So copy the old settings to the new ones.
                Save(settingsProvider);
            }
        }

        private static bool LoadBoolean(OptionName.SettingIds name, ProvideSavedSettings settingsProvider, bool defaultValue)
        {
            var variableName = OptionName.OptionNameForLoad(name);
            if (settingsProvider.GetVariableExists(variableName))
            {
                bool isEnabled;
                var variableValue = (string)settingsProvider[name];
                bool.TryParse(variableValue, out isEnabled);
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
            Save(OptionName.SettingIds.UseP4Config, UseP4Config.ToString(), settingsProvider);
            Save(OptionName.SettingIds.Server, Server, settingsProvider);
            Save(OptionName.SettingIds.User, User, settingsProvider);
            Save(OptionName.SettingIds.Password, Password, settingsProvider);
            Save(OptionName.SettingIds.Workspace, Workspace, settingsProvider);
            Save(OptionName.SettingIds.LogLevel, LogLevel.ToString(), settingsProvider);
            Save(OptionName.SettingIds.IsCheckoutEnabled, IsCheckoutEnabled.ToString(), settingsProvider);
            Save(OptionName.SettingIds.IsAddEnabled, IsAddEnabled.ToString(), settingsProvider);
            Save(OptionName.SettingIds.IsRevertIfUnchangedEnabled, IsRevertIfUnchangedEnabled.ToString(), settingsProvider);
            Save(OptionName.SettingIds.IsRevertEnabled, IsRevertEnabled.ToString(), settingsProvider);
            Save(OptionName.SettingIds.PromptBeforeRevert, PromptBeforeRevert.ToString(), settingsProvider);
            Save(OptionName.SettingIds.IsGetLatestRevisionEnabled, IsGetLatestRevisionEnabled.ToString(), settingsProvider);
            Save(OptionName.SettingIds.IsViewRevisionHistoryEnabled, IsViewRevisionHistoryEnabled.ToString(), settingsProvider);
            Save(OptionName.SettingIds.IsViewDiffEnabled, IsViewDiffEnabled.ToString(), settingsProvider);
            Save(OptionName.SettingIds.IsViewTimeLapseEnabled, IsViewTimeLapseEnabled.ToString(), settingsProvider);
            Save(OptionName.SettingIds.AutoCheckoutOnEdit, AutoCheckoutOnEdit.ToString(), settingsProvider);
            Save(OptionName.SettingIds.AutoCheckoutOnSave, AutoCheckoutOnSave.ToString(), settingsProvider);
            Save(OptionName.SettingIds.AutoAdd, AutoAdd.ToString(), settingsProvider);
            Save(OptionName.SettingIds.AutoDelete, AutoDelete.ToString(), settingsProvider);
            Save(OptionName.SettingIds.IgnoreFilesNotUnderP4Root2, IgnoreFilesNotUnderP4Root.ToString(), settingsProvider);
            Save(OptionName.SettingIds.Version180OrAfter, "true", settingsProvider);
        }

        private static void Save(OptionName.SettingIds name, string variableValue, ProvideSavedSettings settingsProvider)
        {
            var variableName = OptionName.OptionNameForSave(name);
            if (settingsProvider.GetVariableExists(variableName))
            {
                settingsProvider[name] = variableValue;
            }
            else
            {
                settingsProvider[name] = variableValue;
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
