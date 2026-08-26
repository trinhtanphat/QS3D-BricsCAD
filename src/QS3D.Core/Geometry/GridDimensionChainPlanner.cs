using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class GridDimensionSpanPlan
    {
        public GridDimensionSpanPlan(
            string firstElementId,
            string secondElementId,
            double firstCoordinate,
            double secondCoordinate,
            double spacing)
        {
            FirstElementId = firstElementId ?? throw new ArgumentNullException(nameof(firstElementId));
            SecondElementId = secondElementId ?? throw new ArgumentNullException(nameof(secondElementId));
            FirstCoordinate = firstCoordinate;
            SecondCoordinate = secondCoordinate;
            Spacing = spacing;
        }

        public string FirstElementId { get; }
        public string SecondElementId { get; }
        public double FirstCoordinate { get; }
        public double SecondCoordinate { get; }
        public double Spacing { get; }
    }

    /// <summary>
    /// Vendor-neutral adjacent spacing plan for one reviewed parallel LINE Grid family.
    /// Native associative Dimension creation/placement remains an adapter/runtime concern.
    /// </summary>
    public static class GridDimensionChainPlanner
    {
        public static IReadOnlyList<GridDimensionSpanPlan> BuildAdjacentSpans(
            IEnumerable<GridReferenceCurve> curves,
            Point2 orderingAxis,
            bool descending = false,
            double alignmentTolerance = 1e-6,
            double coordinateTolerance = 1e-8)
        {
            if (curves == null) throw new ArgumentNullException(nameof(curves));

            // Materialize once because the canonical ordering planner also performs bounded
            // validation over the reviewed set. This class deliberately delegates all
            // LINE/alignment/identity ambiguity decisions to that existing authority.
            var materialized = curves.ToList();
            if (materialized.Count < 2)
                throw new InvalidOperationException("At least two Grid LINE references are required for an adjacent dimension chain.");

            var ordered = GridSpatialOrderingPlanner.OrderParallelLines(
                materialized,
                orderingAxis,
                descending,
                alignmentTolerance,
                coordinateTolerance);

            if (ordered.Count < 2)
                throw new InvalidOperationException("At least two ordered Grid references are required for an adjacent dimension chain.");

            var spans = new List<GridDimensionSpanPlan>(ordered.Count - 1);
            for (var i = 1; i < ordered.Count; i++)
            {
                var first = ordered[i - 1];
                var second = ordered[i];
                var signedDelta = second.Coordinate - first.Coordinate;
                if (!Finite(signedDelta))
                    throw new OverflowException(
                        "Grid dimension spacing exceeds the supported numeric range between " +
                        first.ElementId + " and " + second.ElementId + ".");

                var spacing = Math.Abs(signedDelta);
                if (!Finite(spacing) || !(spacing > coordinateTolerance))
                    throw new InvalidOperationException(
                        "Grid dimension spacing is zero/ambiguous within tolerance between " +
                        first.ElementId + " and " + second.ElementId + ".");

                spans.Add(new GridDimensionSpanPlan(
                    first.ElementId,
                    second.ElementId,
                    first.Coordinate,
                    second.Coordinate,
                    spacing));
            }

            if (spans.Count != ordered.Count - 1)
                throw new InvalidOperationException("Grid dimension chain cardinality is inconsistent with the ordered Grid family.");

            return spans.AsReadOnly();
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
