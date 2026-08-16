using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class GridLinearStation
    {
        public GridLinearStation(string elementId, double coordinateM)
        {
            ElementId = NormalizeId(elementId);
            CoordinateM = coordinateM;
        }

        public string ElementId { get; }
        public double CoordinateM { get; }

        private static string NormalizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Grid station element id is required.", nameof(value));
            return value.Trim();
        }
    }

    public sealed class RectangularGridSystemInput
    {
        public Point2 OriginM { get; set; }
        public Point2 UAxis { get; set; }
        public Point2 VAxis { get; set; }
        public IReadOnlyList<GridLinearStation> UStations { get; set; } = Array.Empty<GridLinearStation>();
        public IReadOnlyList<GridLinearStation> VStations { get; set; } = Array.Empty<GridLinearStation>();
        public double UMinM { get; set; }
        public double UMaxM { get; set; }
        public double VMinM { get; set; }
        public double VMaxM { get; set; }
    }

    public sealed class GridAngularStation
    {
        public GridAngularStation(string elementId, double angleRad)
        {
            ElementId = NormalizeId(elementId);
            AngleRad = angleRad;
        }

        public string ElementId { get; }
        public double AngleRad { get; }

        private static string NormalizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Grid angular station element id is required.", nameof(value));
            return value.Trim();
        }
    }

    public sealed class GridRadialStation
    {
        public GridRadialStation(string elementId, double radiusM)
        {
            ElementId = NormalizeId(elementId);
            RadiusM = radiusM;
        }

        public string ElementId { get; }
        public double RadiusM { get; }

        private static string NormalizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Grid radial station element id is required.", nameof(value));
            return value.Trim();
        }
    }

    public sealed class RadialGridSystemInput
    {
        public Point2 CenterM { get; set; }
        public IReadOnlyList<GridAngularStation> Rays { get; set; } = Array.Empty<GridAngularStation>();
        public IReadOnlyList<GridRadialStation> Rings { get; set; } = Array.Empty<GridRadialStation>();
        public double InnerRadiusM { get; set; }
        public double OuterRadiusM { get; set; }
    }

    public static class GridSystemPlanner
    {
        private const int MaxCurves = 2000;
        private const double TwoPi = Math.PI * 2d;

        public static IReadOnlyList<GridReferenceCurve> PlanRectangular(
            RectangularGridSystemInput input,
            double coordinateTolerance = 1e-8d,
            double orthogonalityTolerance = 1e-6d)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            ValidateTolerance(coordinateTolerance, nameof(coordinateTolerance));
            if (!Finite(orthogonalityTolerance) || orthogonalityTolerance <= 0d || orthogonalityTolerance >= 1d)
                throw new ArgumentOutOfRangeException(nameof(orthogonalityTolerance), "Grid orthogonality tolerance must be finite and in (0, 1).");
            EnsureFinitePoint(input.OriginM, "Rectangular Grid origin");

            var u = NormalizeAxis(input.UAxis, coordinateTolerance, "U");
            var v = NormalizeAxis(input.VAxis, coordinateTolerance, "V");
            var dot = u.X * v.X + u.Y * v.Y;
            if (!Finite(dot)) throw new OverflowException("Rectangular Grid axis dot product is not finite.");
            if (Math.Abs(dot) > orthogonalityTolerance)
                throw new InvalidOperationException("Rectangular Grid U/V axes are not orthogonal within the explicit tolerance.");

            ValidateExtent(input.UMinM, input.UMaxM, coordinateTolerance, "U");
            ValidateExtent(input.VMinM, input.VMaxM, coordinateTolerance, "V");
            var uStations = RequireStations(input.UStations, "U");
            var vStations = RequireStations(input.VStations, "V");
            if ((long)uStations.Count + vStations.Count > MaxCurves)
                throw new InvalidOperationException("Rectangular Grid system exceeds the supported " + MaxCurves + " curve limit.");

            ValidateLinearStations(uStations, input.UMinM, input.UMaxM, coordinateTolerance, "U");
            ValidateLinearStations(vStations, input.VMinM, input.VMaxM, coordinateTolerance, "V");
            ValidateIds(uStations.Select(x => x.ElementId).Concat(vStations.Select(x => x.ElementId)));

            var curves = new List<GridReferenceCurve>(uStations.Count + vStations.Count);
            foreach (var station in uStations)
            {
                var basePoint = Add(input.OriginM, Scale(u, station.CoordinateM), coordinateTolerance);
                var start = Add(basePoint, Scale(v, input.VMinM), coordinateTolerance);
                var end = Add(basePoint, Scale(v, input.VMaxM), coordinateTolerance);
                curves.Add(GridReferenceCurve.Line(station.ElementId, start, end));
            }
            foreach (var station in vStations)
            {
                var basePoint = Add(input.OriginM, Scale(v, station.CoordinateM), coordinateTolerance);
                var start = Add(basePoint, Scale(u, input.UMinM), coordinateTolerance);
                var end = Add(basePoint, Scale(u, input.UMaxM), coordinateTolerance);
                curves.Add(GridReferenceCurve.Line(station.ElementId, start, end));
            }
            return curves.AsReadOnly();
        }

        public static IReadOnlyList<GridReferenceCurve> PlanRadial(
            RadialGridSystemInput input,
            double coordinateTolerance = 1e-8d,
            double angleTolerance = 1e-8d)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            ValidateTolerance(coordinateTolerance, nameof(coordinateTolerance));
            ValidateTolerance(angleTolerance, nameof(angleTolerance));
            EnsureFinitePoint(input.CenterM, "Radial Grid center");
            if (!Finite(input.InnerRadiusM) || input.InnerRadiusM < 0d)
                throw new ArgumentOutOfRangeException(nameof(input.InnerRadiusM), "Radial Grid inner radius must be finite and non-negative.");
            if (!Finite(input.OuterRadiusM) || input.OuterRadiusM - input.InnerRadiusM <= coordinateTolerance)
                throw new ArgumentOutOfRangeException(nameof(input.OuterRadiusM), "Radial Grid outer radius must exceed inner radius by more than tolerance.");

            var rays = input.Rays ?? throw new ArgumentNullException(nameof(input.Rays));
            var rings = input.Rings ?? throw new ArgumentNullException(nameof(input.Rings));
            if (rays.Count == 0) throw new InvalidOperationException("Radial Grid system requires at least one ray.");
            if (rings.Count == 0) throw new InvalidOperationException("Radial Grid system requires at least one ring.");
            if ((long)rays.Count + rings.Count > MaxCurves)
                throw new InvalidOperationException("Radial Grid system exceeds the supported " + MaxCurves + " curve limit.");

            ValidateIds(rays.Select(x => x?.ElementId ?? string.Empty).Concat(rings.Select(x => x?.ElementId ?? string.Empty)));

            var normalizedAngles = new List<double>(rays.Count);
            for (var i = 0; i < rays.Count; i++)
            {
                var ray = rays[i] ?? throw new ArgumentException("Radial Grid contains a null ray at index " + i + ".", nameof(input));
                if (!Finite(ray.AngleRad)) throw new ArgumentOutOfRangeException(nameof(input), "Radial Grid ray angle must be finite for " + ray.ElementId + ".");
                var angle = NormalizeAngle(ray.AngleRad);
                foreach (var existing in normalizedAngles)
                    if (AngularDistance(angle, existing) <= angleTolerance)
                        throw new InvalidOperationException("Radial Grid contains duplicate/ambiguous ray angles within tolerance.");
                normalizedAngles.Add(angle);
            }

            var normalizedRadii = new List<double>(rings.Count);
            for (var i = 0; i < rings.Count; i++)
            {
                var ring = rings[i] ?? throw new ArgumentException("Radial Grid contains a null ring at index " + i + ".", nameof(input));
                if (!Finite(ring.RadiusM) || ring.RadiusM <= coordinateTolerance)
                    throw new ArgumentOutOfRangeException(nameof(input), "Radial Grid ring radius must be finite and positive for " + ring.ElementId + ".");
                if (ring.RadiusM < input.InnerRadiusM - coordinateTolerance || ring.RadiusM > input.OuterRadiusM + coordinateTolerance)
                    throw new InvalidOperationException("Radial Grid ring " + ring.ElementId + " lies outside the declared radial extent.");
                foreach (var existing in normalizedRadii)
                    if (Math.Abs(ring.RadiusM - existing) <= coordinateTolerance)
                        throw new InvalidOperationException("Radial Grid contains duplicate/ambiguous ring radii within tolerance.");
                normalizedRadii.Add(ring.RadiusM);
            }

            var curves = new List<GridReferenceCurve>(rays.Count + rings.Count);
            for (var i = 0; i < rays.Count; i++)
            {
                var ray = rays[i];
                var angle = normalizedAngles[i];
                var direction = new Point2(Math.Cos(angle), Math.Sin(angle));
                var start = Add(input.CenterM, Scale(direction, input.InnerRadiusM), coordinateTolerance);
                var end = Add(input.CenterM, Scale(direction, input.OuterRadiusM), coordinateTolerance);
                curves.Add(GridReferenceCurve.Line(ray.ElementId, start, end));
            }
            for (var i = 0; i < rings.Count; i++)
            {
                var ring = rings[i];
                var curve = GridReferenceCurve.Arc(ring.ElementId, input.CenterM, ring.RadiusM, 0d, TwoPi);
                if (!Finite(curve.Start.X) || !Finite(curve.Start.Y) || !Finite(curve.End.X) || !Finite(curve.End.Y))
                    throw new OverflowException("Radial Grid ring endpoint generation exceeded the supported numeric range for " + ring.ElementId + ".");
                curves.Add(curve);
            }
            return curves.AsReadOnly();
        }

        private static IReadOnlyList<GridLinearStation> RequireStations(IReadOnlyList<GridLinearStation> stations, string family)
        {
            if (stations == null) throw new ArgumentNullException(nameof(stations));
            if (stations.Count == 0) throw new InvalidOperationException("Rectangular Grid system requires at least one " + family + " station.");
            return stations;
        }

        private static void ValidateLinearStations(IReadOnlyList<GridLinearStation> stations, double min, double max, double tolerance, string family)
        {
            var coordinates = new List<double>(stations.Count);
            for (var i = 0; i < stations.Count; i++)
            {
                var station = stations[i] ?? throw new ArgumentException("Rectangular Grid contains a null " + family + " station at index " + i + ".", nameof(stations));
                if (!Finite(station.CoordinateM)) throw new ArgumentOutOfRangeException(nameof(stations), "Rectangular Grid station coordinate must be finite for " + station.ElementId + ".");
                if (station.CoordinateM < min - tolerance || station.CoordinateM > max + tolerance)
                    throw new InvalidOperationException("Rectangular Grid station " + station.ElementId + " lies outside the declared " + family + " extent.");
                foreach (var existing in coordinates)
                    if (Math.Abs(station.CoordinateM - existing) <= tolerance)
                        throw new InvalidOperationException("Rectangular Grid contains duplicate/ambiguous " + family + " station coordinates within tolerance.");
                coordinates.Add(station.CoordinateM);
            }
        }

        private static void ValidateIds(IEnumerable<string> ids)
        {
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in ids)
            {
                var id = (raw ?? string.Empty).Trim();
                if (id.Length == 0) throw new ArgumentException("Grid system curve element id is required.", nameof(ids));
                if (!unique.Add(id)) throw new InvalidOperationException("Grid system contains duplicate element id: " + id + ".");
            }
        }

        private static Point2 NormalizeAxis(Point2 axis, double tolerance, string label)
        {
            EnsureFinitePoint(axis, "Rectangular Grid " + label + " axis");
            var length = Hypot(axis.X, axis.Y);
            if (!(length > tolerance) || !Finite(length))
                throw new InvalidOperationException("Rectangular Grid " + label + " axis is zero/degenerate within tolerance.");
            var normalized = new Point2(axis.X / length, axis.Y / length);
            EnsureFinitePoint(normalized, "Rectangular Grid normalized " + label + " axis");
            return normalized;
        }

        private static void ValidateExtent(double min, double max, double tolerance, string label)
        {
            if (!Finite(min) || !Finite(max)) throw new ArgumentOutOfRangeException(label + "Extent", "Rectangular Grid extents must be finite.");
            var span = max - min;
            if (!Finite(span)) throw new OverflowException("Rectangular Grid " + label + " extent span exceeds the supported numeric range.");
            if (span <= tolerance) throw new InvalidOperationException("Rectangular Grid " + label + " extent must have positive span above tolerance.");
        }

        private static Point2 Scale(Point2 value, double scalar)
        {
            var x = value.X * scalar;
            var y = value.Y * scalar;
            if (!Finite(x) || !Finite(y)) throw new OverflowException("Grid system coordinate scaling exceeded the supported numeric range.");
            return new Point2(x, y);
        }

        private static Point2 Add(Point2 left, Point2 right, double tolerance)
        {
            var x = left.X + right.X;
            var y = left.Y + right.Y;
            if (!Finite(x) || !Finite(y)) throw new OverflowException("Grid system coordinate addition exceeded the supported numeric range.");
            RequireRepresentableAddition(left.X, right.X, x, tolerance, "X");
            RequireRepresentableAddition(left.Y, right.Y, y, tolerance, "Y");
            return new Point2(x, y);
        }

        private static void RequireRepresentableAddition(double left, double right, double sum, double tolerance, string component)
        {
            if ((Math.Abs(right) > tolerance && sum == left) ||
                (Math.Abs(left) > tolerance && sum == right))
                throw new OverflowException("Grid system " + component + " coordinate addition lost a meaningful nonzero operand to floating-point precision.");
        }

        private static double NormalizeAngle(double angle)
        {
            var normalized = angle % TwoPi;
            if (normalized < 0d) normalized += TwoPi;
            if (!Finite(normalized)) throw new OverflowException("Radial Grid angle normalization exceeded the supported numeric range.");
            return normalized;
        }

        private static double AngularDistance(double left, double right)
        {
            var delta = Math.Abs(left - right);
            return Math.Min(delta, TwoPi - delta);
        }

        private static double Hypot(double x, double y)
        {
            var ax = Math.Abs(x);
            var ay = Math.Abs(y);
            var scale = Math.Max(ax, ay);
            if (scale == 0d) return 0d;
            var ratio = Math.Min(ax, ay) / scale;
            var result = scale * Math.Sqrt(1d + ratio * ratio);
            return Finite(result) ? result : double.PositiveInfinity;
        }

        private static void ValidateTolerance(double value, string name)
        {
            if (!Finite(value) || value <= 0d) throw new ArgumentOutOfRangeException(name, "Grid tolerance must be finite and positive.");
        }

        private static void EnsureFinitePoint(Point2 point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y)) throw new ArgumentOutOfRangeException(label, "Grid point coordinates must be finite.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
