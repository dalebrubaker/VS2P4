using System;
using System.Collections.Generic;

namespace BruSoft.VS2P4
{
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Refresh glyphs for nodes, on the VS UI thread
    /// </summary>
    public class NodesGlyphsRefresher
    {
        private readonly IList<VSITEMSELECTION> _nodes;

        private readonly VS2P4Package _sccProvider;

        public NodesGlyphsRefresher(IEnumerable<VSITEMSELECTION> nodes, VS2P4Package sccProvider)
        {
            _nodes = new List<VSITEMSELECTION>(nodes);
            _sccProvider = sccProvider;
        }

        public void Refresh()
        {
            ThreadHelper threadHelper = ThreadHelper.Generic;
            try
            {
                //threadHelper.Invoke(_sccProvider.RefreshSolutionGlyphs);
                threadHelper.Invoke(RefreshSelectedNodes);
                Log.Debug(String.Format("NodesGlyphsRefresher.Refresh: Updated all glyphs, for {0} nodes", _nodes.Count));
            }
            catch (NullReferenceException)
            {
                // This happens during unit testing.
                //_sccProvider.RefreshSolutionGlyphs();
                Log.Error("NodesGlyphsRefresher.Refresh:  NullReferenceException");
                RefreshSelectedNodes();
            }
        }

        private void RefreshSelectedNodes()
        {
            _sccProvider.RefreshNodesGlyphs(_nodes);
        }
    }
}
