using System;
<<<<<<< origin/main
using System.IO;
using Bricscad.ApplicationServices;
using QS3D.Core.Persistence;
=======
using System.Collections.Generic;
using System.IO;
using Bricscad.ApplicationServices;
>>>>>>> origin/agent/full-domain-integrate-20260810
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
<<<<<<< origin/main
            using (ProjectFileLock.Acquire(path)) Store.Save(snapshot, path);
=======
            Store.Save(snapshot, path);
>>>>>>> origin/agent/full-domain-integrate-20260810
            return path;
        }

        public static RevisionSnapshot LoadBaseline(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var path = GetPath(document);
<<<<<<< origin/main
            if (!File.Exists(path) && !File.Exists(path + ".bak")) throw new FileNotFoundException("Chưa có revision baseline. Chạy QS3DREVBASE trước.", path);
            return Store.LoadWithBackupFallback(path);
=======
            if (!File.Exists(path)) throw new FileNotFoundException("QS3D revision baseline was not found. Run QS3DREVBASE first.", path);
            return Store.Load(path);
>>>>>>> origin/agent/full-domain-integrate-20260810
        }

        public static RevisionSnapshot CaptureCurrent(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return Service.Capture(ProjectContextCoordinator.GetOrCreate(document), "CURRENT-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        }

<<<<<<< origin/main
        public static string GetPath(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return Path.ChangeExtension(ProjectContextCoordinator.GetProjectPath(document), ".qsrev");
        }
=======
        public static IReadOnlyList<RevisionDelta> Compare(Document document, out RevisionSnapshot before, out RevisionSnapshot after)
        {
            before = LoadBaseline(document);
            after = CaptureCurrent(document);
            return Service.Compare(before, after);
        }

        public static string GetPath(Document document) => Path.ChangeExtension(ProjectContextCoordinator.GetProjectPath(document), ".qsrev");
>>>>>>> origin/agent/full-domain-integrate-20260810
    }
}
