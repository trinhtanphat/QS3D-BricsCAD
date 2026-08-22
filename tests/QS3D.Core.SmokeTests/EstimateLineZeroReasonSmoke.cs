using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimateLineZeroReasonSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var trace = new MeasurementTrace("SEM-ZR", "SRC-ZR", "Count", Array.Empty<MeasurementTraceFact>(), 10d, Array.Empty<MeasurementTraceAdjustment>(), 10d, "ea", "none");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var book = new RateBook("BOOK-ZR", new[] { new RateItem("RATE-ZR", new CostCode("ITEM"), "ea", "USD", 3m, Utc(2026, 1, 1), "v1") });

            var zero = EstimateLine.Create("LINE-ZR", snapshot, "SEM-ZR", "SRC-ZR", "Count", book, new CostCode("ITEM"), "USD", Utc(2026, 1, 2), 0m, "Redundant reason");
            if (zero.CommercialAdjustmentQuantity != 0m || zero.CommercialAdjustmentReason != null || zero.EstimatingQuantity != 10m || zero.FinalAmount != 30m)
                throw new InvalidOperationException("Zero commercial adjustment was not canonicalized.");

            var nonZero = EstimateLine.Create("LINE-NZR", snapshot, "SEM-ZR", "SRC-ZR", "Count", book, new CostCode("ITEM"), "USD", Utc(2026, 1, 2), 1m, "Explicit allowance");
            if (nonZero.CommercialAdjustmentReason != "Explicit allowance" || nonZero.EstimatingQuantity != 11m || nonZero.FinalAmount != 33m)
                throw new InvalidOperationException("Non-zero commercial adjustment behavior changed.");
        }

        private static DateTime Utc(int y, int m, int d) => new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc);
    }
}
