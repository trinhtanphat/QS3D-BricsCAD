using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TradeCostAggregationPrecisionSmoke
    {
        private const decimal Large = 10000000000000000000000000000m;
        private const decimal ExpectedRecovered = 10000000000000000000000000001m;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RecoverableHalfUnitsArePreserved();
            InputOrderDoesNotChangeRepresentableTotal();
            SeparateTradesRemainIndependent();
            OrdinaryAggregationRemainsExact();
            UnrepresentableFinalTotalFailsClosed();
            Console.WriteLine("PASS Trade Cost aggregate precision");
        }

        private static void RecoverableHalfUnitsArePreserved()
        {
            var rows = Analyze(
                Item("A-LARGE", "Trade-A", Large),
                Item("A-HALF-1", "Trade-A", 0.5m),
                Item("A-HALF-2", "Trade-A", 0.5m));

            Require(rows.Count == 1, "recoverable Trade Cost aggregate must produce one row");
            Require(rows[0].TotalCost == ExpectedRecovered,
                "recoverable Trade Cost aggregate must preserve two half-unit contributions");
            Require(rows[0].ItemCount == 3,
                "recoverable Trade Cost aggregate must preserve item count");
        }

        private static void InputOrderDoesNotChangeRepresentableTotal()
        {
            var forward = Analyze(
                Item("F-LARGE", "Trade-F", Large),
                Item("F-HALF-1", "Trade-F", 0.5m),
                Item("F-HALF-2", "Trade-F", 0.5m));
            var reverse = Analyze(
                Item("R-HALF-1", "Trade-R", 0.5m),
                Item("R-HALF-2", "Trade-R", 0.5m),
                Item("R-LARGE", "Trade-R", Large));

            Require(forward[0].TotalCost == ExpectedRecovered,
                "forward high-dynamic-range aggregate must remain exact");
            Require(reverse[0].TotalCost == ExpectedRecovered,
                "reverse high-dynamic-range aggregate must remain exact");
        }

        private static void SeparateTradesRemainIndependent()
        {
            var rows = Analyze(
                Item("A-LARGE-2", "Trade-A", Large),
                Item("B-ONE", "Trade-B", 2m),
                Item("A-HALF-3", "Trade-A", 0.5m),
                Item("A-HALF-4", "Trade-A", 0.5m),
                Item("B-TWO", "Trade-B", 3m));

            Require(rows.Count == 2, "separate trades must produce two rows");
            Require(rows[0].TradeCode == "Trade-A" && rows[0].TotalCost == ExpectedRecovered,
                "Trade-A must retain its exact recovered total");
            Require(rows[1].TradeCode == "Trade-B" && rows[1].TotalCost == 5m,
                "Trade-B must retain its independent ordinary total");
        }

        private static void OrdinaryAggregationRemainsExact()
        {
            var rows = Analyze(
                Item("O-1", "Ordinary", 10.25m),
                Item("O-2", "Ordinary", 2.75m),
                Item("O-3", "Ordinary", 7m));

            Require(rows.Count == 1 && rows[0].TotalCost == 20m,
                "ordinary Trade Cost aggregation must remain exact");
        }

        private static void UnrepresentableFinalTotalFailsClosed()
        {
            try
            {
                Analyze(
                    Item("MAX", "Overflow", decimal.MaxValue),
                    Item("PLUS", "Overflow", 1m));
            }
            catch (OverflowException)
            {
                return;
            }

            throw new InvalidOperationException(
                "Trade Cost aggregation accepted an exact final total outside decimal range.");
        }

        private static IReadOnlyList<TradeCostAnalysisRow> Analyze(params TradeCostItem[] items) =>
            new TradeCostAnalysisService().Analyze(items, 0m);

        private static TradeCostItem Item(string code, string trade, decimal cost) =>
            new TradeCostItem(code, trade, cost);

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
