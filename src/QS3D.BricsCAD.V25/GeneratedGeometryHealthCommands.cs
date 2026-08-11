using System;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GeneratedGeometryHealthCommands
    {
        [CommandMethod("QS3DGENERATEDHEALTH", CommandFlags.Modal)]
        public void Inspect()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Generated Geometry Health: BLOCKED • chưa có QS3D project state/sidecar; health check không tạo project mới.");
                    return;
                }

                var issues = new GeneratedGeometryStaleHealthService().Inspect(project);
                if (issues.Count == 0)
                {
                    Report(document, "Generated Geometry Health: không có generated output stale.");
                    return;
                }

                foreach (var issue in issues)
                    document.Editor.WriteMessage("\nQS3D " + issue.Code + " • " + issue.ElementId + " • " + issue.Message);
                Report(document, "Generated Geometry Health: " + issues.Count + " stale output issue(s).");
            }
            catch (System.Exception ex)
            {
                Report(document, "QS3DGENERATEDHEALTH lỗi: " + ex.Message);
            }
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + message); } catch { }
        }
    }
}