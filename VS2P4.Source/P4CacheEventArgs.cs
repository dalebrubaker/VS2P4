using System;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// The list of fileNames that were updated and the nodes to refresh
    /// </summary>
    public class P4CacheEventArgs : EventArgs
    {
        public VsSelection VsSelection { get; private set; }

        public P4CacheEventArgs(VsSelection vsSelection)
        {
            VsSelection = vsSelection;
        }
    }
}
