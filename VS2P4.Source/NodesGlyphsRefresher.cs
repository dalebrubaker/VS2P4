using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Diagnostics;

namespace BruSoft.VS2P4
{
    /// <summary>
    /// Refresh glyphs for nodes, on the VS UI thread
    /// </summary>
    public class NodesGlyphsRefresher
    {
        private readonly IList<VSITEMSELECTION> _nodes;
        
        private readonly VS2P4Package _sccProvider;
        private bool _isRunning = false;

        public NodesGlyphsRefresher(IEnumerable<VSITEMSELECTION> nodes, VS2P4Package sccProvider)
        {
            _nodes = new List<VSITEMSELECTION>(nodes);
            _sccProvider = sccProvider;
        }

        public void Refresh()
        {
            if (_isRunning)
            {
                return;
            }
            _isRunning = true;
            Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await RefreshSelectedNodesAsync();
                _isRunning = false;
            });
        }

        private async System.Threading.Tasks.Task RefreshSelectedNodesAsync()
        {
            var sw = new Stopwatch();
            sw.Start();
            await _sccProvider.RefreshNodesGlyphs(_nodes);
            sw.Stop();
            Log.Information(string.Format("{0} msec to refresh {1} nodes", sw.ElapsedMilliseconds, _nodes.Count));
        }
    }
}
