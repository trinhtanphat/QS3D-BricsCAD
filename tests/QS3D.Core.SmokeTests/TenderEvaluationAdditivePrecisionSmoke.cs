using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TenderEvaluationAdditivePrecisionSmoke
    {
        internal static void Run()
        {
            SwallowedNonZeroContributionFailsClosed();
            RepresentableLowOrderContributionIsPreserved();
            OrdinaryTotalsAndRankingRemainStable();
            ZeroAndMissingLineSemanticsRemainStable();
            ArithmeticOverflowStillFailsClosed();
        }

        private static void SwallowedNonZeroContributionFailsClosed()
        {
            var requirements = new[]
            {
                Requirement("A-HUGE", 10000000000000000000000000000m),
                Requirement("B-TINY", 0.1m)
            };
            var bid = Bid(
                "BID-PRECISION",
                Line("A-HUGE", 1m),
                Line("B-TINY", 1m));

            var error = Capture<OverflowException>(() =>
                new TenderEvaluationService().Evaluate(requirements, new[] { bid }));

            Contains(
                "Cost addition precision loss: tender evaluated total.",
                error.Message,
                "Tender evaluation must fail closed when decimal addition swallows a non-zero line contribution.");
        }

        private static void RepresentableLowOrderContributionIsPreserved()
        {
            var requirements = new[]
            {
                Requirement("A-HUGE", 100000000000000000000m),
                Requirement("B-UNIT", 1m)
            };
            var bid = Bid(
                "BID-REPRESENTABLE",
                Line("A-HUGE", 1m),
                Line("B-UNIT", 1m));

            var result = new TenderEvaluationService().Evaluate(requirements, new[] { bid });

            Equal(1, result.Count, "Representable tender evaluation returned the wrong bid count.");
            Equal(100000000000000000001m, result[0].EvaluatedTotal, "Representable low-order tender contribution was not preserved.");
            Equal(1, result[0].Rank, "Single complete tender bid rank changed.");
        }

        private static void OrdinaryTotalsAndRankingRemainStable()
        {
            var requirements = new[]
            {
                Requirement("A", 2m),
                Requirement("B", 3m)
            };
            var bids = new[]
            {
                Bid("BID-B", Line("A", 10m), Line("B", 20m)),
                Bid("BID-A", Line("A", 5m), Line("B", 10m))
            };

            var result = new TenderEvaluationService().Evaluate(requirements, bids);

            Equal(2, result.Count, "Ordinary tender evaluation returned the wrong bid count.");
            Equal("BID-A", result[0].BidId, "Tender result output ordering by bid id changed.");
            Equal(40m, result[0].EvaluatedTotal, "Ordinary tender total changed for BID-A.");
            Equal(1, result[0].Rank, "Lower complete tender total must rank first.");
            Equal("BID-B", result[1].BidId, "Tender result output ordering by bid id changed.");
            Equal(80m, result[1].EvaluatedTotal, "Ordinary tender total changed for BID-B.");
            Equal(2, result[1].Rank, "Higher complete tender total must rank second.");
        }

        private static void ZeroAndMissingLineSemanticsRemainStable()
        {
            var requirements = new[]
            {
                Requirement("A-ZERO", 0m),
                Requirement("B-MISSING", 5m)
            };
            var bid = Bid("BID-INCOMPLETE", Line("A-ZERO", decimal.MaxValue));

            var result = new TenderEvaluationService().Evaluate(requirements, new[] { bid });

            Equal(1, result.Count, "Incomplete tender evaluation returned the wrong bid count.");
            Equal(0m, result[0].EvaluatedTotal, "Exact-zero tender contribution must remain zero.");
            Equal(0, result[0].Rank, "Incomplete tender bid must remain unranked.");
            Equal(1, result[0].MissingItemCodes.Count, "Missing tender item count changed.");
            Equal("B-MISSING", result[0].MissingItemCodes[0], "Missing tender item identity changed.");
        }

        private static void ArithmeticOverflowStillFailsClosed()
        {
            var requirements = new[] { Requirement("A-OVERFLOW", decimal.MaxValue) };
            var bid = Bid("BID-OVERFLOW", Line("A-OVERFLOW", 2m));

            Capture<OverflowException>(() =>
                new TenderEvaluationService().Evaluate(requirements, new[] { bid }));
        }

        private static TenderRequirement Requirement(string itemCode, decimal quantity)
        {
            return new TenderRequirement(itemCode, itemCode, "m", quantity);
        }

        private static TenderQuoteLine Line(string itemCode, decimal unitRate)
        {
            return new TenderQuoteLine(itemCode, unitRate);
        }

        private static TenderBid Bid(string bidId, params TenderQuoteLine[] lines)
        {
            return new TenderBid(bidId, bidId, "VND", lines);
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
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class TenderEvaluationAdditivePrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TenderEvaluationAdditivePrecisionSmoke.Run();
        }
    }
}
