using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class GridDimensionChainSpanPlan
    {
        public GridDimensionChainSpanPlan(
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
    /// Deterministic adjacent spacing plan for one reviewed family of parallel straight Grid references.
    /// Ordering, alignment, bounded input enumeration, identity validation and ambiguity rejection are
    /// delegated to GridSpatialOrderingPlanner. Native Dimension creation remains an adapter concern.
    /// </summary>
    public static class GridDimensionChainPlanner
    {
        public static IReadOnlyList<GridDimensionChainSpanPlan> BuildAdjacentSpans(
            IEnumerable<GridReferenceCurve> curves,
            Point2 orderingAxis,
            bool descending = false,
            double alignmentTolerance = 1e-6,
            double coordinateTolerance = 1e-8)
        {
            if (curves == null) throw new ArgumentNullException(nameof(curves));

            var ordered = GridSpatialOrderingPlanner.OrderParallelLines(
                curves,
                orderingAxis,
                descending,
                alignmentTolerance,
                coordinateTolerance);
            if (ordered.Count < 2)
                throw new InvalidOperationException("At least two ordered parallel Grid LINE references are required for a dimension chain.");

            var spans = new List<GridDimensionChainSpanPlan>(ordered.Count - 1);
            for (var i = 1; i < ordered.Count; i++)
            {
                var first = ordered[i - 1];
                var second = ordered[i];
                var signedSpacing = second.Coordinate - first.Coordinate;
                if (!Finite(signedSpacing))
                    throw new OverflowException(
                        "Grid dimension spacing exceeds the supported numeric range between " +
                        first.ElementId + " and " + second.ElementId + ".");

                var spacing = Math.Abs(signedSpacing);
                if (!Finite(spacing) || !(spacing > coordinateTolerance))
                    throw new InvalidOperationException(
                        "Grid dimension spacing is zero/ambiguous within tolerance between " +
                        first.ElementId + " and " + second.ElementId + ".");

                spans.Add(new GridDimensionChainSpanPlan(
                    first.ElementId,
                    second.ElementId,
                    first.Coordinate,
                    second.Coordinate,
                    spacing));
            }

            if (spans.Count != ordered.Count - 1)
                throw new InvalidOperationException("Grid dimension-chain cardinality is inconsistent with the ordered Grid family.");

            return spans.AsReadOnly();
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
