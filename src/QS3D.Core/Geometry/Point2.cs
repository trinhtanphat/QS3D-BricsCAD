using System;

namespace QS3D.Core.Geometry
{
    public readonly struct Point2 : IEquatable<Point2>
    {
        public Point2(double x, double y) { X = x; Y = y; }
        public double X { get; }
        public double Y { get; }
        public double DistanceTo(Point2 other) { var dx = other.X - X; var dy = other.Y - Y; return Math.Sqrt(dx * dx + dy * dy); }
        public bool Equals(Point2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Point2 p && Equals(p);
        public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Y.GetHashCode());
        public override string ToString() => $"({X}, {Y})";
    }
}
