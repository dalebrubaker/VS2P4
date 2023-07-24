using System;
using System.Collections.Generic;
using System.Diagnostics;

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
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                RefreshSelectedNodes();
            });
        }

        private void RefreshSelectedNodes()
        {
            var sw = new Stopwatch();
            sw.Start();
            _sccProvider.RefreshNodesGlyphs(_nodes);
            sw.Stop();
            Log.Debug(string.Format("{0} msec to refresh {1} nodes", sw.ElapsedMilliseconds, _nodes.Count));
        }
    }
}
