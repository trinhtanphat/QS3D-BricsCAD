using System;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostBenchmarkMedianPrecisionSmoke
    {
        private const decimal HighMiddle = 79228162514264337593543950330m;
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            HighMagnitudeEvenMedianFailsClosed();
            OrdinaryEvenMedianRemainsStable();
            OddMedianRemainsStable();
        }

        private static void HighMagnitudeEvenMedianFailsClosed()
        {
            var error = Capture<OverflowException>(() =>
                Analyze(HighMiddle, HighMiddle - 4m, HighMiddle, HighMiddle + 1m, HighMiddle + 3m));

            Contains(
                "Cost addition precision loss: benchmark median.",
                error.Message,
                "Even-sample benchmark median must fail closed when a non-zero half-difference cannot affect the lower middle value.");
        }

        private static void OrdinaryEvenMedianRemainsStable()
        {
            var result = Analyze(25m, 10m, 20m, 30m, 40m);

            Equal(4, result.SampleCount, "Ordinary even benchmark sample count changed.");
            Equal(10m, result.MinimumUnitCost, "Ordinary even benchmark minimum changed.");
            Equal(40m, result.MaximumUnitCost, "Ordinary even benchmark maximum changed.");
            Equal(25m, result.AverageUnitCost, "Ordinary even benchmark average changed.");
            Equal(25m, result.MedianUnitCost, "Ordinary even benchmark median changed.");
            Equal(25m, result.CurrentUnitCost, "Ordinary even benchmark current value changed.");
            Equal<decimal?>(0m, result.DeviationFromAveragePercent, "Ordinary even benchmark deviation changed.");
        }

        private static void OddMedianRemainsStable()
        {
            var result = Analyze(20m, 10m, 20m, 30m);

            Equal(3, result.SampleCount, "Odd benchmark sample count changed.");
            Equal(10m, result.MinimumUnitCost, "Odd benchmark minimum changed.");
            Equal(30m, result.MaximumUnitCost, "Odd benchmark maximum changed.");
            Equal(20m, result.AverageUnitCost, "Odd benchmark average changed.");
            Equal(20m, result.MedianUnitCost, "Odd benchmark median changed.");
            Equal(20m, result.CurrentUnitCost, "Odd benchmark current value changed.");
            Equal<decimal?>(0m, result.DeviationFromAveragePercent, "Odd benchmark deviation changed.");
        }

        private static CostBenchmarkResult Analyze(decimal currentUnitCost, params decimal[] unitCosts)
        {
            var records = new HistoricalCostRecord[unitCosts.Length];
            for (var index = 0; index < unitCosts.Length; index++)
            {
                records[index] = new HistoricalCostRecord(
                    "MEDIAN-" + index,
                    "BUILDING",
                    "OFFICE",
                    1m,
                    unitCosts[index],
                    "VND",
                    StartUtc.AddTicks(index));
            }

            return new CostBenchmarkService().Analyze(
                new HistoricalCostCatalog(records),
                "BUILDING",
                "OFFICE",
                "VND",
                currentUnitCost);
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
