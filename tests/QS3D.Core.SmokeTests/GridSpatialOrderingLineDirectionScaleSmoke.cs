using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridSpatialOrderingLineDirectionScaleSmoke
    {
        public static void Run()
        {
            LargeFiniteLineDirectionRemainsOrderable();
            TrulyDegenerateLineRemainsRejected();
        }

        private static void LargeFiniteLineDirectionRemainsOrderable()
        {
            var axis = new Point2(1d, -1d);
            var normal = GridSpatialOrderingPlanner.OrderParallelLines(
                new[] { GridReferenceCurve.Line("G", new Point2(0d, 0d), new Point2(1d, 1d)) },
                axis);

            var large = double.MaxValue * 0.9d;
            var huge = GridSpatialOrderingPlanner.OrderParallelLines(
                new[] { GridReferenceCurve.Line("G", new Point2(0d, 0d), new Point2(large, large)) },
                axis);

            if (normal.Count != 1 || huge.Count != 1 ||
                !string.Equals(normal[0].ElementId, huge[0].ElementId, StringComparison.Ordinal))
                throw new InvalidOperationException("Large finite Grid LINE direction changed ordering identity.");
            if (!Finite(normal[0].Coordinate) || !Finite(huge[0].Coordinate) ||
                Math.Abs(normal[0].Coordinate - huge[0].Coordinate) > 1e-12d)
                throw new InvalidOperationException("Large finite Grid LINE direction changed its ordering coordinate.");
        }

        private static void TrulyDegenerateLineRemainsRejected()
        {
            try
            {
                GridSpatialOrderingPlanner.OrderParallelLines(
                    new[] { GridReferenceCurve.Line("SHORT", new Point2(0d, 0d), new Point2(1e-10d, 0d)) },
                    new Point2(0d, 1d),
                    coordinateTolerance: 1e-8d);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Grid LINE within coordinate tolerance was not rejected as degenerate.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
