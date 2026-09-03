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
#if BRICSCAD_V26
        private const string ExpectedRuntimeLabel = "V26";
#else
        private const string ExpectedRuntimeLabel = "V25";
#endif

        [CommandMethod("QS3DRELEASECHECK", CommandFlags.Modal)]
        public void ReviewReleaseReadiness()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    ReportBlocked(document, "Release Check: BLOCKED • chưa có QS3D project state/sidecar cho DWG hiện tại; lệnh kiểm tra không tạo project mới.");
                    return;
                }

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
                issues.AddRange(new GeneratedCurtainPanelHealthService().Inspect(project, liveGenerated));
                issues.AddRange(CurtainWallPanelLiveStateService.Inspect(document, project));
                issues.AddRange(GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project));
                issues.AddRange(PhysicalOpeningCutLiveStateService.Inspect(document, project));
                issues.AddRange(new GeneratedGeometryStaleHealthService().Inspect(project));
                issues.AddRange(new GeneratedRebarModeHealthService().Inspect(project));
                issues.AddRange(new RebarFabricationQualificationHealthService().Inspect(project));
                issues.AddRange(SemanticScheduleNativeTableBuilder.Inspect(document, project));
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
                    ? "Release Check: READY • không có Error/Warning trong semantic/generated/live CAD health. " + ExpectedRuntimeLabel + " runtime/private-DWG gate vẫn là bước riêng."
                    : "Release Check: BLOCKED • " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                ModelHealthWindowPresenter.Show(document, issues, issue => Locate(document, issue));
            }
            catch (System.Exception ex)
            {
                var message = "QS3DRELEASECHECK lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static void ReportBlocked(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + message); } catch { }
        }

        private static void Locate(Document document, ModelHealthIssue issue)
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)) return;
            if (string.IsNullOrWhiteSpace(issue.ElementId))
            {
                if ((issue.Code ?? string.Empty).StartsWith("WALL_JUNCTION_NATIVE_", StringComparison.OrdinalIgnoreCase))
                {
                    var junctionHandles = GeneratedWallJunctionRuntimeHealthService.Handles(document);
                    var junctionCount = CadHandleService.Select(document, junctionHandles);
                    PaletteCoordinator.SetStatus("Release Check Định vị " + issue.Code + " • " + junctionCount + " CAD object(s)");
                    if (junctionCount > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                    return;
                }
                if (!(issue.Code ?? string.Empty).StartsWith("CUSTOM_SCHEDULE_TABLE_", StringComparison.OrdinalIgnoreCase)) return;
                var artifactHandles = SemanticScheduleNativeTableBuilder.PersistedHandles(currentProject);
                if (artifactHandles.Count == 0) return;
                var artifactCount = CadHandleService.Select(document, artifactHandles);
                PaletteCoordinator.SetStatus("Release Check Định vị " + issue.Code + " • " + artifactCount + " CAD object(s)");
                if (artifactCount > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                return;
            }
            var element = currentProject.FindElement(issue.ElementId);
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
