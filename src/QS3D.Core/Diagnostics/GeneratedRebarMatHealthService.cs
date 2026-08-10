using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedRebarMatHealthService
    {
        private const string HandlesKey = "GeneratedRebarMatHandles";
        private const string CountKey = "GeneratedRebarMatCount";

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var owners = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
            {
                if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var validCount = 0;
                foreach (var item in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var handle = (item ?? string.Empty).Trim();
                    if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                    {
                        issues.Add(new ModelHealthIssue("INVALID_REBAR_MAT_GENERATED_HANDLE", HealthSeverity.Error, HandlesKey + " chứa handle không hợp lệ.", element.Id));
                        continue;
                    }
                    if (!local.Add(handle))
                    {
                        issues.Add(new ModelHealthIssue("DUPLICATE_REBAR_MAT_GENERATED_HANDLE", HealthSeverity.Error, "Một rebar mat handle bị lặp trong cùng element: " + handle, element.Id));
                        continue;
                    }
                    validCount++;
                    var expectedOwner = element.Id + "/" + HandlesKey;
                    if (owners.TryGetValue(handle, out var owner) && !string.Equals(owner, expectedOwner, StringComparison.OrdinalIgnoreCase))
                        issues.Add(new ModelHealthIssue("REBAR_MAT_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated rebar mat solid xung đột owner/project handle khác: " + owner, element.Id));
                    if (element.SourceHandles.Any(x => string.Equals((x ?? string.Empty).Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                        issues.Add(new ModelHealthIssue("REBAR_MAT_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated rebar mat handle không được nằm trong SourceHandles.", element.Id));
                    if (liveSolidHandles != null && !liveSolidHandles.Contains(handle))
                        issues.Add(new ModelHealthIssue("REBAR_MAT_GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy generated rebar mat Solid3d: " + handle, element.Id));
                }

                if (!element.Properties.TryGetValue(CountKey, out var countText) ||
                    !int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0 || count != validCount)
                    issues.Add(new ModelHealthIssue("REBAR_MAT_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning, CountKey + " không khớp số handle hợp lệ.", element.Id));

                if (element.Category != ElementCategory.Slab && element.Category != ElementCategory.Foundation)
                    issues.Add(new ModelHealthIssue("REBAR_MAT_CATEGORY_MISMATCH", HealthSeverity.Error, "Generated rebar mat metadata chỉ hợp lệ trên Slab hoặc Foundation element.", element.Id));

                if (!element.Properties.TryGetValue("GeneratedRebarMatFaces", out var faces) ||
                    !(string.Equals(faces, "Bottom", StringComparison.OrdinalIgnoreCase) || string.Equals(faces, "Top", StringComparison.OrdinalIgnoreCase) || string.Equals(faces, "Both", StringComparison.OrdinalIgnoreCase)))
                    issues.Add(new ModelHealthIssue("REBAR_MAT_FACES_INVALID", HealthSeverity.Warning, "GeneratedRebarMatFaces thiếu hoặc không hợp lệ.", element.Id));

                if (element.Dirty != ElementDirtyFlags.None)
                    issues.Add(new ModelHealthIssue("REBAR_MAT_GENERATED_STALE", HealthSeverity.Warning, "Slab/Foundation đang dirty nhưng vẫn còn generated rebar mat; rebuild trước khi phát hành bản vẽ.", element.Id));
            }
            return issues;
        }

        private static Dictionary<string, string> BuildOwnershipIndex(ProjectState project)
        {
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                foreach (var handle in element.SourceHandles) Reserve(owners, handle, element.Id + "/SourceHandles");
                ReserveProperty(owners, element, "GeneratedSolidHandle");
                ReserveProperty(owners, element, "PhysicalOpeningCutSolidHandle");
            }
            foreach (var element in project.Elements)
            {
                ReserveProperty(owners, element, "GeneratedRebarHandles");
                ReserveProperty(owners, element, "GeneratedShapeRebarHandles");
                ReserveProperty(owners, element, "GeneratedTieRebarHandles");
                ReserveProperty(owners, element, "GeneratedBeamStirrupHandles");
                ReserveProperty(owners, element, HandlesKey);
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
    }
}
