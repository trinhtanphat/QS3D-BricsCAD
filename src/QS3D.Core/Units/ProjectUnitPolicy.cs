using System;
using DrawingUnitValue = QS3D.Core.Units.DrawingUnit;

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

        public static DrawingUnitValue ToDrawingUnit(LengthUnit unit)
        {
            switch (unit)
            {
                case LengthUnit.Millimeter: return DrawingUnitValue.Millimeter;
                case LengthUnit.Centimeter: return DrawingUnitValue.Centimeter;
                case LengthUnit.Meter: return DrawingUnitValue.Meter;
                case LengthUnit.Inch: return DrawingUnitValue.Inch;
                case LengthUnit.Foot: return DrawingUnitValue.Foot;
                case LengthUnit.Yard: return DrawingUnitValue.Yard;
                case LengthUnit.Mile: return DrawingUnitValue.Mile;
                case LengthUnit.Kilometer: return DrawingUnitValue.Kilometer;
                case LengthUnit.Microinch: return DrawingUnitValue.Microinch;
                case LengthUnit.Mil: return DrawingUnitValue.Mil;
                case LengthUnit.Angstrom: return DrawingUnitValue.Angstrom;
                case LengthUnit.Nanometer: return DrawingUnitValue.Nanometer;
                case LengthUnit.Micrometer: return DrawingUnitValue.Micrometer;
                case LengthUnit.Decimeter: return DrawingUnitValue.Decimeter;
                case LengthUnit.Decameter: return DrawingUnitValue.Decameter;
                case LengthUnit.Hectometer: return DrawingUnitValue.Hectometer;
                case LengthUnit.Gigameter: return DrawingUnitValue.Gigameter;
                case LengthUnit.AstronomicalUnit: return DrawingUnitValue.AstronomicalUnit;
                case LengthUnit.LightYear: return DrawingUnitValue.LightYear;
                case LengthUnit.Parsec: return DrawingUnitValue.Parsec;
                case LengthUnit.USSurveyFoot: return DrawingUnitValue.USSurveyFoot;
                case LengthUnit.USSurveyInch: return DrawingUnitValue.USSurveyInch;
                case LengthUnit.USSurveyYard: return DrawingUnitValue.USSurveyYard;
                case LengthUnit.USSurveyMile: return DrawingUnitValue.USSurveyMile;
                default: throw new ArgumentOutOfRangeException(nameof(unit));
            }
        }
    }
}
