using System;

namespace QS3D.Core.Units
{
    public static class UnitScale
    {
        public static double LinearToMeters(DrawingUnit unit)
        {
            switch (unit)
            {
                case DrawingUnit.Millimeter: return 0.001d;
                case DrawingUnit.Centimeter: return 0.01d;
                case DrawingUnit.Meter: return 1d;
                case DrawingUnit.Inch: return 0.0254d;
                case DrawingUnit.Foot: return 0.3048d;
                case DrawingUnit.Yard: return 0.9144d;
                default: throw new ArgumentOutOfRangeException(nameof(unit));
            }
        }
        public static double ToMeters(double value, DrawingUnit unit) => value * LinearToMeters(unit);
        public static double FromMeters(double meters, DrawingUnit unit) => meters / LinearToMeters(unit);
        public static double ToSquareMeters(double value, DrawingUnit unit) { var s = LinearToMeters(unit); return value * s * s; }
        public static double ToCubicMeters(double value, DrawingUnit unit) { var s = LinearToMeters(unit); return value * s * s * s; }
    }
}
