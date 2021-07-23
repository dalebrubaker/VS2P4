using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace BruSoft.VS2P4
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// This class provides the interface to Perforce, replacing the SccProviderStorage class in the SccProvider sample.
    /// Be sure to use a new instance of this class on each new thread. This is a design decision of P4.Net.
    /// Be sure to Dispose of this class when through with it.
    /// </summary>
    /// <remarks>
    /// Docs: https://www.perforce.com/manuals/p4api.net/p4api.net_reference/
    /// </remarks>
    public class P4Service : IDisposable
    {
        private const string P4CmdLinePath = "P4VC";
        private readonly Map _map;
        private Perforce.P4.Options _p4Options = null;
        private Perforce.P4.ServerAddress _p4ServerAddress = null;
        private Perforce.P4.Server _p4Server = null;
        private Perforce.P4.Repository _p4Repository = null;
        //private string _password = null;

        public bool IsConnected { get; set; }

        private readonly object _statesLock = new object();

        private string Server { get { return _p4ServerAddress.Uri; } }

        private string User { get { return _p4Repository.Connection.UserName; } }

        private string Workspace { get { return _p4Repository.Connection.Client.Name; } }

        public P4Service(string server, string user, string password, string workspace, bool useP4Config, string path, Map map)
        {
            _map = map;
            if (_map == null)
            {
                _map = new Map(true);
            }

            //_password = password;

            // Before we set up the connection to Perforce, set the CWD so we pick up any changes to P4Config
            if (useP4Config)
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);

                _p4Options = new Perforce.P4.Options();
                _p4Options["ProgramName"] = "VS2P4";
                _p4Options["ProgramVersion"] = fvi.FileVersion;
                _p4Options["cwd"] = path;
            }
            else
            {
                _p4Options = null;
            }
            _p4ServerAddress = new Perforce.P4.ServerAddress(server);
            _p4Server = new Perforce.P4.Server(_p4ServerAddress);
            _p4Repository = new Perforce.P4.Repository(_p4Server);

            // use the connection variables for this connection
            // _p4Repository.Connection.ApiLevel = 65; // 2009.1, to support P4 Move. See http://kb.perforce.com/article/512/perforce-protocol-levels
            _p4Repository.Connection.UserName = user;
            _p4Repository.Connection.Client = new Perforce.P4.Client();
            _p4Repository.Connection.Client.Name = workspace;

            //Log.Information(String.Format(Resources.Connected_To, _p4.Port, _p4.User, _p4.Client));
        }

        /// <summary>
        /// Connect to Perforce if not already connected.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when null or empty P4Options.Port, or when P4Options.Client is invalid</exception>
        /// <exception cref="P4API.Exceptions.PerforceInitializationError">Thrown P4Options.Port is invalid</exception>
        public void Connect()
        {
            if (IsConnected) 
            {
                return;
            }

            do
            {
                try
                {
                    // .Api must be set before the call to Connect()
                    // _p4.Api = 65; // 2009.1, to support P4 Move. See http://kb.perforce.com/article/512/perforce-protocol-levels  

                    _p4Repository.Connection.Connect(_p4Options);
                }
                catch (Perforce.P4.P4Exception)
                {
                    Log.Error(String.Format(Resources.Unable_To_Connect, Server, ""));
                    IsConnected = false;
                    throw;
                }

                // There seems to be a problem in P4.Net IsValidConnection() -- the login check always succeeds.

                IsConnected = _p4Repository.Connection.connectionEstablished();
                if (IsConnected)
                {
                    break;
                }

                // If connection failed, ask for a login and try again.
                var dlgLogin = new DlgLogin(Server, "");
                if (dlgLogin.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                {
                    Log.Error(String.Format(Resources.Unable_To_Connect, Server, ""));
                    throw new ArgumentException("Login failed");
                }

                _p4Repository.Connection.Login(dlgLogin.Password);

                dlgLogin.Dispose();
            } while (true);

            var root = _p4Repository.Connection.Client.Root;
            _map.SetRoot(root);
        }

        /// <summary>
        ///  Disconnect from Perforce.
        /// </summary>
        public void Disconnect()
        {
            if (_p4Repository.Connection != null)
            {
                _p4Repository.Connection.Disconnect();
            }
            IsConnected = false;
        }

        public void Dispose()
        {
            if (_p4Repository != null)
            {
                _p4Repository.Dispose();
                _p4Repository = null;
            }
        }

        /// <summary>
        /// Add fileName to Perforce source control.
        /// </summary>
        /// <param name="vsFileName">The file name to add.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool AddFile(string vsFileName, out string message)
        {
            // This is a new file, so add it to the map.
            string warning;
            string p4FileName = _map.GetP4FileName(vsFileName, out warning);
            return SendCommand("add", vsFileName, out message);
        }

        /// <summary>
        /// Send the equivalent to a P4 command.
        /// </summary>
        /// <param name="command">the p4 command, like "edit"</param>
        /// <param name="vsFileName">The file name to process.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        private bool SendCommand(string command, string vsFileName, out string message)
        {
            if (!_map.IsVsFileNameUnderRoot(vsFileName))
            {
                message = string.Format("Refusing to send command {0} to Perforce for {1} because it is not under the Perforce root {2}.", command, vsFileName, _map.Root);
                Log.Warning(message);
                return false;
            }
            string p4FileName = GetP4FileName(vsFileName);
            Perforce.P4.P4CommandResult recordSet;
            return SendCommand(command, out message, out recordSet, p4FileName);
        }

        /// <summary>
        /// Given a VS file name, return the fileName we want to use for Perforce.
        /// Load _isFileNameUnderRoot also
        /// </summary>
        /// <param name="vsFileName"></param>
        /// <returns></returns>
        private string GetP4FileName(string vsFileName)
        {
            string warning;
            string result = _map.GetP4FileName(vsFileName, out warning);
            if (!string.IsNullOrEmpty(warning))
            {
                Log.Warning(warning);
            }
            return result;
        }

        /// <summary>
        /// Send the equivalent to a P4 command.
        /// Note that P4UnParsedRecordSet returns Empty from fstat.
        /// </summary>
        /// <param name="command">the p4 command, like "edit"</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <param name="recordSet">The recordSet from P4</param>
        /// <param name="args">The args to add to the P4 command.</param>
        /// <returns>false if error (see message)</returns>
        private bool SendCommand(string command, out string message, out Perforce.P4.P4CommandResult recordSet, params string[] args)
        {
            string argsStr = Concatenate(args);
            try
            {
                //Log.Information(String.Format("P4Service.SendCommand() Starting: {0} {1}", command, argsStr));
                var cmd = new Perforce.P4.P4Command(_p4Repository.Connection, command, true);
                var opts = new Perforce.P4.StringList(args);
                
                recordSet = cmd.Run(opts);
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("P4Service.SendCommand() Exception: {0}", ex.Message));
                message = ex.Message;
                recordSet = null;
                return false;
            }

            if (recordSet.ErrorList != null && recordSet.ErrorList.Count > 0)
            {
                List<string> errors = new List<string>();
                foreach (var error in recordSet.ErrorList)
                {
                    if (error.SeverityLevel >= Perforce.P4.ErrorSeverity.E_FAILED)
                    {
                        errors.Add(error.ErrorMessage);
                        Log.Error(String.Format("P4Service.SendCommand(): {0} {1}", error.ErrorCode, error.ErrorMessage));
                    }
                    else if (error.SeverityLevel == Perforce.P4.ErrorSeverity.E_WARN)
                    {
                        Log.Warning(String.Format("P4Service.SendCommand(): {0} {1}", error.ErrorCode, error.ErrorMessage));
                    }
                    else if (error.SeverityLevel == Perforce.P4.ErrorSeverity.E_INFO)
                    {
                        Log.Information(String.Format("P4Service.SendCommand(): {0} {1}", error.ErrorCode, error.ErrorMessage));
                    }
                }
                if (errors.Count > 1)
                {
                    message = string.Join("\n", errors);
                    return false;
                }
            }

            message = Concatenate(recordSet.InfoOutput);
            if (!String.IsNullOrEmpty(message))
            {
                Log.Information(String.Format("P4Service.SendCommand() 3: {0}", message));
            }

            return true;
        }

        private static string Concatenate(string [] strings)
        {
            var result = new StringBuilder();
            foreach (string str in strings)
            {
                result.Append(str);
                result.Append('\n');
            }

            if (result.Length > 0)
            {
                result.Length--;
            }
            return result.ToString();
        }


        /// <summary>
        /// Delete fileName from Perforce source control.
        /// </summary>
        /// <param name="vsFileName">The file name to delete.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool DeleteFile(string vsFileName, out string message)
        {
            string p4FileName = GetP4FileName(vsFileName);
            var result = SendCommand("delete",p4FileName, out message);
            if (!result)
            {
                return false;
            }

            // 2/7/12 When we delete a file that is already opened for edit, the delete seems to succeed but an error message says it didn't
            result = !message.ToLower().Contains("can't delete");
            return result;
        }


        /// <summary>
        /// Revert fileName to Perforce head revision.
        /// </summary>
        /// <param name="vsFileName">The file name to revert.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool RevertFile(string vsFileName, out string message)
        {
            string p4FileName = GetP4FileName(vsFileName);
            return SendCommand("revert", p4FileName, out message);
        }

        /// <summary>
        /// Revert fileName to Perforce head revision IFF the file has not been changed since check out.
        /// </summary>
        /// <param name="vsFileName">The file name to revert.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool RevertIfUnchangedFile(string vsFileName, out string message)
        {
            Perforce.P4.P4CommandResult recordSet;
            string p4FileName = GetP4FileName(vsFileName);
            return SendCommand("revert", out message, out recordSet, "-a", p4FileName);
        }

        /// <summary>
        /// Sync fileName to Perforce head revision.
        /// </summary>
        /// <param name="vsFileName">The file name to revert.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool SyncFile(string vsFileName, out string message)
        {
            return SendCommand("sync", vsFileName, out message);
        }

        /// <summary>
        /// Add a fileName to Perforce then submit it.
        /// Used for testing
        /// </summary>
        /// <param name="vsFileName">The file name to add then submit.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool AddAndSubmitFile(string vsFileName, out string message)
        {
            // Need to do this for unit testing, or we get NullReferenceException in P4API
            Connect();
            Disconnect();

            Perforce.P4.Changelist cl = null;
            try
            {
                cl = _p4Repository.NewChangelist();
            }
            catch (NullReferenceException ex)
            {
                Trace.WriteLine(String.Format("P4Service.AddAndSubmitFile: {0}", ex.Message));
            }

            Perforce.P4.P4CommandResult recordSet;
            string p4FileName = GetP4FileName(vsFileName);
            bool result = SendCommand("add", out message, out recordSet, "-c", cl.Id.ToString(), p4FileName);
            if (!result)
            {
                return false;
            }

            Perforce.P4.SubmitResults unparsedRecordset = null;
            try
            {
                unparsedRecordset = cl.Submit(_p4Options);
            }
            catch (Perforce.P4.P4Exception ex)
            {
                message = HandleRunUnParsedExceptionError(ex);
                return false;
            }
            return true;
        }

        private string HandleRunUnParsedExceptionError(Perforce.P4.P4Exception ex)
        {
            string message = Concatenate(ex.Details);
            //message += "\n" + ex.Result.ErrorMessage;
            Log.Error(message);
            return message;
        }

        /// <summary>
        /// Edit (check out) fileName.
        /// </summary>
        /// <param name="vsFileName">The file name to edit.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool EditFile(string vsFileName, out string message)
        {
            return SendCommand("edit", vsFileName, out message);
        }

        /// <summary>
        /// Create a new changelist with the provided description
        /// </summary>
        /// <param name="description">The description to use for the new changelist</param>
        /// <param name="changeListNumber">If successful, the new changelist number</param>
        /// <returns>True on success</returns>
        public bool CreateChangelist(string description, ref int changeListNumber)
        {
            var cl = _p4Repository.NewChangelist();
            cl.Description = string.IsNullOrEmpty(description) ? "<VS2P4>" : description;
            changeListNumber = cl.Id;
            return false;
        }

        /// <summary>
        /// Edit (check out) fileName.
        /// </summary>
        /// <param name="vsFileName">The file name to edit.</param>
        /// <param name="changeListNumber">The changelist number to save the edit to.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool EditFile(string vsFileName, int changeListNumber, out string message)
        {
            if (!_map.IsVsFileNameUnderRoot(vsFileName))
            {
                message = string.Format("Refusing to send command {0} to Perforce for {1} because it is not under the Perforce root {2}.", "edit", vsFileName, _map.Root);
                Log.Warning(message);
                return false;
            }
            string p4FileName = GetP4FileName(vsFileName);
            Perforce.P4.P4CommandResult recordSet;
            if (changeListNumber > 0)
            {
                return SendCommand("edit", out message, out recordSet, "-c", changeListNumber.ToString(), p4FileName);
            }
            else
            {
                return SendCommand("edit", out message, out recordSet, p4FileName);
            }
        }

        /// <summary>
        /// Lock fileName.
        /// </summary>
        /// <param name="vsFileName">The file name to lock.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool LockFile(string vsFileName, out string message)
        {
            return SendCommand("lock", vsFileName, out message);
        }

        /// <summary>
        /// Unlock fileName.
        /// </summary>
        /// <param name="vsFileName">The file name to lock.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool UnlockFile(string vsFileName, out string message)
        {
            return SendCommand("unlock", vsFileName, out message);
        }

        /// <summary>
        /// Move (Rename) fileName.
        /// I get an error that the version is too old (before 2009.1), even on a newer version. 
        ///     So we do the combo here: integrate, delete
        /// </summary>
        /// <param name="fromFile">The old file name/location.</param>
        /// <param name="toFile">The new file name/location.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool MoveFileOld(string fromFile, string toFile, out string message)
        {
            Perforce.P4.P4CommandResult recordSet;
            string toP4FileName = GetP4FileName(toFile);
            string fromP4FileName = GetP4FileName(fromFile);
            bool result = SendCommand("integrate", out message, out recordSet, fromP4FileName, toP4FileName);
            if (!result)
            {
                return false;
            }

            return SendCommand("delete", fromFile, out message);
        }

        /// <summary>
        /// Move (Rename) fileName. 
        /// </summary>
        /// <param name="fromFile">The old file name/location.</param>
        /// <param name="toFile">The new file name/location.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool MoveFile(string fromFile, string toFile, out string message)
        {
            Perforce.P4.P4CommandResult recordSet;
            string toP4FileName = GetP4FileName(toFile);
            string fromP4FileName = GetP4FileName(fromFile);
            return SendCommand("move", out message, out recordSet, fromP4FileName, toP4FileName);
        }

        /// <summary>
        /// Diff Against Have Revision (Set environment variable $P4DIF to change the Diff program.)
        /// P4.Net doesn't support diff without -s flag
        /// </summary>
        /// <param name="fileName">The file name to lock.</param>
        /// <param name="message">The first line of the P4 command result if no error, else the error message.</param>
        /// <returns>false if error (see message)</returns>
        public bool DiffFile(string fileName, out string message)
        {
            throw new NotSupportedException();
            //return SendCommand("diff", fileName, out message);
        }

        /// <summary>
        /// Get the fileState for a single file, after converting fileName to the real (non-SUBST) path.
        /// </summary>
        /// <param name="vsFileName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public FileState GetVsFileState(string vsFileName, out string message)
        {
            string p4FileName = GetP4FileName(vsFileName);
            return GetP4FileState(p4FileName, out message);
        }

        /// <summary>
        /// Get the fileState for a single file
        /// </summary>
        /// <param name="vsFileName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public FileState GetP4FileState(string vsFileName, out string message)
        {
            Perforce.P4.P4CommandResult recordSet;
            var p4FileName = GetP4FileName(vsFileName);
            bool result = SendCommand("fstat", out message, out recordSet, p4FileName);
            if (!result)
            {
                // Some kind of error. This happens when the file is not under the client's root. 
                Log.Debug(string.Format("Not in Perforce (1): {0}", vsFileName));
                return FileState.NotInPerforce;
            }

            if (recordSet.TaggedOutput == null || recordSet.TaggedOutput.Count <= 0)
            {
                Log.Debug(string.Format("Not in Perforce (2): {0}", vsFileName));
                return FileState.NotInPerforce;
            }

            var record = recordSet.TaggedOutput[0];
            var fileState = GetFileStateFromRecordSet(record, out vsFileName);
            Log.Debug(string.Format("Perforce FileState: {0} for {1}", fileState, vsFileName));
            return fileState;
        }
        /// <summary>
        /// Get all the pending changelist for the current workspace
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, string> GetPendingChangelists()
        {
            var opts = new Perforce.P4.ChangesCmdOptions(Perforce.P4.ChangesCmdFlags.FullDescription, Workspace, 0, Perforce.P4.ChangeListStatus.Pending, null);
            var changeLists = _p4Repository.GetChangelists(opts);

            var output = new Dictionary<int, string>();
            foreach (var changeList in changeLists)
            {
                output.Add(changeList.Id, changeList.Description);
            }
            return output;
        }
        /// <summary>
        /// Get the FileState for each files in vsFileNames.
        /// The key to state must be the vsFileNames, not the p4FileNames
        /// </summary>
        /// <param name="vsFileNames"></param>
        public Dictionary<string, FileState> GetFileStates(IList<string> vsFileNames)
        {
            var sw = new Stopwatch();
            sw.Start();
            var warningsSb = new StringBuilder();
            lock (_statesLock)
            {
                // The key to states is the vs fileName.
                var states = new Dictionary<string, FileState>(vsFileNames.Count);
                var filesUnderPerforceRoot = new List<string>(vsFileNames.Count);
                var p4FileNames = new List<string>(vsFileNames.Count);
                foreach (var vsFileName in vsFileNames)
                {
                    // Do GetP4FileName() just so we can set _isFileNameUnderRoot[]
                    string warning;
                    string p4FileName = _map.GetP4FileName(vsFileName, out warning);
                    if (!string.IsNullOrEmpty(warning))
                    {
                        warningsSb.Append(warning);
                        warningsSb.Append('\n');
                    }

                    // Pull files out of the list that are not under Root, so we don't get exceptions on the full list
                    if (_map.IsVsFileNameUnderRoot(vsFileName))
                    {
                        filesUnderPerforceRoot.Add(vsFileName);
                        p4FileNames.Add(p4FileName);
                    }
                    else
                    {
                        states[vsFileName] = FileState.NotInPerforce;
                    }
                }
                var tmp1 = sw.ElapsedMilliseconds;
                AddStatesForAllFilesUnderPerforceRoot(filesUnderPerforceRoot, p4FileNames, states);
                var tmp2 = sw.ElapsedMilliseconds;
                Log.Debug(string.Format("To GetFileStates() before printing warnings is {0} msec", sw.ElapsedMilliseconds));
                sw.Restart();
                if (warningsSb.Length > 0)
                {
                    Log.Warning(warningsSb.ToString());
                }
                Log.Debug(string.Format("To log warnings in GetFileStates() is {0} msec", sw.ElapsedMilliseconds));
                sw.Stop();

                return states;
            }
        }

        /// <summary>
        /// Add to states the state for each file, all at one time (Fast)
        /// If there's an exception, falls back to doing one at a time.
        /// </summary>
        /// <param name="filesUnderPerforceRoot">the VS fileNames for files already verified to be under the Perforce root</param>
        /// <param name="p4FileNames">parallel list of the P4 fileNames for files in filesUnderPerforceRoot</param>
        /// <param name="states">The dictionary we are loading. Key is vsFileName</param>
        private void AddStatesForAllFilesUnderPerforceRoot(List<string> filesUnderPerforceRoot, List<string> p4FileNames, IDictionary<string, FileState> states)
        {
            if (filesUnderPerforceRoot.Count == 0)
            {
                return;
            }
            Perforce.P4.P4CommandResult recordSet = null;
            string message;
            bool result = SendCommand("fstat", out message, out recordSet, p4FileNames.ToArray());

            if (!result)
            {
                 //Some kind of error. Try to do each file individually so the error doesn't reflect on EVERY file
                AddStateForEachFile(filesUnderPerforceRoot, states);
                return;
            }

            if (recordSet.TaggedOutput.Count <= 0)
            {
                foreach (var vsFileName in filesUnderPerforceRoot)
                {
                    states[vsFileName] = FileState.NotInPerforce;
                }
                return;
            }

            // Now decode each record. Missing records must be NotInPerforce
            // The key to filesWithState is p4FileName
            var filesWithState = new Dictionary<string, FileState>(filesUnderPerforceRoot.Count);
            foreach (var record in recordSet.TaggedOutput)
            {
                string p4FileName;
                FileState state = GetFileStateFromRecordSet(record, out p4FileName);
                filesWithState[p4FileName.ToLower()] = state;
            }

#if DEBUG
            var keysTmp = new List<string>(filesWithState.Count);
            var statesTmp = new List<FileState>(filesWithState.Count);
            foreach (var kvp in filesWithState)
            {
                keysTmp.Add(kvp.Key);
                statesTmp.Add(kvp.Value);
            }
#endif

            // Now set each state we return.
            for (int i = 0; i < filesUnderPerforceRoot.Count; i++)
            {
                string vsFileName = filesUnderPerforceRoot[i];
                var p4FileName = GetP4FileName(vsFileName);
                FileState state;
                bool hasState = filesWithState.TryGetValue(p4FileName.ToLower(), out state);
                if (hasState)
                {
                    states[vsFileName] = state;
                }
                else
                {
                    states[vsFileName] = FileState.NotInPerforce;
                }
            }
        }

        /// <summary>
        /// Add to states the state for each file, one at a time (Slow)
        /// </summary>
        /// <param name="vsFileNames"></param>
        /// <param name="states"></param>
        private void AddStateForEachFile(IEnumerable<string> vsFileNames, IDictionary<string, FileState> states)
        {
            foreach (var vsFileName in vsFileNames)
            {
                string message;
                FileState state = GetP4FileState(vsFileName, out message);
                states[vsFileName] = state;
            }
        }

        /// <summary>
        /// Decode the Fields in record (returned from fstat for a file).
        /// </summary>
        /// <param name="fields">A record returned from fstat for a file</param>
        /// <param name="p4FileName">The P4 fileName for which this record applies</param>
        /// <returns>The FileState for the file</returns>
        private static FileState GetFileStateFromRecordSet(Perforce.P4.TaggedObject fields, out string p4FileName)
        {
            p4FileName = fields["clientFile"];
            p4FileName = p4FileName.Replace('/', '\\'); 

            if (fields.ContainsKey("headAction") && fields["headAction"] == "delete")
            {
                return FileState.DeletedAtHeadRevision;
            }

            if (fields.ContainsKey("ourLock"))
            {
                return FileState.Locked; // implies also opened for edit
            }

            if (fields.ContainsKey("action"))
            {
                string val = fields["action"];
                if (val == "edit")
                {
                    if (fields.ContainsKey("unresolved"))
                    {
                        return FileState.NeedsResolved;
                    }

                    if (fields.ContainsKey("haveRev") && fields.ContainsKey("headRev") && fields["haveRev"] != fields["headRev"])
                    {
                        return FileState.OpenForEditDiffers;
                    }

                    return FileState.OpenForEdit;
                }

                if (val == "add")
                {
                    if (fields.ContainsKey("movedFile"))
                    {
                        return FileState.OpenForRenameTarget;
                    }

                    return FileState.OpenForAdd;
                }

                if (val == "delete")
                {
                    if (fields.ContainsKey("movedFile"))
                    {
                        return FileState.OpenForRenameSource;
                    }

                    return FileState.OpenForDelete;
                }

                if (val == "move/delete")
                {
                    return FileState.OpenForRenameSource;
                }

                if (val == "move/add")
                {
                    return FileState.OpenForRenameTarget;
                }

                if (val == "integrate")
                {
                    return FileState.OpenForIntegrate;
                }

                if (val == "branch")
                {
                    return FileState.OpenForBranch;
                }
            }
            else
            {
                // No action field
                if (fields.ContainsKey("haveRev"))
                {
                    if  (fields.ContainsKey("headRev") && fields["haveRev"] == fields["headRev"])
                    {
                        return FileState.CheckedInHeadRevision;
                    }
                    else
                    {
                        return FileState.CheckedInPreviousRevision;
                    }
                }
            }

            if (fields.ContainsKey("otherOpen"))
            {
                var arrayFields = fields;
                if (arrayFields.ContainsKey("otherLock"))
                {
                    return FileState.LockedByOtherUser;
                }

                if (arrayFields.ContainsKey("otherAction"))
                {
                    /*
                                        string[] array = arrayFields["otherAction"];
                                        if (array.Any(val => val == "edit"))
                                        {
                                            return FileState.OpenForEditOtherUser;
                                        }

                                        if (array.Any(val => val == "delete"))
                                        {
                                            return FileState.OpenForDeleteOtherUser;
                                        }
                    */
                }
            }

            return FileState.NotSet;
        }

        /// <summary>
        /// Run program fileName with args, don't wait for the end of program.
        /// This is better than Process.Start(cmd, args), which can pop a command window
        /// </summary>
        /// <param name="fileName">The fully qualified name of the executable to start</param>
        /// <param name="args">The arguments, if any</param>
        private void RunCommandNoWait(string fileName, string args)
        {
            Log.Information(String.Format(Resources.Running_program, fileName, args));
            var pinfo = new ProcessStartInfo(fileName)
            {
                Arguments = args,
                CreateNoWindow = true,
                RedirectStandardError = false,
                RedirectStandardOutput = false,
                UseShellExecute = true,
            };

            Process.Start(pinfo);
        }

        private string GetConnectionString()
        {
            return String.Format("-p {0} -u {1} -c {2}", Server, User, Workspace);
        }

        /// <summary>
        /// Show the Revision History Report in P4V.exe
        /// </summary>
        /// <param name="fileName">the file name.</param>
        /// <returns>false if the command fails.</returns>
        public bool RevisionHistory(string fileName)
        {
            string warning;
            fileName = _map.GetP4FileName(fileName, out warning);
            if (!string.IsNullOrEmpty(warning))
            {
                Log.Warning(warning);
            }
            RunCommandNoWait(P4CmdLinePath, String.Format("{0} history \"{1}\"", GetConnectionString(), fileName));
            return true;
        }

        /// <summary>
        /// Show the Diff Report (Diff of head revision against workspace file) for fileName
        /// </summary>
        /// 
        /// <param name="fileName">the file name.</param>
        /// <returns>false if the command fails.</returns>
        public bool Diff(string fileName)
        {
            string warning;
            fileName = _map.GetP4FileName(fileName, out warning);
            if (!string.IsNullOrEmpty(warning))
            {
                Log.Warning(warning);
            }
            RunCommandNoWait(P4CmdLinePath, String.Format("{0} diff \"{1}\"", GetConnectionString(), fileName));
            return true;
        }

        /// <summary>
        /// Show the Time-Lapse Report for fileName
        /// </summary>
        /// <param name="fileName">the file name.</param>
        /// <returns>false if the command fails.</returns>
        public bool TimeLapse(string fileName)
        {
            string warning;
            fileName = _map.GetP4FileName(fileName, out warning);
            if (!string.IsNullOrEmpty(warning))
            {
                Log.Warning(warning);
            }
            RunCommandNoWait(P4CmdLinePath, String.Format("{0} timelapse \"{1}\"", GetConnectionString(), fileName));
            return true;
        }



    }
}
