using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedRebarModeHealthService
    {
        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            foreach (var element in project.Elements)
            {
                if (!element.Properties.TryGetValue("GeneratedRebarHandles", out var handles) || string.IsNullOrWhiteSpace(handles)) continue;
                if (!element.Properties.TryGetValue("GeneratedRebarMode", out var rawMode) || string.IsNullOrWhiteSpace(rawMode))
                {
                    issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_MISSING", HealthSeverity.Warning, "GeneratedRebarHandles tồn tại nhưng thiếu GeneratedRebarMode.", element.Id));
                    continue;
                }

                var mode = rawMode.Trim();
                switch (mode.ToUpperInvariant())
                {
                    case "COLUMNVERTICALBARS":
                        RequireCategory(element, ElementCategory.Column, mode, issues);
                        RequirePositive(element, "GeneratedRebarDiameterMm", issues);
                        RequireNonNegative(element, "GeneratedRebarCoverM", issues);
                        break;
                    case "BEAMLONGITUDINALBARS":
                        RequireCategory(element, ElementCategory.Beam, mode, issues);
                        RequirePositive(element, "GeneratedRebarDiameterMm", issues);
                        RequireNonNegative(element, "GeneratedRebarCoverM", issues);
                        RequireNonNegative(element, "GeneratedRebarBeamEndCoverM", issues);
                        RequirePositiveInteger(element, "GeneratedRebarBeamTopCount", issues);
                        RequirePositiveInteger(element, "GeneratedRebarBeamBottomCount", issues);
                        break;
                    case "SLABMESHXY":
                        RequireCategory(element, ElementCategory.Slab, mode, issues);
                        RequirePositive(element, "GeneratedRebarDiameterMm", issues);
                        RequireNonNegative(element, "GeneratedRebarCoverM", issues);
                        RequirePositive(element, "GeneratedRebarSlabXActualSpacingM", issues);
                        RequirePositive(element, "GeneratedRebarSlabYActualSpacingM", issues);
                        RequireChoice(element, "GeneratedRebarSlabFaces", new[] { "Bottom", "Top", "Both" }, issues);
                        break;
                    case "STRUCTURALWALLMESH":
                        RequireCategory(element, ElementCategory.StructuralWall, mode, issues);
                        RequirePositive(element, "GeneratedRebarDiameterMm", issues);
                        RequireNonNegative(element, "GeneratedRebarCoverM", issues);
                        RequirePositive(element, "GeneratedRebarWallHorizontalActualSpacingM", issues);
                        RequirePositive(element, "GeneratedRebarWallVerticalActualSpacingM", issues);
                        RequireChoice(element, "GeneratedRebarWallFaces", new[] { "Near", "Far", "Both" }, issues);
                        break;
                    default:
                        issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_UNKNOWN", HealthSeverity.Warning, "GeneratedRebarMode chưa được health service nhận diện: " + mode, element.Id));
                        break;
                }
            }
            return issues.AsReadOnly();
        }

        private static void RequireCategory(ProjectElement element, ElementCategory expected, string mode, ICollection<ModelHealthIssue> issues)
        {
            if (element.Category == expected) return;
            issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_CATEGORY_MISMATCH", HealthSeverity.Error, "GeneratedRebarMode " + mode + " yêu cầu category " + expected + " nhưng element là " + element.Category + ".", element.Id));
        }

        private static void RequirePositive(ProjectElement element, string key, ICollection<ModelHealthIssue> issues)
        {
            if (TryNumber(element, key, out var value) && value > 0d) return;
            issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_METADATA_INVALID", HealthSeverity.Warning, key + " phải là số hữu hạn > 0 cho GeneratedRebarMode hiện tại.", element.Id));
        }

        private static void RequireNonNegative(ProjectElement element, string key, ICollection<ModelHealthIssue> issues)
        {
            if (TryNumber(element, key, out var value) && value >= 0d) return;
            issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_METADATA_INVALID", HealthSeverity.Warning, key + " phải là số hữu hạn >= 0 cho GeneratedRebarMode hiện tại.", element.Id));
        }

        private static void RequirePositiveInteger(ProjectElement element, string key, ICollection<ModelHealthIssue> issues)
        {
            if (element.Properties.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0) return;
            issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_METADATA_INVALID", HealthSeverity.Warning, key + " phải là integer > 0 cho GeneratedRebarMode hiện tại.", element.Id));
        }

        private static void RequireChoice(ProjectElement element, string key, IReadOnlyList<string> choices, ICollection<ModelHealthIssue> issues)
        {
            if (element.Properties.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
            {
                foreach (var choice in choices)
                    if (string.Equals(raw.Trim(), choice, StringComparison.OrdinalIgnoreCase)) return;
            }
            issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_METADATA_INVALID", HealthSeverity.Warning, key + " không hợp lệ cho GeneratedRebarMode hiện tại.", element.Id));
        }

        private static bool TryNumber(ProjectElement element, string key, out double value)
        {
            value = 0d;
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return false;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return false;
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
