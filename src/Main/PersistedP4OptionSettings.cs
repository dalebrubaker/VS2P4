using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// PersistedP4OptionSettings implements the ProvideSavedSettings interface to provide
    /// saved P4Options settings using an EnvDTE.Globals object and a specified P4OptionsDefaultsProvider
    /// object.  However, the Globals object is optional; if it is not provided, then only defaults are
    /// returned and saved values are discarded.  For settings that do not have defaults provided by
    /// P4OptionsDefaultsProvider, null is returned for values, and the exists and persists checks all
    /// return false.
    /// </summary>
    class PersistedP4OptionSettings : ProvideSavedSettings
    {
        private readonly EnvDTE.Globals _globals;
        private readonly P4OptionsDefaultsProvider _defaults;

        public PersistedP4OptionSettings(EnvDTE.Globals globals, P4OptionsDefaultsProvider defaults)
        {
            if (defaults == null)
            {
                throw new ArgumentException("A defaults provider must be specified.");
            }
            this._globals = globals;
            this._defaults = defaults;
        }

        public PersistedP4OptionSettings(P4OptionsDefaultsProvider defaults)
        {
            if (defaults == null)
            {
                throw new ArgumentException("A defaults provider must be specified.");
            }
            this._globals = null;
            this._defaults = defaults;
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
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(OptionName.SettingIds.UseP4Config);
                    if (_globals.VariableExists[variableName])
                    {
                        var sval = (string)_globals[variableName];
                        bool val;

                        if (bool.TryParse(sval, out val))
                        {
                            return val;
                        }
                    }
                }

                // No persisted value, so get the default.
                return _defaults.UseP4Config;
            }
            set
            {
                if (_globals != null)
                {
                    Save(OptionName.SettingIds.UseP4Config, (value == null ? false : value.Value).ToString());
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
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(OptionName.SettingIds.UseP4Config);
                    return _globals.VariablePersists[variableName];
                }
                return false;
            }
            set
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForSave(OptionName.SettingIds.UseP4Config);
                    _globals.VariablePersists[variableName] = value;
                }
            }
        }

        public string PerforceServer
        {
            get
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(OptionName.SettingIds.Server);
                    if (_globals.VariableExists[variableName])
                    {
                        return (string)_globals[variableName];
                    }
                }

                // No persistence, so get the default value;
                return _defaults.PerforceServer;
            }
            set
            {
                if (_globals != null)
                {
                    Save(OptionName.SettingIds.Server, value);
                }
            }
        }

        public bool PerforceServerExists
        {
            get
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(OptionName.SettingIds.Server);
                    if (_globals.VariableExists[variableName])
                    {
                        return true;
                    }
                }

                // No persisted value, so see if a default exists.  A null value here
                // indicates no default either.
                return _defaults.PerforceServer != null;
            }
        }

        public bool PerforceServerPersists
        {
            get
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(OptionName.SettingIds.Server);
                    return _globals.VariablePersists[variableName];
                }
                return false;
            }
            set
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForSave(OptionName.SettingIds.Server);
                    _globals.VariablePersists[variableName] = value;
                }
            }
        }

        public string PerforceUser
        {
            get
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(OptionName.SettingIds.User);
                    if (_globals.VariableExists[variableName])
                    {
                        return (string)_globals[variableName];
                    }
                }

                // No persistence, so get the default value;
                return _defaults.PerforceUser;
            }
            set
            {
                if (_globals != null)
                {
                    Save(OptionName.SettingIds.User, value);
                }
            }
        }

        public bool PerforceUserExists
        {
            get
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(OptionName.SettingIds.User);
                    if (_globals.VariableExists[variableName])
                    {
                        return true;
                    }
                }

                // No persisted value, so see if a default exists.  A null value here
                // indicates no default either.
                return _defaults.PerforceUser != null;
            }
        }

        public bool PerforceUserPersists
        {
            get
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(OptionName.SettingIds.User);
                    return _globals.VariablePersists[variableName];
                }
                return false;
            }
            set
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForSave(OptionName.SettingIds.User);
                    _globals.VariablePersists[variableName] = value;
                }
            }
        }

        public string PerforceWorkspace
        {
            get
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(OptionName.SettingIds.Workspace);
                    if (_globals.VariableExists[variableName])
                    {
                        return (string)_globals[variableName];
                    }
                }

                // No persistence, so get the default value;
                return _defaults.PerforceWorkspace;
            }
            set
            {
                if (_globals != null)
                {
                    Save(OptionName.SettingIds.Workspace, value);
                }
            }
        }

        public bool PerforceWorkspaceExists
        {
            get
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(OptionName.SettingIds.Workspace);
                    if (_globals.VariableExists[variableName])
                    {
                        return true;
                    }
                }

                // No persisted value, so see if a default exists.  A null value here
                // indicates no default either.
                return _defaults.PerforceWorkspace != null;
            }
        }

        public bool PerforceWorkspacePersists
        {
            get
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(OptionName.SettingIds.Workspace);
                    return _globals.VariablePersists[variableName];
                }
                return false;
            }
            set
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForSave(OptionName.SettingIds.Workspace);
                    _globals.VariablePersists[variableName] = value;
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
                return _defaults.WorkspacePath;
            }
        }

        public object this[OptionName.SettingIds name]
        {
            get
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForLoad(name);
                    return _globals[variableName];
                }
                return null;
            }
            set
            {
                if (_globals != null)
                {
                    var variableName = OptionName.OptionNameForSave(name);
                    _globals[variableName] = value;
                }
            }
        }

        public bool GetVariableExists(string variableName)
        {
            if (_globals != null)
            {
                return _globals.VariableExists[variableName];
            }
            return false;
        }

        public bool GetVariablePersists(string variableName)
        {
            if (_globals != null)
            {
                return _globals.VariablePersists[variableName];
            }
            return false;
        }

        public void SetVariablePersists(string variableName, bool pVal)
        {
            if (_globals != null)
            {
                _globals.VariablePersists[variableName] = pVal;
            }
        }

        private void Save(OptionName.SettingIds name, string variableValue)
        {
            if (_globals == null)
            {
                Log.Information(string.Format("globals was null trying to set {0} to {1}", name, variableValue));
                return;
            }

            var variableName = OptionName.OptionNameForSave(name);
            Log.Information(string.Format("globals setting {0} set to {1}", variableName, variableValue));
            if (_globals.VariableExists[variableName])
            {
                _globals[variableName.ToString()] = variableValue;
            }
            else
            {
                _globals[variableName] = variableValue;
                Log.Information(string.Format("globals setting {0} set to persist", variableName));
                _globals.VariablePersists[variableName] = true;
            }
        }

        #endregion ProvideSavedSettings

    }
}
