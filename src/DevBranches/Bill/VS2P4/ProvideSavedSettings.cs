using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// ProvideSavedSettings is a specialization of the EnvDTE.Globals interface for options that
    /// can be persisted in P4Options.  Using this interface, instead of Globals, provides flexibility
    /// for unit testing so that we can test without an EnvDTE object.
    /// </summary>
    public interface ProvideSavedSettings
    {
        /// <summary>
        /// Sets or returns the value of the UseP4Config setting, indicating whether or not to use
        /// the P4CONFIG environment variable.  If the setting does not exist in the settings,
        /// null is returned.  Setting the value to null is the same as setting it to false.
        /// </summary>
        Nullable<bool> UseP4Config { get; set; }

        /// <summary>
        /// Returns true if a setting for UseP4Config exists.
        /// </summary>
        bool UseP4ConfigExists { get; }

        /// <summary>
        /// Sets or returns whether the value of UseP4Config is persisted or not.
        /// </summary>
        bool UseP4ConfigPersists { get; set; }

        /// <summary>
        /// Sets or returns the value of the PerforceServer setting.  If the setting does not exist in
        /// the settings, null is returned.
        /// </summary>
        string PerforceServer { get; set; }

        /// <summary>
        /// Returns true if a setting for PerforceServer exists.
        /// </summary>
        bool PerforceServerExists { get; }

        /// <summary>
        /// Sets or returns whether the value of PerforceServer is persisted or not.
        /// </summary>
        bool PerforceServerPersists { get; set; }

        /// <summary>
        /// Sets or returns the value of the PerforceUser setting.  If the setting does not exist in
        /// the settings, null is returned.
        /// </summary>
        string PerforceUser { get; set; }

        /// <summary>
        /// Returns true if a setting for PerforceUser exists.
        /// </summary>
        bool PerforceUserExists { get; }

        /// <summary>
        /// Sets or returns whether the value of PerforceUser is persisted or not.
        /// </summary>
        bool PerforceUserPersists { get; set; }

        /// <summary>
        /// Sets or returns the value of the PerforceWorkspace setting.  If the setting does not exist in
        /// the settings, null is returned.
        /// </summary>
        string PerforceWorkspace { get; set; }

        /// <summary>
        /// Returns true if a setting for PerforceWorkspace exists.
        /// </summary>
        bool PerforceWorkspaceExists { get; }

        /// <summary>
        /// Sets or returns whether the value of PerforceWorkspace is persisted or not.
        /// </summary>
        bool PerforceWorkspacePersists { get; set; }

        /// <summary>
        /// Returns the value of the WorkspacePath setting.  If the setting does not exist in
        /// the settings, null is returned.
        /// </summary>
        string WorkspacePath { get; }

        /// <summary>
        /// Gets or sets the value of the specified variable.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        object this[string name] { get; set; }

        /// <summary>
        /// Returns true if a setting exists for the specified variable.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool GetVariableExists(string name);

        /// <summary>
        /// Returns true if the specified variable is persisted by the environment.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool GetVariablePersists(string name);

        /// <summary>
        /// Set whether the specified variable should be persisted or not.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="pVal"></param>
        void SetVariablePersists(string Name, bool pVal);
    }
}
