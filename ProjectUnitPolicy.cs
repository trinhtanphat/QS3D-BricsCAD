using System;

namespace QS3D.Core.Units
{
<<<<<<< origin/main
    public enum LengthUnit
    {
        Millimeter,
        Centimeter,
        Meter,
        Inch,
        Foot,
        Yard,
        Mile,
        Kilometer,
        Microinch,
        Mil,
        Angstrom,
        Nanometer,
        Micrometer,
        Decimeter,
        Decameter,
        Hectometer,
        Gigameter,
        AstronomicalUnit,
        LightYear,
        Parsec,
        USSurveyFoot,
        USSurveyInch,
        USSurveyYard,
        USSurveyMile
    }
=======
    public enum LengthUnit { Millimeter, Centimeter, Meter, Inch, Foot, Yard }
>>>>>>> origin/agent/review-hardening-20260810

    public sealed class ProjectUnitPolicy
    {
        public ProjectUnitPolicy(LengthUnit drawingUnit = LengthUnit.Millimeter, int displayDecimals = 3)
        {
            if (displayDecimals < 0 || displayDecimals > 9) throw new ArgumentOutOfRangeException(nameof(displayDecimals));
            DrawingUnit = drawingUnit;
            DisplayDecimals = displayDecimals;
        }

        public LengthUnit DrawingUnit { get; }
        public int DisplayDecimals { get; }
<<<<<<< origin/main
        public double ToMeters(double drawingLength) => UnitScale.ToMeters(drawingLength, ToDrawingUnit(DrawingUnit));
        public double FromMeters(double meters) => UnitScale.FromMeters(meters, ToDrawingUnit(DrawingUnit));
        public double AreaToSquareMeters(double drawingArea) => UnitScale.ToSquareMeters(drawingArea, ToDrawingUnit(DrawingUnit));
        public double VolumeToCubicMeters(double drawingVolume) => UnitScale.ToCubicMeters(drawingVolume, ToDrawingUnit(DrawingUnit));
        public double RoundForDisplay(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "Display value must be finite.");
            return Math.Round(value, DisplayDecimals, MidpointRounding.AwayFromZero);
        }

        public static DrawingUnit ToDrawingUnit(LengthUnit unit)
        {
            if (!Enum.IsDefined(typeof(LengthUnit), unit)) throw new ArgumentOutOfRangeException(nameof(unit));
            return (DrawingUnit)(int)unit;
        }
=======
        public double ToMeters(double drawingLength) => drawingLength * LinearToMeters(DrawingUnit);
        public double FromMeters(double meters) => meters / LinearToMeters(DrawingUnit);
        public double AreaToSquareMeters(double drawingArea) { var scale = LinearToMeters(DrawingUnit); return drawingArea * scale * scale; }
        public double VolumeToCubicMeters(double drawingVolume) { var scale = LinearToMeters(DrawingUnit); return drawingVolume * scale * scale * scale; }
        public double RoundForDisplay(double value) => Math.Round(value, DisplayDecimals, MidpointRounding.AwayFromZero);

        private static double LinearToMeters(LengthUnit unit)
        {
            switch (unit)
            {
                case LengthUnit.Millimeter: return 0.001d;
                case LengthUnit.Centimeter: return 0.01d;
                case LengthUnit.Meter: return 1d;
                case LengthUnit.Inch: return 0.0254d;
                case LengthUnit.Foot: return 0.3048d;
                case LengthUnit.Yard: return 0.9144d;
                default: throw new ArgumentOutOfRangeException(nameof(unit));
            }
        }
>>>>>>> origin/agent/review-hardening-20260810
    }
}
