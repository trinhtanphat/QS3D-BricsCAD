using System;
using System.IO;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Revisions;
using QS3D.Core.Services;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class RevisionCoordinator
    {
        private static readonly RevisionService Service = new RevisionService();
        private static readonly RevisionSnapshotStore Store = new RevisionSnapshotStore();

        public static string CaptureBaseline(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var project = ExistingProjectMutationContext.Require(document, "Revision baseline");
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
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("Revision diff cần một QS3D project hiện hữu; capture hiện tại không tạo project mới.");
            var snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
            new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
            return CaptureCurrent(snapshot);
        }

        public static RevisionSnapshot CaptureCurrent(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return Service.Capture(project, "CURRENT-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        }

        public static string GetPath(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return Path.ChangeExtension(ProjectContextCoordinator.GetProjectPath(document), ".qsrev");
        }
    }
}
