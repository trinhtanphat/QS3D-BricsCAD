using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineSignedAreaCrossUnderflowSmoke
    {
        private const double SmallAxis = 1e-200;
        private const double LargeAxis = 2.4e-124;

        [ModuleInitializer]
        internal static void Initialize()
        {
            CounterexampleActuallyUnderflowsDirectProducts();
            UnrepresentableNonZeroTriangleFailsClosed();
            RepresentablePositiveSubnormalAreaIsPreserved();
            RepresentableNegativeSubnormalAreaIsPreserved();
            LegitimateZeroAreaRemainsZero();
            OrdinaryAreaRemainsUnchanged();
        }

        private static void CounterexampleActuallyUnderflowsDirectProducts()
        {
            if (SmallAxis * LargeAxis != 0d)
                throw new InvalidOperationException("Polyline cross-underflow smoke no longer exercises direct multiplication underflow.");
        }

        private static void UnrepresentableNonZeroTriangleFailsClosed()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(SmallAxis, SmallAxis),
                new Point2(-LargeAxis, LargeAxis)
            };

            Throws<OverflowException>(() => PolylineMetrics.SignedArea(points), "terminal signed-area underflow");
            Throws<OverflowException>(() => PolylineMetrics.Area(points), "terminal absolute-area underflow");
        }

        private static void RepresentablePositiveSubnormalAreaIsPreserved()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(SmallAxis, SmallAxis),
                new Point2(-LargeAxis, LargeAxis),
                new Point2(-SmallAxis, -SmallAxis)
            };

            Exact(double.Epsilon, PolylineMetrics.SignedArea(points), "positive subnormal area");
            Exact(double.Epsilon, PolylineMetrics.Area(points), "absolute subnormal area");
        }

        private static void RepresentableNegativeSubnormalAreaIsPreserved()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(-SmallAxis, -SmallAxis),
                new Point2(-LargeAxis, LargeAxis),
                new Point2(SmallAxis, SmallAxis)
            };

            Exact(-double.Epsilon, PolylineMetrics.SignedArea(points), "negative subnormal area");
        }

        private static void LegitimateZeroAreaRemainsZero()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(SmallAxis, SmallAxis),
                new Point2(2d * SmallAxis, 2d * SmallAxis)
            };

            Exact(0d, PolylineMetrics.SignedArea(points), "collinear zero area");
        }

        private static void OrdinaryAreaRemainsUnchanged()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(2d, 0d),
                new Point2(2d, 3d),
                new Point2(0d, 3d)
            };

            Exact(6d, PolylineMetrics.SignedArea(points), "ordinary square");
        }

        private static void Exact(double expected, double actual, string scenario)
        {
            if (actual != expected)
                throw new InvalidOperationException("Unexpected signed area for " + scenario + ": expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<TException>(Action action, string scenario) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + " for " + scenario + ".");
        }
    }
}
