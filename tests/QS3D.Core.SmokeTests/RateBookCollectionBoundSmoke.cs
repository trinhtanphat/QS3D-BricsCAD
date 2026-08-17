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
            AnyOversizedKnownCountFailsBeforeEnumeration();
            ConflictingInBoundCountsFailBeforeEnumeration();
            ConsistentMultiContractCountsRemainAccepted();
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

        private static void AnyOversizedKnownCountFailsBeforeEnumeration()
        {
            var source = new MultiContractRateItems(1, MaximumItems + 1, 1, Item(0));
            var error = Capture<InvalidOperationException>(() => new RateBook("BOOK-MULTI-OVERSIZE", source));
            Equal(0, source.GetEnumeratorCalls, "Any oversized supported Count contract must reject before enumeration.");
            Contains("at most 10000", error.Message, "Multi-contract oversize failure must use the RateBook capacity contract.");
        }

        private static void ConflictingInBoundCountsFailBeforeEnumeration()
        {
            var source = new MultiContractRateItems(1, 2, 1, Item(0));
            var error = Capture<InvalidOperationException>(() => new RateBook("BOOK-MULTI-CONFLICT", source));
            Equal(0, source.GetEnumeratorCalls, "Conflicting in-bound Count contracts must reject before enumeration.");
            Contains("conflicting known item counts", error.Message, "Conflicting Count contracts must have a deterministic diagnostic.");
        }

        private static void ConsistentMultiContractCountsRemainAccepted()
        {
            var source = new MultiContractRateItems(1, 1, 1, Item(0));
            var book = new RateBook("BOOK-MULTI-CONSISTENT", source);
            Equal(1, source.GetEnumeratorCalls, "Consistent Count contracts must enumerate exactly once.");
            Equal(1, book.Items.Count, "Consistent Count contracts must preserve ordinary RateBook ingestion.");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedItem()
        {
            var source = new StreamingRateItems(MaximumItems + 2);
            var error = Capture<InvalidOperationException>(() => new RateBook("BOOK-STREAMING-OVERSIZE", source));
            Equal(MaximumItems + 1, source.YieldedCount, "Streaming RateBook ingestion must stop immediately after observing item 10,001.");
            Contains("at most 10000", error.Message, "Streaming oversize failure must report the RateBook item bound.");
        }

        private static void ExactBoundaryRemainsAccepted()
        {
            var items = new RateItem[MaximumItems];
            for (var i = 0; i < items.Length; i++) items[i] = Item(i);
            var book = new RateBook("BOOK-BOUNDARY", items);
            Equal(MaximumItems, book.Items.Count, "RateBook must accept exactly 10,000 valid items.");
            var resolved = book.Resolve(new CostCode("CONC"), "m3", "VND", StartUtc.AddTicks(MaximumItems));
            True(resolved.IsMatched && resolved.Item != null, "Boundary-sized RateBook must remain resolvable.");
            Equal("RATE-09999", resolved.Item!.RateItemId, "Boundary-sized RateBook latest-item resolution changed.");
        }

        private static RateItem Item(int index) => new RateItem("RATE-" + index.ToString("D5", CultureInfo.InvariantCulture), new CostCode("CONC"), "m3", "VND", index + 1m, StartUtc.AddTicks(index), "v1");
        private static TException Capture<TException>(Action action) where TException : Exception { try { action(); } catch (TException ex) { return ex; } throw new InvalidOperationException("Expected exception " + typeof(TException).Name + "."); }
        private static void Contains(string expected, string actual, string message) { if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0) throw new InvalidOperationException(message + " Actual: " + actual); }
        private static void Equal<T>(T expected, T actual, string message) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + "."); }
        private static void True(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        private sealed class CountedNeverEnumerated : IReadOnlyCollection<RateItem>
        {
            internal CountedNeverEnumerated(int count) { Count = count; }
            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }
            public IEnumerator<RateItem> GetEnumerator() { GetEnumeratorCalls++; throw new InvalidOperationException("Oversized counted source must not be enumerated."); }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class MultiContractRateItems : ICollection<RateItem>, IReadOnlyCollection<RateItem>, ICollection
        {
            private readonly RateItem[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            internal MultiContractRateItems(int genericCount, int readOnlyCount, int nonGenericCount, params RateItem[] items) { _genericCount = genericCount; _readOnlyCount = readOnlyCount; _nonGenericCount = nonGenericCount; _items = items; }
            int ICollection<RateItem>.Count => _genericCount;
            int IReadOnlyCollection<RateItem>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int GetEnumeratorCalls { get; private set; }
            public IEnumerator<RateItem> GetEnumerator() { GetEnumeratorCalls++; return ((IEnumerable<RateItem>)_items).GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
            public bool Contains(RateItem item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(RateItem[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(RateItem item) => throw new NotSupportedException();
            public bool Remove(RateItem item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        private sealed class StreamingRateItems : IEnumerable<RateItem>
        {
            private readonly int _count;
            internal StreamingRateItems(int count) { _count = count; }
            internal int YieldedCount { get; private set; }
            public IEnumerator<RateItem> GetEnumerator() { for (var i = 0; i < _count; i++) { YieldedCount++; yield return Item(i); } }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class RateBookCollectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() { RateBookCollectionBoundSmoke.Run(); }
    }
}
