using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationSpatialScaleBenchmarkSmoke
    {
        private const int PairCount = 512;
        private const int ItemCount = PairCount * 2;
        private const int MaxChangedCandidateCount = 1;
        private const int MinimumCandidateReductionFactor = 256;

        [ModuleInitializer]
        internal static void Initialize()
        {
            ChangedOnlyCandidateWorkIsBoundedAtScale();
            IncrementalControllerPreservesNoOpAndChangedOnlyBoundsAtScale();
        }

        private static void ChangedOnlyCandidateWorkIsBoundedAtScale()
        {
            var items = BuildItems("1");
            var index = new CoordinationSpatialIndex(2d, items);
            var full = index.QueryAllPairs();
            Equal(PairCount, full.Count, "full broad-phase pair count");

            var dirtyId = Id(PairCount / 2, "B");
            var changed = index.QueryChangedPairs(new[] { dirtyId });
            var expected = full
                .Where(pair => string.Equals(pair.LeftId, dirtyId, StringComparison.Ordinal) ||
                               string.Equals(pair.RightId, dirtyId, StringComparison.Ordinal))
                .Select(pair => pair.PairKey)
                .ToArray();

            Equal(MaxChangedCandidateCount, changed.Count, "changed-only candidate count");
            Equal(
                string.Join("|", expected),
                string.Join("|", changed.Select(pair => pair.PairKey)),
                "changed-only result diverged from impacted full-scan subset");

            if (full.Count < changed.Count * MinimumCandidateReductionFactor)
                throw new InvalidOperationException(
                    "CoordinationSpatialScaleBenchmarkSmoke: changed-only candidate work did not meet the deterministic " +
                    MinimumCandidateReductionFactor + "x reduction threshold. Full=" + full.Count +
                    ", changed=" + changed.Count + ".");
        }

        private static void IncrementalControllerPreservesNoOpAndChangedOnlyBoundsAtScale()
        {
            var controller = new CoordinationIncrementalScanController();
            var initial = controller.ApplySnapshot(2d, BuildItems("1"));
            Equal(ItemCount, initial.SnapshotItemCount, "initial snapshot item count");
            Equal(PairCount, initial.CandidatePairs.Count, "initial controller pair count");

            var noOp = controller.ApplySnapshot(2d, BuildItems("1").AsEnumerable().Reverse());
            True(noOp.IsNoOp, "same logical scale snapshot produced recomputation churn");
            Equal(0, noOp.CandidatePairs.Count, "no-op candidate count");
            Equal(0, noOp.InvalidatedPairKeys.Count, "no-op invalidation count");

            var changedIndex = PairCount / 2;
            var changedId = Id(changedIndex, "B");
            var revised = BuildItems("1");
            var changedItemIndex = changedIndex * 2 + 1;
            var old = revised[changedItemIndex];
            revised[changedItemIndex] = new CoordinationSpatialItem(old.ItemId, "2", old.Bounds);

            var incremental = controller.ApplySnapshot(2d, revised);
            Equal(changedId, string.Join("|", incremental.Delta.ChangedOrAddedIds), "changed semantic revision id");
            True(!incremental.RequiresFullRescan, "single semantic revision forced full rescan");
            Equal(MaxChangedCandidateCount, incremental.CandidatePairs.Count, "incremental candidate count");
            Equal(MaxChangedCandidateCount, incremental.InvalidatedPairKeys.Count, "incremental invalidation count");

            if (initial.CandidatePairs.Count < incremental.CandidatePairs.Count * MinimumCandidateReductionFactor)
                throw new InvalidOperationException(
                    "CoordinationSpatialScaleBenchmarkSmoke: incremental controller did not meet the deterministic " +
                    MinimumCandidateReductionFactor + "x candidate-work reduction threshold. Initial=" +
                    initial.CandidatePairs.Count + ", incremental=" + incremental.CandidatePairs.Count + ".");
        }

        private static CoordinationSpatialItem[] BuildItems(string revision)
        {
            var items = new List<CoordinationSpatialItem>(ItemCount);
            for (var i = 0; i < PairCount; i++)
            {
                var x = i * 10d;
                items.Add(Item(Id(i, "A"), revision, x, x + 2d));
                items.Add(Item(Id(i, "B"), revision, x + 1d, x + 3d));
            }
            return items.ToArray();
        }

        private static CoordinationSpatialItem Item(string id, string revision, double minX, double maxX)
        {
            return new CoordinationSpatialItem(
                id,
                revision,
                new CoordinationBounds(minX, 0d, 0d, maxX, 1d, 1d));
        }

        private static string Id(int pairIndex, string side)
        {
            return "P" + pairIndex.ToString("D4") + side;
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("CoordinationSpatialScaleBenchmarkSmoke: " + message + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    "CoordinationSpatialScaleBenchmarkSmoke: " + message + ". Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "CoordinationSpatialScaleBenchmarkSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
