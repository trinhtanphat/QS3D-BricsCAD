using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantitySummaryWindow
    {
        private bool _singleInstanceCheckScheduled;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (_singleInstanceCheckScheduled) return;
            _singleInstanceCheckScheduled = true;

            // BricsCAD still owns modeless-window creation. Reconcile against WPF's
            // live top-level window set before the new instance is rendered so a
            // repeated QS3DBQ invocation reuses the logical review tool instead of
            // inventing a feature-specific host/registry beside the generic policy.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ReuseExistingLogicalWindow));
        }

        private void ReuseExistingLogicalWindow()
        {
            var application = System.Windows.Application.Current;
            if (application == null) return;

            var existing = application.Windows
                .OfType<QuantitySummaryWindow>()
                .FirstOrDefault(window =>
                    !ReferenceEquals(window, this) &&
                    ReferenceEquals(window._document, _document));
            if (existing == null) return;

            try
            {
                existing.EnsureCurrentProject("làm mới BQ khi gọi lại QS3DBQ");
                existing.RefreshRowsForCurrentMode(false);
                if (existing.WindowState == WindowState.Minimized)
                    existing.WindowState = WindowState.Normal;
                existing.Activate();
                Close();
            }
            catch
            {
                // A stale/project-rebound window must never win the reuse race.
                // DocumentBoundWindowLifetime remains authoritative for lifecycle;
                // close the stale candidate and keep this freshly bound window.
                try { existing.Close(); }
                catch { }
            }
        }
    }
}
