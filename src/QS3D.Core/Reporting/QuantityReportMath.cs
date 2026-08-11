using System;

namespace QS3D.Core.Reporting
{
    public static class QuantityReportMath
    {
        public static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Quantity report value is not finite: " + label);
            return value;
        }

        public static double NonNegative(double value, string label)
        {
            value = Finite(value, label);
            if (value < 0d) throw new InvalidOperationException("Quantity report value cannot be negative: " + label);
            return value;
        }

        public static double Add(double current, double value, string label)
        {
            Finite(current, label);
            Finite(value, label);
            var result = current + value;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Quantity report total overflow: " + label);
            return result;
        }

        public static int AddCount(int current, int value)
        {
            if (current < 0 || value < 0) throw new InvalidOperationException("Quantity report count cannot be negative.");
            return checked(current + value);
        }
    }
}
