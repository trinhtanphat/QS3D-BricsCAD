using System;

namespace QS3D.Core.Reporting
{
    public static class QuantityReportMath
    {
        public struct FiniteAccumulator
        {
            private double _sum;
            private double _compensation;

            public void Add(double value, string label)
            {
                Finite(_sum, label);
                Finite(_compensation, label);
                Finite(value, label);

                var sum = _sum + value;
                if (double.IsNaN(sum) || double.IsInfinity(sum))
                    throw new OverflowException("Quantity report total overflow: " + label);

                var correction = Math.Abs(_sum) >= Math.Abs(value)
                    ? (_sum - sum) + value
                    : (value - sum) + _sum;

                var compensation = _compensation + correction;
                if (double.IsNaN(compensation) || double.IsInfinity(compensation))
                    throw new OverflowException("Quantity report total overflow: " + label);

                var normalized = sum + compensation;
                if (double.IsNaN(normalized) || double.IsInfinity(normalized))
                    throw new OverflowException("Quantity report total overflow: " + label);

                _compensation = compensation - (normalized - sum);
                _sum = normalized;
            }

            public double Value(string label)
            {
                Finite(_sum, label);
                Finite(_compensation, label);
                var result = _sum + _compensation;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Quantity report total overflow: " + label);
                return result == 0d ? 0d : result;
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
            if (value != 0d && result == current)
                throw new OverflowException("Quantity report total lost a non-zero addend at floating-point precision: " + label);
            if (current != 0d && result == value)
                throw new OverflowException("Quantity report total lost a non-zero accumulated value at floating-point precision: " + label);
            return result == 0d ? 0d : result;
        }

        public static int AddCount(int current, int value)
        {
            if (current < 0 || value < 0) throw new InvalidOperationException("Quantity report count cannot be negative.");
            return checked(current + value);
        }
    }
}
