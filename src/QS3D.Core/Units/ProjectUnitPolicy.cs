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
            var rounded = Math.Round(value, DisplayDecimals, MidpointRounding.AwayFromZero);
            return rounded == 0d ? 0d : rounded;
        }

        public static DrawingUnit ToDrawingUnit(LengthUnit unit)
        {
            switch (unit)
            {
                case LengthUnit.Millimeter: return QS3D.Core.Units.DrawingUnit.Millimeter;
                case LengthUnit.Centimeter: return QS3D.Core.Units.DrawingUnit.Centimeter;
                case LengthUnit.Meter: return QS3D.Core.Units.DrawingUnit.Meter;
                case LengthUnit.Inch: return QS3D.Core.Units.DrawingUnit.Inch;
                case LengthUnit.Foot: return QS3D.Core.Units.DrawingUnit.Foot;
                case LengthUnit.Yard: return QS3D.Core.Units.DrawingUnit.Yard;
                case LengthUnit.Mile: return QS3D.Core.Units.DrawingUnit.Mile;
                case LengthUnit.Kilometer: return QS3D.Core.Units.DrawingUnit.Kilometer;
                case LengthUnit.Microinch: return QS3D.Core.Units.DrawingUnit.Microinch;
                case LengthUnit.Mil: return QS3D.Core.Units.DrawingUnit.Mil;
                case LengthUnit.Angstrom: return QS3D.Core.Units.DrawingUnit.Angstrom;
                case LengthUnit.Nanometer: return QS3D.Core.Units.DrawingUnit.Nanometer;
                case LengthUnit.Micrometer: return QS3D.Core.Units.DrawingUnit.Micrometer;
                case LengthUnit.Decimeter: return QS3D.Core.Units.DrawingUnit.Decimeter;
                case LengthUnit.Decameter: return QS3D.Core.Units.DrawingUnit.Decameter;
                case LengthUnit.Hectometer: return QS3D.Core.Units.DrawingUnit.Hectometer;
                case LengthUnit.Gigameter: return QS3D.Core.Units.DrawingUnit.Gigameter;
                case LengthUnit.AstronomicalUnit: return QS3D.Core.Units.DrawingUnit.AstronomicalUnit;
                case LengthUnit.LightYear: return QS3D.Core.Units.DrawingUnit.LightYear;
                case LengthUnit.Parsec: return QS3D.Core.Units.DrawingUnit.Parsec;
                case LengthUnit.USSurveyFoot: return QS3D.Core.Units.DrawingUnit.USSurveyFoot;
                case LengthUnit.USSurveyInch: return QS3D.Core.Units.DrawingUnit.USSurveyInch;
                case LengthUnit.USSurveyYard: return QS3D.Core.Units.DrawingUnit.USSurveyYard;
                case LengthUnit.USSurveyMile: return QS3D.Core.Units.DrawingUnit.USSurveyMile;
                default: throw new ArgumentOutOfRangeException(nameof(unit));
            }
        }
    }
}
