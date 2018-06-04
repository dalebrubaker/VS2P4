using System.Threading.Tasks;

namespace BruSoft.VS2P4
{
    using System;
    using System.Collections.Generic;
    using System.Threading;


    /// <summary>
    /// Maintains a cache of the Perforce FileStates of every file in the solution.
    /// When a solution is opened, every file in the solution is added to the cache with FileState.NotSet.
    /// Then asynchronously this class goes to Perforce to get the correct states.
    /// Then an event tells SccProviderService that the states are up to date.
    /// This is also done when renaming or adding files and when adding projects.
    /// For now, we are assuming that it is quick enough to refresh all glyphs in the solution,
    ///   rather than trying to cache information that would allow us to update individual nodes.
    /// </summary>
    public class P4Cache
    {
        private readonly Map _map;
        private string _server;
        private string _user;
        private string _password;
        private string _workspace;
        private bool _useP4Config;
        private string _solutionPath;


        /// <summary>
        /// For locking
        /// </summary>
        private readonly object _fileStatesLock = new object();

        /// <summary>
        /// The cache. Key is fileName, value is FileState
        /// </summary>
        private readonly Dictionary<string, FileState> _fileStates = new Dictionary<string, FileState>();

        public event EventHandler<P4CacheEventArgs> P4CacheUpdated;

        public P4Cache(string server, string user, string password, string workspace, bool useP4Config, string solutionPath, Map map)
        {
            _map = map;
            ResetConnection(server, user, password, workspace, useP4Config, solutionPath);
        }

        public void ResetConnection(string server, string user, string password, string workspace, bool useP4Config, string solutionPath)
        {
            _server = server;
            _user = user;
            _password = password;
            _workspace = workspace;
            _useP4Config = useP4Config;
            _solutionPath = solutionPath;
        }

        /// <summary>
        /// This is intended for when a new solution has been loaded. 
        ///     Clear the cache.
        ///     Reload the cache with the fileNames (duplicates are okay).
        ///     Then start a new thread to update the FileState for every file in fileNames.
        ///     Then throw an event when all fileStates have been updated, so VS can rediscover glyphs (from NotSet to whatever is the correct FileState).
        ///     All of this is to avoid sloooowww VS response time for large solutions while waiting for discovery of Perforce FileStates.
        /// </summary>
        /// <param name="vsSelection">A list of files to update and nodes to refresh.</param>
        public void Initialize(VsSelection vsSelection)
        {
            lock (_fileStatesLock)
            {
                _fileStates.Clear();
            }

            AddOrUpdateFilesBackground(vsSelection);
        }

        /// <summary>
        /// Returns the Perforce file state for fileName, or NotSet if fileName is not cached.
        /// </summary>
        /// <param name="fileName">The fileName.</param>
        /// <returns>the Perforce file state for fileName, or NotSet if fileName is not cached.</returns>
        public FileState this[string fileName]
        {
           get
            {
#if DEBUG
               var keys = new string[_fileStates.Count];
               _fileStates.Keys.CopyTo(keys, 0);

                var states = new FileState[_fileStates.Count];
               _fileStates.Values.CopyTo(states, 0);
#endif

               if (_fileStates.ContainsKey(fileName))
                {
                    return _fileStates[fileName];
                }

                return FileState.NotSet;
            }
        }

        /// <summary>
        /// Add one or more fileNames to the cache. Also used to signal the need to update fileStates of existing files.
        ///     Then kick off a thread to update their fileStates.
        ///     Then start a new thread to update the FileState for every file in fileNames.
        ///     Then throw an event when all fileStates have been updated, so VS can rediscover glyphs (from NotSet to whatever is the correct FileState).
        ///     All of this is to avoid sloooowww VS response time for large solutions while waiting for discovery of Perforce FileStates.
        /// </summary>
        /// <param name="vsSelection">A list of files and nodes to refresh.</param>
        public void AddOrUpdateFilesBackground(VsSelection vsSelection)
        {
            IList<string> fileNames = vsSelection.FileNamesUnPiped;
            Log.Debug(String.Format("P4Cache.AddOrUpdateFilesBackground(): {0} files for {1} nodes", fileNames.Count, vsSelection.Nodes.Count));
            lock (_fileStatesLock)
            {
                foreach (var fileName in fileNames)
                {
                    if (_fileStates.ContainsKey(fileName))
                    {
                        // Ignore duplicates. We may already have set their state.
                        continue;
                    }

                    _fileStates[fileName] = FileState.NotSet;
                }
            }

            // Now kick off a thread that updates the FileState for every fileNode in tLhe dictionary.
            Log.Debug("P4Cache:AddOrUpdateFilesBackground: Starting SetFileStates() on background thread.");
            Task.Factory.StartNew(() => SetFileStates(vsSelection));
        }

        /// <summary>
        /// Add one or more fileNames to the cache. This is NOT done on a background thread.
        /// </summary>
        /// <param name="vsSelection">A list of files to add or update.</param>
        public void AddOrUpdateFiles(VsSelection vsSelection)
        {
            IList<string> fileNames = vsSelection.FileNamesUnPiped;
            Log.Debug(String.Format("P4Cache.AddOrUpdateFiles(): Setting cache for {0} files", fileNames.Count));
            Log.Debug("P4Cache:AddOrUpdateFiles: Starting SetFileStates().");
            SetFileStates(fileNames);
        }

        /// <summary>
        /// Updates the cache with the current Perforce FileState of each file in fileNames
        /// Throws on connect error.
        /// Creates a new p4Service just for use during this method, for thread safety.
        /// </summary>
        /// <param name="obj">the object holding the lists of file names and selected nodes.</param>
        /// <exception cref="System.ArgumentException">Thrown when null or empty P4Options.Port, or when P4Options.Client is invalid</exception>
        /// <exception cref="P4API.Exceptions.PerforceInitializationError">Thrown P4Options.Port is invalid</exception>
        private void SetFileStates(object obj)
        {
            var vsSelection = obj as VsSelection;
            if (vsSelection == null)
            {
                Log.Debug("null p4CacheUpdateInfo passed to SetFileStates()");
                return;
            }

            IList<string> fileNames = vsSelection.FileNamesUnPiped;
            try
            {
                Log.Information("Starting to set file states on background thread");
                SetFileStates(fileNames);
                Log.Information(string.Format("Finished setting file states on background thread for {0} files", fileNames.Count));

                // When finished, throw an event that can tell the caller to tell VS to look for new glyphs for every file.
                P4CacheUpdated(this, new P4CacheEventArgs(vsSelection));
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Exception on background thread setting file states: {0}", ex.ToString()));
            }
        }

        private void SetFileStates(IList<string> fileNames)
        {
            var p4Service = new P4Service(_server, _user, _password, _workspace, _useP4Config, _solutionPath, _map);
            try
            {
                p4Service.Connect();
            }
            catch (ArgumentException)
            {
                p4Service.Dispose();
                // We can't handle this exception because we are on a background thread.
                // But the File States will show we are not connected.
                string msg = String.Format("ArgumentException in VS2P4.P4Cache.SetFileStates() -- Unable to connect to P4 at server={0} and user={1}", _server, _user);
                Log.Error(msg);
                return;
            }
            catch (P4API.Exceptions.PerforceInitializationError)
            {
                p4Service.Dispose();
                // We can't handle this exception because we are on a background thread.
                // But the File States will show we are not connected.
                string msg = String.Format("PerforceInitializationError in VS2P4.P4Cache.SetFileStates() -- Unable to connect to P4 at server={0} and user={1}", _server, _user);
                Log.Error(msg);
                return;
            }

            lock (_fileStatesLock)
            {
                Dictionary<string, FileState> states = p4Service.GetFileStates(fileNames);
                foreach (var fileName in states.Keys)
                {
                    FileState state = states[fileName];
                    _fileStates[fileName] = state;
                }
            }


            p4Service.Disconnect();
            p4Service.Dispose();
        }
    }
}