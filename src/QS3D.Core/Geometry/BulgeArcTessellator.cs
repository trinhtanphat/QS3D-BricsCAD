using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public static class BulgeArcTessellator
    {
        private const int MaxSegments = 4096;
        private const double StraightBulgeTolerance = 1e-12d;
        private const double MaximumSegmentAngle = Math.PI / 18d;

        public static IReadOnlyList<Point2> Tessellate(Point2 start, Point2 end, double bulge, double maximumSagitta = 0.002d)
        {
            ValidatePoint(start, nameof(start));
            ValidatePoint(end, nameof(end));
            if (double.IsNaN(bulge) || double.IsInfinity(bulge)) throw new ArgumentOutOfRangeException(nameof(bulge));
            if (double.IsNaN(maximumSagitta) || double.IsInfinity(maximumSagitta) || maximumSagitta <= 0d) throw new ArgumentOutOfRangeException(nameof(maximumSagitta));

            var chord = start.DistanceTo(end);
            if (double.IsNaN(chord) || double.IsInfinity(chord) || chord <= 1e-12d) throw new ArgumentException("Arc chord must be finite and non-degenerate.");
            if (Math.Abs(bulge) <= StraightBulgeTolerance) return Array.AsReadOnly(new[] { start, end });

            var theta = 4d * Math.Atan(bulge);
            var absTheta = Math.Abs(theta);
            if (!(absTheta > 1e-12d) || absTheta > Math.PI * 2d) throw new ArgumentOutOfRangeException(nameof(bulge), "Polyline bulge produced an invalid included angle.");

            var absBulge = Math.Abs(bulge);
            var inverseAbsBulge = 1d / absBulge;
            var radius = chord * 0.25d * (absBulge + inverseAbsBulge);
            if (double.IsNaN(radius) || double.IsInfinity(radius) || radius <= 0d) throw new OverflowException("Arc radius is not finite.");

            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var midpoint = new Point2(start.X + dx * 0.5d, start.Y + dy * 0.5d);
            ValidatePoint(midpoint, "arcMidpoint");
            if ((midpoint.X == start.X && midpoint.Y == start.Y) ||
                (midpoint.X == end.X && midpoint.Y == end.Y))
                throw new InvalidOperationException("Arc midpoint is not representable between the distinct chord endpoints.");
            var nx = -dy / chord;
            var ny = dx / chord;
            var centerOffset = chord * 0.25d * (1d / bulge - bulge);
            if (double.IsNaN(centerOffset) || double.IsInfinity(centerOffset)) throw new OverflowException("Arc center offset is not finite.");

            var centerDx = nx * centerOffset;
            var centerDy = ny * centerOffset;
            if (centerOffset != 0d &&
                ((nx != 0d && centerDx == 0d) || (ny != 0d && centerDy == 0d)))
                throw new InvalidOperationException("Arc center displacement is below numeric resolution.");

            var centerX = midpoint.X + centerDx;
            var centerY = midpoint.Y + centerDy;
            if ((centerDx != 0d && centerX == midpoint.X) ||
                (centerDy != 0d && centerY == midpoint.Y))
                throw new InvalidOperationException("Arc center displacement is not representable at the supplied coordinates.");
            var center = new Point2(centerX, centerY);
            ValidatePoint(center, "arcCenter");

            var sagittaAngle = MaximumSegmentAngle;
            var sagittaRatio = maximumSagitta / radius;
            if (sagittaRatio < 2d)
            {
                var quarterSineSquared = sagittaRatio * 0.5d;
                if (!(quarterSineSquared > 0d)) throw new InvalidOperationException("Arc sagitta tolerance is below numeric resolution for this radius.");
                var quarterSine = Math.Sqrt(Math.Min(1d, quarterSineSquared));
                var bySagitta = 4d * Math.Asin(quarterSine);
                if (!(bySagitta > 0d) || double.IsNaN(bySagitta) || double.IsInfinity(bySagitta)) throw new InvalidOperationException("Arc sagitta angle is invalid.");
                sagittaAngle = Math.Min(sagittaAngle, bySagitta);
            }
            if (!(sagittaAngle > 0d) || double.IsNaN(sagittaAngle) || double.IsInfinity(sagittaAngle)) throw new InvalidOperationException("Arc tessellation angle is invalid.");

            var requiredSegments = Math.Ceiling(absTheta / sagittaAngle);
            if (double.IsNaN(requiredSegments) || double.IsInfinity(requiredSegments) || requiredSegments > MaxSegments)
                throw new InvalidOperationException("Arc tessellation exceeds the supported segment limit.");
            var segmentCount = Math.Max(1, (int)requiredSegments);

            var startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
            var points = new List<Point2>(segmentCount + 1) { start };
            for (var index = 1; index < segmentCount; index++)
            {
                var angle = startAngle + theta * index / segmentCount;
                var point = new Point2(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
                ValidatePoint(point, "tessellatedPoint");
                AddNonDegenerateVertexOrThrow(points, point);
            }
            AddNonDegenerateVertexOrThrow(points, end);
            return points.AsReadOnly();
        }

        private static void AddNonDegenerateVertexOrThrow(List<Point2> points, Point2 point)
        {
            var previous = points[points.Count - 1];
            if (previous.X == point.X && previous.Y == point.Y)
                throw new InvalidOperationException("Bulge arc tessellation collapsed adjacent vertices at the current numeric precision.");
            points.Add(point);
        }

        private static void ValidatePoint(Point2 point, string name)
        {
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                throw new ArgumentOutOfRangeException(name, "Point coordinates must be finite.");
        }
    }
}