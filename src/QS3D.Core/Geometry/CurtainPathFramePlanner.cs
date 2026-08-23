using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class CurtainPathFramePiece
    {
        internal CurtainPathFramePiece(
            int sourceFrameIndex,
            int pathSegmentIndex,
            double stationStartM,
            double stationEndM,
            double centerX_M,
            double centerY_M,
            double angleRadians,
            double zM,
            double heightM)
        {
            SourceFrameIndex = sourceFrameIndex;
            PathSegmentIndex = pathSegmentIndex;
            StationStartM = stationStartM;
            StationEndM = stationEndM;
            CenterX_M = centerX_M;
            CenterY_M = centerY_M;
            AngleRadians = angleRadians;
            Z_M = zM;
            HeightM = heightM;
        }

        public int SourceFrameIndex { get; }
        public int PathSegmentIndex { get; }
        public double StationStartM { get; }
        public double StationEndM { get; }
        public double CenterX_M { get; }
        public double CenterY_M { get; }
        public double AngleRadians { get; }
        public double Z_M { get; }
        public double HeightM { get; }
        public double WidthM => StationEndM - StationStartM;
    }

    public sealed class CurtainPathFramePlan
    {
        internal CurtainPathFramePlan(double pathLengthM, int pathSegmentCount, int sourceFrameCount, IReadOnlyList<CurtainPathFramePiece> pieces)
        {
            PathLengthM = pathLengthM;
            PathSegmentCount = pathSegmentCount;
            SourceFrameCount = sourceFrameCount;
            Pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
        }

        public double PathLengthM { get; }
        public int PathSegmentCount { get; }
        public int SourceFrameCount { get; }
        public IReadOnlyList<CurtainPathFramePiece> Pieces { get; }
    }

    public sealed class CurtainPathProjection
    {
        internal CurtainPathProjection(double stationM, double distanceM, Point2 point, int pathSegmentIndex)
        {
            StationM = stationM;
            DistanceM = distanceM;
            Point = point;
            PathSegmentIndex = pathSegmentIndex;
        }

        public double StationM { get; }
        public double DistanceM { get; }
        public Point2 Point { get; }
        public int PathSegmentIndex { get; }
    }

    public static class CurtainPathFramePlanner
    {
        private const int MaxPathPoints = 8192;
        private const int MaxPieces = 20000;
        private const double Tolerance = 1e-10d;

        public static CurtainPathFramePlan Plan(IReadOnlyList<Point2> centerline, IReadOnlyList<CurtainWallRect> frames)
        {
            if (centerline == null) throw new ArgumentNullException(nameof(centerline));
            if (frames == null) throw new ArgumentNullException(nameof(frames));
            var sourceFrameCount = frames.Count;
            if (sourceFrameCount < 0)
                throw new InvalidOperationException("Curtain path frame input Count cannot be negative.");
            if (sourceFrameCount > MaxPieces)
                throw new InvalidOperationException("Curtain path frame input cannot exceed " + MaxPieces + " rectangles.");
            var path = BuildPath(centerline);
            var pieces = new List<CurtainPathFramePiece>();

            for (var frameIndex = 0; frameIndex < sourceFrameCount; frameIndex++)
            {
                var frame = frames[frameIndex] ?? throw new InvalidOperationException("Curtain frame rectangle cannot be null.");
                var start = Finite(frame.X_M, "curtain frame start station");
                var width = Positive(frame.WidthM, "curtain frame width");
                var z = Finite(frame.Z_M, "curtain frame elevation");
                var height = Positive(frame.HeightM, "curtain frame height");
                if (start < -Tolerance || z < -Tolerance)
                    throw new InvalidOperationException("Curtain frame rectangle starts outside the supported path/elevation extent.");

                var end = Add(start, width, "curtain frame end station");
                var stationTolerance = StationComparisonTolerance(path.TotalLengthM);
                if (start > path.TotalLengthM + stationTolerance || end > path.TotalLengthM + stationTolerance)
                    throw new InvalidOperationException("Curtain frame rectangle exceeds the host path length.");
                start = Math.Max(0d, Math.Min(path.TotalLengthM, start));
                end = Math.Max(0d, Math.Min(path.TotalLengthM, end));
                if (!(end > start)) throw new InvalidOperationException("Curtain frame interval collapses after path clamping.");

                var before = pieces.Count;
                for (var segmentIndex = 0; segmentIndex < path.Segments.Count; segmentIndex++)
                {
                    var segment = path.Segments[segmentIndex];
                    var overlapStart = Math.Max(start, segment.StartStationM);
                    var overlapEnd = Math.Min(end, segment.EndStationM);
                    if (overlapEnd - overlapStart <= Tolerance) continue;
                    if (pieces.Count >= MaxPieces)
                        throw new InvalidOperationException("Curtain path frame mapping requires more than " + MaxPieces + " native pieces.");

                    var centerStation = Midpoint(overlapStart, overlapEnd, "curtain path split center");
                    var ratio = (centerStation - segment.StartStationM) / segment.LengthM;
                    var centerX = Add(segment.Start.X, Multiply(segment.Dx, ratio, "curtain path center X delta"), "curtain path center X");
                    var centerY = Add(segment.Start.Y, Multiply(segment.Dy, ratio, "curtain path center Y delta"), "curtain path center Y");
                    var angle = Finite(Math.Atan2(segment.Dy, segment.Dx), "curtain path segment angle");
                    pieces.Add(new CurtainPathFramePiece(
                        frameIndex,
                        segmentIndex,
                        overlapStart,
                        overlapEnd,
                        centerX,
                        centerY,
                        angle,
                        Math.Max(0d, z),
                        height));
                }

                if (pieces.Count == before)
                    throw new InvalidOperationException("Curtain frame rectangle could not be mapped to any host path segment.");
            }

            return new CurtainPathFramePlan(path.TotalLengthM, path.Segments.Count, sourceFrameCount, pieces.AsReadOnly());
        }

        public static CurtainPathProjection ProjectPoint(IReadOnlyList<Point2> centerline, Point2 point)
        {
            var path = BuildPath(centerline);
            Finite(point.X, "curtain projection point X");
            Finite(point.Y, "curtain projection point Y");

            PathSegment? bestSegment = null;
            Point2 bestPoint = default;
            var bestRatio = 0d;
            var bestDistance = 0d;
            var bestIndex = -1;

            for (var index = 0; index < path.Segments.Count; index++)
            {
                var segment = path.Segments[index];
                var px = Finite(point.X - segment.Start.X, "curtain projection point delta X");
                var py = Finite(point.Y - segment.Start.Y, "curtain projection point delta Y");
                var ratio = ProjectionRatio(segment, px, py);

                var projected = new Point2(
                    Add(segment.Start.X, Multiply(segment.Dx, ratio, "curtain projection X delta"), "curtain projection X"),
                    Add(segment.Start.Y, Multiply(segment.Dy, ratio, "curtain projection Y delta"), "curtain projection Y"));
                var distance = point.DistanceTo(projected);

                if (bestSegment == null || distance < bestDistance - Tolerance)
                {
                    bestSegment = segment;
                    bestPoint = projected;
                    bestRatio = ratio;
                    bestDistance = distance;
                    bestIndex = index;
                    continue;
                }

                if (Math.Abs(distance - bestDistance) <= Tolerance)
                {
                    var candidateStation = ProjectionStation(segment, ratio);
                    var currentStation = ProjectionStation(bestSegment, bestRatio);
                    if (candidateStation < currentStation)
                    {
                        bestSegment = segment;
                        bestPoint = projected;
                        bestRatio = ratio;
                        bestDistance = distance;
                        bestIndex = index;
                    }
                }
            }

            if (bestSegment == null)
                throw new InvalidOperationException("Curtain path projection has no valid segment.");
            return new CurtainPathProjection(
                ProjectionStation(bestSegment, bestRatio),
                bestDistance,
                bestPoint,
                bestIndex);
        }

        public static double Length(IReadOnlyList<Point2> centerline) => BuildPath(centerline).TotalLengthM;

        private static double ProjectionStation(PathSegment segment, double ratio)
        {
            ratio = Finite(ratio, "curtain projection ratio");
            if (!(ratio > 0d)) return segment.StartStationM;
            if (ratio >= 1d) return segment.EndStationM;

            var delta = Multiply(segment.LengthM, ratio, "curtain projection station delta");
            var station = Add(segment.StartStationM, delta, "curtain projection station");
            if (!(station > segment.StartStationM) || !(station < segment.EndStationM))
                throw new OverflowException("Curtain projection station lost an interior offset at floating-point precision.");
            return station;
        }

        private static double ProjectionRatio(PathSegment segment, double px, double py)
        {
            var segmentScale = Positive(Math.Max(Math.Abs(segment.Dx), Math.Abs(segment.Dy)), "curtain projection segment scale");
            var dx = Finite(segment.Dx / segmentScale, "curtain projection normalized dx");
            var dy = Finite(segment.Dy / segmentScale, "curtain projection normalized dy");
            var denominator = Add(
                Multiply(dx, dx, "curtain projection normalized dx2"),
                Multiply(dy, dy, "curtain projection normalized dy2"),
                "curtain projection normalized denominator");

            var pointScale = Math.Max(Math.Abs(px), Math.Abs(py));
            if (pointScale == 0d) return 0d;
            pointScale = Positive(pointScale, "curtain projection point scale");
            var nx = Finite(px / pointScale, "curtain projection normalized point X");
            var ny = Finite(py / pointScale, "curtain projection normalized point Y");
            var dot = Add(
                Multiply(nx, dx, "curtain projection normalized dot X"),
                Multiply(ny, dy, "curtain projection normalized dot Y"),
                "curtain projection normalized numerator");
            if (!(dot > 0d)) return 0d;

            var normalizedRatio = Finite(dot / denominator, "curtain projection normalized ratio");
            if (pointScale >= segmentScale)
            {
                var inverseScaleRatio = Finite(segmentScale / pointScale, "curtain projection inverse scale ratio");
                if (!(inverseScaleRatio > 0d) || normalizedRatio >= inverseScaleRatio) return 1d;
                return Finite(normalizedRatio / inverseScaleRatio, "curtain projection ratio");
            }

            var scaleRatio = Finite(pointScale / segmentScale, "curtain projection scale ratio");
            var ratio = Multiply(scaleRatio, normalizedRatio, "curtain projection ratio");
            return ratio >= 1d ? 1d : ratio;
        }

        private static PathData BuildPath(IReadOnlyList<Point2> centerline)
        {
            if (centerline == null) throw new ArgumentNullException(nameof(centerline));
            if (centerline.Count < 2) throw new ArgumentException("Curtain host path requires at least two points.", nameof(centerline));
            if (centerline.Count > MaxPathPoints) throw new InvalidOperationException("Curtain host path exceeds the supported point budget of " + MaxPathPoints + ".");

            var segments = new List<PathSegment>(centerline.Count - 1);
            var station = 0d;
            for (var index = 0; index < centerline.Count - 1; index++)
            {
                var start = centerline[index];
                var end = centerline[index + 1];
                Finite(start.X, "curtain path start X");
                Finite(start.Y, "curtain path start Y");
                Finite(end.X, "curtain path end X");
                Finite(end.Y, "curtain path end Y");
                var length = start.DistanceTo(end);
                if (!(length > Tolerance)) throw new InvalidOperationException("Curtain host path contains a zero-length segment at index " + index + ".");
                var endStation = Add(station, length, "curtain path cumulative length");
                if (!(endStation > station))
                    throw new OverflowException("Curtain path cumulative length lost a positive segment at floating-point precision.");
                segments.Add(new PathSegment(start, end, station, endStation, length));
                station = endStation;
            }
            return new PathData(station, segments);
        }

        private sealed class PathData
        {
            public PathData(double totalLengthM, List<PathSegment> segments)
            {
                TotalLengthM = totalLengthM;
                Segments = segments;
            }
            public double TotalLengthM { get; }
            public List<PathSegment> Segments { get; }
        }

        private sealed class PathSegment
        {
            public PathSegment(Point2 start, Point2 end, double startStationM, double endStationM, double lengthM)
            {
                Start = start;
                End = end;
                StartStationM = startStationM;
                EndStationM = endStationM;
                LengthM = lengthM;
                Dx = end.X - start.X;
                Dy = end.Y - start.Y;
            }
            public Point2 Start { get; }
            public Point2 End { get; }
            public double StartStationM { get; }
            public double EndStationM { get; }
            public double LengthM { get; }
            public double Dx { get; }
            public double Dy { get; }
        }

        private static double Positive(double value, string label)
        {
            value = Finite(value, label);
            if (!(value > 0d)) throw new InvalidOperationException(label + " must be greater than zero.");
            return value;
        }

        private static double StationComparisonTolerance(double station)
        {
            station = Finite(station, "curtain station tolerance reference");
            var magnitude = Math.Abs(station);
            if (magnitude == 0d) return Tolerance;

            var bits = BitConverter.DoubleToInt64Bits(magnitude);
            var adjacentBits = magnitude == double.MaxValue ? bits - 1L : bits + 1L;
            var adjacent = BitConverter.Int64BitsToDouble(adjacentBits);
            var ulp = Finite(Math.Abs(adjacent - magnitude), "curtain station tolerance ULP");
            return Math.Max(Tolerance, Multiply(ulp, 4d, "curtain station tolerance ULP allowance"));
        }

        private static double Midpoint(double left, double right, string label)
        {
            left = Finite(left, label + " left");
            right = Finite(right, label + " right");
            var delta = Finite(right - left, label + " delta");
            var midpoint = Add(left, delta / 2d, label);
            if (right > left && (!(midpoint > left) || !(midpoint < right)))
                throw new OverflowException(label + " is not representable inside the station interval.");
            return midpoint;
        }

        private static double Add(double left, double right, string label) => Finite(Finite(left, label + " left") + Finite(right, label + " right"), label);
        private static double Multiply(double left, double right, string label) => Finite(Finite(left, label + " left") * Finite(right, label + " right"), label);

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " must be finite.");
            return value;
        }
    }
}
