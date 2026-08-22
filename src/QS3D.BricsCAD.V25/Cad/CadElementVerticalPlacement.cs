using System;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.Cad
{
    /// <summary>
    /// Converts the Core Level contract into drawing-unit coordinates. Native builders must use
    /// this adapter instead of repeating BottomLevel/TopLevel arithmetic or adding BottomOffsetM.
    /// </summary>
    internal sealed class CadElementVerticalPlacement
    {
        private CadElementVerticalPlacement(
            Document document,
            ProjectElement element,
            ElementVerticalPlacement placement,
            double? legacySourceBaseDrawing,
            double? legacyHeightM,
            double? legacyBottomOffsetM)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (element == null) throw new ArgumentNullException(nameof(element));
            Placement = placement ?? throw new ArgumentNullException(nameof(placement));
            LegacyHeightM = legacyHeightM;
            LegacyBottomOffsetM = legacyBottomOffsetM;
            if (placement.UsesBottomLevel)
            {
                BottomDrawing = CadGeometryGuard.ToDrawingUnits(document, placement.BottomElevationM, element.Id + "/resolved bottom elevation");
            }
            else
            {
                if (!legacySourceBaseDrawing.HasValue || !legacyBottomOffsetM.HasValue)
                    throw new InvalidOperationException(element.Id + " legacy placement inputs were not consumed.");
                BottomDrawing = CadGeometryGuard.Add(
                    CadGeometryGuard.Finite(legacySourceBaseDrawing.Value, element.Id + "/legacy source base elevation"),
                    CadGeometryGuard.ToDrawingUnits(document, legacyBottomOffsetM.Value, element.Id + "/legacy bottom offset"),
                    element.Id + "/legacy bottom elevation");
            }
            HeightDrawing = CadGeometryGuard.Positive(
                CadGeometryGuard.ToDrawingUnits(document, placement.HeightM, element.Id + "/resolved height"),
                element.Id + "/resolved drawing height");
            CenterDrawing = CadGeometryGuard.Add(BottomDrawing, HeightDrawing / 2d, element.Id + "/resolved center elevation");
        }

        public ElementVerticalPlacement Placement { get; }
        public bool UsesBottomLevel => Placement.UsesBottomLevel;
        public bool UsesTopLevel => Placement.UsesTopLevel;
        public double? LegacyHeightM { get; }
        public double? LegacyBottomOffsetM { get; }
        public double BottomElevationM => Placement.BottomElevationM;
        public double TopElevationM => Placement.TopElevationM;
        public double HeightM => Placement.HeightM;
        public double BottomDrawing { get; }
        public double HeightDrawing { get; }
        public double CenterDrawing { get; }

        // Existing fingerprints store a source-relative BottomOffsetM. Keep that exact token for
        // legacy elements, while Level-enabled elements use their absolute Level bottom so Core
        // health checks can recompute the value without opening the DWG source entity.
        public double FingerprintBottomM => UsesBottomLevel
            ? BottomElevationM
            : LegacyBottomOffsetM ?? throw new InvalidOperationException("Legacy placement is missing BottomOffsetM.");

        public static CadElementVerticalPlacement Resolve(
            Document document,
            ProjectState project,
            ProjectElement element,
            ProjectFamily? family,
            double sourceBaseDrawing,
            string legacyHeightKey,
            double legacyHeightFallback,
            string legacyBottomOffsetKey = "BottomOffsetM",
            double legacyBottomOffsetFallback = 0d)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(legacyHeightKey)) throw new ArgumentException("Legacy height key is required.", nameof(legacyHeightKey));
            if (string.IsNullOrWhiteSpace(legacyBottomOffsetKey)) throw new ArgumentException("Legacy bottom-offset key is required.", nameof(legacyBottomOffsetKey));

            // This is the final shared gate before native geometry consumes vertical metadata.
            // Keep imported or hand-edited Level properties fail-closed on unsupported categories.
            LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, "Native 3D Level placement");

            var bottomLevelId = Property(element, ProjectFloorService.BottomLevelIdKey);
            var topLevelId = Property(element, ProjectFloorService.TopLevelIdKey);

            ElementVerticalPlacement resolved;
            double? consumedLegacySourceBaseDrawing = null;
            double? consumedLegacyHeightM = null;
            double? consumedLegacyBottomOffsetM = null;
            if (bottomLevelId.Length == 0)
            {
                if (HasAnyLevelConfiguration(element))
                {
                    // Let the Core resolver emit the canonical fail-closed error before touching
                    // legacy inputs that this malformed Level branch must not consume.
                    resolved = ElementVerticalPlacementService.Resolve(project, element, double.NaN, double.NaN, double.NaN);
                }
                else
                {
                    var sourceBaseM = CadGeometryGuard.ToMeters(document, sourceBaseDrawing, element.Id + "/source base elevation");
                    var legacyHeightM = CadGeometryGuard.Number(element, family, legacyHeightKey, legacyHeightFallback);
                    var legacyBottomOffsetM = CadGeometryGuard.Number(element, family, legacyBottomOffsetKey, legacyBottomOffsetFallback);
                    consumedLegacySourceBaseDrawing = sourceBaseDrawing;
                    consumedLegacyHeightM = legacyHeightM;
                    consumedLegacyBottomOffsetM = legacyBottomOffsetM;
                    resolved = ElementVerticalPlacementService.Resolve(project, element, sourceBaseM, legacyHeightM, legacyBottomOffsetM);
                }
            }
            else if (topLevelId.Length == 0)
            {
                // First validate the Level reference and Top-offset shape without reading the
                // legacy height. Missing/ambiguous Levels therefore remain the root error.
                ElementVerticalPlacementService.Resolve(project, element, double.NaN, 1d, double.NaN);
                var legacyHeightM = CadGeometryGuard.Number(element, family, legacyHeightKey, legacyHeightFallback);
                consumedLegacyHeightM = legacyHeightM;
                resolved = ElementVerticalPlacementService.Resolve(project, element, double.NaN, legacyHeightM, double.NaN);
            }
            else
            {
                // A Bottom+Top range owns the effective height and intentionally ignores every
                // legacy source/height/BottomOffset input.
                resolved = ElementVerticalPlacementService.Resolve(project, element, double.NaN, double.NaN, double.NaN);
            }

            return new CadElementVerticalPlacement(
                document,
                element,
                resolved,
                consumedLegacySourceBaseDrawing,
                consumedLegacyHeightM,
                consumedLegacyBottomOffsetM);
        }

        internal static CadElementVerticalPlacement ResolveExplicitLegacy(
            Document document,
            ProjectState project,
            ProjectElement element,
            double sourceBaseDrawing,
            double legacyHeightM,
            double legacyBottomOffsetM)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, "Native 3D Level placement");

            var sourceBaseM = CadGeometryGuard.ToMeters(document, sourceBaseDrawing, element.Id + "/source base elevation");
            var resolved = ElementVerticalPlacementService.Resolve(
                project,
                element,
                sourceBaseM,
                legacyHeightM,
                legacyBottomOffsetM);
            return new CadElementVerticalPlacement(
                document,
                element,
                resolved,
                sourceBaseDrawing,
                legacyHeightM,
                legacyBottomOffsetM);
        }

        public static bool HasAnyLevelConfiguration(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return ElementVerticalPlacementService.HasAnyLevelConfiguration(element);
        }

        public static void CommitSnapshot(ProjectElement element, string prefix, CadElementVerticalPlacement placement)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Vertical snapshot prefix is required.", nameof(prefix));
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            var key = prefix.Trim();
            element.Properties[key + "VerticalBottomM"] = Number(placement.BottomElevationM);
            element.Properties[key + "VerticalTopM"] = Number(placement.TopElevationM);
            element.Properties[key + "VerticalHeightM"] = Number(placement.HeightM);
            element.Properties[key + "VerticalMode"] = placement.UsesTopLevel
                ? "BottomTopLevels"
                : placement.UsesBottomLevel ? "BottomLevel" : "LegacySourceRelative";
        }

        public static void ClearSnapshot(ProjectElement element, string prefix)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Vertical snapshot prefix is required.", nameof(prefix));
            var key = prefix.Trim();
            element.Properties.Remove(key + "VerticalBottomM");
            element.Properties.Remove(key + "VerticalTopM");
            element.Properties.Remove(key + "VerticalHeightM");
            element.Properties.Remove(key + "VerticalMode");
        }

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;

        private static string Number(double value) =>
            CadGeometryGuard.Finite(value, "vertical placement snapshot").ToString("R", CultureInfo.InvariantCulture);
    }

    internal sealed class CadHostedOpeningVerticalPlacement
    {
        private CadHostedOpeningVerticalPlacement(double heightM, double sillHeightM, double bottomElevationM)
        {
            HeightM = CadGeometryGuard.Positive(heightM, "hosted opening height");
            SillHeightM = CadGeometryGuard.Finite(sillHeightM, "hosted opening sill height");
            if (SillHeightM < 0d) throw new InvalidOperationException("Hosted opening sill must be >= 0 relative to its host bottom.");
            BottomElevationM = CadGeometryGuard.Finite(bottomElevationM, "hosted opening bottom elevation");
        }

        public double HeightM { get; }
        public double SillHeightM { get; }
        public double BottomElevationM { get; }

        public static CadHostedOpeningVerticalPlacement Resolve(
            Document document,
            ProjectState project,
            ProjectElement opening,
            ProjectFamily? family,
            double openingSourceBaseDrawing,
            CadElementVerticalPlacement host,
            double legacyHeightFallback,
            double legacySillFallback)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (opening == null) throw new ArgumentNullException(nameof(opening));
            if (host == null) throw new ArgumentNullException(nameof(host));
            _ = openingSourceBaseDrawing;

            LevelReferenceNativeIntegrationPolicy.EnsureQualified(opening, "Hosted opening Level placement");

            var bottomLevelId = Property(opening, ProjectFloorService.BottomLevelIdKey);
            var topLevelId = Property(opening, ProjectFloorService.TopLevelIdKey);
            double legacyHeightM = double.NaN;
            double legacySillM = double.NaN;
            if (!CadElementVerticalPlacement.HasAnyLevelConfiguration(opening))
            {
                legacyHeightM = CadGeometryGuard.Number(opening, family, "HeightM", legacyHeightFallback);
                legacySillM = CadGeometryGuard.Number(
                    opening,
                    family,
                    "SillHeightM",
                    CadGeometryGuard.Number(opening, family, "BottomOffsetM", legacySillFallback));
            }
            else if (bottomLevelId.Length > 0 && topLevelId.Length == 0)
            {
                // Bottom-only placement still consumes legacy height, but never legacy sill or
                // source Z. Top-only/malformed and Bottom+Top branches consume neither value.
                legacyHeightM = CadGeometryGuard.Number(opening, family, "HeightM", legacyHeightFallback);
            }

            var placement = ElementVerticalPlacementService.ResolveHostedOpening(
                project,
                host.Placement,
                opening,
                legacyHeightM,
                legacySillM);
            return new CadHostedOpeningVerticalPlacement(
                placement.Opening.HeightM,
                placement.RelativeSillM,
                placement.Opening.BottomElevationM);
        }

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;
    }

    // Compatibility facade for automation-only legacy/no-Level probes that were merged while the
    // complete Level chain was being integrated. Product builders use CadElementVerticalPlacement;
    // this facade forwards to that same implementation and owns no independent Level arithmetic.
    internal sealed class CadVerticalPlacement
    {
        public CadVerticalPlacement(CadElementVerticalPlacement placement)
        {
            Placement = placement ?? throw new ArgumentNullException(nameof(placement));
        }

        private CadElementVerticalPlacement Placement { get; }
        public ElementVerticalPlacement Semantic => Placement.Placement;
        public double BottomDrawingUnits => Placement.BottomDrawing;
        public double HeightDrawingUnits => Placement.HeightDrawing;
        public double BottomElevationM => Placement.BottomElevationM;
        public double HeightM => Placement.HeightM;
    }

    internal static class CadVerticalPlacementResolver
    {
        public static CadVerticalPlacement Resolve(
            Document document,
            ProjectState project,
            ProjectElement element,
            double sourceBaseDrawingUnits,
            double legacyHeightM,
            double legacyBottomOffsetM)
        {
            return new CadVerticalPlacement(CadElementVerticalPlacement.ResolveExplicitLegacy(
                document,
                project,
                element,
                sourceBaseDrawingUnits,
                legacyHeightM,
                legacyBottomOffsetM));
        }

        public static CadHostedOpeningVerticalPlacement ResolveHostedOpening(
            Document document,
            ProjectState project,
            ProjectElement opening,
            ProjectFamily? family,
            double openingSourceBaseDrawing,
            CadElementVerticalPlacement host,
            double legacyHeightFallback,
            double legacySillFallback)
        {
            return CadHostedOpeningVerticalPlacement.Resolve(
                document,
                project,
                opening,
                family,
                openingSourceBaseDrawing,
                host,
                legacyHeightFallback,
                legacySillFallback);
        }

        public static bool HasConfiguredLevel(ProjectElement element) =>
            CadElementVerticalPlacement.HasAnyLevelConfiguration(element);
    }
}
