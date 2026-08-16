using System;
using System.Collections.Generic;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TenderEvaluationAdditivePrecisionSmoke
    {
        internal static void Run()
        {
            SwallowedEvaluatedLineContributionFailsClosed();
            RepresentableLowOrderContributionRemainsAccepted();
            ExactZeroLineRemainsAccepted();
            AccumulatedOverflowRemainsFailClosed();
            OrdinaryRankingAndMissingItemSemanticsRemainStable();
        }

        private static void SwallowedEvaluatedLineContributionFailsClosed()
        {
            var requirements = new[]
            {
                new TenderRequirement("A-LARGE", "Large item", "ea", 1m),
                new TenderRequirement("B-SMALL", "Small item", "ea", 1m)
            };
            var bid = new TenderBid(
                "BID-SWALLOWED",
                "Precision bidder",
                "VND",
                new[]
                {
                    new TenderQuoteLine("A-LARGE", 70000000000000000000000000000m),
                    new TenderQuoteLine("B-SMALL", 0.1m)
                });

            Throws<OverflowException>(() =>
                new TenderEvaluationService().Evaluate(requirements, new[] { bid }));
        }

        private static void RepresentableLowOrderContributionRemainsAccepted()
        {
            var requirements = new[]
            {
                new TenderRequirement("A-LARGE", "Large item", "ea", 1m),
                new TenderRequirement("B-ONE", "Representable low-order item", "ea", 1m)
            };
            var bid = new TenderBid(
                "BID-REPRESENTABLE",
                "Precision bidder",
                "VND",
                new[]
                {
                    new TenderQuoteLine("A-LARGE", 70000000000000000000000000000m),
                    new TenderQuoteLine("B-ONE", 1m)
                });

            var results = new TenderEvaluationService().Evaluate(requirements, new[] { bid });
            Equal(1, results.Count, "Representable tender evaluation result count changed.");
            Equal(
                70000000000000000000000000001m,
                results[0].EvaluatedTotal,
                "Representable low-order tender contribution changed.");
            Equal(1, results[0].Rank, "Single complete tender rank changed.");
        }

        private static void ExactZeroLineRemainsAccepted()
        {
            var requirements = new[]
            {
                new TenderRequirement("A-LARGE", "Large item", "ea", 1m),
                new TenderRequirement("B-ZERO", "Zero quantity item", "ea", 0m)
            };
            var bid = new TenderBid(
                "BID-ZERO",
                "Zero control bidder",
                "VND",
                new[]
                {
                    new TenderQuoteLine("A-LARGE", 70000000000000000000000000000m),
                    new TenderQuoteLine("B-ZERO", 123m)
                });

            var result = new TenderEvaluationService().Evaluate(requirements, new[] { bid })[0];
            Equal(
                70000000000000000000000000000m,
                result.EvaluatedTotal,
                "Exact-zero tender line must not change the evaluated total.");
        }

        private static void AccumulatedOverflowRemainsFailClosed()
        {
            var requirements = new[]
            {
                new TenderRequirement("A-LARGE", "Large item", "ea", 1m),
                new TenderRequirement("B-OVERFLOW", "Overflow item", "ea", 1m)
            };
            var bid = new TenderBid(
                "BID-OVERFLOW",
                "Overflow bidder",
                "VND",
                new[]
                {
                    new TenderQuoteLine("A-LARGE", 70000000000000000000000000000m),
                    new TenderQuoteLine("B-OVERFLOW", 10000000000000000000000000000m)
                });

            Throws<OverflowException>(() =>
                new TenderEvaluationService().Evaluate(requirements, new[] { bid }));
        }

        private static void OrdinaryRankingAndMissingItemSemanticsRemainStable()
        {
            var requirements = new[]
            {
                new TenderRequirement("A", "First item", "ea", 2m),
                new TenderRequirement("B", "Second item", "ea", 3m)
            };
            var bids = new[]
            {
                new TenderBid(
                    "BID-A",
                    "Lower complete bidder",
                    "VND",
                    new[]
                    {
                        new TenderQuoteLine("A", 10m),
                        new TenderQuoteLine("B", 5m)
                    }),
                new TenderBid(
                    "BID-B",
                    "Higher complete bidder",
                    "VND",
                    new[]
                    {
                        new TenderQuoteLine("A", 12m),
                        new TenderQuoteLine("B", 5m)
                    }),
                new TenderBid(
                    "BID-C",
                    "Incomplete bidder",
                    "VND",
                    new[]
                    {
                        new TenderQuoteLine("A", 1m)
                    })
            };

            var results = new TenderEvaluationService().Evaluate(requirements, bids);
            Equal(3, results.Count, "Ordinary tender result count changed.");
            Equal("BID-A", results[0].BidId, "Tender result ordering changed.");
            Equal(35m, results[0].EvaluatedTotal, "Lower complete tender total changed.");
            Equal(1, results[0].Rank, "Lower complete tender rank changed.");
            Equal("BID-B", results[1].BidId, "Tender result ordering changed.");
            Equal(39m, results[1].EvaluatedTotal, "Higher complete tender total changed.");
            Equal(2, results[1].Rank, "Higher complete tender rank changed.");
            Equal("BID-C", results[2].BidId, "Tender result ordering changed.");
            Equal(2m, results[2].EvaluatedTotal, "Incomplete tender partial total changed.");
            Equal(0, results[2].Rank, "Incomplete tender must remain unranked.");
            Equal(1, results[2].MissingItemCodes.Count, "Incomplete tender missing-item count changed.");
            Equal("B", results[2].MissingItemCodes[0], "Incomplete tender missing-item identity changed.");
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
