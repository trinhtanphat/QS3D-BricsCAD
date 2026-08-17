using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostBenchmarkMedianPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PrecisionLossFailsClosed();
            OrdinaryEvenMedianRemainsExact();
            OddMedianRemainsExact();
        }

        private static void PrecisionLossFailsClosed()
        {
            const decimal a = 79228162514264337593543950330m;
            var catalog = Catalog(a - 4m, a, a + 1m, a + 3m);
            MustThrow<OverflowException>(
                () => new CostBenchmarkService().Analyze(catalog, "BENCH", "DIM", "USD", a),
                "A non-zero half-step that decimal precision cannot add to the lower middle value must fail closed.");
        }

        private static void OrdinaryEvenMedianRemainsExact()
        {
            var result = new CostBenchmarkService().Analyze(
                Catalog(10m, 20m, 30m, 40m), "BENCH", "DIM", "USD", 25m);
            Equal(25m, result.MedianUnitCost, "ordinary even median");
            Equal(4, result.SampleCount, "ordinary even sample count");
        }

        private static void OddMedianRemainsExact()
        {
            var result = new CostBenchmarkService().Analyze(
                Catalog(10m, 20m, 30m), "BENCH", "DIM", "USD", 20m);
            Equal(20m, result.MedianUnitCost, "odd median");
            Equal(3, result.SampleCount, "odd sample count");
        }

        private static HistoricalCostCatalog Catalog(params decimal[] unitCosts)
        {
            var records = new HistoricalCostRecord[unitCosts.Length];
            for (var i = 0; i < unitCosts.Length; i++)
            {
                records[i] = new HistoricalCostRecord(
                    "R" + i,
                    "BENCH",
                    "DIM",
                    1m,
                    unitCosts[i],
                    "USD",
                    new DateTime(2026, 1, 1, 0, 0, i, DateTimeKind.Utc));
            }
            return new HistoricalCostCatalog(records);
        }

        private static void MustThrow<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ": " + message);
        }

        private static void Equal(decimal expected, decimal actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + " mismatch. Expected " + expected + ", actual " + actual + ".");
        }

        private static void Equal(int expected, int actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + " mismatch. Expected " + expected + ", actual " + actual + ".");
        }
    }
}
