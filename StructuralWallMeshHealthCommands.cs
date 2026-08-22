using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class StructuralWallMeshHealthCommands
    {
        [CommandMethod("QS3DWALLREBARHEALTH", CommandFlags.Modal)]
        public void StructuralWallMeshHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var handles = new List<string>();
                foreach (var element in project.Elements)
                {
                    if (!element.Properties.TryGetValue("GeneratedWallMeshHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                    handles.AddRange(raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0));
                }
                var live = CadHandleService.GetLiveSolidHandles(document, handles.Distinct(StringComparer.OrdinalIgnoreCase));
                var issues = new GeneratedWallMeshHealthService().Inspect(project, live);
                var summary = new HealthSummary(issues);
                var message = "Wall Mesh Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                foreach (var issue in issues.Take(50))
                    document.Editor.WriteMessage("\n  [" + issue.Severity + "] " + issue.Code + " • " + issue.ElementId + " • " + issue.Message);
                if (issues.Count > 50) document.Editor.WriteMessage("\n  … health output truncated.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DWALLREBARHEALTH lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
