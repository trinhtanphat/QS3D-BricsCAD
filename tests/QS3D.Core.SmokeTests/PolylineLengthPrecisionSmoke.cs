using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineLengthPrecisionSmoke
    {
        internal static void Run()
        {
            HugeThenSmallSegmentsRemainVisible();
            SmallSegmentsAroundHugeSegmentRemainVisible();
            ClosedPathRetainsSmallClosingContributions();
            ExplicitClosingVertexDoesNotDoubleCount();
            DuplicateVerticesRemainZeroLength();
            EmptyAndSinglePointRemainCanonical();
            OrdinaryOpenAndClosedLengthsRemainStable();
            NonFiniteCoordinatesRemainRejected();
            DistanceDeltaOverflowRemainsRejected();
            AccumulatedLengthOverflowRemainsRejected();
            SignedAreaRemainsUnchanged();
        }

        private static void HugeThenSmallSegmentsRemainVisible()
        {
            const double huge = 9007199254740992d;
            const double expected = 9007199254740994d;
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(huge, 0d),
                new Point2(huge, 1d),
                new Point2(huge, 2d)
            };
            AssertEqual(expected, PolylineMetrics.Length(points, closed: false), "huge-then-small open length");
        }

        private static void SmallSegmentsAroundHugeSegmentRemainVisible()
        {
            const double huge = 9007199254740992d;
            const double expected = 9007199254740994d;
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(1d, 0d),
                new Point2(1d, huge),
                new Point2(2d, huge)
            };
            AssertEqual(expected, PolylineMetrics.Length(points, closed: false), "small-huge-small open length");
        }

        private static void ClosedPathRetainsSmallClosingContributions()
        {
            const double width = 4503599627370496d;
            const double expected = 9007199254740994d;
            var rectangle = new[]
            {
                new Point2(0d, 0d),
                new Point2(width, 0d),
                new Point2(width, 1d),
                new Point2(0d, 1d)
            };
            AssertEqual(expected, PolylineMetrics.Length(rectangle, closed: true), "closed rectangle perimeter");
        }

        private static void ExplicitClosingVertexDoesNotDoubleCount()
        {
            const double width = 4503599627370496d;
            const double expected = 9007199254740994d;
            var rectangle = new[]
            {
                new Point2(0d, 0d),
                new Point2(width, 0d),
                new Point2(width, 1d),
                new Point2(0d, 1d),
                new Point2(0d, 0d)
            };
            AssertEqual(expected, PolylineMetrics.Length(rectangle, closed: true), "explicit closing vertex perimeter");
        }

        private static void DuplicateVerticesRemainZeroLength()
        {
            const double huge = 9007199254740992d;
            const double expected = 9007199254740994d;
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(huge, 0d),
                new Point2(huge, 0d),
                new Point2(huge, 1d),
                new Point2(huge, 2d)
            };
            AssertEqual(expected, PolylineMetrics.Length(points, closed: false), "duplicate vertex length");
        }

        private static void EmptyAndSinglePointRemainCanonical()
        {
            AssertEqual(0d, PolylineMetrics.Length(Array.Empty<Point2>(), closed: false), "empty length");
            AssertEqual(0d, PolylineMetrics.Length(new[] { new Point2(4d, -3d) }, closed: true), "single-point length");
        }

        private static void OrdinaryOpenAndClosedLengthsRemainStable()
        {
            var open = new[] { new Point2(0d, 0d), new Point2(3d, 4d), new Point2(6d, 8d) };
            AssertEqual(10d, PolylineMetrics.Length(open, closed: false), "ordinary open length");

            var triangle = new[] { new Point2(0d, 0d), new Point2(3d, 0d), new Point2(3d, 4d) };
            AssertEqual(12d, PolylineMetrics.Length(triangle, closed: true), "ordinary closed length");
        }

        private static void NonFiniteCoordinatesRemainRejected()
        {
            ExpectThrows<InvalidOperationException>(() =>
                PolylineMetrics.Length(new[] { new Point2(0d, 0d), new Point2(double.NaN, 1d) }, closed: false),
                "NaN coordinate");
            ExpectThrows<InvalidOperationException>(() =>
                PolylineMetrics.Length(new[] { new Point2(0d, 0d), new Point2(double.PositiveInfinity, 1d) }, closed: false),
                "+Infinity coordinate");
            ExpectThrows<InvalidOperationException>(() =>
                PolylineMetrics.Length(new[] { new Point2(0d, 0d), new Point2(double.NegativeInfinity, 1d) }, closed: false),
                "-Infinity coordinate");
        }

        private static void DistanceDeltaOverflowRemainsRejected()
        {
            ExpectThrows<OverflowException>(() =>
                PolylineMetrics.Length(new[] { new Point2(-double.MaxValue, 0d), new Point2(double.MaxValue, 0d) }, closed: false),
                "distance delta overflow");
        }

        private static void AccumulatedLengthOverflowRemainsRejected()
        {
            ExpectThrows<OverflowException>(() =>
                PolylineMetrics.Length(new[]
                {
                    new Point2(0d, 0d),
                    new Point2(double.MaxValue, 0d),
                    new Point2(0d, 0d)
                }, closed: false),
                "accumulated length overflow");
        }

        private static void SignedAreaRemainsUnchanged()
        {
            var rectangle = new[]
            {
                new Point2(0d, 0d),
                new Point2(4d, 0d),
                new Point2(4d, 3d),
                new Point2(0d, 3d)
            };
            AssertEqual(12d, PolylineMetrics.SignedArea(rectangle), "adjacent signed-area invariant");
        }

        private static void AssertEqual(double expected, double actual, string label)
        {
            if (actual != expected)
                throw new Exception(label + ": expected " + expected + " but got " + actual + ".");
        }

        private static void ExpectThrows<T>(Action action, string label) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception(label + ": expected " + typeof(T).Name + ".");
        }
    }
}
