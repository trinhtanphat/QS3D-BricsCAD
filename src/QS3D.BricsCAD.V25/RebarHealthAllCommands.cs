using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RebarHealthAllCommands
    {
        [CommandMethod("QS3DREBARHEALTHALL", CommandFlags.Modal)]
        public void ReviewAllGeneratedRebar()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    ReportBlocked(document, "Rebar Health All: BLOCKED • chưa có QS3D project state/sidecar; lệnh kiểm tra không tạo project mới.");
                    return;
                }

                var columnHandles = Collect(project, "GeneratedRebarHandles");
                var shapeHandles = Collect(project, "GeneratedShapeRebarHandles");
                var tieHandles = Collect(project, "GeneratedTieRebarHandles");
                var stirrupHandles = Collect(project, "GeneratedBeamStirrupHandles");
                var slabMeshHandles = Collect(project, "GeneratedSlabMeshHandles");
                var wallMeshHandles = Collect(project, "GeneratedWallMeshHandles");
                var foundationMeshHandles = Collect(project, FoundationMeshSolidBuilder.HandlesKey);
                var liveColumn = CadHandleService.GetLiveSolidHandles(document, columnHandles);
                var liveShape = CadHandleService.GetLiveSolidHandles(document, shapeHandles);
                var liveTie = CadHandleService.GetLiveSolidHandles(document, tieHandles);
                var liveStirrup = CadHandleService.GetLiveSolidHandles(document, stirrupHandles);
                var liveSlabMesh = CadHandleService.GetLiveSolidHandles(document, slabMeshHandles);
                var liveWallMesh = CadHandleService.GetLiveSolidHandles(document, wallMeshHandles);
                var liveFoundationMesh = CadHandleService.GetLiveSolidHandles(document, foundationMeshHandles);

                var issues = new List<ModelHealthIssue>();
                issues.AddRange(new GeneratedRebarHealthService().InspectAll(project, liveColumn, liveShape));
                issues.AddRange(new GeneratedTieRebarHealthService().Inspect(project, liveTie));
                issues.AddRange(new GeneratedBeamStirrupHealthService().Inspect(project, liveStirrup));
                issues.AddRange(new GeneratedSlabMeshHealthService().Inspect(project, liveSlabMesh));
                issues.AddRange(new GeneratedWallMeshHealthService().Inspect(project, liveWallMesh));
                issues.AddRange(new GeneratedFoundationMeshHealthService().Inspect(project, liveFoundationMesh));
                issues.AddRange(new GeneratedRebarOwnershipHealthService().Inspect(project));
                issues.AddRange(new RebarFabricationQualificationHealthService().Inspect(project));
                issues.AddRange(BbsNativeTableBuilder.Inspect(document, project));
                var summary = new HealthSummary(issues);
                var message = "Rebar Health All: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                ModelHealthWindowPresenter.Show(document, issues, issue =>
                {
                    if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)) return;
                    var element = currentProject.FindElement(issue.ElementId);
                    if (element == null) return;
                    var handles = HandlesForIssue(element, issue.Code);
                    var count = CadHandleService.Select(document, handles);
                    PaletteCoordinator.SetStatus("Rebar Health All Định vị " + element.Id + " • " + count + " solid(s)");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                });
            }
            catch (System.Exception)
            {
                var message = "QS3DREBARHEALTHALL lỗi: không thể hoàn tất health check.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static void ReportBlocked(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + message); } catch { }
        }

        private static string[] Collect(ProjectState project, string key) => project.Elements
            .SelectMany(x => Parse(x, key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        private static IEnumerable<string> HandlesForIssue(ProjectElement element, string code)
        {
            if (code.IndexOf("REBAR_FAB", StringComparison.OrdinalIgnoreCase) >= 0) return AllRebarHandles(element);
            if (code.IndexOf("FOUNDATION_MESH", StringComparison.OrdinalIgnoreCase) >= 0) return Parse(element, FoundationMeshSolidBuilder.HandlesKey);
            if (code.IndexOf("WALL_MESH", StringComparison.OrdinalIgnoreCase) >= 0) return Parse(element, "GeneratedWallMeshHandles");
            if (code.IndexOf("SLAB_MESH", StringComparison.OrdinalIgnoreCase) >= 0) return Parse(element, "GeneratedSlabMeshHandles");
            if (code.IndexOf("BEAM_STIRRUP", StringComparison.OrdinalIgnoreCase) >= 0) return Parse(element, "GeneratedBeamStirrupHandles");
            if (code.IndexOf("TIE_REBAR", StringComparison.OrdinalIgnoreCase) >= 0) return Parse(element, "GeneratedTieRebarHandles");
            if (code.IndexOf("SHAPE_REBAR", StringComparison.OrdinalIgnoreCase) >= 0) return Parse(element, "GeneratedShapeRebarHandles");
            if (code.IndexOf("CROSS_KEY", StringComparison.OrdinalIgnoreCase) >= 0) return AllRebarHandles(element);
            return Parse(element, "GeneratedRebarHandles");
        }

        private static IEnumerable<string> AllRebarHandles(ProjectElement element)
        {
            return GeneratedHandleOwnershipPolicy.RebarHandleKeys
                .SelectMany(key => Parse(element, key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IEnumerable<string> Parse(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0);
        }
    }
}
