using System;
using System.Collections.Generic;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TenderEvaluationAdditivePrecisionSmoke
    {
        internal static void Run()
        {
            SwallowedLineContributionFailsClosed();
            RepresentableLowOrderLineRemainsAccepted();
            ExactZeroLineRemainsAccepted();
            OrdinaryRankingRemainsStable();
        }

        private static void SwallowedLineContributionFailsClosed()
        {
            var requirements = new[]
            {
                new TenderRequirement("A-LARGE", "Large item", "ea", 1m),
                new TenderRequirement("B-SMALL", "Small item", "ea", 1m)
            };
            var bids = new[]
            {
                new TenderBid(
                    "BID-SWALLOWED",
                    "Bidder",
                    "VND",
                    new[]
                    {
                        new TenderQuoteLine("A-LARGE", 70000000000000000000000000000m),
                        new TenderQuoteLine("B-SMALL", 0.1m)
                    })
            };

            Throws<OverflowException>(() => new TenderEvaluationService().Evaluate(requirements, bids));
        }

        private static void RepresentableLowOrderLineRemainsAccepted()
        {
            var results = new TenderEvaluationService().Evaluate(
                new[]
                {
                    new TenderRequirement("A-LARGE", "Large item", "ea", 1m),
                    new TenderRequirement("B-ONE", "Representable item", "ea", 1m)
                },
                new[]
                {
                    new TenderBid(
                        "BID-REPRESENTABLE",
                        "Bidder",
                        "VND",
                        new[]
                        {
                            new TenderQuoteLine("A-LARGE", 70000000000000000000000000000m),
                            new TenderQuoteLine("B-ONE", 1m)
                        })
                });

            Equal(1, results.Count, "Representable tender result count changed.");
            Equal(
                70000000000000000000000000001m,
                results[0].EvaluatedTotal,
                "Representable low-order tender line changed.");
            Equal(1, results[0].Rank, "Single complete tender rank changed.");
        }

        private static void ExactZeroLineRemainsAccepted()
        {
            var results = new TenderEvaluationService().Evaluate(
                new[]
                {
                    new TenderRequirement("A-LARGE", "Large item", "ea", 1m),
                    new TenderRequirement("B-ZERO", "Zero quantity item", "ea", 0m)
                },
                new[]
                {
                    new TenderBid(
                        "BID-ZERO",
                        "Bidder",
                        "VND",
                        new[]
                        {
                            new TenderQuoteLine("A-LARGE", 70000000000000000000000000000m),
                            new TenderQuoteLine("B-ZERO", 123m)
                        })
                });

            Equal(
                70000000000000000000000000000m,
                results[0].EvaluatedTotal,
                "Exact-zero tender line should not change the total.");
        }

        private static void OrdinaryRankingRemainsStable()
        {
            var requirements = new[]
            {
                new TenderRequirement("A", "Item A", "ea", 2m),
                new TenderRequirement("B", "Item B", "ea", 1m)
            };
            var results = new TenderEvaluationService().Evaluate(
                requirements,
                new[]
                {
                    new TenderBid(
                        "BID-A",
                        "Bidder A",
                        "VND",
                        new[]
                        {
                            new TenderQuoteLine("A", 50m),
                            new TenderQuoteLine("B", 25m)
                        }),
                    new TenderBid(
                        "BID-B",
                        "Bidder B",
                        "VND",
                        new[]
                        {
                            new TenderQuoteLine("A", 60m),
                            new TenderQuoteLine("B", 20m)
                        })
                });

            Equal(2, results.Count, "Ordinary tender result count changed.");
            Equal("BID-A", results[0].BidId, "Tender result ordering changed.");
            Equal(125m, results[0].EvaluatedTotal, "Bid A total changed.");
            Equal(1, results[0].Rank, "Bid A rank changed.");
            Equal("BID-B", results[1].BidId, "Tender result ordering changed.");
            Equal(140m, results[1].EvaluatedTotal, "Bid B total changed.");
            Equal(2, results[1].Rank, "Bid B rank changed.");
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
