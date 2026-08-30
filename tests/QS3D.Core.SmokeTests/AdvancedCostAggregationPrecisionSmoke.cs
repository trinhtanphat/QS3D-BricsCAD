using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class AdvancedCostAggregationPrecisionSmoke
    {
        private const decimal Large = 10000000000000000000000000000m;
        private const decimal ExpectedRecovered = 10000000000000000000000000001m;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RateBuildUpPreservesRecoverableContributions();
            RateBuildUpIsInputOrderIndependent();
            BenchmarkAveragePreservesRecoverableContributions();
            BenchmarkAverageIsInputOrderIndependent();
            OrdinaryControlsRemainExact();
            FinalUnrepresentableRateBuildUpFailsClosed();
            Console.WriteLine("PASS advanced cost aggregate precision");
        }

        private static void RateBuildUpPreservesRecoverableContributions()
        {
            var buildUp = BuildUp(
                Component("A-0-LARGE", Large),
                Component("A-1-HALF", 0.5m),
                Component("A-2-HALF", 0.5m));
            Require(buildUp.DirectUnitCost == ExpectedRecovered,
                "rate build-up must preserve recoverable half-unit contributions");
        }

        private static void RateBuildUpIsInputOrderIndependent()
        {
            var buildUp = BuildUp(
                Component("B-2-HALF", 0.5m),
                Component("B-1-HALF", 0.5m),
                Component("B-0-LARGE", Large));
            Require(buildUp.DirectUnitCost == ExpectedRecovered,
                "rate build-up exact total must not depend on caller input order");
        }

        private static void BenchmarkAveragePreservesRecoverableContributions()
        {
            var result = Benchmark(
                Record("A-0-LARGE", Large),
                Record("A-1-HALF", 0.5m),
                Record("A-2-HALF", 0.5m));
            var expected = ExpectedRecovered / 3m;
            Require(result.AverageUnitCost == expected,
                "benchmark average must be derived from the complete exact sample sum");
        }

        private static void BenchmarkAverageIsInputOrderIndependent()
        {
            var result = Benchmark(
                Record("B-2-HALF", 0.5m),
                Record("B-1-HALF", 0.5m),
                Record("B-0-LARGE", Large));
            var expected = ExpectedRecovered / 3m;
            Require(result.AverageUnitCost == expected,
                "benchmark average must not reject a representable complete sum based on caller input order");
        }

        private static void OrdinaryControlsRemainExact()
        {
            var buildUp = BuildUp(Component("O-1", 10.25m), Component("O-2", 2.75m), Component("O-3", 7m));
            Require(buildUp.DirectUnitCost == 20m, "ordinary rate build-up total must remain exact");

            var benchmark = Benchmark(Record("O-1", 10m), Record("O-2", 20m), Record("O-3", 30m));
            Require(benchmark.AverageUnitCost == 20m, "ordinary benchmark average must remain exact");
        }

        private static void FinalUnrepresentableRateBuildUpFailsClosed()
        {
            try
            {
                BuildUp(Component("MAX", decimal.MaxValue), Component("ONE", 1m));
            }
            catch (OverflowException)
            {
                return;
            }
            throw new InvalidOperationException("unrepresentable rate build-up total must fail closed");
        }

        private static CostRateBuildUp BuildUp(params CostResourceComponent[] components) =>
            new CostRateBuildUp("precision-build-up", new CostCode("03.10"), "m2", "USD", components);

        private static CostResourceComponent Component(string code, decimal extendedCost) =>
            new CostResourceComponent(code, "Precision component " + code, "ea", 1m, extendedCost);

        private static CostBenchmarkResult Benchmark(params HistoricalCostRecord[] records)
        {
            var catalog = new HistoricalCostCatalog(records);
            return new CostBenchmarkService().Analyze(catalog, "benchmark", "dimension", "USD", 1m);
        }

        private static HistoricalCostRecord Record(string id, decimal unitCost) =>
            new HistoricalCostRecord(id, "benchmark", "dimension", 1m, unitCost, "USD", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
