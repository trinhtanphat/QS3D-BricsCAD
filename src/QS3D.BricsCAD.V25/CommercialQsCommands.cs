using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CommercialQsCommands
    {
        private static CommercialQsWindow? _window;
        private static Document? _publishedDocument;
        private static IntPtr _publishedNativeDatabaseIdentity;

        [CommandMethod("QS3D_COMMERCIAL_QS", CommandFlags.Modal)]
        public void ShowCommercialQs()
        {
            Show(CommercialQsSurface.Variations);
        }

        [CommandMethod("QS3D_VARIATIONS", CommandFlags.Modal)]
        public void ShowVariations()
        {
            Show(CommercialQsSurface.Variations);
        }

        [CommandMethod("QS3D_IPC", CommandFlags.Modal)]
        public void ShowIpc()
        {
            Show(CommercialQsSurface.Ipc);
        }

        [CommandMethod("QS3D_FINAL_ACCOUNT", CommandFlags.Modal)]
        public void ShowFinalAccount()
        {
            Show(CommercialQsSurface.FinalAccount);
        }

        private static void Show(CommercialQsSurface surface)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(document, nativeDatabaseIdentity))
                {
                    const string blocked = "Commercial QS window could not close safely; no duplicate window was opened.";
                    try { document.Editor.WriteMessage("\nQS3D Commercial QS: " + blocked); } catch { }
                    try { PaletteCoordinator.SetStatus(blocked); } catch { }
                    return;
                }

                if (_window != null)
                {
                    _window.SelectSurface(surface);
                    try { _window.Activate(); } catch { }
                    try { PaletteCoordinator.SetStatus(StatusFor(surface)); } catch { }
                    return;
                }

                var window = new CommercialQsWindow(document, surface);
                window.Closed += (_, __) => ReleasePublishedWindow(window);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded) return;

                _publishedDocument = document;
                _publishedNativeDatabaseIdentity = nativeDatabaseIdentity;
                _window = window;
                PaletteCoordinator.SetStatus(StatusFor(surface));
            }
            catch (Exception ex)
            {
                var message = "QS3D Commercial QS error: " + ex.Message;
                try { PaletteCoordinator.SetStatus(message); } catch { }
                try { document.Editor.WriteMessage("\n" + message); } catch { }
            }
        }

        private static bool PreparePublishedWindow(Document requestedDocument, IntPtr requestedNativeDatabaseIdentity)
        {
            var published = _window;
            if (published == null) return true;

            if (!published.IsLoaded)
            {
                ReleasePublishedWindow(published);
                return true;
            }

            if (ReferenceEquals(_publishedDocument, requestedDocument)
                && _publishedNativeDatabaseIdentity != IntPtr.Zero
                && _publishedNativeDatabaseIdentity == requestedNativeDatabaseIdentity)
            {
                return true;
            }

            try { published.Close(); }
            catch { return false; }
            if (published.IsLoaded) return false;
            ReleasePublishedWindow(published);
            return true;
        }

        private static void ReleasePublishedWindow(CommercialQsWindow window)
        {
            if (!ReferenceEquals(_window, window)) return;
            _window = null;
            _publishedDocument = null;
            _publishedNativeDatabaseIdentity = IntPtr.Zero;
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Commercial QS requires a BricsCAD document database.");
            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Commercial QS requires a live native BricsCAD database.");
            return identity;
        }

        private static string StatusFor(CommercialQsSurface surface)
        {
            switch (surface)
            {
                case CommercialQsSurface.Ipc:
                    return "Commercial QS — IPC: progress claim • approved variations • retention • recovery.";
                case CommercialQsSurface.FinalAccount:
                    return "Commercial QS — Final Account: contract value • approved variations • retention • settlement.";
                default:
                    return "Commercial QS — Variations: register • status • revision • approved value.";
            }
        }
    }
}
