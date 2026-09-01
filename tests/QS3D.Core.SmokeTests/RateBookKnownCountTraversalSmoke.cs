using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateBookKnownCountTraversalSmoke
    {
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            OverYieldFailsAtFirstUnexpectedItem();
            UnderYieldFailsAfterTraversal();
            ExactKnownCountTraversalRemainsAccepted();
            CountDriftAfterExactTraversalFailsClosed();
            CountDriftFromCurrentFailsBeforeNullAcceptance();
            PostTraversalInterfaceConflictFailsClosed();
            HonestMultiInterfaceCountRemainsAccepted();
            PureStreamingInputRemainsAccepted();
        }

        private static void OverYieldFailsAtFirstUnexpectedItem()
        {
            var source = new MisreportedReadOnlyCollection(reportedCount: 1, actualCount: 3);
            var error = Capture<InvalidOperationException>(
                () => new RateBook("BOOK-KNOWN-COUNT-OVER-YIELD", source));

            Equal(
                2,
                source.YieldedCount,
                "RateBook must stop when traversal first exceeds the accepted known Count.");
            Contains(
                "traversal count does not match",
                error.Message,
                "Over-yielding counted input must report a known-count/traversal mismatch.");
        }

        private static void UnderYieldFailsAfterTraversal()
        {
            var source = new MisreportedReadOnlyCollection(reportedCount: 3, actualCount: 1);
            var error = Capture<InvalidOperationException>(
                () => new RateBook("BOOK-KNOWN-COUNT-UNDER-YIELD", source));

            Equal(
                1,
                source.YieldedCount,
                "Under-yielding counted input should be consumed only to its natural end before mismatch detection.");
            Contains(
                "traversal count does not match",
                error.Message,
                "Under-yielding counted input must report a known-count/traversal mismatch.");
        }

        private static void ExactKnownCountTraversalRemainsAccepted()
        {
            var source = new MisreportedReadOnlyCollection(reportedCount: 2, actualCount: 2);
            var book = new RateBook("BOOK-KNOWN-COUNT-EXACT", source);

            Equal(2, source.YieldedCount, "Exact counted input must enumerate every declared item once.");
            Equal(2, book.Items.Count, "Exact known Count/traversal agreement must remain accepted.");
        }

        private static void CountDriftAfterExactTraversalFailsClosed()
        {
            var source = new CountDriftingReadOnlyCollection(initialCount: 2, finalCount: 3);
            var error = Capture<InvalidOperationException>(
                () => new RateBook("BOOK-KNOWN-COUNT-DRIFT", source));

            Equal(2, source.YieldedCount, "Count drift probe must yield exactly the admitted item cardinality.");
            Equal(3, source.Count, "Probe must expose its changed deterministic Count after traversal.");
            Contains(
                "known count changed during traversal",
                error.Message,
                "RateBook must reject Count metadata that changes after exact traversal.");
        }

        private static void CountDriftFromCurrentFailsBeforeNullAcceptance()
        {
            var source = new CurrentDriftingReadOnlyCollection(Item(0));
            var error = Capture<InvalidOperationException>(
                () => new RateBook("BOOK-KNOWN-COUNT-CURRENT-DRIFT", source));

            Equal(1, source.MoveNextCalls, "Current-induced Count drift must advance only the admitted item.");
            Equal(1, source.CurrentReads, "Current-induced Count drift must observe Current exactly once before failing.");
            Contains(
                "known count changed during traversal",
                error.Message,
                "RateBook Current-induced Count drift must win before ordinary returned-item validation.");
        }

        private static void PostTraversalInterfaceConflictFailsClosed()
        {
            var source = new MultiInterfaceCollection(count: 2, finalNonGenericCount: 3);
            var error = Capture<InvalidOperationException>(
                () => new RateBook("BOOK-KNOWN-COUNT-CONFLICT-AFTER", source));

            Equal(2, source.YieldedCount, "Interface-conflict probe must yield exactly the admitted cardinality.");
            Contains(
                "conflicting known counts",
                error.Message,
                "RateBook must re-read all deterministic Count interfaces after traversal.");
        }

        private static void HonestMultiInterfaceCountRemainsAccepted()
        {
            var source = new MultiInterfaceCollection(count: 2, finalNonGenericCount: 2);
            var book = new RateBook("BOOK-KNOWN-COUNT-MULTI-HONEST", source);

            Equal(2, source.YieldedCount, "Honest multi-interface input must enumerate each item once.");
            Equal(2, book.Items.Count, "Stable matching Count interfaces must remain accepted.");
        }

        private static void PureStreamingInputRemainsAccepted()
        {
            var book = new RateBook("BOOK-KNOWN-COUNT-STREAM", StreamItems(2));
            Equal(2, book.Items.Count, "Pure streaming input without deterministic Count metadata must remain accepted.");
        }

        private static IEnumerable<RateItem> StreamItems(int count)
        {
            for (var i = 0; i < count; i++)
                yield return Item(i);
        }

        private static RateItem Item(int index)
        {
            return new RateItem(
                "RATE-COUNT-" + index.ToString("D4", CultureInfo.InvariantCulture),
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

        private sealed class MisreportedReadOnlyCollection : IReadOnlyCollection<RateItem>
        {
            private readonly int _actualCount;

            internal MisreportedReadOnlyCollection(int reportedCount, int actualCount)
            {
                Count = reportedCount;
                _actualCount = actualCount;
            }

            public int Count { get; }
            internal int YieldedCount { get; private set; }

            public IEnumerator<RateItem> GetEnumerator()
            {
                for (var i = 0; i < _actualCount; i++)
                {
                    YieldedCount++;
                    yield return Item(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class CountDriftingReadOnlyCollection : IReadOnlyCollection<RateItem>
        {
            private readonly int _initialCount;
            private readonly int _finalCount;

            internal CountDriftingReadOnlyCollection(int initialCount, int finalCount)
            {
                _initialCount = initialCount;
                _finalCount = finalCount;
                Count = initialCount;
            }

            public int Count { get; private set; }
            internal int YieldedCount { get; private set; }

            public IEnumerator<RateItem> GetEnumerator()
            {
                try
                {
                    for (var i = 0; i < _initialCount; i++)
                    {
                        YieldedCount++;
                        yield return Item(i);
                    }
                }
                finally
                {
                    Count = _finalCount;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class CurrentDriftingReadOnlyCollection : IReadOnlyCollection<RateItem>, IEnumerator<RateItem>
        {
            private readonly RateItem _item;
            private bool _advanced;
            private bool _currentObserved;

            internal CurrentDriftingReadOnlyCollection(RateItem item)
            {
                _item = item ?? throw new ArgumentNullException(nameof(item));
            }

            public int Count => _currentObserved ? 2 : 1;
            public RateItem Current
            {
                get
                {
                    CurrentReads++;
                    _currentObserved = true;
                    return null!;
                }
            }
            object IEnumerator.Current => Current;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<RateItem> GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool MoveNext()
            {
                MoveNextCalls++;
                if (_advanced) return false;
                _advanced = true;
                return true;
            }

            public void Reset() => throw new NotSupportedException();
            public void Dispose() { }
        }

        private sealed class MultiInterfaceCollection : ICollection<RateItem>, IReadOnlyCollection<RateItem>, ICollection
        {
            private readonly List<RateItem> _items;
            private readonly int _finalNonGenericCount;
            private int _nonGenericCount;

            internal MultiInterfaceCollection(int count, int finalNonGenericCount)
            {
                _items = new List<RateItem>(count);
                for (var i = 0; i < count; i++)
                    _items.Add(Item(i));
                _nonGenericCount = count;
                _finalNonGenericCount = finalNonGenericCount;
            }

            public int Count => _items.Count;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int YieldedCount { get; private set; }

            public IEnumerator<RateItem> GetEnumerator()
            {
                try
                {
                    for (var i = 0; i < _items.Count; i++)
                    {
                        YieldedCount++;
                        yield return _items[i];
                    }
                }
                finally
                {
                    _nonGenericCount = _finalNonGenericCount;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(RateItem item) => _items.Contains(item);
            public void CopyTo(RateItem[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items.ToArray()).CopyTo(array, index);
            public void Add(RateItem item) => throw new NotSupportedException();
            public bool Remove(RateItem item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }
    }

    internal static class RateBookKnownCountTraversalRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RateBookKnownCountTraversalSmoke.Run();
        }
    }
}
