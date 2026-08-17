using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostBenchmarkAveragePrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            HighMagnitudeAverageRejectsLostContribution();
            OrdinaryBenchmarkSemanticsRemainStable();
        }

        private static void HighMagnitudeAverageRejectsLostContribution()
        {
            const decimal lower = 79228162514264337593543950330m;
            var catalog = new HistoricalCostCatalog(new[]
            {
                Record("R1", lower, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                Record("R2", lower + 1m, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc))
            });

            Throws<OverflowException>(() =>
                new CostBenchmarkService().Analyze(
                    catalog,
                    "BENCH",
                    "DIM",
                    "VND",
                    lower));
        }

        private static void OrdinaryBenchmarkSemanticsRemainStable()
        {
            var catalog = new HistoricalCostCatalog(new[]
            {
                Record("R3", 30m, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)),
                Record("R1", 10m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                Record("R2", 20m, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc))
            });

            var result = new CostBenchmarkService().Analyze(
                catalog,
                "BENCH",
                "DIM",
                "VND",
                22m);

            Equal(3, result.SampleCount, "Benchmark sample count changed.");
            Equal(10m, result.MinimumUnitCost, "Benchmark minimum changed.");
            Equal(30m, result.MaximumUnitCost, "Benchmark maximum changed.");
            Equal(20m, result.AverageUnitCost, "Benchmark average changed.");
            Equal(20m, result.MedianUnitCost, "Benchmark median changed.");
            Equal(22m, result.CurrentUnitCost, "Benchmark current cost changed.");
            Equal(10m, result.DeviationFromAveragePercent, "Benchmark deviation changed.");
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

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
