using System;

namespace QS3D.Core.Units
{
    public enum LengthUnit { Millimeter, Centimeter, Meter, Inch, Foot, Yard }

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
    }
}
