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
            var owners = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
            {
                InspectSet(element, ColumnSpec, liveColumnSolidHandles, owners, issues);
                InspectSet(element, ShapeSpec, null, owners, issues);
            }
            return issues;
        }

        public IReadOnlyList<ModelHealthIssue> InspectShape(ProjectState project, ISet<string>? liveShapeSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var owners = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
                InspectSet(element, ShapeSpec, liveShapeSolidHandles, owners, issues);
            return issues;
        }

        public IReadOnlyList<ModelHealthIssue> InspectAll(ProjectState project, ISet<string>? liveColumnSolidHandles, ISet<string>? liveShapeSolidHandles)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var owners = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
            {
                InspectSet(element, ColumnSpec, liveColumnSolidHandles, owners, issues);
                InspectSet(element, ShapeSpec, liveShapeSolidHandles, owners, issues);
            }
            return issues;
        }

        private static Dictionary<string, string> BuildOwnershipIndex(ProjectState project)
        {
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                foreach (var sourceHandle in element.SourceHandles)
                    Reserve(owners, sourceHandle, element.Id + "/SourceHandles");
                ReserveProperty(owners, element, "GeneratedSolidHandle");
                ReserveProperty(owners, element, "PhysicalOpeningCutSolidHandle");
            }
            foreach (var element in project.Elements)
            {
                ReserveProperty(owners, element, ColumnSpec.HandlesKey);
                ReserveProperty(owners, element, ShapeSpec.HandlesKey);
                ReserveProperty(owners, element, "GeneratedTieRebarHandles");
                ReserveProperty(owners, element, "GeneratedBeamStirrupHandles");
            }
            return owners;
        }

        private static void ReserveProperty(Dictionary<string, string> owners, ProjectElement element, string propertyKey)
        {
            if (!element.Properties.TryGetValue(propertyKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in SplitHandles(raw)) Reserve(owners, handle, element.Id + "/" + propertyKey);
        }

        private static void Reserve(Dictionary<string, string> owners, string? handle, string token)
        {
            var normalized = (handle ?? string.Empty).Trim();
            if (normalized.Length == 0 || owners.ContainsKey(normalized)) return;
            owners[normalized] = token;
        }

        private static void InspectSet(ProjectElement element, HandleSetSpec spec, ISet<string>? liveSolidHandles, Dictionary<string, string> owners, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(spec.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            var handles = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var validCount = 0;
            foreach (var item in handles)
            {
                var handle = (item ?? string.Empty).Trim();
                if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                {
                    issues.Add(new ModelHealthIssue("INVALID_" + spec.CodePrefix + "_GENERATED_HANDLE", HealthSeverity.Error, spec.HandlesKey + " chứa handle không hợp lệ.", element.Id));
                    continue;
                }
                if (!local.Add(handle))
                {
                    issues.Add(new ModelHealthIssue("DUPLICATE_" + spec.CodePrefix + "_GENERATED_HANDLE", HealthSeverity.Error, "Một " + spec.DisplayName + " handle bị lặp trong cùng element: " + handle, element.Id));
                    continue;
                }
                validCount++;
                var ownerToken = element.Id + "/" + spec.HandlesKey;
                if (owners.TryGetValue(handle, out var owner) && !string.Equals(owner, ownerToken, StringComparison.OrdinalIgnoreCase))
                    issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated rebar solid đang xung đột với owner/project handle khác: " + owner, element.Id));
                if (element.SourceHandles.Any(x => string.Equals((x ?? string.Empty).Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                    issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated rebar handle không được nằm trong SourceHandles.", element.Id));
                if (liveSolidHandles != null && !liveSolidHandles.Contains(handle))
                    issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy generated " + spec.DisplayName + " Solid3d: " + handle, element.Id));
            }

            if (element.Properties.TryGetValue(spec.CountKey, out var countText))
            {
                if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0 || count != validCount)
                    issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning, spec.CountKey + " không khớp số handle hợp lệ.", element.Id));
            }
            else issues.Add(new ModelHealthIssue(spec.CodePrefix + "_GENERATED_COUNT_MISSING", HealthSeverity.Warning, "Thiếu " + spec.CountKey + ".", element.Id));

            if (spec.RequiresSingleDiameter &&
                (!element.Properties.TryGetValue("GeneratedRebarDiameterMm", out var diameterText) ||
                 !double.TryParse(diameterText, NumberStyles.Float, CultureInfo.InvariantCulture, out var diameter) ||
                 double.IsNaN(diameter) || double.IsInfinity(diameter) || diameter <= 0d))
                issues.Add(new ModelHealthIssue("REBAR_GENERATED_DIAMETER_INVALID", HealthSeverity.Warning, "GeneratedRebarDiameterMm thiếu hoặc không hợp lệ.", element.Id));
        }

        private static IEnumerable<string> SplitHandles(string raw) =>
            (raw ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
