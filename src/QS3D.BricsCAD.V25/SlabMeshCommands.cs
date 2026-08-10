using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SlabMeshCommands
    {
        [CommandMethod("QS3DSLABREBAR3D", CommandFlags.UsePickSet)]
        public void BuildSlabMesh3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var result = SlabMeshSolidBuilder.BuildSelected(document, project);
                PaletteCoordinator.RefreshProject();
                var message = result.Bars == 0
                    ? "Slab Mesh 3D: chọn Slab semantic có closed rectangular POLYLINE + RebarSlabXNotation/RebarSlabYNotation."
                    : "Slab Mesh 3D: đã tạo/cập nhật " + result.Bars + " thanh trên " + result.Elements + " sàn.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DSLABREBAR3D lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        [CommandMethod("QS3DSLABREBARHEALTH", CommandFlags.Modal)]
        public void SlabMeshHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var handles = new List<string>();
                foreach (var element in project.Elements)
                {
                    if (!element.Properties.TryGetValue("GeneratedSlabMeshHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                    handles.AddRange(raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0));
                }
                var live = CadHandleService.GetLiveSolidHandles(document, handles.Distinct(StringComparer.OrdinalIgnoreCase));
                var issues = new GeneratedSlabMeshHealthService().Inspect(project, live);
                var summary = new HealthSummary(issues);
                var message = "Slab Mesh Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                foreach (var issue in issues.Take(50))
                    document.Editor.WriteMessage("\n  [" + issue.Severity + "] " + issue.Code + " • " + issue.ElementId + " • " + issue.Message);
                if (issues.Count > 50) document.Editor.WriteMessage("\n  … health output truncated.");
            }
            catch (Exception ex)
            {
                var message = "QS3DSLABREBARHEALTH lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
