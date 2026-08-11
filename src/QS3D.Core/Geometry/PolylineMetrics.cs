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
            for (var i = 1; i < points.Count; i++) total = AddFinite(total, points[i - 1].DistanceTo(points[i]));
            if (closed) total = AddFinite(total, points[points.Count - 1].DistanceTo(points[0]));
            return total;
        }

        public static double SignedArea(IReadOnlyList<Point2> points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < 3) return 0d;

            var origin = points[0];
            EnsureFinite(origin);
            double sum = 0d;
            double compensation = 0d;

            for (var i = 1; i < points.Count - 1; i++)
            {
                EnsureFinite(points[i]);
                EnsureFinite(points[i + 1]);
                var ax = SubtractFinite(points[i].X, origin.X);
                var ay = SubtractFinite(points[i].Y, origin.Y);
                var bx = SubtractFinite(points[i + 1].X, origin.X);
                var by = SubtractFinite(points[i + 1].Y, origin.Y);
                var cross = SubtractFinite(MultiplyFinite(ax, by), MultiplyFinite(ay, bx));

                var corrected = cross - compensation;
                var next = sum + corrected;
                if (!Finite(next)) throw new OverflowException("Polyline area exceeds the supported numeric range.");
                compensation = (next - sum) - corrected;
                sum = next;
            }

            var area = sum * 0.5d;
            if (!Finite(area)) throw new OverflowException("Polyline area exceeds the supported numeric range.");
            return area;
        }

        public static double Area(IReadOnlyList<Point2> points) => Math.Abs(SignedArea(points));

        private static void EnsureFinite(Point2 point)
        {
            if (!Finite(point.X) || !Finite(point.Y))
                throw new InvalidOperationException("Polyline coordinates must be finite.");
        }

        private static double AddFinite(double first, double second)
        {
            var value = first + second;
            if (!Finite(value)) throw new OverflowException("Polyline metric exceeds the supported numeric range.");
            return value;
        }

        private static double SubtractFinite(double first, double second)
        {
            var value = first - second;
            if (!Finite(value)) throw new OverflowException("Polyline coordinate delta exceeds the supported numeric range.");
            return value;
        }

        private static double MultiplyFinite(double first, double second)
        {
            var value = first * second;
            if (!Finite(value)) throw new OverflowException("Polyline area exceeds the supported numeric range.");
            return value;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
