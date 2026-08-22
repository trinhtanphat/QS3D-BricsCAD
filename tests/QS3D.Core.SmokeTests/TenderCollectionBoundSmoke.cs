using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TenderCollectionBoundSmoke
    {
        private const int MaximumEntries = 10000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            KnownCountQuoteOverflowRejectsBeforeEnumeration();
            KnownCountRequirementOverflowRejectsBeforeEnumeration();
            KnownCountBidOverflowRejectsBeforeEnumeration();
            StreamingQuoteOverflowStopsAtFirstDisallowedEntry();
            StreamingRequirementOverflowStopsAtFirstDisallowedEntry();
            StreamingBidOverflowStopsAtFirstDisallowedEntry();
            ExactBoundariesRemainAccepted();
            OrdinaryTenderSemanticsRemainStable();
        }

        private static void KnownCountQuoteOverflowRejectsBeforeEnumeration()
        {
            var lines = new KnownCountCollection<TenderQuoteLine>(MaximumEntries + 1);

            Throws<InvalidOperationException>(() =>
                new TenderBid("BID", "Bidder", "VND", lines));

            Equal(false, lines.EnumerationStarted, "Known-count oversized tender quote collection must fail before enumeration.");
        }

        private static void KnownCountRequirementOverflowRejectsBeforeEnumeration()
        {
            var requirements = new KnownCountCollection<TenderRequirement>(MaximumEntries + 1);

            Throws<InvalidOperationException>(() =>
                new TenderEvaluationService().Evaluate(requirements, Array.Empty<TenderBid>()));

            Equal(false, requirements.EnumerationStarted, "Known-count oversized tender requirement collection must fail before enumeration.");
        }

        private static void KnownCountBidOverflowRejectsBeforeEnumeration()
        {
            var bids = new KnownCountCollection<TenderBid>(MaximumEntries + 1);

            Throws<InvalidOperationException>(() =>
                new TenderEvaluationService().Evaluate(Array.Empty<TenderRequirement>(), bids));

            Equal(false, bids.EnumerationStarted, "Known-count oversized tender bid collection must fail before enumeration.");
        }

        private static void StreamingQuoteOverflowStopsAtFirstDisallowedEntry()
        {
            var counter = new ProductionCounter();

            Throws<InvalidOperationException>(() =>
                new TenderBid("BID", "Bidder", "VND", QuoteSequence(MaximumEntries + 2, counter)));

            Equal(MaximumEntries + 1, counter.Produced, "Tender quote streaming bound requested an entry after the first disallowed item.");
        }

        private static void StreamingRequirementOverflowStopsAtFirstDisallowedEntry()
        {
            var counter = new ProductionCounter();

            Throws<InvalidOperationException>(() =>
                new TenderEvaluationService().Evaluate(
                    RequirementSequence(MaximumEntries + 2, counter),
                    Array.Empty<TenderBid>()));

            Equal(MaximumEntries + 1, counter.Produced, "Tender requirement streaming bound requested an entry after the first disallowed item.");
        }

        private static void StreamingBidOverflowStopsAtFirstDisallowedEntry()
        {
            var counter = new ProductionCounter();

            Throws<InvalidOperationException>(() =>
                new TenderEvaluationService().Evaluate(
                    Array.Empty<TenderRequirement>(),
                    BidSequence(MaximumEntries + 2, counter)));

            Equal(MaximumEntries + 1, counter.Produced, "Tender bid streaming bound requested an entry after the first disallowed item.");
        }

        private static void ExactBoundariesRemainAccepted()
        {
            var quoteLines = new List<TenderQuoteLine>(MaximumEntries);
            var requirements = new List<TenderRequirement>(MaximumEntries);
            for (var i = 0; i < MaximumEntries; i++)
            {
                quoteLines.Add(new TenderQuoteLine(ItemCode(i), 1m));
                requirements.Add(new TenderRequirement(ItemCode(i), "Item " + i, "ea", 1m));
            }

            var boundaryBid = new TenderBid("BOUNDARY", "Boundary bidder", "VND", quoteLines);
            Equal(MaximumEntries, boundaryBid.Lines.Count, "Tender quote exact boundary changed.");

            var noBidResult = new TenderEvaluationService().Evaluate(requirements, Array.Empty<TenderBid>());
            Equal(0, noBidResult.Count, "Tender requirement exact boundary changed empty-bid behavior.");

            var bids = new List<TenderBid>(MaximumEntries);
            for (var i = 0; i < MaximumEntries; i++)
                bids.Add(new TenderBid(BidId(i), "Bidder " + i, "VND", Array.Empty<TenderQuoteLine>()));

            var bidResults = new TenderEvaluationService().Evaluate(Array.Empty<TenderRequirement>(), bids);
            Equal(MaximumEntries, bidResults.Count, "Tender bid exact boundary changed.");
            Equal("B00000", bidResults[0].BidId, "Tender bid boundary ordering changed.");
            Equal(1, bidResults[0].Rank, "Tender bid boundary rank changed.");
            Equal("B09999", bidResults[MaximumEntries - 1].BidId, "Tender bid boundary terminal ordering changed.");
            Equal(MaximumEntries, bidResults[MaximumEntries - 1].Rank, "Tender bid boundary terminal rank changed.");
        }

        private static void OrdinaryTenderSemanticsRemainStable()
        {
            Throws<ArgumentException>(() =>
                new TenderBid(
                    "DUP-LINE",
                    "Bidder",
                    "VND",
                    new[]
                    {
                        new TenderQuoteLine("A", 1m),
                        new TenderQuoteLine("a", 2m)
                    }));

            Throws<ArgumentException>(() =>
                new TenderEvaluationService().Evaluate(
                    new[]
                    {
                        new TenderRequirement("A", "First", "ea", 1m),
                        new TenderRequirement("a", "Duplicate", "ea", 1m)
                    },
                    Array.Empty<TenderBid>()));

            var results = new TenderEvaluationService().Evaluate(
                new[]
                {
                    new TenderRequirement("A", "First", "ea", 2m),
                    new TenderRequirement("B", "Second", "ea", 1m)
                },
                new[]
                {
                    new TenderBid(
                        "BID-B",
                        "Higher bidder",
                        "VND",
                        new[]
                        {
                            new TenderQuoteLine("A", 6m),
                            new TenderQuoteLine("B", 4m)
                        }),
                    new TenderBid(
                        "BID-A",
                        "Lower bidder",
                        "VND",
                        new[]
                        {
                            new TenderQuoteLine("A", 5m),
                            new TenderQuoteLine("B", 4m)
                        }),
                    new TenderBid(
                        "BID-C",
                        "Incomplete bidder",
                        "VND",
                        new[] { new TenderQuoteLine("A", 1m) })
                });

            Equal(3, results.Count, "Ordinary tender result count changed.");
            Equal("BID-A", results[0].BidId, "Tender deterministic result ordering changed.");
            Equal(14m, results[0].EvaluatedTotal, "Lower complete tender total changed.");
            Equal(1, results[0].Rank, "Lower complete tender rank changed.");
            Equal("BID-B", results[1].BidId, "Tender deterministic result ordering changed.");
            Equal(16m, results[1].EvaluatedTotal, "Higher complete tender total changed.");
            Equal(2, results[1].Rank, "Higher complete tender rank changed.");
            Equal("BID-C", results[2].BidId, "Tender deterministic result ordering changed.");
            Equal(2m, results[2].EvaluatedTotal, "Incomplete tender partial total changed.");
            Equal(0, results[2].Rank, "Incomplete tender rank changed.");
            Equal(1, results[2].MissingItemCodes.Count, "Incomplete tender missing-item count changed.");
            Equal("B", results[2].MissingItemCodes[0], "Incomplete tender missing-item identity changed.");
        }

        private static IEnumerable<TenderQuoteLine> QuoteSequence(int count, ProductionCounter counter)
        {
            for (var i = 0; i < count; i++)
            {
                counter.Produced++;
                yield return new TenderQuoteLine(ItemCode(i), 1m);
            }
        }

        private static IEnumerable<TenderRequirement> RequirementSequence(int count, ProductionCounter counter)
        {
            for (var i = 0; i < count; i++)
            {
                counter.Produced++;
                yield return new TenderRequirement(ItemCode(i), "Item " + i, "ea", 1m);
            }
        }

        private static IEnumerable<TenderBid> BidSequence(int count, ProductionCounter counter)
        {
            for (var i = 0; i < count; i++)
            {
                counter.Produced++;
                yield return new TenderBid(BidId(i), "Bidder " + i, "VND", Array.Empty<TenderQuoteLine>());
            }
        }

        private static string ItemCode(int index) => "I" + index.ToString("D5");
        private static string BidId(int index) => "B" + index.ToString("D5");

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

        private sealed class ProductionCounter
        {
            internal int Produced { get; set; }
        }

        private sealed class KnownCountCollection<T> : ICollection<T>
        {
            private readonly int _count;

            internal KnownCountCollection(int count)
            {
                _count = count;
            }

            internal bool EnumerationStarted { get; private set; }
            public int Count => _count;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationStarted = true;
                throw new InvalidOperationException("Enumeration should not start for a rejected known-count collection.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
