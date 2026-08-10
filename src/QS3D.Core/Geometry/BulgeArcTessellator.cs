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
            if (Math.Abs(bulge) <= StraightBulgeTolerance) return new[] { start, end };

            var theta = 4d * Math.Atan(bulge);
            var absTheta = Math.Abs(theta);
            if (!(absTheta > 1e-12d) || absTheta >= Math.PI * 2d) throw new ArgumentOutOfRangeException(nameof(bulge), "Polyline bulge produced an invalid included angle.");

            var absBulge = Math.Abs(bulge);
            var radius = chord * (1d + absBulge * absBulge) / (4d * absBulge);
            if (double.IsNaN(radius) || double.IsInfinity(radius) || radius <= 0d) throw new OverflowException("Arc radius is not finite.");

            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var midpoint = new Point2((start.X + end.X) * 0.5d, (start.Y + end.Y) * 0.5d);
            var nx = -dy / chord;
            var ny = dx / chord;
            var centerOffset = chord * (1d - bulge * bulge) / (4d * bulge);
            if (double.IsNaN(centerOffset) || double.IsInfinity(centerOffset)) throw new OverflowException("Arc center offset is not finite.");
            var center = new Point2(midpoint.X + nx * centerOffset, midpoint.Y + ny * centerOffset);

            var sagittaAngle = MaximumSegmentAngle;
            if (maximumSagitta < radius * 2d)
            {
                var cosine = 1d - maximumSagitta / radius;
                cosine = Math.Max(-1d, Math.Min(1d, cosine));
                var bySagitta = 2d * Math.Acos(cosine);
                if (bySagitta > 1e-12d) sagittaAngle = Math.Min(sagittaAngle, bySagitta);
            }
            if (!(sagittaAngle > 0d) || double.IsNaN(sagittaAngle) || double.IsInfinity(sagittaAngle)) throw new InvalidOperationException("Arc tessellation angle is invalid.");

            var segmentCount = checked((int)Math.Ceiling(absTheta / sagittaAngle));
            segmentCount = Math.Max(1, segmentCount);
            if (segmentCount > MaxSegments) throw new InvalidOperationException("Arc tessellation exceeds the supported segment limit.");

            var startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
            var points = new List<Point2>(segmentCount + 1) { start };
            for (var index = 1; index < segmentCount; index++)
            {
                var angle = startAngle + theta * index / segmentCount;
                var point = new Point2(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
                ValidatePoint(point, "tessellatedPoint");
                points.Add(point);
            }
            points.Add(end);
            return points.AsReadOnly();
        }

        private static void ValidatePoint(Point2 point, string name)
        {
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                throw new ArgumentOutOfRangeException(name, "Point coordinates must be finite.");
        }
    }
}
