using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class PolylineOpeningCutInput
    {
        public IReadOnlyList<Point2> Centerline { get; set; } = Array.Empty<Point2>();
        public Point2 OpeningCenter { get; set; }
        public double HostThicknessM { get; set; }
        public double HostHeightM { get; set; }
        public double OpeningWidthM { get; set; }
        public double OpeningHeightM { get; set; }
        public double SillHeightM { get; set; }
        public double ClearanceM { get; set; }
        public double MaximumCenterlineOffsetM { get; set; }
    }

    public sealed class PolylineOpeningCutPlan
    {
        public PolylineOpeningCutPlan(
            OpeningCutPlan cut,
            int segmentIndex,
            Point2 projectedCenter,
            Point2 tangent,
            double stationM,
            double segmentStationM,
            double segmentLengthM,
            double centerlineOffsetM)
        {
            Cut = cut ?? throw new ArgumentNullException(nameof(cut));
            SegmentIndex = segmentIndex;
            ProjectedCenter = projectedCenter;
            Tangent = tangent;
            StationM = stationM;
            SegmentStationM = segmentStationM;
            SegmentLengthM = segmentLengthM;
            CenterlineOffsetM = centerlineOffsetM;
        }

        public OpeningCutPlan Cut { get; }
        public int SegmentIndex { get; }
        public Point2 ProjectedCenter { get; }
        public Point2 Tangent { get; }
        public double StationM { get; }
        public double SegmentStationM { get; }
        public double SegmentLengthM { get; }
        public double CenterlineOffsetM { get; }
    }

    public static class PolylineOpeningCutPlanner
    {
        public static PolylineOpeningCutPlan Plan(PolylineOpeningCutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Centerline == null || input.Centerline.Count < 2) throw new ArgumentException("Polyline opening host centerline requires at least two points.", nameof(input.Centerline));
            Positive(input.HostThicknessM, nameof(input.HostThicknessM));
            Positive(input.HostHeightM, nameof(input.HostHeightM));
            Positive(input.OpeningWidthM, nameof(input.OpeningWidthM));
            Positive(input.OpeningHeightM, nameof(input.OpeningHeightM));
            NonNegative(input.SillHeightM, nameof(input.SillHeightM));
            NonNegative(input.ClearanceM, nameof(input.ClearanceM));
            Positive(input.MaximumCenterlineOffsetM, nameof(input.MaximumCenterlineOffsetM));
            ValidatePoint(input.OpeningCenter, nameof(input.OpeningCenter));

            var segmentLengths = new double[input.Centerline.Count - 1];
            var totalLengthM = 0d;
            for (var index = 0; index < input.Centerline.Count; index++) ValidatePoint(input.Centerline[index], "Centerline[" + index + "]");
            for (var index = 0; index < segmentLengths.Length; index++)
            {
                var length = Distance(input.Centerline[index], input.Centerline[index + 1], "centerline segment " + index);
                if (!(length > 1e-12d)) throw new ArgumentException("Polyline opening host centerline contains a degenerate segment at index " + index + ".", nameof(input.Centerline));
                segmentLengths[index] = length;
                totalLengthM = Add(totalLengthM, length, "polyline host total length");
            }

            var bestIndex = -1;
            var bestDistance = double.MaxValue;
            var bestAlong = 0d;
            var bestProjected = default(Point2);
            var bestTangent = default(Point2);
            var stationBefore = 0d;
            var bestStation = 0d;

            for (var index = 0; index < segmentLengths.Length; index++)
            {
                var start = input.Centerline[index];
                var end = input.Centerline[index + 1];
                var length = segmentLengths[index];
                var dx = Finite(end.X - start.X, "centerline dx");
                var dy = Finite(end.Y - start.Y, "centerline dy");
                var ux = dx / length;
                var uy = dy / length;
                var fromStartX = Finite(input.OpeningCenter.X - start.X, "opening projection dx");
                var fromStartY = Finite(input.OpeningCenter.Y - start.Y, "opening projection dy");
                var projectionScale = Math.Max(Math.Abs(fromStartX), Math.Abs(fromStartY));
                double along;
                if (projectionScale == 0d)
                {
                    along = 0d;
                }
                else
                {
                    var scaledAlong = Finite(
                        fromStartX / projectionScale * ux + fromStartY / projectionScale * uy,
                        "opening scaled projection along segment");
                    if (scaledAlong <= 0d)
                    {
                        along = 0d;
                    }
                    else
                    {
                        var scaledLength = length / projectionScale;
                        along = scaledAlong >= scaledLength
                            ? length
                            : Finite(scaledAlong * projectionScale, "opening projection along segment");
                    }
                }
                var projected = along <= 0d
                    ? start
                    : along >= length
                        ? end
                        : new Point2(start.X + ux * along, start.Y + uy * along);
                ValidatePoint(projected, "projected opening center");
                var distance = Distance(projected, input.OpeningCenter, "opening centerline offset");

                if (distance < bestDistance - 1e-12d || (Math.Abs(distance - bestDistance) <= 1e-12d && (bestIndex < 0 || index < bestIndex)))
                {
                    bestIndex = index;
                    bestDistance = distance;
                    bestAlong = along;
                    bestProjected = projected;
                    bestTangent = new Point2(ux, uy);
                    bestStation = Add(stationBefore, along, "opening station");
                }
                stationBefore = Add(stationBefore, length, "polyline host station accumulation");
            }

            if (bestIndex < 0) throw new InvalidOperationException("Could not project opening onto the host centerline.");
            if (bestDistance > input.MaximumCenterlineOffsetM)
                throw new InvalidOperationException("Opening center is too far from the host centerline for a safe physical cut.");

            var cut = OpeningCutPlanner.Plan(new OpeningCutInput
            {
                HostLengthM = totalLengthM,
                HostThicknessM = input.HostThicknessM,
                HostHeightM = input.HostHeightM,
                OpeningWidthM = input.OpeningWidthM,
                OpeningHeightM = input.OpeningHeightM,
                SillHeightM = input.SillHeightM,
                CenterAlongHostM = bestStation,
                ClearanceM = input.ClearanceM
            });

            var halfCutterWidthM = cut.CutterWidthM / 2d;
            var segmentLengthM = segmentLengths[bestIndex];
            var distanceToStartM = bestAlong;
            var distanceToEndM = segmentLengthM - bestAlong;
            if (distanceToStartM + 1e-12d < halfCutterWidthM || distanceToEndM + 1e-12d < halfCutterWidthM)
                throw new InvalidOperationException("Opening cutter crosses a polyline wall corner/junction. Reposition the opening or rebuild the host as a dedicated segment before cutting.");

            return new PolylineOpeningCutPlan(cut, bestIndex, bestProjected, bestTangent, bestStation, bestAlong, segmentLengthM, bestDistance);
        }

        private static double Distance(Point2 first, Point2 second, string label)
        {
            var dx = Finite(second.X - first.X, label + " dx");
            var dy = Finite(second.Y - first.Y, label + " dy");
            var scale = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (scale == 0d) return 0d;
            var nx = dx / scale;
            var ny = dy / scale;
            var result = scale * Math.Sqrt(nx * nx + ny * ny);
            return Positive(result, label);
        }

        private static double Add(double left, double right, string label)
        {
            var result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " overflowed.");
            return result;
        }

        private static void ValidatePoint(Point2 point, string name)
        {
            Finite(point.X, name + ".X");
            Finite(point.Y, name + ".Y");
        }

        private static double Positive(double value, string name)
        {
            Finite(value, name);
            if (value <= 0d) throw new ArgumentOutOfRangeException(name);
            return value;
        }

        private static void NonNegative(double value, string name)
        {
            Finite(value, name);
            if (value < 0d) throw new ArgumentOutOfRangeException(name);
        }

        private static double Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }
}
