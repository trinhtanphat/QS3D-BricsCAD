using System;
using System.Collections.Generic;
using QS3D.Core.Reporting;

namespace QS3D.Core.BenchmarkParity
{
    internal static class CubicostQuantityAggregation
    {
        internal static double SumFinite(IEnumerable<double> values, string label)
        {
            if (values == null) throw new ArgumentNullException("values");
            label = QsModelElementSnapshot.Require(label, "label");
            try
            {
                var accumulator = new QuantityReportMath.FiniteAccumulator();
                foreach (var value in values)
                {
                    if (double.IsNaN(value) || double.IsInfinity(value))
                        throw new InvalidOperationException("Invalid Cubicost inventory quantity for " + label + ".");
                    accumulator.Add(value, label);
                }
                return accumulator.Value(label);
            }
            catch (OverflowException)
            {
                throw new InvalidOperationException("Cubicost inventory quantity overflow for " + label + ".");
            }
        }
    }
}
