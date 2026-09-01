using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class GridLineSnapResult
    {
        public GridLineSnapResult(string gridElementId, Point2 snapPoint, double distance)
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
    /// CAD-independent nearest snap policy for finite straight Grid segments.
    /// The planner never mutates semantic/native state and intentionally rejects ARC input
    /// until a separately reviewed radial snap policy exists.
    /// </summary>
    public static class GridLineSnapPlanner
    {
        private const int MaxCurves = 2000;

        public static bool TryFindNearest(
            Point2 candidate,
            IEnumerable<GridReferenceCurve> curves,
            double maxDistance,
            out GridLineSnapResult? result,
            double ambiguityTolerance = 1e-8,
            double geometryTolerance = 1e-10)
        {
            result = null;
            if (curves == null) throw new ArgumentNullException(nameof(curves));
            EnsureFinitePoint(candidate, "Grid snap candidate");
            if (!Finite(maxDistance) || maxDistance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(maxDistance), "Grid snap max distance must be finite and positive.");
            if (!Finite(ambiguityTolerance) || ambiguityTolerance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(ambiguityTolerance), "Grid snap ambiguity tolerance must be finite and positive.");
            if (!Finite(geometryTolerance) || geometryTolerance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(geometryTolerance), "Grid snap geometry tolerance must be finite and positive.");

            var list = GridSnapInputMaterializer.Materialize(curves, MaxCurves, "Grid line snap input");
            if (list.Count == 0) return false;

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var candidates = new List<GridLineSnapResult>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                var curve = list[i] ?? throw new ArgumentException(
                    "Grid line snap curve cannot be null at index " + i + ".", nameof(curves));
                if (!ids.Add(curve.ElementId))
                    throw new InvalidOperationException("Grid line snap input contains duplicate element id: " + curve.ElementId + ".");
                if (curve.Kind != GridReferenceCurveKind.Line)
                    throw new InvalidOperationException(
                        "Grid line snap accepts LINE references only. ARC snapping requires a separately reviewed radial policy.");

                EnsureFinitePoint(curve.Start, "Grid LINE start");
                EnsureFinitePoint(curve.End, "Grid LINE end");
                var nearest = NearestOnFiniteSegment(candidate, curve, geometryTolerance);
                candidates.Add(new GridLineSnapResult(curve.ElementId, nearest.Point, nearest.Distance));
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
                var second = candidates[1];
                var delta = second.Distance - first.Distance;
                if (!Finite(delta))
                    throw new OverflowException("Grid snap distance comparison exceeded the supported numeric range.");
                if (Math.Abs(delta) <= ambiguityTolerance)
                    throw new InvalidOperationException(
                        "Grid snap is ambiguous because " + first.GridElementId + " and " + second.GridElementId +
                        " are equally/near-equally close within tolerance. Review the intended host Grid explicitly.");
            }

            result = first;
            return true;
        }

        private static SegmentProjection NearestOnFiniteSegment(
            Point2 candidate,
            GridReferenceCurve line,
            double geometryTolerance)
        {
            var dx = line.End.X - line.Start.X;
            var dy = line.End.Y - line.Start.Y;
            if (!Finite(dx) || !Finite(dy))
                throw new OverflowException("Grid LINE direction exceeds the supported numeric range for " + line.ElementId + ".");
            var length = Hypot(dx, dy);
            if (!Finite(length) || !(length > geometryTolerance))
                throw new InvalidOperationException("Grid LINE is degenerate within snap tolerance for " + line.ElementId + ".");

            var ux = dx / length;
            var uy = dy / length;
            if (!Finite(ux) || !Finite(uy))
                throw new OverflowException("Grid LINE unit direction is outside the supported numeric range for " + line.ElementId + ".");

            var px = candidate.X - line.Start.X;
            var py = candidate.Y - line.Start.Y;
            if (!Finite(px) || !Finite(py))
                throw new OverflowException("Grid snap candidate offset exceeds the supported numeric range for " + line.ElementId + ".");
            var along = px * ux + py * uy;
            if (!Finite(along))
                throw new OverflowException("Grid snap projection exceeds the supported numeric range for " + line.ElementId + ".");
            if (along < 0.0) along = 0.0;
            else if (along > length) along = length;

            var point = new Point2(line.Start.X + ux * along, line.Start.Y + uy * along);
            EnsureFinitePoint(point, "Grid snap result");
            var rx = candidate.X - point.X;
            var ry = candidate.Y - point.Y;
            if (!Finite(rx) || !Finite(ry))
                throw new OverflowException("Grid snap residual exceeds the supported numeric range for " + line.ElementId + ".");
            var distance = Hypot(rx, ry);
            if (!Finite(distance))
                throw new OverflowException("Grid snap distance exceeds the supported numeric range for " + line.ElementId + ".");
            return new SegmentProjection(point, distance);
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

        private sealed class SegmentProjection
        {
            public SegmentProjection(Point2 point, double distance)
            {
                Point = point;
                Distance = distance;
            }

            public Point2 Point { get; }
            public double Distance { get; }
        }
    }
}
