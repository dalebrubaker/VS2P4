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
        /// <summary>
        /// The VS fileName is the key, and the P4 fileName is the value.
        /// The P4 fileName is a version that is under the root, if possible.
        /// We cache them here to avoid the overhead of continually looking for SUBST drive conversions
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _p4FileNames = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// The VS fileName is the key, and the value is true if this fileName is under the P4 root.
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> _isFileNameUnderRoot = new ConcurrentDictionary<string, bool>();

        /// <summary>
        ///  The Perforce Root
        /// </summary>
        private string _root;


        /// <summary>
        /// If non-null, _root is a virtual (SUBST) drive and this is the actual path.
        /// </summary>
        private string _actualRoot;


        public void SetRoot(string root)
        {
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
                isChanged = true;
            }
            if (isChanged)
            {
                msg += string.Format(", but VS2P4 is using {0}", _root);
            }
            _actualRoot = GetRealPath(_root);
            if (_root.ToLower() != _actualRoot.ToLower())
            {
                msg += string.Format(" (Actual root is {0})", _actualRoot);
            }
            Log.Information(msg);
        }

        /// <summary>
        /// Given a VS file name, return the fileName we want to use for Perforce.
        /// Load _isFileNameUnderRoot also
        /// </summary>
        /// <param name="vsFileName"></param>
        /// <returns></returns>
        public string GetP4FileName(string vsFileName)
        {
            if (string.IsNullOrEmpty(_root))
            {
                // We haven't connected properly to Perforce.
                _isFileNameUnderRoot.TryAdd(vsFileName, false);
                return vsFileName;
            }

            string p4FileName;
            if (_p4FileNames.TryGetValue(vsFileName, out p4FileName))
            {
                // Already cached, just use it.
                return p4FileName;
            }

            // DAB: This doesn't really work well, for things like Rename/Move. 
            //if (!string.IsNullOrEmpty(_actualRoot))
            //{
            //    // The root is a virtual (SUBST) drive. Try to make vsFileName the same
            //    if (vsFileName.StartsWith(_actualRoot, StringComparison.InvariantCultureIgnoreCase))
            //    {
            //        var restOfPath = vsFileName.Substring(_actualRoot.Length);
            //        var virtualVsFileName = _root + restOfPath;
            //        virtualVsFileName = virtualVsFileName.Replace(@"\\", @"\");
            //        if (IsFileUnderRoot(virtualVsFileName))
            //        {
            //            p4FileName = virtualVsFileName;
            //            _p4FileNames.TryAdd(vsFileName, p4FileName);
            //            _isFileNameUnderRoot.TryAdd(vsFileName, true);
            //            return p4FileName;
            //        }
            //    }
            //}

            if (IsFileUnderRoot(vsFileName))
            {
                p4FileName = vsFileName;
                _p4FileNames.TryAdd(vsFileName, p4FileName);
                _isFileNameUnderRoot.TryAdd(vsFileName, true);
                return p4FileName;
            }

            // One last try, checking if vsFileName is on a SUBST virtual drive
            p4FileName = GetRealPath(vsFileName);
            if (!IsFileUnderRoot(p4FileName))
            {
                _isFileNameUnderRoot.TryAdd(vsFileName, false);
                var message = string.Format("File {0} is not under Perforce root {1}", vsFileName, _root);
                Log.Warning(message);
                p4FileName = vsFileName;
            }
            else
            {
                _isFileNameUnderRoot.TryAdd(vsFileName, true);
            }

            // cache the p4FileName so we won't try again in the future.
            _p4FileNames.TryAdd(vsFileName, p4FileName);

            return p4FileName;
        }

        /// <summary>
        /// Return true if vsFileName is within the Perforce root
        /// </summary>
        /// <param name="vsFileName"></param>
        /// <returns></returns>
        private bool IsFileUnderRoot(string vsFileName)
        {
            if (string.IsNullOrEmpty(_root))
            {
                // This happens when we fail to connect to Perforce for some reason.
                return false;
            }
            var diRoot = new DirectoryInfo(_root.ToLower());
            var diFile = new DirectoryInfo(vsFileName.ToLower());
            while (diFile.Parent != null)
            {
                if (diFile.Parent.FullName.TrimEnd('\\') == diRoot.FullName.TrimEnd('\\'))
                {
                    return true;
                }
                diFile = diFile.Parent;
            }
            return false;
        }

        /// <summary>
        /// Return true if a prior GetP4FileName() has determined that vsFileName is under the Perforce root.
        /// </summary>
        /// <param name="vsFileName"></param>
        /// <returns></returns>
        public bool IsVsFileNameUnderRoot(string vsFileName)
        {
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
