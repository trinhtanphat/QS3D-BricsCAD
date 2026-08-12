using System;

namespace QS3D.Core.Rebar
{
    internal static class RebarMath
    {
        public static double NonNegative(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(label, "Rebar value must be finite and non-negative.");
            return value;
        }

        public static double Positive(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentOutOfRangeException(label, "Rebar value must be finite and greater than zero.");
            return value;
        }

        public static double Add(double left, double right, string label)
        {
            NonNegative(left, label);
            NonNegative(right, label);
            var result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Rebar addition overflow: " + label);
            return result;
        }

        public static double Multiply(double left, double right, string label)
        {
            NonNegative(left, label);
            NonNegative(right, label);
            var result = left * right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Rebar multiplication overflow: " + label);
            if (left != 0d && right != 0d && result == 0d) throw new OverflowException("Rebar multiplication underflow: " + label);
            return result;
        }

        public static double Divide(double numerator, double denominator, string label)
        {
            NonNegative(numerator, label);
            Positive(denominator, label);
            var result = numerator / denominator;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Rebar division overflow: " + label);
            if (numerator != 0d && result == 0d) throw new OverflowException("Rebar division underflow: " + label);
            return result;
        }

        public static double CeilingNearInteger(double value, string label)
        {
            NonNegative(value, label);
            var nearestInteger = Math.Round(value);
            if (Math.Abs(value - nearestInteger) <= IntegerSnapTolerance(value)) value = nearestInteger;
            return Math.Ceiling(value);
        }

        private static double IntegerSnapTolerance(double value)
        {
            var magnitude = Math.Abs(value);
            if (magnitude == double.MaxValue) return 0d;
            var bits = BitConverter.DoubleToInt64Bits(magnitude);
            var next = BitConverter.Int64BitsToDouble(bits + 1L);
            return (next - magnitude) * 8d;
        }
    }
}
