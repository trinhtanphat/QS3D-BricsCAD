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
            PreservesSmallClosingContributionOnClosedPolyline();
            RejectsNonFiniteCoordinatesBeforeAccumulation();
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

        private static void PreservesSmallClosingContributionOnClosedPolyline()
        {
            const double huge = 4503599627370496d; // 2^52, so the two unit edges remain representable in the final sum.
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(huge, 0d),
                new Point2(huge, 1d),
                new Point2(0d, 1d)
            };

            Exact(9007199254740994d, PolylineMetrics.Length(points, closed: true), "closed rectangle with unit edges around huge edges");
        }

        private static void RejectsNonFiniteCoordinatesBeforeAccumulation()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(double.NaN, 1d)
            };

            try
            {
                PolylineMetrics.Length(points, closed: false);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Polyline length must reject non-finite coordinates before compensated accumulation.");
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
