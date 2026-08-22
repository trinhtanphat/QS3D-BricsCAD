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
            PreservesFourSmallSegmentsAfterHugeSegment();
            PreservesSmallClosingContributionOnClosedPolyline();
            PreservesSmallSegmentsWhenHugeSegmentComesLast();
            ZeroLengthSegmentsDoNotDisturbCompensation();
            DuplicateClosingVertexRemainsCanonical();
            SinglePointAndEmptyInputsRemainCanonical();
            RejectsNonFiniteCoordinatesBeforeAccumulation();
            RejectsInfiniteCoordinatesBeforeAccumulation();
            RejectsFiniteCoordinateDeltaOverflow();
            RejectsFiniteSegmentsWhoseAccumulatedLengthOverflows();
            OrdinaryOpenAndClosedLengthsRemainUnchanged();
            SignedAreaRemainsUnchanged();
        }

        private static void PreservesRepresentableSmallSegmentsAfterHugeSegment()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(1e16, 0d), new Point2(1e16, 1d), new Point2(1e16, 2d) };
            Exact(10000000000000002d, PolylineMetrics.Length(points, false), "huge segment followed by two unit segments");
        }

        private static void PreservesRepresentableSmallSegmentsAroundHugeSegment()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(0d, 1d), new Point2(1e16, 1d), new Point2(1e16, 2d) };
            Exact(10000000000000002d, PolylineMetrics.Length(points, false), "unit segments around a huge segment");
        }

        private static void PreservesFourSmallSegmentsAfterHugeSegment()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(1e16, 0d), new Point2(1e16, 1d), new Point2(1e16, 2d), new Point2(1e16, 3d), new Point2(1e16, 4d) };
            Exact(10000000000000004d, PolylineMetrics.Length(points, false), "four unit segments accumulated after a huge segment");
        }

        private static void PreservesSmallClosingContributionOnClosedPolyline()
        {
            const double huge = 4503599627370496d;
            var points = new[] { new Point2(0d, 0d), new Point2(huge, 0d), new Point2(huge, 1d), new Point2(0d, 1d) };
            Exact(9007199254740994d, PolylineMetrics.Length(points, true), "closed rectangle with unit edges around huge edges");
        }

        private static void PreservesSmallSegmentsWhenHugeSegmentComesLast()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(0d, 1d), new Point2(0d, 2d), new Point2(1e16, 2d) };
            Exact(10000000000000002d, PolylineMetrics.Length(points, false), "two unit segments before a huge segment");
        }

        private static void ZeroLengthSegmentsDoNotDisturbCompensation()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(1e16, 0d), new Point2(1e16, 0d), new Point2(1e16, 1d), new Point2(1e16, 1d), new Point2(1e16, 2d) };
            Exact(10000000000000002d, PolylineMetrics.Length(points, false), "duplicate vertices around compensated small segments");
        }

        private static void DuplicateClosingVertexRemainsCanonical()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(3d, 0d), new Point2(3d, 4d), new Point2(0d, 0d) };
            Exact(12d, PolylineMetrics.Length(points, false), "explicitly closed point sequence");
            Exact(12d, PolylineMetrics.Length(points, true), "explicitly closed point sequence with zero closing segment");
        }

        private static void SinglePointAndEmptyInputsRemainCanonical()
        {
            Exact(0d, PolylineMetrics.Length(Array.Empty<Point2>(), false), "empty polyline");
            Exact(0d, PolylineMetrics.Length(new[] { new Point2(3d, 4d) }, true), "single-point closed polyline");
        }

        private static void RejectsNonFiniteCoordinatesBeforeAccumulation() => ThrowsInvalidCoordinates(new[] { new Point2(0d, 0d), new Point2(double.NaN, 1d) }, "NaN coordinate");

        private static void RejectsInfiniteCoordinatesBeforeAccumulation()
        {
            ThrowsInvalidCoordinates(new[] { new Point2(0d, 0d), new Point2(double.PositiveInfinity, 1d) }, "infinite coordinate");
            ThrowsInvalidCoordinates(new[] { new Point2(double.NegativeInfinity, 0d), new Point2(0d, 1d) }, "negative infinite coordinate");
        }

        private static void RejectsFiniteCoordinateDeltaOverflow()
        {
            var points = new[] { new Point2(-double.MaxValue, 0d), new Point2(double.MaxValue, 0d) };
            try { PolylineMetrics.Length(points, false); }
            catch (OverflowException) { return; }
            throw new InvalidOperationException("Polyline length must fail closed when finite coordinates overflow during distance delta calculation.");
        }

        private static void RejectsFiniteSegmentsWhoseAccumulatedLengthOverflows()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(9e307, 0d), new Point2(0d, 0d), new Point2(9e307, 0d) };
            try { PolylineMetrics.Length(points, false); }
            catch (OverflowException) { return; }
            throw new InvalidOperationException("Polyline length must fail closed when finite segment lengths overflow during accumulation.");
        }

        private static void OrdinaryOpenAndClosedLengthsRemainUnchanged()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(3d, 4d), new Point2(6d, 8d) };
            Exact(10d, PolylineMetrics.Length(points, false), "ordinary open polyline");
            Exact(20d, PolylineMetrics.Length(points, true), "ordinary closed polyline");
        }

        private static void SignedAreaRemainsUnchanged()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(4d, 0d), new Point2(4d, 3d), new Point2(0d, 3d) };
            Exact(12d, PolylineMetrics.SignedArea(points), "adjacent signed-area invariant");
        }

        private static void ThrowsInvalidCoordinates(Point2[] points, string scenario)
        {
            try { PolylineMetrics.Length(points, false); }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException("Polyline length must reject " + scenario + " before compensated accumulation.");
        }

        private static void Exact(double expected, double actual, string scenario)
        {
            if (actual != expected) throw new InvalidOperationException("Unexpected polyline metric for " + scenario + ": expected " + expected + ", got " + actual + ".");
        }
    }
}
