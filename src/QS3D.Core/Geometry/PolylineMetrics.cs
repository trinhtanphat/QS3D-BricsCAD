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
            double compensation = 0d;
            for (var i = 1; i < points.Count; i++)
                AddLengthCompensated(ref total, ref compensation, points[i - 1].DistanceTo(points[i]));
            if (closed)
                AddLengthCompensated(ref total, ref compensation, points[points.Count - 1].DistanceTo(points[0]));

            var length = AddFinite(total, compensation);
            return length == 0d ? 0d : length;
        }

        public static double SignedArea(IReadOnlyList<Point2> points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < 3)
            {
                EnsureFinite(points);
                return 0d;
            }

            EnsureFinite(points);
            try
            {
                return SignedAreaDirect(points);
            }
            catch (OverflowException)
            {
                return SignedAreaScaled(points);
            }
        }

        public static double Area(IReadOnlyList<Point2> points) => Math.Abs(SignedArea(points));

        private static double SignedAreaDirect(IReadOnlyList<Point2> points)
        {
            var origin = points[0];
            double sum = 0d;
            double compensation = 0d;

            for (var i = 1; i < points.Count - 1; i++)
            {
                var cross = TranslatedCrossFinite(origin, points[i], points[i + 1]);
                AddCompensated(ref sum, ref compensation, cross);
            }

            return MultiplyFinitePreservingNonZero(sum, 0.5d, "Polyline area");
        }

        private static double SignedAreaScaled(IReadOnlyList<Point2> points)
        {
            var origin = points[0];
            var translated = true;
            double scaleX = 0d;
            double scaleY = 0d;

            for (var i = 1; i < points.Count; i++)
            {
                var dx = points[i].X - origin.X;
                var dy = points[i].Y - origin.Y;
                if (!Finite(dx) || !Finite(dy))
                {
                    translated = false;
                    break;
                }

                scaleX = Math.Max(scaleX, Math.Abs(dx));
                scaleY = Math.Max(scaleY, Math.Abs(dy));
            }

            if (!translated)
            {
                scaleX = 0d;
                scaleY = 0d;
                for (var i = 0; i < points.Count; i++)
                {
                    scaleX = Math.Max(scaleX, Math.Abs(points[i].X));
                    scaleY = Math.Max(scaleY, Math.Abs(points[i].Y));
                }
            }

            if (!Finite(scaleX) || !Finite(scaleY))
                throw new OverflowException("Polyline area input exceeds the supported numeric range.");
            if (scaleX == 0d || scaleY == 0d) return 0d;

            double sum = 0d;
            double compensation = 0d;
            for (var i = 1; i < points.Count - 1; i++)
            {
                double ax;
                double ay;
                double bx;
                double by;
                if (translated)
                {
                    ax = (points[i].X - origin.X) / scaleX;
                    ay = (points[i].Y - origin.Y) / scaleY;
                    bx = (points[i + 1].X - origin.X) / scaleX;
                    by = (points[i + 1].Y - origin.Y) / scaleY;
                }
                else
                {
                    ax = points[i].X / scaleX - origin.X / scaleX;
                    ay = points[i].Y / scaleY - origin.Y / scaleY;
                    bx = points[i + 1].X / scaleX - origin.X / scaleX;
                    by = points[i + 1].Y / scaleY - origin.Y / scaleY;
                }

                var cross = ax * by - ay * bx;
                if (!Finite(cross)) throw new OverflowException("Polyline normalized area exceeds the supported numeric range.");
                AddCompensated(ref sum, ref compensation, cross);
            }

            var normalizedArea = MultiplyFinitePreservingNonZero(sum, 0.5d, "Polyline normalized area");
            return RestoreScaledAreaFinite(normalizedArea, scaleX, scaleY);
        }

        private static void AddCompensated(ref double sum, ref double compensation, double value)
        {
            var corrected = value - compensation;
            var next = sum + corrected;
            if (!Finite(next)) throw new OverflowException("Polyline area exceeds the supported numeric range.");
            compensation = (next - sum) - corrected;
            sum = next;
        }

        private static void AddLengthCompensated(ref double total, ref double compensation, double value)
        {
            var next = AddFinite(total, value);
            var correction = Math.Abs(total) >= Math.Abs(value)
                ? (total - next) + value
                : (value - next) + total;

            total = next;
            compensation = AddFinite(compensation, correction);
        }

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
            var scaled = MultiplyFinitePreservingNonZero(normalized, firstScale, "Polyline area");
            return MultiplyFinitePreservingNonZero(scaled, secondScale, "Polyline area");
        }

        private static double RestoreScaledAreaFinite(double normalizedArea, double scaleX, double scaleY)
        {
            if (normalizedArea == 0d) return 0d;

            var smaller = Math.Min(scaleX, scaleY);
            var larger = Math.Max(scaleX, scaleY);
            var firstScale = Math.Abs(normalizedArea) <= 1d ? larger : smaller;
            var secondScale = Math.Abs(normalizedArea) <= 1d ? smaller : larger;
            var scaled = MultiplyFinitePreservingNonZero(normalizedArea, firstScale, "Polyline area");
            return MultiplyFinitePreservingNonZero(scaled, secondScale, "Polyline area");
        }

        private static double CrossFinite(double ax, double ay, double bx, double by)
        {
            var firstProduct = ax * by;
            var secondProduct = ay * bx;
            var firstProductUnderflowed = firstProduct == 0d && ax != 0d && by != 0d;
            var secondProductUnderflowed = secondProduct == 0d && ay != 0d && bx != 0d;
            if (Finite(firstProduct) && Finite(secondProduct) && !firstProductUnderflowed && !secondProductUnderflowed)
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
            var scaled = MultiplyFinitePreservingNonZero(normalized, firstScale, "Polyline area");
            return MultiplyFinitePreservingNonZero(scaled, secondScale, "Polyline area");
        }

        private static double MultiplyFinitePreservingNonZero(double first, double second, string operation)
        {
            var value = first * second;
            if (!Finite(value)) throw new OverflowException(operation + " exceeds the supported numeric range.");
            if (value == 0d && first != 0d && second != 0d)
                throw new OverflowException(operation + " underflowed a non-zero value to zero.");
            return value == 0d ? 0d : value;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
