using System;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationIncrementalDirtySmoke
    {
        internal static void Run()
        {
            ExplicitDirtyRecomputesOnlyTouchingPairs();
            UnchangedSnapshotWithoutDirtyMarkRemainsNoOp();
        }

        private static void ExplicitDirtyRecomputesOnlyTouchingPairs()
        {
            var controller = new CoordinationIncrementalScanController();
            var items = BuildItems();
            var initial = controller.ApplySnapshot(10d, items);
            Equal(3, initial.CandidatePairs.Count, "initial candidate count");

            controller.MarkDirty("B");
            Equal(1, controller.PendingDirtyCount, "pending dirty count before apply");

            var incremental = controller.ApplySnapshot(10d, BuildItems());

            True(incremental.Delta.IsEmpty, "explicit dirty mark must not falsify semantic snapshot delta");
            True(!incremental.RequiresFullRescan, "explicit dirty mark must stay changed-only");
            True(!incremental.IsNoOp, "explicit dirty mark must trigger pair recomputation");
            Equal(2, incremental.CandidatePairs.Count, "dirty B candidate count");
            Equal(2, incremental.InvalidatedPairKeys.Count, "dirty B invalidation count");
            Equal(0, controller.PendingDirtyCount, "pending dirty count after successful apply");
            Pair(incremental.CandidatePairs[0], "A", "B");
            Pair(incremental.CandidatePairs[1], "B", "C");
        }

        private static void UnchangedSnapshotWithoutDirtyMarkRemainsNoOp()
        {
            var controller = new CoordinationIncrementalScanController();
            controller.ApplySnapshot(10d, BuildItems());

            var unchanged = controller.ApplySnapshot(10d, BuildItems());

            True(unchanged.IsNoOp, "unchanged snapshot without dirty marks must remain a no-op");
            Equal(0, unchanged.CandidatePairs.Count, "no-op candidate count");
            Equal(0, unchanged.InvalidatedPairKeys.Count, "no-op invalidation count");
        }

        private static CoordinationSpatialItem[] BuildItems()
        {
            return new[]
            {
                Item("A", 0d, 4d),
                Item("B", 2d, 6d),
                Item("C", 4d, 8d)
            };
        }

        private static CoordinationSpatialItem Item(string id, double minX, double maxX)
        {
            return new CoordinationSpatialItem(
                id,
                "rev-1",
                new CoordinationBounds(minX, 0d, 0d, maxX, 1d, 1d));
        }

        private static void Pair(CoordinationCandidatePair actual, string left, string right)
        {
            if (!string.Equals(actual.LeftId, left, StringComparison.Ordinal) ||
                !string.Equals(actual.RightId, right, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Expected pair " + left + "/" + right + " but got " + actual.LeftId + "/" + actual.RightId + ".");
        }

        private static void Equal(int expected, int actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
        }

        private static void True(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label + ".");
        }
    }
}
