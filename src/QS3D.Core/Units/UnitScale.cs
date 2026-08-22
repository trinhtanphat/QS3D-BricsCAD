using System;

namespace QS3D.Core.Units
{
    public static class UnitScale
    {
        public static double LinearToMeters(DrawingUnit unit)
        {
            switch (unit)
            {
                case DrawingUnit.Millimeter: return 1e-3d;
                case DrawingUnit.Centimeter: return 1e-2d;
                case DrawingUnit.Meter: return 1d;
                case DrawingUnit.Inch: return 0.0254d;
                case DrawingUnit.Foot: return 0.3048d;
                case DrawingUnit.Yard: return 0.9144d;
                case DrawingUnit.Mile: return 1609.344d;
                case DrawingUnit.Kilometer: return 1000d;
                case DrawingUnit.Microinch: return 2.54e-8d;
                case DrawingUnit.Mil: return 2.54e-5d;
                case DrawingUnit.Angstrom: return 1e-10d;
                case DrawingUnit.Nanometer: return 1e-9d;
                case DrawingUnit.Micrometer: return 1e-6d;
                case DrawingUnit.Decimeter: return 0.1d;
                case DrawingUnit.Decameter: return 10d;
                case DrawingUnit.Hectometer: return 100d;
                case DrawingUnit.Gigameter: return 1e9d;
                case DrawingUnit.AstronomicalUnit: return 149597870700d;
                case DrawingUnit.LightYear: return 9.4607304725808e15d;
                case DrawingUnit.Parsec: return 3.0856775814913673e16d;
                case DrawingUnit.USSurveyFoot: return 1200d / 3937d;
                case DrawingUnit.USSurveyInch: return (1200d / 3937d) / 12d;
                case DrawingUnit.USSurveyYard: return (1200d / 3937d) * 3d;
                case DrawingUnit.USSurveyMile: return (1200d / 3937d) * 5280d;
                default: throw new ArgumentOutOfRangeException(nameof(unit));
            }
        }

        public static double ToMeters(double value, DrawingUnit unit) => Scale(value, LinearToMeters(unit), nameof(value));
        public static double FromMeters(double meters, DrawingUnit unit) => Scale(meters, 1d / LinearToMeters(unit), nameof(meters));
        public static double ToSquareMeters(double value, DrawingUnit unit) { var s = LinearToMeters(unit); return Scale(value, s * s, nameof(value)); }
        public static double ToCubicMeters(double value, DrawingUnit unit) { var s = LinearToMeters(unit); return Scale(value, s * s * s, nameof(value)); }

        private static double Scale(double value, double scale, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, "Unit conversion input must be finite.");
            var result = value * scale;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Unit conversion produced a non-finite result.");
            if (value != 0d && result == 0d) throw new OverflowException("Unit conversion underflowed a non-zero input to zero.");
            if (value != 0d && scale != 1d && result == value) throw new OverflowException("Unit conversion rounded a non-zero input back to its unchanged value.");
            return result == 0d ? 0d : result;
        }
    }
}
