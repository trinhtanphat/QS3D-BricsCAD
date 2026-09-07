using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;

namespace QS3D.BricsCAD.V25.Services
{
    /// <summary>
    /// Stable, exception-isolated presentation reporting for Direct Draw command boundaries.
    /// This helper owns no CAD/project mutation. Palette status is source-document fenced so a
    /// stale command cannot publish presentation state onto a different active DWG.
    /// </summary>
    internal static class DirectDrawUiFailureReporter
    {
        internal static void ReportOperationFailure(Document document, string operation)
        {
            if (document == null) return;
            var label = string.IsNullOrWhiteSpace(operation) ? "Direct Draw" : operation.Trim();
            var message = label + ": không thể hoàn tất thao tác. Vui lòng thử lại.";
            TryWriteEditor(document, message);
            TrySetPaletteForCurrentDocument(document, message);
        }

        internal static void ReportPostCommitWarning(Document document)
        {
            if (document == null) return;
            const string message = "Direct Draw đã commit nhưng đồng bộ giao diện chưa hoàn tất. Hãy refresh giao diện.";
            TryWriteEditor(document, message);
            TrySetPaletteForCurrentDocument(document, message);
        }

        internal static void ReportPostCommitSuccess(Document document, string message)
        {
            if (document == null) return;
            TrySetPaletteForCurrentDocument(document, message);
        }

        private static void TryWriteEditor(Document document, string message)
        {
            try { document.Editor.WriteMessage("\nQS3D " + message); }
            catch { }
        }

        private static void TrySetPaletteForCurrentDocument(Document document, string message)
        {
            try
            {
                if (ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                    PaletteCoordinator.SetStatus(message);
            }
            catch { }
        }
    }
}
