using System;
using System.Collections.Generic;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarCuttingFitPrecisionSmoke
    {
        internal static void Run()
        {
            PrecisionCollapsedOverfillUsesTwoBars();
            RepresentableRemainderStillFits();
            RetainedLowTermsReachExactFill();
            OrdinaryPackingRemainsStable();
        }

        private static void PrecisionCollapsedOverfillUsesTwoBars()
        {
            var result = RebarCuttingOptimizer.Plan(Demand(1e16d, 1e16d, 1d));
            Equal(2, result.StockBars.Count, "Precision-collapsed overfill was incorrectly packed into one stock bar.");
        }

        private static void RepresentableRemainderStillFits()
        {
            var stock = 10000000000000002d;
            var result = RebarCuttingOptimizer.Plan(Demand(stock, 1e16d, 1d));

            Equal(1, result.StockBars.Count, "A mathematically fitting compensated allocation was rejected.");
            Equal(1d, result.StockBars[0].OffCutLengthM, "Compensated off-cut did not preserve the one-metre remainder.");
        }

        private static void RetainedLowTermsReachExactFill()
        {
            var stock = 10000000000000002d;
            var result = RebarCuttingOptimizer.Plan(Demand(stock, 1e16d, 1d, 1d));

            Equal(1, result.StockBars.Count, "Compensated exact fill unexpectedly used multiple stock bars.");
            Equal(0d, result.StockBars[0].OffCutLengthM, "Compensated exact fill reported a non-zero off-cut.");
            Equal(stock, result.StockBars[0].AllocatedLengthBeforeKerfM, "Compensated allocated length did not preserve the representable exact total.");
        }

        private static void OrdinaryPackingRemainsStable()
        {
            var result = RebarCuttingOptimizer.Plan(Demand(12d, 5d, 5d));

            Equal(1, result.StockBars.Count, "Ordinary best-fit packing changed unexpectedly.");
            Equal(10d, result.StockBars[0].AllocatedLengthBeforeKerfM, "Ordinary allocated length changed unexpectedly.");
            Equal(2d, result.StockBars[0].OffCutLengthM, "Ordinary off-cut changed unexpectedly.");
        }

        private static RebarStockDemand Demand(double stockLengthM, params double[] lengths)
        {
            var cuts = new List<RebarCutRequirement>(lengths.Length);
            for (var index = 0; index < lengths.Length; index++)
                cuts.Add(new RebarCutRequirement("CUT-" + index, lengths[index], 1));

            return new RebarStockDemand(
                "GROUP",
                "CB400-V",
                16d,
                stockLengthM,
                cuts.AsReadOnly(),
                new RebarCutAllowancePolicy());
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
