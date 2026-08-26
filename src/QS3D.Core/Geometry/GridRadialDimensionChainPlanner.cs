using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class GridRadialDimensionSpanPlan
    {
        public GridRadialDimensionSpanPlan(
            string firstElementId,
            string secondElementId,
            double firstRadius,
            double secondRadius,
            double spacing)
        {
            FirstElementId = firstElementId ?? throw new ArgumentNullException(nameof(firstElementId));
            SecondElementId = secondElementId ?? throw new ArgumentNullException(nameof(secondElementId));
            FirstRadius = firstRadius;
            SecondRadius = secondRadius;
            Spacing = spacing;
        }

        public string FirstElementId { get; }
        public string SecondElementId { get; }
        public double FirstRadius { get; }
        public double SecondRadius { get; }
        public double Spacing { get; }
    }

    /// <summary>
    /// Adjacent radial-spacing plan for one reviewed concentric ARC Grid family.
    /// Ordering/concentric validation is delegated to GridRadialOrderingPlanner.
    /// Native Dimension creation and paper/model-space placement remain adapter concerns.
    /// </summary>
    public static class GridRadialDimensionChainPlanner
    {
        public static IReadOnlyList<GridRadialDimensionSpanPlan> BuildAdjacentSpans(
            IEnumerable<GridReferenceCurve> curves,
            bool descending = false,
            double centerTolerance = 1e-8,
            double radiusTolerance = 1e-8)
        {
            if (curves == null) throw new ArgumentNullException(nameof(curves));

            var materialized = curves.ToList();
            if (materialized.Count < 2)
                throw new InvalidOperationException("At least two concentric Grid ARC references are required for a radial dimension chain.");

            var ordered = GridRadialOrderingPlanner.OrderConcentricArcs(
                materialized,
                descending,
                centerTolerance,
                radiusTolerance);
            if (ordered.Count < 2)
                throw new InvalidOperationException("At least two ordered Grid ARC references are required for a radial dimension chain.");

            var spans = new List<GridRadialDimensionSpanPlan>(ordered.Count - 1);
            for (var i = 1; i < ordered.Count; i++)
            {
                var first = ordered[i - 1];
                var second = ordered[i];
                var signedDelta = second.Radius - first.Radius;
                if (!Finite(signedDelta))
                    throw new OverflowException(
                        "Grid radial dimension spacing exceeds the supported numeric range between " +
                        first.ElementId + " and " + second.ElementId + ".");

                var spacing = Math.Abs(signedDelta);
                if (!Finite(spacing) || !(spacing > radiusTolerance))
                    throw new InvalidOperationException(
                        "Grid radial dimension spacing is zero/ambiguous within tolerance between " +
                        first.ElementId + " and " + second.ElementId + ".");

                spans.Add(new GridRadialDimensionSpanPlan(
                    first.ElementId,
                    second.ElementId,
                    first.Radius,
                    second.Radius,
                    spacing));
            }

            if (spans.Count != ordered.Count - 1)
                throw new InvalidOperationException("Grid radial dimension chain cardinality is inconsistent with the ordered ARC family.");

            return spans.AsReadOnly();
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
