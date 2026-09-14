using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DQuantityAggregationPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PreservesPositiveHighDynamicResidual();
        }

        private static void PreservesPositiveHighDynamicResidual()
        {
            var aggregate = new TakeoffQuantityAggregator2D().Aggregate(new[]
            {
                Evidence("M-1", 1e16),
                Evidence("M-2", 3d),
                Evidence("M-3", 2d),
                Evidence("M-4", 1e-16)
            }).Single();

            Equal(10000000000000004d, aggregate.Quantity, "canonical positive residual");
        }

        private static TakeoffQuantityEvidence2D Evidence(string markupId, double quantity)
        {
            return new TakeoffQuantityEvidence2D(markupId, "S-1", "R1", "sheet.pdf", "h-" + markupId,
                "TEST", "", "QTO", quantity, "ea");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (!expected.Equals(actual))
                throw new InvalidOperationException(
                    "Qs2DQuantityAggregationPrecisionSmoke " + label + ": expected=" + expected.ToString("R") +
                    ", actual=" + actual.ToString("R") + ".");
        }
    }
}
