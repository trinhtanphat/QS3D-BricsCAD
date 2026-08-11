using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionOwnershipSmoke
    {
        public static void Run()
        {
            StableIdentityAcrossOrderAndKindChange();
            MultipleOccurrencesAreDeterministic();
            SameSemanticWallDoesNotCreateComposite();
            FingerprintTracksProfileWithoutChangingOwner();
            RejectsCrossDrawingAndMissingOwners();
            RejectsIncompatibleVerticalRanges();
            RejectsInconsistentSameWallProfile();
            RejectsNearDuplicateOccurrences();
        }

        private static void StableIdentityAcrossOrderAndKindChange()
        {
            var first = Plan(
                new[] { Junction(WallJunctionKind.L, 1, 2, "S2", "S1") },
                Owner("S2", "wall-b", "P", "DWG", 0, 3, 0.30),
                Owner("S1", "wall-a", "P", "DWG", 0, 3, 0.20));
            var second = Plan(
                new[] { Junction(WallJunctionKind.T, 1, 3, "S1", "S2") },
                Owner("S1", "WALL-A", "p", "dwg", 0, 3, 0.20),
                Owner("S2", "WALL-B", "p", "dwg", 0, 3, 0.30));

            Equal(1, first.Count);
            Equal(1, second.Count);
            Equal(first[0].GroupToken, second[0].GroupToken);
            Equal(first[0].OwnerToken, second[0].OwnerToken);
            NotEqual(first[0].InputFingerprint, second[0].InputFingerprint);
            Equal("WALL-A", first[0].OwnerWallIds[0]);
            Equal("WALL-B", first[0].OwnerWallIds[1]);
            Equal(0, first[0].OccurrenceIndex);
        }

        private static void MultipleOccurrencesAreDeterministic()
        {
            var near = JunctionAt(WallJunctionKind.L, 1, 0, 2, "S1", "S2");
            var far = JunctionAt(WallJunctionKind.L, 3, 0, 2, "S1", "S2");
            var owners = new[]
            {
                Owner("S1", "W1", "P", "D", 0, 4, 0.20),
                Owner("S2", "W2", "P", "D", 0, 4, 0.20)
            };

            var forward = WallJunctionOwnershipPlanner.Plan(new[] { near, far }, owners);
            var reverse = WallJunctionOwnershipPlanner.Plan(new[] { far, near }, owners.Reverse());

            Equal(2, forward.Count);
            Equal(2, reverse.Count);
            Equal(0, forward[0].OccurrenceIndex);
            Equal(1, forward[1].OccurrenceIndex);
            Equal(1d, forward[0].JunctionPoint.X);
            Equal(3d, forward[1].JunctionPoint.X);
            Equal(forward[0].OwnerToken, reverse[0].OwnerToken);
            Equal(forward[1].OwnerToken, reverse[1].OwnerToken);
        }

        private static void SameSemanticWallDoesNotCreateComposite()
        {
            var plans = Plan(
                new[] { Junction(WallJunctionKind.L, 1, 2, "S1", "S2") },
                Owner("S1", "W1", "P", "D", 0, 3, 0.20),
                Owner("S2", "W1", "P", "D", 0, 3, 0.20));
            Equal(0, plans.Count);
        }

        private static void FingerprintTracksProfileWithoutChangingOwner()
        {
            var junction = new[] { Junction(WallJunctionKind.X, 1, 4, "S1", "S2") };
            var first = Plan(
                junction,
                Owner("S1", "W1", "P", "D", 0, 3, 0.20),
                Owner("S2", "W2", "P", "D", 0, 3, 0.30));
            var changed = Plan(
                junction,
                Owner("S1", "W1", "P", "D", 0, 3, 0.25),
                Owner("S2", "W2", "P", "D", 0, 3, 0.30));

            Equal(first[0].OwnerToken, changed[0].OwnerToken);
            NotEqual(first[0].InputFingerprint, changed[0].InputFingerprint);
            Equal(0d, first[0].BottomM);
            Equal(3d, first[0].TopM);
            Equal(0.20d, first[0].MinThicknessM);
            Equal(0.30d, first[0].MaxThicknessM);
        }

        private static void RejectsCrossDrawingAndMissingOwners()
        {
            var junction = new[] { Junction(WallJunctionKind.L, 1, 2, "S1", "S2") };
            Throws<InvalidOperationException>(() => Plan(
                junction,
                Owner("S1", "W1", "P", "D1", 0, 3, 0.20),
                Owner("S2", "W2", "P", "D2", 0, 3, 0.20)));
            Throws<InvalidOperationException>(() => Plan(
                junction,
                Owner("S1", "W1", "P", "D", 0, 3, 0.20)));
        }

        private static void RejectsIncompatibleVerticalRanges()
        {
            Throws<InvalidOperationException>(() => Plan(
                new[] { Junction(WallJunctionKind.T, 1, 3, "S1", "S2") },
                Owner("S1", "W1", "P", "D", 0, 2, 0.20),
                Owner("S2", "W2", "P", "D", 2.1, 4, 0.20)));
        }

        private static void RejectsInconsistentSameWallProfile()
        {
            Throws<InvalidOperationException>(() => Plan(
                new[] { Junction(WallJunctionKind.L, 1, 2, "S1", "S2") },
                Owner("S1", "W1", "P", "D", 0, 3, 0.20),
                Owner("S2", "W1", "P", "D", 0, 3, 0.25)));
        }

        private static void RejectsNearDuplicateOccurrences()
        {
            var owners = new[]
            {
                Owner("S1", "W1", "P", "D", 0, 3, 0.20),
                Owner("S2", "W2", "P", "D", 0, 3, 0.20)
            };
            Throws<InvalidOperationException>(() => WallJunctionOwnershipPlanner.Plan(
                new[]
                {
                    JunctionAt(WallJunctionKind.L, 1, 0, 2, "S1", "S2"),
                    JunctionAt(WallJunctionKind.L, 1.0000005, 0, 2, "S1", "S2")
                },
                owners,
                pointToleranceM: 1e-6d));
        }

        private static IReadOnlyList<WallJunctionOwnershipPlan> Plan(
            IEnumerable<WallJunction> junctions,
            params WallJunctionOwnerContext[] owners) =>
            WallJunctionOwnershipPlanner.Plan(junctions, owners);

        private static WallJunction Junction(WallJunctionKind kind, double x, int rays, params string[] segmentIds) =>
            JunctionAt(kind, x, 0, rays, segmentIds);

        private static WallJunction JunctionAt(WallJunctionKind kind, double x, double y, int rays, params string[] segmentIds) =>
            new WallJunction(new Point2(x, y), kind, segmentIds, rays);

        private static WallJunctionOwnerContext Owner(
            string sourceSegmentId,
            string wallElementId,
            string projectId,
            string drawingFingerprint,
            double bottomM,
            double topM,
            double thicknessM) =>
            new WallJunctionOwnerContext(sourceSegmentId, wallElementId, projectId, drawingFingerprint, bottomM, topM, thicknessM);

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void NotEqual<T>(T first, T second)
        {
            if (Equals(first, second)) throw new Exception("Expected values to differ, got " + first + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
