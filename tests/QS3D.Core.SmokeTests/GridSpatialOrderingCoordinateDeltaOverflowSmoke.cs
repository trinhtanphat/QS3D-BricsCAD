using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridSpatialOrderingCoordinateDeltaOverflowSmoke
    {
        public static void Run()
        {
            FarFiniteCoordinatesRemainOrderable();
            NearbyCoordinatesRemainAmbiguous();
        }

        private static void FarFiniteCoordinatesRemainOrderable()
        {
            var coordinate = double.MaxValue * 0.9d;
            var result = GridSpatialOrderingPlanner.OrderParallelLines(
                new[]
                {
                    GridReferenceCurve.Line("NEG", new Point2(-coordinate, -1d), new Point2(-coordinate, 1d)),
                    GridReferenceCurve.Line("POS", new Point2(coordinate, -1d), new Point2(coordinate, 1d))
                },
                new Point2(1d, 0d));

            if (result.Count != 2 ||
                !string.Equals(result[0].ElementId, "NEG", StringComparison.Ordinal) ||
                !string.Equals(result[1].ElementId, "POS", StringComparison.Ordinal))
                throw new InvalidOperationException("Far finite Grid coordinates were not ordered correctly.");
            if (!Finite(result[0].Coordinate) || !Finite(result[1].Coordinate) ||
                !(result[0].Coordinate < 0d) || !(result[1].Coordinate > 0d))
                throw new InvalidOperationException("Far Grid ordering coordinates must remain finite and signed.");
        }

        private static void NearbyCoordinatesRemainAmbiguous()
        {
            try
            {
                GridSpatialOrderingPlanner.OrderParallelLines(
                    new[]
                    {
                        GridReferenceCurve.Line("A", new Point2(0d, -1d), new Point2(0d, 1d)),
                        GridReferenceCurve.Line("B", new Point2(5e-9d, -1d), new Point2(5e-9d, 1d))
                    },
                    new Point2(1d, 0d),
                    coordinateTolerance: 1e-8d);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Nearby Grid ordering coordinates were not rejected as ambiguous.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
