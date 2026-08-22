using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedRebarHealthService
    {
        private sealed class HandleSetSpec
        {
            public string HandlesKey { get; set; } = string.Empty;
            public string CountKey { get; set; } = string.Empty;
            public string CodePrefix { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public bool RequiresSingleDiameter { get; set; }
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

        private static readonly HandleSetSpec ColumnSpec = new HandleSetSpec
        {
            HandlesKey = "GeneratedRebarHandles",
            CountKey = "GeneratedRebarCount",
            CodePrefix = "REBAR",
            DisplayName = "column/beam longitudinal rebar",
            RequiresSingleDiameter = true
        };

        private static readonly HandleSetSpec ShapeSpec = new HandleSetSpec
        {
            HandlesKey = "GeneratedShapeRebarHandles",
            CountKey = "GeneratedShapeRebarCount",
            CodePrefix = "SHAPE_REBAR",
            DisplayName = "shape rebar",
            RequiresSingleDiameter = false
        };

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveColumnSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var ownership = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Generated rebar health cannot inspect a null project element.");
                InspectSet(element, ColumnSpec, liveColumnSolidHandles, ownership, issues);
                InspectSet(element, ShapeSpec, null, ownership, issues);
            }
            return issues.AsReadOnly();
        }

        public IReadOnlyList<ModelHealthIssue> InspectShape(ProjectState project, ISet<string>? liveShapeSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var ownership = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Generated rebar health cannot inspect a null project element.");
                InspectSet(element, ShapeSpec, liveShapeSolidHandles, ownership, issues);
            }
            return issues.AsReadOnly();
        }

        public IReadOnlyList<ModelHealthIssue> InspectAll(ProjectState project, ISet<string>? liveColumnSolidHandles, ISet<string>? liveShapeSolidHandles)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var ownership = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Generated rebar health cannot inspect a null project element.");
                InspectSet(element, ColumnSpec, liveColumnSolidHandles, ownership, issues);
                InspectSet(element, ShapeSpec, liveShapeSolidHandles, ownership, issues);
            }
            return issues.AsReadOnly();
        }

        private static OwnershipIndex BuildOwnershipIndex(ProjectState project)
        {
            var index = new OwnershipIndex();
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Generated rebar health cannot inspect a null project element.");
                foreach (var sourceHandle in element.SourceHandles)
                    Reserve(index, sourceHandle, element.Id + "/SourceHandles");
                foreach (var property in element.Properties)
                {
                    if (!GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)) continue;
                    ReserveProperty(index, element, property.Key, property.Value);
                }
            }
            return index;
        }

        private static void ReserveProperty(OwnershipIndex index, ProjectElement element, string propertyKey, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in SplitHandles(raw)) Reserve(index, handle, element.Id + "/" + propertyKey);
        }

        private static void Reserve(OwnershipIndex index, string? handle, string token)
        {
            var normalized = (handle ?? string.Empty).Trim();
            if (normalized.Length == 0) return;
            if (!index.Owners.TryGetValue(normalized, out var existing))
            {
                index.Owners[normalized] = token;
                return;
            }
            if (!string.Equals(existing, token, StringComparison.OrdinalIgnoreCase))
                index.Conflicts.Add(normalized);
        }

        private static void InspectSet(ProjectElement element, HandleSetSpec spec, ISet<string>? liveSolidHandles, OwnershipIndex ownership, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(spec.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            var handles = raw.Split(new[] { ';' }, StringSplitOptions.None);
            var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var validCount = 0;
            foreach (var item in handles)
            {
                var handleText = item ?? string.Empty;
                var handle = handleText.Trim();
                if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                {
                    issues.Add(new ModelHealthIssue("INVALID_" + spec.CodePrefix + "_GENERATED_HANDLE", HealthSeverity.Error, spec.HandlesKey + " chứa handle không hợp lệ.", element.Id));
                    continue;
                }
                if (!string.Equals(handleText, handle, StringComparison.Ordinal))
                    issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_HANDLE_NON_CANONICAL", HealthSeverity.Error, spec.HandlesKey + " không được có khoảng trắng đầu/cuối ở từng handle.", element.Id));
                if (!local.Add(handle))
                {
                    issues.Add(new ModelHealthIssue("DUPLICATE_" + spec.CodePrefix + "_GENERATED_HANDLE", HealthSeverity.Error, "Một " + spec.DisplayName + " handle bị lặp trong cùng element: " + handle, element.Id));
                    continue;
                }
                validCount++;
                var ownerToken = element.Id + "/" + spec.HandlesKey;
                if (ownership.IsConflicted(handle, ownerToken))
                    issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated rebar solid đang xung đột với owner/project handle khác: " + ownership.Describe(handle), element.Id));
                if (element.SourceHandles.Any(x => string.Equals((x ?? string.Empty).Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                    issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated rebar handle không được nằm trong SourceHandles.", element.Id));
                if (liveSolidHandles != null && !liveSolidHandles.Contains(handle))
                    issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy generated " + spec.DisplayName + " Solid3d: " + handle, element.Id));
            }

            if (element.Properties.TryGetValue(spec.CountKey, out var countText))
            {
                if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0 || count != validCount)
                {
                    issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning, spec.CountKey + " không khớp số handle hợp lệ.", element.Id));
                }
                else
                {
                    var canonicalCount = count.ToString(CultureInfo.InvariantCulture);
                    if (!string.Equals(countText, canonicalCount, StringComparison.Ordinal))
                        issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_COUNT_NON_CANONICAL", HealthSeverity.Error, spec.CountKey + " phải dùng đúng invariant integer spelling: " + canonicalCount + ".", element.Id));
                }
            }
            else issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_COUNT_MISSING", HealthSeverity.Warning, "Thiếu " + spec.CountKey + ".", element.Id));

            if (spec.RequiresSingleDiameter)
            {
                if (!element.Properties.TryGetValue("GeneratedRebarDiameterMm", out var diameterText) ||
                    !double.TryParse(diameterText, NumberStyles.Float, CultureInfo.InvariantCulture, out var diameter) ||
                    double.IsNaN(diameter) || double.IsInfinity(diameter) || diameter <= 0d)
                {
                    issues.Add(new ModelHealthIssue("REBAR_GENERATED_DIAMETER_INVALID", HealthSeverity.Warning, "GeneratedRebarDiameterMm thiếu hoặc không hợp lệ.", element.Id));
                }
                else
                {
                    var canonicalDiameter = diameter.ToString("R", CultureInfo.InvariantCulture);
                    if (!string.Equals(diameterText, canonicalDiameter, StringComparison.Ordinal))
                        issues.Add(new ModelHealthIssue("REBAR_GENERATED_DIAMETER_NON_CANONICAL", HealthSeverity.Error, "GeneratedRebarDiameterMm phải dùng đúng round-trip invariant numeric spelling: " + canonicalDiameter + ".", element.Id));
                }
            }
        }

        private static IEnumerable<string> SplitHandles(string raw) =>
            (raw ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
