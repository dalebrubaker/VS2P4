using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;

namespace BruSoft.VS2P4
{

    /// <summary>
    /// The "selected nodes" and the fileNames of every file within the selection.
    /// The nodes are used to determine which solution nodes need updated glyphs. They don't necessarily correspond
    ///     to the files (e.g. sometimes we check out one file but we update glyphs for all nodes in the solution.)
    /// The fileNames are used to determine which fileNames need updated FileStates in P4Cache.
    /// </summary>
    public class VsSelection
    {

        /// <summary>
        /// The fileNames that we want to cache in P4Cache.
        /// These may be piped, as in sourceName|targetName.
        /// </summary>
        public IList<string> FileNames { get; private set; }


        public IList<VSITEMSELECTION> Nodes { get; private set; }

        public VsSelection(IList<string> fileNames, IList<VSITEMSELECTION> nodes)
        {
            FileNames = fileNames;
            Nodes = nodes;
        }

        ///// <summary>
        ///// For rename, we use piped fileNames, like sourceFileName|targetFileName.
        ///// This method converts those to targetFileName (or whatever is the last piped string).
        ///// </summary>
        //public void ConvertPipedFileNames()
        //{
        //    for (int i = 0; i < FileNames.Count; i++)
        //    {
        //        string fileName = FileNames[i];
        //        string[] splits = fileName.Split('|');
        //        if (splits.Length > 1)
        //        {
        //            string lastString = splits[splits.Length - 1];
        //            FileNames[i] = lastString;
        //        }
        //    }
        //}

        public IList<string> FileNamesUnPiped
        {
            get
            {
                IList<string> result = new List<string>(FileNames.Count);
                foreach (var fileName in FileNames)
                {
                    string[] splits = fileName.Split('|');
                    foreach (var split in splits)
                    {
                        result.Add(split);
                    }
                }
                return result;
            }
        }
    }
}