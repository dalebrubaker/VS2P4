using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// This class became necessary because the old option settings names conflicted with names used by other extensions.
    /// Using this class allows us to preserve existing settings from older releases, if any.
    /// </summary>
    public static class OptionName
    {
        const string PREFIX = "VS2P4_";

        /// <summary>
        /// The DTE2 object used to persist option settings between sessions.
        /// Injected by SccProviderService
        /// </summary>
        public static EnvDTE80.DTE2 Dte2 { get; set; }

        /// <summary>
        /// Set to true by P4Options when the first load is done.
        /// After this is true, we always prepend VS2P4_ to the name. 
        /// </summary>
        public static bool IsFirstLoadDone { get; set; }

        public enum SettingIds
        {
            UseP4Config,
            Server,
            User,
            Password,
            Workspace,
            LogLevel,
            IsCheckoutEnabled,
            IsAddEnabled,
            IsRevertIfUnchangedEnabled,
            IsRevertEnabled,
            PromptBeforeRevert,
            IsGetLatestRevisionEnabled,
            IsViewRevisionHistoryEnabled,
            IsViewDiffEnabled,
            IsViewTimeLapseEnabled,
            AutoCheckoutOnEdit,
            AutoCheckoutOnSave,
            AutoAdd,
            AutoDelete,
            IgnoreFilesNotUnderP4Root2,
            Version180OrAfter,
            IsOpenInSwarmEnabled,
        }

        private static string Prefix
        {
            get
            {
                if (Dte2 == null)
                {
                    Log.Information("Tried to set _prefix with null Dte2");

                    return String.Empty;
                }

                if (IsFirstLoadDone)
                {
                    return PREFIX;
                }

                if (Dte2.Globals.VariableExists["VS2P4_Version180OrAfter"])
                {
                    // The new VS2P4 settings (version 1.80 and after) are in place
                    return PREFIX;
                }

                // Assume the old VS2P4 settings (version 1.79 and below) are in place
                return String.Empty;
            }
        }

        /// <summary>
        /// We return an empty string if there is no global called VS2P4_Version180OrAfter 
        ///     and we have not yet done our first load of options.
        /// </summary>
        /// <param name="settingsId"></param>
        /// <returns></returns>
        public static string OptionNameForLoad(SettingIds settingsId)
        {
            switch (settingsId)
            {
                case SettingIds.UseP4Config:
                case SettingIds.Server:
                case SettingIds.User:
                case SettingIds.Password:
                case SettingIds.Workspace:
                case SettingIds.LogLevel:
                case SettingIds.IsCheckoutEnabled:
                case SettingIds.IsAddEnabled:
                case SettingIds.IsRevertIfUnchangedEnabled:
                case SettingIds.IsRevertEnabled:
                case SettingIds.PromptBeforeRevert:
                case SettingIds.IsGetLatestRevisionEnabled:
                case SettingIds.IsViewRevisionHistoryEnabled:
                case SettingIds.IsViewDiffEnabled:
                case SettingIds.IsViewTimeLapseEnabled:
                case SettingIds.AutoCheckoutOnEdit:
                case SettingIds.AutoCheckoutOnSave:
                case SettingIds.AutoAdd:
                case SettingIds.AutoDelete:
                case SettingIds.IgnoreFilesNotUnderP4Root2:
                case SettingIds.Version180OrAfter:
                    return Prefix + settingsId;
                default:
                    throw new ArgumentOutOfRangeException("settingsId");
            }
        }

        /// <summary>
        /// We always save the new name (prefixed by VS2P4_)
        /// </summary>
        /// <param name="settingsId"></param>
        /// <returns></returns>
        public static string OptionNameForSave(SettingIds settingsId)
        {
            switch (settingsId)
            {
                case SettingIds.UseP4Config:
                case SettingIds.Server:
                case SettingIds.User:
                case SettingIds.Password:
                case SettingIds.Workspace:
                case SettingIds.LogLevel:
                case SettingIds.IsCheckoutEnabled:
                case SettingIds.IsAddEnabled:
                case SettingIds.IsRevertIfUnchangedEnabled:
                case SettingIds.IsRevertEnabled:
                case SettingIds.PromptBeforeRevert:
                case SettingIds.IsGetLatestRevisionEnabled:
                case SettingIds.IsViewRevisionHistoryEnabled:
                case SettingIds.IsViewDiffEnabled:
                case SettingIds.IsViewTimeLapseEnabled:
                case SettingIds.AutoCheckoutOnEdit:
                case SettingIds.AutoCheckoutOnSave:
                case SettingIds.AutoAdd:
                case SettingIds.AutoDelete:
                case SettingIds.IgnoreFilesNotUnderP4Root2:
                case SettingIds.Version180OrAfter:
                    return PREFIX + settingsId;
                default:
                    throw new ArgumentOutOfRangeException("settingsId");
            }
        }

    }
}
