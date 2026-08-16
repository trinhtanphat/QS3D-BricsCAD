using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineLengthCompensationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PreservesRepresentableSmallSegmentsAfterHugeSegment();
            PreservesRepresentableSmallSegmentsAroundHugeSegment();
            OrdinaryOpenAndClosedLengthsRemainUnchanged();
        }

        private static void PreservesRepresentableSmallSegmentsAfterHugeSegment()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(1e16, 0d),
                new Point2(1e16, 1d),
                new Point2(1e16, 2d)
            };

            Exact(10000000000000002d, PolylineMetrics.Length(points, closed: false), "huge segment followed by two unit segments");
        }

        private static void PreservesRepresentableSmallSegmentsAroundHugeSegment()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(0d, 1d),
                new Point2(1e16, 1d),
                new Point2(1e16, 2d)
            };

            Exact(10000000000000002d, PolylineMetrics.Length(points, closed: false), "unit segments around a huge segment");
        }

        private static void OrdinaryOpenAndClosedLengthsRemainUnchanged()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(3d, 4d),
                new Point2(6d, 8d)
            };

            Exact(10d, PolylineMetrics.Length(points, closed: false), "ordinary open polyline");
            Exact(20d, PolylineMetrics.Length(points, closed: true), "ordinary closed polyline");
        }

        private static void Exact(double expected, double actual, string scenario)
        {
            if (actual != expected)
                throw new InvalidOperationException("Unexpected polyline length for " + scenario + ": expected " + expected + ", got " + actual + ".");
        }
    }
}
