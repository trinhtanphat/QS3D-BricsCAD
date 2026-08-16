using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class CurtainWallOpeningRect
    {
        public double X_M { get; set; }
        public double Z_M { get; set; }
        public double WidthM { get; set; }
        public double HeightM { get; set; }
    }

    public sealed class CurtainWallFramePiece
    {
        public int SourceFrameIndex { get; set; }
        public double X_M { get; set; }
        public double Z_M { get; set; }
        public double WidthM { get; set; }
        public double HeightM { get; set; }
        public double AreaM2
        {
            get
            {
                var area = WidthM * HeightM;
                if (double.IsNaN(area) || double.IsInfinity(area))
                    throw new OverflowException("Curtain frame piece area overflowed.");
                if (area == 0d && WidthM != 0d && HeightM != 0d)
                    throw new OverflowException("Curtain frame piece area underflowed to zero.");
                return area == 0d ? 0d : area;
            }
        }
    }

    public sealed class CurtainWallOpeningFramePlan
    {
        public IReadOnlyList<CurtainWallFramePiece> Pieces { get; set; } = Array.Empty<CurtainWallFramePiece>();
        public double OriginalFrameAreaM2 { get; set; }
        public double RemainingFrameAreaM2 { get; set; }
        public double RemovedFrameAreaM2 => Math.Max(0d, OriginalFrameAreaM2 - RemainingFrameAreaM2);
        public int InterruptedFrameCount { get; set; }
    }

    public static class CurtainWallOpeningFramePlanner
    {
        public const int MaxInputFrames = 20000;
        public const int MaxOpenings = 4096;
        public const int MaxOutputPieces = 32768;
        private const double Epsilon = 1e-9d;

        private sealed class Rect
        {
            public int SourceFrameIndex { get; set; }
            public double X { get; set; }
            public double Z { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double Right => X + Width;
            public double Top => Z + Height;
        }

        public static CurtainWallOpeningFramePlan Plan(
            IReadOnlyList<CurtainWallRect> frames,
            IReadOnlyList<CurtainWallOpeningRect> openings,
            double clearanceM = 0d)
        {
            if (frames == null) throw new ArgumentNullException(nameof(frames));
            if (openings == null) throw new ArgumentNullException(nameof(openings));
            FiniteNonNegative(clearanceM, nameof(clearanceM));
            if (frames.Count > MaxInputFrames) throw new InvalidOperationException("Curtain frame interruption input exceeds " + MaxInputFrames + " frames.");
            if (openings.Count > MaxOpenings) throw new InvalidOperationException("Curtain frame interruption input exceeds " + MaxOpenings + " openings.");

            var expandedOpenings = new List<Rect>(openings.Count);
            for (var i = 0; i < openings.Count; i++)
            {
                var opening = openings[i] ?? throw new InvalidOperationException("Curtain opening rectangle cannot be null.");
                var label = "opening[" + i + "]";
                ValidateRect(opening.X_M, opening.Z_M, opening.WidthM, opening.HeightM, label);
                var openingRight = opening.X_M + opening.WidthM;
                var openingTop = opening.Z_M + opening.HeightM;

                var expandedX = opening.X_M - clearanceM;
                var expandedZ = opening.Z_M - clearanceM;
                var expandedWidth = opening.WidthM + clearanceM * 2d;
                var expandedHeight = opening.HeightM + clearanceM * 2d;
                ValidateRect(expandedX, expandedZ, expandedWidth, expandedHeight, "expandedOpening[" + i + "]");

                if (clearanceM > 0d)
                {
                    var expandedRight = expandedX + expandedWidth;
                    var expandedTop = expandedZ + expandedHeight;
                    if (!(expandedX < opening.X_M) || !(expandedRight > openingRight))
                        throw new OverflowException(label + " horizontal clearance is below the representable coordinate resolution.");
                    if (!(expandedZ < opening.Z_M) || !(expandedTop > openingTop))
                        throw new OverflowException(label + " vertical clearance is below the representable coordinate resolution.");
                }

                expandedOpenings.Add(new Rect
                {
                    X = expandedX,
                    Z = expandedZ,
                    Width = expandedWidth,
                    Height = expandedHeight
                });
            }

            var output = new List<CurtainWallFramePiece>();
            var originalArea = 0d;
            var interrupted = 0;
            for (var frameIndex = 0; frameIndex < frames.Count; frameIndex++)
            {
                var frame = frames[frameIndex] ?? throw new InvalidOperationException("Curtain frame rectangle cannot be null.");
                ValidateRect(frame.X_M, frame.Z_M, frame.WidthM, frame.HeightM, "frame[" + frameIndex + "]");
                originalArea = CheckedAdd(originalArea, CheckedMultiply(frame.WidthM, frame.HeightM, "frame area"), "total frame area");

                var pieces = new List<Rect>
                {
                    new Rect { SourceFrameIndex = frameIndex, X = frame.X_M, Z = frame.Z_M, Width = frame.WidthM, Height = frame.HeightM }
                };
                var changed = false;
                foreach (var opening in expandedOpenings)
                {
                    if (pieces.Count == 0) break;
                    var next = new List<Rect>();
                    foreach (var piece in pieces)
                    {
                        var split = Subtract(piece, opening);
                        if (split.Count != 1 || !Same(piece, split[0])) changed = true;
                        next.AddRange(split);
                        if (output.Count + next.Count > MaxOutputPieces)
                            throw new InvalidOperationException("Curtain frame interruption output exceeds " + MaxOutputPieces + " pieces.");
                    }
                    pieces = next;
                }
                if (changed) interrupted++;
                foreach (var piece in pieces)
                {
                    if (piece.Width <= Epsilon || piece.Height <= Epsilon) continue;
                    output.Add(new CurtainWallFramePiece
                    {
                        SourceFrameIndex = frameIndex,
                        X_M = piece.X,
                        Z_M = piece.Z,
                        WidthM = piece.Width,
                        HeightM = piece.Height
                    });
                    if (output.Count > MaxOutputPieces)
                        throw new InvalidOperationException("Curtain frame interruption output exceeds " + MaxOutputPieces + " pieces.");
                }
            }

            var remainingArea = 0d;
            foreach (var piece in output)
                remainingArea = CheckedAdd(remainingArea, CheckedMultiply(piece.WidthM, piece.HeightM, "frame piece area"), "remaining frame area");
            if (interrupted > 0 && !(remainingArea < originalArea))
                throw new OverflowException("Curtain removed frame area was lost at floating-point precision.");

            return new CurtainWallOpeningFramePlan
            {
                Pieces = Array.AsReadOnly(output
                    .OrderBy(x => x.SourceFrameIndex)
                    .ThenBy(x => x.Z_M)
                    .ThenBy(x => x.X_M)
                    .ThenBy(x => x.HeightM)
                    .ThenBy(x => x.WidthM)
                    .ToArray()),
                OriginalFrameAreaM2 = originalArea,
                RemainingFrameAreaM2 = remainingArea,
                InterruptedFrameCount = interrupted
            };
        }

        private static List<Rect> Subtract(Rect source, Rect cut)
        {
            var ix0 = Math.Max(source.X, cut.X);
            var iz0 = Math.Max(source.Z, cut.Z);
            var ix1 = Math.Min(source.Right, cut.Right);
            var iz1 = Math.Min(source.Top, cut.Top);
            if (ix1 <= ix0 + Epsilon || iz1 <= iz0 + Epsilon)
                return new List<Rect> { source };

            var result = new List<Rect>(4);
            Add(result, source.SourceFrameIndex, source.X, source.Z, ix0 - source.X, source.Height);
            Add(result, source.SourceFrameIndex, ix1, source.Z, source.Right - ix1, source.Height);
            Add(result, source.SourceFrameIndex, ix0, source.Z, ix1 - ix0, iz0 - source.Z);
            Add(result, source.SourceFrameIndex, ix0, iz1, ix1 - ix0, source.Top - iz1);
            return result;
        }

        private static void Add(List<Rect> result, int sourceFrameIndex, double x, double z, double width, double height)
        {
            if (width <= Epsilon || height <= Epsilon) return;
            result.Add(new Rect { SourceFrameIndex = sourceFrameIndex, X = x, Z = z, Width = width, Height = height });
        }

        private static bool Same(Rect a, Rect b) =>
            Math.Abs(a.X - b.X) <= Epsilon && Math.Abs(a.Z - b.Z) <= Epsilon &&
            Math.Abs(a.Width - b.Width) <= Epsilon && Math.Abs(a.Height - b.Height) <= Epsilon;

        private static void ValidateRect(double x, double z, double width, double height, string label)
        {
            Finite(x, label + ".X_M");
            Finite(z, label + ".Z_M");
            FinitePositive(width, label + ".WidthM");
            FinitePositive(height, label + ".HeightM");
            var right = x + width;
            var top = z + height;
            Finite(right, label + ".Right");
            Finite(top, label + ".Top");
            if (!(right > x))
                throw new OverflowException(label + " width is below the representable coordinate resolution.");
            if (!(top > z))
                throw new OverflowException(label + " height is below the representable coordinate resolution.");
        }

        private static void Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(label, "Value must be finite.");
        }

        private static void FinitePositive(double value, string label)
        {
            Finite(value, label);
            if (value <= 0d) throw new ArgumentOutOfRangeException(label, "Value must be > 0.");
        }

        private static void FiniteNonNegative(double value, string label)
        {
            Finite(value, label);
            if (value < 0d) throw new ArgumentOutOfRangeException(label, "Value must be >= 0.");
        }

        private static double CheckedMultiply(double a, double b, string label)
        {
            var value = a * b;
            Finite(value, label);
            if (value == 0d && a != 0d && b != 0d)
                throw new OverflowException(label + " underflowed to zero.");
            return value == 0d ? 0d : value;
        }

        private static double CheckedAdd(double a, double b, string label)
        {
            var value = a + b;
            Finite(value, label);
            if (a > 0d && b > 0d && (value == a || value == b))
                throw new OverflowException(label + " lost a positive contribution at floating-point precision.");
            return value == 0d ? 0d : value;
        }
    }
}
