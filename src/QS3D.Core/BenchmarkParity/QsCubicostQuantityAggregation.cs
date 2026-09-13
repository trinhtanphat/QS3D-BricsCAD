using System;
using System.Collections.Generic;

namespace QS3D.Core.BenchmarkParity
{
    internal static class CubicostQuantityAggregation
    {
        internal static double SumFinite(IEnumerable<double> values, string label)
        {
            if (values == null) throw new ArgumentNullException("values");
            label = QsModelElementSnapshot.Require(label, "label");
            var sum = 0d;
            var compensation = 0d;
            foreach (var value in values)
            {
                if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Invalid Cubicost inventory quantity for " + label + ".");
                var next = sum + value;
                if (double.IsNaN(next) || double.IsInfinity(next)) throw new InvalidOperationException("Cubicost inventory quantity overflow for " + label + ".");
                var correction = Math.Abs(sum) >= Math.Abs(value) ? (sum - next) + value : (value - next) + sum;
                compensation += correction;
                if (double.IsNaN(compensation) || double.IsInfinity(compensation)) throw new InvalidOperationException("Cubicost inventory compensation overflow for " + label + ".");
                sum = next;
            }

            var result = sum + compensation;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new InvalidOperationException("Cubicost inventory quantity overflow for " + label + ".");
            return result == 0d ? 0d : result;
        }
    }
}
