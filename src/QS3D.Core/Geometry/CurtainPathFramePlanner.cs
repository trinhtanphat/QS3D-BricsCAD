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
            var path = BuildPath(centerline);
            var pieces = new List<CurtainPathFramePiece>();

            for (var frameIndex = 0; frameIndex < frames.Count; frameIndex++)
            {
                var frame = frames[frameIndex] ?? throw new InvalidOperationException("Curtain frame rectangle cannot be null.");
                var start = Finite(frame.X_M, "curtain frame start station");
                var width = Positive(frame.WidthM, "curtain frame width");
                var z = Finite(frame.Z_M, "curtain frame elevation");
                var height = Positive(frame.HeightM, "curtain frame height");
                if (start < -Tolerance || z < -Tolerance)
                    throw new InvalidOperationException("Curtain frame rectangle starts outside the supported path/elevation extent.");

                var end = Add(start, width, "curtain frame end station");
                var stationTolerance = Math.Max(Tolerance, path.TotalLengthM * 1e-10d);
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
                    if (overlapEnd - overlapStart <= stationTolerance) continue;
                    if (pieces.Count >= MaxPieces)
                        throw new InvalidOperationException("Curtain path frame mapping requires more than " + MaxPieces + " native pieces.");

                    var centerStation = (overlapStart + overlapEnd) / 2d;
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

            return new CurtainPathFramePlan(path.TotalLengthM, path.Segments.Count, frames.Count, pieces.AsReadOnly());
        }

        public static CurtainPathProjection ProjectPoint(IReadOnlyList<Point2> centerline, Point2 point)
        {
            var path = BuildPath(centerline);
            Finite(point.X, "curtain projection point X");
            Finite(point.Y, "curtain projection point Y");

            CurtainPathProjection? best = null;
            for (var index = 0; index < path.Segments.Count; index++)
            {
                var segment = path.Segments[index];
                var px = point.X - segment.Start.X;
                var py = point.Y - segment.Start.Y;
                var denominator = Add(Multiply(segment.Dx, segment.Dx, "curtain projection dx2"), Multiply(segment.Dy, segment.Dy, "curtain projection dy2"), "curtain projection denominator");
                var numerator = Add(Multiply(px, segment.Dx, "curtain projection dot X"), Multiply(py, segment.Dy, "curtain projection dot Y"), "curtain projection numerator");
                var ratio = numerator / denominator;
                if (ratio < 0d) ratio = 0d;
                else if (ratio > 1d) ratio = 1d;
                ratio = Finite(ratio, "curtain projection ratio");

                var projected = new Point2(
                    Add(segment.Start.X, Multiply(segment.Dx, ratio, "curtain projection X delta"), "curtain projection X"),
                    Add(segment.Start.Y, Multiply(segment.Dy, ratio, "curtain projection Y delta"), "curtain projection Y"));
                var distance = point.DistanceTo(projected);
                var station = Add(segment.StartStationM, Multiply(segment.LengthM, ratio, "curtain projection station delta"), "curtain projection station");
                var candidate = new CurtainPathProjection(station, distance, projected, index);
                if (best == null || distance < best.DistanceM - Tolerance ||
                    (Math.Abs(distance - best.DistanceM) <= Tolerance && station < best.StationM))
                    best = candidate;
            }

            return best ?? throw new InvalidOperationException("Curtain path projection has no valid segment.");
        }

        public static double Length(IReadOnlyList<Point2> centerline) => BuildPath(centerline).TotalLengthM;

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

        private static double Add(double left, double right, string label) => Finite(Finite(left, label + " left") + Finite(right, label + " right"), label);
        private static double Multiply(double left, double right, string label) => Finite(Finite(left, label + " left") * Finite(right, label + " right"), label);

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " must be finite.");
            return value;
        }
    }
}
