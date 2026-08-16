using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TradeCostAdditivePrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            SwallowedTrailingContributionFailsClosed();
            SwallowedAccumulatedSubtotalFailsClosed();
            RepresentableLowOrderContributionRemainsAccepted();
            ZeroAndOrdinaryGroupingRemainStable();
        }

        private static void SwallowedTrailingContributionFailsClosed()
        {
            var service = new TradeCostAnalysisService();
            var items = new[]
            {
                new TradeCostItem("LARGE", "Structure", 70000000000000000000000000000m),
                new TradeCostItem("SMALL", "Structure", 0.1m)
            };

            Throws<OverflowException>(() => service.Analyze(items, 0m));
        }

        private static void SwallowedAccumulatedSubtotalFailsClosed()
        {
            var service = new TradeCostAnalysisService();
            var items = new[]
            {
                new TradeCostItem("SMALL", "Structure", 0.1m),
                new TradeCostItem("LARGE", "Structure", 70000000000000000000000000000m)
            };

            Throws<OverflowException>(() => service.Analyze(items, 0m));
        }

        private static void RepresentableLowOrderContributionRemainsAccepted()
        {
            var rows = new TradeCostAnalysisService().Analyze(
                new[]
                {
                    new TradeCostItem("LARGE", "Structure", 70000000000000000000000000000m),
                    new TradeCostItem("ONE", "Structure", 1m)
                },
                0m);

            Equal(1, rows.Count, "Representable aggregate should produce one trade row.");
            Equal(2, rows[0].ItemCount, "Representable aggregate item count changed.");
            Equal(
                70000000000000000000000000001m,
                rows[0].TotalCost,
                "Representable low-order contribution changed.");
            Equal<decimal?>(null, rows[0].CostPerCfaM2, "Zero CFA should still suppress cost-per-CFA output.");
        }

        private static void ZeroAndOrdinaryGroupingRemainStable()
        {
            var rows = new TradeCostAnalysisService().Analyze(
                new[]
                {
                    new TradeCostItem("A", "mep", 0m),
                    new TradeCostItem("B", "MEP", 10m),
                    new TradeCostItem("C", null, 6m)
                },
                2m);

            Equal(2, rows.Count, "Case-insensitive trade grouping changed.");
            Equal("MEP", rows[0].TradeCode, "Trade-code canonical casing changed.");
            Equal(2, rows[0].ItemCount, "Zero-cost item should remain counted.");
            Equal(10m, rows[0].TotalCost, "Zero-cost item should not perturb the trade total.");
            Equal<decimal?>(5m, rows[0].CostPerCfaM2, "Ordinary CFA division changed.");
            Equal("Unclassified", rows[1].TradeCode, "Unclassified trade ordering changed.");
            Equal(6m, rows[1].TotalCost, "Unclassified trade total changed.");
            Equal<decimal?>(3m, rows[1].CostPerCfaM2, "Unclassified CFA division changed.");
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
