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
            docs.DocumentDestroyed += OnDocumentDestroyed;
            _started = true;
        }

        public static void Stop()
        {
            if (!_started) return;
            var docs = Application.DocumentManager;
            docs.DocumentCreated -= OnDocumentCreated;
            docs.DocumentActivated -= OnDocumentActivated;
            docs.DocumentDestroyed -= OnDocumentDestroyed;
            _started = false;
        }

        private static void OnDocumentCreated(object sender, DocumentCollectionEventArgs e) => EnsureProject(e.Document, false);
        private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e) => EnsureProject(e.Document, true);
        private static void OnDocumentDestroyed(object sender, DocumentDestroyedEventArgs e) => ProjectContextCoordinator.ForgetByName(e.FileName);

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
