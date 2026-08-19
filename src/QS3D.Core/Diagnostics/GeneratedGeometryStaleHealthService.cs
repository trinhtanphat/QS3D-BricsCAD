using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedGeometryStaleHealthService
    {
        private static readonly KeyValuePair<string, string>[] StaleMetadataPairs =
        {
            new KeyValuePair<string, string>(ProjectElement.GeneratedSolidStateKey, ProjectElement.GeneratedSolidStaleSnapshotKey),
            new KeyValuePair<string, string>(ProjectElement.GeneratedRebarStateKey, ProjectElement.GeneratedRebarStaleSnapshotKey),
            new KeyValuePair<string, string>(ProjectElement.GeneratedShapeRebarStateKey, ProjectElement.GeneratedShapeRebarStaleSnapshotKey),
            new KeyValuePair<string, string>(ProjectElement.GeneratedTieRebarStateKey, ProjectElement.GeneratedTieRebarStaleSnapshotKey),
            new KeyValuePair<string, string>(ProjectElement.GeneratedBeamStirrupStateKey, ProjectElement.GeneratedBeamStirrupStaleSnapshotKey),
            new KeyValuePair<string, string>(ProjectElement.GeneratedSlabMeshStateKey, ProjectElement.GeneratedSlabMeshStaleSnapshotKey),
            new KeyValuePair<string, string>(ProjectElement.GeneratedWallMeshStateKey, ProjectElement.GeneratedWallMeshStaleSnapshotKey),
            new KeyValuePair<string, string>(ProjectElement.GeneratedFoundationMeshStateKey, ProjectElement.GeneratedFoundationMeshStaleSnapshotKey),
            new KeyValuePair<string, string>(ProjectElement.GeneratedCurtainFrameStateKey, ProjectElement.GeneratedCurtainFrameStaleSnapshotKey),
            new KeyValuePair<string, string>(ProjectElement.GeneratedCurtainPanelStateKey, ProjectElement.GeneratedCurtainPanelStaleSnapshotKey)
        };

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Generated-geometry stale diagnostics cannot inspect a project containing a null semantic element.");
                InspectMalformedStaleMetadata(element, issues);
                if (element.IsGeneratedSolidStale())
                    issues.Add(new ModelHealthIssue(
                        "GENERATED_SOLID_STALE",
                        HealthSeverity.Warning,
                        Message(element, "Generated host 3D không còn khớp semantic/source hiện tại; Build 3D lại trước khi dùng geometry này."),
                        element.Id));
                if (element.IsGeneratedRebarStale())
                    issues.Add(new ModelHealthIssue(
                        "REBAR_GENERATED_STALE",
                        HealthSeverity.Warning,
                        Message(element, "Generated longitudinal rebar 3D không còn khớp thông số hiện tại; rebuild cốt thép 3D cho cấu kiện."),
                        element.Id));
                if (element.IsGeneratedShapeRebarStale())
                    issues.Add(new ModelHealthIssue(
                        "SHAPE_REBAR_GENERATED_STALE",
                        HealthSeverity.Warning,
                        Message(element, "Generated shape rebar 3D không còn khớp BBS/thông số hiện tại; rebuild shape rebar 3D."),
                        element.Id));
                if (element.IsGeneratedTieRebarStale())
                    issues.Add(new ModelHealthIssue(
                        "TIE_REBAR_GENERATED_STALE",
                        HealthSeverity.Warning,
                        Message(element, "Generated column ties không còn khớp thông số/cấu kiện hiện tại; rebuild đai cột 3D."),
                        element.Id));
                if (element.IsGeneratedBeamStirrupStale())
                    issues.Add(new ModelHealthIssue(
                        "BEAM_STIRRUP_GENERATED_STALE",
                        HealthSeverity.Warning,
                        Message(element, "Generated beam stirrups không còn khớp thông số/cấu kiện hiện tại; rebuild đai dầm 3D."),
                        element.Id));
                if (element.IsGeneratedSlabMeshStale())
                    issues.Add(new ModelHealthIssue(
                        "SLAB_MESH_GENERATED_STALE",
                        HealthSeverity.Warning,
                        Message(element, "Generated slab mesh không còn khớp thông số/sàn hiện tại; rebuild lưới thép sàn 3D."),
                        element.Id));
                if (element.IsGeneratedWallMeshStale())
                    issues.Add(new ModelHealthIssue(
                        "WALL_MESH_GENERATED_STALE",
                        HealthSeverity.Warning,
                        Message(element, "Generated structural-wall mesh không còn khớp thông số/vách hiện tại; rebuild lưới thép vách 3D."),
                        element.Id));
                if (element.IsGeneratedFoundationMeshStale())
                    issues.Add(new ModelHealthIssue(
                        "FOUNDATION_MESH_GENERATED_STALE",
                        HealthSeverity.Warning,
                        Message(element, "Generated foundation mesh không còn khớp thông số/móng hiện tại; rebuild lưới thép móng 3D."),
                        element.Id));
                if (element.IsGeneratedCurtainFrameStale())
                    issues.Add(new ModelHealthIssue(
                        "CURTAIN_FRAME_GENERATED_STALE",
                        HealthSeverity.Warning,
                        Message(element, "Generated curtain-wall frame detail không còn khớp Family/Instance/source hiện tại; rebuild khung Vách Kính 3D."),
                        element.Id));
                if (element.IsGeneratedCurtainPanelStale())
                    issues.Add(new ModelHealthIssue(
                        "CURTAIN_PANEL_GENERATED_STALE",
                        HealthSeverity.Warning,
                        Message(element, "Generated curtain-wall panels no longer match the current semantic/opening state; rebuild curtain panels before release."),
                        element.Id));
            }
            return issues.AsReadOnly();
        }

        private static void InspectMalformedStaleMetadata(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            foreach (var pair in StaleMetadataPairs)
            {
                if (!element.Properties.TryGetValue(pair.Key, out var state))
                    continue;
                var stateText = state ?? string.Empty;
                var normalizedState = stateText.Trim();
                if (!string.Equals(normalizedState, "stale", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(stateText, "stale", StringComparison.Ordinal))
                    issues.Add(new ModelHealthIssue(
                        "GENERATED_STALE_STATE_NON_CANONICAL",
                        HealthSeverity.Error,
                        "Generated stale state phải dùng chính xác giá trị canonical 'stale' cho " + pair.Key + ". Rebuild generated output trước khi release.",
                        element.Id));
                if (element.Properties.TryGetValue(pair.Value, out var snapshot) && !string.IsNullOrWhiteSpace(snapshot))
                    continue;
                issues.Add(new ModelHealthIssue(
                    "GENERATED_STALE_METADATA_INVALID",
                    HealthSeverity.Error,
                    "Generated stale metadata thiếu snapshot bắt buộc cho " + pair.Key + ". Rebuild generated output trước khi release.",
                    element.Id));
            }
        }

        private static string Message(ProjectElement element, string fallback)
        {
            if (element.Properties.TryGetValue(ProjectElement.GeneratedGeometryStaleReasonKey, out var reason) && !string.IsNullOrWhiteSpace(reason))
                return fallback + " Lý do: " + reason.Trim();
            return fallback;
        }
    }
}
