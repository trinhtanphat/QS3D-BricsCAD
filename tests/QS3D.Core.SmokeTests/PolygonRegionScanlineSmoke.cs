using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonRegionScanlineSmoke
    {
        public static void Run()
        {
            HoleSplitsHorizontalAndVerticalScanlines();
            OutsideHoleFailsClosed();
            BoundaryTouchFailsClosed();
            OverlappingHolesFailClosed();
            NestedHolesFailClosed();
            WindingDirectionDoesNotChangeRegion();
            PolygonRegionHoleSnapshotSmoke.Run();
        }

        private static void HoleSplitsHorizontalAndVerticalScanlines()
        {
            var region = PolygonRegionScanlineClipper.NormalizeAndValidate(
                Square(0, 0, 10, 10),
                new[] { Square(4, 4, 6, 6) });

            var horizontal = PolygonRegionScanlineClipper.Clip(region, PolygonScanAxis.Horizontal, 5);
            Equal(2, horizontal.Count);
            Near(0, horizontal[0].Start.X); Near(4, horizontal[0].End.X);
            Near(6, horizontal[1].Start.X); Near(10, horizontal[1].End.X);

            var vertical = PolygonRegionScanlineClipper.Clip(region, PolygonScanAxis.Vertical, 5);
            Equal(2, vertical.Count);
            Near(0, vertical[0].Start.Y); Near(4, vertical[0].End.Y);
            Near(6, vertical[1].Start.Y); Near(10, vertical[1].End.Y);
        }

        private static void OutsideHoleFailsClosed()
        {
            Throws<ArgumentException>(() => PolygonRegionScanlineClipper.NormalizeAndValidate(
                Square(0, 0, 10, 10),
                new[] { Square(9, 9, 12, 12) }));
        }

        private static void BoundaryTouchFailsClosed()
        {
            Throws<ArgumentException>(() => PolygonRegionScanlineClipper.NormalizeAndValidate(
                Square(0, 0, 10, 10),
                new[] { Square(0, 3, 2, 5) }));
        }

        private static void OverlappingHolesFailClosed()
        {
            Throws<ArgumentException>(() => PolygonRegionScanlineClipper.NormalizeAndValidate(
                Square(0, 0, 10, 10),
                new[] { Square(2, 2, 5, 5), Square(4, 4, 7, 7) }));
        }

        private static void NestedHolesFailClosed()
        {
            Throws<ArgumentException>(() => PolygonRegionScanlineClipper.NormalizeAndValidate(
                Square(0, 0, 10, 10),
                new[] { Square(2, 2, 8, 8), Square(3, 3, 4, 4) }));
        }

        private static void WindingDirectionDoesNotChangeRegion()
        {
            var clockwiseHole = new[]
            {
                new Point2(4, 4), new Point2(4, 6), new Point2(6, 6), new Point2(6, 4)
            };
            var region = PolygonRegionScanlineClipper.NormalizeAndValidate(Square(0, 0, 10, 10), new[] { clockwiseHole });
            Equal(2, PolygonRegionScanlineClipper.Clip(region, PolygonScanAxis.Horizontal, 5).Count);
        }

        private static Point2[] Square(double minX, double minY, double maxX, double maxY) => new[]
        {
            new Point2(minX, minY), new Point2(maxX, minY), new Point2(maxX, maxY), new Point2(minX, maxY)
        };

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
