using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class HealthAllCommands
    {
        [CommandMethod("QS3DHEALTHALL", CommandFlags.Modal)]
        public void HealthAll()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    ReportBlocked(document, "Health All: BLOCKED • chưa có QS3D project state/sidecar; lệnh kiểm tra không tạo project mới.");
                    return;
                }

                var sourceHandles = project.Elements
                    .SelectMany(x => x.SourceHandles)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var mainHandles = PropertyHandles(project, "GeneratedSolidHandle");
                var longitudinalHandles = PropertyHandles(project, "GeneratedRebarHandles");
                var shapeHandles = PropertyHandles(project, "GeneratedShapeRebarHandles");
                var tieHandles = PropertyHandles(project, "GeneratedTieRebarHandles");
                var stirrupHandles = PropertyHandles(project, "GeneratedBeamStirrupHandles");
                var slabMeshHandles = PropertyHandles(project, "GeneratedSlabMeshHandles");
                var wallMeshHandles = PropertyHandles(project, "GeneratedWallMeshHandles");
                var foundationMeshHandles = PropertyHandles(project, FoundationMeshSolidBuilder.HandlesKey);
                var curtainFrameHandles = PropertyHandles(project, "GeneratedCurtainFrameHandles");
                var curtainPanelHandles = PropertyHandles(project, "GeneratedCurtainPanelHandles");

                var liveSources = CadHandleService.GetLiveHandles(document, sourceHandles);
                var liveMain = CadHandleService.GetLiveSolidHandles(document, mainHandles);
                var liveLongitudinal = CadHandleService.GetLiveSolidHandles(document, longitudinalHandles);
                var liveShape = CadHandleService.GetLiveSolidHandles(document, shapeHandles);
                var liveTies = CadHandleService.GetLiveSolidHandles(document, tieHandles);
                var liveStirrups = CadHandleService.GetLiveSolidHandles(document, stirrupHandles);
                var liveSlabMesh = CadHandleService.GetLiveSolidHandles(document, slabMeshHandles);
                var liveWallMesh = CadHandleService.GetLiveSolidHandles(document, wallMeshHandles);
                var liveFoundationMesh = CadHandleService.GetLiveSolidHandles(document, foundationMeshHandles);
                var liveCurtainFrames = CadHandleService.GetLiveSolidHandles(document, curtainFrameHandles);
                var liveCurtainPanels = CadHandleService.GetLiveSolidHandles(document, curtainPanelHandles);

                var combined = new List<ModelHealthIssue>();
                combined.AddRange(new ModelHealthService().Inspect(project, liveSources, liveMain));
                combined.AddRange(GeneratedSolidRuntimeHealthService.Inspect(document, project));
                combined.AddRange(new RoomFinishHealthService().Inspect(project));
                combined.AddRange(new DependencyHealthService().Inspect(project));
                combined.AddRange(new LevelReferenceHealthService().Inspect(project));
                combined.AddRange(new GeneratedGeometryStaleHealthService().Inspect(project));
                combined.AddRange(new GeneratedGridAnnotationHealthService().Inspect(project));
                combined.AddRange(GeneratedGridAnnotationRuntimeHealthService.Inspect(document, project));
                combined.AddRange(new GeneratedSemanticTagHealthService().Inspect(project));
                combined.AddRange(GeneratedSemanticTagRuntimeHealthService.Inspect(document, project));
                combined.AddRange(GeneratedSemanticElementTableRuntimeHealthService.Inspect(document, project));
                combined.AddRange(SemanticScheduleNativeTableBuilder.Inspect(document, project));
                combined.AddRange(BbsNativeTableBuilder.Inspect(document, project));
                combined.AddRange(BqNativeTableBuilder.Inspect(document, project));
                combined.AddRange(DoorOpeningNativeTableBuilder.Inspect(document, project));
                combined.AddRange(MaterialUsageNativeTableBuilder.Inspect(document, project));
                combined.AddRange(RoomFinishNativeTableBuilder.Inspect(document, project));
                combined.AddRange(new GeneratedRebarHealthService().InspectAll(project, liveLongitudinal, liveShape));
                combined.AddRange(new GeneratedTieRebarHealthService().Inspect(project, liveTies));
                combined.AddRange(new GeneratedBeamStirrupHealthService().Inspect(project, liveStirrups));
                combined.AddRange(new GeneratedSlabMeshHealthService().Inspect(project, liveSlabMesh));
                combined.AddRange(new GeneratedWallMeshHealthService().Inspect(project, liveWallMesh));
                combined.AddRange(new GeneratedFoundationMeshHealthService().Inspect(project, liveFoundationMesh));
                combined.AddRange(new GeneratedCurtainFrameHealthService().Inspect(project, liveCurtainFrames));
                combined.AddRange(CurtainWallFrameLiveStateService.Inspect(document, project));
                combined.AddRange(new GeneratedCurtainPanelHealthService().Inspect(project, liveCurtainPanels));
                combined.AddRange(CurtainWallPanelLiveStateService.Inspect(document, project));
                combined.AddRange(GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project));
                combined.AddRange(PhysicalOpeningCutLiveStateService.Inspect(document, project));
                combined.AddRange(new GeneratedRebarOwnershipHealthService().Inspect(project));
                combined.AddRange(new GeneratedHandleOwnershipHealthService().Inspect(project));
                combined.AddRange(new GeneratedRebarModeHealthService().Inspect(project));
                combined.AddRange(new RebarFabricationQualificationHealthService().Inspect(project));

                var issues = combined
                    .GroupBy(x => x.Severity + "|" + x.Code + "|" + x.ElementId + "|" + x.Message, StringComparer.Ordinal)
                    .Select(x => x.First())
                    .OrderByDescending(x => x.Severity)
                    .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var summary = new HealthSummary(issues);
                var message = "Health All: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);

                ModelHealthWindowPresenter.Show(document, issues, issue =>
                {
                    if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)) return;

                    if (string.IsNullOrWhiteSpace(issue.ElementId))
                    {
                        var artifactHandles = LocateProjectArtifactHandles(currentProject, issue.Code).ToArray();
                        if (artifactHandles.Length == 0 && IsWallJunctionNativeIssue(issue.Code))
                        {
                            artifactHandles = GeneratedWallJunctionRuntimeHealthService.Handles(document).ToArray();
                        }
                        if (artifactHandles.Length == 0) return;
                        var artifactCount = CadHandleService.Select(document, artifactHandles);
                        PaletteCoordinator.SetStatus("Health All Locate " + issue.Code + " • " + artifactCount + " CAD object");
                        if (artifactCount > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                        return;
                    }

                    var element = currentProject.FindElement(issue.ElementId);
                    if (element == null) return;
                    var handles = LocateHandles(element, issue.Code).ToArray();
                    if (handles.Length == 0) handles = SourceHandleResolver.Resolve(currentProject, new[] { element.Id }).ToArray();
                    var count = CadHandleService.Select(document, handles);
                    PaletteCoordinator.SetStatus("Health All Locate " + element.Id + " • " + count + " CAD object");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                });
            }
            catch (System.Exception)
            {
                var message = "QS3DHEALTHALL lỗi: không thể hoàn tất health check.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static void ReportBlocked(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + message); } catch { }
        }

        private static string[] PropertyHandles(ProjectState project, string key)
        {
            return project.Elements
                .SelectMany(x => SplitPropertyHandles(x, key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IEnumerable<string> LocateProjectArtifactHandles(ProjectState project, string code)
        {
            var normalized = (code ?? string.Empty).ToUpperInvariant();
            if (normalized.StartsWith("CUSTOM_SCHEDULE_TABLE_", StringComparison.Ordinal))
                return SemanticScheduleNativeTableBuilder.PersistedHandles(project);
            if (normalized.StartsWith("SEMANTIC_ELEMENT_TABLE_", StringComparison.Ordinal))
                return MetadataHandle(project, SemanticElementTableBuilder.HandleKey);
            if (normalized.StartsWith("BBS_", StringComparison.Ordinal))
                return MetadataHandle(project, BbsNativeTableBuilder.Definition.HandleKey);
            if (normalized.StartsWith("BQ_", StringComparison.Ordinal))
                return MetadataHandle(project, BqNativeTableBuilder.Definition.HandleKey);
            if (normalized.StartsWith("DOOR_OPENING_", StringComparison.Ordinal))
                return MetadataHandle(project, DoorOpeningNativeTableBuilder.Definition.HandleKey);
            if (normalized.StartsWith("MATERIAL_USAGE_", StringComparison.Ordinal))
                return MetadataHandle(project, MaterialUsageNativeTableBuilder.Definition.HandleKey);
            if (normalized.StartsWith("ROOM_FINISH_", StringComparison.Ordinal))
                return MetadataHandle(project, RoomFinishNativeTableBuilder.Definition.HandleKey);
            return Array.Empty<string>();
        }

        private static IEnumerable<string> MetadataHandle(ProjectState project, string key)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return new[] { raw.Trim() };
        }

        private static IEnumerable<string> LocateHandles(ProjectElement element, string code)
        {
            var normalized = (code ?? string.Empty).ToUpperInvariant();
            if (normalized.Contains("SEMANTIC_TAG")) return SplitPropertyHandles(element, GeneratedSemanticTagHealthService.HandlesKey);
            if (normalized.Contains("GRID_ANNOTATION")) return SplitPropertyHandles(element, "GeneratedGridAnnotationHandles");
            if (normalized.Contains("PHYSICAL_OPENING_CUT")) return SplitPropertyHandles(element, "PhysicalOpeningCutSolidHandle");
            if (normalized.Contains("CURTAIN_FRAME")) return SplitPropertyHandles(element, "GeneratedCurtainFrameHandles");
            if (normalized.Contains("CURTAIN_PANEL")) return SplitPropertyHandles(element, "GeneratedCurtainPanelHandles");
            if (normalized.Contains("REBAR_FAB")) return RebarOwnerSlotHandles(element);
            if (normalized.Contains("FOUNDATION_MESH")) return SplitPropertyHandles(element, FoundationMeshSolidBuilder.HandlesKey);
            if (normalized.Contains("WALL_MESH")) return SplitPropertyHandles(element, "GeneratedWallMeshHandles");
            if (normalized.Contains("SLAB_MESH")) return SplitPropertyHandles(element, "GeneratedSlabMeshHandles");
            if (normalized.Contains("BEAM_STIRRUP")) return SplitPropertyHandles(element, "GeneratedBeamStirrupHandles");
            if (normalized.Contains("TIE_REBAR")) return SplitPropertyHandles(element, "GeneratedTieRebarHandles");
            if (normalized.Contains("SHAPE_REBAR")) return SplitPropertyHandles(element, "GeneratedShapeRebarHandles");
            if (normalized.Contains("CROSS_KEY") || normalized.Contains("GENERATED_HANDLE_OWNERSHIP")) return OwnerSlotHandles(element);
            if (normalized.Contains("REBAR_GENERATED") || normalized.Contains("GENERATED_REBAR")) return SplitPropertyHandles(element, "GeneratedRebarHandles");
            if (normalized.Contains("GENERATED_SOLID") || normalized.Contains("GENERATED_HANDLE"))
                return SplitPropertyHandles(element, "GeneratedSolidHandle");
            return Array.Empty<string>();
        }

        private static IEnumerable<string> RebarOwnerSlotHandles(ProjectElement element)
        {
            return GeneratedHandleOwnershipPolicy.RebarHandleKeys
                .SelectMany(key => SplitPropertyHandles(element, key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool IsWallJunctionNativeIssue(string code)
        {
            var normalized = (code ?? string.Empty).ToUpperInvariant();
            return normalized.StartsWith("WALL_JUNCTION_NATIVE_", StringComparison.Ordinal);
        }

        private static IEnumerable<string> OwnerSlotHandles(ProjectElement element)
        {
            return element.Properties
                .Where(x => GeneratedHandleOwnershipPolicy.IsOwnerSlot(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                .SelectMany(x => SplitPropertyHandles(element, x.Key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IEnumerable<string> SplitPropertyHandles(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
