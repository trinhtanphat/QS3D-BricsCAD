using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedTieRebarHealthService
    {
        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (!element.Properties.TryGetValue("GeneratedTieRebarHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                var handles = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var valid = 0;
                foreach (var handle in handles)
                {
                    if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                    {
                        issues.Add(new ModelHealthIssue("INVALID_TIE_REBAR_GENERATED_HANDLE", HealthSeverity.Error, "GeneratedTieRebarHandles chứa handle không hợp lệ.", element.Id));
                        continue;
                    }
                    if (!local.Add(handle))
                    {
                        issues.Add(new ModelHealthIssue("DUPLICATE_TIE_REBAR_GENERATED_HANDLE", HealthSeverity.Error, "Generated tie handle bị lặp: " + handle, element.Id));
                        continue;
                    }
                    valid++;
                    if (owners.TryGetValue(handle, out var owner) && !string.Equals(owner, element.Id, StringComparison.OrdinalIgnoreCase))
                        issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated tie solid đang được nhiều element nhận sở hữu; element khác: " + owner, element.Id));
                    else owners[handle] = element.Id;
                    if (element.SourceHandles.Any(x => string.Equals((x ?? string.Empty).Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                        issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated tie handle không được nằm trong SourceHandles.", element.Id));
                    if (liveSolidHandles != null && !liveSolidHandles.Contains(handle))
                        issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy generated tie Solid3d: " + handle, element.Id));
                }

                if (!element.Properties.TryGetValue("GeneratedTieRebarCount", out var countText) ||
                    !int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0 || count != valid)
                    issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning, "GeneratedTieRebarCount thiếu hoặc không khớp số handle hợp lệ.", element.Id));

                CheckPositive(element, "GeneratedTieRebarDiameterMm", "TIE_REBAR_GENERATED_DIAMETER_INVALID", "GeneratedTieRebarDiameterMm thiếu hoặc không hợp lệ.", issues);
                CheckNonNegative(element, "GeneratedTieRebarActualSpacingM", "TIE_REBAR_GENERATED_SPACING_INVALID", "GeneratedTieRebarActualSpacingM thiếu hoặc không hợp lệ.", issues);
            }
            return issues;
        }

        private static void CheckPositive(ProjectElement element, string key, string code, string message, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, message, element.Id));
        }

        private static void CheckNonNegative(ProjectElement element, string key, string code, string message, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, message, element.Id));
        }
    }
}
