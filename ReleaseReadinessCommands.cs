using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ReleaseReadinessCommands
    {
        [CommandMethod("QS3DRELEASECHECK", CommandFlags.Modal)]
        public void ReviewReleaseReadiness()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var sourceHandles = project.Elements
                    .SelectMany(x => x.SourceHandles)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var generatedHandles = GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project).ToArray();
                var liveSources = new HashSet<string>(CadHandleService.GetLiveHandles(document, sourceHandles), StringComparer.OrdinalIgnoreCase);
                var liveGenerated = new HashSet<string>(CadHandleService.GetLiveSolidHandles(document, generatedHandles), StringComparer.OrdinalIgnoreCase);

                var issues = new List<ModelHealthIssue>();
                issues.AddRange(new ModelHealthService().Inspect(project, liveSources, liveGenerated));
                issues.AddRange(GeneratedSolidRuntimeHealthService.Inspect(document, project));
                issues.AddRange(new DependencyHealthService().Inspect(project));
                issues.AddRange(new LevelReferenceHealthService().Inspect(project));
                issues.AddRange(new SafeGeneratedHandleOwnershipHealthService().Inspect(project));
                issues.AddRange(new GeneratedRebarHealthService().InspectAll(project, liveGenerated, liveGenerated));
                issues.AddRange(new GeneratedTieRebarHealthService().Inspect(project, liveGenerated));
                issues.AddRange(new GeneratedBeamStirrupHealthService().Inspect(project, liveGenerated));
                issues.AddRange(new GeneratedSlabMeshHealthService().Inspect(project, liveGenerated));
                issues.AddRange(new GeneratedWallMeshHealthService().Inspect(project, liveGenerated));
                issues.AddRange(new GeneratedFoundationMeshHealthService().Inspect(project, liveGenerated));
                issues.AddRange(new GeneratedCurtainFrameHealthService().Inspect(project, liveGenerated));
                issues.AddRange(CurtainWallFrameLiveStateService.Inspect(document, project));
                issues.AddRange(PhysicalOpeningCutLiveStateService.Inspect(document, project));
                issues.AddRange(new GeneratedGeometryStaleHealthService().Inspect(project));
                issues.AddRange(new GeneratedRebarModeHealthService().Inspect(project));
                issues.AddRange(new RebarFabricationQualificationHealthService().Inspect(project));
                issues.AddRange(BomReleaseGuardService.Inspect(project, liveGenerated));
                issues = issues
                    .GroupBy(x => x.Code + "\n" + x.ElementId + "\n" + x.Message, StringComparer.Ordinal)
                    .Select(x => x.First())
                    .OrderByDescending(x => x.Severity)
                    .ThenBy(x => x.Code, StringComparer.Ordinal)
                    .ThenBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var summary = new HealthSummary(issues);
                var ready = summary.Errors == 0 && summary.Warnings == 0;
                var message = ready
                    ? "Release Check: READY • không có Error/Warning trong semantic/generated/live CAD health. V25 runtime/private-DWG gate vẫn là bước riêng."
                    : "Release Check: BLOCKED • " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                Application.ShowModelessWindow(IntPtr.Zero, new ModelHealthWindow(document, issues, issue => Locate(document, project, issue)), true);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DRELEASECHECK lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static void Locate(Document document, QS3D.Core.Domain.ProjectState project, ModelHealthIssue issue)
        {
            if (string.IsNullOrWhiteSpace(issue.ElementId)) return;
            var element = project.FindElement(issue.ElementId);
            if (element == null) return;
            var handles = SemanticReferenceHandles.Get(element)
                .Concat(GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element).Select(x => x.Key))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var count = CadHandleService.Select(document, handles);
            PaletteCoordinator.SetStatus("Release Check Định vị " + element.Id + " • " + count + " CAD object(s)");
            if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
        }
    }
}
