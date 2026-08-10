using System;
using System.Collections.Generic;
using System.IO;
using Bricscad.ApplicationServices;
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
            Store.Save(snapshot, path);
            return path;
        }

        public static RevisionSnapshot LoadBaseline(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var path = GetPath(document);
            if (!File.Exists(path)) throw new FileNotFoundException("QS3D revision baseline was not found. Run QS3DREVBASE first.", path);
            return Store.Load(path);
        }

        public static RevisionSnapshot CaptureCurrent(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return Service.Capture(ProjectContextCoordinator.GetOrCreate(document), "CURRENT-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        }

        public static IReadOnlyList<RevisionDelta> Compare(Document document, out RevisionSnapshot before, out RevisionSnapshot after)
        {
            before = LoadBaseline(document);
            after = CaptureCurrent(document);
            return Service.Compare(before, after);
        }

        public static string GetPath(Document document) => Path.ChangeExtension(ProjectContextCoordinator.GetProjectPath(document), ".qsrev");
    }
}
