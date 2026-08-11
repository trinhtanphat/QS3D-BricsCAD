using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundaryRegressionSmoke
    {
        public static void Run()
        {
            RectangleBoundary();
            TjunctionCreatesAdjacentRooms();
            EndpointToleranceClosesGap();
            DanglingBridgeIsIgnored();
            LongDanglingChainIsIgnored();
            SparseBroadPhasePreservesRoom();
            DuplicateSegmentsKeepSourceEvidence();
            BulgeSemicircleTessellation();
            BulgeDirectionMirrors();
            CurvedRoomBoundary();
            LargeRadiusTinySagittaHonorsLimit();
            ExtremeFiniteBulgeAvoidsIntermediateOverflow();
            InvalidCoordinatesRejected();
            InvalidBulgeToleranceRejected();
        }

        private static void RectangleBoundary()
        {
            var boundaries = new RoomBoundaryEngine().Discover(new[]
            {
                S(4, 0, 4, 3, "R"), S(0, 3, 0, 0, "L"), S(4, 3, 0, 3, "T"), S(0, 0, 4, 0, "B")
            });
            Equal(1, boundaries.Count);
            Near(12d, boundaries[0].Area);
            Near(14d, boundaries[0].Perimeter);
            Equal(4, boundaries[0].Vertices.Count);
            Equal(4, boundaries[0].SourceIds.Count);
            True(!string.IsNullOrWhiteSpace(boundaries[0].Key));
        }

        private static void TjunctionCreatesAdjacentRooms()
        {
            var boundaries = new RoomBoundaryEngine().Discover(new[]
            {
                S(0, 0, 6, 0), S(6, 0, 6, 3), S(6, 3, 0, 3), S(0, 3, 0, 0), S(3, 0, 3, 3)
            });
            Equal(2, boundaries.Count);
            Near(18d, boundaries.Sum(x => x.Area));
            True(boundaries.All(x => Math.Abs(x.Area - 9d) < 1e-9));
        }

        private static void EndpointToleranceClosesGap()
        {
            var boundaries = new RoomBoundaryEngine().Discover(new[]
            {
                S(0, 0, 4, 0), S(4.0004, 0.0002, 4, 3), S(4, 3, 0, 3), S(0, 3, -0.0003, 0.0001)
            }, 0.001d, 0.01d);
            Equal(1, boundaries.Count);
            Near(12d, boundaries[0].Area, 0.01d);
        }

        private static void DanglingBridgeIsIgnored()
        {
            var boundaries = new RoomBoundaryEngine().Discover(new[]
            {
                S(0, 0, 4, 0), S(4, 0, 4, 3), S(4, 3, 0, 3), S(0, 3, 0, 0), S(2, 0, 2, -2)
            });
            Equal(1, boundaries.Count);
            Near(12d, boundaries[0].Area);
            Near(14d, boundaries[0].Perimeter);
        }

        private static void LongDanglingChainIsIgnored()
        {
            var segments = new List<BoundarySegment>
            {
                S(0, 0, 4, 0), S(4, 0, 4, 3), S(4, 3, 0, 3), S(0, 3, 0, 0)
            };
            for (var index = 0; index < 1024; index++) segments.Add(S(4 + index, 0, 5 + index, 0));

            var boundaries = new RoomBoundaryEngine().Discover(segments);
            Equal(1, boundaries.Count);
            Near(12d, boundaries[0].Area);
            Near(14d, boundaries[0].Perimeter);
        }

        private static void SparseBroadPhasePreservesRoom()
        {
            var segments = new List<BoundarySegment>
            {
                S(0, 0, 4, 0, "B"), S(4, 0, 4, 3, "R"), S(4, 3, 0, 3, "T"), S(0, 3, 0, 0, "L")
            };
            for (var index = 0; index < 512; index++)
            {
                var x = 1000d + index * 10d;
                segments.Add(S(x, 1000d, x + 1d, 1000d, "SPARSE-" + index));
            }

            var boundaries = new RoomBoundaryEngine().Discover(segments);
            Equal(1, boundaries.Count);
            Near(12d, boundaries[0].Area);
            Near(14d, boundaries[0].Perimeter);
            Equal(4, boundaries[0].SourceIds.Count);
        }

        private static void DuplicateSegmentsKeepSourceEvidence()
        {
            var boundaries = new RoomBoundaryEngine().Discover(new[]
            {
                S(0, 0, 4, 0, "B1"), S(4, 0, 0, 0, "B2"),
                S(4, 0, 4, 3, "R"), S(4, 3, 0, 3, "T"), S(0, 3, 0, 0, "L")
            });
            Equal(1, boundaries.Count);
            Equal(5, boundaries[0].SourceIds.Count);
            True(boundaries[0].SourceIds.Contains("B1"));
            True(boundaries[0].SourceIds.Contains("B2"));
        }

        private static void BulgeSemicircleTessellation()
        {
            var points = BulgeArcTessellator.Tessellate(new Point2(-1, 0), new Point2(1, 0), 1d, 0.001d);
            True(points.Count > 8);
            Equal(new Point2(-1, 0), points[0]);
            Equal(new Point2(1, 0), points[points.Count - 1]);
            foreach (var point in points) Near(1d, Math.Sqrt(point.X * point.X + point.Y * point.Y), 1e-9);
            Near(Math.PI, PolylineMetrics.Length(points, false), 0.01d);
        }

        private static void BulgeDirectionMirrors()
        {
            var positive = BulgeArcTessellator.Tessellate(new Point2(-1, 0), new Point2(1, 0), 1d, 0.002d);
            var negative = BulgeArcTessellator.Tessellate(new Point2(-1, 0), new Point2(1, 0), -1d, 0.002d);
            var positiveY = positive.Skip(1).Take(positive.Count - 2).Average(x => x.Y);
            var negativeY = negative.Skip(1).Take(negative.Count - 2).Average(x => x.Y);
            True(positiveY < 0d);
            True(negativeY > 0d);
            Near(Math.Abs(positiveY), Math.Abs(negativeY), 1e-12d);
        }

        private static void CurvedRoomBoundary()
        {
            var arc = BulgeArcTessellator.Tessellate(new Point2(-1, 0), new Point2(1, 0), 1d, 0.0005d);
            var segments = new List<BoundarySegment>();
            for (var i = 1; i < arc.Count; i++) segments.Add(new BoundarySegment(arc[i - 1], arc[i], "ARC"));
            segments.Add(new BoundarySegment(new Point2(1, 0), new Point2(-1, 0), "DIAMETER"));
            var boundaries = new RoomBoundaryEngine().Discover(segments, 0.0001d, 0.01d);
            Equal(1, boundaries.Count);
            Near(Math.PI / 2d, boundaries[0].Area, 0.01d);
            Near(Math.PI + 2d, boundaries[0].Perimeter, 0.01d);
            Equal(2, boundaries[0].SourceIds.Count);
        }

        private static void LargeRadiusTinySagittaHonorsLimit()
        {
            Throws<InvalidOperationException>(() => BulgeArcTessellator.Tessellate(
                new Point2(-1e12, 0), new Point2(1e12, 0), 1d, 1e-6d));
        }

        private static void ExtremeFiniteBulgeAvoidsIntermediateOverflow()
        {
            var start = new Point2(-1d, 0d);
            var end = new Point2(1d, 0d);
            var points = BulgeArcTessellator.Tessellate(start, end, 1e200d, 1e200d);
            True(points.Count > 2 && points.Count <= 4097);
            Equal(start, points[0]);
            Equal(end, points[points.Count - 1]);
            True(points.All(point => !double.IsNaN(point.X) && !double.IsInfinity(point.X) && !double.IsNaN(point.Y) && !double.IsInfinity(point.Y)));
        }

        private static void InvalidCoordinatesRejected()
        {
            Throws<ArgumentOutOfRangeException>(() => new RoomBoundaryEngine().Discover(new[]
            {
                new BoundarySegment(new Point2(double.NaN, 0), new Point2(1, 0))
            }));
        }

        private static void InvalidBulgeToleranceRejected()
        {
            Throws<ArgumentOutOfRangeException>(() => BulgeArcTessellator.Tessellate(new Point2(0, 0), new Point2(1, 0), 0.5d, 0d));
            Throws<ArgumentOutOfRangeException>(() => BulgeArcTessellator.Tessellate(new Point2(0, 0), new Point2(1, 0), double.NaN, 0.001d));
        }

        private static BoundarySegment S(double x1, double y1, double x2, double y2, string source = "") => new BoundarySegment(new Point2(x1, y1), new Point2(x2, y2), source);
        private static void Near(double expected, double actual, double tolerance = 1e-9) { if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
