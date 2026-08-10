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
                case 4: return LengthUnit.Millimeter;
                case 5: return LengthUnit.Centimeter;
                case 6: return LengthUnit.Meter;
                case 10: return LengthUnit.Yard;
                default: return LengthUnit.Millimeter;
            }
        }

        public static DrawingUnit GetDrawingUnit(Document document)
        {
            switch (GetLengthUnit(document))
            {
                case LengthUnit.Inch: return DrawingUnit.Inch;
                case LengthUnit.Foot: return DrawingUnit.Foot;
                case LengthUnit.Centimeter: return DrawingUnit.Centimeter;
                case LengthUnit.Meter: return DrawingUnit.Meter;
                case LengthUnit.Yard: return DrawingUnit.Yard;
                default: return DrawingUnit.Millimeter;
            }
        }

        public static bool IsAssumedMillimeter(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var code = (int)document.Database.Insunits;
            return code != 1 && code != 2 && code != 4 && code != 5 && code != 6 && code != 10;
        }

        public static double MetersToDrawingUnits(Document document, double meters) => GetPolicy(document).FromMeters(meters);
        public static double DrawingUnitsToMeters(Document document, double value) => GetPolicy(document).ToMeters(value);
        public static string Describe(Document document) => GetLengthUnit(document).ToString();
    }
}
