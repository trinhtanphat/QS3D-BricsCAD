using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public static class PolylineMetrics
    {
        public static double Length(IReadOnlyList<Point2> points, bool closed)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < 2)
            {
                EnsureFinite(points);
                return 0d;
            }

            double total = 0d;
            for (var i = 1; i < points.Count; i++) total = AddFinite(total, points[i - 1].DistanceTo(points[i]));
            if (closed) total = AddFinite(total, points[points.Count - 1].DistanceTo(points[0]));
            return total;
        }

        public static double SignedArea(IReadOnlyList<Point2> points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < 3)
            {
                EnsureFinite(points);
                return 0d;
            }

            var origin = points[0];
            EnsureFinite(origin);
            double sum = 0d;
            double compensation = 0d;

            for (var i = 1; i < points.Count - 1; i++)
            {
                EnsureFinite(points[i]);
                EnsureFinite(points[i + 1]);
                var cross = TranslatedCrossFinite(origin, points[i], points[i + 1]);

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

        private static void EnsureFinite(IReadOnlyList<Point2> points)
        {
            for (var i = 0; i < points.Count; i++) EnsureFinite(points[i]);
        }

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

        private static double TranslatedCrossFinite(Point2 origin, Point2 first, Point2 second)
        {
            var ax = first.X - origin.X;
            var ay = first.Y - origin.Y;
            var bx = second.X - origin.X;
            var by = second.Y - origin.Y;
            if (Finite(ax) && Finite(ay) && Finite(bx) && Finite(by))
                return CrossFinite(ax, ay, bx, by);

            var scaleX = Math.Max(Math.Abs(origin.X), Math.Max(Math.Abs(first.X), Math.Abs(second.X)));
            var scaleY = Math.Max(Math.Abs(origin.Y), Math.Max(Math.Abs(first.Y), Math.Abs(second.Y)));
            if (!Finite(scaleX) || !Finite(scaleY)) throw new OverflowException("Polyline area input exceeds the supported numeric range.");
            if (scaleX == 0d || scaleY == 0d) return 0d;

            ax = first.X / scaleX - origin.X / scaleX;
            bx = second.X / scaleX - origin.X / scaleX;
            ay = first.Y / scaleY - origin.Y / scaleY;
            by = second.Y / scaleY - origin.Y / scaleY;
            var normalized = ax * by - ay * bx;
            if (!Finite(normalized)) throw new OverflowException("Polyline area exceeds the supported numeric range.");
            return RestoreScaledCrossFinite(normalized, scaleX, scaleY);
        }

        private static double RestoreScaledCrossFinite(double normalized, double scaleX, double scaleY)
        {
            if (normalized == 0d) return 0d;

            var smaller = Math.Min(scaleX, scaleY);
            var larger = Math.Max(scaleX, scaleY);
            var firstScale = Math.Abs(normalized) <= 1d ? larger : smaller;
            var secondScale = Math.Abs(normalized) <= 1d ? smaller : larger;
            var scaled = normalized * firstScale;
            if (!Finite(scaled)) throw new OverflowException("Polyline area exceeds the supported numeric range.");
            var value = scaled * secondScale;
            if (!Finite(value)) throw new OverflowException("Polyline area exceeds the supported numeric range.");
            return value;
        }

        private static double CrossFinite(double ax, double ay, double bx, double by)
        {
            var firstProduct = ax * by;
            var secondProduct = ay * bx;
            if (Finite(firstProduct) && Finite(secondProduct))
            {
                var direct = firstProduct - secondProduct;
                if (Finite(direct)) return direct;
            }

            var scaleA = Math.Max(Math.Abs(ax), Math.Abs(ay));
            var scaleB = Math.Max(Math.Abs(bx), Math.Abs(by));
            if (!Finite(scaleA) || !Finite(scaleB)) throw new OverflowException("Polyline area input exceeds the supported numeric range.");
            if (scaleA == 0d || scaleB == 0d) return 0d;

            var normalized = ax / scaleA * (by / scaleB) - ay / scaleA * (bx / scaleB);
            if (!Finite(normalized)) throw new OverflowException("Polyline area exceeds the supported numeric range.");

            var firstScale = Math.Min(scaleA, scaleB);
            var secondScale = Math.Max(scaleA, scaleB);
            var scaled = normalized * firstScale;
            if (!Finite(scaled)) throw new OverflowException("Polyline area exceeds the supported numeric range.");
            var value = scaled * secondScale;
            if (!Finite(value)) throw new OverflowException("Polyline area exceeds the supported numeric range.");
            return value;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
