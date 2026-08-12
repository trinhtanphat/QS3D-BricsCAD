using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class CurvedOpeningFootprintInput
    {
        public IReadOnlyList<Point2> Centerline { get; set; } = Array.Empty<Point2>();
        public Point2 OpeningPoint { get; set; }
        public double OpeningWidthM { get; set; }
        public double HostThicknessM { get; set; }
        public double ClearanceM { get; set; } = 0.01d;
        public double MaximumCenterlineOffsetM { get; set; } = 0.35d;
        public double AmbiguityMarginM { get; set; } = 0.01d;
        public double MiterLimit { get; set; } = 4d;
        public double ToleranceM { get; set; } = 1e-9d;
    }

    public sealed class CurvedOpeningFootprintPlan
    {
        public IReadOnlyList<Point2> CutterPolygon { get; set; } = Array.Empty<Point2>();
        public IReadOnlyList<Point2> CutterCenterline { get; set; } = Array.Empty<Point2>();
        public double HostCenterlineLengthM { get; set; }
        public double CenterStationM { get; set; }
        public double StartStationM { get; set; }
        public double EndStationM { get; set; }
        public double CenterlineOffsetM { get; set; }
        public int ProjectionSegmentIndex { get; set; }
        public double CutterFootprintAreaM2 { get; set; }
    }

    public static class CurvedOpeningFootprintPlanner
    {
        private sealed class Segment
        {
            public int Index { get; set; }
            public Point2 Start { get; set; }
            public Point2 End { get; set; }
            public double Length { get; set; }
            public double StationStart { get; set; }
        }

        private sealed class Projection
        {
            public Segment Segment { get; set; } = null!;
            public double T { get; set; }
            public Point2 Point { get; set; }
            public double Distance { get; set; }
            public double Station { get; set; }
        }

        public static CurvedOpeningFootprintPlan Plan(CurvedOpeningFootprintInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Centerline == null || input.Centerline.Count < 2) throw new ArgumentException("Curved host centerline requires at least two points.", nameof(input.Centerline));
            Positive(input.OpeningWidthM, nameof(input.OpeningWidthM));
            Positive(input.HostThicknessM, nameof(input.HostThicknessM));
            NonNegative(input.ClearanceM, nameof(input.ClearanceM));
            Positive(input.MaximumCenterlineOffsetM, nameof(input.MaximumCenterlineOffsetM));
            NonNegative(input.AmbiguityMarginM, nameof(input.AmbiguityMarginM));
            if (!Finite(input.MiterLimit) || input.MiterLimit < 1d) throw new ArgumentOutOfRangeException(nameof(input.MiterLimit));
            Positive(input.ToleranceM, nameof(input.ToleranceM));
            Validate(input.OpeningPoint, "opening point");

            var segments = BuildSegments(input.Centerline, input.ToleranceM);
            var totalLength = segments[segments.Count - 1].StationStart + segments[segments.Count - 1].Length;
            var projections = segments.Select(x => Project(input.OpeningPoint, x)).OrderBy(x => x.Distance).ThenBy(x => x.Station).ToList();
            var best = projections[0];
            if (best.Distance > input.MaximumCenterlineOffsetM)
                throw new InvalidOperationException("Opening point lies too far from the curved host centerline.");

            var competing = projections.Skip(1)
                .Where(x => Math.Abs(x.Segment.Index - best.Segment.Index) > 1)
                .FirstOrDefault(x => x.Distance <= best.Distance + input.AmbiguityMarginM);
            if (competing != null)
                throw new InvalidOperationException("Opening point is ambiguous between non-adjacent curved-host branches.");

            var halfWidth = input.OpeningWidthM / 2d;
            var startStation = best.Station - halfWidth;
            var endStation = best.Station + halfWidth;
            if (startStation < -input.ToleranceM || endStation > totalLength + input.ToleranceM)
                throw new InvalidOperationException("Opening width/position extends beyond the curved host centerline.");
            startStation = Math.Max(0d, startStation);
            endStation = Math.Min(totalLength, endStation);

            var cutterCenterline = Slice(segments, startStation, endStation, input.ToleranceM);
            var cutterDepth = input.HostThicknessM + input.ClearanceM * 2d;
            if (!Finite(cutterDepth)) throw new OverflowException("Opening cutter thickness overflowed.");
            var footprint = new WallFootprintEngine().Build(cutterCenterline, cutterDepth, input.MiterLimit, input.ToleranceM);

            return new CurvedOpeningFootprintPlan
            {
                CutterPolygon = footprint.Polygon,
                CutterCenterline = cutterCenterline,
                HostCenterlineLengthM = totalLength,
                CenterStationM = best.Station,
                StartStationM = startStation,
                EndStationM = endStation,
                CenterlineOffsetM = best.Distance,
                ProjectionSegmentIndex = best.Segment.Index,
                CutterFootprintAreaM2 = footprint.Area
            };
        }

        private static List<Segment> BuildSegments(IReadOnlyList<Point2> points, double tolerance)
        {
            var segments = new List<Segment>(points.Count - 1);
            var station = 0d;
            for (var i = 0; i < points.Count; i++) Validate(points[i], "centerline point " + i);
            for (var i = 1; i < points.Count; i++)
            {
                var length = points[i - 1].DistanceTo(points[i]);
                if (!Finite(length) || length <= tolerance) throw new InvalidOperationException("Curved host centerline contains a degenerate segment.");
                segments.Add(new Segment { Index = i - 1, Start = points[i - 1], End = points[i], Length = length, StationStart = station });
                station = Add(station, length, "curved host centerline length");
            }
            return segments;
        }

        private static Projection Project(Point2 point, Segment segment)
        {
            var dx = segment.End.X - segment.Start.X;
            var dy = segment.End.Y - segment.Start.Y;
            var ux = dx / segment.Length;
            var uy = dy / segment.Length;
            if (!Finite(ux) || !Finite(uy)) throw new OverflowException("Curved host projection direction is invalid.");
            var px = point.X - segment.Start.X;
            var py = point.Y - segment.Start.Y;
            if (!Finite(px) || !Finite(py)) throw new OverflowException("Curved host projection offset overflowed.");
            var along = DotFinite(px, py, ux, uy, "curved host projection distance");
            var t = Math.Max(0d, Math.Min(1d, along / segment.Length));
            var projected = new Point2(segment.Start.X + dx * t, segment.Start.Y + dy * t);
            Validate(projected, "curved host projection");
            var distance = point.DistanceTo(projected);
            var station = Add(segment.StationStart, segment.Length * t, "curved host projection station");
            return new Projection { Segment = segment, T = t, Point = projected, Distance = distance, Station = station };
        }

        private static IReadOnlyList<Point2> Slice(IReadOnlyList<Segment> segments, double startStation, double endStation, double tolerance)
        {
            if (!(endStation > startStation)) throw new InvalidOperationException("Opening cutter centerline range is degenerate.");
            var result = new List<Point2> { PointAtStation(segments, startStation) };
            foreach (var segment in segments)
            {
                var vertexStation = segment.StationStart + segment.Length;
                if (vertexStation > startStation + tolerance && vertexStation < endStation - tolerance)
                    result.Add(segment.End);
            }
            result.Add(PointAtStation(segments, endStation));
            var cleaned = new List<Point2>(result.Count);
            foreach (var point in result)
                if (cleaned.Count == 0 || cleaned[cleaned.Count - 1].DistanceTo(point) > tolerance) cleaned.Add(point);
            if (cleaned.Count < 2) throw new InvalidOperationException("Opening cutter centerline collapsed after slicing.");
            return cleaned.AsReadOnly();
        }

        private static Point2 PointAtStation(IReadOnlyList<Segment> segments, double station)
        {
            foreach (var segment in segments)
            {
                var end = segment.StationStart + segment.Length;
                if (station > end && segment.Index < segments.Count - 1) continue;
                var t = Math.Max(0d, Math.Min(1d, (station - segment.StationStart) / segment.Length));
                var point = new Point2(segment.Start.X + (segment.End.X - segment.Start.X) * t, segment.Start.Y + (segment.End.Y - segment.Start.Y) * t);
                Validate(point, "curved host station point");
                return point;
            }
            return segments[segments.Count - 1].End;
        }

        private static double DotFinite(double ax, double ay, double bx, double by, string label)
        {
            var scale = Math.Max(Math.Abs(ax), Math.Abs(ay));
            if (!Finite(scale)) throw new OverflowException(label + " contains a non-finite value.");
            if (scale == 0d) return 0d;
            var normalized = ax / scale * bx + ay / scale * by;
            if (!Finite(normalized)) throw new OverflowException(label + " overflowed.");
            var value = normalized * scale;
            if (!Finite(value)) throw new OverflowException(label + " overflowed.");
            return value;
        }

        private static double Add(double left, double right, string label)
        {
            if (!Finite(left) || !Finite(right)) throw new OverflowException(label + " contains a non-finite value.");
            var value = left + right;
            if (!Finite(value)) throw new OverflowException(label + " overflowed.");
            return value;
        }

        private static void Positive(double value, string name)
        {
            if (!Finite(value) || value <= 0d) throw new ArgumentOutOfRangeException(name);
        }

        private static void NonNegative(double value, string name)
        {
            if (!Finite(value) || value < 0d) throw new ArgumentOutOfRangeException(name);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static void Validate(Point2 point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y)) throw new ArgumentOutOfRangeException(label, "Point coordinates must be finite.");
        }
    }
}
