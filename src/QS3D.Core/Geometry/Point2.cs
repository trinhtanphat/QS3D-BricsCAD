using System;
using System.Globalization;

namespace QS3D.Core.Geometry
{
    public readonly struct Point2 : IEquatable<Point2>
    {
        public Point2(double x, double y) { X = x; Y = y; }
        public double X { get; }
        public double Y { get; }

        public double DistanceTo(Point2 other)
        {
            if (!Finite(X) || !Finite(Y) || !Finite(other.X) || !Finite(other.Y))
                throw new InvalidOperationException("Point coordinates must be finite.");

            var dx = other.X - X;
            var dy = other.Y - Y;
            if (!Finite(dx) || !Finite(dy))
                throw new OverflowException("Point distance delta exceeds the supported numeric range.");

            var ax = Math.Abs(dx);
            var ay = Math.Abs(dy);
            var scale = Math.Max(ax, ay);
            if (scale == 0d) return 0d;

            var ratio = Math.Min(ax, ay) / scale;
            var distance = scale * Math.Sqrt(1d + ratio * ratio);
            if (!Finite(distance))
                throw new OverflowException("Point distance exceeds the supported numeric range.");
            return distance;
        }

        public bool Equals(Point2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Point2 p && Equals(p);
        public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Y.GetHashCode());
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0}, {1})", X, Y);

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
