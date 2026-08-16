using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineLengthCompensatedSumSmoke
    {
        internal static void Run()
        {
            HugeThenSmallContributionsSurvive();
            SmallHugeSmallOrderingSurvives();
            ClosedPathPreservesUnitEdges();
            DuplicateAndExplicitClosingVerticesStayCanonical();
            EmptyAndSinglePointStayZero();
            NonFiniteCoordinatesFailClosed();
            CoordinateDeltaOverflowFailsClosed();
            SignedAreaIsUnaffected();
        }

        private static void HugeThenSmallContributionsSurvive()
        {
            const double largeSegment = 9007199254740992d;
            const double expectedLength = 9007199254740994d;
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(largeSegment, 0d),
                new Point2(largeSegment, 1d),
                new Point2(largeSegment, 2d)
            };

            AssertEqual(expectedLength, PolylineMetrics.Length(points, closed: false), "huge-then-small compensated length");
        }

        private static void SmallHugeSmallOrderingSurvives()
        {
            const double largeSegment = 9007199254740992d;
            const double expectedLength = 9007199254740994d;
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(0d, 1d),
                new Point2(largeSegment, 1d),
                new Point2(largeSegment, 2d)
            };

            AssertEqual(expectedLength, PolylineMetrics.Length(points, closed: false), "small-huge-small compensated length");
        }

        private static void ClosedPathPreservesUnitEdges()
        {
            const double largeSegment = 4503599627370496d;
            const double expectedPerimeter = 9007199254740994d;
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(largeSegment, 0d),
                new Point2(largeSegment, 1d),
                new Point2(0d, 1d)
            };

            AssertEqual(expectedPerimeter, PolylineMetrics.Length(points, closed: true), "closed compensated perimeter");
        }

        private static void DuplicateAndExplicitClosingVerticesStayCanonical()
        {
            var withDuplicate = new[]
            {
                new Point2(0d, 0d),
                new Point2(3d, 0d),
                new Point2(3d, 0d),
                new Point2(3d, 4d)
            };
            AssertEqual(7d, PolylineMetrics.Length(withDuplicate, closed: false), "duplicate vertex length");

            var explicitClosingVertex = new[]
            {
                new Point2(0d, 0d),
                new Point2(3d, 0d),
                new Point2(3d, 4d),
                new Point2(0d, 0d)
            };
            AssertEqual(12d, PolylineMetrics.Length(explicitClosingVertex, closed: true), "explicit closing vertex length");
        }

        private static void EmptyAndSinglePointStayZero()
        {
            AssertEqual(0d, PolylineMetrics.Length(Array.Empty<Point2>(), closed: false), "empty length");
            AssertEqual(0d, PolylineMetrics.Length(new[] { new Point2(1d, 2d) }, closed: true), "single-point length");
        }

        private static void NonFiniteCoordinatesFailClosed()
        {
            AssertThrows<InvalidOperationException>(() => PolylineMetrics.Length(new[]
            {
                new Point2(0d, 0d),
                new Point2(double.NaN, 1d)
            }, closed: false), "NaN coordinate");

            AssertThrows<InvalidOperationException>(() => PolylineMetrics.Length(new[]
            {
                new Point2(0d, 0d),
                new Point2(double.PositiveInfinity, 1d)
            }, closed: false), "infinite coordinate");
        }

        private static void CoordinateDeltaOverflowFailsClosed()
        {
            AssertThrows<OverflowException>(() => PolylineMetrics.Length(new[]
            {
                new Point2(-double.MaxValue, 0d),
                new Point2(double.MaxValue, 0d)
            }, closed: false), "coordinate delta overflow");
        }

        private static void SignedAreaIsUnaffected()
        {
            var square = new[]
            {
                new Point2(0d, 0d),
                new Point2(2d, 0d),
                new Point2(2d, 2d),
                new Point2(0d, 2d)
            };
            AssertEqual(4d, PolylineMetrics.SignedArea(square), "signed area invariant");
        }

        private static void AssertEqual(double expected, double actual, string scenario)
        {
            if (actual != expected)
                throw new Exception("Expected " + scenario + " " + expected + " but got " + actual + ".");
        }

        private static void AssertThrows<TException>(Action action, string scenario) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new Exception("Expected " + scenario + " to throw " + typeof(TException).Name + ".");
        }
    }
}
