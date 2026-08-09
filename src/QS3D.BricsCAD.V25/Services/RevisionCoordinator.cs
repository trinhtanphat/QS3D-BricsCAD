using System;
using System.IO;
using Bricscad.ApplicationServices;
using QS3D.Core.Revisions;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class RevisionCoordinator
    {
        private static readonly RevisionService Service = new RevisionService(); private static readonly RevisionSnapshotStore Store = new RevisionSnapshotStore();
        public static string CaptureBaseline(Document document)
        {
            var project = ProjectContextCoordinator.GetOrCreate(document); var snapshot = Service.Capture(project, "BASE-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); var path = GetPath(document); Store.Save(snapshot, path); return path;
        }
        public static RevisionSnapshot LoadBaseline(Document document) => Store.Load(GetPath(document));
        public static RevisionSnapshot CaptureCurrent(Document document) => Service.Capture(ProjectContextCoordinator.GetOrCreate(document), "CURRENT-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        public static string GetPath(Document document) => Path.ChangeExtension(ProjectContextCoordinator.GetProjectPath(document), ".qsrev");
    }
}
