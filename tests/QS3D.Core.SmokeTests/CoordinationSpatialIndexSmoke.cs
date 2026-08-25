using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationSpatialIndexSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PairEnumerationIsDeterministic();
            ChangedOnlyMatchesImpactedFullPairs();
            SnapshotDiffTracksLifecycleChanges();
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

        private static void InvalidInputsFailClosed()
        {
            Throws<ArgumentException>(() => new CoordinationBounds(1, 0, 0, 0, 1, 1));
            Throws<ArgumentOutOfRangeException>(() => new CoordinationSpatialIndex(0, new CoordinationSpatialItem[0]));
            Throws<ArgumentException>(() => new CoordinationSpatialIndex(1, new[] { Item("A", "1", 0, 1), Item("a", "2", 2, 3) }));

            var index = new CoordinationSpatialIndex(1, new[] { Item("A", "1", 0, 1) });
            Throws<System.Collections.Generic.KeyNotFoundException>(() => index.QueryChangedPairs(new[] { "MISSING" }));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("CoordinationSpatialIndexSmoke: expected " + typeof(T).Name + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("CoordinationSpatialIndexSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
