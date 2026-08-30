using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class GridRadialOrderingEntry
    {
        public GridRadialOrderingEntry(string elementId, double radius)
        {
            ElementId = elementId ?? throw new ArgumentNullException(nameof(elementId));
            Radius = radius;
        }

        public string ElementId { get; }
        public double Radius { get; }
    }

    /// <summary>
    /// Deterministic ordering for one reviewed family of concentric Grid ARC references.
    /// This intentionally does not infer mixed LINE/ARC families or silently choose a center.
    /// </summary>
    public static class GridRadialOrderingPlanner
    {
        private const int MaxCurves = 2000;
        private const double AngleTolerance = 1e-12;
        private const double TwoPi = Math.PI * 2.0;

        public static IReadOnlyList<GridRadialOrderingEntry> OrderConcentricArcs(
            IEnumerable<GridReferenceCurve> curves,
            bool descending = false,
            double centerTolerance = 1e-8,
            double radiusTolerance = 1e-8)
        {
            if (curves == null) throw new ArgumentNullException(nameof(curves));
            if (!Finite(centerTolerance) || centerTolerance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(centerTolerance), "Grid radial center tolerance must be finite and positive.");
            if (!Finite(radiusTolerance) || radiusTolerance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(radiusTolerance), "Grid radial radius tolerance must be finite and positive.");

            var list = GridSnapInputMaterializer.Materialize(curves, MaxCurves, "Grid radial ordering input");
            if (list.Count == 0)
                throw new InvalidOperationException("At least one Grid ARC is required for radial ordering.");

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var entries = new List<GridRadialOrderingEntry>(list.Count);
            Point2? reviewedCenter = null;

            for (var i = 0; i < list.Count; i++)
            {
                var curve = list[i] ?? throw new ArgumentException(
                    "Grid radial ordering curve cannot be null at index " + i + ".", nameof(curves));
                if (!ids.Add(curve.ElementId))
                    throw new InvalidOperationException("Grid radial ordering input contains duplicate element id: " + curve.ElementId + ".");
                if (curve.Kind != GridReferenceCurveKind.Arc)
                    throw new InvalidOperationException(
                        "Grid radial ordering accepts ARC references only. Split LINE and ARC Grid families before automatic ordering.");

                EnsureFinitePoint(curve.Center, "Grid ARC center");
                EnsureFinitePoint(curve.Start, "Grid ARC start");
                EnsureFinitePoint(curve.End, "Grid ARC end");
                if (!Finite(curve.Radius) || !(curve.Radius > radiusTolerance))
                    throw new InvalidOperationException(
                        "Grid ARC radius must be finite and greater than the radial tolerance for element " + curve.ElementId + ".");
                if (!Finite(curve.StartAngleRad) || !Finite(curve.SweepAngleRad))
                    throw new InvalidOperationException("Grid ARC angles must be finite for element " + curve.ElementId + ".");
                var sweep = Math.Abs(curve.SweepAngleRad);
                if (!(sweep > AngleTolerance) || sweep > TwoPi + AngleTolerance)
                    throw new InvalidOperationException(
                        "Grid ARC sweep must be within (0, 2π] for deterministic radial ordering: " + curve.ElementId + ".");

                if (!reviewedCenter.HasValue)
                {
                    reviewedCenter = curve.Center;
                }
                else
                {
                    var dx = curve.Center.X - reviewedCenter.Value.X;
                    var dy = curve.Center.Y - reviewedCenter.Value.Y;
                    if (!Finite(dx) || !Finite(dy))
                        throw new OverflowException("Grid ARC center delta exceeds the supported numeric range for " + curve.ElementId + ".");
                    var distance = Hypot(dx, dy);
                    if (!Finite(distance))
                        throw new OverflowException("Grid ARC center distance exceeds the supported numeric range for " + curve.ElementId + ".");
                    if (distance > centerTolerance)
                        throw new InvalidOperationException(
                            "Grid radial ordering requires one concentric ARC family. Element " + curve.ElementId +
                            " uses a different center beyond tolerance; split/review the Grid families explicitly.");
                }

                entries.Add(new GridRadialOrderingEntry(curve.ElementId, curve.Radius));
            }

            entries.Sort((left, right) =>
            {
                var comparison = left.Radius.CompareTo(right.Radius);
                if (comparison != 0) return comparison;
                return StringComparer.OrdinalIgnoreCase.Compare(left.ElementId, right.ElementId);
            });

            for (var i = 1; i < entries.Count; i++)
            {
                var delta = entries[i].Radius - entries[i - 1].Radius;
                if (Finite(delta) && Math.Abs(delta) <= radiusTolerance)
                    throw new InvalidOperationException(
                        "Grid radial ordering is ambiguous because elements " + entries[i - 1].ElementId + " and " +
                        entries[i].ElementId + " have equal/near-equal radii within tolerance. Review duplicate radial Grid references.");
            }

            if (descending) entries.Reverse();
            return entries.AsReadOnly();
        }

        private static void EnsureFinitePoint(Point2 point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y))
                throw new InvalidOperationException(label + " must contain finite coordinates.");
        }

        private static double Hypot(double x, double y)
        {
            var ax = Math.Abs(x);
            var ay = Math.Abs(y);
            var scale = Math.Max(ax, ay);
            if (scale == 0.0) return 0.0;
            if (!Finite(scale)) return double.PositiveInfinity;
            var ratio = Math.Min(ax, ay) / scale;
            var value = scale * Math.Sqrt(1.0 + ratio * ratio);
            return Finite(value) ? value : double.PositiveInfinity;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
