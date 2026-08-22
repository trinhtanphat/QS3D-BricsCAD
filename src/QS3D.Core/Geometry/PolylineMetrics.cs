using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public static class PolylineMetrics
    {
        public static double Length(IReadOnlyList<Point2> points, bool closed)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < 2) return 0d;
            double total = 0d;
            for (var i = 1; i < points.Count; i++) total += points[i - 1].DistanceTo(points[i]);
            if (closed && points.Count > 2) total += points[points.Count - 1].DistanceTo(points[0]);
            return total;
        }
        public static double SignedArea(IReadOnlyList<Point2> points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < 3) return 0d;
            double sum = 0d;
            for (var i = 0; i < points.Count; i++) { var a = points[i]; var b = points[(i + 1) % points.Count]; sum += a.X * b.Y - b.X * a.Y; }
            return sum / 2d;
        }
        public static double Area(IReadOnlyList<Point2> points) => Math.Abs(SignedArea(points));
    }
}
