using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedGeometryStaleHealthService
    {
        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            foreach (var element in project.Elements)
            {
                if (element == null) continue;
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
            }
            return issues.AsReadOnly();
        }

        private static string Message(ProjectElement element, string fallback)
        {
            if (element.Properties.TryGetValue(ProjectElement.GeneratedGeometryStaleReasonKey, out var reason) && !string.IsNullOrWhiteSpace(reason))
                return fallback + " Lý do: " + reason.Trim();
            return fallback;
        }
    }
}
