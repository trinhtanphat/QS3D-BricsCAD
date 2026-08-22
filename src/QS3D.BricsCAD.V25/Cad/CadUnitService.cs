using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using QS3D.Core.Units;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CadUnitService
    {
        public static ProjectUnitPolicy GetPolicy(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!TryGetPolicy(document, out var policy, out _))
                throw new InvalidOperationException("Drawing units are unresolved. Set INSUNITS or run QS3DUNITS before creating, capturing, reconciling, or exporting quantity data.");
            return policy;
        }

        public static bool TryGetPolicy(Document document, out ProjectUnitPolicy policy, out DrawingUnitResolution resolution)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            IDictionary<string, string> metadata;
            var hasElements = false;
            if (ProjectContextCoordinator.TryGetReadOnly(document, out var project))
            {
                metadata = project.Metadata;
                hasElements = project.Elements.Count > 0;
            }
            else
            {
                // Resolving a native INSUNITS value is an inspection operation. A
                // unit-dependent read command must not bootstrap/cache a QS3D
                // project merely because the drawing has no sidecar yet.
                metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            var native = TryGetNativeLengthUnit(document, out var nativeUnit) ? nativeUnit : (LengthUnit?)null;
            if (!DrawingUnitResolutionPolicy.TryResolve(native, metadata, out resolution))
            {
                policy = null!;
                return false;
            }
            DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(metadata, hasElements, resolution.Unit);
            policy = new ProjectUnitPolicy(resolution.Unit);
            return true;
        }

        public static bool TryGetNativeLengthUnit(Document document, out LengthUnit unit)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            switch ((int)document.Database.Insunits)
            {
                case 1: unit = LengthUnit.Inch; return true;
                case 2: unit = LengthUnit.Foot; return true;
                case 3: unit = LengthUnit.Mile; return true;
                case 4: unit = LengthUnit.Millimeter; return true;
                case 5: unit = LengthUnit.Centimeter; return true;
                case 6: unit = LengthUnit.Meter; return true;
                case 7: unit = LengthUnit.Kilometer; return true;
                case 8: unit = LengthUnit.Microinch; return true;
                case 9: unit = LengthUnit.Mil; return true;
                case 10: unit = LengthUnit.Yard; return true;
                case 11: unit = LengthUnit.Angstrom; return true;
                case 12: unit = LengthUnit.Nanometer; return true;
                case 13: unit = LengthUnit.Micrometer; return true;
                case 14: unit = LengthUnit.Decimeter; return true;
                case 15: unit = LengthUnit.Decameter; return true;
                case 16: unit = LengthUnit.Hectometer; return true;
                case 17: unit = LengthUnit.Gigameter; return true;
                case 18: unit = LengthUnit.AstronomicalUnit; return true;
                case 19: unit = LengthUnit.LightYear; return true;
                case 20: unit = LengthUnit.Parsec; return true;
                case 21: unit = LengthUnit.USSurveyFoot; return true;
                case 22: unit = LengthUnit.USSurveyInch; return true;
                case 23: unit = LengthUnit.USSurveyYard; return true;
                case 24: unit = LengthUnit.USSurveyMile; return true;
                default: unit = default(LengthUnit); return false;
            }
        }

        public static LengthUnit GetLengthUnit(Document document) => GetPolicy(document).DrawingUnit;
        public static DrawingUnit GetDrawingUnit(Document document) => ProjectUnitPolicy.ToDrawingUnit(GetLengthUnit(document));

        public static double MetersToDrawingUnits(Document document, double meters) => GetPolicy(document).FromMeters(meters);
        public static double DrawingUnitsToMeters(Document document, double value) => GetPolicy(document).ToMeters(value);
        public static string Describe(Document document)
        {
            if (!TryGetPolicy(document, out _, out var resolution)) return "Unresolved";
            return resolution.Unit + (resolution.Source == DrawingUnitResolutionSource.ProjectOverride ? " (project override)" : " (INSUNITS)");
        }
    }
}
