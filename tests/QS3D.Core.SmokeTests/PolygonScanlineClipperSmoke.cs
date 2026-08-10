using System;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonScanlineClipperSmoke
    {
        public static void Run()
        {
            RectangleClipsBothAxes();
            ConcavePolygonCreatesDeterministicSegments();
            ClosingVertexMayBeRepeated();
            SelfIntersectionFailsClosed();
            BoundaryVertexRuleIsDeterministic();
            InvalidAxisFailsClosed();
        }

        private static void RectangleClipsBothAxes()
        {
            var polygon = new[]
            {
                new Point2(0d, 0d), new Point2(4d, 0d), new Point2(4d, 3d), new Point2(0d, 3d)
            };
            var horizontal = PolygonScanlineClipper.Clip(polygon, PolygonScanAxis.Horizontal, 1.5d).Single();
            Near(0d, horizontal.Start.X);
            Near(4d, horizontal.End.X);
            Near(4d, horizontal.Length);

            var vertical = PolygonScanlineClipper.Clip(polygon, PolygonScanAxis.Vertical, 2d).Single();
            Near(0d, vertical.Start.Y);
            Near(3d, vertical.End.Y);
            Near(3d, vertical.Length);
        }

        private static void ConcavePolygonCreatesDeterministicSegments()
        {
            var polygon = new[]
            {
                new Point2(0d, 0d), new Point2(5d, 0d), new Point2(5d, 1d),
                new Point2(2d, 1d), new Point2(2d, 4d), new Point2(5d, 4d),
                new Point2(5d, 5d), new Point2(0d, 5d)
            };

            var segments = PolygonScanlineClipper.Clip(polygon, PolygonScanAxis.Horizontal, 2.5d).ToArray();
            if (segments.Length != 1) throw new Exception("Expected one clipped bar segment through the concave trunk.");
            Near(0d, segments[0].Start.X);
            Near(2d, segments[0].End.X);

            var split = PolygonScanlineClipper.Clip(polygon, PolygonScanAxis.Vertical, 3d).ToArray();
            if (split.Length != 2) throw new Exception("Expected two clipped segments through the concave polygon arms.");
            Near(0d, split[0].Start.Y);
            Near(1d, split[0].End.Y);
            Near(4d, split[1].Start.Y);
            Near(5d, split[1].End.Y);
        }

        private static void ClosingVertexMayBeRepeated()
        {
            var polygon = new[]
            {
                new Point2(0d, 0d), new Point2(2d, 0d), new Point2(2d, 2d), new Point2(0d, 2d), new Point2(0d, 0d)
            };
            var normalized = PolygonScanlineClipper.NormalizeAndValidate(polygon);
            if (normalized.Count != 4) throw new Exception("Repeated closing vertex should be normalized away.");
        }

        private static void SelfIntersectionFailsClosed()
        {
            var failed = false;
            try
            {
                PolygonScanlineClipper.NormalizeAndValidate(new[]
                {
                    new Point2(0d, 0d), new Point2(3d, 3d), new Point2(0d, 3d), new Point2(3d, 0d)
                });
            }
            catch (ArgumentException) { failed = true; }
            if (!failed) throw new Exception("Self-intersecting polygon must fail closed.");
        }

        private static void BoundaryVertexRuleIsDeterministic()
        {
            var triangle = new[] { new Point2(0d, 0d), new Point2(4d, 0d), new Point2(2d, 2d) };
            var baseLine = PolygonScanlineClipper.Clip(triangle, PolygonScanAxis.Horizontal, 0d).Single();
            Near(0d, baseLine.Start.X);
            Near(4d, baseLine.End.X);
            if (PolygonScanlineClipper.Clip(triangle, PolygonScanAxis.Horizontal, 2d).Count != 0)
                throw new Exception("Top boundary scanline should not create a zero-length phantom segment.");
        }

        private static void InvalidAxisFailsClosed()
        {
            var polygon = new[] { new Point2(0d, 0d), new Point2(2d, 0d), new Point2(0d, 2d) };
            var failed = false;
            try { PolygonScanlineClipper.Clip(polygon, (PolygonScanAxis)123, 0.5d); }
            catch (ArgumentOutOfRangeException) { failed = true; }
            if (!failed) throw new Exception("Undefined polygon scan axis must fail closed.");
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
