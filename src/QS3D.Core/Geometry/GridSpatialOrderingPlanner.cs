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

    public static class GridSpatialOrderingPlanner
    {
        private const int MaxCurves = 2000;
        private const int MaxElementIdLength = 128;

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

            var axisLength = Hypot(orderingAxis.X, orderingAxis.Y);
            if (!(axisLength > 0.0) || !Finite(axisLength))
                throw new ArgumentException("Grid ordering axis must be finite and non-zero.", nameof(orderingAxis));
            var ux = orderingAxis.X / axisLength;
            var uy = orderingAxis.Y / axisLength;
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
                if (!(lineLength > coordinateTolerance) || !Finite(lineLength))
                    throw new InvalidOperationException("Grid LINE is degenerate within the ordering tolerance for element " + id + ".");

                var lx = dx / lineLength;
                var ly = dy / lineLength;
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
                if (!Finite(delta))
                    throw new OverflowException("Grid ordering coordinate delta exceeds the supported numeric range.");
                if (Math.Abs(delta) <= coordinateTolerance)
                    throw new InvalidOperationException(
                        "Grid spatial ordering is ambiguous because elements " + entries[i - 1].ElementId + " and " + entries[i].ElementId +
                        " project to the same ordering coordinate within tolerance. Review duplicate/overlapping Grid lines instead of relying on an arbitrary tie-break.");
            }

            if (descending) entries.Reverse();
            return entries.AsReadOnly();
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
