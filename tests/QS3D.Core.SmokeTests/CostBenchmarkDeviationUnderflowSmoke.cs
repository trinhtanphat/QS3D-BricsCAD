using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostBenchmarkDeviationUnderflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RepresentableTinyDeviationIsPreserved();
            LargeDeltaUsesOverflowSafeOrdering();
            OrdinaryDeviationRemainsStable();
            ZeroAverageSemanticsRemainStable();
        }

        private static void RepresentableTinyDeviationIsPreserved()
        {
            var service = new CostBenchmarkService();
            var result = service.Analyze(
                Catalog(decimal.MaxValue),
                "benchmark",
                "dimension",
                "VND",
                decimal.MaxValue - 1m);

            Assert(result.DeviationFromAveragePercent.HasValue, "A positive historical average must produce a numeric deviation.");
            Assert(result.DeviationFromAveragePercent.Value < 0m, "A current rate below the average must report a negative deviation.");
            Assert(result.DeviationFromAveragePercent.Value != 0m, "A representable nonzero final deviation must not underflow to zero before percent scaling.");
        }

        private static void LargeDeltaUsesOverflowSafeOrdering()
        {
            var service = new CostBenchmarkService();
            var result = service.Analyze(
                Catalog(decimal.MaxValue),
                "benchmark",
                "dimension",
                "VND",
                0m);

            Assert(result.DeviationFromAveragePercent == -100m, "A full drop from a positive benchmark must remain exactly -100 percent even when delta-first scaling would overflow.");
        }

        private static void OrdinaryDeviationRemainsStable()
        {
            var service = new CostBenchmarkService();
            var result = service.Analyze(
                Catalog(100m),
                "benchmark",
                "dimension",
                "VND",
                110m);

            Assert(result.AverageUnitCost == 100m, "Ordinary benchmark average changed unexpectedly.");
            Assert(result.DeviationFromAveragePercent == 10m, "Ordinary benchmark deviation changed unexpectedly.");
        }

        private static void ZeroAverageSemanticsRemainStable()
        {
            var service = new CostBenchmarkService();
            var zero = service.Analyze(Catalog(0m), "benchmark", "dimension", "VND", 0m);
            var nonzero = service.Analyze(Catalog(0m), "benchmark", "dimension", "VND", 1m);

            Assert(zero.DeviationFromAveragePercent == 0m, "Zero current cost against a zero average must remain a zero deviation.");
            Assert(!nonzero.DeviationFromAveragePercent.HasValue, "A nonzero current cost against a zero average must remain undefined.");
        }

        private static HistoricalCostCatalog Catalog(decimal unitCost)
        {
            return new HistoricalCostCatalog(new[]
            {
                new HistoricalCostRecord(
                    "history-1",
                    "benchmark",
                    "dimension",
                    1m,
                    unitCost,
                    "VND",
                    new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc))
            });
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
