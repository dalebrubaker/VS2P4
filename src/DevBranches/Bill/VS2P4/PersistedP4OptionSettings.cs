using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// PersistedP4OptionSettings implements to ProvideSavedSettings interface to provide
    /// saved P4Options settings using an EnvDTE.Globals object and a specified P4OptionsDefaultsProvider
    /// object.  However, the Globals object is optional; if it is not provided, then only defaults are
    /// returned and saved values are discarded.  For settings that do not have defaults provided by
    /// P4OptionsDefaultsProvider, null is returned for values, and the exists and persists checks all
    /// return false.
    /// </summary>
    class PersistedP4OptionSettings : ProvideSavedSettings
    {
        public PersistedP4OptionSettings(EnvDTE.Globals globals, P4OptionsDefaultsProvider defaults)
        {
            if (defaults == null)
            {
                throw new ArgumentException("A defaults provider must be specified.");
            }
            this.globals = globals;
            this.defaults = defaults;
        }

        public PersistedP4OptionSettings(P4OptionsDefaultsProvider defaults)
        {
            if (defaults == null)
            {
                throw new ArgumentException("A defaults provider must be specified.");
            }
            this.globals = null;
            this.defaults = defaults;
        }

        public override string ToString()
        {
            return String.Format("UseP4Config = {0}, Server = {1}, User={2}, Workspace={3}",
                                 UseP4Config, PerforceServer, PerforceUser, PerforceWorkspace);
        }

        #region ProvideSavedSettings implementation

        public bool? UseP4Config
        {
            get
            {
                if (globals != null)
                {
                    if (globals.VariableExists["UseP4Config"])
                    {
                        string sval = (string)globals["UseP4Config"];
                        bool val;

                        if (bool.TryParse(sval, out val))
                        {
                            return val;
                        }
                    }
                }

                // No persisted value, so get the default.
                return defaults.UseP4Config;
            }
            set
            {
                if (globals != null)
                {
                    globals["UseP4Config"] = (value == null ? false : value.Value).ToString();
                }
            }
        }

        public bool UseP4ConfigExists
        {
            get
            {
                // A default value always exists, so this is always true.
                return true;
            }
        }

        public bool UseP4ConfigPersists
        {
            get
            {
                if (globals != null)
                {
                    return globals.VariablePersists["UseP4Config"];
                }
                return false;
            }
            set
            {
                if (globals != null)
                {
                    globals.VariablePersists["UseP4Config"] = value;
                }
            }
        }

        public string PerforceServer
        {
            get
            {
                if (globals != null)
                {
                    if (globals.VariableExists["PerforceServer"])
                    {
                        return (string) globals["PerforceServer"];
                    }
                }

                // No persistence, so get the default value;
                return defaults.PerforceServer;
            }
            set
            {
                if (globals != null)
                {
                    globals["PerforceServer"] = value;
                }
            }
        }

        public bool PerforceServerExists
        {
            get
            {
                if (globals != null)
                {
                    if (globals.VariableExists["PerforceServer"])
                    {
                        return true;
                    }
                }

                // No persisted value, so see if a default exists.  A null value here
                // indicates no default either.
                return defaults.PerforceServer != null;
            }
        }

        public bool PerforceServerPersists
        {
            get
            {
                if (globals != null)
                {
                    return globals.VariablePersists["PerforceServer"];
                }
                return false;
            }
            set
            {
                if (globals != null)
                {
                    globals.VariablePersists["PerforceServer"] = value;
                }
            }
        }

        public string PerforceUser
        {
            get
            {
                if (globals != null)
                {
                    if (globals.VariableExists["PerforceUser"])
                    {
                        return (string)globals["PerforceUser"];
                    }
                }

                // No persistence, so get the default value;
                return defaults.PerforceUser;
            }
            set
            {
                if (globals != null)
                {
                    globals["PerforceUser"] = value;
                }
            }
        }

        public bool PerforceUserExists
        {
            get
            {
                if (globals != null)
                {
                    if (globals.VariableExists["PerforceUser"])
                    {
                        return true;
                    }
                }

                // No persisted value, so see if a default exists.  A null value here
                // indicates no default either.
                return defaults.PerforceUser != null;
            }
        }

        public bool PerforceUserPersists
        {
            get
            {
                if (globals != null)
                {
                    return globals.VariablePersists["PerforceUser"];
                }
                return false;
            }
            set
            {
                if (globals != null)
                {
                    globals.VariablePersists["PerforceUser"] = value;
                }
            }
        }

        public string PerforceWorkspace
        {
            get
            {
                if (globals != null)
                {
                    if (globals.VariableExists["PerforceWorkspace"])
                    {
                        return (string)globals["PerforceWorkspace"];
                    }
                }

                // No persistence, so get the default value;
                return defaults.PerforceWorkspace;
            }
            set
            {
                if (globals != null)
                {
                    globals["PerforceWorkspace"] = value;
                }
            }
        }

        public bool PerforceWorkspaceExists
        {
            get
            {
                if (globals != null)
                {
                    if (globals.VariableExists["PerforceWorkspace"])
                    {
                        return true;
                    }
                }

                // No persisted value, so see if a default exists.  A null value here
                // indicates no default either.
                return defaults.PerforceWorkspace != null;
            }
        }

        public bool PerforceWorkspacePersists
        {
            get
            {
                if (globals != null)
                {
                    return globals.VariablePersists["PerforceWorkspace"];
                }
                return false;
            }
            set
            {
                if (globals != null)
                {
                    globals.VariablePersists["PerforceWorkspace"] = value;
                }
            }
        }

        /// <summary>
        /// WorkspacePath is not stored in the environment Globals, so this just accesses the default,
        /// if any.
        /// </summary>
        public string WorkspacePath
        {
            get
            {
                return defaults.WorkspacePath;
            }
        }

        public object this[string name]
        {
            get
            {
                if (globals != null)
                {
                    return globals[name];
                }
                return null;
            }
            set
            {
                if (globals != null)
                {
                    globals[name] = value;
                }
            }
        }

        public bool GetVariableExists(string name)
        {
            if (globals != null)
            {
                return globals.VariableExists[name];
            }
            return false;
        }

        public bool GetVariablePersists(string name)
        {
            if (globals != null)
            {
                return globals.VariablePersists[name];
            }
            return false;
        }

        public void SetVariablePersists(string Name, bool pVal)
        {
            if (globals != null)
            {
                globals.VariablePersists[Name] = pVal;
            }
        }

        #endregion ProvideSavedSettings

        private EnvDTE.Globals globals;
        private P4OptionsDefaultsProvider defaults;
    }
}
