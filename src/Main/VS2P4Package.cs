using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;
using MsVsShell = Microsoft.VisualStudio.Shell;

namespace BruSoft.VS2P4
{
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// It also is responsible for handling the enabling and execution of source control commands.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    [MsVsShell.DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\10.0Exp")]
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // Register an options page visible as Tools/Options/Source Control/VS2P4 Connection when the provider is active
    [MsVsShell.ProvideOptionPageAttribute(typeof(SccProviderConnectionOptions), "Source Control", "VS2P4 Connection", 106, 107, false)]
    [ProvideToolsOptionsPageVisibility("Source Control", "VS2P4 Connection", "8358dd60-00b0-478a-83b8-ea8ae3ecdaa2")]
    // Set the sort order on this page so that it comes before the Commands page.
    [ProvideToolsOptionsPageOrder("Source Control", "VS2P4 Connection", 1)]
    // Register an options page visible as Tools/Options/Source Control/VS2P4 Commands when the provider is active
    [MsVsShell.ProvideOptionPageAttribute(typeof(SccProviderCommandOptions), "Source Control", "VS2P4 Commands", 108, 109, false)]
    [ProvideToolsOptionsPageVisibility("Source Control", "VS2P4 Commands", "8358dd60-00b0-478a-83b8-ea8ae3ecdaa2")]
    // Set the sort order on this page so that it comes after Connection.
    [ProvideToolsOptionsPageOrder("Source Control", "VS2P4 Commands", 2)]
    //// Register a sample tool window visible only when the provider is active
    //[MsVsShell.ProvideToolWindow(typeof(SccProviderToolWindow))]
    //[MsVsShell.ProvideToolWindowVisibility(typeof(SccProviderToolWindow), "8358dd60-00b0-478a-83b8-ea8ae3ecdaa2")]
    // Register the source control provider's service (implementing IVsScciProvider interface)
    [MsVsShell.ProvideService(typeof(SccProviderService), ServiceName = "VS2P4 Source Control Provider Service")]
    // Register the source control provider to be visible in Tools/Options/VS2P4SourceControl/Plugin dropdown selector
    [@ProvideSourceControlProvider("VS2P4 Source Control Provider", "#100")]
    // Pre-load the package when the command UI context is asserted (the provider will be automatically loaded after restarting the shell if it was active last time the shell was shutdown)
    [MsVsShell.ProvideAutoLoad("8358dd60-00b0-478a-83b8-ea8ae3ecdaa2")]
    // Register the key used for persisting solution properties, so the IDE will know to load the source control package when opening a controlled solution containing properties written by this package
    [ProvideSolutionProps(_strSolutionPersistanceKey)]
    [Guid(GuidList.guidVS2P4PkgString)]
    public sealed class VS2P4Package : Package,
        IOleCommandTarget
    {
        // The service provider implemented by the package
        private SccProviderService _sccService;
        // The name of this provider (to be written in solution and project files)
        // As a best practice, to be sure the provider has an unique name, a guid like the provider guid can be used as a part of the name
        const string _strProviderName = "VS2P4 Source Control Provider:{" + GuidList.guidVS2P4PkgString + "}";
        // The name of the solution section used to persist provider options (should be unique)
        private const string _strSolutionPersistanceKey = "VS2P4SourceControlProviderSolutionProperties";
        // The guid of solution folders
        private readonly Guid guidSolutionFolderProject = new Guid(0x2150e333, 0x8fdc, 0x42a3, 0x94, 0x74, 0x1a, 0x39, 0x56, 0xd4, 0x6d, 0xe8);


        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VS2P4Package()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));

            // The provider implements the IVsPersistSolutionProps interface which is derived from IVsPersistSolutionOpts,
            // The base class MsVsShell.Package also implements IVsPersistSolutionOpts, so we're overriding its functionality
            // Therefore, to persist user options in the suo file we will not use the set of AddOptionKey/OnLoadOptions/OnSaveOptions 
            // set of functions, but instead we'll use the IVsPersistSolutionProps functions directly.
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        public new Object GetService(Type serviceType)
        {
            return base.GetService(serviceType);
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Proffer the source control service implemented by the provider
            _sccService = new SccProviderService(this);
            ((IServiceContainer)this).AddService(typeof(SccProviderService), _sccService, true);

            // Add our command handlers (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                //// ToolWindow Command
                //var cmd = new CommandID(GuidList.guidVS2P4CmdSet, CommandId.icmdViewToolWindow);
                //var menuCmd = new MenuCommand(new EventHandler(Exec_icmdViewToolWindow), cmd);
                //mcs.AddCommand(menuCmd);

                //// ToolWindow's ToolBar Command
                //cmd = new CommandID(GuidList.guidVS2P4CmdSet, CommandId.icmdToolWindowToolbarCommand);
                //menuCmd = new MenuCommand(new EventHandler(Exec_icmdToolWindowToolbarCommand), cmd);
                //mcs.AddCommand(menuCmd);

                // Source control menu commmads
                CommandID cmd = new CommandID(GuidList.guidVS2P4CmdSet, CommandId.icmdCheckout);
                MenuCommand menuCmd = new MenuCommand(new EventHandler(Exec_icmdCheckout), cmd);
                mcs.AddCommand(menuCmd);

                cmd = new CommandID(GuidList.guidVS2P4CmdSet, CommandId.icmdMarkForAdd);
                menuCmd = new MenuCommand(new EventHandler(Exec_icmdMarkForAdd), cmd);
                mcs.AddCommand(menuCmd);

                cmd = new CommandID(GuidList.guidVS2P4CmdSet, CommandId.icmdRevertIfUnchanged);
                menuCmd = new MenuCommand(new EventHandler(Exec_icmdRevertIfUnchanged), cmd);
                mcs.AddCommand(menuCmd);

                cmd = new CommandID(GuidList.guidVS2P4CmdSet, CommandId.icmdRevert);
                menuCmd = new MenuCommand(new EventHandler(Exec_icmdRevert), cmd);
                mcs.AddCommand(menuCmd);

                cmd = new CommandID(GuidList.guidVS2P4CmdSet, CommandId.icmdGetLatestRevison);
                menuCmd = new MenuCommand(new EventHandler(Exec_icmdGetLatestRevison), cmd);
                mcs.AddCommand(menuCmd);

                cmd = new CommandID(GuidList.guidVS2P4CmdSet, CommandId.icmdRevisionHistory);
                menuCmd = new MenuCommand(new EventHandler(Exec_icmdRevisionHistory), cmd);
                mcs.AddCommand(menuCmd);

                cmd = new CommandID(GuidList.guidVS2P4CmdSet, CommandId.icmdDiff);
                menuCmd = new MenuCommand(new EventHandler(Exec_icmdDiff), cmd);
                mcs.AddCommand(menuCmd);

                cmd = new CommandID(GuidList.guidVS2P4CmdSet, CommandId.icmdTimeLapse);
                menuCmd = new MenuCommand(new EventHandler(Exec_icmdTimeLapse), cmd);
                mcs.AddCommand(menuCmd);

                cmd = new CommandID(GuidList.guidVS2P4CmdSet, CommandId.icmdRefresh);
                menuCmd = new MenuCommand(new EventHandler(Exec_icmdRefresh), cmd);
                mcs.AddCommand(menuCmd);

            }

            // Register the provider with the source control manager
            // If the package is to become active, this will also callback on OnActiveStateChange and the menu commands will be enabled
            var rscp = (IVsRegisterScciProvider)GetService(typeof(IVsRegisterScciProvider));
            rscp.RegisterSourceControlProvider(GuidList.guidSccProvider);
        }

        protected override void Dispose(bool disposing)
        {
            Trace.WriteLine(String.Format(CultureInfo.CurrentUICulture, "Entering Dispose() of: {0}", this.ToString()));

            _sccService.Dispose();

            base.Dispose(disposing);
        }

        // This function is called by the IVsSccProvider service implementation when the active state of the provider changes
        // If the package needs to refresh UI or perform other tasks, this is a good place to add the code
        public void OnActiveStateChange()
        {
        }

        // Returns the name of the source control provider
        public string ProviderName
        {
            get { return _strProviderName; }
        }


        #endregion


        #region Source Control Command Enabling

        /// <summary>
        /// Shameful hack for unit testing.
        /// Don't consider the cache when unit testing.
        /// </summary>
        public bool IsUnitTesting;

        /// <summary>
        /// The shell calls this function to know if a menu item should be visible and
        /// if it should be enabled/disabled.
        /// Note that this function will only be called when an instance of this editor
        /// is open.
        /// </summary>
        /// <param name="guidCmdGroup">Guid describing which set of command the current command(s) belong to</param>
        /// <param name="cCmds">Number of command which status are being asked for</param>
        /// <param name="prgCmds">Information for each command</param>
        /// <param name="pCmdText">Used to dynamically change the command text</param>
        /// <returns>HRESULT</returns>
        public int QueryStatus(ref Guid guidCmdGroup, uint cCmds, OLECMD[] prgCmds, System.IntPtr pCmdText)
        {
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();

            Debug.Assert(cCmds == 1, "Multiple commands");
            Debug.Assert(prgCmds != null, "NULL argument");

            // Filter out commands that are not defined by this package
            if (guidCmdGroup != GuidList.guidVS2P4CmdSet)
            {
                return (int)(Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED); ;
            }

            OLECMDF cmdf = OLECMDF.OLECMDF_SUPPORTED;

            // All source control commands needs to be hidden and disabled when the provider is not active
            // And we wait until the cache is current
            if (!_sccService.IsActive || (!IsUnitTesting && !_sccService.IsCacheCurrent()))
            {
                cmdf = cmdf | OLECMDF.OLECMDF_INVISIBLE;
                cmdf = cmdf & ~(OLECMDF.OLECMDF_ENABLED);

                prgCmds[0].cmdf = (uint)cmdf;
                return VSConstants.S_OK;
            }

            // Process our Commands
            switch (prgCmds[0].cmdID)
            {
                case CommandId.icmdCheckout:
                    cmdf |= QueryStatus_icmdCheckout();
                    break;

                case CommandId.icmdMarkForAdd:
                    cmdf |= QueryStatus_icmdMarkForAdd();
                    break;

                case CommandId.icmdRevertIfUnchanged:
                    cmdf |= QueryStatus_icmdRevertIfUnchanged();
                    break;

                case CommandId.icmdRevert:
                    cmdf |= QueryStatus_icmdRevert();
                    break;

                case CommandId.icmdGetLatestRevison:
                    cmdf |= QueryStatus_icmdGetLatestRevison();
                    break;

                case CommandId.icmdRevisionHistory:
                    cmdf |= QueryStatus_icmdRevisionHistory();
                    break;

                case CommandId.icmdDiff:
                    cmdf |= QueryStatus_icmdDiff();
                    break;

                case CommandId.icmdTimeLapse:
                    cmdf |= QueryStatus_icmdTimeLapse();
                    break;

                case CommandId.icmdRefresh:
                    cmdf |= QueryStatus_icmdRefresh();
                    break;

                default:
                    return (int)(Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED);
            }

            prgCmds[0].cmdf = (uint)cmdf;

            //stopwatch.Stop();
            //if (stopwatch.ElapsedTicks > 10)
            //{
            //    Log.Debug(String.Format("QueryStatus() took {0} msec", stopwatch.ElapsedMilliseconds));
            //}

            return VSConstants.S_OK;
        }

        OLECMDF QueryStatus_icmdCheckout()
        {
            if (!_sccService.IsSolutionLoaded)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            if (!_sccService.Options.IsCheckoutEnabled)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            VsSelection selection = GetSelection();
            return selection.FileNames.Any(file => _sccService.IsEligibleForCheckOut(file)) ? OLECMDF.OLECMDF_ENABLED : OLECMDF.OLECMDF_SUPPORTED;
        }

        OLECMDF QueryStatus_icmdMarkForAdd()
        {
            if (!_sccService.IsSolutionLoaded)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            if (!_sccService.Options.IsAddEnabled)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            VsSelection selection = GetSelection();
            return selection.FileNames.Any(file => _sccService.IsEligibleForAdd(file)) ? OLECMDF.OLECMDF_ENABLED : OLECMDF.OLECMDF_SUPPORTED;
        }

        OLECMDF QueryStatus_icmdRevertIfUnchanged()
        {
            if (!_sccService.IsSolutionLoaded)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            if (!_sccService.Options.IsRevertIfUnchangedEnabled)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            VsSelection selection = GetSelection();
            return selection.FileNames.Any(file => _sccService.IsEligibleForRevertIfUnchanged(file)) ? OLECMDF.OLECMDF_ENABLED : OLECMDF.OLECMDF_SUPPORTED;
        }

        OLECMDF QueryStatus_icmdRevert()
        {
            if (!_sccService.IsSolutionLoaded)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            if (!_sccService.Options.IsRevertEnabled)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            VsSelection selection = GetSelection();
            return selection.FileNames.Any(file => _sccService.IsEligibleForRevert(file)) ? OLECMDF.OLECMDF_ENABLED : OLECMDF.OLECMDF_SUPPORTED;
        }

        OLECMDF QueryStatus_icmdGetLatestRevison()
        {
            if (!_sccService.IsSolutionLoaded)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            if (!_sccService.Options.IsGetLatestRevisionEnabled)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }
            
            VsSelection selection = GetSelection();
            return selection.FileNames.Any(file => _sccService.IsEligibleForGetLatestRevision(file)) ? OLECMDF.OLECMDF_ENABLED : OLECMDF.OLECMDF_SUPPORTED;
        }

        OLECMDF QueryStatus_icmdRevisionHistory()
        {
            if (!_sccService.IsSolutionLoaded)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            if (!_sccService.Options.IsViewRevisionHistoryEnabled)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }
            
            VsSelection selection = GetSelection();
            return selection.FileNames.Any(file => _sccService.IsEligibleForRevisionHistory(file)) ? OLECMDF.OLECMDF_ENABLED : OLECMDF.OLECMDF_SUPPORTED;
        }

        OLECMDF QueryStatus_icmdDiff()
        {
            if (!_sccService.IsSolutionLoaded)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            if (!_sccService.Options.IsViewDiffEnabled)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            VsSelection selection = GetSelection();
            return selection.FileNames.Any(file => _sccService.IsEligibleForDiff(file)) ? OLECMDF.OLECMDF_ENABLED : OLECMDF.OLECMDF_SUPPORTED;
        }

        OLECMDF QueryStatus_icmdTimeLapse()
        {
            if (!_sccService.IsSolutionLoaded)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            if (!_sccService.Options.IsViewTimeLapseEnabled)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            VsSelection selection = GetSelection();
            return selection.FileNames.Any(file => _sccService.IsEligibleForTimeLapse(file)) ? OLECMDF.OLECMDF_ENABLED : OLECMDF.OLECMDF_SUPPORTED;
        }

        OLECMDF QueryStatus_icmdRefresh()
        {
            if (!_sccService.IsSolutionLoaded)
            {
                return OLECMDF.OLECMDF_INVISIBLE;
            }

            return OLECMDF.OLECMDF_ENABLED;
        }

        #endregion

        #region Source Control Commands Execution

        /// <summary>
        /// Excecute a command.
        /// </summary>
        /// <param name="isCommandEnabled">The options parameter, true if this command is enabled.</param>
        /// <param name="commandName">The friendly name of this command, for logging.</param>
        /// <param name="action">The method to be executed to carry out the command.</param>
        private void ExecuteCommand(bool isCommandEnabled, string commandName, Func<VsSelection, bool> action)
        {
            // DAB: I doubt if this is necessary, but the Microsoft sample SccProvider did this. Very defensive.
            if (!_sccService.IsSolutionLoaded)
            {
                Debug.Assert(false, "No solution, so the command should have been disabled");
                return;
            }

            // DAB: I doubt if this is necessary, I'm being defensive.
            if (!isCommandEnabled)
            {
                LogCommandError(commandName);
                return;
            }

            if (!SaveSolution())
            {
                return;
            }

            VsSelection selection = GetSelection();
            action(selection);

            // All nodes and fileNames in the selection will be refreshed when P4Cache.P4CacheUpdated is thrown.
        }


        private void Exec_icmdCheckout(object sender, EventArgs e)
        {
            ExecuteCommand(_sccService.Options.IsCheckoutEnabled, Resources.Checkout, _sccService.CheckoutFiles);
        }


        private static void LogCommandError(string commandName)
        {
            Log.Error(commandName + Resources.Command_Is_Disabled);
        }

        private void Exec_icmdMarkForAdd(object sender, EventArgs e)
        {
            ExecuteCommand(_sccService.Options.IsAddEnabled, Resources.Add, _sccService.AddFiles);
        }

        private void Exec_icmdRevertIfUnchanged(object sender, EventArgs e)
        {
            ExecuteCommand(_sccService.Options.IsRevertIfUnchangedEnabled, Resources.Revert_If_Unchanged, _sccService.RevertFilesIfUnchanged);
        }

        private void Exec_icmdRevert(object sender, EventArgs e)
        {
            ExecuteCommand(_sccService.Options.IsRevertEnabled, Resources.Revert, _sccService.RevertFiles);
        }

        private void Exec_icmdGetLatestRevison(object sender, EventArgs e)
        {
            ExecuteCommand(_sccService.Options.IsGetLatestRevisionEnabled, Resources.Get_Latest_Revision, _sccService.GetLatestRevision);
        }

        private void Exec_icmdRevisionHistory(object sender, EventArgs e)
        {
            ExecuteCommand(_sccService.Options.IsViewRevisionHistoryEnabled, Resources.View_Revision_History_Report, _sccService.RevisionHistory);
        }

        private void Exec_icmdDiff(object sender, EventArgs e)
        {
            ExecuteCommand(_sccService.Options.IsViewDiffEnabled, Resources.View_Diff_Report, _sccService.Diff);
        }

        private void Exec_icmdTimeLapse(object sender, EventArgs e)
        {
            ExecuteCommand(_sccService.Options.IsViewTimeLapseEnabled, Resources.View_Time_Lapse_Report, _sccService.TimeLapse);
        }

        private void Exec_icmdRefresh(object sender, EventArgs e)
        {
            if (!_sccService.IsSolutionLoaded)
            {
                Debug.Assert(false, "No solution, so the command should have been disabled");
                return;
            }

            _sccService.Refresh(null);
        }

        #endregion

        #region Source Control Utility Functions

        /// <summary>
        /// Gets the list of directly selected VSITEMSELECTION objects
        /// </summary>
        /// <returns>A list of VSITEMSELECTION objects</returns>
        private IList<VSITEMSELECTION> GetSelectedNodes()
        {
            // Retrieve shell interface in order to get current selection
            var monitorSelection = GetService(typeof(IVsMonitorSelection)) as IVsMonitorSelection;
            Debug.Assert(monitorSelection != null, "Could not get the IVsMonitorSelection object from the services exposed by this project");
            if (monitorSelection == null)
            {
                throw new InvalidOperationException();
            }

            var selectedNodes = new List<VSITEMSELECTION>();
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainer = IntPtr.Zero;
            try
            {
                // Get the current project hierarchy, project item, and selection container for the current selection
                // If the selection spans multiple hierachies, hierarchyPtr is Zero
                uint itemid;
                IVsMultiItemSelect multiItemSelect;
                ErrorHandler.ThrowOnFailure(monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainer));

                if (itemid != VSConstants.VSITEMID_SELECTION)
                {
                    // We only care if there are nodes selected in the tree
                    if (itemid != VSConstants.VSITEMID_NIL)
                    {
                        if (hierarchyPtr == IntPtr.Zero)
                        {
                            // Solution is selected
                            // In this case we want to return all nodes in the solution
                            return GetSolutionNodes();

                            //VSITEMSELECTION vsItemSelection;
                            //vsItemSelection.pHier = null;
                            //vsItemSelection.itemid = itemid;
                            //selectedNodes.Add(vsItemSelection);
                        }
                        else
                        {
                            var hierarchy = (IVsHierarchy)Marshal.GetObjectForIUnknown(hierarchyPtr);

                            // Single item selection
                            VSITEMSELECTION vsItemSelection;
                            vsItemSelection.pHier = hierarchy;
                            vsItemSelection.itemid = itemid;
                            selectedNodes.Add(vsItemSelection);
                        }
                    }
                }
                else
                {
                    if (multiItemSelect != null)
                    {
                        // This is a multiple item selection.

                        //Get number of items selected and also determine if the items are located in more than one hierarchy
                        uint numberOfSelectedItems;
                        int isSingleHierarchyInt;
                        ErrorHandler.ThrowOnFailure(multiItemSelect.GetSelectionInfo(out numberOfSelectedItems, out isSingleHierarchyInt));

                        // Now loop all selected items and add them to the list 
                        Debug.Assert(numberOfSelectedItems > 0, "Bad number of selected itemd");
                        if (numberOfSelectedItems > 0)
                        {
                            var vsItemSelections = new VSITEMSELECTION[numberOfSelectedItems];
                            ErrorHandler.ThrowOnFailure(multiItemSelect.GetSelectedItems(0, numberOfSelectedItems, vsItemSelections));
                            foreach (VSITEMSELECTION vsItemSelection in vsItemSelections)
                            {
                                selectedNodes.Add(vsItemSelection);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
                if (selectionContainer != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainer);
                }
            }

            return selectedNodes;
        }

        /// <summary>
        /// Returns the selection
        /// </summary>
        /// <returns></returns>
        private VsSelection GetSelection()
        {
            IList<VSITEMSELECTION> selectedNodes = GetSelectedNodes();

            // now look in the rest of selection and accumulate scc files
            return GetSelection(selectedNodes);
        }


        //------------------------------------------------------------------
        private bool GetSelectedItemFileName(IVsSccProject2 pscp2,
            VSITEMSELECTION selected_item, out string filename)
        {
            // special or uncontrolled file
            var project = (IVsProject)pscp2;
            string bstrMKDocument;

            if (project.GetMkDocument(selected_item.itemid, out bstrMKDocument) == VSConstants.S_OK
                && !string.IsNullOrEmpty(bstrMKDocument))
            {
                object prop;
                var res = selected_item.pHier.GetProperty(selected_item.itemid, (int)__VSHPROPID.VSHPROPID_Parent, out prop);
                if (res == VSConstants.S_OK)
                {
                    Log.Debug(string.Format("ParentId: {0}", prop));
                }

                uint parent_id = (uint)((int)prop);
                IList<string> files = GetNodeFiles(pscp2, parent_id);
                if (files.Count != 0)
                {
                    foreach (var f in files)
                    {
                        if (f == bstrMKDocument)
                        {
                            filename = f;
                            return true;
                        }
                    }
                }
            }

            filename = null;
            return false;
        }

        public VsSelection GetSolutionSelection()
        {
            IList<VSITEMSELECTION> nodes = GetSolutionNodes();
            return GetSelection(nodes);
        }

        /// <summary>
        /// Return the VsSelection which includes both nodes and the files corresponding to nodes (recursive).
        /// </summary>
        /// <param name="nodes">The nodes we're checking.</param>
        /// <returns>the VsSelection which includes both nodes and the files corresponding to nodes (recursive).</returns>
        internal VsSelection GetSelection(IList<VSITEMSELECTION> nodes)
        {
            List<string> fileNames = new List<string>();
            foreach (VSITEMSELECTION vsItemSel in nodes)
            {
                IVsSccProject2 pscp2 = vsItemSel.pHier as IVsSccProject2;
                if (pscp2 == null)
                {
                    // solution case
                    var solutionFileName = GetSolutionFileName();
                    if (solutionFileName != null)
                    {
                        fileNames.Add(solutionFileName);
                    }
                }
                else
                {
                    IList<string> projectFiles = GetProjectFiles(pscp2, vsItemSel.itemid);
                    fileNames.AddRange(projectFiles);
                }
            }

            return new VsSelection(fileNames, nodes);
        }

        /// <summary>
        /// Returns a list of source controllable files associated with the specified node
        /// </summary>
        public IList<string> GetNodeFiles(IVsHierarchy hier, uint itemid)
        {
            var pscp2 = hier as IVsSccProject2;
            return GetNodeFiles(pscp2, itemid);
        }

        /// <summary>
        /// Returns a list of source controllable files associated with the specified node
        /// </summary>
        private static IList<string> GetNodeFiles(IVsSccProject2 pscp2, uint itemid)
        {
            // NOTE: the function returns only a list of files, containing both regular files and special files
            // If you want to hide the special files (similar with solution explorer), you may need to return 
            // the special files in a hashtable (key=master_file, values=special_file_list)

            // Initialize output parameters
            IList<string> sccFiles = new List<string>();
            if (pscp2 != null)
            {
                var pathStr = new CALPOLESTR[1];
                var flags = new CADWORD[1];

                if (pscp2.GetSccFiles(itemid, pathStr, flags) == 0)
                {
                    uint arraySize = pathStr[0].cElems;
                    IntPtr arrayPtr = pathStr[0].pElems;
                    for (int elemIndex = 0; elemIndex < arraySize; elemIndex++)
                    {
                        IntPtr pathIntPtr = Marshal.ReadIntPtr(arrayPtr, elemIndex * IntPtr.Size);
                        String path;
                        try
                        {
                            path = Marshal.PtrToStringAuto(pathIntPtr);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("In GetNodeFiles: " + ex.Message);
                            continue;
                        }

                        //Log.Debug(string.Format("Regular file: {0}", path)); does this continually for selected file
                        sccFiles.Add(path);

                        // See if there are special files
                        uint flagsArraySize = flags[0].cElems;
                        IntPtr flagsPtr = flags[0].pElems;
                        if (flags.Length > 0 && flagsArraySize > 0)
                        {
                            int flag = Marshal.ReadInt32(flagsPtr, elemIndex);

                            if (flag != 0)
                            {
                                // We have special files
                                var specialFiles = new CALPOLESTR[1];
                                var specialFlags = new CADWORD[1];

                                pscp2.GetSccSpecialFiles(itemid, path, specialFiles, specialFlags);
                                for (int i = 0; i < specialFiles[0].cElems; i++)
                                {
                                    IntPtr specialPathIntPtr = Marshal.ReadIntPtr(specialFiles[0].pElems, i*IntPtr.Size);
                                    String specialPath = Marshal.PtrToStringAuto(specialPathIntPtr);

                                    //Log.Debug(string.Format("Special file: {0}", path));
                                    sccFiles.Add(specialPath);
                                    Marshal.FreeCoTaskMem(specialPathIntPtr);
                                }

                                if (specialFiles[0].cElems > 0)
                                {
                                    Marshal.FreeCoTaskMem(specialFiles[0].pElems);
                                }
                            }
                        }

                        Marshal.FreeCoTaskMem(pathIntPtr);
                    }
                    if (arraySize > 0)
                    {
                        Marshal.FreeCoTaskMem(arrayPtr);
                    }
                }
            }

            return sccFiles;
        }

        /// <summary>
        /// Refresh the source control glyphs for all files in the solution.
        /// </summary>
        public void RefreshSolutionGlyphs()
        {
            IList<VSITEMSELECTION> nodes = GetSolutionNodes();
            RefreshNodesGlyphs(nodes);
        }

        /// <summary>
        /// Thanks to HgSccPackage (Sergey Antonov) for this improvement, to allow multiple nodes to be refreshed in one call.
        /// A huge performance benefit for very-large solutions (1000's of files)
        /// </summary>
        class GlyphsToUpdate
        {
            public GlyphsToUpdate()
            {
                SccFiles = new List<string>();
                Glyphs = new List<VsStateIcon>();
                SccStatus = new List<uint>();
                AffectedNodes = new List<uint>();
            }

            public List<string> SccFiles { get; private set; }
            public List<VsStateIcon> Glyphs { get; private set; }
            public List<uint> SccStatus { get; private set; }
            public List<uint> AffectedNodes { get; private set; }
        }

        /// <summary>
        /// Refreshes the glyphs of the specified hierarchy nodes
        /// </summary>
        public void RefreshNodesGlyphs(IList<VSITEMSELECTION> selectedNodes)
        {
            var map = new Dictionary<IVsSccProject2, GlyphsToUpdate>();
            foreach (VSITEMSELECTION vsItemSel in selectedNodes)
            {
                var sccProject2 = vsItemSel.pHier as IVsSccProject2;
                if (vsItemSel.itemid == VSConstants.VSITEMID_ROOT)
                {
                    if (sccProject2 == null)
                    {
                        // Note: The solution's hierarchy does not implement IVsSccProject2, IVsSccProject interfaces
                        // It may be a pain to treat the solution as special case everywhere; a possible workaround is 
                        // to implement a solution-wrapper class, that will implement IVsSccProject2, IVsSccProject and
                        // IVsHierarhcy interfaces, and that could be used in provider's code wherever a solution is needed.
                        // This approach could unify the treatment of solution and projects in the provider's code.

                        // Until then, solution is treated as special case
                        var rgpszFullPaths = new string[1];
                        rgpszFullPaths[0] = GetSolutionFileName();
                        var rgsiGlyphs = new VsStateIcon[1];
                        var rgdwSccStatus = new uint[1];
                        _sccService.GetSccGlyph(1, rgpszFullPaths, rgsiGlyphs, rgdwSccStatus);

                        // Set the solution's glyph directly in the hierarchy
                        var solHier = (IVsHierarchy)GetService(typeof(SVsSolution));
                        solHier.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_StateIconIndex, rgsiGlyphs[0]);
                    }
                    else
                    {
                        // Refresh all the glyphs in the project; the project will call back GetSccGlyphs() 
                        // with the files for each node that will need new glyph
                        var projectFileName = GetProjectFileName(sccProject2);
                        var sw = new Stopwatch();
                        sw.Start();
                        Log.Debug(string.Format("Starting to update all glyphs for entire project (on UI thread) {0}", projectFileName));

                        // With thousands of files in a file, this is horribly slow.
                        //sccProject2.SccGlyphChanged(0, null, null, null);

                        BuildUpdateInfo(map, vsItemSel, sccProject2);
                        sw.Stop();
                        Log.Debug(string.Format("Done updating all glyphs for entire project (on UI thread) {0}, took {1} msec", projectFileName, sw.ElapsedMilliseconds));
                    }
                }
                else
                {
                    BuildUpdateInfo(map, vsItemSel, sccProject2);
                }
            }
            foreach (var project in map.Keys)
            {
                var glyphs = map[project];
                if (glyphs.SccFiles.Count > 0)
                {
                    project.SccGlyphChanged(
                        glyphs.SccFiles.Count,
                        glyphs.AffectedNodes.ToArray(),
                        glyphs.Glyphs.ToArray(),
                        glyphs.SccStatus.ToArray());
                }
            }
        }

        /// <summary>
        /// Build a map of GlyphsToUpdate information
        /// </summary>
        /// <param name="map"></param>
        /// <param name="vsItemSel"></param>
        /// <param name="sccProject2"></param>
        private void BuildUpdateInfo(Dictionary<IVsSccProject2, GlyphsToUpdate> map, VSITEMSELECTION vsItemSel, IVsSccProject2 sccProject2)
        {
            // Thanks to HgSccPackage (Sergey Antonov) for this improvement, to allow multiple nodes to be refreshed in one call.
            // A huge performance benefit for very-large solutions (1000's of files)
            GlyphsToUpdate glyphs = null;
            if (!map.TryGetValue(sccProject2, out glyphs))
            {
                glyphs = new GlyphsToUpdate();
                map.Add(sccProject2, glyphs);
            }
            IList<string> sccFiles;
            if (vsItemSel.itemid == VSConstants.VSITEMID_ROOT)
            {
                // Update all the files at or under this project
                sccFiles = GetProjectFiles(sccProject2, vsItemSel.itemid);
            }
            else
            {
                // Update only the files at or under this node, e.g. Form1.cs and Form1.designer.cs
                sccFiles = GetNodeFiles(sccProject2, vsItemSel.itemid);
            }
            if (sccFiles.Count == 0)
            {
                string filename;
                if (GetSelectedItemFileName(sccProject2, vsItemSel, out filename))
                    sccFiles.Add(filename);
            }

            if (sccFiles.Count > 0)
            {
                string[] rgpszFullPaths = new string[sccFiles.Count];
                for (int i = 0; i < sccFiles.Count; ++i)
                    rgpszFullPaths[i] = sccFiles[i];

                VsStateIcon[] rgsiGlyphs = new VsStateIcon[sccFiles.Count];
                uint[] rgdwSccStatus = new uint[sccFiles.Count];
                _sccService.GetSccGlyph(sccFiles.Count, rgpszFullPaths, rgsiGlyphs, rgdwSccStatus);

                uint[] rguiAffectedNodes = new uint[sccFiles.Count];
                IList<uint> subnodes = GetProjectItems(vsItemSel.pHier, vsItemSel.itemid);
                if (subnodes.Count != sccFiles.Count)
                {
                    //Log.Debug("RefreshNodeGlyphs: subnodes.Count != sccFiles.Count");
                    //for (int i = 0; i < sccFiles.Count; ++i)
                    //    Log.Debug(string.Format("[{0}]: {1}", i, sccFiles[i]));

                    var dict = new Dictionary<string, uint>();
                    var proj = vsItemSel.pHier as IVsProject2;

                    foreach (var id in subnodes)
                    {
                        string docname;
                        var res = proj.GetMkDocument(id, out docname);

                        if (res == VSConstants.S_OK && !string.IsNullOrEmpty(docname))
                            dict[docname] = id;
                    }

                    for (int i = 0; i < sccFiles.Count; ++i)
                    {
                        uint id;
                        if (dict.TryGetValue(sccFiles[i], out id))
                        {
                            rguiAffectedNodes[i] = id;
                        }
                        else
                        {
                            Log.Debug(string.Format("Error: Unable to map id<->filename: {0}", sccFiles[i]));
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < sccFiles.Count; ++i)
                    {
                        rguiAffectedNodes[i] = subnodes[i];
                    }
                }

                glyphs.SccFiles.AddRange(sccFiles);
                glyphs.Glyphs.AddRange(rgsiGlyphs);
                glyphs.SccStatus.AddRange(rgdwSccStatus);
                glyphs.AffectedNodes.AddRange(rguiAffectedNodes);
            }
        }

        /// <summary>
        /// Get all the nodes in the solution.
        /// </summary>
        /// <returns>all the nodes in the solution.</returns>
        public IList<VSITEMSELECTION> GetSolutionNodes()
        {
            IList<VSITEMSELECTION> nodes = new List<VSITEMSELECTION>();
            VSITEMSELECTION vsItem;

            // Add the solution
            vsItem.itemid = VSConstants.VSITEMID_ROOT;
            vsItem.pHier = null;
            nodes.Add(vsItem);

            // Add any solution folders
            Hashtable enumSolFolders = GetSolutionFoldersEnum();
            foreach (IVsHierarchy pHier in enumSolFolders.Keys)
            {
                vsItem.itemid = VSConstants.VSITEMID_ROOT;
                vsItem.pHier = pHier;
                nodes.Add(vsItem);
            }
            return nodes;
        }

        /// <summary>
        /// Save all files in the solution.
        /// </summary>
        /// <returns>false if saving the files failed.</returns>
        internal bool SaveSolution()
        {
            var sol = (IVsSolution)GetService(typeof(SVsSolution));
            if (sol.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty, null, 0) != VSConstants.S_OK)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the filename of the solution, or null if no solution has been loaded.
        /// </summary>
        /// <returns> the filename of the solution, or null if no solution has been loaded.</returns>
        public string GetSolutionFileName()
        {
            var sol = (IVsSolution)GetService(typeof(SVsSolution));
            string solutionDirectory, solutionFile, solutionUserOptions;
            if (sol.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionUserOptions) == VSConstants.S_OK)
            {
                return solutionFile;
            }

            return null;
        }

   

        public string GetProjectFileName(IVsHierarchy pHier)
        {
            if (!(pHier is IVsSccProject2)) 
            {
                return GetSolutionFileName();
            }

            var files = GetNodeFiles(pHier as IVsSccProject2, VSConstants.VSITEMID_ROOT);
            return files.Count <= 0 ? null : files[0];
        }


        /// <summary>
        /// Returns the filename of the specified controllable project 
        /// </summary>
        public string GetProjectFileName(IVsSccProject2 pscp2Project)
        {
            // Note: Solution folders return currently a name like "NewFolder1{1DBFFC2F-6E27-465A-A16A-1AECEA0B2F7E}.storage"
            // Your provider may consider returning the solution file as the project name for the solution, if it has to persist some properties in the "project file"
            // UNDONE: What to return for web projects? They return a folder name, not a filename! Consider returning a pseudo-project filename instead of folder.

            var hierProject = (IVsHierarchy)pscp2Project;
            var project = (IVsProject)pscp2Project;

            // Attempt to get first the filename controlled by the root node 
            IList<string> sccFiles = GetNodeFiles(pscp2Project, VSConstants.VSITEMID_ROOT);
            if (sccFiles.Count > 0 && sccFiles[0] != null && sccFiles[0].Length > 0)
            {
                return sccFiles[0];
            }

            // If that failed, attempt to get a name from the IVsProject interface
            string bstrMKDocument;
            if (project.GetMkDocument(VSConstants.VSITEMID_ROOT, out bstrMKDocument) == VSConstants.S_OK &&
                !string.IsNullOrEmpty(bstrMKDocument))
            {
                return bstrMKDocument;
            }

            // If that fails, attempt to get the filename from the solution
            var sol = (IVsSolution)GetService(typeof(SVsSolution));
            string uniqueName;
            if (sol.GetUniqueNameOfProject(hierProject, out uniqueName) == VSConstants.S_OK &&
                !string.IsNullOrEmpty(uniqueName))
            {
                // uniqueName may be a full-path or may be relative to the solution's folder
                if (uniqueName.Length > 2 && uniqueName[1] == ':')
                {
                    return uniqueName;
                }

                // try to get the solution's folder and relativize the project name to it
                string solutionDirectory, solutionFile, solutionUserOptions;
                if (sol.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionUserOptions) == VSConstants.S_OK)
                {
                    uniqueName = solutionDirectory + "\\" + uniqueName;

                    // UNDONE: eliminate possible "..\\.." from path
                    return uniqueName;
                }
            }

            // If that failed, attempt to get the project name from 
            string bstrName;
            if (hierProject.GetCanonicalName(VSConstants.VSITEMID_ROOT, out bstrName) == VSConstants.S_OK)
            {
                return bstrName;
            }

            // if everything we tried fail, return null string
            return null;
        }

        private static void DebugWalkingNode(IVsHierarchy pHier, uint itemid)
        {
#if DEBUG
            //object property = null;
            //if (pHier.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_Name, out property) == VSConstants.S_OK)
            //{
            //    Trace.WriteLine(String.Format(CultureInfo.CurrentUICulture, "Walking hierarchy node: {0}", (string)property));
            //}
#endif
        }

        /// <summary>
        /// Gets the list of ItemIDs that are nodes in the specified project
        /// </summary>
        private IList<uint> GetProjectItems(IVsHierarchy pHier)
        {
            // Start with the project root and walk all expandable nodes in the project
            return GetProjectItems(pHier, VSConstants.VSITEMID_ROOT);
        }

        /// <summary>
        /// Gets the list of ItemIDs that are nodes in the specified project, starting with the specified item
        /// </summary>
        private static IList<uint> GetProjectItems(IVsHierarchy pHier, uint startItemid)
        {
            var projectNodes = new List<uint>();

            // The method does a breadth-first traversal of the project's hierarchy tree
            var nodesToWalk = new Queue<uint>();
            nodesToWalk.Enqueue(startItemid);

            while (nodesToWalk.Count > 0)
            {
                uint node = nodesToWalk.Dequeue();
                projectNodes.Add(node);

                DebugWalkingNode(pHier, node);

                object firstChildProperty;
                if (pHier.GetProperty(node, (int)__VSHPROPID.VSHPROPID_FirstChild, out firstChildProperty) == VSConstants.S_OK)
                {
                    var childnode = (uint)(int)firstChildProperty;
                    if (childnode == VSConstants.VSITEMID_NIL)
                    {
                        continue;
                    }

                    DebugWalkingNode(pHier, childnode);

                    object expandableProperty;
                    object containerProperty;
                    if ((pHier.GetProperty(childnode, (int)__VSHPROPID.VSHPROPID_Expandable, out expandableProperty) == VSConstants.S_OK && ((expandableProperty is bool) ? (bool)expandableProperty : (int)expandableProperty != 0 )) ||
                        (pHier.GetProperty(childnode, (int)__VSHPROPID2.VSHPROPID_Container, out containerProperty) == VSConstants.S_OK && (containerProperty != null && (bool)containerProperty)))
                    {
                        nodesToWalk.Enqueue(childnode);
                    }
                    else
                    {
                        projectNodes.Add(childnode);
                    }
                    object nextSiblingProperty;
                    while (pHier.GetProperty(childnode, (int)__VSHPROPID.VSHPROPID_NextSibling, out nextSiblingProperty) == VSConstants.S_OK)
                    {
                        childnode = (uint)(int)nextSiblingProperty;
                        if (childnode == VSConstants.VSITEMID_NIL)
                        {
                            break;
                        }

                        DebugWalkingNode(pHier, childnode);
                        bool isExpandableOK = pHier.GetProperty(childnode, (int)__VSHPROPID.VSHPROPID_Expandable, out expandableProperty) == VSConstants.S_OK;
                        bool isContainerOK = pHier.GetProperty(childnode, (int)__VSHPROPID2.VSHPROPID_Container, out containerProperty) == VSConstants.S_OK;

                        // Seems to be a VS issue? Sometimes expandableProerty comes back bool, usually it's int
                        int expandablePropertyInt; // = (int)expandableProperty;
                        bool expandablePropertyBool; // = expandablePropertyInt != 0;
                        if (int.TryParse(expandableProperty.ToString(), out expandablePropertyInt))
                        {
                            expandablePropertyBool = expandablePropertyInt != 0;
                        }
                        else
                        {
                            bool.TryParse(expandableProperty.ToString(), out expandablePropertyBool);
                        }

                        bool containerPropertyBool = containerProperty == null ? false : (bool)containerProperty;
                        if ((isExpandableOK && expandablePropertyBool) ||
                            (isContainerOK && containerPropertyBool))
                        {
                            nodesToWalk.Enqueue(childnode);
                        }
                        else
                        {
                            projectNodes.Add(childnode);
                        }
                    }
                }

            }

            return projectNodes;
        }

        /// <summary>
        /// Gets the list of source controllable files in the specified project
        /// </summary>
        public IList<string> GetProjectFiles(IVsSccProject2 pscp2Project, uint startItemId)
        {
            IList<string> projectFiles = new List<string>();
            var hierProject = (IVsHierarchy)pscp2Project;
            IList<uint> projectItems = GetProjectItems(hierProject, startItemId);

            for (int i = 0; i < projectItems.Count; i++)
            {
                uint itemid = projectItems[i];
                IList<string> sccFiles = GetNodeFiles(pscp2Project, itemid);
                foreach (string file in sccFiles)
                {
                    //Log.Information(string.Format("Found project file: {0}", file));
                    projectFiles.Add(file);
                }
            }

            return projectFiles;
        }

   
        /// <summary>
        /// Checks whether the provider is invoked in command line mode
        /// </summary>
        public bool InCommandLineMode()
        {
            var shell = (IVsShell)GetService(typeof(SVsShell));
            object pvar;
            if (shell.GetProperty((int)__VSSPROPID.VSSPROPID_IsInCommandLineMode, out pvar) == VSConstants.S_OK &&
                (bool)pvar)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether the specified project is a solution folder
        /// </summary>
        public bool IsSolutionFolderProject(IVsHierarchy pHier)
        {
            var pFileFormat = pHier as IPersistFileFormat;
            if (pFileFormat != null)
            {
                Guid guidClassID;
                if (pFileFormat.GetClassID(out guidClassID) == VSConstants.S_OK &&
                    guidClassID.CompareTo(guidSolutionFolderProject) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a list of solution folders projects in the solution.
        /// (DAB: This actually gets all projects loaded in the solution, including solution folders.)
        /// </summary>
        public Hashtable GetSolutionFoldersEnum()
        {
            var mapHierarchies = new Hashtable();

            var sol = (IVsSolution)GetService(typeof(SVsSolution));
            Guid rguidEnumOnlyThisType = guidSolutionFolderProject;
            IEnumHierarchies ppenum = null;
            ErrorHandler.ThrowOnFailure(sol.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref rguidEnumOnlyThisType, out ppenum));

            var rgelt = new IVsHierarchy[1];
            uint pceltFetched = 0;
            while (ppenum.Next(1, rgelt, out pceltFetched) == VSConstants.S_OK &&
                   pceltFetched == 1)
            {
                mapHierarchies[rgelt[0]] = true;
            }

            return mapHierarchies;
        }

        #endregion
    }
}
