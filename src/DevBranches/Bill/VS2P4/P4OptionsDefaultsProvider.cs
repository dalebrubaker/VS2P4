using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// P4OptionsDefaultsProvider is the central mechanism for getting default settings of the Perforce connection options.  It
    /// uses UserSettings as the underlying mechanism for retrieving the values of the settings.  It implements a priority
    /// scheme for looking up the settings.
    /// 
    /// If UserSettings.UserSettingsXML has not been set, or has been set to null, then the defaults will be based on the user's
    /// settings file, if one exists.
    /// </summary>
    public class P4OptionsDefaultsProvider
    {
        private List<string> nameList;
        /// <summary>
        /// To construct a P4OptionsProvider, you must supply a list of settings group names that define the priorities for
        /// looking up individual settings.  The lookup for a setting proceeds by looking up a group by the name specified
        /// first in the list.  If that group defines the requested setting, return it.  If not, proceed to the next group in
        /// the list, and so on until a setting is found or the list is exhausted.  If a specified group does not exist, that
        /// group is skipped in the search.
        /// </summary>
        /// <param name="nameList">Prioritized list of settings group names</param>
        public P4OptionsDefaultsProvider(List<string> nameList)
        {
            this.nameList = nameList;
        }

        /// <summary>
        /// Return true or false if an explicit default was specified indicating whether to use the P4CONFIG environment
        /// variable.  Return false if no default was provided.
        /// </summary>
        public bool UseP4Config
        {
            get
            {
                try
                {
                    foreach (string groupName in nameList)
                    {
                        UserSettings us = UserSettings.GetSettings(groupName);
                        if (us != null)
                        {
                            if (us.UseP4Config != null)
                            {
                                return us.UseP4Config.Value;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                return false;
            }
        }

        /// <summary>
        /// Return the Perforce server and port to be used for connections to Perforce.
        /// </summary>
        public string PerforceServer
        {
            get
            {
                try
                {
                    foreach (string groupName in nameList)
                    {
                        UserSettings us = UserSettings.GetSettings(groupName);
                        if (us != null)
                        {
                            if (us.PerforceServer != null)
                            {
                                return us.PerforceServer;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return null;
                }
                return null;
            }
        }

        /// <summary>
        /// Return the username for connections to Perforce.
        /// </summary>
        public string PerforceUser
        {
            get
            {
                try
                {
                    foreach (string groupName in nameList)
                    {
                        UserSettings us = UserSettings.GetSettings(groupName);
                        if (us != null)
                        {
                            if (us.PerforceUser != null)
                            {
                                return us.PerforceUser;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return null;
                }
                return null;
            }
        }

        /// <summary>
        /// Return the name of the workspace to use in Perforce connections.
        /// </summary>
        public string PerforceWorkspace
        {
            get
            {
                try
                {
                    foreach (string groupName in nameList)
                    {
                        UserSettings us = UserSettings.GetSettings(groupName);
                        if (us != null)
                        {
                            if (us.PerforceWorkspace != null)
                            {
                                return us.PerforceWorkspace;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return null;
                }
                return null;
            }
        }

        /// <summary>
        /// Return the path to the workspace specified in the Perforce connection.  Primarily this
        /// only needs to be specified if you specify using P4CONFIG, and then only if somehow the
        /// the solution path does not match the Perforce client root path.
        /// 
        /// This attribute may also be used in unit tests of VS2P4 itself.
        /// </summary>
        public string WorkspacePath
        {
            get
            {
                try
                {
                    foreach (string groupName in nameList)
                    {
                        UserSettings us = UserSettings.GetSettings(groupName);
                        if (us != null)
                        {
                            if (us.WorkspacePath != null)
                            {
                                return us.WorkspacePath;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return null;
                }
                return null;
            }
        }
    }
}
