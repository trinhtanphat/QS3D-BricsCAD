using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionAdjustmentResultSnapshotSmoke
    {
        public static void Run()
        {
            AdjustmentOwnsJunctionIdSnapshot();
            PlanOwnsResultListSnapshots();
        }

        private static void AdjustmentOwnsJunctionIdSnapshot()
        {
            var ids = new List<string> { "A", "B" };
            var adjustment = new WallEndpointAdjustment(
                "A",
                WallEndpointKind.Start,
                new Point2(0d, 0d),
                new Point2(0.001d, 0d),
                0.001d,
                WallJunctionKind.L,
                ids);

            ids[0] = "MUTATED";
            ids.Clear();

            if (adjustment.JunctionSegmentIds.Count != 2 ||
                !string.Equals(adjustment.JunctionSegmentIds[0], "A", StringComparison.Ordinal) ||
                !string.Equals(adjustment.JunctionSegmentIds[1], "B", StringComparison.Ordinal))
                throw new InvalidOperationException("Wall endpoint adjustment changed after mutating its source junction-id list.");
        }

        private static void PlanOwnsResultListSnapshots()
        {
            var adjustmentIds = new List<string> { "A", "B" };
            var adjustment = new WallEndpointAdjustment(
                "A",
                WallEndpointKind.End,
                new Point2(1d, 0d),
                new Point2(1d, 0.001d),
                0.001d,
                WallJunctionKind.T,
                adjustmentIds);
            var junction = new WallJunction(
                new Point2(1d, 0.001d),
                WallJunctionKind.T,
                Array.AsReadOnly(new[] { "A", "B", "C" }),
                3);
            var junctions = new List<WallJunction> { junction };
            var adjustments = new List<WallEndpointAdjustment> { adjustment };
            var plan = new WallJunctionAdjustmentPlan(junctions, adjustments);

            junctions.Clear();
            adjustments.Clear();
            adjustmentIds.Clear();

            if (plan.Junctions.Count != 1 || !ReferenceEquals(plan.Junctions[0], junction))
                throw new InvalidOperationException("Wall junction adjustment plan changed after mutating its source junction list.");
            if (plan.Adjustments.Count != 1 || !ReferenceEquals(plan.Adjustments[0], adjustment))
                throw new InvalidOperationException("Wall junction adjustment plan changed after mutating its source adjustment list.");
            if (plan.Adjustments[0].JunctionSegmentIds.Count != 2 ||
                !string.Equals(plan.Adjustments[0].JunctionSegmentIds[0], "A", StringComparison.Ordinal) ||
                !string.Equals(plan.Adjustments[0].JunctionSegmentIds[1], "B", StringComparison.Ordinal))
                throw new InvalidOperationException("Wall junction adjustment plan lost the nested junction-id snapshot.");
        }
    }

    internal static class WallJunctionAdjustmentResultSnapshotSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            WallJunctionAdjustmentResultSnapshotSmoke.Run();
        }
    }
}
