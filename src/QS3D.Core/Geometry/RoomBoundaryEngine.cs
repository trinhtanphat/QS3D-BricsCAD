using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class BoundarySegment
    {
        public BoundarySegment(Point2 start, Point2 end, string sourceId = "")
        {
            Start = start;
            End = end;
            SourceId = sourceId?.Trim() ?? string.Empty;
        }

        public Point2 Start { get; }
        public Point2 End { get; }
        public string SourceId { get; }
    }

    public sealed class RoomBoundary
    {
        internal RoomBoundary(string key, IReadOnlyList<Point2> vertices, IReadOnlyList<string> sourceIds, double area, double perimeter)
        {
            Key = key;
            Vertices = vertices;
            SourceIds = sourceIds;
            Area = area;
            Perimeter = perimeter;
        }

        public string Key { get; }
        public IReadOnlyList<Point2> Vertices { get; }
        public IReadOnlyList<string> SourceIds { get; }
        public double Area { get; }
        public double Perimeter { get; }
    }

    public sealed class RoomBoundaryEngine
    {
        private const int MaxInputSegments = 5000;
        private const int MaxSubdividedEdges = 20000;

        public IReadOnlyList<RoomBoundary> Discover(IEnumerable<BoundarySegment> source, double tolerance = 0.001d, double minimumArea = 0.01d)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!FinitePositive(tolerance)) throw new ArgumentOutOfRangeException(nameof(tolerance));
            if (!FiniteNonNegative(minimumArea)) throw new ArgumentOutOfRangeException(nameof(minimumArea));

            var segments = source.Take(MaxInputSegments + 1).ToList();
            if (segments.Count > MaxInputSegments) throw new InvalidOperationException("Room boundary input exceeds the supported segment limit.");
            foreach (var segment in segments) ValidateSegment(segment, tolerance);
            if (segments.Count < 3) return Array.Empty<RoomBoundary>();

            var cuts = new List<Cut>[segments.Count];
            for (var i = 0; i < segments.Count; i++)
            {
                cuts[i] = new List<Cut>
                {
                    new Cut(0d, segments[i].Start),
                    new Cut(1d, segments[i].End)
                };
            }

            foreach (var pair in EnumeratePotentialPairs(segments, tolerance))
                CollectPairCuts(segments[pair.Item1], segments[pair.Item2], cuts[pair.Item1], cuts[pair.Item2], tolerance);

            var rawEdges = new List<RawEdge>();
            for (var i = 0; i < segments.Count; i++)
            {
                var ordered = DeduplicateCuts(cuts[i], segments[i], tolerance);
                for (var k = 1; k < ordered.Count; k++)
                {
                    var a = ordered[k - 1].Point;
                    var b = ordered[k].Point;
                    if (a.DistanceTo(b) <= tolerance * 0.25d) continue;
                    rawEdges.Add(new RawEdge(a, b, segments[i].SourceId));
                    if (rawEdges.Count > MaxSubdividedEdges) throw new InvalidOperationException("Room boundary subdivision exceeds the supported edge limit.");
                }
            }

            if (rawEdges.Count < 3) return Array.Empty<RoomBoundary>();
            var graph = BuildGraph(rawEdges, tolerance);
            if (graph.Edges.Count < 3) return Array.Empty<RoomBoundary>();

            var bridges = FindBridges(graph.Vertices.Count, graph.Edges);
            var cyclicEdges = graph.Edges.Where((edge, index) => !bridges.Contains(index)).ToList();
            if (cyclicEdges.Count < 3) return Array.Empty<RoomBoundary>();

            var adjacency = BuildAdjacency(graph.Vertices, cyclicEdges);
            var sourceLookup = BuildSourceLookup(cyclicEdges);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<RoomBoundary>();
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            for (var edgeIndex = 0; edgeIndex < cyclicEdges.Count; edgeIndex++)
            {
                var edge = cyclicEdges[edgeIndex];
                Trace(edge.A, edge.B);
                Trace(edge.B, edge.A);
            }

            return result.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();

            void Trace(int startA, int startB)
            {
                var startKey = DirectedKey(startA, startB);
                if (visited.Contains(startKey)) return;

                var cycle = new List<int>();
                var boundarySources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var a = startA;
                var b = startB;
                var closed = false;
                var guard = cyclicEdges.Count * 2 + 4;

                for (var step = 0; step < guard; step++)
                {
                    var key = DirectedKey(a, b);
                    if (visited.Contains(key)) break;
                    visited.Add(key);
                    cycle.Add(a);
                    AddSources(a, b, sourceLookup, boundarySources);

                    var next = NextFaceVertex(a, b, adjacency);
                    if (next < 0) break;
                    a = b;
                    b = next;
                    if (a == startA && b == startB) { closed = true; break; }
                }

                if (!closed || cycle.Count < 3) return;
                var points = cycle.Select(index => graph.Vertices[index]).ToList();
                var signedArea = PolylineMetrics.SignedArea(points);
                if (!(signedArea > minimumArea)) return;
                var perimeter = PolylineMetrics.Length(points, true);
                if (!FinitePositive(perimeter)) return;
                var boundaryKey = BuildBoundaryKey(points, tolerance);
                if (!seenKeys.Add(boundaryKey)) return;
                result.Add(new RoomBoundary(
                    boundaryKey,
                    points.AsReadOnly(),
                    boundarySources.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly(),
                    signedArea,
                    perimeter));
            }
        }

        private static IEnumerable<Tuple<int, int>> EnumeratePotentialPairs(IReadOnlyList<BoundarySegment> segments, double tolerance)
        {
            var ordered = new List<SegmentBounds>(segments.Count);
            for (var index = 0; index < segments.Count; index++) ordered.Add(new SegmentBounds(index, segments[index], tolerance));
            ordered.Sort((left, right) =>
            {
                var compare = left.MinX.CompareTo(right.MinX);
                if (compare != 0) return compare;
                compare = left.MaxX.CompareTo(right.MaxX);
                if (compare != 0) return compare;
                compare = left.MinY.CompareTo(right.MinY);
                if (compare != 0) return compare;
                compare = left.MaxY.CompareTo(right.MaxY);
                return compare != 0 ? compare : left.Index.CompareTo(right.Index);
            });

            var active = new List<SegmentBounds>();
            foreach (var current in ordered)
            {
                for (var index = active.Count - 1; index >= 0; index--)
                    if (active[index].MaxX < current.MinX) active.RemoveAt(index);

                foreach (var other in active)
                {
                    if (!other.Overlaps(current)) continue;
                    var first = Math.Min(other.Index, current.Index);
                    var second = Math.Max(other.Index, current.Index);
                    yield return Tuple.Create(first, second);
                }

                active.Add(current);
            }
        }

        private static void ValidateSegment(BoundarySegment segment, double tolerance)
        {
            if (segment == null) throw new ArgumentException("Room boundary input cannot contain null segments.", nameof(segment));
            if (!Finite(segment.Start.X) || !Finite(segment.Start.Y) || !Finite(segment.End.X) || !Finite(segment.End.Y))
                throw new ArgumentOutOfRangeException(nameof(segment), "Room boundary coordinates must be finite.");
            if (segment.Start.DistanceTo(segment.End) <= tolerance * 0.25d)
                throw new ArgumentException("Room boundary segment is degenerate.", nameof(segment));
        }

        private static void CollectPairCuts(BoundarySegment first, BoundarySegment second, ICollection<Cut> firstCuts, ICollection<Cut> secondCuts, double tolerance)
        {
            var ax = first.Start.X; var ay = first.Start.Y;
            var bx = first.End.X; var by = first.End.Y;
            var cx = second.Start.X; var cy = second.Start.Y;
            var dx = second.End.X; var dy = second.End.Y;
            var rx = bx - ax; var ry = by - ay;
            var sx = dx - cx; var sy = dy - cy;
            var denominator = Cross(rx, ry, sx, sy);
            var qpx = cx - ax; var qpy = cy - ay;
            var scale = Math.Max(Math.Max(Math.Abs(rx), Math.Abs(ry)), Math.Max(Math.Abs(sx), Math.Abs(sy)));
            var epsilon = Math.Max(1e-12d, tolerance * Math.Max(1d, scale) * 1e-6d);

            if (Math.Abs(denominator) > epsilon)
            {
                var t = Cross(qpx, qpy, sx, sy) / denominator;
                var u = Cross(qpx, qpy, rx, ry) / denominator;
                var firstParamTolerance = tolerance / Math.Max(first.Start.DistanceTo(first.End), tolerance);
                var secondParamTolerance = tolerance / Math.Max(second.Start.DistanceTo(second.End), tolerance);
                if (t >= -firstParamTolerance && t <= 1d + firstParamTolerance && u >= -secondParamTolerance && u <= 1d + secondParamTolerance)
                {
                    t = Clamp01(t); u = Clamp01(u);
                    var p = new Point2(ax + rx * t, ay + ry * t);
                    firstCuts.Add(new Cut(t, p));
                    secondCuts.Add(new Cut(u, p));
                }
                return;
            }

            if (Math.Abs(Cross(qpx, qpy, rx, ry)) > epsilon) return;
            AddEndpointCut(second.Start, first, firstCuts, tolerance);
            AddEndpointCut(second.End, first, firstCuts, tolerance);
            AddEndpointCut(first.Start, second, secondCuts, tolerance);
            AddEndpointCut(first.End, second, secondCuts, tolerance);
        }

        private static void AddEndpointCut(Point2 point, BoundarySegment segment, ICollection<Cut> cuts, double tolerance)
        {
            var dx = segment.End.X - segment.Start.X;
            var dy = segment.End.Y - segment.Start.Y;
            var lengthSquared = dx * dx + dy * dy;
            if (!(lengthSquared > 0d)) return;
            var t = ((point.X - segment.Start.X) * dx + (point.Y - segment.Start.Y) * dy) / lengthSquared;
            if (t < 0d || t > 1d) return;
            var projected = new Point2(segment.Start.X + dx * t, segment.Start.Y + dy * t);
            if (projected.DistanceTo(point) <= tolerance) cuts.Add(new Cut(Clamp01(t), projected));
        }

        private static IReadOnlyList<Cut> DeduplicateCuts(IEnumerable<Cut> source, BoundarySegment segment, double tolerance)
        {
            var paramTolerance = tolerance / Math.Max(segment.Start.DistanceTo(segment.End), tolerance) * 0.5d;
            var ordered = source.OrderBy(x => x.T).ToList();
            var result = new List<Cut>();
            foreach (var cut in ordered)
            {
                if (result.Count == 0 || Math.Abs(cut.T - result[result.Count - 1].T) > paramTolerance) result.Add(cut);
            }
            return result;
        }

        private static Graph BuildGraph(IReadOnlyList<RawEdge> rawEdges, double tolerance)
        {
            var pointRefs = new List<PointRef>(rawEdges.Count * 2);
            for (var i = 0; i < rawEdges.Count; i++)
            {
                pointRefs.Add(new PointRef(i, true, rawEdges[i].A));
                pointRefs.Add(new PointRef(i, false, rawEdges[i].B));
            }
            pointRefs.Sort((x, y) => ComparePoints(x.Point, y.Point));

            var snapper = new PointSnapper(tolerance);
            var edgeVertices = new int[rawEdges.Count, 2];
            foreach (var pointRef in pointRefs)
            {
                var vertex = snapper.GetOrAdd(pointRef.Point);
                edgeVertices[pointRef.EdgeIndex, pointRef.IsStart ? 0 : 1] = vertex;
            }

            var edgeMap = new Dictionary<string, Edge>(StringComparer.Ordinal);
            for (var i = 0; i < rawEdges.Count; i++)
            {
                var a = edgeVertices[i, 0]; var b = edgeVertices[i, 1];
                if (a == b) continue;
                var key = UndirectedKey(a, b);
                if (!edgeMap.TryGetValue(key, out var edge))
                {
                    if (a > b) { var temp = a; a = b; b = temp; }
                    edge = new Edge(a, b);
                    edgeMap[key] = edge;
                }
                if (!string.IsNullOrWhiteSpace(rawEdges[i].SourceId)) edge.SourceIds.Add(rawEdges[i].SourceId);
            }

            return new Graph(snapper.Points, edgeMap.Values.OrderBy(x => x.A).ThenBy(x => x.B).ToList());
        }

        private static HashSet<int> FindBridges(int vertexCount, IReadOnlyList<Edge> edges)
        {
            var adjacency = new List<Tuple<int, int>>[vertexCount];
            for (var i = 0; i < vertexCount; i++) adjacency[i] = new List<Tuple<int, int>>();
            for (var i = 0; i < edges.Count; i++)
            {
                adjacency[edges[i].A].Add(Tuple.Create(edges[i].B, i));
                adjacency[edges[i].B].Add(Tuple.Create(edges[i].A, i));
            }

            var discovery = Enumerable.Repeat(-1, vertexCount).ToArray();
            var low = new int[vertexCount];
            var parentVertex = Enumerable.Repeat(-1, vertexCount).ToArray();
            var parentEdge = Enumerable.Repeat(-1, vertexCount).ToArray();
            var nextNeighbor = new int[vertexCount];
            var time = 0;
            var bridges = new HashSet<int>();
            var stack = new Stack<int>();

            for (var root = 0; root < vertexCount; root++)
            {
                if (discovery[root] >= 0) continue;
                discovery[root] = low[root] = time++;
                stack.Push(root);

                while (stack.Count > 0)
                {
                    var vertex = stack.Peek();
                    if (nextNeighbor[vertex] < adjacency[vertex].Count)
                    {
                        var item = adjacency[vertex][nextNeighbor[vertex]++];
                        var neighbor = item.Item1;
                        var edgeIndex = item.Item2;
                        if (edgeIndex == parentEdge[vertex]) continue;

                        if (discovery[neighbor] < 0)
                        {
                            parentVertex[neighbor] = vertex;
                            parentEdge[neighbor] = edgeIndex;
                            discovery[neighbor] = low[neighbor] = time++;
                            stack.Push(neighbor);
                        }
                        else
                        {
                            low[vertex] = Math.Min(low[vertex], discovery[neighbor]);
                        }
                        continue;
                    }

                    stack.Pop();
                    var parent = parentVertex[vertex];
                    if (parent < 0) continue;
                    low[parent] = Math.Min(low[parent], low[vertex]);
                    if (low[vertex] > discovery[parent]) bridges.Add(parentEdge[vertex]);
                }
            }
            return bridges;
        }

        private static IReadOnlyList<int>[] BuildAdjacency(IReadOnlyList<Point2> vertices, IReadOnlyList<Edge> edges)
        {
            var work = new List<int>[vertices.Count];
            for (var i = 0; i < vertices.Count; i++) work[i] = new List<int>();
            foreach (var edge in edges) { work[edge.A].Add(edge.B); work[edge.B].Add(edge.A); }
            var result = new IReadOnlyList<int>[vertices.Count];
            for (var i = 0; i < vertices.Count; i++)
            {
                var origin = vertices[i];
                work[i].Sort((a, b) =>
                {
                    var aa = Math.Atan2(vertices[a].Y - origin.Y, vertices[a].X - origin.X);
                    var bb = Math.Atan2(vertices[b].Y - origin.Y, vertices[b].X - origin.X);
                    var compare = aa.CompareTo(bb);
                    return compare != 0 ? compare : a.CompareTo(b);
                });
                result[i] = work[i].AsReadOnly();
            }
            return result;
        }

        private static int NextFaceVertex(int previous, int current, IReadOnlyList<int>[] adjacency)
        {
            var neighbors = adjacency[current];
            if (neighbors.Count == 0) return -1;
            var reverseIndex = -1;
            for (var i = 0; i < neighbors.Count; i++) if (neighbors[i] == previous) { reverseIndex = i; break; }
            if (reverseIndex < 0) return -1;
            return neighbors[(reverseIndex - 1 + neighbors.Count) % neighbors.Count];
        }

        private static IReadOnlyDictionary<string, ISet<string>> BuildSourceLookup(IEnumerable<Edge> edges)
        {
            var result = new Dictionary<string, ISet<string>>(StringComparer.Ordinal);
            foreach (var edge in edges) result[UndirectedKey(edge.A, edge.B)] = edge.SourceIds;
            return result;
        }

        private static void AddSources(int a, int b, IReadOnlyDictionary<string, ISet<string>> sourceLookup, ISet<string> target)
        {
            if (!sourceLookup.TryGetValue(UndirectedKey(a, b), out var sources)) return;
            foreach (var source in sources) target.Add(source);
        }

        private static string BuildBoundaryKey(IReadOnlyList<Point2> points, double tolerance)
        {
            var tokens = points.Select(p => QuantizedToken(p, tolerance)).ToList();
            var forward = CanonicalRotation(tokens);
            var reversedTokens = tokens.AsEnumerable().Reverse().ToList();
            var reverse = CanonicalRotation(reversedTokens);
            return string.CompareOrdinal(forward, reverse) <= 0 ? forward : reverse;
        }

        private static string CanonicalRotation(IReadOnlyList<string> tokens)
        {
            if (tokens.Count == 0) return string.Empty;

            var first = 0;
            var second = 1;
            var offset = 0;
            while (first < tokens.Count && second < tokens.Count && offset < tokens.Count)
            {
                var compare = CompareRotationToken(tokens[(first + offset) % tokens.Count], tokens[(second + offset) % tokens.Count]);
                if (compare == 0)
                {
                    offset++;
                    continue;
                }

                if (compare > 0)
                {
                    first += offset + 1;
                    if (first <= second) first = second + 1;
                }
                else
                {
                    second += offset + 1;
                    if (second <= first) second = first + 1;
                }
                offset = 0;
            }

            var start = Math.Min(first, second);
            var ordered = new string[tokens.Count];
            for (var index = 0; index < tokens.Count; index++) ordered[index] = tokens[(start + index) % tokens.Count];
            return string.Join("|", ordered);
        }

        private static int CompareRotationToken(string left, string right)
        {
            var leftLength = left.Length + 1;
            var rightLength = right.Length + 1;
            var commonLength = Math.Min(leftLength, rightLength);
            for (var index = 0; index < commonLength; index++)
            {
                var leftChar = index < left.Length ? left[index] : '|';
                var rightChar = index < right.Length ? right[index] : '|';
                var compare = leftChar.CompareTo(rightChar);
                if (compare != 0) return compare;
            }
            return leftLength.CompareTo(rightLength);
        }

        private static string QuantizedToken(Point2 point, double tolerance)
        {
            var x = Math.Round(point.X / tolerance, MidpointRounding.AwayFromZero);
            var y = Math.Round(point.Y / tolerance, MidpointRounding.AwayFromZero);
            return x.ToString("0", CultureInfo.InvariantCulture) + "," + y.ToString("0", CultureInfo.InvariantCulture);
        }

        private static string DirectedKey(int a, int b) => a.ToString(CultureInfo.InvariantCulture) + ">" + b.ToString(CultureInfo.InvariantCulture);
        private static string UndirectedKey(int a, int b)
        {
            if (a > b) { var temp = a; a = b; b = temp; }
            return a.ToString(CultureInfo.InvariantCulture) + ":" + b.ToString(CultureInfo.InvariantCulture);
        }
        private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;
        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool FinitePositive(double value) => Finite(value) && value > 0d;
        private static bool FiniteNonNegative(double value) => Finite(value) && value >= 0d;
        private static int ComparePoints(Point2 a, Point2 b) { var x = a.X.CompareTo(b.X); return x != 0 ? x : a.Y.CompareTo(b.Y); }

        private sealed class SegmentBounds
        {
            public SegmentBounds(int index, BoundarySegment segment, double tolerance)
            {
                Index = index;
                MinX = ExpandDown(Math.Min(segment.Start.X, segment.End.X), tolerance);
                MaxX = ExpandUp(Math.Max(segment.Start.X, segment.End.X), tolerance);
                MinY = ExpandDown(Math.Min(segment.Start.Y, segment.End.Y), tolerance);
                MaxY = ExpandUp(Math.Max(segment.Start.Y, segment.End.Y), tolerance);
            }

            public int Index { get; }
            public double MinX { get; }
            public double MaxX { get; }
            public double MinY { get; }
            public double MaxY { get; }

            public bool Overlaps(SegmentBounds other) =>
                other != null && MaxX >= other.MinX && other.MaxX >= MinX && MaxY >= other.MinY && other.MaxY >= MinY;

            private static double ExpandDown(double value, double tolerance)
            {
                var result = value - tolerance;
                return double.IsNegativeInfinity(result) ? double.MinValue : result;
            }

            private static double ExpandUp(double value, double tolerance)
            {
                var result = value + tolerance;
                return double.IsPositiveInfinity(result) ? double.MaxValue : result;
            }
        }

        private sealed class PointSnapper
        {
            private readonly double _tolerance;
            private readonly Dictionary<string, List<int>> _cells = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            private readonly List<Point2> _points = new List<Point2>();
            public PointSnapper(double tolerance) { _tolerance = tolerance; }
            public IReadOnlyList<Point2> Points => _points.AsReadOnly();

            public int GetOrAdd(Point2 point)
            {
                var cellX = Cell(point.X); var cellY = Cell(point.Y);
                var best = -1; var bestDistance = double.MaxValue;
                for (var x = cellX - 1; x <= cellX + 1; x++)
                    for (var y = cellY - 1; y <= cellY + 1; y++)
                        if (_cells.TryGetValue(CellKey(x, y), out var candidates))
                            foreach (var index in candidates)
                            {
                                var distance = _points[index].DistanceTo(point);
                                if (distance <= _tolerance && (distance < bestDistance || (Math.Abs(distance - bestDistance) <= 1e-15d && index < best)))
                                { best = index; bestDistance = distance; }
                            }
                if (best >= 0) return best;
                var created = _points.Count;
                _points.Add(point);
                var key = CellKey(cellX, cellY);
                if (!_cells.TryGetValue(key, out var list)) { list = new List<int>(); _cells[key] = list; }
                list.Add(created);
                return created;
            }

            private long Cell(double value) => checked((long)Math.Floor(value / _tolerance));
            private static string CellKey(long x, long y) => x.ToString(CultureInfo.InvariantCulture) + ":" + y.ToString(CultureInfo.InvariantCulture);
        }

        private sealed class Cut { public Cut(double t, Point2 point) { T = t; Point = point; } public double T { get; } public Point2 Point { get; } }
        private sealed class RawEdge { public RawEdge(Point2 a, Point2 b, string sourceId) { A = a; B = b; SourceId = sourceId; } public Point2 A { get; } public Point2 B { get; } public string SourceId { get; } }
        private sealed class Edge { public Edge(int a, int b) { A = a; B = b; } public int A { get; } public int B { get; } public ISet<string> SourceIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
        private sealed class Graph { public Graph(IReadOnlyList<Point2> vertices, IReadOnlyList<Edge> edges) { Vertices = vertices; Edges = edges; } public IReadOnlyList<Point2> Vertices { get; } public IReadOnlyList<Edge> Edges { get; } }
        private sealed class PointRef { public PointRef(int edgeIndex, bool isStart, Point2 point) { EdgeIndex = edgeIndex; IsStart = isStart; Point = point; } public int EdgeIndex { get; } public bool IsStart { get; } public Point2 Point { get; } }
    }
}
