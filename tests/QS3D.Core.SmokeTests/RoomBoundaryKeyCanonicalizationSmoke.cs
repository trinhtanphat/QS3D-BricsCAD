using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundaryKeyCanonicalizationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LargePolygonBoundaryKeyIsStable();
            BoundaryKeyIsOrientationAndStartInvariant();
        }

        private static void LargePolygonBoundaryKeyIsStable()
        {
            const int count = 2048;
            const double radius = 10d;
            var points = new List<Point2>(count);
            for (var index = 0; index < count; index++)
            {
                var angle = Math.PI * 2d * index / count;
                points.Add(new Point2(radius * Math.Cos(angle), radius * Math.Sin(angle)));
            }

            var segments = Ring(points, 0, false, "RING");
            var first = new RoomBoundaryEngine().Discover(segments, 0.0001d, 0.01d);
            var second = new RoomBoundaryEngine().Discover(segments, 0.0001d, 0.01d);

            Equal(1, first.Count);
            Equal(1, second.Count);
            Equal(first[0].Key, second[0].Key);
            Equal(count, first[0].Vertices.Count);
            True(first[0].Key.Length > count);
        }

        private static void BoundaryKeyIsOrientationAndStartInvariant()
        {
            var points = new[]
            {
                new Point2(0, 0),
                new Point2(12, 0),
                new Point2(12, 4),
                new Point2(7, 4),
                new Point2(7, 9),
                new Point2(0, 9)
            };

            var original = new RoomBoundaryEngine().Discover(Ring(points, 0, false, "A"), 0.001d, 0.01d);
            var shifted = new RoomBoundaryEngine().Discover(Ring(points, 3, false, "B"), 0.001d, 0.01d);
            var reversed = new RoomBoundaryEngine().Discover(Ring(points, 2, true, "C"), 0.001d, 0.01d);

            Equal(1, original.Count);
            Equal(1, shifted.Count);
            Equal(1, reversed.Count);
            Equal(original[0].Key, shifted[0].Key);
            Equal(original[0].Key, reversed[0].Key);
        }

        private static IReadOnlyList<BoundarySegment> Ring(IReadOnlyList<Point2> points, int start, bool reverse, string source)
        {
            var result = new List<BoundarySegment>(points.Count);
            for (var offset = 0; offset < points.Count; offset++)
            {
                var index = reverse
                    ? Mod(start - offset, points.Count)
                    : Mod(start + offset, points.Count);
                var next = reverse
                    ? Mod(index - 1, points.Count)
                    : Mod(index + 1, points.Count);
                result.Add(new BoundarySegment(points[index], points[next], source));
            }
            return result;
        }

        private static int Mod(int value, int modulus)
        {
            var result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }
    }
}
