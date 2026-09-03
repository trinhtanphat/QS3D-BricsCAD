using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimatingPortfolioAggregationPrecisionSmoke
    {
        private const decimal Large = 10000000000000000000000000000m;
        private const decimal ExpectedRecovered = 10000000000000000000000000001m;

        [ModuleInitializer]
        internal static void Initialize()
        {
            PortfolioPreservesRecoverableContributions();
            OrdinaryAndUnpricedControlsRemainCorrect();
            BulkPreviewPreservesRecoverableAggregates();
            FinalUnrepresentablePortfolioTotalFailsClosed();
            Console.WriteLine("PASS estimating portfolio aggregation precision");
        }

        private static void PortfolioPreservesRecoverableContributions()
        {
            var portfolio = new EstimatingPortfolio(new[]
            {
                PricedLine("A-LARGE", 1m, Large),
                PricedLine("B-HALF", 1m, 0.5m),
                PricedLine("C-HALF", 1m, 0.5m)
            });

            Require(portfolio.PricedTotal == ExpectedRecovered,
                "estimating portfolio must preserve recoverable half-unit contributions");
        }

        private static void OrdinaryAndUnpricedControlsRemainCorrect()
        {
            var portfolio = new EstimatingPortfolio(new[]
            {
                PricedLine("A", 2m, 10m),
                UnpricedLine("B", 999m),
                PricedLine("C", 3m, 5m)
            });

            Require(portfolio.PricedTotal == 35m,
                "ordinary priced total must remain exact and exclude unpriced lines");
        }

        private static void BulkPreviewPreservesRecoverableAggregates()
        {
            var portfolio = new EstimatingPortfolio(new[]
            {
                PricedLine("A-LARGE", Large, 1m),
                PricedLine("B-HALF", 0.5m, 1m),
                PricedLine("C-HALF", 0.5m, 1m)
            });
            var request = new BulkRateAssignmentRequest(
                new[] { "A-LARGE", "B-HALF", "C-HALF" },
                "NEW-COST",
                "NEW-RATES",
                "R2",
                new[] { new UnitRateAssignment("m2", 1m) });

            var preview = new EstimatingWorkflowService().PreviewBulkRateAssignment(portfolio, request);

            Require(preview.TotalBefore == ExpectedRecovered,
                "bulk preview total-before must preserve recoverable contributions");
            Require(preview.TotalAfter == ExpectedRecovered,
                "bulk preview total-after must preserve recoverable contributions");
            Require(preview.UnitDistribution.Count == 1,
                "bulk preview must preserve one unit distribution row");
            Require(preview.UnitDistribution[0].Quantity == ExpectedRecovered,
                "bulk preview unit quantity must preserve recoverable contributions");
            Require(preview.ValueDelta == 0m,
                "bulk preview parity control must retain a zero value delta");
        }

        private static void FinalUnrepresentablePortfolioTotalFailsClosed()
        {
            var portfolio = new EstimatingPortfolio(new[]
            {
                PricedLine("MAX", 1m, decimal.MaxValue),
                PricedLine("ONE", 1m, 1m)
            });

            try
            {
                _ = portfolio.PricedTotal;
            }
            catch (OverflowException)
            {
                return;
            }

            throw new InvalidOperationException("unrepresentable estimating portfolio total must fail closed");
        }

        private static EstimatingLine PricedLine(string id, decimal quantity, decimal rate) =>
            new EstimatingLine(
                id,
                "quantity-" + id,
                "Q1",
                quantity,
                "m2",
                "COST",
                "RATES",
                "R1",
                rate);

        private static EstimatingLine UnpricedLine(string id, decimal quantity) =>
            new EstimatingLine(
                id,
                "quantity-" + id,
                "Q1",
                quantity,
                "m2",
                "COST");

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
