using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedRebarModeHealthService
    {
        private const string ShapeModeKey = "GeneratedShapeRebarMode";
        private const string ShapeMode = "BBS.ShapePath.SegmentedCylinder";

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Generated-rebar mode diagnostics cannot inspect a project containing a null semantic element.");
                InspectLongitudinal(element, issues);
                InspectShape(element, issues);
                InspectSlabMesh(element, issues);
                InspectWallMesh(element, issues);
                InspectFoundationMesh(element, issues);
            }
            return issues.AsReadOnly();
        }

        private static void InspectLongitudinal(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (!HasHandles(element, "GeneratedRebarHandles")) return;
            if (!element.Properties.TryGetValue("GeneratedRebarMode", out var rawMode) || string.IsNullOrWhiteSpace(rawMode))
            {
                issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_MISSING", HealthSeverity.Warning, "GeneratedRebarHandles tồn tại nhưng thiếu GeneratedRebarMode.", element.Id));
                return;
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
                default:
                    issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_UNKNOWN", HealthSeverity.Warning, "GeneratedRebarMode chưa được health service nhận diện: " + mode, element.Id));
                    break;
            }
        }

        private static void InspectShape(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (!HasHandles(element, "GeneratedShapeRebarHandles")) return;
            var raw = element.Properties.TryGetValue(ShapeModeKey, out var stored) ? stored ?? string.Empty : string.Empty;
            var normalized = raw.Trim();
            if (!string.Equals(normalized, ShapeMode, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_METADATA_INVALID", HealthSeverity.Warning, ShapeModeKey + " thiếu hoặc phải là " + ShapeMode + ".", element.Id));
                return;
            }
            if (!string.Equals(raw, ShapeMode, StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_METADATA_NON_CANONICAL", HealthSeverity.Error, ShapeModeKey + " phải dùng đúng writer-owned token: " + ShapeMode + ".", element.Id));
        }

        private static void InspectSlabMesh(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (!HasHandles(element, "GeneratedSlabMeshHandles")) return;
            RequireExactMode(element, "GeneratedSlabMeshMode", "SlabMeshXY", issues);
            RequireCategory(element, ElementCategory.Slab, "SlabMeshXY", issues);
            RequirePositive(element, "GeneratedSlabMeshXDiameterMm", issues);
            RequirePositive(element, "GeneratedSlabMeshYDiameterMm", issues);
            RequireNonNegative(element, "GeneratedSlabMeshCoverM", issues);
            RequirePositive(element, "GeneratedSlabMeshXActualSpacingM", issues);
            RequirePositive(element, "GeneratedSlabMeshYActualSpacingM", issues);
            RequireChoice(element, "GeneratedSlabMeshFaces", new[] { "Bottom", "Top", "Both" }, issues);
        }

        private static void InspectWallMesh(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (!HasHandles(element, "GeneratedWallMeshHandles")) return;
            RequireExactMode(element, "GeneratedWallMeshMode", "StructuralWallMesh", issues);
            RequireCategory(element, ElementCategory.StructuralWall, "StructuralWallMesh", issues);
            RequirePositive(element, "GeneratedWallMeshHorizontalDiameterMm", issues);
            RequirePositive(element, "GeneratedWallMeshVerticalDiameterMm", issues);
            RequireNonNegative(element, "GeneratedWallMeshCoverM", issues);
            RequirePositive(element, "GeneratedWallMeshHorizontalActualSpacingM", issues);
            RequirePositive(element, "GeneratedWallMeshVerticalActualSpacingM", issues);
            RequireChoice(element, "GeneratedWallMeshFaces", new[] { "Near", "Far", "Both" }, issues);
        }

        private static void InspectFoundationMesh(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (!HasHandles(element, "GeneratedFoundationMeshHandles")) return;
            RequireExactMode(element, "GeneratedFoundationMeshMode", "FoundationMeshXY", issues);
            RequireCategory(element, ElementCategory.Foundation, "FoundationMeshXY", issues);
            RequirePositive(element, "GeneratedFoundationMeshXDiameterMm", issues);
            RequirePositive(element, "GeneratedFoundationMeshYDiameterMm", issues);
            RequireNonNegative(element, "GeneratedFoundationMeshCoverM", issues);
            RequirePositive(element, "GeneratedFoundationMeshXActualSpacingM", issues);
            RequirePositive(element, "GeneratedFoundationMeshYActualSpacingM", issues);
            RequireChoice(element, "GeneratedFoundationMeshFaces", new[] { "Bottom", "Top", "Both" }, issues);
        }

        private static bool HasHandles(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var handles) && !string.IsNullOrWhiteSpace(handles);

        private static void RequireExactMode(ProjectElement element, string key, string expected, ICollection<ModelHealthIssue> issues)
        {
            if (element.Properties.TryGetValue(key, out var raw) && string.Equals((raw ?? string.Empty).Trim(), expected, StringComparison.OrdinalIgnoreCase)) return;
            issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_METADATA_INVALID", HealthSeverity.Warning, key + " phải là " + expected + ".", element.Id));
        }

        private static void RequireCategory(ProjectElement element, ElementCategory expected, string mode, ICollection<ModelHealthIssue> issues)
        {
            if (element.Category == expected) return;
            issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_CATEGORY_MISMATCH", HealthSeverity.Error, "Generated mode " + mode + " yêu cầu category " + expected + " nhưng element là " + element.Category + ".", element.Id));
        }

        private static void RequirePositive(ProjectElement element, string key, ICollection<ModelHealthIssue> issues)
        {
            if (TryNumber(element, key, out var value) && value > 0d) return;
            issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_METADATA_INVALID", HealthSeverity.Warning, key + " phải là số hữu hạn > 0 cho generated rebar mode hiện tại.", element.Id));
        }

        private static void RequireNonNegative(ProjectElement element, string key, ICollection<ModelHealthIssue> issues)
        {
            if (TryNumber(element, key, out var value) && value >= 0d) return;
            issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_METADATA_INVALID", HealthSeverity.Warning, key + " phải là số hữu hạn >= 0 cho generated rebar mode hiện tại.", element.Id));
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
            issues.Add(new ModelHealthIssue("GENERATED_REBAR_MODE_METADATA_INVALID", HealthSeverity.Warning, key + " không hợp lệ cho generated rebar mode hiện tại.", element.Id));
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
