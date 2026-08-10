using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedTieRebarHealthService
    {
        private const string HandlesKey = "GeneratedTieRebarHandles";

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var owners = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
            {
                if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                var handles = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var valid = 0;
                foreach (var handle in handles)
                {
                    if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                    {
                        issues.Add(new ModelHealthIssue("INVALID_TIE_REBAR_GENERATED_HANDLE", HealthSeverity.Error, HandlesKey + " chứa handle không hợp lệ.", element.Id));
                        continue;
                    }
                    if (!local.Add(handle))
                    {
                        issues.Add(new ModelHealthIssue("DUPLICATE_TIE_REBAR_GENERATED_HANDLE", HealthSeverity.Error, "Generated tie handle bị lặp: " + handle, element.Id));
                        continue;
                    }
                    valid++;
                    var expectedOwner = element.Id + "/" + HandlesKey;
                    if (owners.TryGetValue(handle, out var owner) && !string.Equals(owner, expectedOwner, StringComparison.OrdinalIgnoreCase))
                        issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated tie solid xung đột owner/project handle khác: " + owner, element.Id));
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
                if (element.Category != ElementCategory.Column)
                    issues.Add(new ModelHealthIssue("TIE_REBAR_CATEGORY_MISMATCH", HealthSeverity.Error, "Generated tie metadata chỉ hợp lệ trên Column element.", element.Id));
                if (element.IsGeneratedTieRebarStale())
                    issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_STALE", HealthSeverity.Warning, "Generated tie snapshot không còn khớp semantic/source hiện tại; rebuild ties trước khi phát hành bản vẽ.", element.Id));
            }
            return issues;
        }

        private static Dictionary<string, string> BuildOwnershipIndex(ProjectState project)
        {
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                foreach (var handle in element.SourceHandles) Reserve(owners, handle, element.Id + "/SourceHandles");
                foreach (var property in element.Properties)
                {
                    if (!GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)) continue;
                    ReserveProperty(owners, element, property.Key);
                }
            }
            return owners;
        }

        private static void ReserveProperty(Dictionary<string, string> owners, ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
                Reserve(owners, handle, element.Id + "/" + key);
        }

        private static void Reserve(Dictionary<string, string> owners, string? handle, string token)
        {
            var normalized = (handle ?? string.Empty).Trim();
            if (normalized.Length == 0 || owners.ContainsKey(normalized)) return;
            owners[normalized] = token;
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
