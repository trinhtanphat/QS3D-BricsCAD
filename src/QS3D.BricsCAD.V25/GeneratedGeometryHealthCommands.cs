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
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var issues = new GeneratedGeometryStaleHealthService().Inspect(project);
                if (issues.Count == 0)
                {
                    const string clean = "Generated Geometry Health: không có generated output stale.";
                    PaletteCoordinator.SetStatus(clean);
                    document.Editor.WriteMessage("\nQS3D " + clean);
                    return;
                }

                foreach (var issue in issues)
                    document.Editor.WriteMessage("\nQS3D " + issue.Code + " • " + issue.ElementId + " • " + issue.Message);
                var summary = "Generated Geometry Health: " + issues.Count + " stale output issue(s).";
                PaletteCoordinator.SetStatus(summary);
                document.Editor.WriteMessage("\nQS3D " + summary);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DGENERATEDHEALTH lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
