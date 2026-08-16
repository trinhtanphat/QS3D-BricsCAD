using System;

namespace QS3D.Core.Services
{
    internal static class QuantityMath
    {
        public static double Positive(double value) => value > 0d && IsFinite(value) ? value : 0d;

        public static double Multiply(double left, double right, string label)
        {
            RequireNonNegativeFinite(left, label);
            RequireNonNegativeFinite(right, label);
            var result = left * right;
            if (!IsFinite(result)) throw new OverflowException("Quantity multiplication overflow: " + label);
            if (result == 0d && left != 0d && right != 0d) throw new InvalidOperationException("Quantity multiplication underflow: " + label);
            return result == 0d ? 0d : result;
        }

        public static double Add(double left, double right, string label)
        {
            RequireNonNegativeFinite(left, label);
            RequireNonNegativeFinite(right, label);
            var result = left + right;
            if (!IsFinite(result)) throw new OverflowException("Quantity addition overflow: " + label);
            if (left != 0d && right != 0d && (result.Equals(left) || result.Equals(right)))
                throw new InvalidOperationException("Quantity addition lost a positive contribution at floating-point precision: " + label);
            return result == 0d ? 0d : result;
        }

        public static double SubtractFloorZero(double left, double right, string label)
        {
            RequireNonNegativeFinite(left, label);
            RequireNonNegativeFinite(right, label);
            var result = left - right;
            if (!IsFinite(result)) throw new OverflowException("Quantity subtraction overflow: " + label);
            if (right > 0d && right < left && result.Equals(left))
                throw new InvalidOperationException("Quantity subtraction lost a positive deduction at floating-point precision: " + label);
            return result > 0d ? result : 0d;
        }

        public static double Divide(double numerator, double denominator, string label)
        {
            RequireNonNegativeFinite(numerator, label);
            if (!IsFinite(denominator) || denominator <= 0d) throw new InvalidOperationException("Quantity denominator must be finite and greater than zero: " + label);
            var result = numerator / denominator;
            if (!IsFinite(result)) throw new OverflowException("Quantity division overflow: " + label);
            if (result == 0d && numerator != 0d) throw new InvalidOperationException("Quantity division underflow: " + label);
            return result == 0d ? 0d : result;
        }

        public static double Hypot(double first, double second, string label)
        {
            RequireNonNegativeFinite(first, label);
            RequireNonNegativeFinite(second, label);
            var maximum = Math.Max(first, second);
            if (maximum <= 0d) return 0d;
            var minimum = Math.Min(first, second);
            var ratio = minimum / maximum;
            var factor = Math.Sqrt(1d + ratio * ratio);
            var result = maximum * factor;
            if (!IsFinite(result)) throw new OverflowException("Quantity hypotenuse overflow: " + label);
            if (minimum != 0d && result.Equals(maximum))
                throw new InvalidOperationException("Quantity hypotenuse lost a positive component at floating-point precision: " + label);
            return result;
        }

        public static double Clamp(double value, double minimum, double maximum, string label)
        {
            if (!IsFinite(value) || !IsFinite(minimum) || !IsFinite(maximum)) throw new InvalidOperationException("Quantity clamp requires finite values: " + label);
            if (minimum > maximum) throw new InvalidOperationException("Quantity clamp bounds are invalid: " + label);
            var result = Math.Max(minimum, Math.Min(maximum, value));
            return result == 0d ? 0d : result;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void RequireNonNegativeFinite(double value, string label)
        {
            if (!IsFinite(value) || value < 0d) throw new InvalidOperationException("Quantity value must be finite and non-negative: " + label);
        }
    }
}
