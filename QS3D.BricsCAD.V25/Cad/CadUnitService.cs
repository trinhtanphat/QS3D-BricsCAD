using System;
using Bricscad.ApplicationServices;
using QS3D.Core.Units;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CadUnitService
    {
        public static ProjectUnitPolicy GetPolicy(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return new ProjectUnitPolicy(GetLengthUnit(document));
        }

        public static LengthUnit GetLengthUnit(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            switch ((int)document.Database.Insunits)
            {
                case 1: return LengthUnit.Inch;
                case 2: return LengthUnit.Foot;
                case 3: return LengthUnit.Mile;
                case 4: return LengthUnit.Millimeter;
                case 5: return LengthUnit.Centimeter;
                case 6: return LengthUnit.Meter;
                case 7: return LengthUnit.Kilometer;
                case 8: return LengthUnit.Microinch;
                case 9: return LengthUnit.Mil;
                case 10: return LengthUnit.Yard;
                case 11: return LengthUnit.Angstrom;
                case 12: return LengthUnit.Nanometer;
                case 13: return LengthUnit.Micrometer;
                case 14: return LengthUnit.Decimeter;
                case 15: return LengthUnit.Decameter;
                case 16: return LengthUnit.Hectometer;
                case 17: return LengthUnit.Gigameter;
                case 18: return LengthUnit.AstronomicalUnit;
                case 19: return LengthUnit.LightYear;
                case 20: return LengthUnit.Parsec;
                case 21: return LengthUnit.USSurveyFoot;
                case 22: return LengthUnit.USSurveyInch;
                case 23: return LengthUnit.USSurveyYard;
                case 24: return LengthUnit.USSurveyMile;
                default: return LengthUnit.Millimeter;
            }
        }

        public static DrawingUnit GetDrawingUnit(Document document) => ProjectUnitPolicy.ToDrawingUnit(GetLengthUnit(document));

        public static bool IsAssumedMillimeter(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var code = (int)document.Database.Insunits;
            return code < 1 || code > 24;
        }

        public static double MetersToDrawingUnits(Document document, double meters) => GetPolicy(document).FromMeters(meters);
        public static double DrawingUnitsToMeters(Document document, double value) => GetPolicy(document).ToMeters(value);
        public static string Describe(Document document) => IsAssumedMillimeter(document) ? "Millimeter (assumed)" : GetLengthUnit(document).ToString();
    }
}
