using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineDegenerateFiniteSmoke
    {
        internal static void Run()
        {
            Equal(0d, PolylineMetrics.Length(Array.Empty<Point2>(), false), "empty length");
            Equal(0d, PolylineMetrics.Length(new[] { new Point2(1d, 2d) }, true), "single-point closed length");
            Equal(0d, PolylineMetrics.SignedArea(Array.Empty<Point2>()), "empty signed area");
            Equal(0d, PolylineMetrics.SignedArea(new[] { new Point2(1d, 2d) }), "single-point signed area");
            Equal(0d, PolylineMetrics.SignedArea(new[] { new Point2(1d, 2d), new Point2(3d, 4d) }), "two-point signed area");

            ExpectInvalid(() => PolylineMetrics.Length(new[] { new Point2(double.NaN, 0d) }, false), "NaN single-point length");
            ExpectInvalid(() => PolylineMetrics.Length(new[] { new Point2(0d, double.PositiveInfinity) }, true), "infinite single-point length");
            ExpectInvalid(() => PolylineMetrics.SignedArea(new[] { new Point2(double.NaN, 0d) }), "NaN single-point area");
            ExpectInvalid(() => PolylineMetrics.SignedArea(new[] { new Point2(0d, 0d), new Point2(double.NegativeInfinity, 1d) }), "infinite two-point area");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (!expected.Equals(actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void ExpectInvalid(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException(label + ": non-finite coordinates were accepted.");
        }
    }
}
