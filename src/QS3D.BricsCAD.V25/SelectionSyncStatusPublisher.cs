using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25
{
    internal static class SelectionSyncStatusPublisher
    {
        public static void SetStatusForDocument(Document? sourceDocument, string status)
        {
            if (sourceDocument == null ||
                !ReferenceEquals(sourceDocument, Application.DocumentManager.MdiActiveDocument))
                return;

            PaletteCoordinator.SetStatus(status);
        }
    }
}
