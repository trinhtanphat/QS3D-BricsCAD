using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineSignedAreaDeltaOverflowSmoke
    {
        private const double MinNormal = 2.2250738585072014e-308;

        [ModuleInitializer]
        internal static void Initialize()
        {
            ExtremeFiniteTriangleKeepsRepresentablePositiveArea();
            ExtremeFiniteTriangleKeepsRepresentableNegativeArea();
            OrdinaryAreaRemainsUnchanged();
            GenuineAreaOverflowStillFailsClosed();
            NonFiniteInputStillFailsClosed();
        }

        private static void ExtremeFiniteTriangleKeepsRepresentablePositiveArea()
        {
            var points = new[]
            {
                new Point2(-double.MaxValue, 0d),
                new Point2(double.MaxValue, 0d),
                new Point2(0d, MinNormal)
            };
            Near(4d, PolylineMetrics.SignedArea(points), 1e-12d, "extreme finite positive triangle");
        }

        private static void ExtremeFiniteTriangleKeepsRepresentableNegativeArea()
        {
            var points = new[]
            {
                new Point2(-double.MaxValue, 0d),
                new Point2(0d, MinNormal),
                new Point2(double.MaxValue, 0d)
            };
            Near(-4d, PolylineMetrics.SignedArea(points), 1e-12d, "extreme finite negative triangle");
        }

        private static void OrdinaryAreaRemainsUnchanged()
        {
            var square = new[]
            {
                new Point2(0d, 0d),
                new Point2(2d, 0d),
                new Point2(2d, 3d),
                new Point2(0d, 3d)
            };
            Near(6d, PolylineMetrics.SignedArea(square), 1e-12d, "ordinary square");
        }

        private static void GenuineAreaOverflowStillFailsClosed()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(double.MaxValue, 0d),
                new Point2(0d, double.MaxValue)
            };
            Expect<OverflowException>(() => PolylineMetrics.SignedArea(points), "genuine signed-area overflow");
        }

        private static void NonFiniteInputStillFailsClosed()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(double.PositiveInfinity, 0d),
                new Point2(0d, 1d)
            };
            Expect<InvalidOperationException>(() => PolylineMetrics.SignedArea(points), "non-finite coordinate");
        }

        private static void Near(double expected, double actual, double tolerance, string scenario)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(actual - expected) > tolerance)
                throw new InvalidOperationException("Unexpected signed area for " + scenario + ": expected " + expected + ", got " + actual + ".");
        }

        private static void Expect<T>(Action action, string scenario) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(T).Name + " for " + scenario + ".");
        }
    }
}
