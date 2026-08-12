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
            switch (unit)
            {
                case LengthUnit.Millimeter: return DrawingUnit.Millimeter;
                case LengthUnit.Centimeter: return DrawingUnit.Centimeter;
                case LengthUnit.Meter: return DrawingUnit.Meter;
                case LengthUnit.Inch: return DrawingUnit.Inch;
                case LengthUnit.Foot: return DrawingUnit.Foot;
                case LengthUnit.Yard: return DrawingUnit.Yard;
                case LengthUnit.Mile: return DrawingUnit.Mile;
                case LengthUnit.Kilometer: return DrawingUnit.Kilometer;
                case LengthUnit.Microinch: return DrawingUnit.Microinch;
                case LengthUnit.Mil: return DrawingUnit.Mil;
                case LengthUnit.Angstrom: return DrawingUnit.Angstrom;
                case LengthUnit.Nanometer: return DrawingUnit.Nanometer;
                case LengthUnit.Micrometer: return DrawingUnit.Micrometer;
                case LengthUnit.Decimeter: return DrawingUnit.Decimeter;
                case LengthUnit.Decameter: return DrawingUnit.Decameter;
                case LengthUnit.Hectometer: return DrawingUnit.Hectometer;
                case LengthUnit.Gigameter: return DrawingUnit.Gigameter;
                case LengthUnit.AstronomicalUnit: return DrawingUnit.AstronomicalUnit;
                case LengthUnit.LightYear: return DrawingUnit.LightYear;
                case LengthUnit.Parsec: return DrawingUnit.Parsec;
                case LengthUnit.USSurveyFoot: return DrawingUnit.USSurveyFoot;
                case LengthUnit.USSurveyInch: return DrawingUnit.USSurveyInch;
                case LengthUnit.USSurveyYard: return DrawingUnit.USSurveyYard;
                case LengthUnit.USSurveyMile: return DrawingUnit.USSurveyMile;
                default: throw new ArgumentOutOfRangeException(nameof(unit));
            }
        }
    }
}
