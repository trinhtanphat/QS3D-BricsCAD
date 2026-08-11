using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class PolygonRegionSeed2
    {
        public PolygonRegionSeed2(
            string regionId,
            IReadOnlyList<Point2> outer,
            IReadOnlyList<IReadOnlyList<Point2>>? holes = null)
        {
            RegionId = (regionId ?? string.Empty).Trim();
            Outer = outer ?? throw new ArgumentNullException(nameof(outer));
            Holes = holes ?? Array.Empty<IReadOnlyList<Point2>>();
        }

        public string RegionId { get; }
        public IReadOnlyList<Point2> Outer { get; }
        public IReadOnlyList<IReadOnlyList<Point2>> Holes { get; }
    }

    public sealed class PolygonRegionIsland2
    {
        internal PolygonRegionIsland2(string regionId, PolygonRegion2 region)
        {
            RegionId = regionId ?? throw new ArgumentNullException(nameof(regionId));
            Region = region ?? throw new ArgumentNullException(nameof(region));
        }

        public string RegionId { get; }
        public PolygonRegion2 Region { get; }
    }

    public sealed class PolygonRegionSet2
    {
        internal PolygonRegionSet2(IReadOnlyList<PolygonRegionIsland2> islands)
        {
            Islands = islands ?? throw new ArgumentNullException(nameof(islands));
        }

        public IReadOnlyList<PolygonRegionIsland2> Islands { get; }
    }

    public sealed class PolygonRegionTaggedScanSegment
    {
        internal PolygonRegionTaggedScanSegment(string regionId, Point2 start, Point2 end)
        {
            RegionId = regionId ?? throw new ArgumentNullException(nameof(regionId));
            Start = start;
            End = end;
        }

        public string RegionId { get; }
        public Point2 Start { get; }
        public Point2 End { get; }
    }

    public static class PolygonRegionSetTopology
    {
        private const int MaxRegions = 256;
        private const int MaxTotalVertices = 65536;
        private const int MaxTaggedScanSegments = 16384;
        private const int MaxRegionIdLength = 160;
        private const double Epsilon = 1e-10d;

        public static PolygonRegionSet2 NormalizeAndValidate(IEnumerable<PolygonRegionSeed2> regions)
        {
            if (regions == null) throw new ArgumentNullException(nameof(regions));

            var materialized = regions.Take(MaxRegions + 1).ToList();
            if (materialized.Count == 0)
                throw new ArgumentException("Polygon multi-region topology requires at least one island.", nameof(regions));
            if (materialized.Count > MaxRegions)
                throw new ArgumentException("Polygon multi-region topology exceeds the supported " + MaxRegions + " island limit.", nameof(regions));

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var islands = new List<PolygonRegionIsland2>(materialized.Count);
            var totalVertices = 0;

            for (var i = 0; i < materialized.Count; i++)
            {
                var seed = materialized[i] ?? throw new ArgumentException("Polygon multi-region island cannot be null at index " + i + ".", nameof(regions));
                var id = NormalizeRegionId(seed.RegionId, i);
                if (!ids.Add(id))
                    throw new ArgumentException("Polygon multi-region island id is duplicated: " + id + ".", nameof(regions));

                var region = PolygonRegionScanlineClipper.NormalizeAndValidate(seed.Outer, seed.Holes);
                foreach (var loop in region.BoundaryLoops)
                {
                    totalVertices = checked(totalVertices + loop.Count);
                    if (totalVertices > MaxTotalVertices)
                        throw new ArgumentException("Polygon multi-region topology exceeds the supported " + MaxTotalVertices + " total vertex limit.", nameof(regions));
                }

                islands.Add(new PolygonRegionIsland2(id, region));
            }

            islands.Sort((left, right) =>
            {
                var comparison = StringComparer.OrdinalIgnoreCase.Compare(left.RegionId, right.RegionId);
                return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(left.RegionId, right.RegionId);
            });

            for (var i = 0; i < islands.Count; i++)
                for (var j = i + 1; j < islands.Count; j++)
                    ValidateIslandPair(islands[i], islands[j]);

            return new PolygonRegionSet2(islands.AsReadOnly());
        }

        public static IReadOnlyList<PolygonRegionTaggedScanSegment> Clip(
            PolygonRegionSet2 topology,
            PolygonScanAxis axis,
            double coordinate)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (!Finite(coordinate)) throw new ArgumentOutOfRangeException(nameof(coordinate));

            var result = new List<PolygonRegionTaggedScanSegment>();
            foreach (var island in topology.Islands)
            {
                var segments = PolygonRegionScanlineClipper.Clip(island.Region, axis, coordinate);
                foreach (var segment in segments)
                {
                    if (result.Count >= MaxTaggedScanSegments)
                        throw new InvalidOperationException("Polygon multi-region scanline exceeds the supported " + MaxTaggedScanSegments + " tagged segment limit.");
                    result.Add(new PolygonRegionTaggedScanSegment(island.RegionId, segment.Start, segment.End));
                }
            }

            return result.AsReadOnly();
        }

        private static string NormalizeRegionId(string regionId, int index)
        {
            var id = (regionId ?? string.Empty).Trim();
            if (id.Length == 0)
                throw new ArgumentException("Polygon multi-region island id is required at index " + index + ".");
            if (id.Length > MaxRegionIdLength)
                throw new ArgumentException("Polygon multi-region island id exceeds the supported " + MaxRegionIdLength + " character limit: " + id.Substring(0, MaxRegionIdLength) + "...");
            if (id.Any(char.IsControl))
                throw new ArgumentException("Polygon multi-region island id contains control characters: " + id + ".");
            return id;
        }

        private static void ValidateIslandPair(PolygonRegionIsland2 left, PolygonRegionIsland2 right)
        {
            var leftBounds = Bounds(left.Region.Outer);
            var rightBounds = Bounds(right.Region.Outer);
            if (!leftBounds.OverlapsOrTouches(rightBounds)) return;

            if (BoundariesIntersect(left.Region.Outer, right.Region.Outer))
                throw new ArgumentException("Polygon multi-region islands " + left.RegionId + " and " + right.RegionId + " intersect or touch. Each island requires a disjoint outer boundary.");

            var rightInLeft = LocatePoint(left.Region.Outer, right.Region.Outer[0]);
            var leftInRight = LocatePoint(right.Region.Outer, left.Region.Outer[0]);
            if (rightInLeft != PointLocation.Outside || leftInRight != PointLocation.Outside)
                throw new ArgumentException("Polygon multi-region islands " + left.RegionId + " and " + right.RegionId + " overlap or are nested. Nested outer islands require an explicit ownership/topology policy and are not treated as holes.");
        }

        private readonly struct Box2
        {
            public Box2(double minX, double minY, double maxX, double maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }

            public double MinX { get; }
            public double MinY { get; }
            public double MaxX { get; }
            public double MaxY { get; }

            public bool OverlapsOrTouches(Box2 other) =>
                MaxX >= other.MinX - Epsilon && other.MaxX >= MinX - Epsilon &&
                MaxY >= other.MinY - Epsilon && other.MaxY >= MinY - Epsilon;
        }

        private static Box2 Bounds(IReadOnlyList<Point2> polygon)
        {
            var minX = polygon[0].X;
            var minY = polygon[0].Y;
            var maxX = minX;
            var maxY = minY;
            for (var i = 1; i < polygon.Count; i++)
            {
                minX = Math.Min(minX, polygon[i].X);
                minY = Math.Min(minY, polygon[i].Y);
                maxX = Math.Max(maxX, polygon[i].X);
                maxY = Math.Max(maxY, polygon[i].Y);
            }
            return new Box2(minX, minY, maxX, maxY);
        }

        private enum PointLocation
        {
            Outside,
            Inside,
            Boundary
        }

        private static PointLocation LocatePoint(IReadOnlyList<Point2> polygon, Point2 point)
        {
            var inside = false;
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                if (OnSegment(a, point, b)) return PointLocation.Boundary;

                var crosses = (a.Y > point.Y) != (b.Y > point.Y);
                if (!crosses) continue;
                var x = a.X + (point.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                if (!Finite(x))
                    throw new OverflowException("Polygon multi-region point-in-polygon intersection is not finite.");
                if (x > point.X + Epsilon) inside = !inside;
                else if (Math.Abs(x - point.X) <= Epsilon) return PointLocation.Boundary;
            }
            return inside ? PointLocation.Inside : PointLocation.Outside;
        }

        private static bool BoundariesIntersect(IReadOnlyList<Point2> left, IReadOnlyList<Point2> right)
        {
            for (var i = 0; i < left.Count; i++)
            {
                var a = left[i];
                var b = left[(i + 1) % left.Count];
                for (var j = 0; j < right.Count; j++)
                {
                    var c = right[j];
                    var d = right[(j + 1) % right.Count];
                    if (SegmentsIntersect(a, b, c, d)) return true;
                }
            }
            return false;
        }

        private static bool SegmentsIntersect(Point2 a, Point2 b, Point2 c, Point2 d)
        {
            var o1 = Orientation(a, b, c);
            var o2 = Orientation(a, b, d);
            var o3 = Orientation(c, d, a);
            var o4 = Orientation(c, d, b);
            if (Opposite(o1, o2) && Opposite(o3, o4)) return true;
            if (Math.Abs(o1) <= Epsilon && OnSegment(a, c, b)) return true;
            if (Math.Abs(o2) <= Epsilon && OnSegment(a, d, b)) return true;
            if (Math.Abs(o3) <= Epsilon && OnSegment(c, a, d)) return true;
            if (Math.Abs(o4) <= Epsilon && OnSegment(c, b, d)) return true;
            return false;
        }

        private static double Orientation(Point2 a, Point2 b, Point2 c)
        {
            var value = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            if (!Finite(value))
                throw new OverflowException("Polygon multi-region orientation exceeds the supported numeric range.");
            return value;
        }

        private static bool OnSegment(Point2 a, Point2 point, Point2 b)
        {
            if (Math.Abs(Orientation(a, b, point)) > Epsilon) return false;
            return point.X >= Math.Min(a.X, b.X) - Epsilon && point.X <= Math.Max(a.X, b.X) + Epsilon &&
                   point.Y >= Math.Min(a.Y, b.Y) - Epsilon && point.Y <= Math.Max(a.Y, b.Y) + Epsilon;
        }

        private static bool Opposite(double left, double right) =>
            (left > Epsilon && right < -Epsilon) || (left < -Epsilon && right > Epsilon);

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
