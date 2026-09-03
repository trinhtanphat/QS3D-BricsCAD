using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class GridArcSnapResult
    {
        public GridArcSnapResult(string gridElementId, Point2 snapPoint, double distance)
        {
            GridElementId = gridElementId ?? throw new ArgumentNullException(nameof(gridElementId));
            SnapPoint = snapPoint;
            Distance = distance;
        }

        public string GridElementId { get; }
        public Point2 SnapPoint { get; }
        public double Distance { get; }
    }

    /// <summary>
    /// CAD-independent nearest-point policy for bounded Grid ARC references.
    /// It snaps to the reviewed finite sweep, never to the infinite support circle.
    /// </summary>
    public static class GridArcSnapPlanner
    {
        private const int MaxCurves = 2000;
        private const double TwoPi = Math.PI * 2.0;

        public static bool TryFindNearest(
            Point2 candidate,
            IEnumerable<GridReferenceCurve> curves,
            double maxDistance,
            out GridArcSnapResult? result,
            double ambiguityTolerance = 1e-8,
            double geometryTolerance = 1e-10,
            double angleTolerance = 1e-10)
        {
            result = null;
            if (curves == null) throw new ArgumentNullException(nameof(curves));
            EnsureFinitePoint(candidate, "Grid ARC snap candidate");
            RequirePositiveFinite(maxDistance, nameof(maxDistance));
            RequirePositiveFinite(ambiguityTolerance, nameof(ambiguityTolerance));
            RequirePositiveFinite(geometryTolerance, nameof(geometryTolerance));
            RequirePositiveFinite(angleTolerance, nameof(angleTolerance));

            var list = GridSnapInputMaterializer.Materialize(curves, MaxCurves, "Grid ARC snap input");
            if (list.Count == 0) return false;

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var candidates = new List<GridArcSnapResult>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                var arc = list[i] ?? throw new ArgumentException(
                    "Grid ARC snap curve cannot be null at index " + i + ".", nameof(curves));
                if (!ids.Add(arc.ElementId))
                    throw new InvalidOperationException("Grid ARC snap input contains duplicate element id: " + arc.ElementId + ".");
                if (arc.Kind != GridReferenceCurveKind.Arc)
                    throw new InvalidOperationException(
                        "Grid ARC snap accepts ARC references only. LINE snapping uses the separately reviewed LINE policy.");

                ValidateArc(arc, geometryTolerance, angleTolerance);
                var nearest = NearestOnFiniteArc(candidate, arc, geometryTolerance, angleTolerance);
                candidates.Add(new GridArcSnapResult(arc.ElementId, nearest.Point, nearest.Distance));
            }

            candidates.Sort((left, right) =>
            {
                var comparison = left.Distance.CompareTo(right.Distance);
                if (comparison != 0) return comparison;
                return StringComparer.OrdinalIgnoreCase.Compare(left.GridElementId, right.GridElementId);
            });

            var first = candidates[0];
            if (first.Distance > maxDistance) return false;

            if (candidates.Count > 1)
            {
                var delta = candidates[1].Distance - first.Distance;
                if (!Finite(delta))
                    throw new OverflowException("Grid ARC snap distance comparison exceeded the supported numeric range.");
                if (Math.Abs(delta) <= ambiguityTolerance)
                    throw new InvalidOperationException(
                        "Grid ARC snap is ambiguous because " + first.GridElementId + " and " + candidates[1].GridElementId +
                        " are equally/near-equally close within tolerance. Review the intended Grid explicitly.");
            }

            result = first;
            return true;
        }

        private static void ValidateArc(GridReferenceCurve arc, double geometryTolerance, double angleTolerance)
        {
            EnsureFinitePoint(arc.Center, "Grid ARC center");
            EnsureFinitePoint(arc.Start, "Grid ARC start");
            EnsureFinitePoint(arc.End, "Grid ARC end");
            if (!Finite(arc.Radius) || !(arc.Radius > geometryTolerance))
                throw new InvalidOperationException("Grid ARC radius must be finite and positive for " + arc.ElementId + ".");
            if (!Finite(arc.StartAngleRad) || !Finite(arc.SweepAngleRad))
                throw new InvalidOperationException("Grid ARC angles must be finite for " + arc.ElementId + ".");

            var sweep = Math.Abs(arc.SweepAngleRad);
            if (!(sweep > angleTolerance))
                throw new InvalidOperationException("Grid ARC sweep is degenerate for " + arc.ElementId + ".");
            if (sweep >= TwoPi - angleTolerance)
                throw new InvalidOperationException(
                    "Grid ARC snap requires a bounded open sweep smaller than 2π. Full/over-sweep input is ambiguous for " + arc.ElementId + ".");

            var startRadius = Distance(arc.Center, arc.Start);
            var endRadius = Distance(arc.Center, arc.End);
            if (Math.Abs(startRadius - arc.Radius) > geometryTolerance ||
                Math.Abs(endRadius - arc.Radius) > geometryTolerance)
                throw new InvalidOperationException("Grid ARC endpoint/radius geometry is inconsistent for " + arc.ElementId + ".");
        }

        private static ArcProjection NearestOnFiniteArc(
            Point2 candidate,
            GridReferenceCurve arc,
            double geometryTolerance,
            double angleTolerance)
        {
            var dx = candidate.X - arc.Center.X;
            var dy = candidate.Y - arc.Center.Y;
            if (!Finite(dx) || !Finite(dy))
                throw new OverflowException("Grid ARC snap candidate offset exceeds the supported numeric range for " + arc.ElementId + ".");
            var radialDistance = Hypot(dx, dy);
            if (!Finite(radialDistance))
                throw new OverflowException("Grid ARC snap radial distance exceeds the supported numeric range for " + arc.ElementId + ".");
            if (!(radialDistance > geometryTolerance))
                throw new InvalidOperationException(
                    "Grid ARC snap is ambiguous because the candidate lies at/near the ARC center for " + arc.ElementId + ".");

            var candidateAngle = Math.Atan2(dy, dx);
            if (!Finite(candidateAngle))
                throw new OverflowException("Grid ARC snap angle is outside the supported numeric range for " + arc.ElementId + ".");

            if (AngleWithinSweep(candidateAngle, arc.StartAngleRad, arc.SweepAngleRad, angleTolerance))
            {
                var scale = arc.Radius / radialDistance;
                if (!Finite(scale))
                    throw new OverflowException("Grid ARC snap radial scale is outside the supported numeric range for " + arc.ElementId + ".");
                var point = new Point2(arc.Center.X + dx * scale, arc.Center.Y + dy * scale);
                EnsureFinitePoint(point, "Grid ARC radial snap result");
                return new ArcProjection(point, Distance(candidate, point));
            }

            var startDistance = Distance(candidate, arc.Start);
            var endDistance = Distance(candidate, arc.End);
            var endpointDelta = Math.Abs(startDistance - endDistance);
            if (!Finite(endpointDelta))
                throw new OverflowException("Grid ARC endpoint comparison exceeded the supported numeric range for " + arc.ElementId + ".");
            if (endpointDelta <= geometryTolerance)
                throw new InvalidOperationException(
                    "Grid ARC snap is ambiguous because both finite ARC endpoints are equally/near-equally close for " + arc.ElementId + ".");

            return startDistance < endDistance
                ? new ArcProjection(arc.Start, startDistance)
                : new ArcProjection(arc.End, endDistance);
        }

        private static bool AngleWithinSweep(double angle, double start, double sweep, double tolerance)
        {
            if (sweep > 0.0)
            {
                var delta = NormalizePositive(angle - start);
                return delta <= sweep + tolerance;
            }

            var clockwiseDelta = NormalizePositive(start - angle);
            return clockwiseDelta <= -sweep + tolerance;
        }

        private static double NormalizePositive(double angle)
        {
            if (!Finite(angle)) throw new OverflowException("Grid ARC angle normalization received a non-finite value.");
            var normalized = angle % TwoPi;
            if (normalized < 0.0) normalized += TwoPi;
            return normalized;
        }

        private static double Distance(Point2 first, Point2 second)
        {
            var dx = first.X - second.X;
            var dy = first.Y - second.Y;
            if (!Finite(dx) || !Finite(dy))
                throw new OverflowException("Grid ARC snap distance delta exceeds the supported numeric range.");
            var distance = Hypot(dx, dy);
            if (!Finite(distance))
                throw new OverflowException("Grid ARC snap distance exceeds the supported numeric range.");
            return distance;
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

        private static void EnsureFinitePoint(Point2 point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y))
                throw new InvalidOperationException(label + " must contain finite coordinates.");
        }

        private static void RequirePositiveFinite(double value, string parameterName)
        {
            if (!Finite(value) || value <= 0.0)
                throw new ArgumentOutOfRangeException(parameterName, "Grid ARC snap tolerance/range must be finite and positive.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private sealed class ArcProjection
        {
            public ArcProjection(Point2 point, double distance)
            {
                Point = point;
                Distance = distance;
            }

            public Point2 Point { get; }
            public double Distance { get; }
        }
    }
}
