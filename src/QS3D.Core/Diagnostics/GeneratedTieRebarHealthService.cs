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
        private const string CoverKey = "GeneratedTieRebarCoverM";
        private const string ModeKey = "GeneratedTieRebarMode";
        private const string CanonicalMode = "ColumnRectangularTies";

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var ownership = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Tie rebar health cannot inspect a null project element.");
                if (!element.Properties.TryGetValue(HandlesKey, out var raw)) continue;
                var handles = (raw ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.None);
                var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var valid = 0;
                foreach (var item in handles)
                {
                    var handleText = item ?? string.Empty;
                    var handle = handleText.Trim();
                    if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                    {
                        issues.Add(new ModelHealthIssue("INVALID_TIE_REBAR_GENERATED_HANDLE", HealthSeverity.Error, HandlesKey + " chứa handle không hợp lệ.", element.Id));
                        continue;
                    }
                    if (!string.Equals(handleText, handle, StringComparison.Ordinal))
                        issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_HANDLE_NON_CANONICAL", HealthSeverity.Error, HandlesKey + " không được có khoảng trắng đầu/cuối ở từng handle.", element.Id));
                    var identity = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);
                    if (!local.Add(identity))
                    {
                        issues.Add(new ModelHealthIssue("DUPLICATE_TIE_REBAR_GENERATED_HANDLE", HealthSeverity.Error, "Generated tie handle bị lặp: " + identity, element.Id));
                        continue;
                    }
                    valid++;
                    var expectedOwner = element.Id + "/" + HandlesKey;
                    if (ownership.IsConflicted(identity, expectedOwner))
                        issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated tie solid xung đột owner/project handle khác: " + ownership.Describe(identity), element.Id));
                    if (element.SourceHandles.Any(x => string.Equals(GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(x), identity, StringComparison.OrdinalIgnoreCase)))
                        issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated tie handle không được nằm trong SourceHandles.", element.Id));
                    if (liveSolidHandles != null && !liveSolidHandles.Contains(identity))
                        issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy generated tie Solid3d: " + identity, element.Id));
                }

                if (!element.Properties.TryGetValue("GeneratedTieRebarCount", out var countText) ||
                    !int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0 || count != valid)
                {
                    issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning, "GeneratedTieRebarCount thiếu hoặc không khớp số handle hợp lệ.", element.Id));
                }
                else
                {
                    var canonicalCount = count.ToString(CultureInfo.InvariantCulture);
                    if (!string.Equals(countText, canonicalCount, StringComparison.Ordinal))
                        issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_COUNT_NON_CANONICAL", HealthSeverity.Error, "GeneratedTieRebarCount phải dùng đúng invariant integer spelling: " + canonicalCount + ".", element.Id));
                }

                CheckPositive(element, "GeneratedTieRebarDiameterMm", "TIE_REBAR_GENERATED_DIAMETER_INVALID", "TIE_REBAR_GENERATED_DIAMETER_NON_CANONICAL", "GeneratedTieRebarDiameterMm thiếu hoặc không hợp lệ.", issues);
                CheckNonNegative(element, "GeneratedTieRebarActualSpacingM", "TIE_REBAR_GENERATED_SPACING_INVALID", "TIE_REBAR_GENERATED_SPACING_NON_CANONICAL", "GeneratedTieRebarActualSpacingM thiếu hoặc không hợp lệ.", issues);
                InspectCover(element, issues);
                InspectMode(element, issues);
                if (element.Category != ElementCategory.Column)
                    issues.Add(new ModelHealthIssue("TIE_REBAR_CATEGORY_MISMATCH", HealthSeverity.Error, "Generated tie metadata chỉ hợp lệ trên Column element.", element.Id));
                if (element.IsGeneratedTieRebarStale())
                    issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_STALE", HealthSeverity.Warning, "Generated tie snapshot không còn khớp semantic/source hiện tại; rebuild ties trước khi phát hành bản vẽ.", element.Id));
            }
            return issues.AsReadOnly();
        }

        private static void InspectCover(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(CoverKey, out var raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_COVER_INVALID", HealthSeverity.Warning, CoverKey + " thiếu hoặc phải là số hữu hạn >= 0.", element.Id));
                return;
            }

            var canonical = value.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_COVER_NON_CANONICAL", HealthSeverity.Error, CoverKey + " phải dùng đúng round-trip invariant numeric spelling: " + canonical + ".", element.Id));
        }

        private static void InspectMode(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            var raw = element.Properties.TryGetValue(ModeKey, out var stored) ? stored ?? string.Empty : string.Empty;
            var normalized = raw.Trim();
            if (!string.Equals(normalized, CanonicalMode, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_MODE_INVALID", HealthSeverity.Warning, ModeKey + " thiếu hoặc không phải mode được hỗ trợ " + CanonicalMode + ".", element.Id));
                return;
            }
            if (!string.Equals(raw, CanonicalMode, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("TIE_REBAR_GENERATED_MODE_NON_CANONICAL", HealthSeverity.Error, ModeKey + " phải dùng đúng writer-owned token: " + CanonicalMode + ".", element.Id));
        }

        private sealed class OwnershipIndex
        {
            public Dictionary<string, string> Owners { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Conflicts { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public bool IsConflicted(string handle, string expectedOwner)
            {
                if (Conflicts.Contains(handle)) return true;
                return Owners.TryGetValue(handle, out var owner) && !string.Equals(owner, expectedOwner, StringComparison.OrdinalIgnoreCase);
            }

            public string Describe(string handle)
            {
                if (Conflicts.Contains(handle)) return "multiple owners";
                return Owners.TryGetValue(handle, out var owner) ? owner : "unknown owner";
            }
        }

        private static OwnershipIndex BuildOwnershipIndex(ProjectState project)
        {
            var index = new OwnershipIndex();
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Tie rebar health cannot inspect a null project element.");
                foreach (var handle in element.SourceHandles) Reserve(index, handle, element.Id + "/SourceHandles");
                foreach (var property in element.Properties)
                {
                    if (!GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)) continue;
                    ReserveProperty(index, element, property.Key, property.Value);
                }
            }
            return index;
        }

        private static void ReserveProperty(OwnershipIndex index, ProjectElement element, string key, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
                Reserve(index, handle, element.Id + "/" + key);
        }

        private static void Reserve(OwnershipIndex index, string? handle, string token)
        {
            var normalized = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);
            if (normalized.Length == 0) return;
            if (!index.Owners.TryGetValue(normalized, out var existing))
            {
                index.Owners[normalized] = token;
                return;
            }
            if (!string.Equals(existing, token, StringComparison.OrdinalIgnoreCase))
                index.Conflicts.Add(normalized);
        }

        private static void CheckPositive(ProjectElement element, string key, string code, string canonicalCode, string message, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, message, element.Id));
                return;
            }
            ValidateNumericCanonicality(element, key, raw, value, canonicalCode, issues);
        }

        private static void CheckNonNegative(ProjectElement element, string key, string code, string canonicalCode, string message, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            {
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, message, element.Id));
                return;
            }
            ValidateNumericCanonicality(element, key, raw, value, canonicalCode, issues);
        }

        private static void ValidateNumericCanonicality(ProjectElement element, string key, string raw, double value, string code, ICollection<ModelHealthIssue> issues)
        {
            var canonical = value.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Error, key + " phải dùng đúng round-trip invariant numeric spelling: " + canonical + ".", element.Id));
        }
    }
}
