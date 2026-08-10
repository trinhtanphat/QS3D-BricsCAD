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
            InvalidCoordinatesRejected();
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

        private static void InvalidCoordinatesRejected()
        {
            Throws<ArgumentOutOfRangeException>(() => new RoomBoundaryEngine().Discover(new[]
            {
                new BoundarySegment(new Point2(double.NaN, 0), new Point2(1, 0))
            }));
        }

        private static BoundarySegment S(double x1, double y1, double x2, double y2, string source = "") => new BoundarySegment(new Point2(x1, y1), new Point2(x2, y2), source);
        private static void Near(double expected, double actual, double tolerance = 1e-9) { if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
