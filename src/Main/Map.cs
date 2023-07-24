using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// This class maintains the association between the filenames reported by Visual Studio (vsFileName)
    /// and the corresponding filenames in Perforce (p4FileName).
    /// We use this to handle virtual (SUBST) drives on the client side. 
    ///     That is, a filename on a virtual drive is converted to the non-virtual real path for matching to Perforce.
    ///     But if the Perforce root is a virtual drive, the client vsFileName must also be on the virtual drive.
    /// </summary>
    public class Map
    {
        private readonly bool _ignoreFilesNotUnderP4Root;

        public Map(bool ignoreFilesNotUnderP4Root)
        {
            _ignoreFilesNotUnderP4Root = ignoreFilesNotUnderP4Root;
        }

        /// <summary>
        /// The VS fileName is the key, and the P4 fileName is the value.
        /// The P4 fileName is a version that is under the root, if possible.
        /// We cache them here to avoid the overhead of continually looking for SUBST drive conversions
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _localFileNames = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, string> _p4FileNames = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// The VS fileName is the key, and the value is true if this fileName is under the P4 root.
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> _isFileNameUnderRoot = new ConcurrentDictionary<string, bool>();

        /// <summary>
        ///  The Perforce Root
        /// </summary>
        private string _root;

        private string _rootDir;
        private string _stream;


        /// <summary>
        /// If non-null, _root is a virtual (SUBST) drive and this is the actual path.
        /// </summary>
        private string _actualRoot;

        public string Root
        {
            get { return _root; }
        }

        public string Stream
        {
            get { return _stream; }
            set { _stream = value; }
        }

        public void SetRoot(string root)
        {
            var oldRoot = _root;
            _root = root;
            var msg = string.Format("Perforce Root is {0}", _root);
            var isChanged = false;
            var newRoot = _root.Replace('/', '\\');
            if (newRoot != _root)
            {
                _root = newRoot;
                isChanged = true;
            }
            if (_root[_root.Length - 1] != '\\')
            {
                _root += "\\";
            }
            if (isChanged)
            {
                msg += string.Format(", but VS2P4 is using {0}", _root);
            }
            _actualRoot = GetRealPath(_root);
            _rootDir = Path.GetDirectoryName(Path.GetFullPath(_root));
            if (!string.Equals(_root, _actualRoot, StringComparison.InvariantCultureIgnoreCase))
            {
                msg += string.Format(" (Actual root is {0})", _actualRoot);
            }
            if (_root != oldRoot)
            {
                string msgRoot;
                if (String.IsNullOrEmpty(oldRoot))
                {
                    msgRoot = string.Format("Perforce root is {0}", _root);
                }
                else
                {
                    msgRoot = string.Format("Perforce root has changed from {0} to {1}", oldRoot, _root);
                    _isFileNameUnderRoot.Clear();
                    _localFileNames.Clear();
                }
                Log.Information(msgRoot);
            }

            //Log.Information(msg);
        }

        /// <summary>
        /// Given a VS file name, return the fileName we want to use for Perforce.
        /// Load _isFileNameUnderRoot also
        /// </summary>
        /// <param name="vsFileName"></param>
        /// <param name="warning"></param>
        /// <returns></returns>
        public string GetLocalFileName(string vsFileName, out string warning)
        {
            warning = "";
            if (string.IsNullOrEmpty(_root))
            {
                // We haven't connected properly to Perforce.
                _isFileNameUnderRoot.TryAdd(vsFileName, false);
                return vsFileName;
            }

            string localFileName;
            if (_localFileNames.TryGetValue(vsFileName, out localFileName))
            {
                // Already cached, just use it.
                return localFileName;
            }

            if (IsFileUnderRoot(vsFileName))
            {
                localFileName = vsFileName;
                _localFileNames.TryAdd(vsFileName, localFileName);

                var p4FileName = localFileName;
                p4FileName = p4FileName.Replace(_rootDir, "");
                p4FileName = p4FileName.Replace('\\', '/');
                // TODO: should call P4Service.EscapeFilename here probably
                p4FileName = Stream + p4FileName;
                _p4FileNames.TryAdd(vsFileName, p4FileName);

                _isFileNameUnderRoot.TryAdd(vsFileName, true);
                return localFileName;
            }

            // One last try, checking if vsFileName is on a SUBST virtual drive
            localFileName = GetRealPath(vsFileName);
            if (!IsFileUnderRoot(localFileName))
            {
                _isFileNameUnderRoot.TryAdd(vsFileName, false);
                warning = string.Format("File {0} is not under Perforce root ({1})", vsFileName, _root);
                localFileName = vsFileName;
            }
            else
            {
                _isFileNameUnderRoot.TryAdd(vsFileName, true);
            }

            // cache the p4FileName so we won't try again in the future.
            _localFileNames.TryAdd(vsFileName, localFileName);

            return localFileName;
        }

        /// <summary>
        /// Given a VS file name, return the fileName we want to use for Perforce.
        /// Load _isFileNameUnderRoot also
        /// </summary>
        /// <param name="vsFileName"></param>
        /// <param name="warning"></param>
        /// <returns></returns>
        public string GetP4FileName(string vsFileName, out string warning)
        {
            warning = "";

            string p4FileName;
            if (_p4FileNames.TryGetValue(vsFileName, out p4FileName))
            {
                // Already cached, just use it.
                return p4FileName;
            }

            return GetLocalFileName(vsFileName, out warning);
        }

        /// <summary>
        /// Return true if vsFileName is within the Perforce root
        ///   or always true if !_ignoreFilesNotUnderP4Root
        /// </summary>
        /// <param name="vsFileName"></param>
        /// <returns></returns>
        public bool IsFileUnderRoot(string vsFileName)
        {
            if (!_ignoreFilesNotUnderP4Root)
            {
                // We don't want to ignore files under the Perforce root, so skip this check
                return true;
            }

            if (string.IsNullOrEmpty(_rootDir))
            {
                // This happens when we fail to connect to Perforce for some reason.
                Log.Error("Failed to connect to Perforce, null or empty root");
                return false;
            }

            var filename = Path.GetFullPath(vsFileName);
            var fileNameDir = Path.GetDirectoryName(filename);
            bool result = fileNameDir.StartsWith(_rootDir, StringComparison.InvariantCultureIgnoreCase);
            return result;
        }

        /// <summary>
        /// Return true if a prior GetP4FileName() has determined that vsFileName is under the Perforce root.
        /// </summary>
        /// <param name="vsFileName"></param>
        /// <returns></returns>
        public bool IsVsFileNameUnderRoot(string vsFileName)
        {
            if (!_isFileNameUnderRoot.ContainsKey(vsFileName))
            {
                return false;
            }
            return _isFileNameUnderRoot[vsFileName];
        }

        /// <summary>
        /// Get the real path in case this is a SUBST virtual drive.
        /// Eat any ArgumentException and return the input.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetRealPath(string path)
        {
            string result = path;
            try
            {
                result = GetRealPathImpl(path);
            }
            catch (Exception ex)
            {
                var message = String.Format("Map.GetRealPath threw exception: {0}", ex.Message);
                Log.Error(message);
            }

            return result;
        }

        /// <summary>
        /// Thanks to http://weblogs.asp.net/avnerk/archive/2006/06/04/Query-SUBST-information.aspx
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string GetRealPathImpl(string path)
        {
            if (path == null)
            {
                return null;
            }

            const int BUFFER_SIZE = 260; // DAB changed from 250
            string realPath = path;
            var pathInformation = new StringBuilder(BUFFER_SIZE);
            string root = Path.GetPathRoot(realPath);
            if (root == null)
            {
                Log.Error(String.Format("null root for path {0}", root));
                return null;
            }

            string driveLetter = root.Replace("\\", "");
            QueryDosDevice(driveLetter, pathInformation, BUFFER_SIZE);

            // If drive is substed, the result will be in the format of "\??\C:\RealPath\".
            if (pathInformation.ToString().Contains("\\??\\"))
            {
                // Strip the \??\ prefix.
                string realRoot = pathInformation.ToString().Remove(0, 4);

                //Combine the paths.
                var restOfPath = realPath.Replace(root, "");
                realPath = Path.Combine(realRoot, restOfPath);
            }

            // WCL - Try sending the path through Path.GetFullPath in an attempt to "canonicalize" pathnames.
            realPath = Path.GetFullPath(realPath);

            return realPath;
        }

        [DllImport("kernel32.dll")]
        static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);


  
    }
}
