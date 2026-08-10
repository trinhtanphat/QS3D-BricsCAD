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
                        Message(element, "Generated column rebar 3D không còn khớp thông số hiện tại; rebuild cốt thép cột 3D."),
                        element.Id));
                if (element.IsGeneratedShapeRebarStale())
                    issues.Add(new ModelHealthIssue(
                        "SHAPE_REBAR_GENERATED_STALE",
                        HealthSeverity.Warning,
                        Message(element, "Generated shape rebar 3D không còn khớp BBS/thông số hiện tại; rebuild shape rebar 3D."),
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
