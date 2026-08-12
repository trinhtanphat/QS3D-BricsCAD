using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridSpatialOrderingAxisScaleSmoke
    {
        public static void Run()
        {
            LargeFiniteAxisPreservesOrdering();
            InvalidAxesRemainRejected();
        }

        private static void LargeFiniteAxisPreservesOrdering()
        {
            var curves = new[]
            {
                GridReferenceCurve.Line("A", new Point2(-1d, 1d), new Point2(1d, -1d)),
                GridReferenceCurve.Line("B", new Point2(9d, 11d), new Point2(11d, 9d))
            };

            var unit = GridSpatialOrderingPlanner.OrderParallelLines(curves, new Point2(1d, 1d));
            var huge = GridSpatialOrderingPlanner.OrderParallelLines(
                curves,
                new Point2(double.MaxValue, double.MaxValue));

            if (unit.Count != 2 || huge.Count != unit.Count)
                throw new InvalidOperationException("Grid ordering axis scaling changed result cardinality.");

            for (var i = 0; i < unit.Count; i++)
            {
                if (!string.Equals(unit[i].ElementId, huge[i].ElementId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Grid ordering axis scaling changed element order.");
                if (!Finite(unit[i].Coordinate) || !Finite(huge[i].Coordinate) ||
                    Math.Abs(unit[i].Coordinate - huge[i].Coordinate) > 1e-12d)
                    throw new InvalidOperationException("Grid ordering axis scaling changed projected coordinates.");
            }
        }

        private static void InvalidAxesRemainRejected()
        {
            var curves = new[]
            {
                GridReferenceCurve.Line("A", new Point2(-1d, 1d), new Point2(1d, -1d))
            };

            ThrowsAxisArgument(curves, new Point2(0d, 0d));
            ThrowsAxisArgument(curves, new Point2(double.NaN, 1d));
            ThrowsAxisArgument(curves, new Point2(1d, double.PositiveInfinity));
        }

        private static void ThrowsAxisArgument(GridReferenceCurve[] curves, Point2 axis)
        {
            try
            {
                GridSpatialOrderingPlanner.OrderParallelLines(curves, axis);
            }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, "orderingAxis", StringComparison.Ordinal))
                    throw new InvalidOperationException("Invalid ordering axis must fail on orderingAxis.");
                return;
            }

            throw new InvalidOperationException("Invalid grid ordering axis was accepted.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
