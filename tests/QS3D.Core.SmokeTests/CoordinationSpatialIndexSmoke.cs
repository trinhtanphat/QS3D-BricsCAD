using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationSpatialIndexSmoke
    {
        private const int MaximumEntries = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            PairEnumerationIsDeterministic();
            ChangedOnlyMatchesImpactedFullPairs();
            SnapshotDiffTracksLifecycleChanges();
            SnapshotDiffTracksCaseOnlyIdentityDrift();
            ItemEnumerationIsBounded();
            ChangedItemEnumerationIsBounded();
            InvalidInputsFailClosed();
        }

        private static CoordinationSpatialItem Item(string id, string revision, double minX, double maxX)
        {
            return new CoordinationSpatialItem(id, revision, new CoordinationBounds(minX, 0, 0, maxX, 1, 1));
        }

        private static void PairEnumerationIsDeterministic()
        {
            var first = new CoordinationSpatialIndex(2d, new[]
            {
                Item("C", "1", 10, 11), Item("B", "1", 1, 3), Item("A", "1", 0, 2)
            });
            var second = new CoordinationSpatialIndex(2d, new[]
            {
                Item("A", "1", 0, 2), Item("C", "1", 10, 11), Item("B", "1", 1, 3)
            });

            Equal("A\u001fB", string.Join("|", first.QueryAllPairs().Select(pair => pair.PairKey)), "unexpected full candidate pairs");
            Equal(
                string.Join("|", first.QueryAllPairs().Select(pair => pair.PairKey)),
                string.Join("|", second.QueryAllPairs().Select(pair => pair.PairKey)),
                "candidate ordering changed with input order");
        }

        private static void ChangedOnlyMatchesImpactedFullPairs()
        {
            var index = new CoordinationSpatialIndex(2d, new[]
            {
                Item("A", "1", 0, 2), Item("B", "1", 1, 3), Item("C", "1", 2.5, 4), Item("D", "1", 10, 11)
            });

            var expected = string.Join("|", index.QueryAllPairs()
                .Where(pair => pair.LeftId == "B" || pair.RightId == "B")
                .Select(pair => pair.PairKey));
            var actual = string.Join("|", index.QueryChangedPairs(new[] { "B" }).Select(pair => pair.PairKey));
            Equal(expected, actual, "changed-only query diverged from impacted subset of full scan");
        }

        private static void SnapshotDiffTracksLifecycleChanges()
        {
            var before = new CoordinationSpatialIndex(2d, new[]
            {
                Item("A", "1", 0, 2), Item("B", "1", 4, 5), Item("REMOVED", "1", 8, 9)
            });
            var same = new CoordinationSpatialIndex(2d, new[]
            {
                Item("B", "1", 4, 5), Item("REMOVED", "1", 8, 9), Item("A", "1", 0, 2)
            });
            if (!same.Diff(before).IsEmpty)
                throw new InvalidOperationException("CoordinationSpatialIndexSmoke: no-op snapshot diff was not empty.");

            var after = new CoordinationSpatialIndex(2d, new[]
            {
                Item("A", "2", 0, 2), Item("B", "1", 4, 6), Item("ADDED", "1", 12, 13)
            });
            var delta = after.Diff(before);
            Equal("A|ADDED|B", string.Join("|", delta.ChangedOrAddedIds), "changed/add set was incorrect");
            Equal("REMOVED", string.Join("|", delta.RemovedIds), "removed set was incorrect");
            Equal("A|ADDED|B|REMOVED", string.Join("|", delta.AllDirtyIds), "dirty invalidation set was incorrect");
        }

        private static void SnapshotDiffTracksCaseOnlyIdentityDrift()
        {
            var before = new CoordinationSpatialIndex(2d, new[] { Item("A", "1", 0, 2) });
            var after = new CoordinationSpatialIndex(2d, new[] { Item("a", "1", 0, 2) });
            var delta = after.Diff(before);

            Equal("a", string.Join("|", delta.ChangedOrAddedIds), "case-only ItemId drift was not detected");
            Equal(string.Empty, string.Join("|", delta.RemovedIds), "case-only ItemId drift was misclassified as removal");
        }

        private static void ItemEnumerationIsBounded()
        {
            CountedOversizeFailsBeforeEnumeration();
            StreamingOversizeStopsAtFirstDisallowedEntry();
            ExactBoundaryIsAccepted();
        }

        private static void CountedOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<CoordinationSpatialItem>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() => new CoordinationSpatialIndex(1d, source));

            Equal(0, source.GetEnumeratorCalls, "oversized counted spatial items must fail before enumeration");
            Contains("at most 10000", error.Message, "counted spatial-item oversize must report the coordination bound");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedEntry()
        {
            var source = new StreamingItems(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() => new CoordinationSpatialIndex(1d, source));

            Equal(MaximumEntries + 1, source.YieldedCount,
                "streaming spatial-item ingestion must stop after observing item 10,001");
            Contains("at most 10000", error.Message, "streaming spatial-item oversize must report the coordination bound");
        }

        private static void ExactBoundaryIsAccepted()
        {
            var items = new CoordinationSpatialItem[MaximumEntries];
            for (var i = 0; i < items.Length; i++)
            {
                var coordinate = i * 2d;
                items[i] = Item(
                    "BOUND-" + i.ToString("D5", CultureInfo.InvariantCulture),
                    "1",
                    coordinate,
                    coordinate);
            }

            var index = new CoordinationSpatialIndex(1d, items);
            Equal(MaximumEntries, index.Items.Count, "spatial index must accept exactly 10,000 items");
        }

        private static void ChangedItemEnumerationIsBounded()
        {
            CountedChangedItemOversizeFailsBeforeEnumeration();
            StreamingDuplicateChangedItemsStopAtFirstDisallowedEntry();
            ExactChangedItemBoundaryIsAccepted();
        }

        private static void CountedChangedItemOversizeFailsBeforeEnumeration()
        {
            var index = new CoordinationSpatialIndex(1d, new[] { Item("A", "1", 0, 1) });
            var source = new CountedNeverEnumerated<string>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() => index.QueryChangedPairs(source));

            Equal(0, source.GetEnumeratorCalls, "oversized counted changed-item IDs must fail before enumeration");
            Contains("at most 10000", error.Message, "counted changed-item oversize must report the coordination bound");
        }

        private static void StreamingDuplicateChangedItemsStopAtFirstDisallowedEntry()
        {
            var index = new CoordinationSpatialIndex(1d, new[] { Item("A", "1", 0, 1) });
            var source = new StreamingChangedIds(MaximumEntries + 2, " A ");
            var error = Capture<InvalidOperationException>(() => index.QueryChangedPairs(source));

            Equal(MaximumEntries + 1, source.YieldedCount,
                "streaming duplicate changed-item IDs must stop after observing entry 10,001");
            Contains("at most 10000", error.Message, "streaming changed-item oversize must report the coordination bound");
        }

        private static void ExactChangedItemBoundaryIsAccepted()
        {
            var index = new CoordinationSpatialIndex(1d, new[]
            {
                Item("A", "1", 0, 1), Item("B", "1", 0.5, 1.5)
            });
            var source = Enumerable.Repeat(" a ", MaximumEntries).ToArray();
            var pairs = index.QueryChangedPairs(source);

            Equal("A\u001fB", string.Join("|", pairs.Select(pair => pair.PairKey)),
                "exactly 10,000 changed-item observations must remain accepted with case-insensitive deduplication");
        }

        private static void InvalidInputsFailClosed()
        {
            Throws<ArgumentException>(() => new CoordinationBounds(1, 0, 0, 0, 1, 1));
            Throws<ArgumentOutOfRangeException>(() => new CoordinationSpatialIndex(0, new CoordinationSpatialItem[0]));
            Throws<ArgumentException>(() => new CoordinationSpatialIndex(1, new[] { Item("A", "1", 0, 1), Item("a", "2", 2, 3) }));

            var index = new CoordinationSpatialIndex(1, new[] { Item("A", "1", 0, 1) });
            Throws<KeyNotFoundException>(() => index.QueryChangedPairs(new[] { "MISSING" }));
            Throws<ArgumentException>(() => index.QueryChangedPairs(new[] { "   " }));
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("CoordinationSpatialIndexSmoke: expected " + typeof(TException).Name + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("CoordinationSpatialIndexSmoke: expected " + typeof(T).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("CoordinationSpatialIndexSmoke: " + message + ". Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    "CoordinationSpatialIndexSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
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
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingItems : IEnumerable<CoordinationSpatialItem>
        {
            private readonly int _count;

            internal StreamingItems(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<CoordinationSpatialItem> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    var coordinate = i * 2d;
                    yield return Item(
                        "STREAM-" + i.ToString("D5", CultureInfo.InvariantCulture),
                        "1",
                        coordinate,
                        coordinate);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingChangedIds : IEnumerable<string>
        {
            private readonly int _count;
            private readonly string _value;

            internal StreamingChangedIds(int count, string value)
            {
                _count = count;
                _value = value;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return _value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
