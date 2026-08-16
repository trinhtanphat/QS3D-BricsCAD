using System;

namespace QS3D.Core.Rebar
{
    public sealed class ColumnTieQuantity
    {
        public int Count { get; set; }
        public double CuttingLengthPerTieM { get; set; }
        public double TotalLengthM { get; set; }
        public double KgPerMeter { get; set; }
        public double TotalWeightKg { get; set; }
    }

    public static class ColumnTieQuantityCalculator
    {
        public static ColumnTieQuantity Calculate(ColumnTieLayout layout, double diameterMm, double hookAllowancePerTieM = 0d)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            Positive(diameterMm, nameof(diameterMm));
            NonNegative(hookAllowancePerTieM, nameof(hookAllowancePerTieM));
            if (layout.ElevationsM.Count <= 0) throw new InvalidOperationException("Tie layout contains no tie elevations.");
            Positive(layout.PathPerimeterM, nameof(layout.PathPerimeterM));

            var cuttingLength = Add(layout.PathPerimeterM, hookAllowancePerTieM, "tie cutting length");
            var totalLength = Multiply(cuttingLength, layout.ElevationsM.Count, "tie total length");
            var kgPerMeter = Multiply(diameterMm, diameterMm, "tie diameter squared") / 162d;
            Positive(kgPerMeter, nameof(kgPerMeter));
            var totalWeight = Multiply(totalLength, kgPerMeter, "tie total weight");

            return new ColumnTieQuantity
            {
                Count = layout.ElevationsM.Count,
                CuttingLengthPerTieM = cuttingLength,
                TotalLengthM = totalLength,
                KgPerMeter = kgPerMeter,
                TotalWeightKg = totalWeight
            };
        }

        private static double Add(double left, double right, string label)
        {
            Finite(left, label); Finite(right, label);
            var value = left + right;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " overflowed.");
            return value;
        }

        private static double Multiply(double left, double right, string label)
        {
            Finite(left, label); Finite(right, label);
            var value = left * right;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " overflowed.");
            if (left != 0d && right != 0d && value == 0d) throw new OverflowException(label + " underflowed.");
            return value;
        }

        private static void Positive(double value, string name)
        {
            Finite(value, name);
            if (value <= 0d) throw new ArgumentOutOfRangeException(name);
        }

        private static void NonNegative(double value, string name)
        {
            Finite(value, name);
            if (value < 0d) throw new ArgumentOutOfRangeException(name);
        }

        private static void Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name);
        }
    }
}
