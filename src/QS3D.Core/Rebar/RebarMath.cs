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
            return result;
        }

        public static double Divide(double numerator, double denominator, string label)
        {
            NonNegative(numerator, label);
            Positive(denominator, label);
            var result = numerator / denominator;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Rebar division overflow: " + label);
            return result;
        }
    }
}
