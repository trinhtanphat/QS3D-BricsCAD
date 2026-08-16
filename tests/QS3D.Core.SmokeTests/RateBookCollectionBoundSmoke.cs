using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateBookCollectionBoundSmoke
    {
        private const int MaximumItems = 10000;
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            CountedOversizeFailsBeforeEnumeration();
            StreamingOversizeStopsAtFirstDisallowedItem();
            ExactBoundaryRemainsAccepted();
        }

        private static void CountedOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated(MaximumItems + 1);
            var error = Capture<InvalidOperationException>(() => new RateBook("BOOK-COUNTED-OVERSIZE", source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted RateBook input must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted oversize failure must report the RateBook item bound.");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedItem()
        {
            var source = new StreamingRateItems(MaximumItems + 2);
            var error = Capture<InvalidOperationException>(() => new RateBook("BOOK-STREAMING-OVERSIZE", source));

            Equal(
                MaximumItems + 1,
                source.YieldedCount,
                "Streaming RateBook ingestion must stop immediately after observing item 10,001.");
            Contains("at most 10000", error.Message, "Streaming oversize failure must report the RateBook item bound.");
        }

        private static void ExactBoundaryRemainsAccepted()
        {
            var items = new RateItem[MaximumItems];
            for (var i = 0; i < items.Length; i++)
                items[i] = Item(i);

            var book = new RateBook("BOOK-BOUNDARY", items);
            Equal(MaximumItems, book.Items.Count, "RateBook must accept exactly 10,000 valid items.");

            var resolved = book.Resolve(new CostCode("CONC"), "m3", "VND", StartUtc.AddTicks(MaximumItems));
            True(resolved.IsMatched && resolved.Item != null, "Boundary-sized RateBook must remain resolvable.");
            Equal(
                "RATE-09999",
                resolved.Item!.RateItemId,
                "Boundary-sized RateBook latest-item resolution changed.");
        }

        private static RateItem Item(int index)
        {
            return new RateItem(
                "RATE-" + index.ToString("D5", CultureInfo.InvariantCulture),
                new CostCode("CONC"),
                "m3",
                "VND",
                index + 1m,
                StartUtc.AddTicks(index),
                "v1");
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

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class CountedNeverEnumerated : IReadOnlyCollection<RateItem>
        {
            internal CountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<RateItem> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingRateItems : IEnumerable<RateItem>
        {
            private readonly int _count;

            internal StreamingRateItems(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<RateItem> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return Item(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class RateBookCollectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RateBookCollectionBoundSmoke.Run();
        }
    }
}
