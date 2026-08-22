using System;

namespace QS3D.Core.Revisions
{
    internal static class RevisionMath
    {
        public static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Revision quantity is not finite: " + label);
            return value == 0d ? 0d : value;
        }

        public static double Add(double left, double right, string label)
        {
            Finite(left, label);
            Finite(right, label);
            var result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Revision quantity total overflow: " + label);
            return result == 0d ? 0d : result;
        }

        public static double Subtract(double left, double right, string label)
        {
            Finite(left, label);
            Finite(right, label);
            var result = left - right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Revision quantity delta overflow: " + label);
            if (right != 0d && result == left)
                throw new OverflowException("Revision quantity delta lost a non-zero right operand at floating-point precision: " + label);
            if (left != 0d && result == -right)
                throw new OverflowException("Revision quantity delta lost a non-zero left operand at floating-point precision: " + label);
            return result == 0d ? 0d : result;
        }

        public static double Percent(double delta, double baseline, string label)
        {
            Finite(delta, label);
            Finite(baseline, label);
            var denominator = Math.Abs(baseline);
            if (denominator < 1e-12) throw new DivideByZeroException("Revision percentage baseline is effectively zero: " + label);
            var ratio = delta / denominator;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio)) throw new OverflowException("Revision percentage ratio overflow: " + label);
            var result = ratio * 100d;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Revision percentage overflow: " + label);
            return result == 0d ? 0d : result;
        }
    }
}
