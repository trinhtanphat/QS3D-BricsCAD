using System;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class CadVerticalPlacement
    {
        public CadVerticalPlacement(ElementVerticalPlacement semantic, double bottomDrawingUnits, double heightDrawingUnits)
        {
            Semantic = semantic ?? throw new ArgumentNullException(nameof(semantic));
            BottomDrawingUnits = bottomDrawingUnits;
            HeightDrawingUnits = heightDrawingUnits;
        }

        public ElementVerticalPlacement Semantic { get; }
        public double BottomDrawingUnits { get; }
        public double HeightDrawingUnits { get; }
        public double BottomElevationM => Semantic.BottomElevationM;
        public double HeightM => Semantic.HeightM;
    }

    internal sealed class CadHostedOpeningPlacement
    {
        public CadHostedOpeningPlacement(
            CadVerticalPlacement host,
            CadVerticalPlacement opening,
            double relativeSillM)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Opening = opening ?? throw new ArgumentNullException(nameof(opening));
            relativeSillM = CadGeometryGuard.Finite(relativeSillM, "hosted opening relative sill");
            if (relativeSillM < 0d) throw new InvalidOperationException("Hosted opening relative sill must be >= 0.");
            RelativeSillM = relativeSillM;
        }

        public CadVerticalPlacement Host { get; }
        public CadVerticalPlacement Opening { get; }
        public double RelativeSillM { get; }
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
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));

            var sourceBaseM = CadGeometryGuard.ToMeters(document, sourceBaseDrawingUnits, element.Id + "/source base elevation");
            var semantic = ElementVerticalPlacementService.Resolve(project, element, sourceBaseM, legacyHeightM, legacyBottomOffsetM);
            LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, "Native 3D Level placement");
            return ToCadPlacement(document, element, semantic, sourceBaseDrawingUnits, legacyBottomOffsetM);
        }

        public static CadHostedOpeningPlacement ResolveHostedOpening(
            Document document,
            ProjectState project,
            ProjectElement host,
            ProjectElement opening,
            double hostSourceBaseDrawingUnits,
            double hostLegacyHeightM,
            double hostLegacyBottomOffsetM,
            double openingLegacyHeightM,
            double openingLegacySillM)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (opening == null) throw new ArgumentNullException(nameof(opening));

            var hostSourceBaseM = CadGeometryGuard.ToMeters(document, hostSourceBaseDrawingUnits, host.Id + "/source base elevation");
            var semantic = ElementVerticalPlacementService.ResolveHostedOpening(
                project,
                host,
                opening,
                hostSourceBaseM,
                hostLegacyHeightM,
                hostLegacyBottomOffsetM,
                openingLegacyHeightM,
                openingLegacySillM);
            LevelReferenceNativeIntegrationPolicy.EnsureQualified(host, "Hosted opening host Level placement");
            LevelReferenceNativeIntegrationPolicy.EnsureQualified(opening, "Hosted opening Level placement");
            var hostCad = ToCadPlacement(document, host, semantic.Host, hostSourceBaseDrawingUnits, hostLegacyBottomOffsetM);
            var openingCad = ToCadPlacement(document, opening, semantic.Opening, hostCad.BottomDrawingUnits, openingLegacySillM);
            return new CadHostedOpeningPlacement(hostCad, openingCad, semantic.RelativeSillM);
        }

        public static bool HasConfiguredLevel(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return HasValue(element, ProjectFloorService.BottomLevelIdKey) ||
                   HasValue(element, ProjectFloorService.TopLevelIdKey) ||
                   HasValue(element, ProjectFloorService.BottomLevelOffsetKey) ||
                   HasValue(element, ProjectFloorService.TopLevelOffsetKey);
        }

        private static CadVerticalPlacement ToCadPlacement(
            Document document,
            ProjectElement element,
            ElementVerticalPlacement semantic,
            double sourceBaseDrawingUnits,
            double legacyBottomOffsetM)
        {
            var bottomDrawing = semantic.UsesBottomLevel
                ? CadGeometryGuard.ToDrawingUnits(document, semantic.BottomElevationM, element.Id + "/resolved bottom elevation")
                : CadGeometryGuard.Add(
                    sourceBaseDrawingUnits,
                    CadGeometryGuard.ToDrawingUnits(document, legacyBottomOffsetM, element.Id + "/legacy bottom offset"),
                    element.Id + "/legacy bottom elevation");
            var heightDrawing = CadGeometryGuard.Positive(
                CadGeometryGuard.ToDrawingUnits(document, semantic.HeightM, element.Id + "/resolved height"),
                element.Id + "/resolved height drawing units");
            return new CadVerticalPlacement(semantic, bottomDrawing, heightDrawing);
        }

        private static bool HasValue(ProjectElement element, string key)
        {
            return element.Properties.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw);
        }
    }
}
