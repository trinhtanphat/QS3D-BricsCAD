using System;
using System.IO;
using Bricscad.ApplicationServices;
using QS3D.Core.Persistence;
using QS3D.Core.Revisions;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class RevisionCoordinator
    {
        private static readonly RevisionService Service = new RevisionService();
        private static readonly RevisionSnapshotStore Store = new RevisionSnapshotStore();

        public static string CaptureBaseline(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var snapshot = Service.Capture(project, "BASE-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            var path = GetPath(document);
            using (ProjectFileLock.Acquire(path)) Store.Save(snapshot, path);
            return path;
        }

        public static RevisionSnapshot LoadBaseline(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var path = GetPath(document);
            if (!File.Exists(path) && !File.Exists(path + ".bak")) throw new FileNotFoundException("Chưa có revision baseline. Chạy QS3DREVBASE trước.", path);
            return Store.LoadWithBackupFallback(path);
        }

        public static RevisionSnapshot CaptureCurrent(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return Service.Capture(ProjectContextCoordinator.GetOrCreate(document), "CURRENT-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        }

        public static string GetPath(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return Path.ChangeExtension(ProjectContextCoordinator.GetProjectPath(document), ".qsrev");
        }
    }
}
