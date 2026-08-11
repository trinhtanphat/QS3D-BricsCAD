using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundaryBroadPhaseSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SparseNearLimitNetworkPreservesRoom();
            SweepKeepsTJunctionCandidates();
            ToleranceExpandedBoundsKeepNearEndpoints();
        }

        private static void SparseNearLimitNetworkPreservesRoom()
        {
            var segments = new List<BoundarySegment>();
            for (var index = 0; index < 4500; index++)
            {
                var x = 10000d + index * 10d;
                segments.Add(S(x, 1000d, x + 1d, 1000d, "SPARSE-" + index));
            }

            segments.Add(S(0, 0, 4, 0, "B"));
            segments.Add(S(4, 0, 4, 3, "R"));
            segments.Add(S(4, 3, 0, 3, "T"));
            segments.Add(S(0, 3, 0, 0, "L"));

            var first = new RoomBoundaryEngine().Discover(segments);
            var second = new RoomBoundaryEngine().Discover(segments);

            Equal(1, first.Count);
            Equal(1, second.Count);
            Equal(first[0].Key, second[0].Key);
            Near(12d, first[0].Area);
            Near(14d, first[0].Perimeter);
            Equal(new[] { "B", "L", "R", "T" }, first[0].SourceIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray());
        }

        private static void SweepKeepsTJunctionCandidates()
        {
            var boundaries = new RoomBoundaryEngine().Discover(new[]
            {
                S(6, 3, 0, 3, "T"),
                S(3, 0, 3, 3, "M"),
                S(0, 3, 0, 0, "L"),
                S(6, 0, 6, 3, "R"),
                S(0, 0, 6, 0, "B")
            });

            Equal(2, boundaries.Count);
            Near(18d, boundaries.Sum(x => x.Area));
            True(boundaries.All(x => Math.Abs(x.Area - 9d) < 1e-9d));
        }

        private static void ToleranceExpandedBoundsKeepNearEndpoints()
        {
            var boundaries = new RoomBoundaryEngine().Discover(new[]
            {
                S(0, 0, 4, 0),
                S(4.0004, 0.0002, 4, 3),
                S(4, 3, 0, 3),
                S(0, 3, -0.0003, 0.0001)
            }, 0.001d, 0.01d);

            Equal(1, boundaries.Count);
            Near(12d, boundaries[0].Area, 0.01d);
        }

        private static BoundarySegment S(double x1, double y1, double x2, double y2, string source = "")
            => new BoundarySegment(new Point2(x1, y1), new Point2(x2, y2), source);

        private static void Near(double expected, double actual, double tolerance = 1e-9d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (expected is Array expectedArray && actual is Array actualArray)
            {
                if (expectedArray.Length != actualArray.Length) throw new Exception("Array lengths differ.");
                for (var index = 0; index < expectedArray.Length; index++)
                    if (!Equals(expectedArray.GetValue(index), actualArray.GetValue(index))) throw new Exception("Array values differ at index " + index + ".");
                return;
            }

            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }
    }
}
