using System;

namespace QS3D.Core.Rebar
{
    public static class RebarWeight
    {
        public static double KilogramsPerMeter(double diameterMm)
        {
            RequireFiniteNonNegative(diameterMm, nameof(diameterMm));
            if (diameterMm <= 0d) throw new ArgumentOutOfRangeException(nameof(diameterMm));
            return FiniteProduct(diameterMm, diameterMm / 162d, "Rebar unit weight");
        }

        public static double TotalKilograms(double diameterMm, double totalLengthMeters, double wastePercent = 0d)
        {
            RequireFiniteNonNegative(totalLengthMeters, nameof(totalLengthMeters));
            RequireFiniteNonNegative(wastePercent, nameof(wastePercent));
            var netWeight = FiniteProduct(KilogramsPerMeter(diameterMm), totalLengthMeters, "Rebar net weight");
            var wasteFactor = 1d + wastePercent / 100d;
            if (double.IsNaN(wasteFactor) || double.IsInfinity(wasteFactor)) throw new OverflowException("Rebar waste factor is not finite.");
            return FiniteProduct(netWeight, wasteFactor, "Rebar total weight");
        }

        private static void RequireFiniteNonNegative(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new ArgumentOutOfRangeException(name);
        }

        private static double FiniteProduct(double left, double right, string label)
        {
            var result = left * right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " is not finite.");
            return result;
        }
    }
}
