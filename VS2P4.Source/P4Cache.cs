#if VS2P4_VS2022
using Community.VisualStudio.Toolkit;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BruSoft.VS2P4
{


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
        private bool _cacheActive;
        private bool _cacheIsUpdating;
        private Task _cacheUpdateTask;

        public class P4CacheUpdateException : Exception
        {
            public P4CacheUpdateException(string message)
                : base(message)
            {
            }
        };

        private static System.Diagnostics.TraceSwitch _traceSwitch = new TraceSwitch("P4CacheTracing", "Tracing switch controlling the level of tracing in P4Cache");

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
            Trace.WriteLineIf(_traceSwitch.TraceVerbose, "Creating P4Cache");
            // The cache isn't active until someone calls Initialize on it.
            _cacheActive = false;
            _cacheIsUpdating = false;
            _map = map;
            ResetConnection(server, user, password, workspace, useP4Config, solutionPath);
            Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache created");
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
            var msg = string.Format("Initializing P4Cache with {0} files", vsSelection.FileNames.Count);
            Trace.WriteLineIf(_traceSwitch.TraceVerbose, msg);
            Log.Information(msg);
            lock (_fileStatesLock)
            {
                // Just to be safe, make sure we aren't already initializing or updating the cache...
                if (_cacheIsUpdating)
                {
                    throw new P4CacheUpdateException("Initialize called while Initialize or an update is already active.");
                }
                _cacheActive = true;
                _fileStates.Clear();

                AddOrUpdateFilesBackground(vsSelection);
            }
            Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache initialized");
        }

        /// <summary>
        /// IsCacheActive_WaitForUpdate checks whether the cache is being used or not to cache file states.  If the cache is
        /// in the process of being updated, this property waits for that to complete, so that the answer
        /// reflects a consistent state of the cache.
        /// </summary>
        public bool IsCacheActive_WaitForUpdate()
        {
            if (_cacheIsUpdating)
            {
                Log.Debug("Waiting for cache to update");
                // If the cache is updating, wait for it to finish.
                try
                {
                    _cacheUpdateTask.Wait();
                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(_traceSwitch.TraceError,
                        String.Format("Exception {0} encountered while waiting for update to complete.",
                                        ex.Message));
                }
                // Double-check that initialization is not still in progress.
                if (_cacheIsUpdating)
                {
                    throw new P4CacheUpdateException("Cache update still in progress.");
                }
                Log.Debug("Cache updated, no longer waiting");
            }
            return _cacheActive;
        }

        /// <summary>
        /// IsCacheCurrent returns true if the cache currently holds valid states for filenames.  If the cache
        /// is in the process of being initialized or updated, IsCacheCurrent returns false.
        /// </summary>
        public bool IsCacheCurrent
        {
            get
            {
                return _cacheActive && (!_cacheIsUpdating);
            }
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
            if (vsSelection.FileNamesUnPiped.Count == 0)
            {
                return;
            }
            Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.AddOrUpdateFilesBackground started");
            Log.Debug(String.Format("P4Cache.AddOrUpdateFilesBackground(): {0} files for {1} nodes", vsSelection.FileNamesUnPiped.Count, vsSelection.Nodes.Count));
            SetFileStatesToNotSet(vsSelection);


            // Note: The updating flag *could* already be set if we get here between the time we release the lock and the
            // time the background thread is able to get the lock again.  In that case, just do nothing more, because the
            // background thread will eventually get to run and update the files we just added to _fileStates.
            if (!_cacheIsUpdating)
            {
                _cacheIsUpdating = true;

                // Now kick off a thread that updates the FileState for every fileNode in tLhe dictionary.  We do this
                // inside the lock, so that 2 threads can't do it at the same time.
                Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.AddOrUpdateFilesBackground starting SetFileStates on background thread");
                Log.Debug("P4Cache:AddOrUpdateFilesBackground: Starting SetFileStates() on background thread.");
                _cacheUpdateTask = Task.Factory.StartNew(() => 
                {
                    // Execute the entire background thread with the lock engaged, to prevent another call to
                    // AddOrUpdateFilesBackground from attempting to start us again until we've completed.  It is
                    // important that we reset the "is updating" flag while locked or we could get into this
                    // race condition.
                    lock (_fileStatesLock)
                    {
                        Trace.WriteLineIf(_traceSwitch.TraceVerbose, "Background thread acquired lock.");
                        try
                        {
                            SetFileStates(vsSelection);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLineIf(_traceSwitch.TraceError,
                                    String.Format("Background update thread encountered exception: {0}",
                                                ex.Message));
                            // Regardless of how many times we may have tried before to update the cache, now that
                            // it has failed, we must consider the cache inactive because it's not current and we
                            // apparently cannot make it current.
                            _cacheActive = false;
                        }
                        finally
                        {
                            // SetFileStates will reset this if it finishes successfully.  This is here mostly in
                            // case we get any exceptions.
                            _cacheIsUpdating = false;
                        }
                        Trace.WriteLineIf(_traceSwitch.TraceVerbose, "Background thread releasing lock.");
                    }
                });
            }
            Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.AddOrUpdateFilesBackground unlocking _fileStatesLock");
        }

        private void SetFileStatesToNotSet(VsSelection vsSelection)
        {
            IList<string> fileNames = vsSelection.FileNamesUnPiped;
            lock (_fileStatesLock)
            {
                foreach (var fileName in fileNames)
                {
                    Trace.WriteLineIf(_traceSwitch.TraceVerbose, String.Format("P4Cache.SetFileStatesToNotSet received filename {0}", fileName));
                    if (_fileStates.ContainsKey(fileName))
                    {
                        // Ignore duplicates. We may already have set their state.
                        continue;
                    }

                    _fileStates[fileName] = FileState.NotSet;
                }
            }
        }

        /// <summary>
        /// Updates the cache with the current Perforce FileState of each file in fileNames
        /// Throws on connect error.
        /// Creates a new p4Service just for use during this method, for thread safety.
        /// </summary>
        /// <param name="obj">the object holding the lists of file names and selected nodes.</param>
        /// <exception cref="System.ArgumentException">Thrown when null or empty P4Options.Port, or when P4Options.Client is invalid</exception>
        /// <exception cref="P4API.Exceptions.PerforceInitializationError">Thrown P4Options.Port is invalid</exception>
        private async Task SetFileStates(object obj)
        {
            Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.SetFileStates(object) started");
            var vsSelection = obj as VsSelection;
            if (vsSelection == null)
            {
                Log.Debug("null p4CacheUpdateInfo passed to SetFileStates()");
                return;
            }

            try
            {
                int fileCount = vsSelection.FileNames.Count;
                Log.Information(string.Format("Starting to set file states for {0} files on background thread", fileCount));

                const int stepSize = 5000;
                var sw = new Stopwatch();
                for (int i = 0; i < fileCount; i += stepSize)
                {
                    var chunk = vsSelection.GetSection(i, i + stepSize);

                    IList<string> fileNames = chunk.FileNamesUnPiped;
                    var end = Math.Min(i + stepSize, fileCount);
                    sw.Restart();
                    SetFileStates(fileNames);
                    sw.Stop();
                    var message = string.Format("Finished setting file states on background thread for {0}/{1} files", end, fileCount);
                    Log.Information($"{message}, took {sw.ElapsedMilliseconds} msec");
#if VS2P4_VS2022
                    await VS.StatusBar.ShowProgressAsync(message, end, fileCount);
#endif
                }
#if VS2P4_VS2022
                if (fileCount > 0)
                {
                    await VS.StatusBar.ShowProgressAsync(string.Empty, fileCount, fileCount);
                }
#endif
                // When finished, throw an event that can tell the caller to tell VS to look for new glyphs for every file.
                // But first reset the "is updating" flag so that the recipient can take advantage of the states already
                // being in the cache.
                _cacheIsUpdating = false;
                P4CacheUpdated(this, new P4CacheEventArgs(vsSelection));
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Exception on background thread setting file states: {0}", ex.ToString()));
            }
            Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.SetFileStates(object) completed");
        }

        private void SetFileStates(IList<string> fileNames)
        {
            if (fileNames.Count == 0)
            {
                return;
            }
            Trace.WriteLineIf(_traceSwitch.TraceVerbose,
                String.Format("P4Cache.SetFileStates(IList) started: _server={0}, _user={1}, _workspace={2}, _solutionPath={3}",
                               _server, _user, _workspace, _solutionPath));
            var p4Service = new P4Service(_server, _user, _password, _workspace, _useP4Config, _solutionPath, _map);
            try
            {
                p4Service.Connect();
            }
            catch (ArgumentException)
            {
                Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.SetFileStates(IList) got ArgumentException");
                p4Service.Dispose();
                // We can't handle this exception because we are on a background thread.
                // But the File States will show we are not connected.
                string msg = String.Format("ArgumentException in VS2P4.P4Cache.SetFileStates() -- Unable to connect to P4 at server={0} and user={1}", _server, _user);
                Log.Error(msg);
                return;
            }
            catch (Perforce.P4.P4Exception)
            {
                Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.SetFileStates(IList) got PerforceInitializationError");
                p4Service.Dispose();
                // We can't handle this exception because we are on a background thread.
                // But the File States will show we are not connected.
                string msg = String.Format("PerforceInitializationError in VS2P4.P4Cache.SetFileStates() -- Unable to connect to P4 at server={0} and user={1}", _server, _user);
                Log.Error(msg);
                return;
            }
            catch (Exception)
            {
                Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.SetFileStates(IList) got Exception");
                p4Service.Dispose();
                // We can't handle this exception because we are on a background thread.
                // But the File States will show we are not connected.
                string msg = String.Format("Exception in VS2P4.P4Cache.SetFileStates() -- Unable to connect to P4 at server={0} and user={1}", _server, _user);
                Log.Error(msg);
                return;
            }
            finally
            {
                Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.SetFileStates(IList) finished attempt to create P4Service");
            }
            Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.SetFileStates(IList) created P4Service");

            lock (_fileStatesLock)
            {
                Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.SetFileStates(IList) locked _fileStatesLock");
                Dictionary<string, FileState> states = p4Service.GetFileStates(fileNames);
                foreach (var fileName in states.Keys)
                {
                    Trace.WriteLineIf(_traceSwitch.TraceVerbose, String.Format("P4Cache.SetFileStates(IList) setting state for file {0}", fileName));
                    FileState state = states[fileName];
                    _fileStates[fileName] = state;
                    Trace.WriteLineIf(_traceSwitch.TraceVerbose, String.Format("P4Cache.SetFileStates(IList) state set for file {0} to {1}", fileName, state.ToString()));
                }
                Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.SetFileStates(IList) unlocking _fileStatesLock");
            }

            p4Service.Disconnect();
            p4Service.Dispose();
            Trace.WriteLineIf(_traceSwitch.TraceVerbose, "P4Cache.SetFileStates(IList) completed");
        }
    }
}
