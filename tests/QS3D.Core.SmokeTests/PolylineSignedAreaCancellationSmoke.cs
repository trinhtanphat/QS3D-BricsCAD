using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineSignedAreaCancellationSmoke
    {
        private const double Ax = 134217729d;
        private const double Ay = 134217728d;
        private const double Bx = 134217728d;
        private const double By = 134217727d;

        [ModuleInitializer]
        internal static void Initialize()
        {
            CounterexampleActuallyCollapsesRoundedProducts();
            NegativeOrientationPreservesHalfSquareUnitArea();
            ReversedOrientationPreservesPositiveHalfSquareUnitArea();
            LargeCollinearTriangleRemainsZero();
        }

        private static void CounterexampleActuallyCollapsesRoundedProducts()
        {
            var firstProduct = Ax * By;
            var secondProduct = Ay * Bx;
            if (firstProduct != secondProduct)
                throw new InvalidOperationException("Polyline signed-area cancellation smoke no longer exercises rounded cross-product cancellation.");
        }

        private static void NegativeOrientationPreservesHalfSquareUnitArea()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(Ax, Ay),
                new Point2(Bx, By)
            };

            Exact(-0.5d, PolylineMetrics.SignedArea(points), "negative cancellation orientation");
            Exact(0.5d, PolylineMetrics.Area(points), "negative cancellation absolute area");
        }

        private static void ReversedOrientationPreservesPositiveHalfSquareUnitArea()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(Bx, By),
                new Point2(Ax, Ay)
            };

            Exact(0.5d, PolylineMetrics.SignedArea(points), "positive cancellation orientation");
            Exact(0.5d, PolylineMetrics.Area(points), "positive cancellation absolute area");
        }

        private static void LargeCollinearTriangleRemainsZero()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(134217728d, 134217728d),
                new Point2(268435456d, 268435456d)
            };

            Exact(0d, PolylineMetrics.SignedArea(points), "large exact collinear triangle");
            Exact(0d, PolylineMetrics.Area(points), "large exact collinear absolute area");
        }

        private static void Exact(double expected, double actual, string scenario)
        {
            if (actual != expected)
                throw new InvalidOperationException("Unexpected signed area for " + scenario + ": expected " + expected + ", got " + actual + ".");
        }
    }
}
