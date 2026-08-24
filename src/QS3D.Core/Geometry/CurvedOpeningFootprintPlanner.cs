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
        private const int MaxCenterlinePoints = 8192;

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
            if (input.Centerline == null) throw new ArgumentException("Curved host centerline is required.", nameof(input.Centerline));
            var centerline = SnapshotCenterline(input.Centerline);
            Positive(input.OpeningWidthM, nameof(input.OpeningWidthM));
            Positive(input.HostThicknessM, nameof(input.HostThicknessM));
            NonNegative(input.ClearanceM, nameof(input.ClearanceM));
            Positive(input.MaximumCenterlineOffsetM, nameof(input.MaximumCenterlineOffsetM));
            NonNegative(input.AmbiguityMarginM, nameof(input.AmbiguityMarginM));
            if (!Finite(input.MiterLimit) || input.MiterLimit < 1d) throw new ArgumentOutOfRangeException(nameof(input.MiterLimit));
            Positive(input.ToleranceM, nameof(input.ToleranceM));
            Validate(input.OpeningPoint, "opening point");

            var segments = BuildSegments(centerline, input.ToleranceM);
            var lastSegment = segments[segments.Count - 1];
            var totalLength = AddAdvancing(lastSegment.StationStart, lastSegment.Length, "curved host centerline length");
            var projections = segments.Select(x => Project(input.OpeningPoint, x)).OrderBy(x => x.Distance).ThenBy(x => x.Station).ToList();
            var best = projections[0];
            if (best.Distance > input.MaximumCenterlineOffsetM)
                throw new InvalidOperationException("Opening point lies too far from the curved host centerline.");

            var ambiguityLimit = Add(best.Distance, input.AmbiguityMarginM, "curved host ambiguity distance");
            var competing = projections.Skip(1)
                .Where(x => Math.Abs(x.Segment.Index - best.Segment.Index) > 1)
                .FirstOrDefault(x => x.Distance <= ambiguityLimit);
            if (competing != null)
                throw new InvalidOperationException("Opening point is ambiguous between non-adjacent curved-host branches.");

            var halfWidth = input.OpeningWidthM / 2d;
            var startStation = Subtract(best.Station, halfWidth, "curved opening start station");
            var endStation = Add(best.Station, halfWidth, "curved opening end station");
            if (!(startStation < best.Station) || !(endStation > best.Station))
                throw new OverflowException("Curved opening width is below the representable station resolution.");
            var maximumEndStation = Add(totalLength, input.ToleranceM, "curved host end tolerance");
            if (startStation < -input.ToleranceM || endStation > maximumEndStation)
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

        private static IReadOnlyList<Point2> SnapshotCenterline(IReadOnlyList<Point2> source)
        {
            var count = source.Count;
            if (count < 2) throw new ArgumentException("Curved host centerline requires at least two points.", nameof(source));
            if (count > MaxCenterlinePoints) throw new InvalidOperationException("Curved host centerline exceeds the supported point budget of " + MaxCenterlinePoints + ".");

            var snapshot = new Point2[count];
            for (var i = 0; i < count; i++) snapshot[i] = source[i];
            if (source.Count != count)
                throw new InvalidOperationException("Curved host centerline cardinality changed while it was being read.");
            return snapshot;
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
                var endStation = AddAdvancing(station, length, "curved host centerline length");
                segments.Add(new Segment { Index = i - 1, Start = points[i - 1], End = points[i], Length = length, StationStart = station });
                station = endStation;
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
            var station = StationAtRatio(segment, t, "curved host projection station");
            return new Projection { Segment = segment, T = t, Point = projected, Distance = distance, Station = station };
        }

        private static double StationAtRatio(Segment segment, double ratio, string label)
        {
            var endStation = AddAdvancing(segment.StationStart, segment.Length, label + " segment end");
            if (ratio <= 0d) return segment.StationStart;
            if (ratio >= 1d) return endStation;
            if (!Finite(ratio)) throw new OverflowException(label + " ratio is invalid.");

            var offset = segment.Length * ratio;
            if (!Finite(offset) || !(offset > 0d) || !(offset < segment.Length))
                throw new OverflowException(label + " interior offset is below the supported numeric precision.");
            var station = Add(segment.StationStart, offset, label);
            if (!(station > segment.StationStart) || !(station < endStation))
                throw new OverflowException(label + " interior station is below the representable station resolution.");
            return station;
        }

        private static IReadOnlyList<Point2> Slice(IReadOnlyList<Segment> segments, double startStation, double endStation, double tolerance)
        {
            if (!(endStation > startStation)) throw new InvalidOperationException("Opening cutter centerline range is degenerate.");
            var result = new List<Point2> { PointAtStation(segments, startStation) };
            var interiorStartStation = Add(startStation, tolerance, "curved opening interior start tolerance");
            var interiorEndStation = Subtract(endStation, tolerance, "curved opening interior end tolerance");
            foreach (var segment in segments)
            {
                var vertexStation = AddAdvancing(segment.StationStart, segment.Length, "curved host vertex station");
                if (vertexStation > interiorStartStation && vertexStation < interiorEndStation)
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
                var end = AddAdvancing(segment.StationStart, segment.Length, "curved host segment end station");
                if (station > end && segment.Index < segments.Count - 1) continue;

                double t;
                var interior = station > segment.StationStart && station < end;
                if (station <= segment.StationStart) t = 0d;
                else if (station >= end) t = 1d;
                else
                {
                    var stationOffset = Subtract(station, segment.StationStart, "curved host station offset");
                    t = stationOffset / segment.Length;
                    if (!Finite(t) || !(t > 0d) || !(t < 1d)) throw new OverflowException("Curved host station interpolation is below the supported numeric precision.");
                }

                var point = new Point2(segment.Start.X + (segment.End.X - segment.Start.X) * t, segment.Start.Y + (segment.End.Y - segment.Start.Y) * t);
                Validate(point, "curved host station point");
                if (interior && (point.Equals(segment.Start) || point.Equals(segment.End))) throw new OverflowException("Curved host station point collapsed to a segment endpoint at floating-point precision.");
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

        private static double AddAdvancing(double left, double positiveRight, string label)
        {
            var value = Add(left, positiveRight, label);
            if (!(positiveRight > 0d) || !(value > left)) throw new OverflowException(label + " lost a positive station increment at floating-point precision.");
            return value;
        }

        private static double Add(double left, double right, string label)
        {
            if (!Finite(left) || !Finite(right)) throw new OverflowException(label + " contains a non-finite value.");
            var value = left + right;
            if (!Finite(value)) throw new OverflowException(label + " overflowed.");
            return value;
        }

        private static double Subtract(double left, double right, string label)
        {
            if (!Finite(left) || !Finite(right)) throw new OverflowException(label + " contains a non-finite value.");
            var value = left - right;
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
