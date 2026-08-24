using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostBenchmarkAverageTransientUnderflowSmoke
    {
        private const decimal Quantum = 0.0000000000000000000000000001m;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            RepresentableFinalMeanSurvivesTransientUnderflow();
            OverflowSafeHighValueMeanRemainsAccepted();
        }

        private static void RepresentableFinalMeanSurvivesTransientUnderflow()
        {
            var catalog = new HistoricalCostCatalog(new[]
            {
                Record("R1", 0m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                Record("R2", Quantum, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
                Record("R3", 2m * Quantum, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc))
            });

            var result = new CostBenchmarkService().Analyze(
                catalog,
                "BENCH",
                "DIM",
                "VND",
                Quantum);

            Equal(3, result.SampleCount, "Transient-underflow sample count changed.");
            Equal(0m, result.MinimumUnitCost, "Transient-underflow minimum changed.");
            Equal(2m * Quantum, result.MaximumUnitCost, "Transient-underflow maximum changed.");
            Equal(Quantum, result.AverageUnitCost, "Representable final mean was lost to a transient running-mean underflow.");
            Equal(Quantum, result.MedianUnitCost, "Transient-underflow median changed.");
            Equal(0m, result.DeviationFromAveragePercent, "Transient-underflow deviation changed.");
        }

        private static void OverflowSafeHighValueMeanRemainsAccepted()
        {
            var catalog = new HistoricalCostCatalog(new[]
            {
                Record("M1", decimal.MaxValue, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
                Record("M2", decimal.MaxValue, new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc))
            });

            var result = new CostBenchmarkService().Analyze(
                catalog,
                "BENCH",
                "DIM",
                "VND",
                decimal.MaxValue);

            Equal(decimal.MaxValue, result.AverageUnitCost, "Overflow-safe high-value average regressed.");
            Equal(decimal.MaxValue, result.MedianUnitCost, "Overflow-safe high-value median regressed.");
            Equal(0m, result.DeviationFromAveragePercent, "Overflow-safe high-value deviation regressed.");
        }

        private static HistoricalCostRecord Record(string id, decimal unitCost, DateTime asOfUtc)
        {
            return new HistoricalCostRecord(
                id,
                "BENCH",
                "DIM",
                1m,
                unitCost,
                "VND",
                asOfUtc);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
