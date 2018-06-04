
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

using P4API;

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
    public class P4Service : IDisposable
    {
        private readonly Map _map;
        private P4Connection _p4;

        public bool IsConnected { get; set; }

        private readonly object _statesLock = new object();

        
        public string Server
        {
            get
            {
                if (_p4 == null)
                {
                    return "";
                }

                return _p4.Port;
            }
        }

        public string User
        {
            get
            {
                if (_p4 == null)
                {
                    return "";
                }

                return _p4.User;
            }
        }

        public string Workspace
        {
            get
            {
                if (_p4 == null)
                {
                    return "";
                }

                return _p4.Client;
            }
        }

        public P4Service(string server, string user, string password, string workspace, bool useP4Config, string path, Map map) 
        {
            _map = map;
            if (useP4Config && String.IsNullOrEmpty(path))
            {
                return;
            }

            // Before we set up the connection to Perforce, set the CWD so we pick up any changes to P4Config
            _p4 = new P4Connection();
            var tmp1 = _p4.CallingVersion;
            _p4.Api = 65; // 2009.1, to support P4 Move. See http://kb.perforce.com/article/512/perforce-protocol-levels
            if (useP4Config)
            {
                _p4.CWD = path;
                _p4.Port = "";
                _p4.User = "";
                _p4.Password = "";
                _p4.Client = "";
            }
            else
            {
                _p4.Port = server;
                _p4.User = user;
                _p4.Password = password;
                _p4.Client = workspace;
            }

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

            try
            {
                // .Api must be set before the call to Connect()
                _p4.Api = 65; // 2009.1, to support P4 Move. See http://kb.perforce.com/article/512/perforce-protocol-levels  
                 
                _p4.Connect();
            }
            catch (P4API.Exceptions.PerforceInitializationError)
            {
                Log.Error(String.Format(Resources.Unable_To_Connect, _p4.Port, _p4.Client));
                IsConnected = false;
                throw;
            }

            // There seems to be a problem in P4.Net IsValidConnection() -- the login check always succeeds.
            IsConnected = _p4.IsValidConnection(true, true);
            if (!IsConnected)
            {
                Log.Error(String.Format(Resources.Unable_To_Connect, _p4.Port, _p4.Client));
                throw new ArgumentException("Not a valid workspace.");
            }

            P4Form client;
            try
            {
                client = _p4.Fetch_Form("client");
            }
            catch (Exception ex)
            {
                Log.Error("Unable to fetch client form.\n" + ex.Message);
                return;
            }

            var root = client["Root"];
            _map.SetRoot(root);
        }

        /// <summary>
        ///  Disconnect from Perforce.
        /// </summary>
        public void Disconnect()
        {
            if (_p4 != null)
            {
                _p4.Disconnect();
            }
            IsConnected = false;
        }

        public void Dispose()
        {
            if (_p4 != null)
            {
                _p4.Dispose();
                _p4 = null;
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
            string p4FileName = GetP4FileName(vsFileName);
            P4RecordSet recordSet;
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
            return _map.GetP4FileName(vsFileName);
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
        private bool SendCommand(string command, out string message, out P4RecordSet recordSet, params string[] args)
        {
            string argsStr = Concatenate(args);
            try
            {
                //Log.Information(String.Format("P4Service.SendCommand() Starting: {0} {1}", command, argsStr));
                recordSet = _p4.Run(command, args);
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("P4Service.SendCommand() Exception: {0}", ex.Message));
                message = ex.Message;
                recordSet = null;
                return false;
            }

            if (recordSet.HasErrors())
            {
                Log.Error(String.Format("P4Service.SendCommand() 1: {0}", recordSet.ErrorMessage));
                message = recordSet.ErrorMessage;
                return false;
            }

            if (recordSet.HasWarnings())
            {
                message = Concatenate(recordSet.Warnings);
                Log.Warning(String.Format("P4Service.SendCommand() 2: {0}", message));
                return true;
            }

            message = Concatenate(recordSet.Messages);
            if (!String.IsNullOrEmpty(message))
            {
                Log.Information(String.Format("P4Service.SendCommand() 3: {0}", message));
            }

            //Log.Information(String.Format("P4Service.SendCommand() Finished: {0} {1}", command, argsStr));
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
            return SendCommand("delete",p4FileName, out message);
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
            P4RecordSet recordSet;
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

            P4PendingChangelist cl = null;
            try
            {
                cl = _p4.CreatePendingChangelist("A testing changelist");
            }
            catch (NullReferenceException ex)
            {
                Trace.WriteLine(String.Format("P4Service.AddAndSubmitFile: {0}", ex.Message));
            }

            P4RecordSet recordSet;
            string p4FileName = GetP4FileName(vsFileName);
            bool result = SendCommand("add", out message, out recordSet, "-c", cl.Number.ToString(), p4FileName);
            if (!result)
            {
                return false;
            }

            try
            {
                cl.Submit();
            }
            catch (P4API.Exceptions.RunUnParsedException ex)
            {
                message = ex.Message;
                return false;
            }
            return true;
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
            P4RecordSet recordSet;
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
            P4RecordSet recordSet;
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
            P4RecordSet recordSet;
            var p4FileName = GetP4FileName(vsFileName);
            bool result = SendCommand("fstat", out message, out recordSet, p4FileName);
            if (!result)
            {
                // Some kind of error. This happens when the file is not under the client's root. 
                Log.Debug(string.Format("Not in Perforce (1): {0}", vsFileName));
                return FileState.NotInPerforce;
            }

            if (recordSet.Records.Length <= 0)
            {
                Log.Debug(string.Format("Not in Perforce (2): {0}", vsFileName));
                return FileState.NotInPerforce;
            }

            P4Record record = recordSet[0];
            var fileState = GetFileStateFromRecordSet(record, out vsFileName);
            Log.Debug(string.Format("Perforce FilesState: {0} for {1}", fileState, vsFileName));
            return fileState;
        }
        /// <summary>
        /// Get the FileState for each files in vsFileNames.
        /// The key to state must be the vsFileNames, not the p4FileNames
        /// </summary>
        /// <param name="vsFileNames"></param>
        public Dictionary<string, FileState> GetFileStates(IList<string> vsFileNames)
        {
            lock (_statesLock)
            {
                // The key to states is the vs fileName.
                var states = new Dictionary<string, FileState>(vsFileNames.Count);
                var filesUnderPerforceRoot = new List<string>(vsFileNames.Count);
                foreach (var vsFileName in vsFileNames)
                {
                    // Do GetP4FileName just so we can set _isFileNameUnderRoot[]
                    string p4FileName = GetP4FileName(vsFileName);

                    // Pull files out of the list that are not under Root, so we don't get exceptions on the full list
                    if (_map.IsVsFileNameUnderRoot(vsFileName))
                    {
                        filesUnderPerforceRoot.Add(vsFileName);
                    }
                    else
                    {
                        var msg = string.Format("Not under the root: {0}", vsFileName);
                        Log.Information(msg);
                        states[vsFileName] = FileState.NotInPerforce;
                    }
                }

                AddStatesForAllFilesUnderPerforceRoot(filesUnderPerforceRoot, states);
                return states;
            }
        }

        /// <summary>
        /// Add to states the state for each file, all at one time (Fast)
        /// If there's an exception, falls back to doing one at a time.
        /// </summary>
        /// <param name="filesUnderPerforceRoot">the VS fileNames for files already verified to be under the Perforce root</param>
        /// <param name="states">The dictionary we are loading. Key is vsFileName</param>
        private void AddStatesForAllFilesUnderPerforceRoot(List<string> filesUnderPerforceRoot, IDictionary<string, FileState> states)
        {
            if (filesUnderPerforceRoot.Count == 0)
            {
                return;
            }

            P4RecordSet recordSet;
            string message;
            var p4FileNames = new string[filesUnderPerforceRoot.Count];
            for (int i = 0; i < filesUnderPerforceRoot.Count; i++)
            {
                var vsFileName = filesUnderPerforceRoot[i];
                var p4FileName = GetP4FileName(vsFileName);
                p4FileNames[i] = p4FileName;
            }

            bool result = SendCommand("fstat", out message, out recordSet, p4FileNames);
            if (!result)
            {
                // Some kind of error. Try to do each file individually so the error doesn't reflect on EVERY file
                AddStateForEachFile(filesUnderPerforceRoot, states);
                return;
            }

            if (recordSet.Records.Length <= 0)
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
            foreach (P4Record record in recordSet)
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
        /// <param name="record">A record returned from fstat for a file</param>
        /// <param name="p4FileName">The P4 fileName for which this record applies</param>
        /// <returns>The FileState for the file</returns>
        private static FileState GetFileStateFromRecordSet(P4Record record, out string p4FileName)
        {
            FieldDictionary fields = record.Fields;

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
                ArrayFieldDictionary arrayFields = record.ArrayFields;
                if (arrayFields.ContainsKey("otherLock"))
                {
                    return FileState.LockedByOtherUser;
                }

                if (arrayFields.ContainsKey("otherAction"))
                {
                    string[] array = arrayFields["otherAction"];
                    if (array.Any(val => val == "edit"))
                    {
                        return FileState.OpenForEditOtherUser;
                    }

                    if (array.Any(val => val == "delete"))
                    {
                        return FileState.OpenForDeleteOtherUser;
                    }
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
                UseShellExecute = false,
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
            fileName = _map.GetP4FileName(fileName);
            RunCommandNoWait("P4V.EXE", String.Format("{0} -cmd \"history {1}\"", GetConnectionString(), fileName));
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
            fileName = _map.GetP4FileName(fileName);
            RunCommandNoWait("P4.EXE", String.Format("{0} diff \"{1}\"", GetConnectionString(), fileName));
            return true;
        }

        /// <summary>
        /// Show the Time-Lapse Report for fileName
        /// </summary>
        /// <param name="fileName">the file name.</param>
        /// <returns>false if the command fails.</returns>
        public bool TimeLapse(string fileName)
        {
            fileName = _map.GetP4FileName(fileName);
            RunCommandNoWait("P4V.EXE", String.Format("{0} -cmd \"annotate -i {1}\"", GetConnectionString(), fileName));
            return true;
        }



    }
}
