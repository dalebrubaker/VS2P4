using System;

using EnvDTE;
using BruSoft.VS2P4.Properties;
using System.Collections.Generic;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// This class holds the options set by SccProviderOptionsControl and allows loading them and persisting them between sessions.
    /// </summary>
    public class P4Options
    {
        public P4Options(ProvideSavedSettings settingsProvider, IServiceProvider serviceProvider)
        {
            _settingsManager = new ShellSettingsManager(serviceProvider);
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
        const string defaultCollectionPath = "VS2P4";
#if DEBUG
        const Log.Level defaultLogLevel = Log.Level.Debug;
#else
        const Log.Level defaultLogLevel = Log.Level.Information;
#endif
        private const bool defaultCommandsEnabled = true;
        private SettingsManager _settingsManager;

        /// <summary>
        /// Load all options and return the loaded instance of this class.
        /// </summary>
        public static P4Options Load(ProvideSavedSettings settingsProvider, IServiceProvider serviceProvider)
        {
            var p4Options = new P4Options(settingsProvider, serviceProvider);
            p4Options.LoadPersisted(settingsProvider);
            return p4Options;
        }

        /// <summary>
        /// Load persisted options except the connection options, which are loaded in the ctor
        /// </summary>
        /// <param name="settingsProvider"></param>
        private void LoadPersisted(ProvideSavedSettings settingsProvider)
        {
            var store = _settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            UseP4Config = LoadBoolean(OptionName.SettingIds.UseP4Config, store, defaultCommandsEnabled);
            Server = LoadString(OptionName.SettingIds.Server, store, string.Empty);
            User = LoadString(OptionName.SettingIds.User, store, string.Empty);
            Password = LoadString(OptionName.SettingIds.Password, store, defaultPassword);
            Workspace = LoadString(OptionName.SettingIds.Workspace, store, string.Empty);
            LogLevel = (Log.Level)Enum.Parse(typeof(Log.Level), LoadString(OptionName.SettingIds.LogLevel, store, defaultLogLevel.ToString()));

            IsCheckoutEnabled = LoadBoolean(OptionName.SettingIds.IsCheckoutEnabled, store, defaultCommandsEnabled);
            IsAddEnabled = LoadBoolean(OptionName.SettingIds.IsAddEnabled, store, defaultCommandsEnabled);
            IsRevertIfUnchangedEnabled = LoadBoolean(OptionName.SettingIds.IsRevertIfUnchangedEnabled, store, defaultCommandsEnabled);
            IsRevertEnabled = LoadBoolean(OptionName.SettingIds.IsRevertEnabled, store, defaultCommandsEnabled);
            PromptBeforeRevert = LoadBoolean(OptionName.SettingIds.PromptBeforeRevert, store, defaultCommandsEnabled);
            IsGetLatestRevisionEnabled = LoadBoolean(OptionName.SettingIds.IsGetLatestRevisionEnabled, store, defaultCommandsEnabled);
            IsViewRevisionHistoryEnabled = LoadBoolean(OptionName.SettingIds.IsViewRevisionHistoryEnabled, store, defaultCommandsEnabled);
            IsViewDiffEnabled = LoadBoolean(OptionName.SettingIds.IsViewDiffEnabled, store, defaultCommandsEnabled);
            IsViewTimeLapseEnabled = LoadBoolean(OptionName.SettingIds.IsViewTimeLapseEnabled, store, defaultCommandsEnabled);
            AutoCheckoutOnEdit = LoadBoolean(OptionName.SettingIds.AutoCheckoutOnEdit, store, defaultCommandsEnabled);
            AutoCheckoutOnSave = LoadBoolean(OptionName.SettingIds.AutoCheckoutOnSave, store, defaultCommandsEnabled);
            AutoAdd = LoadBoolean(OptionName.SettingIds.AutoAdd, store, defaultCommandsEnabled);
            AutoDelete = LoadBoolean(OptionName.SettingIds.AutoDelete, store, defaultCommandsEnabled);
            IgnoreFilesNotUnderP4Root = LoadBoolean(OptionName.SettingIds.IgnoreFilesNotUnderP4Root2, store, false);
            OptionName.IsFirstLoadDone = true;

            if (!settingsProvider.GetVariableExists(OptionName.OptionNameForSave(OptionName.SettingIds.Version180OrAfter)))
            {
                // This is the first time we've loaded starting with Release 1.80. So copy the old settings to the new ones.
                Save(settingsProvider);
            }
        }

        private static string LoadString(OptionName.SettingIds name, SettingsStore settingsStore, string defaultValue)
        {
            return settingsStore.GetString(defaultCollectionPath, name.ToString(), defaultValue);
        }

        private static bool LoadBoolean(OptionName.SettingIds name, SettingsStore settingsStore, bool defaultValue)
        {
            return settingsStore.GetBoolean(defaultCollectionPath, name.ToString(), defaultValue);
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

            var store = _settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            SaveBoolean(OptionName.SettingIds.UseP4Config, UseP4Config, store);
            SaveString(OptionName.SettingIds.Server, Server, store);
            SaveString(OptionName.SettingIds.User, User, store);
            SaveString(OptionName.SettingIds.Password, Password, store);
            SaveString(OptionName.SettingIds.Workspace, Workspace, store);
            SaveString(OptionName.SettingIds.LogLevel, LogLevel.ToString(), store);

            SaveBoolean(OptionName.SettingIds.IsCheckoutEnabled, IsCheckoutEnabled, store);
            SaveBoolean(OptionName.SettingIds.IsAddEnabled, IsAddEnabled, store);
            SaveBoolean(OptionName.SettingIds.IsRevertIfUnchangedEnabled, IsRevertIfUnchangedEnabled, store);
            SaveBoolean(OptionName.SettingIds.IsRevertEnabled, IsRevertEnabled, store);
            SaveBoolean(OptionName.SettingIds.PromptBeforeRevert, PromptBeforeRevert, store);
            SaveBoolean(OptionName.SettingIds.IsGetLatestRevisionEnabled, IsGetLatestRevisionEnabled, store);
            SaveBoolean(OptionName.SettingIds.IsViewRevisionHistoryEnabled, IsViewRevisionHistoryEnabled, store);
            SaveBoolean(OptionName.SettingIds.IsViewDiffEnabled, IsViewDiffEnabled, store);
            SaveBoolean(OptionName.SettingIds.IsViewTimeLapseEnabled, IsViewTimeLapseEnabled, store);
            SaveBoolean(OptionName.SettingIds.AutoCheckoutOnEdit, AutoCheckoutOnEdit, store);
            SaveBoolean(OptionName.SettingIds.AutoCheckoutOnSave, AutoCheckoutOnSave, store);
            SaveBoolean(OptionName.SettingIds.AutoAdd, AutoAdd, store);
            SaveBoolean(OptionName.SettingIds.AutoDelete, AutoDelete, store);
            SaveBoolean(OptionName.SettingIds.IgnoreFilesNotUnderP4Root2, IgnoreFilesNotUnderP4Root, store);
            SaveBoolean(OptionName.SettingIds.Version180OrAfter, true, store);
        }

        private static void SaveString(OptionName.SettingIds name, string variableValue, WritableSettingsStore settingsStore)
        {
            if (!settingsStore.CollectionExists(defaultCollectionPath))
            {
                settingsStore.CreateCollection(defaultCollectionPath);
            }
            settingsStore.SetString(defaultCollectionPath, name.ToString(), variableValue);
        }

        private static void SaveBoolean(OptionName.SettingIds name, bool variableValue, WritableSettingsStore settingsStore)
        {
            if (!settingsStore.CollectionExists(defaultCollectionPath))
            {
                settingsStore.CreateCollection(defaultCollectionPath);
            }
            settingsStore.SetBoolean(defaultCollectionPath, name.ToString(), variableValue);
        }

        public override string ToString()
        {
            var txt = String.Format("{0} {1} {2}", Server, User, Workspace);
            return txt;
        }
    }
}
