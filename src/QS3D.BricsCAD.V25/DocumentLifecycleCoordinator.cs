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
        private static void OnDocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document == null) return;
            ProjectContextCoordinator.GetOrCreate(e.Document);
        }
        private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document == null) return;
            ProjectContextCoordinator.GetOrCreate(e.Document);
            PaletteCoordinator.RefreshAll();
        }
        private static void OnDocumentDestroyed(object sender, DocumentDestroyedEventArgs e) => ProjectContextCoordinator.ForgetByName(e.FileName);
    }
}
