using System;

namespace QS3D.Core.Units
{
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

    public sealed class ProjectUnitPolicy
    {
        public ProjectUnitPolicy(LengthUnit drawingUnit = LengthUnit.Millimeter, int displayDecimals = 3)
        {
            if (!Enum.IsDefined(typeof(LengthUnit), drawingUnit)) throw new ArgumentOutOfRangeException(nameof(drawingUnit));
            if (displayDecimals < 0 || displayDecimals > 9) throw new ArgumentOutOfRangeException(nameof(displayDecimals));
            DrawingUnit = drawingUnit;
            DisplayDecimals = displayDecimals;
        }

        public LengthUnit DrawingUnit { get; }
        public int DisplayDecimals { get; }
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
    }
}
