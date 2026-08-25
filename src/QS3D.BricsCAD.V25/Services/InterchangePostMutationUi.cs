using System;
using System.Threading;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class InterchangePostMutationUi
    {
        public static void RefreshProjectFailClosed(Document document)
        {
            if (document == null) return;

            try
            {
                // Reuse the existing dispatcher on BricsCAD's command/UI thread. Do not create
                // a WPF Application or a dispatcher on a worker thread just to refresh palettes.
                var Dispatcher = System.Windows.Threading.Dispatcher.FromThread(Thread.CurrentThread);
                if (Dispatcher == null) return;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)) return;

                        // Revalidate the canonical project/backing store inside the queued callback.
                        // A document switch, reload, stale sidecar or teardown therefore turns the
                        // refresh into a no-op instead of letting WPF touch stale host state.
                        if (!ProjectContextCoordinator.TryGetReadOnly(document, out _)) return;

                        PaletteCoordinator.RefreshProject();
                    }
                    catch (Exception)
                    {
                        // The semantic mutation already succeeded. A best-effort palette refresh
                        // must never surface a managed exception into BricsCAD's unmanaged pump.
                    }
                }));
            }
            catch (Exception)
            {
                // Scheduling is also best-effort after mutation; preserve the import result.
            }
        }
    }
}
