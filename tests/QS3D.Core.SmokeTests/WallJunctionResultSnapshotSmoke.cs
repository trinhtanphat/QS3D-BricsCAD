using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionResultSnapshotSmoke
    {
        public static void Run()
        {
            DirectResultOwnsSegmentIdSnapshot();
            PlannerResultRemainsStableAndReadOnly();
        }

        private static void DirectResultOwnsSegmentIdSnapshot()
        {
            var source = new List<string> { "A", "B", "C" };
            var junction = new WallJunction(new Point2(1d, 2d), WallJunctionKind.T, source, 3);

            source[0] = "MUTATED";
            source.Clear();

            if (junction.SegmentIds.Count != 3 ||
                !string.Equals(junction.SegmentIds[0], "A", StringComparison.Ordinal) ||
                !string.Equals(junction.SegmentIds[1], "B", StringComparison.Ordinal) ||
                !string.Equals(junction.SegmentIds[2], "C", StringComparison.Ordinal))
                throw new InvalidOperationException("Wall junction result changed after mutating its source segment-id list.");

            if (junction.SegmentIds is IList<string> mutable)
            {
                try
                {
                    mutable[0] = "ILLEGAL";
                    throw new InvalidOperationException("Wall junction segment-id snapshot remained externally mutable.");
                }
                catch (NotSupportedException)
                {
                }
            }
        }

        private static void PlannerResultRemainsStableAndReadOnly()
        {
            var junctions = new WallJunctionPlanner().Plan(new[]
            {
                new WallAxisSegment("H", new Point2(-1d, 0d), new Point2(1d, 0d)),
                new WallAxisSegment("V", new Point2(0d, 0d), new Point2(0d, 1d))
            });

            WallJunction? t = null;
            foreach (var junction in junctions)
            {
                if (junction.Kind == WallJunctionKind.T && junction.SegmentIds.Count == 2)
                {
                    t = junction;
                    break;
                }
            }

            if (t == null)
                throw new InvalidOperationException("Wall junction planner did not preserve the expected T-junction result.");
            if (!string.Equals(t.SegmentIds[0], "H", StringComparison.Ordinal) ||
                !string.Equals(t.SegmentIds[1], "V", StringComparison.Ordinal))
                throw new InvalidOperationException("Wall junction planner segment-id ordering changed unexpectedly.");
        }
    }

    internal static class WallJunctionResultSnapshotSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            WallJunctionResultSnapshotSmoke.Run();
        }
    }
}
