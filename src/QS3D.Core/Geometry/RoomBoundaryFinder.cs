using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class BoundarySegment2
    {
        public BoundarySegment2(Point2 start, Point2 end, string sourceId = "")
        {
            Start = start;
            End = end;
            SourceId = sourceId ?? string.Empty;
        }

        public Point2 Start { get; }
        public Point2 End { get; }
        public string SourceId { get; }
    }

    public sealed class RoomBoundary2
    {
        internal RoomBoundary2(IReadOnlyList<Point2> vertices, IReadOnlyList<string> sourceIds, double area, double perimeter, Point2 centroid, string key)
        {
            Vertices = vertices;
            SourceIds = sourceIds;
            Area = area;
            Perimeter = perimeter;
            Centroid = centroid;
            Key = key;
        }

        public IReadOnlyList<Point2> Vertices { get; }
        public IReadOnlyList<string> SourceIds { get; }
        public double Area { get; }
        public double Perimeter { get; }
        public Point2 Centroid { get; }
        public string Key { get; }
    }

    public static class RoomBoundaryFinder
    {
        private readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            public EdgeKey(int a, int b)
            {
                if (a <= b) { A = a; B = b; }
                else { A = b; B = a; }
            }
            public int A { get; }
            public int B { get; }
            public bool Equals(EdgeKey other) => A == other.A && B == other.B;
            public override bool Equals(object? obj) => obj is EdgeKey other && Equals(other);
            public override int GetHashCode() => unchecked((A * 397) ^ B);
        }

        private readonly struct DirectedEdge : IEquatable<DirectedEdge>
        {
            public DirectedEdge(int from, int to) { From = from; To = to; }
            public int From { get; }
            public int To { get; }
            public bool Equals(DirectedEdge other) => From == other.From && To == other.To;
            public override bool Equals(object? obj) => obj is DirectedEdge other && Equals(other);
            public override int GetHashCode() => unchecked((From * 397) ^ To);
        }

        private readonly struct GridKey : IEquatable<GridKey>
        {
            public GridKey(long x, long y) { X = x; Y = y; }
            public long X { get; }
            public long Y { get; }
            public bool Equals(GridKey other) => X == other.X && Y == other.Y;
            public override bool Equals(object? obj) => obj is GridKey other && Equals(other);
            public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Y.GetHashCode());
        }

        private sealed class VertexIndex
        {
            private readonly double _tolerance;
            private readonly List<Point2> _points = new List<Point2>();
            private readonly Dictionary<GridKey, List<int>> _cells = new Dictionary<GridKey, List<int>>();

            public VertexIndex(double tolerance) { _tolerance = tolerance; }
            public IReadOnlyList<Point2> Points => _points;

            public int GetOrAdd(Point2 point)
            {
                var cell = Cell(point);
                for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                {
                    var probe = new GridKey(cell.X + dx, cell.Y + dy);
                    if (!_cells.TryGetValue(probe, out var candidates)) continue;
                    foreach (var index in candidates)
                        if (_points[index].DistanceTo(point) <= _tolerance) return index;
                }

                var id = _points.Count;
                _points.Add(point);
                if (!_cells.TryGetValue(cell, out var bucket)) { bucket = new List<int>(); _cells.Add(cell, bucket); }
                bucket.Add(id);
                return id;
            }

            private GridKey Cell(Point2 point)
            {
                var x = Math.Floor(point.X / _tolerance);
                var y = Math.Floor(point.Y / _tolerance);
                if (x < long.MinValue + 2d || x > long.MaxValue - 2d || y < long.MinValue + 2d || y > long.MaxValue - 2d)
                    throw new ArgumentOutOfRangeException(nameof(point), "Coordinate magnitude is too large for the requested snap tolerance.");
                return new GridKey((long)x, (long)y);
            }
        }

        public static IReadOnlyList<RoomBoundary2> Find(IEnumerable<BoundarySegment2> segments, double snapTolerance = 1e-6, double minimumArea = 1e-6, int maximumSegments = 5000)
        {
            if (segments == null) throw new ArgumentNullException(nameof(segments));
            if (!FinitePositive(snapTolerance)) throw new ArgumentOutOfRangeException(nameof(snapTolerance));
            if (!Finite(minimumArea) || minimumArea < 0d) throw new ArgumentOutOfRangeException(nameof(minimumArea));
            if (maximumSegments <= 0) throw new ArgumentOutOfRangeException(nameof(maximumSegments));

            var source = segments.ToList();
            if (source.Count > maximumSegments) throw new InvalidOperationException("Boundary network exceeds the configured segment limit.");
            if (source.Count < 3) return Array.Empty<RoomBoundary2>();
            foreach (var segment in source) ValidateSegment(segment, snapTolerance);

            var splitParameters = new List<double>[source.Count];
            for (var i = 0; i < source.Count; i++) splitParameters[i] = new List<double> { 0d, 1d };
            for (var i = 0; i < source.Count; i++)
            for (var j = i + 1; j < source.Count; j++)
                AddIntersections(source[i], source[j], splitParameters[i], splitParameters[j], snapTolerance);

            var vertices = new VertexIndex(snapTolerance);
            var edgeSources = new Dictionary<EdgeKey, HashSet<string>>();
            for (var i = 0; i < source.Count; i++)
            {
                var segment = source[i];
                var length = segment.Start.DistanceTo(segment.End);
                var parameterTolerance = Math.Min(1e-6, snapTolerance / Math.Max(length, snapTolerance));
                var parameters = UniqueSorted(splitParameters[i], parameterTolerance);
                for (var p = 1; p < parameters.Count; p++)
                {
                    var a = Interpolate(segment.Start, segment.End, parameters[p - 1]);
                    var b = Interpolate(segment.Start, segment.End, parameters[p]);
                    if (a.DistanceTo(b) <= snapTolerance) continue;
                    var va = vertices.GetOrAdd(a);
                    var vb = vertices.GetOrAdd(b);
                    if (va == vb) continue;
                    var key = new EdgeKey(va, vb);
                    if (!edgeSources.TryGetValue(key, out var ids))
                    {
                        ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        edgeSources.Add(key, ids);
                    }
                    if (!string.IsNullOrWhiteSpace(segment.SourceId)) ids.Add(segment.SourceId.Trim());
                }
            }

            if (edgeSources.Count < 3) return Array.Empty<RoomBoundary2>();
            var adjacency = new Dictionary<int, List<int>>();
            foreach (var edge in edgeSources.Keys)
            {
                AddNeighbor(adjacency, edge.A, edge.B);
                AddNeighbor(adjacency, edge.B, edge.A);
            }
            foreach (var item in adjacency)
            {
                var origin = vertices.Points[item.Key];
                item.Value.Sort((a, b) =>
                {
                    var pa = vertices.Points[a];
                    var pb = vertices.Points[b];
                    var aa = Math.Atan2(pa.Y - origin.Y, pa.X - origin.X);
                    var ab = Math.Atan2(pb.Y - origin.Y, pb.X - origin.X);
                    var comparison = aa.CompareTo(ab);
                    return comparison != 0 ? comparison : a.CompareTo(b);
                });
            }

            var visited = new HashSet<DirectedEdge>();
            var faces = new List<RoomBoundary2>();
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in edgeSources.Keys.OrderBy(x => x.A).ThenBy(x => x.B))
            {
                TraceFace(new DirectedEdge(edge.A, edge.B), adjacency, edgeSources, vertices.Points, visited, faces, seenKeys, snapTolerance, minimumArea);
                TraceFace(new DirectedEdge(edge.B, edge.A), adjacency, edgeSources, vertices.Points, visited, faces, seenKeys, snapTolerance, minimumArea);
            }

            return faces
                .OrderBy(x => x.Centroid.X)
                .ThenBy(x => x.Centroid.Y)
                .ThenBy(x => x.Area)
                .ThenBy(x => x.Key, StringComparer.Ordinal)
                .ToArray();
        }

        private static void TraceFace(DirectedEdge start, IReadOnlyDictionary<int, List<int>> adjacency, IReadOnlyDictionary<EdgeKey, HashSet<string>> edgeSources, IReadOnlyList<Point2> points, ISet<DirectedEdge> visited, ICollection<RoomBoundary2> output, ISet<string> seenKeys, double tolerance, double minimumArea)
        {
            if (visited.Contains(start)) return;
            var walk = new List<int>();
            var current = start;
            var maxSteps = edgeSources.Count * 2 + 2;
            var closed = false;
            for (var step = 0; step < maxSteps; step++)
            {
                if (visited.Contains(current)) { closed = current.Equals(start); break; }
                visited.Add(current);
                walk.Add(current.From);
                var next = Next(current, adjacency);
                if (next.Equals(start)) { closed = true; break; }
                current = next;
            }
            if (!closed) return;

            var normalized = RemoveBacktracks(walk);
            if (normalized.Count < 3) return;
            if (normalized.Distinct().Count() != normalized.Count) return;
            var rawPoints = normalized.Select(x => points[x]).ToList();
            var signedArea = PolylineMetrics.SignedArea(rawPoints);
            if (!Finite(signedArea) || signedArea <= minimumArea) return;

            var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < normalized.Count; i++)
            {
                var edge = new EdgeKey(normalized[i], normalized[(i + 1) % normalized.Count]);
                if (!edgeSources.TryGetValue(edge, out var edgeIds)) return;
                foreach (var id in edgeIds) sources.Add(id);
            }

            var simplified = Simplify(rawPoints, tolerance);
            if (simplified.Count < 3) return;
            var area = PolylineMetrics.SignedArea(simplified);
            if (!Finite(area) || area <= minimumArea) return;
            var perimeter = PolylineMetrics.Length(simplified, true);
            if (!FinitePositive(perimeter)) return;
            var key = CanonicalKey(simplified, tolerance);
            if (!seenKeys.Add(key)) return;
            var centroid = PolygonCentroid(simplified, area);
            var sourceIds = sources.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal).ToArray();
            output.Add(new RoomBoundary2(simplified.ToArray(), sourceIds, area, perimeter, centroid, key));
        }

        private static DirectedEdge Next(DirectedEdge edge, IReadOnlyDictionary<int, List<int>> adjacency)
        {
            if (!adjacency.TryGetValue(edge.To, out var neighbors) || neighbors.Count == 0) return edge;
            var reverseIndex = neighbors.IndexOf(edge.From);
            if (reverseIndex < 0) return edge;
            var nextIndex = reverseIndex == 0 ? neighbors.Count - 1 : reverseIndex - 1;
            return new DirectedEdge(edge.To, neighbors[nextIndex]);
        }

        private static List<int> RemoveBacktracks(IReadOnlyList<int> input)
        {
            var result = input.ToList();
            var changed = true;
            while (changed && result.Count >= 3)
            {
                changed = false;
                for (var i = 0; i < result.Count; i++)
                {
                    var second = (i + 1) % result.Count;
                    var third = (i + 2) % result.Count;
                    if (result[i] != result[third]) continue;
                    if (third > second)
                    {
                        result.RemoveAt(third);
                        result.RemoveAt(second);
                    }
                    else
                    {
                        var remove = new HashSet<int> { second, third };
                        result = result.Where((_, index) => !remove.Contains(index)).ToList();
                    }
                    changed = true;
                    break;
                }
            }
            return result;
        }

        private static List<Point2> Simplify(IReadOnlyList<Point2> input, double tolerance)
        {
            var points = input.ToList();
            var changed = true;
            while (changed && points.Count >= 3)
            {
                changed = false;
                for (var i = 0; i < points.Count; i++)
                {
                    var previous = points[(i + points.Count - 1) % points.Count];
                    var current = points[i];
                    var next = points[(i + 1) % points.Count];
                    var ax = current.X - previous.X; var ay = current.Y - previous.Y;
                    var bx = next.X - current.X; var by = next.Y - current.Y;
                    var la = Math.Sqrt(ax * ax + ay * ay); var lb = Math.Sqrt(bx * bx + by * by);
                    if (la <= tolerance || lb <= tolerance)
                    {
                        points.RemoveAt(i); changed = true; break;
                    }
                    var cross = Math.Abs(ax * by - ay * bx);
                    var dot = ax * bx + ay * by;
                    if (cross <= tolerance * (la + lb) && dot >= 0d)
                    {
                        points.RemoveAt(i); changed = true; break;
                    }
                }
            }
            return points;
        }

        private static Point2 PolygonCentroid(IReadOnlyList<Point2> points, double signedArea)
        {
            var cx = 0d; var cy = 0d;
            for (var i = 0; i < points.Count; i++)
            {
                var a = points[i]; var b = points[(i + 1) % points.Count];
                var cross = a.X * b.Y - b.X * a.Y;
                cx += (a.X + b.X) * cross;
                cy += (a.Y + b.Y) * cross;
            }
            var factor = 1d / (6d * signedArea);
            return new Point2(cx * factor, cy * factor);
        }

        private static string CanonicalKey(IReadOnlyList<Point2> points, double tolerance)
        {
            var tokens = points.Select(p => Token(p, tolerance)).ToArray();
            string? best = null;
            for (var start = 0; start < tokens.Length; start++)
            {
                var rotated = string.Join(";", Enumerable.Range(0, tokens.Length).Select(offset => tokens[(start + offset) % tokens.Length]));
                if (best == null || string.CompareOrdinal(rotated, best) < 0) best = rotated;
            }
            return best ?? string.Empty;
        }

        private static string Token(Point2 point, double tolerance)
        {
            var x = Math.Round(point.X / tolerance) * tolerance;
            var y = Math.Round(point.Y / tolerance) * tolerance;
            if (x == 0d) x = 0d;
            if (y == 0d) y = 0d;
            return x.ToString("R", CultureInfo.InvariantCulture) + "," + y.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void AddIntersections(BoundarySegment2 a, BoundarySegment2 b, ICollection<double> aParameters, ICollection<double> bParameters, double tolerance)
        {
            var rx = a.End.X - a.Start.X; var ry = a.End.Y - a.Start.Y;
            var sx = b.End.X - b.Start.X; var sy = b.End.Y - b.Start.Y;
            var qpx = b.Start.X - a.Start.X; var qpy = b.Start.Y - a.Start.Y;
            var rxs = Cross(rx, ry, sx, sy);
            var lenR = Math.Sqrt(rx * rx + ry * ry); var lenS = Math.Sqrt(sx * sx + sy * sy);
            var crossTolerance = tolerance * (lenR + lenS + tolerance);
            if (Math.Abs(rxs) > crossTolerance)
            {
                var t = Cross(qpx, qpy, sx, sy) / rxs;
                var u = Cross(qpx, qpy, rx, ry) / rxs;
                var tTolerance = tolerance / Math.Max(lenR, tolerance);
                var uTolerance = tolerance / Math.Max(lenS, tolerance);
                if (t >= -tTolerance && t <= 1d + tTolerance && u >= -uTolerance && u <= 1d + uTolerance)
                {
                    AddParameter(aParameters, Clamp01(t), tTolerance);
                    AddParameter(bParameters, Clamp01(u), uTolerance);
                }
                return;
            }

            if (Math.Abs(Cross(qpx, qpy, rx, ry)) > crossTolerance) return;
            AddEndpointIfOn(a, b.Start, aParameters, tolerance);
            AddEndpointIfOn(a, b.End, aParameters, tolerance);
            AddEndpointIfOn(b, a.Start, bParameters, tolerance);
            AddEndpointIfOn(b, a.End, bParameters, tolerance);
        }

        private static void AddEndpointIfOn(BoundarySegment2 segment, Point2 point, ICollection<double> parameters, double tolerance)
        {
            var dx = segment.End.X - segment.Start.X; var dy = segment.End.Y - segment.Start.Y;
            var lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= tolerance * tolerance) return;
            var px = point.X - segment.Start.X; var py = point.Y - segment.Start.Y;
            var t = (px * dx + py * dy) / lengthSquared;
            var length = Math.Sqrt(lengthSquared);
            var parameterTolerance = tolerance / Math.Max(length, tolerance);
            if (t < -parameterTolerance || t > 1d + parameterTolerance) return;
            var projected = Interpolate(segment.Start, segment.End, Clamp01(t));
            if (projected.DistanceTo(point) <= tolerance) AddParameter(parameters, Clamp01(t), parameterTolerance);
        }

        private static List<double> UniqueSorted(IEnumerable<double> values, double tolerance)
        {
            var sorted = values.OrderBy(x => x).ToList();
            var result = new List<double>();
            foreach (var value in sorted)
            {
                var clamped = Clamp01(value);
                if (result.Count == 0 || Math.Abs(clamped - result[result.Count - 1]) > tolerance) result.Add(clamped);
            }
            if (result.Count == 0 || result[0] > tolerance) result.Insert(0, 0d); else result[0] = 0d;
            if (result[result.Count - 1] < 1d - tolerance) result.Add(1d); else result[result.Count - 1] = 1d;
            return result;
        }

        private static void AddParameter(ICollection<double> parameters, double value, double tolerance)
        {
            foreach (var existing in parameters) if (Math.Abs(existing - value) <= tolerance) return;
            parameters.Add(value);
        }

        private static void AddNeighbor(IDictionary<int, List<int>> adjacency, int from, int to)
        {
            if (!adjacency.TryGetValue(from, out var list)) { list = new List<int>(); adjacency.Add(from, list); }
            if (!list.Contains(to)) list.Add(to);
        }

        private static Point2 Interpolate(Point2 a, Point2 b, double t) => new Point2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;
        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool FinitePositive(double value) => Finite(value) && value > 0d;

        private static void ValidateSegment(BoundarySegment2 segment, double tolerance)
        {
            if (segment == null) throw new ArgumentException("Boundary segment collection cannot contain null.", nameof(segment));
            if (!Finite(segment.Start.X) || !Finite(segment.Start.Y) || !Finite(segment.End.X) || !Finite(segment.End.Y))
                throw new ArgumentOutOfRangeException(nameof(segment), "Boundary coordinates must be finite.");
            if (segment.Start.DistanceTo(segment.End) <= tolerance)
                throw new ArgumentException("Boundary segment length must exceed the snap tolerance.", nameof(segment));
        }
    }
}
