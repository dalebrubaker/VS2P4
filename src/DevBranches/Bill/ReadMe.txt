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

1.72	3/18/12
		Refactored to use XML file for default settings.




		










