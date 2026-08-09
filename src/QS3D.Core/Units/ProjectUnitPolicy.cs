using System;

namespace QS3D.Core.Units
{
    public enum LengthUnit { Millimeter, Centimeter, Meter }

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
        public double ToMeters(double drawingLength)
        {
            switch (DrawingUnit)
            {
                case LengthUnit.Millimeter: return drawingLength / 1000d;
                case LengthUnit.Centimeter: return drawingLength / 100d;
                default: return drawingLength;
            }
        }
        public double AreaToSquareMeters(double drawingArea)
        {
            var scale = ToMeters(1d);
            return drawingArea * scale * scale;
        }
        public double VolumeToCubicMeters(double drawingVolume)
        {
            var scale = ToMeters(1d);
            return drawingVolume * scale * scale * scale;
        }
        public double RoundForDisplay(double value) => Math.Round(value, DisplayDecimals, MidpointRounding.AwayFromZero);
    }
}
