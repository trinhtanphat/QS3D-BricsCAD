using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class InterchangePostMutationRefresh
    {
        public static void Schedule(Document document)
        {
            if (document == null) return;

            try
            {
                // Interchange commands run on BricsCAD's UI thread. Reuse its existing WPF
                // dispatcher rather than creating a dispatcher on a worker thread. The queued
                // callback runs only after the modal command path has unwound back to the host.
                var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
                if (dispatcher == null) return;

                dispatcher.BeginInvoke(new Action(() => {
                    try {
                        var activeDocument = Application.DocumentManager.MdiActiveDocument;
                        if (!ReferenceEquals(activeDocument, document)) return;

                        // A queued refresh must not resurrect or touch UI state for a project that
                        // was forgotten/reloaded while the command was unwinding.
                        if (!ProjectContextCoordinator.TryGetCached(document, out _)) return;

                        PaletteCoordinator.RefreshProject();
                    }
                    catch (Exception ex)
                    {
                        // UI refresh is best-effort after a successful semantic mutation. Keep
                        // dispatcher exceptions inside this callback so they cannot escape into
                        // BricsCAD's unmanaged message pump and terminate the host.
                        Trace.WriteLine("QS3D Interchange post-mutation refresh failed: " + ex);
                    }
                }));
            }
            catch (Exception ex)
            {
                // Scheduling itself is also best-effort. The semantic mutation has already
                // completed, so a UI scheduling failure must not relabel it as an import failure.
                Trace.WriteLine("QS3D Interchange post-mutation refresh scheduling failed: " + ex);
            }
        }
    }
}
