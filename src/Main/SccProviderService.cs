// Undefine this to turn off use of P4Cache.  This is mostly for testing in cases where
// the cache is suspected of causing a problem.
#define USE_CACHE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace BruSoft.VS2P4
{
    using Process = System.Diagnostics.Process;

    /// <summary>
    /// This file contains the source control service implementation.
    /// The class implements the the IVsSccProvider interface that enables source control provider activation and switching.
    /// </summary>
    [Guid("8358dd60-10b0-478a-83b8-ea8ae3ecdaa2")]
    public class SccProviderService : 
        IVsSccProvider,             // Required for provider registration with source control manager
        IVsSccManager2,             // Base source control functionality interface
        IVsSccManagerTooltip,       // Provide tooltips for source control items
        IVsSolutionEvents,          // We'll register for solution events, these are useful for source control
        IVsSccGlyphs,               // For custom glyphs
        IVsSolutionEvents2,
        IVsQueryEditQuerySave2,     // Required to allow editing of controlled files 
        IVsTrackProjectDocumentsEvents2,  // Useful to track project changes (add, renames, deletes, etc)
        IDisposable
    {
        // Whether the provider is active or not. Set by SetActive() when the provider has been initialized.
        // Also set by IsActive property for unit testing.
        private bool _isActive;

        /// <summary>
        /// Set true after refreshing the first project in a solution, so we can refresh collapsed projects as well on the first one.
        /// </summary>
        private bool _isAllProjectsRefreshed;

        // The service and source control provider
        private readonly VS2P4Package _sccProvider;

        // The cookie for solution events 
        private uint _vsSolutionEventsCookie;

        // The cookie for project document events
        private uint _tpdTrackProjectDocumentsCookie;

        // The list of files approved for in-memory edit
        private readonly Hashtable _approvedForInMemoryEdit = new Hashtable();

        /// <summary>
        /// The Perforce service used to interface to P4.Net.
        /// </summary>
        private P4Service _p4Service;

        /// <summary>
        /// The Perforce file state cache
        /// </summary>
        private P4Cache _p4Cache;

        /// <summary>
        /// The options used by P4Service.
        /// </summary>
        internal P4Options Options { get; set; }

        /// <summary>
        /// The DTE2 object used to persist option settings between sessions.
        /// </summary>
        private EnvDTE80.DTE2 dte2 { get; set; }

        /// <summary>
        /// The path to the currently-opened solution
        /// </summary>
        private string _solutionPath;

        /// <summary>
        /// The map of vsFileName to p4FileName, shared by P4Service and P4Cache
        /// </summary>
        private Map _map;

        /// <summary>
        /// The map of vsFileName to p4FileName, shared by P4Service and P4Cache
        /// </summary>
        internal Map Map
        {
            get { return _map; }
        }

        /// <summary>
        /// true when a solution is open.
        /// </summary>
        internal bool IsSolutionLoaded;

        /// <summary>
        /// true when we have finished our first P4CacheUpdate after a solution is opened
        /// </summary>
        public bool IsFirstP4CacheUpdateComplete;

        /// <summary>
        /// The selection and files we are removing.
        /// Set by OnQueryRemoveFiles
        /// May be deleted or not from Perforce in OnAfterRemovedFiles, depending on whether the user 
        ///     actually did delete the file (Remove, Cut) or didn't (Exclude From Project)
        /// </summary>
        private VsSelection _vsSelectionFilesDeleted;

        private PersistedP4OptionSettings _persistedSettings;

        public VS2P4Package SccProvider;

        public const int CL_NEW = -1;
        public const int CL_DEFAULT = 0;

        #region SccProvider Service initialization/unitialization

        public SccProviderService(VS2P4Package sccProvider)
        {
            Debug.Assert(null != sccProvider);
            _sccProvider = sccProvider;
            SccProvider = _sccProvider;

            // Subscribe to solution events
            var sol = (IVsSolution)_sccProvider.GetService(typeof(SVsSolution));
            sol.AdviseSolutionEvents(this, out _vsSolutionEventsCookie);
            Debug.Assert(VSConstants.VSCOOKIE_NIL != _vsSolutionEventsCookie);

            // Subscribe to project documents
            var tpdService = (IVsTrackProjectDocuments2)_sccProvider.GetService(typeof(SVsTrackProjectDocuments));
            tpdService.AdviseTrackProjectDocumentsEvents(this, out _tpdTrackProjectDocumentsCookie);
            Debug.Assert(VSConstants.VSCOOKIE_NIL != _tpdTrackProjectDocumentsCookie);
        }

        

        public void Dispose()
        {
            // Unregister from receiving solution events
            if (VSConstants.VSCOOKIE_NIL != _vsSolutionEventsCookie)
            {
                var sol = (IVsSolution)_sccProvider.GetService(typeof(SVsSolution));
                sol.UnadviseSolutionEvents(_vsSolutionEventsCookie);
                _vsSolutionEventsCookie = VSConstants.VSCOOKIE_NIL;
            }

            // Unregister from receiving project documents
            if (VSConstants.VSCOOKIE_NIL != _tpdTrackProjectDocumentsCookie)
            {
                var tpdService = (IVsTrackProjectDocuments2)_sccProvider.GetService(typeof(SVsTrackProjectDocuments));
                tpdService.UnadviseTrackProjectDocumentsEvents(_tpdTrackProjectDocumentsCookie);
                _tpdTrackProjectDocumentsCookie = VSConstants.VSCOOKIE_NIL;
            }

            // Also dispose of P4Service
            if (_p4Service != null)
            {
                _p4Service.Dispose();
            }

            if (_customSccGlyphsImageList != null)
            {
                _customSccGlyphsImageList.Dispose();
            }
        }

        internal P4Service P4Service
        {
            get
            {
                return _p4Service;
            }
            set
            {
                _p4Service = value;

                // If we have ever started up the P4Cache, then we need to restart it here with the current
                // connection settings.
                if (IsSolutionLoaded && _p4Cache != null)
                {
                    _p4Cache = null;
                    StartP4ServiceAndInitializeCache();
                }
            }
        }
        #endregion

        //--------------------------------------------------------------------------------
        // IVsSccProvider specific functions
        //--------------------------------------------------------------------------------
        #region IVsSccProvider interface functions

        // Called by the scc manager when the provider is activated. 
        // Make visible and enable if necessary scc related menu commands
        public int SetActive()
        {
            Trace.WriteLine(String.Format(CultureInfo.CurrentUICulture, "Provider set active"));
            Log.Information("VS2P4 set active");

            // Load options persisted between sessions
            dte2 = (EnvDTE80.DTE2)_sccProvider.GetService(typeof(SDTE));
            OptionName.Dte2 = dte2;
            PersistedP4OptionSettings persistedSettings;
            LoadOptions(out persistedSettings);
            string solutionName = _sccProvider.GetSolutionFileName();
            Log.OptionsLevel = Options.LogLevel; if (solutionName != null)
            {
                // The solution was loaded before we were made active.
                IsSolutionLoaded = true;
            }

            if (IsSolutionLoaded)
            {
                // We are being activated after the solution was already opened.
                StartP4ServiceAndInitializeCache();
            }

            _isActive = true;
            _sccProvider.OnActiveStateChange();

            return VSConstants.S_OK;
        }


        // Called by the scc manager when the provider is deactivated. 
        // Hides and disable scc related menu commands
        public int SetInactive()
        {
            Trace.WriteLine(String.Format(CultureInfo.CurrentUICulture, "Provider set inactive"));
            Log.Information("VS2P4 set inactive");

            _isActive = false;
            _sccProvider.OnActiveStateChange();

            return VSConstants.S_OK;
        }


        public int AnyItemsUnderSourceControl(out int pfResult)
        {
            // Although the parameter is an int, it's in reality a BOOL value, so let's return 0/1 values
            if (!_isActive)
            {
                pfResult = 0;
            }
            else
            {
                pfResult = 1; // (_controlledProjects.Count != 0) ? 1 : 0;
            }
    
            return VSConstants.S_OK;
        }

        #endregion

        //--------------------------------------------------------------------------------
        // IVsSccManager2 specific functions
        //--------------------------------------------------------------------------------
        #region IVsSccManager2 interface functions

        public int BrowseForProject(out string pbstrDirectory, out int pfOK)
        {
            // Obsolete method
            pbstrDirectory = null;
            pfOK = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int CancelAfterBrowseForProject() 
        {
            // Obsolete method
            return VSConstants.E_NOTIMPL;
        }

        /// <summary>
        /// Returns whether the source control provider is fully installed
        /// </summary>
        public int IsInstalled(out int pbInstalled)
        {
            // All source control packages should always return S_OK and set pbInstalled to nonzero
            pbInstalled = 1;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Provide source control icons for the specified files and returns scc status of files
        /// Thanks to HgSccPackage (Sergey Antonov) for this improvement, to allow multiple nodes to be refreshed in one call.
        ///     A huge performance benefit for very-large solutions (1000's of files)
        /// </summary>
        /// <returns>The method returns S_OK if at least one of the files is controlled, S_FALSE if none of them are</returns>
        public int GetSccGlyph([InAttribute] int cFiles, [InAttribute] string[] rgpszFullPaths, [OutAttribute] VsStateIcon[] rgsiGlyphs, [OutAttribute] uint[] rgdwSccStatus)
        {
            //Debug.Assert(cFiles == 1, "Only getting one file icon at a time is supported");

			//Iterate through all the files
            for (int iFile = 0; iFile < cFiles; iFile++)
            {
                // Return the icons and the status. While the status is a combination a flags, we'll return just values 
                // with one bit set, to make life easier for GetSccGlyphsFromStatus
                FileState status = GetFileState(rgpszFullPaths[iFile]);
                switch (status)
                {
                    case FileState.CheckedInHeadRevision:
                    case FileState.OpenForEditOtherUser:
                    case FileState.LockedByOtherUser:
                        rgsiGlyphs[iFile] = (VsStateIcon)(_customSccGlyphBaseIndex + (uint)CustomSccGlyphs.CheckedIn);
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_CONTROLLED;
                        }
                        break;
                    case FileState.OpenForEdit:
                        rgsiGlyphs[iFile] = VsStateIcon.STATEICON_CHECKEDOUT;
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_CHECKEDOUT;
                        }
                        break;
                    case FileState.NotSet:
                        rgsiGlyphs[iFile] = VsStateIcon.STATEICON_EXCLUDEDFROMSCC;
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_CONTROLLED;
                        }
                        break;
                    case FileState.NotInPerforce:
                        rgsiGlyphs[iFile] = VsStateIcon.STATEICON_BLANK;
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_NOTCONTROLLED;
                        }
                        break;
                    case FileState.OpenForEditDiffers:
                        rgsiGlyphs[iFile] = (VsStateIcon)(_customSccGlyphBaseIndex + (uint)CustomSccGlyphs.Differs);
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_CHECKEDOUT;
                        }
                        break;
                    case FileState.CheckedInPreviousRevision:
                        rgsiGlyphs[iFile] = (VsStateIcon)(_customSccGlyphBaseIndex + (uint)CustomSccGlyphs.Differs);
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_CONTROLLED;
                        }
                        break;
                    case FileState.OpenForDelete:
                    case FileState.OpenForDeleteOtherUser:
                    case FileState.DeletedAtHeadRevision:
                    case FileState.OpenForRenameSource:
                        rgsiGlyphs[iFile] = VsStateIcon.STATEICON_DISABLED;
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_CONTROLLED;
                        }
                        break;
                    case FileState.OpenForAdd:
                    case FileState.OpenForRenameTarget:
                        rgsiGlyphs[iFile] = (VsStateIcon)(_customSccGlyphBaseIndex + (uint)CustomSccGlyphs.Add);
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_CHECKEDOUT;
                        }
                        break;
                    case FileState.NeedsResolved:
                    case FileState.OpenForIntegrate:
                        rgsiGlyphs[iFile] = (VsStateIcon)(_customSccGlyphBaseIndex + (uint)CustomSccGlyphs.Resolve);
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_CHECKEDOUT;
                        }
                        break;
                    case FileState.Locked:
                        rgsiGlyphs[iFile] = VsStateIcon.STATEICON_READONLY;
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_CHECKEDOUT;
                        }
                        break;
                    case FileState.OpenForBranch:
                        rgsiGlyphs[iFile] = VsStateIcon.STATEICON_ORPHANED;
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_CHECKEDOUT;
                        }
                        break;
                    default:
                        // This is an uncontrolled file, return a blank scc glyph for it
                        rgsiGlyphs[iFile] = VsStateIcon.STATEICON_BLANK;
                        if (rgdwSccStatus != null)
                        {
                            rgdwSccStatus[iFile] = (uint)__SccStatus.SCC_STATUS_NOTCONTROLLED;
                        }
                        break;
                }
            }

            return VSConstants.S_OK;
        }

       

        /// <summary>
        /// Determines the corresponding scc status glyph to display, given a combination of scc status flags.
        /// DAB: AFAIK this method is NEVER called.
        /// </summary>
        public int GetSccGlyphFromStatus([InAttribute] uint dwSccStatus, [OutAttribute] VsStateIcon[] psiGlyph)
        {
            switch (dwSccStatus)
            {
                case (uint) __SccStatus.SCC_STATUS_CHECKEDOUT:
                    psiGlyph[0] = VsStateIcon.STATEICON_CHECKEDOUT;
                    break;
                case (uint) __SccStatus.SCC_STATUS_CONTROLLED:
                    psiGlyph[0] = (VsStateIcon)(_customSccGlyphBaseIndex + (uint)CustomSccGlyphs.CheckedIn); // VsStateIcon.STATEICON_CHECKEDIN;
                    break;
                default:
                    // Uncontrolled
                    psiGlyph[0] = VsStateIcon.STATEICON_BLANK;
                    break;
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// One of the most important methods in a source control provider, is called by projects that are under source control when they are first opened to register project settings
        /// </summary>
        public int RegisterSccProject([InAttribute] IVsSccProject2 pscp2Project, [InAttribute] string pszSccProjectName, [InAttribute] string pszSccAuxPath, [InAttribute] string pszSccLocalPath, [InAttribute] string pszProvider)
        {
            Log.Information("VS2P4 RegisterSccProject()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called by projects registered with the source control portion of the environment before they are closed. 
        /// </summary>
        public int UnregisterSccProject([InAttribute] IVsSccProject2 pscp2Project)
        {
            Log.Information("VS2P4 UnregisterSccProject()");
            return VSConstants.S_OK;
        }

        #endregion

        //--------------------------------------------------------------------------------
        // IVsSccManagerTooltip specific functions
        //--------------------------------------------------------------------------------
        #region IVsSccManagerTooltip interface functions

        /// <summary>
        /// Called by solution explorer to provide tooltips for items. Returns a text describing the source control status of the item.
        /// </summary>
        public int GetGlyphTipText([InAttribute] IVsHierarchy phierHierarchy, [InAttribute] uint itemidNode, out string pbstrTooltipText)
        {
            // Initialize output parameters
            pbstrTooltipText = "";

            IList<string> files = _sccProvider.GetNodeFiles(phierHierarchy, itemidNode);
            if (files.Count == 0)
            {
                return VSConstants.S_OK;
            }

            // Return the glyph text based on the first file of node (the master file)
            FileState status = GetFileState(files[0]);
            switch (status)
            {
                case FileState.NotSet: // Don't know yet if this is controlled.
                case FileState.NotInPerforce: // Uncontrolled files don't have tooltips.
                    pbstrTooltipText = "";
                    break;
                case FileState.OpenForEdit:
                    pbstrTooltipText = Resources.State_OpenForEdit;
                    break;
                case FileState.OpenForEditOtherUser:
                    pbstrTooltipText = Resources.State_OpenForEditOtherUser;
                    break;
                case FileState.OpenForEditDiffers:
                    pbstrTooltipText = Resources.State_OpenForEditDiffers;
                    break;
                case FileState.Locked:
                    pbstrTooltipText = Resources.State_Locked;
                    break;
                case FileState.LockedByOtherUser:
                    pbstrTooltipText = Resources.State_LockedByOtherUser;
                    break;
                case FileState.OpenForDelete:
                    pbstrTooltipText = Resources.State_OpenForDelete;
                    break;
                case FileState.OpenForDeleteOtherUser:
                    pbstrTooltipText = Resources.State_OpenForDeleteOtherUser;
                    break;
                case FileState.DeletedAtHeadRevision:
                    pbstrTooltipText = Resources.State_DeletedAtHeadRevision;
                    break;
                case FileState.OpenForAdd:
                    pbstrTooltipText = Resources.State_OpenForAdd;
                    break;
                case FileState.OpenForRenameSource:
                    pbstrTooltipText = Resources.State_OpenForRenameSource;
                    break;
                case FileState.OpenForRenameTarget:
                    pbstrTooltipText = Resources.State_OpenForRenameTarget;
                    break;
                case FileState.CheckedInHeadRevision:
                    pbstrTooltipText = Resources.State_CheckedInHeadRevision;
                    break;
                case FileState.CheckedInPreviousRevision:
                    pbstrTooltipText = Resources.State_CheckedInPreviousRevision;
                    break;
                case FileState.NeedsResolved:
                    pbstrTooltipText = Resources.State_NeedsResolved;
                    break;
                case FileState.OpenForBranch:
                    pbstrTooltipText = Resources.State_OpenForBranch;
                    break;
                case FileState.OpenForIntegrate:
                    pbstrTooltipText = Resources.State_OpenForIntegrate;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return VSConstants.S_OK;
        }

        #endregion

        //--------------------------------------------------------------------------------
        // IVsSolutionEvents and IVsSolutionEvents2 specific functions
        //--------------------------------------------------------------------------------
        #region IVsSolutionEvents interface functions

        public int OnAfterCloseSolution([InAttribute] Object pUnkReserved)
        {
            Log.Information("VS2P4 OnAfterCloseSolution()");
            _p4Service = null;
            if (_p4Cache != null)
            {
                _p4Cache.P4CacheUpdated -= P4CacheUpdated;
                _p4Cache = null;
            }
            
            IsSolutionLoaded = false;
            _approvedForInMemoryEdit.Clear();
            _isAllProjectsRefreshed = false;
            IsSolutionLoaded = false;
            IsFirstP4CacheUpdateComplete = false;

            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject([InAttribute] IVsHierarchy pStubHierarchy, [InAttribute] IVsHierarchy pRealHierarchy)
        {
            var projName = _sccProvider.GetProjectFileName(pRealHierarchy);
            var msg = string.Format("OnAfterLoadProject({0})", projName);
            Log.Information(msg);
            // If a project is reloaded in the solution after the solution was opened, do Refresh to pick up file states for the added project
            if (_isActive && IsSolutionLoaded)
            {
                Log.Information("After loading project, refreshing all project glyphs");
                Refresh(pRealHierarchy);
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Note changes between VS2010 and VS2012, at these links:
        ///     http://stackoverflow.com/questions/15012275/onafteropenproject-called-with-fadded-1-when-opening-solution-in-vs2012
        ///     http://social.msdn.microsoft.com/Forums/vstudio/en-US/2d38f312-e566-4f65-bf2a-92041c51d7cc/how-do-i-differentiate-between-a-newly-added-project-and-reloaded-project-in-onafteropenproject?forum=vsx
        ///     
        /// also http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.shell.interop.ivssolutionevents.onafteropenproject(v=vs.100).aspx
        /// 
        /// In a nutshell, fAdded can no longer be trusted. So with Release 1.87 we ignore it and always refresh the project's glyphs.
        /// This may cause some redundant refreshing.
        /// 
        /// And because this is now called asynchronously, we now refresh all solutions in the project on the "first one" 
        ///     in order to pick up files in collapsed projects. I'm really not sure if this will
        /// 
        /// </summary>
        /// <param name="pHierarchy"></param>
        /// <param name="fAdded">1 (true) means in VS2010 that the project is added to the solution after the solution is opened</param>
        /// <returns></returns>
        public int OnAfterOpenProject([InAttribute] IVsHierarchy pHierarchy, [InAttribute] int fAdded)
        {
            var projName = _sccProvider.GetProjectFileName(pHierarchy);
            var msg = string.Format("OnAfterOpenProject({0}) with _isActive={1} and IsSolutionLoaded={2}, fAdded={3}", projName, _isActive, IsSolutionLoaded, fAdded);
            Log.Information(msg);
            // If a project is added to the solution after the solution was opened, do Refresh to pick up file states for the added project
            if (_isActive && IsSolutionLoaded)
            {
                if (_isAllProjectsRefreshed)
                {
                    Log.Information("After opening project, refreshing all project glyphs");
                    Refresh(pHierarchy);
                }
                else
                {
                    Log.Information("Opening first project so refreshing all solution glyphs");
                    Refresh(null);
                    _isAllProjectsRefreshed = true;
                }
            }
            else
            {
                if (!_isActive)
                {
                    Log.Information("A project has been opened, but the VS2P4 provider is not active, so we can't refresh its glyphs");
                }
                if (!IsSolutionLoaded)
                {
                    Log.Information("A project has been opened, but the solution hasn't been opened, so we can't refresh its glyphs");
                }
            }

            return VSConstants.S_OK;
        }

        public int OnBeforeOpenSolution(string pszSolutionFilename)
        {
            var msg = string.Format("OnBeforeOpenSolution({0})", pszSolutionFilename);
            Log.Information(msg);
            _isAllProjectsRefreshed = false;
            IsSolutionLoaded = false;

            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution([InAttribute] Object pUnkReserved, [InAttribute] int fNewSolution)
        {
            var solutionName = _sccProvider.GetSolutionFileName();
            var msg = string.Format("OnAfterOpenSolution({0})", solutionName);
            Log.Information(msg);
            _isAllProjectsRefreshed = false;
            IsSolutionLoaded = false;
            IsFirstP4CacheUpdateComplete = false;
            if (_isActive)
            {
                msg = string.Format("Solution {0} has been opened. Reinitializing and refreshing all solution glyphs.", solutionName);
                Log.Information(msg);
                PersistedP4OptionSettings persistedSettings;
                LoadOptions(out persistedSettings);
                StartP4ServiceAndInitializeCache();
            }
            else
            {
                msg = string.Format("Solution {0} has been opened but the VS2P4 provider is not active, so we can't initialize or refresh any glyphs.", solutionName);
                Log.Information(msg);
            }

            IsSolutionLoaded = true;

            return VSConstants.S_OK;
        }

        private void StartP4ServiceAndInitializeCache()
        {
            // Before we set up the connection to Perforce, set the CWD so we pick up any changes to P4Config
            // We assume that the solution is in the right workspace
            Trace.WriteLine("StartP4ServiceAndInitializeCache called.");
            Log.Information("VS2P4 StartP4ServiceAndInitializeCache()");

            _map = new Map(Options.IgnoreFilesNotUnderP4Root);
            string solutionName = _sccProvider.GetSolutionFileName();
            if (solutionName != null)
            {
                _solutionPath = Path.GetDirectoryName(solutionName);
            }
            else
            {
                _solutionPath = _persistedSettings.WorkspacePath;
            }
            _p4Service = new P4Service(Options.Server, Options.User, Options.Password, Options.Workspace, Options.UseP4Config, _solutionPath, _map);

#if USE_CACHE
            _p4Cache = new P4Cache(Options.Server, Options.User, Options.Password, Options.Workspace, Options.UseP4Config, _solutionPath, _map);
            _p4Cache.P4CacheUpdated += P4CacheUpdated;

            VsSelection vsSelection = _sccProvider.GetSolutionSelection();
            _p4Cache.Initialize(vsSelection);
#endif
        }

        private void P4CacheUpdated(object sender, P4CacheEventArgs e)
        {
            Log.Information("VS2P4 P4CacheUpdated()");
            IsFirstP4CacheUpdateComplete = true;
            IList<string> fileNames = e.VsSelection.FileNames;
            IList<VSITEMSELECTION> nodes = e.VsSelection.Nodes;
            Log.Debug(String.Format("SccProviderService.P4CacheUpdated Starting: Updating all glyphs, for {0} files and {1} nodes", fileNames.Count, nodes.Count));

            var nodesGlyphsRefresher = new NodesGlyphsRefresher(nodes, _sccProvider);
            nodesGlyphsRefresher.Refresh();
            Log.Debug("Finished P4CacheUpdated");
        }

        public int OnBeforeCloseProject([InAttribute] IVsHierarchy pHierarchy, [InAttribute] int fRemoved)
        {
            Log.Information("VS2P4 OnBeforeCloseProject()");
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution([InAttribute] Object pUnkReserved)
        {
            Log.Information("VS2P4 OnBeforeCloseSolution()");
            // Since we registered the solution with source control from OnAfterOpenSolution, it would be nice to unregister it, too, when it gets closed.
            // Also, unregister the solution folders
            Hashtable enumSolFolders = _sccProvider.GetSolutionFoldersEnum();
            foreach (IVsHierarchy pHier in enumSolFolders.Keys)
            {
                IVsSccProject2 pSccProject = pHier as IVsSccProject2;
                if (pSccProject != null)
                {
                    UnregisterSccProject(pSccProject);
                }
            }

            UnregisterSccProject(null);

            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject([InAttribute] IVsHierarchy pRealHierarchy, [InAttribute] IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject([InAttribute] IVsHierarchy pHierarchy, [InAttribute] int fRemoving, [InAttribute] ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution([InAttribute] Object pUnkReserved, [InAttribute] ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject([InAttribute] IVsHierarchy pRealHierarchy, [InAttribute] ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterMergeSolution ([InAttribute] Object pUnkReserved )
        {
            // reset the flag now that solutions were merged and the merged solution completed opening
            //_loadingControlledSolutionLocation = "";

            return VSConstants.S_OK;
        }

        #endregion

        //--------------------------------------------------------------------------------
        // IVsQueryEditQuerySave2 specific functions
        //--------------------------------------------------------------------------------
        #region IVsQueryEditQuerySave2 interface functions

        public int BeginQuerySaveBatch ()
        {
            return VSConstants.S_OK;
        }

        public int EndQuerySaveBatch ()
        {
            return VSConstants.S_OK;
        }

        public int DeclareReloadableFile([InAttribute] string pszMkDocument, [InAttribute] uint rgf, [InAttribute] VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            return VSConstants.S_OK;
        }

        public int DeclareUnreloadableFile([InAttribute] string pszMkDocument, [InAttribute] uint rgf, [InAttribute] VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            return VSConstants.S_OK;
        }

        public int IsReloadable ([InAttribute] string pszMkDocument, out int pbResult )
        {
            // Since we're not tracking which files are reloadable and which not, consider everything reloadable
            pbResult = 1;
            return VSConstants.S_OK;
        }

        public int OnAfterSaveUnreloadableFile([InAttribute] string pszMkDocument, [InAttribute] uint rgf, [InAttribute] VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called by projects and editors before modifying a file
        /// The function allows the source control systems to take the necessary actions (checkout, flip attributes)
        /// to make the file writable in order to allow the edit to continue
        ///
        /// There are a lot of cases to deal with during QueryEdit/QuerySave. 
        /// - called in commmand line mode, when UI cannot be displayed
        /// - called during builds, when save shoudn't probably be allowed
        /// - called during projects migration, when projects are not open and not registered yet with source control
        /// - checking out files may bring new versions from vss database which may be reloaded and the user may lose in-memory changes; some other files may not be reloadable
        /// - not all editors call QueryEdit when they modify the file the first time (buggy editors!), and the files may be already dirty in memory when QueryEdit is called
        /// - files on disk may be modified outside IDE and may have attributes incorrect for their scc status
        /// - checkouts may fail
        /// The sample provider won't deal with all these situations, but a real source control provider should!
        /// </summary>
        public int QueryEditFiles([InAttribute] uint rgfQueryEdit, [InAttribute] int cFiles, [InAttribute] string[] rgpszMkDocuments, [InAttribute] uint[] rgrgf, [InAttribute] VSQEQS_FILE_ATTRIBUTE_DATA[] rgFileInfo, out uint pfEditVerdict, out uint prgfMoreInfo)
        {
            // Initialize output variables
            pfEditVerdict = (uint)tagVSQueryEditResult.QER_EditOK;
            prgfMoreInfo = 0;

            // In non-UI mode just allow the edit, because the user cannot be asked what to do with the file
            if (_sccProvider.InCommandLineMode())
            {
                return VSConstants.S_OK;
            }

            VsSelection vsSelectionToCheckOut = GetVsSelectionNoFileNamesAllNodes();
            int changelistNumber = CL_DEFAULT;

            try 
            {
                //Iterate through all the files
                for (int iFile = 0; iFile < cFiles; iFile++)
                {

                    uint fEditVerdict = (uint)tagVSQueryEditResult.QER_EditNotOK;
                    uint fMoreInfo = 0;
                    string fileName = rgpszMkDocuments[iFile];

                    // Because of the way we calculate the status, it is not possible to have a 
                    // checked in file that is writeable on disk, or a checked out file that is read-only on disk
                    // A source control provider would need to deal with those situations, too
                    FileState state = GetFileState(fileName);
                    bool fileExists = File.Exists(fileName);
                    bool isFileReadOnly = false;
                    if (fileExists)
                    {
                        isFileReadOnly = ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
                    }

                    // Allow the edits if the file does not exist or is writable
                    if (!fileExists || !isFileReadOnly)
                    {
                        fEditVerdict = (uint)tagVSQueryEditResult.QER_EditOK;
                    }
                    else
                    {
                        // If the IDE asks about a file that was already approved for in-memory edit, allow the edit without asking the user again
                        if (_approvedForInMemoryEdit.ContainsKey(fileName.ToLower()))
                        {
                            fEditVerdict = (uint)tagVSQueryEditResult.QER_EditOK;
                            fMoreInfo = (uint)(tagVSQueryEditResultFlags.QER_InMemoryEdit);
                        }
                        else
                        {
                            switch (state)
                            {
                                case FileState.CheckedInHeadRevision:
                                case FileState.CheckedInPreviousRevision:
                                case FileState.OpenForIntegrate:
                                case FileState.OpenForBranch:
                                case FileState.OpenForEdit:
                                case FileState.OpenForEditDiffers:
                                    fMoreInfo = QueryEditFileCheckedIn(vsSelectionToCheckOut, fileName, rgfQueryEdit, ref fEditVerdict, ref changelistNumber);
                                    break;
                                case FileState.OpenForAdd:
                                case FileState.OpenForRenameTarget:
                                case FileState.NotInPerforce:
                                    fMoreInfo = QueryEditFileNotCheckedIn(fileName, rgfQueryEdit, ref fEditVerdict);
                                    break;
                                case FileState.NotSet:
                                    // We must ignore files that are .NotSet -- no way to determine what to do.
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(state.ToString());
                            }
                        }
                    }

                    // It's a bit unfortunate that we have to return only one set of flags for all the files involved in the operation
                    // The edit can continue if all the files were approved for edit
                    prgfMoreInfo |= fMoreInfo;
                    pfEditVerdict |= fEditVerdict;

                    if (pfEditVerdict == (uint)tagVSQueryEditResult.QER_EditOK && vsSelectionToCheckOut.FileNames.Count > 0)
                    {
                        CheckoutFiles(vsSelectionToCheckOut, changelistNumber);
                    }
                }
            }
            catch(Exception ex)
            {
                // If an exception was caught, do not allow the edit
                pfEditVerdict = (uint)tagVSQueryEditResult.QER_EditNotOK;
                prgfMoreInfo = (uint)tagVSQueryEditResultFlags.QER_EditNotPossible;
                Log.Error("SccProviderService: QueryEditFiles: " + ex.Message + "\n" + ex.StackTrace);
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// QueryEdit and QuerySave and OnAfterRenameFiles (and maybe others) may change the state of a file but we don't know what nodes to update.
        /// So we start here with an empty list of fileNames but with all nodes in the solution.
        /// TODO: Figure out a way to discover the node(s) that use a particular fileName, and just update those nodes.
        /// </summary>
        /// <returns></returns>
        private VsSelection GetVsSelectionNoFileNamesAllNodes()
        {
            VsSelection vsSelection = new VsSelection(new List<string>(), _sccProvider.GetSolutionNodes());
            return vsSelection;
        }

        /// <summary>
        /// This method is called only for readonly files that are checked in.
        /// If the user agrees (or AutoEdit), add fileName to vsSelectionToCheckOut
        /// </summary>
        /// <param name="vsSelectionToCheckOut">a list of fileNames to check out</param>
        /// <param name="fileName">The fileName we're checking.</param>
        /// <param name="rgfQueryEdit">The kind of query.</param>
        /// <param name="fEditVerdict">The flag whether or not edit will be allowed.</param>
        /// <returns>More Info flag.</returns>
        private uint QueryEditFileCheckedIn(VsSelection vsSelectionToCheckOut, string fileName, uint rgfQueryEdit, ref uint fEditVerdict, ref int changelistNumber)
        {
            uint fMoreInfo;
            if ((rgfQueryEdit & (uint)tagVSQueryEditFlags.QEF_ReportOnly) != 0)
            {
                // The file is checked in and ReportOnly means we can't ask the user anything. The answer is "No."
                fMoreInfo = (uint)(tagVSQueryEditResultFlags.QER_EditNotPossible | tagVSQueryEditResultFlags.QER_ReadOnlyUnderScc);
            }
            else
            {
                if (Options.AutoCheckoutOnEdit)
                {
                    // Add this file to the list to be checked out.
                    vsSelectionToCheckOut.FileNames.Add(fileName);
                    fEditVerdict = (uint)tagVSQueryEditResult.QER_EditOK;
                    fMoreInfo = (uint)tagVSQueryEditResultFlags.QER_MaybeCheckedout;
                }
                else
                {
                    Dictionary<int, string> changelists = null;
                    try
                    {
                        if (!_p4Service.IsConnected)
                        {
                            _p4Service.Connect();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        changelists = _p4Service.GetPendingChangelists();
                        _p4Service.Disconnect();
                    }

                    var dlgAskCheckout = new DlgQueryEditCheckedInFile(fileName, changelists);
                    if ((rgfQueryEdit & (uint)tagVSQueryEditFlags.QEF_SilentMode) != 0)
                    {
                        // When called in silent mode, attempt the checkout
                        // (The alternative is to deny the edit and return QER_NoisyPromptRequired and expect for a non-silent call)
                        dlgAskCheckout.Answer = DlgQueryEditCheckedInFile.qecifCheckout;
                    }
                    else
                    {
                        dlgAskCheckout.ShowDialog();
                    }

                    if (dlgAskCheckout.Answer == DlgQueryEditCheckedInFile.qecifCheckout)
                    {
                        // Add this file to the list to be checked out.
                        vsSelectionToCheckOut.FileNames.Add(fileName);
                        fEditVerdict = (uint)tagVSQueryEditResult.QER_EditOK;
                        fMoreInfo = (uint)tagVSQueryEditResultFlags.QER_MaybeCheckedout;
                        changelistNumber = dlgAskCheckout.SelectedChangelist;
                        // Do not forget to set QER_Changed if the content of the file on disk changes during the query edit
                        // Do not forget to set QER_Reloaded if the source control reloads the file from disk after such changing checkout.
                    }
                    else if (dlgAskCheckout.Answer == DlgQueryEditCheckedInFile.qecifEditInMemory)
                    {
                        // Allow edit in memory
                        fEditVerdict = (uint)tagVSQueryEditResult.QER_EditOK;
                        fMoreInfo = (uint)(tagVSQueryEditResultFlags.QER_InMemoryEdit);
                        // Add the file to the list of files approved for edit, so if the IDE asks again about this file, we'll allow the edit without asking the user again
                        // UNDONE: Currently, a file gets removed from _approvedForInMemoryEdit list only when the solution is closed. Consider intercepting the 
                        // IVsRunningDocTableEvents.OnAfterSave/OnAfterSaveAll interface and removing the file from the approved list after it gets saved once.
                        _approvedForInMemoryEdit[fileName.ToLower()] = true;
                    }
                    else
                    {
                        fEditVerdict = (uint)tagVSQueryEditResult.QER_NoEdit_UserCanceled;
                        fMoreInfo = (uint)(tagVSQueryEditResultFlags.QER_ReadOnlyUnderScc | tagVSQueryEditResultFlags.QER_CheckoutCanceledOrFailed);
                    }
                    dlgAskCheckout.Dispose();
                }
            }
            return fMoreInfo;
        }

        /// <summary>
        /// This method is called only for readonly files that are not controlled or are already checked out.
        /// If the user agrees, make it writeable.
        /// </summary>
        /// <param name="fileName">The fileName we're checking.</param>
        /// <param name="rgfQueryEdit">The kind of query.</param>
        /// <param name="fEditVerdict">The flag whether or not edit will be allowed.</param>
        /// <returns>More Info flag.</returns>
        private uint QueryEditFileNotCheckedIn(string fileName, uint rgfQueryEdit, ref uint fEditVerdict)
        {
            uint fMoreInfo = 0;
            if ((rgfQueryEdit & (uint)tagVSQueryEditFlags.QEF_ReportOnly) != 0)
            {
                fMoreInfo = (uint)(tagVSQueryEditResultFlags.QER_EditNotPossible | tagVSQueryEditResultFlags.QER_ReadOnlyNotUnderScc);
            }
            else
            {
                bool allowMakeFileWritable = false;
                if ((rgfQueryEdit & (uint)tagVSQueryEditFlags.QEF_SilentMode) != 0)
                {
                    // When called in silent mode, deny the edit and return QER_NoisyPromptRequired and expect for a non-silent call)
                    // (The alternative is to silently make the file writable and accept the edit)
                    fMoreInfo = (uint)(tagVSQueryEditResultFlags.QER_EditNotPossible | 
                        tagVSQueryEditResultFlags.QER_ReadOnlyNotUnderScc |
                        tagVSQueryEditResultFlags.QER_NoisyPromptRequired);
                }
                else
                {
                    // This is a read-only file, warn the user
                    string messageText = Resources.QEQS_EditUncontrolledReadOnly;
                    allowMakeFileWritable = PromptForAllowMakeFileWritable(messageText, fileName);
                }

                if (allowMakeFileWritable)
                {
                    // Make the file writable and allow the edit
                    File.SetAttributes(fileName, FileAttributes.Normal);
                    fEditVerdict = (uint)tagVSQueryEditResult.QER_EditOK;
                }
            }
            return fMoreInfo;
        }

        /// <summary>
        /// Ask user if it's okay to make this file writable. 
        /// </summary>
        /// <param name="messageText">The prompt text.</param>
        /// <param name="fileName">The filename.</param>
        /// <returns>true if user says "Okay"</returns>
        private bool PromptForAllowMakeFileWritable(string messageText, string fileName)
        {
            bool allowMakeFileWritable = false;
            var uiShell = (IVsUIShell)_sccProvider.GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            string messageCaption = Resources.ProviderName;
            if (uiShell.ShowMessageBox(
                    0,
                    ref clsid,
                    messageCaption,
                    String.Format(CultureInfo.CurrentUICulture, messageText, fileName),
                    string.Empty,
                    0,
                    OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND,
                    OLEMSGICON.OLEMSGICON_QUERY,
                    0,
                        // false = application modal; true would make it system modal
                    out result) == VSConstants.S_OK && result == (int)DialogResult.Yes)
            {
                allowMakeFileWritable = true;
            }
            return allowMakeFileWritable;
        }

        /// <summary>
        /// Called by editors and projects before saving the files
        /// The function allows the source control systems to take the necessary actions (checkout, flip attributes)
        /// to make the file writable in order to allow the file saving to continue
        /// </summary>
        public int QuerySaveFile([InAttribute] string pszMkDocument, [InAttribute] uint rgf, [InAttribute] VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo, out uint pdwQSResult)
        {
            // Delegate to the other QuerySave function
            string[] rgszDocuements = new string[1];
            uint[] rgrgf = new uint[1];
            rgszDocuements[0] = pszMkDocument;
            rgrgf[0] = rgf;
            return QuerySaveFiles(rgf, 1, rgszDocuements, rgrgf, pFileInfo, out pdwQSResult);
        }

        /// <summary>
        /// Called by editors and projects before saving the files
        /// The function allows the source control systems to take the necessary actions (checkout, flip attributes)
        /// to make the file writable in order to allow the file saving to continue
        /// </summary>
        public int QuerySaveFiles([InAttribute] uint rgfQuerySave, [InAttribute] int cFiles, [InAttribute] string[] rgpszMkDocuments, [InAttribute] uint[] rgrgf, [InAttribute] VSQEQS_FILE_ATTRIBUTE_DATA[] rgFileInfo, out uint pdwQSResult)
        {
            // Initialize output variables
            // It's a bit unfortunate that we have to return only one set of flags for all the files involved in the operation
            // The last file will win setting this flag
            pdwQSResult = (uint)tagVSQuerySaveResult.QSR_SaveOK;

            // In non-UI mode attempt to silently flip the attributes of files or check them out 
            // and allow the save, because the user cannot be asked what to do with the file
            // Don't bother us!!!!!!!
            //if (_sccProvider.InCommandLineMode())
            {
                rgfQuerySave = rgfQuerySave | (uint)tagVSQuerySaveFlags.QSF_SilentMode;
            }

            VsSelection vsSelection = GetVsSelectionNoFileNamesAllNodes();
            try 
            {
                for (int iFile = 0; iFile < cFiles; iFile++)
                {
                    string fileName = rgpszMkDocuments[iFile];
                    FileState state = GetFileState(fileName);
                    bool fileExists = File.Exists(fileName);
                    bool isFileReadOnly = false;
                    if (fileExists)
                    {
                        isFileReadOnly = ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
                    }

                    switch (state)
                    {
                        case FileState.CheckedInHeadRevision:
                        case FileState.CheckedInPreviousRevision:
                            if (Options.AutoCheckoutOnSave)
                            {
                                vsSelection.FileNames.Add(fileName);
                                pdwQSResult = (uint)tagVSQuerySaveResult.QSR_SaveOK;
                                break;
                            }

                            var dlgAskCheckout = new DlgQuerySaveCheckedInFile(fileName);
                            if ((rgfQuerySave & (uint)tagVSQuerySaveFlags.QSF_SilentMode) != 0)
                            {
                                // When called in silent mode, attempt the checkout
                                // (The alternative is to deny the save, return QSR_NoSave_NoisyPromptRequired and expect for a non-silent call)
                                dlgAskCheckout.Answer = DlgQuerySaveCheckedInFile.qscifCheckout;
                            }
                            else
                            {
                                dlgAskCheckout.ShowDialog();
                            }

                            switch (dlgAskCheckout.Answer)
                            {
                                case DlgQueryEditCheckedInFile.qecifCheckout:
                                    vsSelection.FileNames.Add(fileName);
                                    pdwQSResult = (uint)tagVSQuerySaveResult.QSR_SaveOK;
                                    break;
                                case DlgQuerySaveCheckedInFile.qscifForceSaveAs:
                                    pdwQSResult = (uint)tagVSQuerySaveResult.QSR_ForceSaveAs;
                                    break;
                                case DlgQuerySaveCheckedInFile.qscifSkipSave:
                                    pdwQSResult = (uint)tagVSQuerySaveResult.QSR_NoSave_Continue;
                                    break;
                                default:
                                    pdwQSResult = (uint)tagVSQuerySaveResult.QSR_NoSave_Cancel;
                                    break;
                            }
                            dlgAskCheckout.Dispose();
                            break;
                        case FileState.OpenForEdit:
                        case FileState.OpenForAdd:
                        case FileState.OpenForEditDiffers:
                        case FileState.OpenForRenameTarget:
                        case FileState.NotSet:
                        case FileState.NotInPerforce:
                            if (fileExists && isFileReadOnly)
                            {
                                // This is a read-only file, warn the user
                                string messageText = Resources.QEQS_SaveReadOnly;
                                bool allowMakeFileWritable = PromptForAllowMakeFileWritable(messageText, fileName);
                                if (!allowMakeFileWritable)
                                {
                                    pdwQSResult = (uint)tagVSQuerySaveResult.QSR_NoSave_Continue;
                                    break;
                                }

                                // Make the file writable and allow the save
                                File.SetAttributes(fileName, FileAttributes.Normal);
                            }

                            // Allow the save now 
                            pdwQSResult = (uint)tagVSQuerySaveResult.QSR_SaveOK;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                // If an exception was caught, do not allow the save
                pdwQSResult = (uint)tagVSQuerySaveResult.QSR_NoSave_Cancel;
                Log.Error(String.Format("SccProviderService.QuerySaveFiles() Exception: {0}", ex.Message));
            }

            if (vsSelection.FileNames.Count > 0)
            {
                CheckoutFiles(vsSelection);
            }
     
            return VSConstants.S_OK;
        }

        #endregion

        //--------------------------------------------------------------------------------
        // IVsTrackProjectDocumentsEvents2 specific functions
        //--------------------------------------------------------------------------------

        public int OnQueryAddFiles([InAttribute] IVsProject pProject, [InAttribute] int cFiles, [InAttribute] string[] rgpszMkDocuments, [InAttribute] VSQUERYADDFILEFLAGS[] rgFlags, [OutAttribute] VSQUERYADDFILERESULTS[] pSummaryResult, [OutAttribute] VSQUERYADDFILERESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        /// <summary>
        /// Implement this function to update the project scc glyphs when the items are added to the project.
        /// If a project doesn't call GetSccGlyphs as they should do (as solution folder do), this will update the glyphs correctly when the project is controlled
        /// </summary>
        public int OnAfterAddFilesEx([InAttribute] int cProjects, [InAttribute] int cFiles, [InAttribute] IVsProject[] rgpProjects, [InAttribute] int[] rgFirstIndices, [InAttribute] string[] rgpszMkDocuments, [InAttribute] VSADDFILEFLAGS[] rgFlags)
        {
            VsSelection vsSelection = GetVsSelectionNoFileNamesAllNodes();

            // Start by iterating through all projects calling this function
            for (int iProject = 0; iProject < cProjects; iProject++)
            {
                IVsSccProject2 sccProject = rgpProjects[iProject] as IVsSccProject2;

                // If the project is not controllable, or is not controlled, skip it
                if (sccProject == null)
                {
                    continue;
                }

                // Files in this project are in rgszMkOldNames, rgszMkNewNames arrays starting with iProjectFilesStart index and ending at iNextProjecFilesStart-1
                int iProjectFilesStart = rgFirstIndices[iProject];
                int iNextProjectFilesStart = cFiles;
                if (iProject < cProjects - 1)
                {
                    iNextProjectFilesStart = rgFirstIndices[iProject + 1];
                }


                // Now that we know which files belong to this project, iterate the project files
                for (int iFile = iProjectFilesStart; iFile < iNextProjectFilesStart; iFile++)
                {
                    string fileName = rgpszMkDocuments[iFile];
                    if (Options.AutoAdd)
                    {
                        vsSelection.FileNames.Add(fileName);
                        continue;
                    }

                    string msg = String.Format(Resources.Add_filename_to_Perforce, fileName);
                    DialogResult dialogResult = MessageBox.Show(msg, Resources.Add_File, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dialogResult == DialogResult.Yes)
                    {
                        vsSelection.FileNames.Add(fileName);
                    }
                }

            }

            if (vsSelection.FileNames.Count > 0)
            {
                AddFiles(vsSelection);
            }

            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryAddDirectories ([InAttribute] IVsProject pProject, [InAttribute] int cDirectories, [InAttribute] string[] rgpszMkDocuments, [InAttribute] VSQUERYADDDIRECTORYFLAGS[] rgFlags, [OutAttribute] VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, [OutAttribute] VSQUERYADDDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterAddDirectoriesEx ([InAttribute] int cProjects, [InAttribute] int cDirectories, [InAttribute] IVsProject[] rgpProjects, [InAttribute] int[] rgFirstIndices, [InAttribute] string[] rgpszMkDocuments, [InAttribute] VSADDDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }



        /// <summary>
        /// Implement OnQueryRemoveFiles event to warn the user when he's deleting controlled files.
        /// The user gets the chance to cancel the file removal.
        /// This routine only builds the list of files that MIGHT be removed from Perforce.
        /// </summary>
        public int OnQueryRemoveFiles([InAttribute] IVsProject pProject, [InAttribute] int cFiles, [InAttribute] string[] rgpszMkDocuments, [InAttribute] VSQUERYREMOVEFILEFLAGS[] rgFlags, [OutAttribute] VSQUERYREMOVEFILERESULTS[] pSummaryResult, [OutAttribute] VSQUERYREMOVEFILERESULTS[] rgResults)
        {
            pSummaryResult[0] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveOK;
            if (rgResults != null)
            {
                for (int iFile = 0; iFile < cFiles; iFile++)
                {
                    rgResults[iFile] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveOK;
                }
            }

            if (_vsSelectionFilesDeleted == null || _vsSelectionFilesDeleted.FileNames.Count == 0)
            {
                _vsSelectionFilesDeleted = GetVsSelectionNoFileNamesAllNodes();
            }
            try
            {
                var sccProject = pProject as IVsSccProject2;
                string projectName;
                if (sccProject == null)
                {
                    // This is the solution calling
                    projectName = _sccProvider.GetSolutionFileName();
                }
                else
                {
                    // If the project doesn't support source control, it will be skipped
                    projectName = _sccProvider.GetProjectFileName(sccProject);
                }

                if (projectName != null)
                {
                    for (int iFile = 0; iFile < cFiles; iFile++)
                    {
                        string fileName = rgpszMkDocuments[iFile];
                        FileState state = GetFileState(fileName);
                        if (state == FileState.NotInPerforce)
                        {
                            if (rgResults != null)
                            {
                                rgResults[iFile] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveOK;
                                pSummaryResult[iFile] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveOK;
                            }
                        }
                        else
                        {
                            if (state == FileState.OpenForEdit)
                            {
                                if (rgResults != null)
                                {
                                    rgResults[iFile] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveNotOK;
                                }
                                pSummaryResult[iFile] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveNotOK;
                                string msg = String.Format(Resources.Cant_Remove_File_filename_Because_It_Is_Checked_Out, fileName);
                                MessageBox.Show(msg, Resources.Rename_File, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                if (rgResults != null)
                                {
                                    rgResults[iFile] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveOK;
                                }
                                pSummaryResult[iFile] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveOK;
                            }
                        }
                        if (IsInPerforceAndIsEligibleForDelete(fileName))
                        {
                            // This is a controlled file
                            if (!Options.AutoDelete)
                            {
                                // Warn the user
                                var uiShell = (IVsUIShell)_sccProvider.GetService(typeof(SVsUIShell));
                                var clsid = Guid.Empty;
                                int dialogResult;
                                string messageText = Resources.TPD_DeleteControlledFile;
                                string messageCaption = Resources.ProviderName;
                                int result = uiShell.ShowMessageBox(
                                    0,
                                    ref clsid,
                                    messageCaption,
                                    String.Format(CultureInfo.CurrentUICulture, messageText, fileName),
                                    string.Empty,
                                    0,
                                    OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                                    OLEMSGICON.OLEMSGICON_QUERY,
                                    0,
                                    // false = application modal; true would make it system modal
                                    out dialogResult);
                                if (result != VSConstants.S_OK || dialogResult != (int)DialogResult.Yes)
                                {
                                    pSummaryResult[0] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveNotOK;
                                    if (rgResults != null)
                                    {
                                        rgResults[iFile] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveNotOK;
                                    }
                                    // Don't spend time iterating through the rest of the files once the delete has been cancelled
                                    break;
                                }
                            }

                            // User has said okay, we're going to delete this controlled file.
                            _vsSelectionFilesDeleted.FileNames.Add(fileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("SccProviderService.OnQueryRemove() Exception: {0}", ex.Message));
                pSummaryResult[0] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveNotOK;
                if (rgResults != null)
                {
                    for (int iFile = 0; iFile < cFiles; iFile++)
                    {
                        rgResults[iFile] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveNotOK;
                    }
                }
            }
            
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Shameful hack for unit testing.
        /// Don't use a background thread when unit testing.
        /// </summary>
        public bool IsUnitTesting;

        public int OnAfterRemoveFiles([InAttribute] int cProjects, [InAttribute] int cFiles, [InAttribute] IVsProject[] rgpProjects, [InAttribute] int[] rgFirstIndices, [InAttribute] string[] rgpszMkDocuments, [InAttribute] VSREMOVEFILEFLAGS[] rgFlags)
        {
            if (IsUnitTesting)
            {
                DeleteFilesActuallyDeletedByVS();
            }
            else
            {
                Task.Factory.StartNew(DeleteFilesActuallyDeletedByVS);
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Visual Studio 2010 is not properly telling me when a file is actually being deleted as opposed to being excluded from a project.
        /// With C#, a deleted file is actually deleted before OnAfterRemoveFiles().
        /// But with C++, the delete occurs AFTER OnAfterRemoveFiles().
        /// My hack, embarrassing as it is, is to wait a few milliseconds on another thread before doing the check to see if VS actually deleted it.
        /// </summary>
        private void DeleteFilesActuallyDeletedByVS()
        {
            Thread.Sleep(500);
            for (int i = 0; i < _vsSelectionFilesDeleted.FileNames.Count; i++)
            {
                string fileName = _vsSelectionFilesDeleted.FileNames[i];
                bool exists = File.Exists(fileName);
                if (exists)
                {
                    // This file wasn't actually deleted, it was just excluded from the project
                    _vsSelectionFilesDeleted.FileNames.RemoveAt(i--);
                }
                
            }
            if (_vsSelectionFilesDeleted.FileNames.Count > 0)
            {
                DeleteFiles(_vsSelectionFilesDeleted);
                _vsSelectionFilesDeleted.FileNames.Clear();
            }
        }

        public int OnQueryRemoveDirectories([InAttribute] IVsProject pProject, [InAttribute] int cDirectories, [InAttribute] string[] rgpszMkDocuments, [InAttribute] VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, [OutAttribute] VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, [OutAttribute] VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterRemoveDirectories([InAttribute] int cProjects, [InAttribute] int cDirectories, [InAttribute] IVsProject[] rgpProjects, [InAttribute] int[] rgFirstIndices, [InAttribute] string[] rgpszMkDocuments, [InAttribute] VSREMOVEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryRenameFiles([InAttribute] IVsProject pProject, [InAttribute] int cFiles, [InAttribute] string[] rgszMkOldNames, [InAttribute] string[] rgszMkNewNames, [InAttribute] VSQUERYRENAMEFILEFLAGS[] rgFlags, [OutAttribute] VSQUERYRENAMEFILERESULTS[] pSummaryResult, [OutAttribute] VSQUERYRENAMEFILERESULTS[] rgResults)
        {
            for (int i = 0; i < cFiles; i++)
            {
                string sourceName = rgszMkOldNames[i];
                FileState state = GetFileState(sourceName);
                bool isDirectory = String.IsNullOrEmpty(Path.GetFileName(sourceName));
                if (state == FileState.NotInPerforce || IsInPerforceAndIsEligibleForRename(sourceName) || isDirectory)
                {
                    if (rgResults != null)
                    {
                        rgResults[i] = VSQUERYRENAMEFILERESULTS.VSQUERYRENAMEFILERESULTS_RenameOK;
                        pSummaryResult[i] = VSQUERYRENAMEFILERESULTS.VSQUERYRENAMEFILERESULTS_RenameOK;
                    }
                }
                else
                {
                    if (rgResults != null)
                    {
                        rgResults[i] = VSQUERYRENAMEFILERESULTS.VSQUERYRENAMEFILERESULTS_RenameNotOK;
                    }
                    pSummaryResult[i] = VSQUERYRENAMEFILERESULTS.VSQUERYRENAMEFILERESULTS_RenameNotOK;
                    string msg = String.Format(Resources.Cant_Rename_File_filename_Because_It_Must_Be_Checked_Out, sourceName);
                    MessageBox.Show(msg, Resources.Rename_File, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Implement OnAfterRenameFiles event to rename a file in the source control store when it gets renamed in the project
        /// Also, rename the store if the project itself is renamed
        /// </summary>
        public int OnAfterRenameFiles([InAttribute] int cProjects, [InAttribute] int cFiles, [InAttribute] IVsProject[] rgpProjects, [InAttribute] int[] rgFirstIndices, [InAttribute] string[] rgszMkOldNames, [InAttribute] string[] rgszMkNewNames, [InAttribute] VSRENAMEFILEFLAGS[] rgFlags)
        {
            VsSelection vsSelection = GetVsSelectionNoFileNamesAllNodes();
            VsSelection vsSelectionUncontrolled = GetVsSelectionNoFileNamesAllNodes();

            // Start by iterating through all projects calling this function
            for (int iProject = 0; iProject < cProjects; iProject++)
            {
                // Files in this project are in rgszMkOldNames, rgszMkNewNames arrays starting with iProjectFilesStart index and ending at iNextProjecFilesStart-1
                int iProjectFilesStart = rgFirstIndices[iProject];
                int iNextProjecFilesStart = cFiles;
                if (iProject < cProjects - 1)
                {
                    iNextProjecFilesStart = rgFirstIndices[iProject+1];
                }

                // Now that we know which files belong to this project, iterate the project files
                for (int iFile = iProjectFilesStart; iFile < iNextProjecFilesStart; iFile++)
                {
                    string sourceName = rgszMkOldNames[iFile];
                    string targetName = rgszMkNewNames[iFile];
                    string fileName = sourceName + "|" + targetName;
                    if (IsInPerforceAndIsEligibleForRename(sourceName))
                    {
                        vsSelection.FileNames.Add(fileName); // we'll be asking P4Cache to update the targetName, which shows in Solution Explorer
                    }
                    else
                    {
                        vsSelectionUncontrolled.FileNames.Add(fileName); // we'll be asking P4Cache to update the targetName, which shows in Solution Explorer
                    }
                }
            }

            if (vsSelection.FileNames.Count > 0)
            {
                RenameFiles(vsSelection);
            }

            if (IsSolutionLoaded && vsSelectionUncontrolled.FileNames.Count > 0 && IsCacheActive_WaitForUpdate())
            {
                // Update the file states of renamed files.
                _p4Cache.AddOrUpdateFilesBackground(vsSelectionUncontrolled);
            }

            return VSConstants.S_OK;
        }

        public int OnQueryRenameDirectories([InAttribute] IVsProject pProject, [InAttribute] int cDirs, [InAttribute] string[] rgszMkOldNames, [InAttribute] string[] rgszMkNewNames, [InAttribute] VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, [OutAttribute] VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, [OutAttribute] VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterRenameDirectories([InAttribute] int cProjects, [InAttribute] int cDirs, [InAttribute] IVsProject[] rgpProjects, [InAttribute] int[] rgFirstIndices, [InAttribute] string[] rgszMkOldNames, [InAttribute] string[] rgszMkNewNames, [InAttribute] VSRENAMEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterSccStatusChanged([InAttribute] int cProjects, [InAttribute] int cFiles, [InAttribute] IVsProject[] rgpProjects, [InAttribute] int[] rgFirstIndices, [InAttribute] string[] rgpszMkDocuments, [InAttribute] uint[] rgdwSccStatus)
        {
            return VSConstants.E_NOTIMPL;
        }

        #region Files and Project Management Functions

        /// <summary>
        /// Returns whether this source control provider is the active scc provider.
        /// Is set true for unit testing
        /// </summary>
        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; }
        }


        /// <summary>
        /// Returns the FileState status of the specified file
        /// Throws on connect error.
        /// </summary>
        /// <param name="vsFileName">the file name.</param>
        /// <returns>the Perforce file state, or FileState.NotSet if error.</returns>
        public FileState GetFileState(string vsFileName)
        {
            if (!IsFirstP4CacheUpdateComplete || String.IsNullOrEmpty(vsFileName) || !IsSolutionLoaded)
            {
                return FileState.NotSet;
            }

            FileState state;
            if (IsCacheCurrent())
            {
                state = _p4Cache[vsFileName];
                return state;
            }

            Log.Information(string.Format("Cache not current, so getting Perforce file state the slow way for {0}", vsFileName));
            return GetFileStateWithoutCache(vsFileName);
        }

        public FileState GetFileStateWithoutCache(string vsFilename)
        {
            if (_p4Service == null)
            {
                return FileState.NotSet;
            }

            FileState state;
            try
            {
                if (!_p4Service.IsConnected)
                {
                    _p4Service.Connect();
                }
            }
            catch (ArgumentException)
            {
                return FileState.NotSet;
            }
            catch (Perforce.P4.P4Exception)
            {
                return FileState.NotSet;
            }
            finally
            {
                string message;
                state = _p4Service.GetVsFileState(vsFilename, out message);
                //_p4Service.Disconnect();
            }

            return state;
        }

        /// <summary>
        /// Execute the commandMethod named commandName if conditionMethod is true, on all files in selection. Return false if the command fails.
        /// </summary>
        /// <param name="vsSelection">The selected nodes and files.</param>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="changelistNumber">The number of the CL this command should be executed on.</param>
        /// <param name="conditionMethod">The condition that must be true before executing commandMethod.</param>
        /// <param name="commandMethod">The command to execute.</param>
        /// <returns>false if the command fails.</returns>
        public bool ExecuteCommand(VsSelection vsSelection, string commandName, int changelistNumber, Func<string, bool> conditionMethod, Func<CommandArguments, bool> commandMethod)
        {
            if (!IsSolutionLoaded || vsSelection.FileNames.Count <= 0)
            {
                return true;
            }

            Log.Debug(String.Format("SccProviderService.{0}: Executing command for {1} files on {2} nodes.", commandName, vsSelection.FileNames.Count, vsSelection.Nodes.Count));

            bool success = true;
            try
            {
                _p4Service.Connect();
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (Perforce.P4.P4Exception)
            {
                return false;
            }
            finally
            {
                foreach (string fileName in vsSelection.FileNames)
                {
                    string sourceName = ConvertPipedFileNameToSource(fileName);
                    if (conditionMethod(sourceName))
                    {
                        var cmdArgs = new CommandArguments(fileName, changelistNumber);
                        if (!commandMethod(cmdArgs))
                        {
                            success = false;
                        }
                    }
                }

                if (IsSolutionLoaded && IsCacheActive_WaitForUpdate())
                {
                    _p4Cache.AddOrUpdateFilesBackground(vsSelection); // All nodes and file names in vsSelection will be refreshed when P4Cache.P4CacheUpdated is thrown.
                }

                _p4Service.Disconnect();
            }

            return success;
        }

        /// <summary>
        /// Execute the commandMethod named commandName if conditionMethod is true, on all files in selection. Return false if the command fails.
        /// </summary>
        /// <param name="vsSelection">The selected nodes and files.</param>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="conditionMethod">The condition that must be true before executing commandMethod.</param>
        /// <param name="commandMethod">The command to execute.</param>
        /// <returns>false if the command fails.</returns>
        public bool ExecuteCommand(VsSelection vsSelection, string commandName, Func<string, bool> conditionMethod, Func<CommandArguments, bool> commandMethod)
        {
            return ExecuteCommand(vsSelection, commandName, CL_DEFAULT, conditionMethod, commandMethod);
        }

        /// <summary>
        /// If fileName is piped, like sourceName|targetName, return the targetName
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string ConvertPipedFileNameToSource(string fileName)
        {
            string[] splits = fileName.Split('|');
            return splits[0];
        }

        /// <summary>
        /// Checkout the specified files from source control
        /// </summary>
        /// <param name="selection">the selected file names and nodes.</param>
        /// <returns>false if the command fails.</returns>
        public bool CheckoutFiles(VsSelection selection)
        {
            return ExecuteCommand(selection, Resources.Checkout_Files, IsEligibleForCheckOut, CheckoutFile);
        }

        /// <summary>
        /// Checkout the specified files from source control
        /// </summary>
        /// <param name="selection">the selected file names and nodes.</param>
        /// <param name="changeListNumber">The number of the CL to check out files to</param>
        /// <returns>false if the command fails.</returns>
        public bool CheckoutFiles(VsSelection selection, int changeListNumber)
        {
            return ExecuteCommand(selection, Resources.Checkout_Files, changeListNumber, IsEligibleForCheckOut, CheckoutFile);
        }

        /// <summary>
        /// Checkout the specified file from source control.
        /// </summary>
        /// <param name="cmdArgs">the arguments for this command.</param>
        /// <returns>false if the command fails.</returns>
        public bool CheckoutFile(CommandArguments cmdArgs)
        {
            string message;
            var changelistNumber = cmdArgs.ChangelistNumber;
            if (changelistNumber == CL_NEW)
            {
                _p4Service.CreateChangelist("<VS2P4>", ref changelistNumber); // TODO: prompt 
            }
            bool success = _p4Service.EditFile(cmdArgs.Filename, changelistNumber, out message);
            return success;
        }

        /// <summary>
        /// Lock the specified files. Currently used only for unit testing
        /// </summary>
        /// <param name="selection">the selected file names and nodes.</param>
        /// <returns>false if the command fails.</returns>
        public bool LockFiles(VsSelection selection)
        {
            // Used only for unit testing, or we should write an IsEligibleForLock()
            return ExecuteCommand(selection, Resources.Lock_Files, IsEligibleForRevertIfUnchanged, LockFile);
        }

        /// <summary>
        /// Marks the specified file to be locked
        /// </summary>
        /// <param name="cmdArgs">the arguments for this command.</param>
        /// <returns>false if the command fails.</returns>
        public bool LockFile(CommandArguments cmdArgs)
        {
            string message;
            bool success = _p4Service.LockFile(cmdArgs.Filename, out message);
            return success;
        }

        /// <summary>
        /// Add (actually mark for add) the specified files from source control
        /// </summary>
        /// <param name="selection">the selected file names and nodes.</param>
        /// <returns>false if the command fails.</returns>
        public bool AddFiles(VsSelection selection)
        {
            // First force current file states for new files into the cache
            if (IsSolutionLoaded && IsCacheActive_WaitForUpdate())
            {
                _p4Cache.AddOrUpdateFilesBackground(selection);
            }

            return ExecuteCommand(selection, Resources.Add_Files, IsEligibleForAdd, AddFile);
        }

        /// <summary>
        /// Marks the specified file to be added to Perforce
        /// </summary>
        /// <param name="cmdArgs">the arguments for this command.</param>
        /// <returns>false if the command fails.</returns>
        public bool AddFile(CommandArguments cmdArgs)
        {
            string message;
            bool success = _p4Service.AddFile(cmdArgs.Filename, out message);
            return success;
        }

        /// <summary>
        /// Reverts the specified files if they are unchanged from the head revision
        /// </summary>
        /// <param name="selection">the selected file names and nodes.</param>
        /// <returns>false if the command fails.</returns>
        public bool RevertFilesIfUnchanged(VsSelection selection)
        {
            return ExecuteCommand(selection, Resources.Revert_Files_If_Unchanged, IsEligibleForRevertIfUnchanged, RevertFileIfUnchanged);
        }

        /// <summary>
        /// Reverts the specified file if it is unchanged from the head revision
        /// </summary>
        /// <param name="cmdArgs">the arguments for this command.</param>
        /// <returns>false if the command fails.</returns>
        public bool RevertFileIfUnchanged(CommandArguments cmdArgs)
        {
            string message;
            bool success = _p4Service.RevertIfUnchangedFile(cmdArgs.Filename, out message);
            return success;
        }

        /// <summary>
        /// Reverts the specified files 
        /// </summary>
        /// <param name="selection">the selected file names and nodes.</param>
        /// <returns>false if the command fails.</returns>
        public bool RevertFiles(VsSelection selection)
        {
            if (Options.PromptBeforeRevert)
            {
                DialogResult result = MessageBox.Show(
                    Resources.revertPrompt,
                    Resources.Revert,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    return false;
                }
            }

            return ExecuteCommand(selection, Resources.Revert_Files, IsEligibleForRevert, RevertFile);
        }

        /// <summary>
        /// Reverts the specified file
        /// </summary>
        /// <param name="cmdArgs">the arguments for this command.</param>
        /// <returns>false if the command fails.</returns>
        public bool RevertFile(CommandArguments cmdArgs)
        {
            string message;
            bool success = _p4Service.RevertFile(cmdArgs.Filename, out message);
            return success;
        }

        /// <summary>
        /// Removes (marks for delete) the specified files 
        /// </summary>
        /// <param name="selection">the selected file names and nodes.</param>
        /// <returns>false if the command fails.</returns>
        public bool DeleteFiles(VsSelection selection)
        {
            return ExecuteCommand(selection, Resources.Delete_Files, IsInPerforceAndIsEligibleForDelete, DeleteFile);
        }

        /// <summary>
        /// Marks the specified file to be deleted from Perforce.
        /// Currently we don't have a command for this. It's done as a part of VS file removal, if AutoDelete is set.
        /// </summary>
        /// <param name="cmdArgs">the arguments for this command.</param>
        /// <returns>false if the command fails.</returns>
        public bool DeleteFile(CommandArguments cmdArgs)
        {
            var fileName = cmdArgs.Filename;
            string message;
            if (IsEligibleForRevert(fileName))
            {
                // We must revert this file before we can mark it for delete
                if (Options.PromptBeforeRevert)
                {
                    DialogResult result = MessageBox.Show(
                        Resources.revertPrompt,
                        Resources.Revert,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (result != DialogResult.Yes)
                    {
                        return false;
                    }
                } 
                _p4Service.RevertFile(fileName, out message);
            }

            bool success = _p4Service.DeleteFile(fileName, out message);
            return success;
        }

        /// <summary>
        /// Rename (actually move) the specified files.
        /// Note that for purposes of refactoring, fileNames are piped, as in sourceFile|targetFile.
        /// Files must be checked out before you can rename them.
        /// </summary>
        /// <param name="selection">the file names and nodes.</param>
        /// <returns>false if the command fails.</returns>
        public bool RenameFiles(VsSelection selection)
        {
            // First force current file states for new files into the cache
            if (IsSolutionLoaded && IsCacheActive_WaitForUpdate())
            {
                _p4Cache.AddOrUpdateFilesBackground(selection);
            }

            return ExecuteCommand(selection, Resources.Rename_Files, IsInPerforceAndIsEligibleForRename, RenameFile);
        }

        /// <summary>
        /// Rename (actually move) the specified file.
        /// Note that for purposes of refactoring, fileNames are piped, as in sourceFile|targetFile.
        /// </summary>
        /// <param name="cmdArgs">the arguments for this command.</param>
        /// <returns>false if the command fails.</returns>
        public bool RenameFile(CommandArguments cmdArgs)
        {
            var pipedFileName = cmdArgs.Filename;
            string[] splits = pipedFileName.Split('|');
            bool success = false;
            if (splits.Length < 2)
            {
                string msg = String.Format(Resources.RenameFile_expects_piped_fileName, pipedFileName);
                Log.Error(msg);
                throw new ArgumentException(msg);
            }

            string sourceName = splits[0];
            string targetName = splits[1];

            string sourceDirectory = Path.GetDirectoryName(sourceName);
            string targetDirectory = Path.GetDirectoryName(targetName);
            bool createdSourceDirectory = false;
            if (sourceDirectory != targetDirectory)
            {
                // VS may have done a folder rename
                if (!Directory.Exists(sourceDirectory))
                {
                    Directory.CreateDirectory(sourceDirectory);
                    createdSourceDirectory = true;
                }
            }

            if (File.Exists(targetName) && !File.Exists(sourceName))
            {
                // VS has already done the rename. Try to undo that so we can let Perforce do it again, below.
                try
                {
                    File.Move(targetName, sourceName);
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("SccProviderService.RenameFile() Exception: {0}", ex.Message));
                }
            }

            string message;
            try
            {
                _p4Service.Connect();
            }
            catch (ArgumentException ex)
            {
                Log.Error(String.Format("SccProviderService.RenameFile() Exception: {0}", ex.Message));
            }
            catch (Perforce.P4.P4Exception ex)
            {
                Log.Error(String.Format("SccProviderService.RenameFile() P4Exception: {0}", ex.Message));
            }
            finally
            {
                success = _p4Service.MoveFile(sourceName, targetName, out message);
                _p4Service.Disconnect();
            }

            if (createdSourceDirectory && Directory.Exists(sourceDirectory))
            {
                Directory.Delete(sourceDirectory);
            }

            return success;
        }

        /// <summary>
        /// Refresh all glyphs in the solution. 
        /// We do the entire solution whenever a project is loaded,
        /// including the initial load.
        /// </summary>
        public void Refresh(IVsHierarchy node)
        {
            VsSelection vsSelection = null;
            if (node == null)
            {
                vsSelection = _sccProvider.GetSolutionSelection();
            }
            else
            {
                VSITEMSELECTION vsItem;
                vsItem.pHier = node;
                vsItem.itemid = VSConstants.VSITEMID_ROOT;

                IList<VSITEMSELECTION> nodes = new List<VSITEMSELECTION>();
                nodes.Add(vsItem);
                vsSelection = _sccProvider.GetSelection(nodes);
            } 
            
            _p4Cache.Initialize(vsSelection);

            // All nodes and fileNames in the selection will be refreshed when P4Cache.P4CacheUpdated is thrown.
        }

        /// <summary>
        /// Is the P4 Server connected to a Swarm instance?
        /// </summary>
        public bool IsSwarmConnected { get { return string.IsNullOrEmpty(_p4Service.SwarmURL); } }

        /// <summary>
        /// Open file selection's Swarm URLs in browser
        /// </summary>
        public bool OpenInSwarm(VsSelection vsSelection)
        {
            try
            {
                if (!_p4Service.IsConnected)
                {
                    _p4Service.Connect();
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (Perforce.P4.P4Exception)
            {
                return false;
            }
            if (string.IsNullOrEmpty(_p4Service.SwarmURL))
            {
                return false;
            }

            foreach (string fileName in vsSelection.FileNames)
            {
                string warning;
                var p4Filename = _map.GetP4FileName(fileName, out warning).TrimStart(new char[] { '/' });
                var url = _p4Service.SwarmURL + "/files/" + p4Filename;
                if (!string.IsNullOrEmpty(warning))
                {
                    Log.Warning("Map.GetP4FileName: " + warning);
                }
                Process.Start("explorer", url);
            }

            return true;
        }

        /// <summary>
        /// Get Latest Revision for the specified files 
        /// </summary>
        /// <param name="selection">the selected file names and nodes.</param>
        /// <returns>false if the command fails.</returns>
        public bool GetLatestRevision(VsSelection selection)
        {
            return ExecuteCommand(selection, Resources.Get_Latest_Revision, IsEligibleForGetLatestRevision, GetLatestRevision);
        }

        /// <summary>
        /// Get the latest revision of file
        /// </summary>
        /// <param name="cmdArgs">the arguments for this command.</param>
        /// <returns>false if the command fails.</returns>
        public bool GetLatestRevision(CommandArguments cmdArgs)
        {
            string message;
            bool success = _p4Service.SyncFile(cmdArgs.Filename, out message);
            return success;
        }

        /// <summary>
        /// View Revision History report for the specified files 
        /// </summary>
        /// <param name="selection">the selected file names and nodes.</param>
        /// <returns>false if the command fails.</returns>
        public bool RevisionHistory(VsSelection selection)
        {
            return ExecuteCommand(selection, Resources.Revision_History, IsEligibleForRevisionHistory, RevisionHistory);
        }

        /// <summary>
        /// Show the Revision History Report in P4V.exe
        /// </summary>
        /// <param name="cmdArgs">the arguments for this command.</param>
        /// <returns>false if the command fails.</returns>
        public bool RevisionHistory(CommandArguments cmdArgs)
        {
            return _p4Service.RevisionHistory(cmdArgs.Filename);
        }


        /// <summary>
        /// View Diff report for the specified files 
        /// </summary>
        /// <param name="selection">the selected file names and nodes.</param>
        /// <returns>false if the command fails.</returns>
        public bool Diff(VsSelection selection)
        {
            return ExecuteCommand(selection, Resources.Diff, IsEligibleForDiff, Diff);
        }

        /// <summary>
        /// Show the Diff Report (Diff of head revision against workspace file) for fileName
        /// </summary>
        /// 
        /// <param name="cmdArgs">the arguments for this command.</param>
        /// <returns>false if the command fails.</returns>
        public bool Diff(CommandArguments cmdArgs)
        {
            return _p4Service.Diff(cmdArgs.Filename);
        }

        /// <summary>
        /// View Time-Lapse report for the specified files 
        /// </summary>
        /// <param name="selection">the selected file names and nodes.</param>
        /// <returns>false if the command fails.</returns>
        public bool TimeLapse(VsSelection selection)
        {
            return ExecuteCommand(selection, Resources.Time_Lapse, IsEligibleForTimeLapse, TimeLapse);
        }

        /// <summary>
        /// Show the Time-Lapse Report for fileName
        /// </summary>
        /// <param name="cmdArgs">the arguments for this command.</param>
        /// <returns>false if the command fails.</returns>
        public bool TimeLapse(CommandArguments cmdArgs)
        {
            return _p4Service.TimeLapse(cmdArgs.Filename);
        }

        #endregion

        /// <summary>
        /// Load options that have either been persisted in previous sessions, or saved in the current session via SaveOptions.
        /// </summary>
        /// <returns>P4Options object containing the current options saved by the user.</returns>
        internal P4Options LoadOptions(out PersistedP4OptionSettings persistedSettings)
        {
           // For default settings, we want to first try a group named the same as the solution, if it
            // is loaded, then a "Defaults" group.
            string solutionName = _sccProvider.GetSolutionFileName();
            List<string> groupNames;
            if (solutionName != null)
            {
                groupNames = new List<string> { Path.GetFileNameWithoutExtension(solutionName), "Default" };
            }
            else
            {
                groupNames = new List<string> { "Default" };
            }
            P4OptionsDefaultsProvider defaults = new P4OptionsDefaultsProvider(groupNames);
            if (dte2 != null)
            {
                _persistedSettings = new PersistedP4OptionSettings(dte2.Globals, defaults);
            }
            else
            {
                _persistedSettings = new PersistedP4OptionSettings(defaults);
            }
            Options = P4Options.Load(_persistedSettings, _sccProvider);
            persistedSettings = _persistedSettings;
            return Options;
        }

        /// <summary>
        /// Save options locally and persisted between sessions.
        /// </summary>
        /// <param name="options">The options to save.</param>
        internal void SaveOptions(P4Options options, PersistedP4OptionSettings persistedSettings)
        {
            _persistedSettings = persistedSettings;
            if (_persistedSettings == null)
            {
                throw new InvalidOperationException("SaveOptions called without a preceding LoadOptions.");
            }
            Options = options;
            options.Save(_persistedSettings);
            Log.OptionsLevel = options.LogLevel;

            if (_isActive && IsSolutionLoaded)
            {
                StartP4ServiceAndInitializeCache();
            }
        }

        #region IVsSccGlyphs Members

        // Remember the base index where our custom scc glyph start
        private uint _customSccGlyphBaseIndex;

        // Our custom image list
        ImageList _customSccGlyphsImageList;


        public int GetCustomGlyphList(uint BaseIndex, out uint pdwImageListHandle)
        {
            // If this is the first time we got called, construct the image list, remember the index, etc
            if (_customSccGlyphsImageList == null)
            {
                // The shell calls this function when the provider becomes active to get our custom glyphs
                // and to tell us what's the first index we can use for our glyphs
                // Remember the index in the scc glyphs (VsStateIcon) where our custom glyphs will start
                _customSccGlyphBaseIndex = BaseIndex;

                // Create a new imagelist
                _customSccGlyphsImageList = new ImageList();

                // Set the transparent color for the imagelist (the SccGlyphs.bmp uses magenta for background)
                _customSccGlyphsImageList.TransparentColor = Color.FromArgb(255, 0, 255);

                // Set the corret imagelist size (7x16 pixels, otherwise the system will either stretch the image or fill in with black blocks)
                _customSccGlyphsImageList.ImageSize = new Size(7, 16);

                // Add the custom scc glyphs we support to the list
                // NOTE: VS2005 and VS2008 are limited to 4 custom scc glyphs (let's hope this will change in future versions)
                var sccGlyphs = (Image)Resources.SccGlyphs4;
                _customSccGlyphsImageList.Images.AddStrip(sccGlyphs);
            }

            // Return a Win32 HIMAGELIST handle to our imagelist to the shell (by keeping the ImageList a member of the class we guarantee the Win32 object is still valid when the shell needs it)
            pdwImageListHandle = (uint)_customSccGlyphsImageList.Handle;


            // Return success (If you don't want to have custom glyphs return VSConstants.E_NOTIMPL)
            return VSConstants.S_OK;
        }

        #endregion IVsSccGlyphs Members

        /// <summary>
        /// Returns true if fileName is eligible to be checked out (opened for edit)
        /// </summary>
        /// <param name="fileName">the file name</param>
        /// <returns>true if fileName is eligible to be checked out (opened for edit)</returns>
        public bool IsEligibleForCheckOut(string fileName)
        {
            FileState state = GetFileState(fileName);
            switch (state)
            {
                case FileState.NotSet:
                case FileState.NotInPerforce:
                case FileState.Locked: // Locked implies also Open For Edit
                case FileState.OpenForDelete:
                case FileState.DeletedAtHeadRevision:
                case FileState.OpenForAdd:
                case FileState.OpenForRenameSource:
                case FileState.OpenForRenameTarget:
                case FileState.NeedsResolved:
                    return false;
                case FileState.OpenForEdit:
                case FileState.OpenForEditDiffers:
                case FileState.OpenForDeleteOtherUser:
                case FileState.OpenForEditOtherUser:
                case FileState.LockedByOtherUser:
                case FileState.CheckedInHeadRevision:
                case FileState.CheckedInPreviousRevision:
                case FileState.OpenForIntegrate:
                case FileState.OpenForBranch:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns true if fileName is under Perforce control and is eligible to be deleted (marked for delete)
        /// </summary>
        /// <param name="fileName">the file name</param>
        /// <returns>true if fileName is eligible to be deleted (marked for delete)</returns>
        public bool IsInPerforceAndIsEligibleForDelete(string fileName)
        {
            FileState state = GetFileState(fileName);
            switch (state)
            {
                case FileState.NotSet:
                case FileState.NotInPerforce:
                case FileState.OpenForEdit:
                case FileState.OpenForEditDiffers:
                case FileState.Locked: // Locked implies also Open For Edit
                case FileState.OpenForDelete:
                case FileState.DeletedAtHeadRevision:
                case FileState.OpenForAdd:
                case FileState.OpenForRenameSource:
                case FileState.OpenForRenameTarget:
                case FileState.NeedsResolved:
                case FileState.OpenForBranch:
                case FileState.OpenForIntegrate:
                    return false;
                case FileState.OpenForDeleteOtherUser:
                case FileState.OpenForEditOtherUser:
                case FileState.LockedByOtherUser:
                case FileState.CheckedInHeadRevision:
                case FileState.CheckedInPreviousRevision:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns true if fileName is in Perforce and is eligible to be renamed (moved)
        /// </summary>
        /// <param name="fileName">the file name</param>
        /// <returns>true if fileName is eligible to be renamed (moved)</returns>
        public bool IsInPerforceAndIsEligibleForRename(string fileName)
        {
            FileState state = GetFileState(fileName);
            switch (state)
            {
                case FileState.NotSet:
                case FileState.NotInPerforce:
                case FileState.OpenForDelete:
                case FileState.DeletedAtHeadRevision:
                case FileState.OpenForRenameSource:
                case FileState.NeedsResolved:
                case FileState.OpenForBranch:
                case FileState.OpenForIntegrate:
                case FileState.CheckedInHeadRevision:
                case FileState.CheckedInPreviousRevision:
                    return false;
                case FileState.OpenForAdd:
                case FileState.OpenForEdit:
                case FileState.OpenForEditDiffers:
                case FileState.Locked: // Locked implies also Open For Edit
                case FileState.OpenForDeleteOtherUser:
                case FileState.OpenForEditOtherUser:
                case FileState.LockedByOtherUser:
                case FileState.OpenForRenameTarget:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns true if fileName is eligible to be added (marked for add)
        /// </summary>
        /// <param name="fileName">the file name</param>
        /// <returns>true if fileName is eligible to be added (marked for add)</returns>
        public bool IsEligibleForAdd(string fileName)
        {
            FileState state = GetFileState(fileName);
            switch (state)
            {
                case FileState.NotInPerforce:
                case FileState.NotSet:
                case FileState.OpenForDelete:
                case FileState.OpenForDeleteOtherUser:
                    return true;
                case FileState.OpenForEdit:
                case FileState.OpenForEditOtherUser:
                case FileState.OpenForEditDiffers:
                case FileState.Locked:
                case FileState.LockedByOtherUser:
                case FileState.DeletedAtHeadRevision:
                case FileState.OpenForAdd:
                case FileState.OpenForRenameSource:
                case FileState.OpenForRenameTarget:
                case FileState.CheckedInHeadRevision:
                case FileState.CheckedInPreviousRevision:
                case FileState.NeedsResolved:
                case FileState.OpenForBranch:
                case FileState.OpenForIntegrate:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns true if fileName is eligible to be reverted if it's unchanged
        /// </summary>
        /// <param name="fileName">the file name</param>
        /// <returns>true if fileName is eligible to be reverted if it's unchanged</returns>
        public bool IsEligibleForRevertIfUnchanged(string fileName)
        {
            FileState state = GetFileState(fileName);
            switch (state)
            {
                case FileState.NotSet:
                case FileState.NotInPerforce:
                case FileState.OpenForEditOtherUser:
                case FileState.LockedByOtherUser:
                case FileState.OpenForDelete:
                case FileState.OpenForDeleteOtherUser:
                case FileState.DeletedAtHeadRevision:
                case FileState.OpenForAdd:
                case FileState.OpenForRenameSource:
                case FileState.OpenForRenameTarget:
                case FileState.CheckedInHeadRevision:
                case FileState.CheckedInPreviousRevision:
                case FileState.OpenForIntegrate:
                case FileState.OpenForBranch:
                    return false;
                case FileState.OpenForEdit:
                case FileState.OpenForEditDiffers:
                case FileState.Locked: // implies also open for edit
                case FileState.NeedsResolved:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns true if fileName is eligible to be reverted
        /// </summary>
        /// <param name="fileName">the file name</param>
        /// <returns>true if fileName is eligible to be reverted</returns>
        public bool IsEligibleForRevert(string fileName)
        {
            FileState state = GetFileState(fileName);
            switch (state)
            {
                case FileState.NotSet:
                case FileState.NotInPerforce:
                case FileState.OpenForEditOtherUser:
                case FileState.LockedByOtherUser:
                case FileState.OpenForDeleteOtherUser:
                case FileState.DeletedAtHeadRevision:
                case FileState.CheckedInHeadRevision:
                case FileState.CheckedInPreviousRevision:
                case FileState.OpenForRenameSource:
                case FileState.OpenForRenameTarget: // P4 allows this, but we don't because it confuses VS, which doesn't see the rename
                    return false;
                case FileState.OpenForEdit:
                case FileState.OpenForEditDiffers:
                case FileState.Locked: // implies also open for edit
                case FileState.NeedsResolved:
                case FileState.OpenForBranch:
                case FileState.OpenForIntegrate:
                case FileState.OpenForDelete:
                case FileState.OpenForAdd:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns true if fileName has ever been submitted to Perforce and thus is eligible for one of of the report commands
        /// </summary>
        /// <param name="fileName">the file name</param>
        /// <returns>true if fileName has ever been submitted to Perforce and thus is eligible for one of of the report commands</returns>
        public bool IsEligibleForTimeLapse(string fileName)
        {
            return IsEligibleForRevisionHistory(fileName);
        }

        /// <summary>
        /// Returns true if fileName has ever been submitted to Perforce and thus is eligible for one of of the report commands
        /// </summary>
        /// <param name="fileName">the file name</param>
        /// <returns>true if fileName has ever been submitted to Perforce and thus is eligible for one of of the report commands</returns>
        public bool IsEligibleForRevisionHistory(string fileName)
        {
            FileState state = GetFileState(fileName);
            switch (state)
            {
                case FileState.NotSet:
                case FileState.NotInPerforce:
                case FileState.OpenForAdd:
                case FileState.OpenForRenameTarget: // is okay for diff
                case FileState.OpenForBranch:
                    return false;
                case FileState.OpenForEdit:
                case FileState.OpenForEditOtherUser:
                case FileState.OpenForEditDiffers:
                case FileState.Locked:
                case FileState.LockedByOtherUser:
                case FileState.OpenForDelete:
                case FileState.OpenForDeleteOtherUser:
                case FileState.DeletedAtHeadRevision:
                case FileState.OpenForRenameSource: // is okay for time-lapse and revision history, in P4V
                case FileState.CheckedInHeadRevision:
                case FileState.CheckedInPreviousRevision:
                case FileState.NeedsResolved:
                case FileState.OpenForIntegrate:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns true if fileName has ever been submitted to Perforce and thus is eligible for one of of the report commands
        /// </summary>
        /// <param name="fileName">the file name</param>
        /// <returns>true if fileName has ever been submitted to Perforce and thus is eligible for one of of the report commands</returns>
        public bool IsEligibleForDiff(string fileName)
        {
            FileState state = GetFileState(fileName);
            switch (state)
            {
                case FileState.NotSet:
                case FileState.NotInPerforce:
                case FileState.OpenForAdd:
                case FileState.OpenForRenameSource: // is okay for time-lapse and revision history, in P4V
                case FileState.DeletedAtHeadRevision:
                case FileState.OpenForBranch:
                    return false;
                case FileState.OpenForEdit:
                case FileState.OpenForEditOtherUser:
                case FileState.OpenForEditDiffers:
                case FileState.Locked:
                case FileState.LockedByOtherUser:
                case FileState.OpenForDelete:
                case FileState.OpenForDeleteOtherUser:
                case FileState.OpenForRenameTarget: // is okay for diff
                case FileState.CheckedInHeadRevision:
                case FileState.CheckedInPreviousRevision:
                case FileState.NeedsResolved:
                case FileState.OpenForIntegrate:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Return true if fileName is eligible for GetLatestRevision
        /// </summary>
        /// <param name="fileName">the file name</param>
        /// <returns>true if fileName is eligible for GetLatestRevision</returns>
        public bool IsEligibleForGetLatestRevision(string fileName)
        {
            FileState state = GetFileState(fileName);
            switch (state)
            {
                case FileState.NotSet:
                case FileState.NotInPerforce:
                case FileState.OpenForAdd:
                case FileState.CheckedInHeadRevision:
                case FileState.OpenForDelete:
                case FileState.OpenForBranch:
                case FileState.OpenForIntegrate:
                    return false;
                case FileState.OpenForEdit:
                case FileState.OpenForEditOtherUser:
                case FileState.OpenForEditDiffers:
                case FileState.Locked:
                case FileState.LockedByOtherUser:
                case FileState.OpenForDeleteOtherUser:
                case FileState.DeletedAtHeadRevision:
                case FileState.OpenForRenameSource:
                case FileState.OpenForRenameTarget:
                case FileState.CheckedInPreviousRevision:
                case FileState.NeedsResolved:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal bool IsCacheActive_WaitForUpdate()
        {
            return (_p4Cache != null) && _p4Cache.IsCacheActive_WaitForUpdate();
        }

        internal bool IsCacheCurrent()
        {
            return (_p4Cache != null) && _p4Cache.IsCacheCurrent;
        }

        public class CommandArguments
        {
            public CommandArguments(string filename)
            {
                Filename = filename;
                ChangelistNumber = CL_DEFAULT;
            }

            public CommandArguments(string filename, int changelistNumber)
            {
                Filename = filename;
                ChangelistNumber = changelistNumber;
            }

            public string Filename { get; private set; }
            public int ChangelistNumber { get; private set; }
        }
    }
}