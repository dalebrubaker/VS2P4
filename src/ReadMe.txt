This program borrows VERY heavily from SccProviderService.sln, the Visual Studio 2005/2010 SDK example.
It also uses P4.Net as its primary interface to Perforce. Currently from public depot folder: \guest\shawn_hladky\P4.Net\release\1.0\bin\SN_CLR_2.0
Both products have open licenses, as does this one.

Documentation is at: http://vs2p4.codeplex.com/documentation

Note to myself for debugging: http://social.msdn.microsoft.com/Forums/en/vsx/thread/6f8c20a1-0762-4a7f-9220-ee0b00ae3046
Summary: 
Start Action > Start external program: "C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe "
Start Options > Command line arguments: /rootsuffix Exp

Future?
		JeffM's issue re changelists
		Plus, show changelist # on tooltip, with first part of description 

		Allow Move and Rename on files opened for Add or Renamed (like P4 does).
		Add more logging for information, warnings?
		Support VS2008?
		Support project rename?
		Support solution rename? 
		Allow optionally to put source control info in project (and solution?) files.
		Try revert if unchanged and only prompt whether to revert remaining files (files that have changed).

1.0		8/22/10
		Initial release

1.1		8/23/10
		Changes in 1.1:
			Fixed tab order in options control (known bug in VS, http://social.msdn.microsoft.com/Forums/en/vsx/thread/e01cc25c-9413-48a0-990c-bbd1d1a7aba2)
			Disable editing of the dropdown choices for Logging Level.
			Log.Error at all exceptions.			

1.2		8/27/10
		Changes in 1.2:
			Fixed problem where VS2P4 was unnecessarily checking out project files after opening a solution
			Fixed some spelling and grammar issues.
			Moved hard-coded strings into Resources.resx
			Added UseP4Config checkbox to Options.
				Checking it causes connections to be based on the environment (e.g. P4Config) instead of explicit settings.
				This allows you to connect to different Perforce servers and workspaces without changing VS2P4 Options.
				Note that the connection is based on the location of the solution file. 
				e.g. environment variable P4CONFIG might be set to p4.cfg.
				The p4.cfg file at the root of a workspace might contain:
					P4CLIENT=DALEBPC
					P4PORT=perforce:1666
					P4USER=Dale.Brubaker
			Supported SUBST drives.
			Made Perforce file state updates much faster, especially for large solutions.
		
1.3		8/27/10			
		Changes in 1.3:
			Fixed problems starting VS2P4 after a solution was loaded.

1.31	8/31/10
		Changes in 1.31
			Fixed Codeplex Issue Id #196: Tools + VS2P4 Options confirmation dialog was asking for confirmation even when no changes were made.

1.32	9/18/10
		Changes in 1.32
		Always prompt user for approval before making a file writable at Save.

1.33	9/25/10
		Changes in 1.33
		Fix problem where Exclude From Project was marking for delete in Perforce.
		Lighter-weight and more-green checked-out icon.

1.34	10/2/10
		Changes in 1.34
		When a file is not in the Perforce workspace, show the NotSet icon only for that file, and show correct icons for the other files. 

1.35	10/9/10
		Changes in 1.35
		When a file is not in the Perforce workspace, simply treat it as a file not controlled by Perforce.
		This fixes a problem where VS2P4 disallowed Rename on uncontrolled files.

1.36	12/4/10
		Changes in 1.36
		Auto-refresh all glyphs when a project is added or reloaded.
		test

1.37	2/23/11
		Changes in 1.37 (by Bill Leonard)
		Fix problems where Perforce was sometimes returning forward slashes instead of backslashes, causing VS2P4 to report the file not in Perforce.
		Generalize Unit Testing settings so that multiple developers can easily change them.
		Change File Rename to allow spaces in the pathname.
		Persist the "Use P4Config" option setting.

1.38	3/5/11
		Changes in 1.38 (by Dale Brubaker)
		Fix potential crashing when starting without a Perforce connection

1.50	9/21/11
		Changes in 1.50 (by Bill Leonard)
		Separated the options dialog into two separate dialogs.

1.51	11/9/11
		Changes in 1.51 (by Dale Brubaker)
		Fixed crashing bug (in Microsoft's sample code) involving service references and perhaps other "special" solution nodes.
		Improved speed of updating file states when a solution includes at least one file not under the Perforce Root.

1.52	11/11/11
		Changes in 1.52 (by Dale Brubaker)
		Added better thread safety and fixed duplicate file name exceptions in version 1.51 changes.
		Added timestamp to log messages.

1.53	11/15/11
		Changes in 1.53 (by Dale Brubaker)
		Allow the P4 workspace to be a virtual (SUBST) drive.
			A filename on a virtual drive is converted to the non-virtual real path for matching to Perforce, as before.
		    But if the Perforce root is a virtual drive, the client files must also be loaded from the virtual drive.
		Improve robustness for connection issues.
		Handle and log exceptions on the background thread while setting file states.
1.54	11/16/11
		Changes in 1.54 (by Dale Brubaker)
		Fix a trailing slash issue that sometimes caused a file not to appear to be under the Perforce Root
1.55	11/25/11
		Changes in 1.55 (by Dale Brubaker)
		Fix failure of Log Level option to persist.
1.60	12/2/11
		Changes in 1.60 (by Dale Brubaker)
		Fixed problem where child files (e.g. the Form1.designer.cs file under Form1.cs) were not being handled when the parent was selected.
1.70	2/3/12
		Changed P4.Net files (p4dn and p4api DLLs) to support Perforce 2011.1 stream depot files (2011.1 is the first version which supports streams).
		Accommodate cases where the Perforce root has forward slashes instead of backward slashes.
		Fixed the Move command to do a P4.Net move instead of running P4.EXE. This means that reverting either the add or the delete reverts them both.
1.71	2/4/12
		Recompiled P4.Net files with VS2010 compilers instead of VS2005 compilers (VC10 instead of VC8), because of crashing error due to side-by-side errors.
1.72	2/7/12
		Don't allow deletion of a checked-out file (opened for edit).
		Developer changes for unit testing -- use environment variables instead of Settings.
1.73	3/1/12
		Hacked the .vsix file to support Visual Studio 11
1.74	3/15/12
		Very significant performance improvements, especially for solutions with thousands of files.
		Synopsis of more significant changes:
			1. Re-wrote Map.IsFileUnderRoot() to be faster and more robust.
				Note that some people have thousands of files in a solution that are not under the Perforce root. 
				We detect those, and don't bother to ask Perforce for their file states (which is VERY slow in this case).
			2. Removed P4Cache.AddOrUpdateFiles(), the non-background request to set file states. 
				EVERY request to Perforce is now on a background thread.
			3. P4Service.SendCommand() now checks if a file is under the root; if so, it refused to do the Send.
			4. SccProviderService.GetSccGlyph(). A really major change. 
				Instead of doing 1 file at a time, we handle a batch of files at a time.
			5. Corresponding to the last point, VS2P4Package.RefreshNodesGlyphs() now sends all the nodes at once
				to GetSccGlyph().
				For example, to update an entire project the "normal" method is to do sccProject2.SccGlyphChanged(0, null, null, null); 
				Under the covers, that was doing 2 interop calls for each file in the project!
				Now we do 2 interop calls for ALL calls in the project.
				So if a solution had 3000 files under the Perforce root, we've gone from 6000 interop calls to 2.
				(If I understand it correctly.)
				The performance gain is at least one order of magnitude.
			6. As part of the last point, we can now selecting a WinForm (e.g.) and only update the status for that node,
				and we will pick up nodes under that (e.g. the .designer.cs file) as well. Before we were doing the entire project.
1.75	3/31/12
		Allow rename of a file Marked for Add as well as a file Checked Out
		Allow rename of a folder
		Made QueryStatus() a bit faster (right clicking on Solution or any other node).
			But it's still slower than when VS2P4 is not enabled, as we must check the file states to see what Context Menu items should be enabled.
		Merged in Bill Leonard's dev branch. Bill did the following:
			Fixed several timing problems and race conditions with P4Cache. Implemented a wait mechanism for waiting for the cache to update for tests.
			Implemented an XML-based mechanism for getting default settings for the Perforce connection information. This allows unit tests to have different settings 
				depending on what is being tested. Also allows the user to set some defaults for Visual Studio connections based on the solution name, or general defaults.
1.76	4/2/12
		Fixed an ArgumentNullException in Map.IsFileUnderRoot() that could occur when nothing in the solution is under source control.
1.77	4/18/12
		On Connection Options dialog, enable the Test button even when "Use P4Config instead" is checked.
		Much faster start-up for opening a new solution (in many cases was checking Perforce one file at a time).
		Fixed bug -- we weren't persisting Connection settings between sessions.
1.78	4/20/12
		Added an option on the Command Options dialog to "Ignore Files Not Under Perforce Root". 
			Checking this box will make solutions refresh faster when they have lots of files not under the Perforce root.
			But "non-standard" workspace mappings could cause VS2P4 to show files "not under Perforce control" that actually were under Perforce control.
			The default (not checked) is to rely on Perforce to determine which files are under Perforce control.
1.79	4/21/12
		Corrected "Ignore Files Not Under Perforce Root" to default to not-checked.
1.80	6/5/12
		Fixed failure to persist global options for connection options.
		Changed global option names to avoid conflicts with other packages.
1.81	6/23/12
		Speed improvement where UI might seem to hang for a bit. As a result, the context menu for a file won't show P4 options until Perforce file states have been updated.
		Allow editing of files opened for branching or integration. Thanks to Anthony Brien for this fix.
1.82	6/29/12															ed for dele
		Fixed bug where only the last file of a deleted pair of parfiles was markte in Perforce.
		  For example, deleting a WinForm Form1.cs would physicallyent/subsidiary  delete both Form1.cs and Form1.designer.cs, but Perforces wasn't "told" about Form1.cs
1.83	8/18/12
		Allow Mark for Add of files that are Marked for Delete.
		Fixed problem where File Open Web Site tried to add files before VS2P4 was ready to connect to Perforce.
			But note that File Open Web Site creates a solution in the default location which may not be under the Perforce root you expect! 
			You'll need to save the solution under the root, and close and re-open it, before proper connections to Perforce can be made.
1.84	9/3/12
		Fixed bug where files deleted from C++ projects weren't being deleted from Perforce.
1.85	3/15/13
		Do not close the connection when loading files. 
			This hurts the proxy in remote sites, and therefore causes the solution to load slower. [Thanks to vosherman.]
		After editing a file and then an external client (e.g. P4V) changes the file state, 
			allow VS2P4 to checkout and edit the file again instead of complaining about it's being read-only. [Thanks to jbdube.]
		Fixed problem where VS 2012 simply wouldn't work with VS2P4 -- all files showed red icons and buttons were disabled. [Thanks to oroussea.]
1.86	3/29/13 [Thanks to jspelletier]
		Removed writable file popup occurring when trying to save a file that VS2P4 thinks is in checkout but is not.
		Removed writable file popup occurring after reverting a file that is not up to date(2 cases)
		VS2012: Fix refresh when the solution format is VS2012 when loading a solution:
			* With VS2010 sln format: all the projects are already loaded when the OnAfterOpenSolution() is called and OnAfterLoadProject() is not called. 
				Thus the full refresh of the solution works.
			* With VS2012 sln format: When OnAfterOpenSolution() is called the projects are not loaded yet and OnAfterLoadProject will get called after each project
			Previously, when refreshing, VS2P4 was always refreshing the status of the whole solution. 
				This means that with a solution of 50 projects it was refreshing the solution 50 times instead of refreshing the individual project that gets loaded. 
				This was taking forever and blocking the UI almost always during the refresh.
			Note: When working in VS2012 with the VS2010 file formats files stays in VS2010 format until you run the performance wizard. 
				When this occurs VS silently creates a performance project and convert your solution to VS2012 format which triggered that problem.
1.87	10/14/13 
			1.86 was too aggressive in minimizing refreshes and sometimes a "collapsed" project would not be considered.
			Now we always refresh everything when we open a new solution.
			Hopefully we now support VS2013. No promises. Tested with VS 2013 RC, and it seems to work.
  
		   












		







		










