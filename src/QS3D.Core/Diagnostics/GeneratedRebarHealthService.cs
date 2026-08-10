using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedRebarHealthService
    {
        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in project.Elements)
            {
                if (!element.Properties.TryGetValue("GeneratedRebarHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                var handles = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var validCount = 0;
                foreach (var item in handles)
                {
                    var handle = (item ?? string.Empty).Trim();
                    if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                    {
                        issues.Add(new ModelHealthIssue("INVALID_REBAR_GENERATED_HANDLE", HealthSeverity.Error, "GeneratedRebarHandles chứa handle không hợp lệ.", element.Id));
                        continue;
                    }
                    if (!local.Add(handle))
                    {
                        issues.Add(new ModelHealthIssue("DUPLICATE_REBAR_GENERATED_HANDLE", HealthSeverity.Error, "Một generated rebar handle bị lặp trong cùng element: " + handle, element.Id));
                        continue;
                    }
                    validCount++;
                    if (owners.TryGetValue(handle, out var owner) && !string.Equals(owner, element.Id, StringComparison.OrdinalIgnoreCase))
                        issues.Add(new ModelHealthIssue("REBAR_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated rebar solid đang được nhiều element nhận sở hữu; element khác: " + owner, element.Id));
                    else owners[handle] = element.Id;
                    if (element.SourceHandles.Any(x => string.Equals((x ?? string.Empty).Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                        issues.Add(new ModelHealthIssue("REBAR_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated rebar handle không được nằm trong SourceHandles.", element.Id));
                    if (liveSolidHandles != null && !liveSolidHandles.Contains(handle))
                        issues.Add(new ModelHealthIssue("REBAR_GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy generated rebar Solid3d: " + handle, element.Id));
                }

                if (element.Properties.TryGetValue("GeneratedRebarCount", out var countText))
                {
                    if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0 || count != validCount)
                        issues.Add(new ModelHealthIssue("REBAR_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning, "GeneratedRebarCount không khớp số handle hợp lệ.", element.Id));
                }
                else issues.Add(new ModelHealthIssue("REBAR_GENERATED_COUNT_MISSING", HealthSeverity.Warning, "Thiếu GeneratedRebarCount.", element.Id));

                if (!element.Properties.TryGetValue("GeneratedRebarDiameterMm", out var diameterText) ||
                    !double.TryParse(diameterText, NumberStyles.Float, CultureInfo.InvariantCulture, out var diameter) ||
                    double.IsNaN(diameter) || double.IsInfinity(diameter) || diameter <= 0d)
                    issues.Add(new ModelHealthIssue("REBAR_GENERATED_DIAMETER_INVALID", HealthSeverity.Warning, "GeneratedRebarDiameterMm thiếu hoặc không hợp lệ.", element.Id));
            }
            return issues;
        }
    }
}
