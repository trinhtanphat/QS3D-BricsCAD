using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class AdvancedGeometryRepresentableAreaOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            const double width = 1e155d;
            const double height = 1e155d;
            var polygon = new[]
            {
                new Point2(0d, 0d),
                new Point2(width, 0d),
                new Point2(width, height),
                new Point2(0.995d * width, height),
                new Point2(0.995d * width, 0.005d * height),
                new Point2(0d, 0.005d * height)
            };

            var expectedArea =
                width * (0.005d * height) +
                (0.005d * width) * (0.995d * height);
            var area = PolylineMetrics.SignedArea(polygon);
            AssertRelative(area, expectedArea, 2e-14d, "Extreme concave signed area");

            var reversed = (Point2[])polygon.Clone();
            Array.Reverse(reversed);
            var reversedArea = PolylineMetrics.SignedArea(reversed);
            AssertRelative(reversedArea, -expectedArea, 2e-14d, "Extreme concave reversed signed area");

            var segments = PolygonScanlineClipper.Clip(polygon, PolygonScanAxis.Horizontal, 0.5d * height);
            if (segments.Count != 1)
                throw new InvalidOperationException("Extreme concave scanline should produce exactly one segment.");
            AssertRelative(segments[0].Start.X, 0.995d * width, 2e-14d, "Extreme scanline start X");
            AssertRelative(segments[0].End.X, width, 2e-14d, "Extreme scanline end X");
            AssertRelative(segments[0].Length, 0.005d * width, 2e-14d, "Extreme scanline segment length");

            var ordinary = new[]
            {
                new Point2(0d, 0d),
                new Point2(10d, 0d),
                new Point2(10d, 10d),
                new Point2(0d, 10d)
            };
            AssertRelative(PolylineMetrics.SignedArea(ordinary), 100d, 1e-14d, "Ordinary signed area");

            var trueOverflow = new[]
            {
                new Point2(0d, 0d),
                new Point2(width, 0d),
                new Point2(width, height),
                new Point2(0d, height)
            };
            ExpectOverflow(() => PolylineMetrics.SignedArea(trueOverflow));
        }

        private static void AssertRelative(double actual, double expected, double tolerance, string label)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual))
                throw new InvalidOperationException(label + " must be finite.");
            var scale = Math.Max(1d, Math.Abs(expected));
            var relative = Math.Abs(actual - expected) / scale;
            if (relative > tolerance)
                throw new InvalidOperationException(label + " mismatch. Expected " + expected + ", actual " + actual + ", relative error " + relative + ".");
        }

        private static void ExpectOverflow(Action action)
        {
            try
            {
                action();
            }
            catch (OverflowException)
            {
                return;
            }

            throw new InvalidOperationException("A truly unrepresentable polygon area must still fail closed.");
        }
    }
}
