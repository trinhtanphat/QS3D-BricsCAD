using System;

namespace QS3D.Core.Reporting
{
    public static class QuantityReportMath
    {
        public struct CompensatedSum
        {
            private double _sum;
            private double _compensation;

            public double Add(double value, string label)
            {
                Finite(value, label);
                var next = _sum + value;
                if (double.IsNaN(next) || double.IsInfinity(next))
                    throw new OverflowException("Quantity report total overflow: " + label);

                if (Math.Abs(_sum) >= Math.Abs(value))
                    _compensation += (_sum - next) + value;
                else
                    _compensation += (value - next) + _sum;
                if (double.IsNaN(_compensation) || double.IsInfinity(_compensation))
                    throw new OverflowException("Quantity report total compensation overflow: " + label);
                _sum = next;

                var total = _sum + _compensation;
                if (double.IsNaN(total) || double.IsInfinity(total))
                    throw new OverflowException("Quantity report total overflow: " + label);
                return total == 0d ? 0d : total;
            }
        }

        public static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Quantity report value is not finite: " + label);
            return value;
        }

        public static double NonNegative(double value, string label)
        {
            value = Finite(value, label);
            if (value < 0d) throw new InvalidOperationException("Quantity report value cannot be negative: " + label);
            return value == 0d ? 0d : value;
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
