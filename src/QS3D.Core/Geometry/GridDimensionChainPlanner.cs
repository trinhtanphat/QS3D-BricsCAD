using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class GridDimensionChainSpanPlan
    {
        public GridDimensionChainSpanPlan(
            string firstElementId,
            string secondElementId,
            double firstPosition,
            double secondPosition,
            double spacing,
            Point2 firstWitnessPoint,
            Point2 secondWitnessPoint,
            Point2 dimensionLineOrigin)
        {
            FirstElementId = firstElementId ?? throw new ArgumentNullException(nameof(firstElementId));
            SecondElementId = secondElementId ?? throw new ArgumentNullException(nameof(secondElementId));
            FirstPosition = firstPosition;
            SecondPosition = secondPosition;
            Spacing = spacing;
            FirstWitnessPoint = firstWitnessPoint;
            SecondWitnessPoint = secondWitnessPoint;
            DimensionLineOrigin = dimensionLineOrigin;
        }

        public string FirstElementId { get; }
        public string SecondElementId { get; }
        public double FirstPosition { get; }
        public double SecondPosition { get; }
        public double Spacing { get; }
        public Point2 FirstWitnessPoint { get; }
        public Point2 SecondWitnessPoint { get; }
        public Point2 DimensionLineOrigin { get; }
    }

    /// <summary>
    /// Deterministic adjacent dimension-chain plan for one reviewed family of straight Grid references.
    /// Semantic ordering and bounded input validation are delegated to GridSpatialOrderingPlanner.
    /// Native Dimension creation and DIMSTYLE ownership remain adapter concerns.
    /// </summary>
    public static class GridDimensionChainPlanner
    {
        public static IReadOnlyList<GridDimensionChainSpanPlan> BuildAdjacentSpans(
            IEnumerable<GridReferenceCurve> curves,
            Vector2 alongAxis,
            Point2 dimensionLineOrigin,
            double positionTolerance = 1e-8)
        {
            if (curves == null) throw new ArgumentNullException(nameof(curves));
            if (!Finite(dimensionLineOrigin.X) || !Finite(dimensionLineOrigin.Y))
                throw new ArgumentOutOfRangeException(nameof(dimensionLineOrigin), "Grid dimension-line origin must contain finite coordinates.");

            // Keep the canonical spatial-ordering planner as the single authority for
            // input cardinality, identity, LINE-only validation and deterministic order.
            // Do not pre-materialize the sequence here: doing so would defeat its
            // bounded-enumeration contract for streaming inputs.
            var ordered = GridSpatialOrderingPlanner.Order(curves, alongAxis, positionTolerance);
            if (ordered.Count < 2)
                throw new InvalidOperationException("At least two ordered straight Grid references are required for a dimension chain.");

            var plans = new List<GridDimensionChainSpanPlan>(ordered.Count - 1);
            for (var i = 1; i < ordered.Count; i++)
            {
                var first = ordered[i - 1];
                var second = ordered[i];
                var signedSpacing = second.Position - first.Position;
                if (!Finite(signedSpacing))
                    throw new OverflowException(
                        "Grid dimension spacing exceeds the supported numeric range between " +
                        first.ElementId + " and " + second.ElementId + ".");

                var spacing = Math.Abs(signedSpacing);
                if (!Finite(spacing) || !(spacing > positionTolerance))
                    throw new InvalidOperationException(
                        "Grid dimension spacing is zero/ambiguous within tolerance between " +
                        first.ElementId + " and " + second.ElementId + ".");

                plans.Add(new GridDimensionChainSpanPlan(
                    first.ElementId,
                    second.ElementId,
                    first.Position,
                    second.Position,
                    spacing,
                    first.AnchorPoint,
                    second.AnchorPoint,
                    dimensionLineOrigin));
            }

            if (plans.Count != ordered.Count - 1)
                throw new InvalidOperationException("Grid dimension-chain cardinality is inconsistent with the ordered Grid family.");

            return plans.AsReadOnly();
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
