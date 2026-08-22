using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.Rebar
{
    public sealed class PolygonalSlabMeshInput
    {
        public IReadOnlyList<Point2> FootprintM { get; set; } = Array.Empty<Point2>();
        public IReadOnlyList<IReadOnlyList<Point2>> HoleFootprintsM { get; set; } = Array.Empty<IReadOnlyList<Point2>>();
        public double ThicknessM { get; set; }
        public double CoverM { get; set; }
        public double XDiameterMm { get; set; }
        public double YDiameterMm { get; set; }
        public double? XSpacingMm { get; set; }
        public int? XCount { get; set; }
        public double? YSpacingMm { get; set; }
        public int? YCount { get; set; }
        public bool IncludeBottom { get; set; } = true;
        public bool IncludeTop { get; set; }
        public bool XClosestToFace { get; set; } = true;
    }

    public sealed class PolygonalSlabMeshBarPlacement
    {
        public SlabMeshFace Face { get; set; }
        public SlabMeshDirection Direction { get; set; }
        public Point2 StartM { get; set; }
        public Point2 EndM { get; set; }
        public double ElevationOffsetM { get; set; }
        public double DiameterMm { get; set; }
        public double LengthM => StartM.DistanceTo(EndM);
    }

    public sealed class PolygonalSlabMeshLayout
    {
        public PolygonalSlabMeshLayout(IReadOnlyList<PolygonalSlabMeshBarPlacement> bars, double xActualSpacingM, double yActualSpacingM)
        {
            Bars = bars ?? throw new ArgumentNullException(nameof(bars));
            XActualSpacingM = xActualSpacingM;
            YActualSpacingM = yActualSpacingM;
        }

        public IReadOnlyList<PolygonalSlabMeshBarPlacement> Bars { get; }
        public double XActualSpacingM { get; }
        public double YActualSpacingM { get; }
        public int Count => Bars.Count;
    }

    public static class PolygonalSlabMeshPlanner
    {
        private const int MaxBars = 8192;
        private const int MaxForbiddenIntervalsPerScanline = 16384;
        private const double Epsilon = 1e-10d;

        private readonly struct Interval
        {
            public Interval(double start, double end)
            {
                Start = Math.Min(start, end);
                End = Math.Max(start, end);
            }

            public double Start { get; }
            public double End { get; }
            public double Length => End - Start;
        }

        public static PolygonalSlabMeshLayout Plan(PolygonalSlabMeshInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.FootprintM == null) throw new ArgumentNullException(nameof(input.FootprintM));
            if (input.HoleFootprintsM == null) throw new ArgumentNullException(nameof(input.HoleFootprintsM));

            // Keep all topology, scanline and cover math near a local origin. Large WCS offsets can
            // otherwise destroy small polygon areas through floating-point cancellation even though
            // the slab dimensions themselves are perfectly ordinary.
            var origin = input.FootprintM.Count > 0 ? input.FootprintM[0] : new Point2(0d, 0d);
            ValidateFinite(origin.X, "polygonal slab origin X");
            ValidateFinite(origin.Y, "polygonal slab origin Y");
            var localOuter = TranslateLoopToLocal(input.FootprintM, origin, "polygonal slab outer");
            var localHoles = new List<IReadOnlyList<Point2>>(input.HoleFootprintsM.Count);
            for (var index = 0; index < input.HoleFootprintsM.Count; index++)
            {
                var hole = input.HoleFootprintsM[index];
                if (hole == null) throw new ArgumentException("Polygonal slab hole cannot be null at index " + index + ".", nameof(input.HoleFootprintsM));
                localHoles.Add(TranslateLoopToLocal(hole, origin, "polygonal slab hole " + index));
            }

            var region = PolygonRegionScanlineClipper.NormalizeAndValidate(localOuter, localHoles);
            var footprint = region.Outer;
            var thickness = RebarMath.Positive(input.ThicknessM, nameof(input.ThicknessM));
            var cover = RebarMath.NonNegative(input.CoverM, nameof(input.CoverM));
            var xDiameter = RebarMath.Positive(input.XDiameterMm, nameof(input.XDiameterMm));
            var yDiameter = RebarMath.Positive(input.YDiameterMm, nameof(input.YDiameterMm));
            if (!input.IncludeBottom && !input.IncludeTop) throw new InvalidOperationException("At least one polygonal slab mesh face must be enabled.");

            var minX = footprint.Min(point => point.X);
            var maxX = footprint.Max(point => point.X);
            var minY = footprint.Min(point => point.Y);
            var maxY = footprint.Max(point => point.Y);
            var spanX = PositiveSpan(maxX - minX, "polygonal slab X span");
            var spanY = PositiveSpan(maxY - minY, "polygonal slab Y span");
            var centerX = Midpoint(minX, maxX, "polygonal slab center X");
            var centerY = Midpoint(minY, maxY, "polygonal slab center Y");

            var xDistribution = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = spanY,
                CoverM = cover,
                DiameterMm = xDiameter,
                SpacingMm = input.XSpacingMm,
                Count = input.XCount
            });
            var yDistribution = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = spanX,
                CoverM = cover,
                DiameterMm = yDiameter,
                SpacingMm = input.YSpacingMm,
                Count = input.YCount
            });

            var xRadius = RebarMath.Divide(xDiameter, 2000d, "polygonal slab X radius");
            var yRadius = RebarMath.Divide(yDiameter, 2000d, "polygonal slab Y radius");
            var xClearance = RebarMath.Add(cover, xRadius, "polygonal slab X edge clearance");
            var yClearance = RebarMath.Add(cover, yRadius, "polygonal slab Y edge clearance");
            var elevations = ResolveElevations(thickness, cover, xRadius, yRadius, input.XClosestToFace, input.IncludeBottom, input.IncludeTop);

            var xSegments = BuildSegments(region, PolygonScanAxis.Horizontal, centerY, xDistribution.OffsetsM, xClearance);
            var ySegments = BuildSegments(region, PolygonScanAxis.Vertical, centerX, yDistribution.OffsetsM, yClearance);
            if (xSegments.Count == 0) throw new InvalidOperationException("Polygonal slab region leaves no cover-compliant X rebar segments.");
            if (ySegments.Count == 0) throw new InvalidOperationException("Polygonal slab region leaves no cover-compliant Y rebar segments.");

            var faceCount = (input.IncludeBottom ? 1L : 0L) + (input.IncludeTop ? 1L : 0L);
            var projected = faceCount * ((long)xSegments.Count + ySegments.Count);
            if (projected > MaxBars) throw new InvalidOperationException("Polygonal slab mesh exceeds the supported " + MaxBars + " bar limit.");

            var bars = new List<PolygonalSlabMeshBarPlacement>((int)projected);
            if (input.IncludeBottom)
                AppendFace(bars, SlabMeshFace.Bottom, xSegments, ySegments, elevations.BottomX, elevations.BottomY, xDiameter, yDiameter);
            if (input.IncludeTop)
                AppendFace(bars, SlabMeshFace.Top, xSegments, ySegments, elevations.TopX, elevations.TopY, xDiameter, yDiameter);
            RestoreGlobalCoordinates(bars, origin);

            return new PolygonalSlabMeshLayout(bars.AsReadOnly(), xDistribution.ActualSpacingM, yDistribution.ActualSpacingM);
        }

        private static List<PolygonScanSegment> BuildSegments(
            PolygonRegion2 region,
            PolygonScanAxis axis,
            double centerAcross,
            IReadOnlyList<double> offsets,
            double clearance)
        {
            var result = new List<PolygonScanSegment>();
            foreach (var offset in offsets)
            {
                var coordinate = CheckedAdd(centerAcross, offset, "polygonal slab scanline coordinate");
                var clipped = PolygonRegionScanlineClipper.Clip(region, axis, coordinate);
                foreach (var interior in clipped)
                {
                    foreach (var safe in SubtractBoundaryClearance(region.BoundaryLoops, axis, coordinate, interior, clearance))
                    {
                        result.Add(safe);
                        if (result.Count > MaxBars) throw new InvalidOperationException("Polygonal slab mesh exceeds the supported " + MaxBars + " bar limit.");
                    }
                }
            }
            return result;
        }

        private static IReadOnlyList<PolygonScanSegment> SubtractBoundaryClearance(
            IReadOnlyList<IReadOnlyList<Point2>> boundaryLoops,
            PolygonScanAxis axis,
            double coordinate,
            PolygonScanSegment interior,
            double clearance)
        {
            var forbidden = new List<Interval>();
            foreach (var loop in boundaryLoops)
            {
                for (var index = 0; index < loop.Count; index++)
                {
                    var a = loop[index];
                    var b = loop[(index + 1) % loop.Count];
                    AppendCapsuleIntersection(forbidden, axis, coordinate, a, b, clearance);
                    if (forbidden.Count > MaxForbiddenIntervalsPerScanline)
                        throw new InvalidOperationException("Polygonal slab boundary clearance exceeds the supported interval limit.");
                }
            }

            if (forbidden.Count == 0) return new[] { interior };
            var merged = Merge(forbidden);
            var startAlong = Along(axis, interior.Start);
            var endAlong = Along(axis, interior.End);
            if (endAlong < startAlong)
            {
                var swap = startAlong;
                startAlong = endAlong;
                endAlong = swap;
            }

            var pieces = new List<PolygonScanSegment>();
            var cursor = startAlong;
            foreach (var block in merged)
            {
                if (block.End <= cursor + Epsilon) continue;
                if (block.Start >= endAlong - Epsilon) break;
                if (block.Start > cursor + Epsilon)
                {
                    var pieceEnd = Math.Min(block.Start, endAlong);
                    AddPiece(pieces, axis, coordinate, cursor, pieceEnd);
                }
                cursor = Math.Max(cursor, block.End);
                if (cursor >= endAlong - Epsilon) break;
            }
            if (cursor < endAlong - Epsilon) AddPiece(pieces, axis, coordinate, cursor, endAlong);
            return pieces.AsReadOnly();
        }

        private static void AppendCapsuleIntersection(
            ICollection<Interval> target,
            PolygonScanAxis axis,
            double coordinate,
            Point2 a,
            Point2 b,
            double radius)
        {
            var u1 = Along(axis, a);
            var v1 = Across(axis, a);
            var u2 = Along(axis, b);
            var v2 = Across(axis, b);
            var du = u2 - u1;
            var dv = v2 - v1;
            var lengthSquared = du * du + dv * dv;
            if (!Finite(lengthSquared) || !(lengthSquared > 0d)) throw new InvalidOperationException("Polygonal slab boundary edge is not finite/non-degenerate.");
            var length = Math.Sqrt(lengthSquared);
            if (!Finite(length) || !(length > 0d)) throw new InvalidOperationException("Polygonal slab boundary edge length is invalid.");

            AppendDiskIntersection(target, u1, coordinate - v1, radius);
            AppendDiskIntersection(target, u2, coordinate - v2, radius);

            var deltaV = coordinate - v1;
            Interval? perpendicular = null;
            if (Math.Abs(dv) > Epsilon)
            {
                var center = u1 + du * deltaV / dv;
                var half = radius * length / Math.Abs(dv);
                ValidateFinite(center, "polygonal slab capsule center");
                ValidateFinite(half, "polygonal slab capsule half-width");
                perpendicular = new Interval(center - half, center + half);
            }
            else if (Math.Abs(deltaV) < radius - Epsilon)
            {
                perpendicular = new Interval(double.NegativeInfinity, double.PositiveInfinity);
            }

            if (!perpendicular.HasValue) return;

            Interval? projection = null;
            if (Math.Abs(du) > Epsilon)
            {
                var atStart = u1 - dv * deltaV / du;
                var atEnd = u1 + (lengthSquared - dv * deltaV) / du;
                ValidateFinite(atStart, "polygonal slab capsule projection start");
                ValidateFinite(atEnd, "polygonal slab capsule projection end");
                projection = new Interval(atStart, atEnd);
            }
            else
            {
                var t = dv * deltaV / lengthSquared;
                if (Finite(t) && t >= -Epsilon && t <= 1d + Epsilon)
                    projection = new Interval(double.NegativeInfinity, double.PositiveInfinity);
            }

            if (!projection.HasValue) return;
            var start = Math.Max(perpendicular.Value.Start, projection.Value.Start);
            var end = Math.Min(perpendicular.Value.End, projection.Value.End);
            if (Finite(start) && Finite(end) && end - start > Epsilon) target.Add(new Interval(start, end));
        }

        private static void AppendDiskIntersection(ICollection<Interval> target, double centerAlong, double deltaAcross, double radius)
        {
            var absoluteAcross = Math.Abs(deltaAcross);
            if (!(absoluteAcross < radius - Epsilon)) return;
            var squared = radius * radius - deltaAcross * deltaAcross;
            if (!(squared > 0d) || !Finite(squared)) return;
            var half = Math.Sqrt(squared);
            if (!Finite(half) || !(half > Epsilon)) return;
            target.Add(new Interval(centerAlong - half, centerAlong + half));
        }

        private static IReadOnlyList<Interval> Merge(List<Interval> intervals)
        {
            intervals.Sort((left, right) => left.Start.CompareTo(right.Start));
            var merged = new List<Interval>();
            foreach (var interval in intervals)
            {
                if (interval.Length <= Epsilon) continue;
                if (merged.Count == 0 || interval.Start > merged[merged.Count - 1].End + Epsilon)
                {
                    merged.Add(interval);
                    continue;
                }
                var previous = merged[merged.Count - 1];
                merged[merged.Count - 1] = new Interval(previous.Start, Math.Max(previous.End, interval.End));
            }
            return merged.AsReadOnly();
        }

        private static void AddPiece(ICollection<PolygonScanSegment> target, PolygonScanAxis axis, double coordinate, double startAlong, double endAlong)
        {
            if (!(endAlong - startAlong > Epsilon)) return;
            var start = axis == PolygonScanAxis.Horizontal ? new Point2(startAlong, coordinate) : new Point2(coordinate, startAlong);
            var end = axis == PolygonScanAxis.Horizontal ? new Point2(endAlong, coordinate) : new Point2(coordinate, endAlong);
            target.Add(new PolygonScanSegment(start, end));
        }

        private static void AppendFace(
            ICollection<PolygonalSlabMeshBarPlacement> bars,
            SlabMeshFace face,
            IEnumerable<PolygonScanSegment> xSegments,
            IEnumerable<PolygonScanSegment> ySegments,
            double xElevation,
            double yElevation,
            double xDiameter,
            double yDiameter)
        {
            foreach (var segment in xSegments)
                bars.Add(new PolygonalSlabMeshBarPlacement { Face = face, Direction = SlabMeshDirection.X, StartM = segment.Start, EndM = segment.End, ElevationOffsetM = xElevation, DiameterMm = xDiameter });
            foreach (var segment in ySegments)
                bars.Add(new PolygonalSlabMeshBarPlacement { Face = face, Direction = SlabMeshDirection.Y, StartM = segment.Start, EndM = segment.End, ElevationOffsetM = yElevation, DiameterMm = yDiameter });
        }

        private static IReadOnlyList<Point2> TranslateLoopToLocal(IReadOnlyList<Point2> loop, Point2 origin, string label)
        {
            if (loop == null) throw new ArgumentNullException(nameof(loop));
            var translated = new List<Point2>(loop.Count);
            for (var index = 0; index < loop.Count; index++)
            {
                var point = loop[index];
                translated.Add(new Point2(
                    CheckedSubtract(point.X, origin.X, label + "[" + index + "]/X"),
                    CheckedSubtract(point.Y, origin.Y, label + "[" + index + "]/Y")));
            }
            return translated.AsReadOnly();
        }

        private static void RestoreGlobalCoordinates(IList<PolygonalSlabMeshBarPlacement> bars, Point2 origin)
        {
            foreach (var bar in bars)
            {
                bar.StartM = new Point2(
                    CheckedAdd(origin.X, bar.StartM.X, "polygonal slab global start X"),
                    CheckedAdd(origin.Y, bar.StartM.Y, "polygonal slab global start Y"));
                bar.EndM = new Point2(
                    CheckedAdd(origin.X, bar.EndM.X, "polygonal slab global end X"),
                    CheckedAdd(origin.Y, bar.EndM.Y, "polygonal slab global end Y"));
            }
        }

        private sealed class MeshElevations
        {
            public double BottomX { get; set; }
            public double BottomY { get; set; }
            public double TopX { get; set; }
            public double TopY { get; set; }
        }

        private static MeshElevations ResolveElevations(double thickness, double cover, double xRadius, double yRadius, bool xClosestToFace, bool includeBottom, bool includeTop)
        {
            var half = RebarMath.Divide(thickness, 2d, "polygonal slab half thickness");
            double bottomX;
            double bottomY;
            double topX;
            double topY;
            if (xClosestToFace)
            {
                bottomX = -half + cover + xRadius;
                bottomY = bottomX + xRadius + yRadius;
                topX = half - cover - xRadius;
                topY = topX - xRadius - yRadius;
            }
            else
            {
                bottomY = -half + cover + yRadius;
                bottomX = bottomY + xRadius + yRadius;
                topY = half - cover - yRadius;
                topX = topY - xRadius - yRadius;
            }
            ValidateFinite(bottomX, "polygonal slab bottom X elevation");
            ValidateFinite(bottomY, "polygonal slab bottom Y elevation");
            ValidateFinite(topX, "polygonal slab top X elevation");
            ValidateFinite(topY, "polygonal slab top Y elevation");

            var usableLow = -half + cover;
            var usableHigh = half - cover;
            if (includeBottom && includeTop)
            {
                var bottomHigh = Math.Max(bottomX + xRadius, bottomY + yRadius);
                var topLow = Math.Min(topX - xRadius, topY - yRadius);
                if (!(topLow > bottomHigh)) throw new InvalidOperationException("Slab thickness is insufficient for the requested top + bottom polygonal mesh and cover.");
            }
            else if (includeBottom)
            {
                var low = Math.Min(bottomX - xRadius, bottomY - yRadius);
                var high = Math.Max(bottomX + xRadius, bottomY + yRadius);
                if (low < usableLow - 1e-12d || high > usableHigh + 1e-12d)
                    throw new InvalidOperationException("Bottom polygonal slab mesh does not fit within the concrete cover envelope.");
            }
            else if (includeTop)
            {
                var low = Math.Min(topX - xRadius, topY - yRadius);
                var high = Math.Max(topX + xRadius, topY + yRadius);
                if (low < usableLow - 1e-12d || high > usableHigh + 1e-12d)
                    throw new InvalidOperationException("Top polygonal slab mesh does not fit within the concrete cover envelope.");
            }

            return new MeshElevations { BottomX = bottomX, BottomY = bottomY, TopX = topX, TopY = topY };
        }

        private static double Along(PolygonScanAxis axis, Point2 point) => axis == PolygonScanAxis.Horizontal ? point.X : point.Y;
        private static double Across(PolygonScanAxis axis, Point2 point) => axis == PolygonScanAxis.Horizontal ? point.Y : point.X;

        private static double PositiveSpan(double value, string label)
        {
            if (!Finite(value) || !(value > Epsilon)) throw new InvalidOperationException(label + " must be finite and positive.");
            return value;
        }

        private static double Midpoint(double left, double right, string label)
        {
            ValidateFinite(left, label);
            ValidateFinite(right, label);
            var value = left / 2d + right / 2d;
            ValidateFinite(value, label);
            return value;
        }

        private static double CheckedAdd(double left, double right, string label)
        {
            ValidateFinite(left, label);
            ValidateFinite(right, label);
            var value = left + right;
            ValidateFinite(value, label);
            return value;
        }

        private static double CheckedSubtract(double left, double right, string label)
        {
            ValidateFinite(left, label);
            ValidateFinite(right, label);
            var value = left - right;
            ValidateFinite(value, label);
            return value;
        }

        private static void ValidateFinite(double value, string label)
        {
            if (!Finite(value)) throw new OverflowException(label + " is not finite.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}