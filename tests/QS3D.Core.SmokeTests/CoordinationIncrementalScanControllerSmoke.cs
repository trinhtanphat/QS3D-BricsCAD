using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationIncrementalScanControllerSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            InitialAndNoOpAreDeterministic();
            ChangedMoveInvalidatesOldAndQueuesNewPair();
            SameBoundsRevisionChangeInvalidatesAndRequeuesPair();
            RemovalInvalidatesWithoutRequeue();
            CaseOnlyIdentityDriftInvalidatesOldAndQueuesCurrentPair();
            DuplicateDirtyNotificationsCoalesce();
            CellSizeChangeForcesFullRescan();
            UnknownDirtyFailsClosedWithoutLosingState();
        }

        private static CoordinationSpatialItem Item(string id, string revision, double minX, double maxX)
        {
            return new CoordinationSpatialItem(id, revision, new CoordinationBounds(minX, 0, 0, maxX, 1, 1));
        }

        private static void InitialAndNoOpAreDeterministic()
        {
            var controller = new CoordinationIncrementalScanController();
            var initial = controller.ApplySnapshot(2d, new[]
            {
                Item("B", "1", 1, 3), Item("A", "1", 0, 2), Item("C", "1", 10, 11)
            });
            True(initial.IsInitial, "initial snapshot flag missing");
            True(initial.RequiresFullRescan, "initial snapshot must require full rescan");
            Equal("A\u001fB", JoinPairs(initial), "initial pair set incorrect");
            Equal("A|B|C", string.Join("|", initial.Delta.ChangedOrAddedIds), "initial dirty ids incorrect");

            var noOp = controller.ApplySnapshot(2d, new[]
            {
                Item("C", "1", 10, 11), Item("A", "1", 0, 2), Item("B", "1", 1, 3)
            });
            True(noOp.IsNoOp, "same logical snapshot produced churn");
        }

        private static void ChangedMoveInvalidatesOldAndQueuesNewPair()
        {
            var controller = new CoordinationIncrementalScanController();
            controller.ApplySnapshot(2d, new[]
            {
                Item("A", "1", 0, 2), Item("B", "1", 1, 3), Item("C", "1", 5, 7)
            });

            var changed = controller.ApplySnapshot(2d, new[]
            {
                Item("A", "1", 0, 2), Item("B", "2", 5.5, 6.5), Item("C", "1", 5, 7)
            });
            Equal("B", string.Join("|", changed.Delta.ChangedOrAddedIds), "changed element not detected");
            Equal("A\u001fB", string.Join("|", changed.InvalidatedPairKeys), "old impacted pair not invalidated");
            Equal("B\u001fC", JoinPairs(changed), "new impacted pair not queued");
        }

        private static void SameBoundsRevisionChangeInvalidatesAndRequeuesPair()
        {
            var controller = new CoordinationIncrementalScanController();
            controller.ApplySnapshot(2d, new[]
            {
                Item("A", "LIVE:surface=24;volume=8", 0, 2),
                Item("B", "LIVE:surface=24;volume=8", 1, 3)
            });

            var changed = controller.ApplySnapshot(2d, new[]
            {
                Item("A", "LIVE:surface=30;volume=7", 0, 2),
                Item("B", "LIVE:surface=24;volume=8", 1, 3)
            });

            True(!changed.IsNoOp, "same-AABB geometry revision change was treated as a no-op");
            Equal("A", string.Join("|", changed.Delta.ChangedOrAddedIds), "revision-only geometry change was not marked dirty");
            Equal("A\u001fB", string.Join("|", changed.InvalidatedPairKeys), "revision-only geometry change did not invalidate old exact pair state");
            Equal("A\u001fB", JoinPairs(changed), "revision-only geometry change did not queue current narrow-phase pair");
        }

        private static void RemovalInvalidatesWithoutRequeue()
        {
            var controller = new CoordinationIncrementalScanController();
            controller.ApplySnapshot(2d, new[] { Item("A", "1", 0, 2), Item("B", "1", 1, 3) });
            var removed = controller.ApplySnapshot(2d, new[] { Item("A", "1", 0, 2) });
            Equal("B", string.Join("|", removed.Delta.RemovedIds), "removed element not detected");
            Equal("A\u001fB", string.Join("|", removed.InvalidatedPairKeys), "removed pair not invalidated");
            Equal(string.Empty, JoinPairs(removed), "removed element queued a current pair");
        }

        private static void CaseOnlyIdentityDriftInvalidatesOldAndQueuesCurrentPair()
        {
            var controller = new CoordinationIncrementalScanController();
            controller.ApplySnapshot(2d, new[] { Item("A", "1", 0, 2), Item("B", "1", 1, 3) });

            var changed = controller.ApplySnapshot(2d, new[] { Item("a", "1", 0, 2), Item("B", "1", 1, 3) });

            True(!changed.IsNoOp, "case-only ItemId drift was silently treated as a no-op");
            Equal("a", string.Join("|", changed.Delta.ChangedOrAddedIds), "case-only ItemId drift was not marked changed");
            Equal("A\u001fB", string.Join("|", changed.InvalidatedPairKeys), "old pair identity was not invalidated");
            Equal("B\u001fa", JoinPairs(changed), "current pair identity was not queued");
        }

        private static void DuplicateDirtyNotificationsCoalesce()
        {
            var controller = new CoordinationIncrementalScanController();
            controller.ApplySnapshot(2d, new[] { Item("A", "1", 0, 2), Item("B", "1", 1, 3) });
            controller.MarkDirty("A");
            controller.MarkDirty("a");
            True(controller.PendingDirtyCount == 1, "case-insensitive dirty notifications did not coalesce");
            var result = controller.ApplySnapshot(2d, new[] { Item("B", "1", 1, 3), Item("A", "1", 0, 2) });
            True(result.IsNoOp, "dirty notification without source revision/bounds change caused recomputation churn");
            True(controller.PendingDirtyCount == 0, "successful no-op did not consume pending dirty notification");
        }

        private static void CellSizeChangeForcesFullRescan()
        {
            var controller = new CoordinationIncrementalScanController();
            controller.ApplySnapshot(2d, new[] { Item("A", "1", 0, 2), Item("B", "1", 1, 3) });
            var changed = controller.ApplySnapshot(4d, new[] { Item("A", "1", 0, 2), Item("B", "1", 1, 3) });
            True(changed.RequiresFullRescan, "cell-size policy change did not force full rescan");
            Equal("A\u001fB", string.Join("|", changed.InvalidatedPairKeys), "full rescan did not invalidate prior pair set");
            Equal("A\u001fB", JoinPairs(changed), "full rescan did not queue current pair set");
        }

        private static void UnknownDirtyFailsClosedWithoutLosingState()
        {
            var controller = new CoordinationIncrementalScanController();
            controller.ApplySnapshot(2d, new[] { Item("A", "1", 0, 1) });
            controller.MarkDirty("MISSING");
            Throws<System.Collections.Generic.KeyNotFoundException>(() =>
                controller.ApplySnapshot(2d, new[] { Item("A", "1", 0, 1) }));
            True(controller.PendingDirtyCount == 1, "failed apply silently consumed unknown dirty id");
            controller.Reset();
            True(!controller.HasSnapshot && controller.PendingDirtyCount == 0, "reset did not clear incremental state");
        }

        private static string JoinPairs(CoordinationIncrementalScanResult result)
        {
            return string.Join("|", result.CandidatePairs.Select(pair => pair.PairKey));
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("CoordinationIncrementalScanControllerSmoke: " + message + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "CoordinationIncrementalScanControllerSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("CoordinationIncrementalScanControllerSmoke: expected " + typeof(T).Name + ".");
        }
    }
}
