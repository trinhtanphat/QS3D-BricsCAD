using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TenderCollectionBoundSmoke
    {
        private const int MaximumEntries = 10000;

        internal static void Run()
        {
            CountedQuoteLinesFailBeforeEnumeration();
            StreamingQuoteLinesStopAtFirstDisallowedEntry();
            ExactQuoteLineBoundaryIsAccepted();
            CountedRequirementsFailBeforeEnumeration();
            StreamingRequirementsStopAtFirstDisallowedEntry();
            ExactRequirementBoundaryIsAccepted();
            CountedBidsFailBeforeEnumeration();
            StreamingBidsStopAtFirstDisallowedEntry();
            ExactBidBoundaryIsAccepted();
            OrdinaryTenderEvaluationSemanticsArePreserved();
        }

        private static void CountedQuoteLinesFailBeforeEnumeration()
        {
            var lines = new CountedNeverEnumerated<TenderQuoteLine>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() =>
                new TenderBid("BID-COUNTED", "Bidder", "VND", lines));

            Equal(0, lines.GetEnumeratorCalls, "Known oversized tender quote lines must fail before enumeration.");
            Contains("at most 10000", error.Message, "Tender quote-line bound must be reported.");
        }

        private static void StreamingQuoteLinesStopAtFirstDisallowedEntry()
        {
            var lines = new StreamingQuoteLines(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() =>
                new TenderBid("BID-STREAM", "Bidder", "VND", lines));

            Equal(MaximumEntries + 1, lines.YieldedCount, "Tender quote ingestion must stop after observing line 10,001.");
            Contains("at most 10000", error.Message, "Streaming tender quote-line bound must be reported.");
        }

        private static void ExactQuoteLineBoundaryIsAccepted()
        {
            var lines = new TenderQuoteLine[MaximumEntries];
            for (var i = 0; i < lines.Length; i++)
                lines[i] = QuoteLine(i);

            var bid = new TenderBid("BID-BOUNDARY", "Bidder", "VND", lines);
            Equal(MaximumEntries, bid.Lines.Count, "TenderBid must accept exactly 10,000 valid quote lines.");
            Equal(1m, bid.Lines["ITEM-00000"].UnitRate, "Tender quote-line values changed at the boundary.");
            Equal(10000m, bid.Lines["ITEM-09999"].UnitRate, "Tender quote-line final value changed at the boundary.");
        }

        private static void CountedRequirementsFailBeforeEnumeration()
        {
            var requirements = new CountedNeverEnumerated<TenderRequirement>(MaximumEntries + 1);
            var bids = new CountedNeverEnumerated<TenderBid>(0);
            var error = Capture<InvalidOperationException>(() =>
                new TenderEvaluationService().Evaluate(requirements, bids));

            Equal(0, requirements.GetEnumeratorCalls, "Known oversized tender requirements must fail before enumeration.");
            Equal(0, bids.GetEnumeratorCalls, "Bids must not be enumerated after counted requirement rejection.");
            Contains("at most 10000", error.Message, "Tender requirement bound must be reported.");
        }

        private static void StreamingRequirementsStopAtFirstDisallowedEntry()
        {
            var requirements = new StreamingRequirements(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() =>
                new TenderEvaluationService().Evaluate(requirements, Array.Empty<TenderBid>()));

            Equal(MaximumEntries + 1, requirements.YieldedCount, "Tender requirement ingestion must stop after observing item 10,001.");
            Contains("at most 10000", error.Message, "Streaming tender requirement bound must be reported.");
        }

        private static void ExactRequirementBoundaryIsAccepted()
        {
            var requirements = new TenderRequirement[MaximumEntries];
            for (var i = 0; i < requirements.Length; i++)
                requirements[i] = Requirement(i);

            var result = new TenderEvaluationService().Evaluate(requirements, Array.Empty<TenderBid>());
            Equal(0, result.Count, "Exactly 10,000 tender requirements must remain valid when there are no bids.");
        }

        private static void CountedBidsFailBeforeEnumeration()
        {
            var requirements = new CountedNeverEnumerated<TenderRequirement>(0);
            var bids = new CountedNeverEnumerated<TenderBid>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() =>
                new TenderEvaluationService().Evaluate(requirements, bids));

            Equal(1, requirements.GetEnumeratorCalls, "Empty counted requirements should be materialized before bid validation.");
            Equal(0, bids.GetEnumeratorCalls, "Known oversized tender bids must fail before bid enumeration.");
            Contains("at most 10000", error.Message, "Tender bid comparison bound must be reported.");
        }

        private static void StreamingBidsStopAtFirstDisallowedEntry()
        {
            var bids = new StreamingBids(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() =>
                new TenderEvaluationService().Evaluate(Array.Empty<TenderRequirement>(), bids));

            Equal(MaximumEntries + 1, bids.YieldedCount, "Tender bid ingestion must stop after observing bid 10,001.");
            Contains("at most 10000", error.Message, "Streaming tender bid bound must be reported.");
        }

        private static void ExactBidBoundaryIsAccepted()
        {
            var bids = new TenderBid[MaximumEntries];
            for (var i = 0; i < bids.Length; i++)
                bids[i] = Bid(i);

            var result = new TenderEvaluationService().Evaluate(Array.Empty<TenderRequirement>(), bids);
            Equal(MaximumEntries, result.Count, "Tender evaluation must accept exactly 10,000 valid bids.");
            Equal("BID-00000", result[0].BidId, "Tender result ordering changed at the first bid.");
            Equal("BID-09999", result[result.Count - 1].BidId, "Tender result ordering changed at the final bid.");
            Equal(1, result[0].Rank, "Empty-requirement complete bids must retain deterministic rank semantics.");
            Equal(MaximumEntries, result[result.Count - 1].Rank, "Boundary-sized tender rank semantics changed.");
        }

        private static void OrdinaryTenderEvaluationSemanticsArePreserved()
        {
            var requirements = new[]
            {
                new TenderRequirement("A", "Item A", "m2", 2m),
                new TenderRequirement("B", "Item B", "m2", 1m)
            };
            var complete = new TenderBid(
                "BID-A",
                "Alpha",
                "VND",
                new[] { new TenderQuoteLine("A", 3m), new TenderQuoteLine("B", 4m) });
            var missing = new TenderBid(
                "BID-B",
                "Beta",
                "VND",
                new[] { new TenderQuoteLine("A", 2m) });

            var result = new TenderEvaluationService().Evaluate(requirements, new[] { missing, complete });
            Equal(2, result.Count, "Ordinary tender result count changed.");
            Equal("BID-A", result[0].BidId, "Tender results must remain sorted by bid id.");
            Equal(10m, result[0].EvaluatedTotal, "Complete tender evaluated total changed.");
            Equal(1, result[0].Rank, "Complete tender rank changed.");
            Equal("BID-B", result[1].BidId, "Tender missing-item bid ordering changed.");
            Equal(4m, result[1].EvaluatedTotal, "Partial tender evaluated total changed.");
            Equal(0, result[1].Rank, "Incomplete tender bids must remain unranked.");
            Equal(1, result[1].MissingItemCodes.Count, "Missing tender item count changed.");
            Equal("B", result[1].MissingItemCodes[0], "Missing tender item identity changed.");
        }

        private static TenderQuoteLine QuoteLine(int index)
        {
            return new TenderQuoteLine(ItemCode(index), index + 1m);
        }

        private static TenderRequirement Requirement(int index)
        {
            return new TenderRequirement(ItemCode(index), "Item " + index.ToString(CultureInfo.InvariantCulture), "m2", 1m);
        }

        private static TenderBid Bid(int index)
        {
            return new TenderBid(
                "BID-" + index.ToString("D5", CultureInfo.InvariantCulture),
                "Bidder " + index.ToString(CultureInfo.InvariantCulture),
                "VND",
                Array.Empty<TenderQuoteLine>());
        }

        private static string ItemCode(int index)
        {
            return "ITEM-" + index.ToString("D5", CultureInfo.InvariantCulture);
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
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountedNeverEnumerated<T> : IReadOnlyCollection<T>
        {
            internal CountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                if (Count == 0)
                    yield break;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingQuoteLines : IEnumerable<TenderQuoteLine>
        {
            private readonly int _count;
            internal StreamingQuoteLines(int count) { _count = count; }
            internal int YieldedCount { get; private set; }
            public IEnumerator<TenderQuoteLine> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return QuoteLine(i);
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingRequirements : IEnumerable<TenderRequirement>
        {
            private readonly int _count;
            internal StreamingRequirements(int count) { _count = count; }
            internal int YieldedCount { get; private set; }
            public IEnumerator<TenderRequirement> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return Requirement(i);
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingBids : IEnumerable<TenderBid>
        {
            private readonly int _count;
            internal StreamingBids(int count) { _count = count; }
            internal int YieldedCount { get; private set; }
            public IEnumerator<TenderBid> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return Bid(i);
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class TenderCollectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TenderCollectionBoundSmoke.Run();
        }
    }
}
