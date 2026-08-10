using System;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25
{
    internal static class DocumentLifecycleCoordinator
    {
        private static bool _started;

        public static void Start()
        {
            if (_started) return;
            var docs = Application.DocumentManager;
            docs.DocumentCreated += OnDocumentCreated;
            docs.DocumentActivated += OnDocumentActivated;
            docs.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
            SelectionSyncCoordinator.Attach(docs.MdiActiveDocument);
            _started = true;
        }

        public static void Stop()
        {
            if (!_started) return;
            var docs = Application.DocumentManager;
            docs.DocumentCreated -= OnDocumentCreated;
            docs.DocumentActivated -= OnDocumentActivated;
            docs.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
            SelectionSyncCoordinator.Stop();
            _started = false;
        }

        private static void OnDocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            SelectionSyncCoordinator.Attach(e.Document);
            EnsureProject(e.Document, false);
        }

        private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            SelectionSyncCoordinator.Attach(e.Document);
            EnsureProject(e.Document, true);
            SelectionSyncCoordinator.Refresh(e.Document);
        }

        private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            var document = e.Document;
            SelectionSyncCoordinator.Detach(document);
            ProjectContextCoordinator.Forget(document);
        }

        private static void EnsureProject(Document? document, bool refreshUi)
        {
            if (document == null) return;
            try
            {
                ProjectContextCoordinator.GetOrCreate(document);
                if (refreshUi) PaletteCoordinator.RefreshAll();
            }
            catch (Exception ex)
            {
                var message = "QS3D project load error: " + ex.Message;
                document.Editor.WriteMessage("\n" + message);
                PaletteCoordinator.SetStatus(message);
            }
        }
    }
}
