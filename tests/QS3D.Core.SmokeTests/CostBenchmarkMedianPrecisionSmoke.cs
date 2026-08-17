using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostBenchmarkMedianPrecisionSmoke
    {
        private const decimal HighMiddle = 79228162514264337593543950330m;
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            var catalog = new HistoricalCostCatalog(new[]
            {
                Record("MEDIAN-0", HighMiddle - 4m, 0),
                Record("MEDIAN-1", HighMiddle, 1),
                Record("MEDIAN-2", HighMiddle + 1m, 2),
                Record("MEDIAN-3", HighMiddle + 3m, 3),
            });

            var error = Capture<OverflowException>(() =>
                new CostBenchmarkService().Analyze(
                    catalog,
                    "BUILDING",
                    "OFFICE",
                    "VND",
                    HighMiddle));

            Contains(
                "Cost addition precision loss: benchmark median.",
                error.Message,
                "Even-sample benchmark median must fail closed when a non-zero half-difference cannot affect the lower middle value.");
        }

        private static HistoricalCostRecord Record(string id, decimal unitCost, int tickOffset)
        {
            return new HistoricalCostRecord(
                id,
                "BUILDING",
                "OFFICE",
                1m,
                unitCost,
                "VND",
                StartUtc.AddTicks(tickOffset));
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
    }

    internal static class CostBenchmarkMedianPrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CostBenchmarkMedianPrecisionSmoke.Run();
        }
    }
}
