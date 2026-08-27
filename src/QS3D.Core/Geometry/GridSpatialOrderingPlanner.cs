using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class GridSpatialOrderingEntry
    {
        public GridSpatialOrderingEntry(string elementId, double coordinate)
        {
            ElementId = elementId;
            Coordinate = coordinate;
        }

        public string ElementId { get; }
        public double Coordinate { get; }
    }

    public enum GridReviewedGroupPrecedence
    {
        LinesThenArcs = 0,
        ArcsThenLines = 1
    }

    public sealed class GridReviewedOrderingEntry
    {
        public GridReviewedOrderingEntry(string elementId, GridReferenceCurveKind kind, int groupIndex, double coordinate)
        {
            ElementId = elementId;
            Kind = kind;
            GroupIndex = groupIndex;
            Coordinate = coordinate;
        }

        public string ElementId { get; }
        public GridReferenceCurveKind Kind { get; }
        public int GroupIndex { get; }
        public double Coordinate { get; }
    }

    public static class GridSpatialOrderingPlanner
    {
        private const int MaxCurves = 2000;
        private const int MaxElementIdLength = 128;
        private const double TwoPi = Math.PI * 2.0;

        public static IReadOnlyList<GridSpatialOrderingEntry> OrderParallelLines(
            IEnumerable<GridReferenceCurve> curves,
            Point2 orderingAxis,
            bool descending = false,
            double alignmentTolerance = 1e-6,
            double coordinateTolerance = 1e-8)
        {
            if (curves == null) throw new ArgumentNullException(nameof(curves));
            if (!Finite(alignmentTolerance) || alignmentTolerance <= 0.0 || alignmentTolerance >= 1.0)
                throw new ArgumentOutOfRangeException(nameof(alignmentTolerance), "Grid alignment tolerance must be finite and in (0, 1).");
            if (!Finite(coordinateTolerance) || coordinateTolerance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(coordinateTolerance), "Grid coordinate tolerance must be finite and positive.");

            if (!Finite(orderingAxis.X) || !Finite(orderingAxis.Y))
                throw new ArgumentException("Grid ordering axis must be finite and non-zero.", nameof(orderingAxis));
            var axisScale = Math.Max(Math.Abs(orderingAxis.X), Math.Abs(orderingAxis.Y));
            if (!(axisScale > 0.0))
                throw new ArgumentException("Grid ordering axis must be finite and non-zero.", nameof(orderingAxis));
            var scaledAxisX = orderingAxis.X / axisScale;
            var scaledAxisY = orderingAxis.Y / axisScale;
            var scaledAxisLength = Math.Sqrt(scaledAxisX * scaledAxisX + scaledAxisY * scaledAxisY);
            var ux = scaledAxisX / scaledAxisLength;
            var uy = scaledAxisY / scaledAxisLength;
            if (!Finite(ux) || !Finite(uy))
                throw new InvalidOperationException("Grid ordering axis normalization overflowed the supported numeric range.");

            var list = curves.Take(MaxCurves + 1).ToList();
            if (list.Count == 0) throw new InvalidOperationException("At least one Grid LINE is required for spatial ordering.");
            if (list.Count > MaxCurves)
                throw new InvalidOperationException("Grid spatial ordering supports at most " + MaxCurves + " curves.");

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var entries = new List<GridSpatialOrderingEntry>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                var curve = list[i] ?? throw new ArgumentException("Grid spatial ordering curve cannot be null at index " + i + ".", nameof(curves));
                var id = NormalizeElementId(curve.ElementId);
                if (!ids.Add(id))
                    throw new InvalidOperationException("Grid spatial ordering input contains duplicate element id: " + id + ".");
                if (curve.Kind != GridReferenceCurveKind.Line)
                    throw new InvalidOperationException("Grid spatial ordering currently supports parallel LINE references only. ARC/radial ordering requires an explicit reviewed policy.");

                EnsureFinitePoint(curve.Start, "Grid LINE start");
                EnsureFinitePoint(curve.End, "Grid LINE end");
                var dx = curve.End.X - curve.Start.X;
                var dy = curve.End.Y - curve.Start.Y;
                if (!Finite(dx) || !Finite(dy))
                    throw new OverflowException("Grid LINE direction exceeds the supported numeric range for element " + id + ".");
                var lineLength = Hypot(dx, dy);
                if (!(lineLength > coordinateTolerance))
                    throw new InvalidOperationException("Grid LINE is degenerate within the ordering tolerance for element " + id + ".");

                double lx;
                double ly;
                if (Finite(lineLength))
                {
                    lx = dx / lineLength;
                    ly = dy / lineLength;
                }
                else
                {
                    var lineScale = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    var scaledLineX = dx / lineScale;
                    var scaledLineY = dy / lineScale;
                    var scaledLineLength = Math.Sqrt(scaledLineX * scaledLineX + scaledLineY * scaledLineY);
                    lx = scaledLineX / scaledLineLength;
                    ly = scaledLineY / scaledLineLength;
                }
                var alignment = Math.Abs(lx * ux + ly * uy);
                if (!Finite(alignment))
                    throw new OverflowException("Grid LINE alignment exceeded the supported numeric range for element " + id + ".");
                if (alignment > alignmentTolerance)
                    throw new InvalidOperationException(
                        "Grid LINE " + id + " is not perpendicular to the explicit ordering axis within tolerance. " +
                        "Review the axis or split non-parallel Grid families before automatic ordering.");

                var startProjection = curve.Start.X * ux + curve.Start.Y * uy;
                var endProjection = curve.End.X * ux + curve.End.Y * uy;
                if (!Finite(startProjection) || !Finite(endProjection))
                    throw new OverflowException("Grid LINE projection exceeds the supported numeric range for element " + id + ".");
                var coordinate = 0.5 * startProjection + 0.5 * endProjection;
                if (!Finite(coordinate))
                    throw new OverflowException("Grid LINE ordering coordinate exceeds the supported numeric range for element " + id + ".");
                entries.Add(new GridSpatialOrderingEntry(id, coordinate));
            }

            entries.Sort((left, right) =>
            {
                var comparison = left.Coordinate.CompareTo(right.Coordinate);
                if (comparison == 0) return StringComparer.OrdinalIgnoreCase.Compare(left.ElementId, right.ElementId);
                return comparison;
            });

            for (var i = 1; i < entries.Count; i++)
            {
                var delta = entries[i].Coordinate - entries[i - 1].Coordinate;
                if (Finite(delta) && Math.Abs(delta) <= coordinateTolerance)
                    throw new InvalidOperationException(
                        "Grid spatial ordering is ambiguous because elements " + entries[i - 1].ElementId + " and " + entries[i].ElementId +
                        " project to the same ordering coordinate within tolerance. Review duplicate/overlapping Grid lines instead of relying on an arbitrary tie-break.");
            }

            if (descending) entries.Reverse();
            return entries.AsReadOnly();
        }

        public static IReadOnlyList<GridReviewedOrderingEntry> OrderReviewedSet(
            IEnumerable<GridReferenceCurve> curves,
            Point2 lineOrderingAxis,
            Point2 reviewedArcCenter,
            GridReviewedGroupPrecedence groupPrecedence,
            bool descendingLines = false,
            bool descendingArcs = false,
            double alignmentTolerance = 1e-6,
            double coordinateTolerance = 1e-8)
        {
            if (curves == null) throw new ArgumentNullException(nameof(curves));
            if (groupPrecedence != GridReviewedGroupPrecedence.LinesThenArcs &&
                groupPrecedence != GridReviewedGroupPrecedence.ArcsThenLines)
                throw new ArgumentOutOfRangeException(nameof(groupPrecedence), "Grid mixed ordering requires an explicit supported group precedence.");
            if (!Finite(coordinateTolerance) || coordinateTolerance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(coordinateTolerance), "Grid coordinate tolerance must be finite and positive.");
            EnsureFinitePoint(reviewedArcCenter, "Reviewed Grid ARC center");

            var list = curves.Take(MaxCurves + 1).ToList();
            if (list.Count == 0) throw new InvalidOperationException("At least one reviewed Grid reference is required for mixed ordering.");
            if (list.Count > MaxCurves)
                throw new InvalidOperationException("Grid reviewed ordering supports at most " + MaxCurves + " curves.");

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lines = new List<GridReferenceCurve>();
            var arcs = new List<GridReferenceCurve>();
            for (var i = 0; i < list.Count; i++)
            {
                var curve = list[i] ?? throw new ArgumentException("Grid reviewed ordering curve cannot be null at index " + i + ".", nameof(curves));
                var id = NormalizeElementId(curve.ElementId);
                if (!ids.Add(id))
                    throw new InvalidOperationException("Grid reviewed ordering input contains duplicate element id: " + id + ".");
                if (curve.Kind == GridReferenceCurveKind.Line)
                {
                    lines.Add(curve);
                    continue;
                }
                if (curve.Kind == GridReferenceCurveKind.Arc)
                {
                    ValidateReviewedArc(curve, reviewedArcCenter, coordinateTolerance);
                    arcs.Add(curve);
                    continue;
                }
                throw new InvalidOperationException("Unsupported Grid reference curve kind for reviewed ordering: " + id + ".");
            }

            var lineEntries = lines.Count == 0
                ? Array.Empty<GridSpatialOrderingEntry>()
                : OrderParallelLines(lines, lineOrderingAxis, descendingLines, alignmentTolerance, coordinateTolerance).ToArray();

            var arcEntries = new List<GridSpatialOrderingEntry>(arcs.Count);
            foreach (var arc in arcs)
                arcEntries.Add(new GridSpatialOrderingEntry(arc.ElementId, arc.Radius));
            arcEntries.Sort((left, right) =>
            {
                var comparison = left.Coordinate.CompareTo(right.Coordinate);
                if (comparison == 0) return StringComparer.OrdinalIgnoreCase.Compare(left.ElementId, right.ElementId);
                return comparison;
            });
            for (var i = 1; i < arcEntries.Count; i++)
            {
                var delta = arcEntries[i].Coordinate - arcEntries[i - 1].Coordinate;
                if (Finite(delta) && Math.Abs(delta) <= coordinateTolerance)
                    throw new InvalidOperationException(
                        "Grid reviewed ARC ordering is ambiguous because elements " + arcEntries[i - 1].ElementId + " and " + arcEntries[i].ElementId +
                        " have the same radius within tolerance. Review duplicate/concentric Grid arcs instead of relying on an arbitrary tie-break.");
            }
            if (descendingArcs) arcEntries.Reverse();

            var result = new List<GridReviewedOrderingEntry>(list.Count);
            if (groupPrecedence == GridReviewedGroupPrecedence.LinesThenArcs)
            {
                AppendReviewed(result, lineEntries, GridReferenceCurveKind.Line, 0);
                AppendReviewed(result, arcEntries, GridReferenceCurveKind.Arc, 1);
            }
            else
            {
                AppendReviewed(result, arcEntries, GridReferenceCurveKind.Arc, 0);
                AppendReviewed(result, lineEntries, GridReferenceCurveKind.Line, 1);
            }
            return result.AsReadOnly();
        }

        private static void AppendReviewed(
            List<GridReviewedOrderingEntry> target,
            IEnumerable<GridSpatialOrderingEntry> source,
            GridReferenceCurveKind kind,
            int groupIndex)
        {
            foreach (var entry in source)
                target.Add(new GridReviewedOrderingEntry(entry.ElementId, kind, groupIndex, entry.Coordinate));
        }

        private static void ValidateReviewedArc(GridReferenceCurve curve, Point2 reviewedArcCenter, double coordinateTolerance)
        {
            EnsureFinitePoint(curve.Center, "Grid ARC center");
            EnsureFinitePoint(curve.Start, "Grid ARC start");
            EnsureFinitePoint(curve.End, "Grid ARC end");
            if (!Finite(curve.Radius) || !Finite(curve.StartAngleRad) || !Finite(curve.SweepAngleRad))
                throw new InvalidOperationException("Grid ARC geometry must be finite for reviewed ordering: " + curve.ElementId + ".");
            if (!(curve.Radius > coordinateTolerance))
                throw new InvalidOperationException("Grid ARC radius is zero/near-zero within the ordering tolerance: " + curve.ElementId + ".");
            if (!(curve.SweepAngleRad > 0.0) || curve.SweepAngleRad > TwoPi + 1e-10)
                throw new InvalidOperationException("Grid ARC sweep must be in (0, 2π] for reviewed ordering: " + curve.ElementId + ".");

            var centerDx = curve.Center.X - reviewedArcCenter.X;
            var centerDy = curve.Center.Y - reviewedArcCenter.Y;
            if (!Finite(centerDx) || !Finite(centerDy))
                throw new OverflowException("Grid ARC center delta exceeds the supported numeric range: " + curve.ElementId + ".");
            var centerDistance = Hypot(centerDx, centerDy);
            if (!Finite(centerDistance) || centerDistance > coordinateTolerance)
                throw new InvalidOperationException(
                    "Grid ARC " + curve.ElementId + " does not share the explicit reviewed radial center within tolerance. " +
                    "Review the center or split unrelated ARC families before automatic ordering.");
        }

        private static string NormalizeElementId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Grid element id is required.", nameof(value));
            var normalized = value.Trim();
            if (normalized.Length > MaxElementIdLength)
                throw new ArgumentException("Grid element id exceeds " + MaxElementIdLength + " characters.", nameof(value));
            return normalized;
        }

        private static void EnsureFinitePoint(Point2 point, string name)
        {
            if (!Finite(point.X) || !Finite(point.Y))
                throw new InvalidOperationException(name + " must contain finite coordinates.");
        }

        private static double Hypot(double x, double y)
        {
            if (!Finite(x) || !Finite(y)) return double.NaN;
            var ax = Math.Abs(x);
            var ay = Math.Abs(y);
            var scale = Math.Max(ax, ay);
            if (scale == 0.0) return 0.0;
            var ratio = Math.Min(ax, ay) / scale;
            var value = scale * Math.Sqrt(1.0 + ratio * ratio);
            return Finite(value) ? value : double.PositiveInfinity;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}