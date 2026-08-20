using System;
using System.Collections.Generic;
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

            // BricsCAD still owns modeless-window creation. Reconcile against live
            // WPF presentation sources before the new instance is rendered so this
            // remains valid inside a hosted plugin even when there is no WPF
            // Application object owned by the BricsCAD process.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ReuseExistingLogicalWindow));
        }

        private void ReuseExistingLogicalWindow()
        {
            var existing = EnumerateLiveReviewWindows()
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

        private static IEnumerable<QuantitySummaryWindow> EnumerateLiveReviewWindows()
        {
            var seen = new HashSet<QuantitySummaryWindow>();

            // A standalone WPF host may expose Application.Current, so retain it as
            // an additional discovery surface without making it a prerequisite.
            var application = System.Windows.Application.Current;
            if (application != null)
            {
                foreach (Window window in application.Windows)
                    if (window is QuantitySummaryWindow review && seen.Add(review))
                        yield return review;
            }

            // QS3D is a BricsCAD-hosted WPF library. PresentationSource is the host-
            // safe authority for live WPF top-level sources when Application.Current
            // is null in the native BricsCAD process.
            foreach (PresentationSource source in PresentationSource.CurrentSources)
                if (source.RootVisual is QuantitySummaryWindow review && seen.Add(review))
                    yield return review;
        }
    }
}
