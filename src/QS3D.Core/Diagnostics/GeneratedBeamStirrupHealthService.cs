using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedBeamStirrupHealthService
    {
        private const string HandlesKey = "GeneratedBeamStirrupHandles";
        private const string CountKey = "GeneratedBeamStirrupCount";
        private const string DiameterKey = "GeneratedBeamStirrupDiameterMm";

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
                        issues.Add(new ModelHealthIssue("INVALID_BEAM_STIRRUP_GENERATED_HANDLE", HealthSeverity.Error, HandlesKey + " chứa handle không hợp lệ.", element.Id));
                        continue;
                    }
                    if (!local.Add(handle))
                    {
                        issues.Add(new ModelHealthIssue("DUPLICATE_BEAM_STIRRUP_GENERATED_HANDLE", HealthSeverity.Error, "Một beam stirrup handle bị lặp trong cùng element: " + handle, element.Id));
                        continue;
                    }
                    validCount++;
                    var expectedOwner = element.Id + "/" + HandlesKey;
                    if (owners.TryGetValue(handle, out var owner) && !string.Equals(owner, expectedOwner, StringComparison.OrdinalIgnoreCase))
                        issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated beam stirrup solid xung đột owner/project handle khác: " + owner, element.Id));
                    if (element.SourceHandles.Any(x => string.Equals((x ?? string.Empty).Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                        issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated beam stirrup handle không được nằm trong SourceHandles.", element.Id));
                    if (liveSolidHandles != null && !liveSolidHandles.Contains(handle))
                        issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy generated beam stirrup Solid3d: " + handle, element.Id));
                }

                if (!element.Properties.TryGetValue(CountKey, out var countText) ||
                    !int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0 || count != validCount)
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning, CountKey + " không khớp số handle hợp lệ.", element.Id));

                if (!element.Properties.TryGetValue(DiameterKey, out var diameterText) ||
                    !double.TryParse(diameterText, NumberStyles.Float, CultureInfo.InvariantCulture, out var diameter) ||
                    double.IsNaN(diameter) || double.IsInfinity(diameter) || diameter <= 0d)
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_DIAMETER_INVALID", HealthSeverity.Warning, DiameterKey + " thiếu hoặc không hợp lệ.", element.Id));

                if (element.Category != ElementCategory.Beam)
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_CATEGORY_MISMATCH", HealthSeverity.Error, "Generated beam stirrup metadata chỉ hợp lệ trên Beam element.", element.Id));

                if (element.Dirty != ElementDirtyFlags.None)
                    issues.Add(new ModelHealthIssue("BEAM_STIRRUP_GENERATED_STALE", HealthSeverity.Warning, "Beam đang dirty nhưng vẫn còn generated beam stirrup; rebuild/health-check trước khi phát hành bản vẽ.", element.Id));
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
