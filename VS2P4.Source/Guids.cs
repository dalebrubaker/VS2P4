// Guids.cs
// MUST match guids.h
using System;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// This class is used only to expose the list of Guids used by this package.
    /// This list of guids must match the set of Guids used inside the VSCT file.
    /// </summary>
    public static class GuidList
    {
        public const string guidVS2P4PkgString = "8358dd60-20b0-478a-83b8-ea8ae3ecdaa2";
        public const string guidVS2P4CmdSetString = "e64c92e7-8a32-4afd-af57-df9933b4e66d";

        public static readonly Guid guidVS2P4CmdSet = new Guid(guidVS2P4CmdSetString);


        // From SccProvider
        //// Unique ID of the source control provider; this is also used as the command UI context to show/hide the pacakge UI
        public static readonly Guid guidSccProvider = new Guid("{8358dd60-00b0-478a-83b8-ea8ae3ecdaa2}"); 
        // The guid of the source control provider service (implementing IVsSccProvider interface)
        public static readonly Guid guidSccProviderService = new Guid("{8358dd60-10b0-478a-83b8-ea8ae3ecdaa2}"); 
        // The guid of the source control provider package (implementing IVsPackage interface)
        public static readonly Guid guidSccProviderPkg = new Guid(guidVS2P4PkgString);
        //// Other guids for menus and commands
        //public static readonly Guid guidSccProviderCmdSet = guidVS2P4CmdSet;
    };
}