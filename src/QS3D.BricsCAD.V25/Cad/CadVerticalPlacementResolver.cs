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
    }
}
