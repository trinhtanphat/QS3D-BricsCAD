using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionSingleInputValidationSmoke
    {
        internal static void Run()
        {
            Equal(0, GridIntersectionPlanner.FindIntersections(Array.Empty<GridReferenceCurve>()).Count, "empty Grid set");
            Equal(0, GridIntersectionPlanner.FindIntersections(new[]
            {
                GridReferenceCurve.Line("G-VALID", new Point2(0d, 0d), new Point2(5d, 0d))
            }).Count, "single valid Grid LINE");

            Throws<InvalidOperationException>(() => GridIntersectionPlanner.FindIntersections(new[]
            {
                GridReferenceCurve.Line("G-DEGENERATE", new Point2(1d, 1d), new Point2(1d, 1d))
            }), "single degenerate Grid LINE");

            Throws<ArgumentException>(() => GridIntersectionPlanner.FindIntersections(new[]
            {
                GridReferenceCurve.Line("G-NONFINITE", new Point2(double.NaN, 0d), new Point2(1d, 0d))
            }), "single non-finite Grid LINE");
        }

        private static void Equal(int expected, int actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
