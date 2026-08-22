using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class CurtainOpeningRect
    {
        public CurtainOpeningRect(double xM, double zM, double widthM, double heightM, double clearanceM = 0d)
        {
            X_M = Finite(xM, nameof(xM));
            Z_M = Finite(zM, nameof(zM));
            WidthM = Positive(widthM, nameof(widthM));
            HeightM = Positive(heightM, nameof(heightM));
            ClearanceM = NonNegative(clearanceM, nameof(clearanceM));

            EnsureFiniteBounds();
        }

        public double X_M { get; }
        public double Z_M { get; }
        public double WidthM { get; }
        public double HeightM { get; }
        public double ClearanceM { get; }

        internal double Left => X_M - ClearanceM;
        internal double Bottom => Z_M - ClearanceM;
        internal double Right => X_M + WidthM + ClearanceM;
        internal double Top => Z_M + HeightM + ClearanceM;

        private void EnsureFiniteBounds()
        {
            if (!double.IsFinite(Left) ||
                !double.IsFinite(Bottom) ||
                !double.IsFinite(Right) ||
                !double.IsFinite(Top))
            {
                throw new OverflowException("Curtain opening bounds must remain finite after applying size and clearance.");
            }
        }

        private static double Positive(double value, string label)
        {
            value = Finite(value, label);
            if (!(value > 0d)) throw new ArgumentOutOfRangeException(label, "Value must be > 0.");
            return value;
        }
        private static double NonNegative(double value, string label)
        {
            value = Finite(value, label);
            if (value < 0d) throw new ArgumentOutOfRangeException(label, "Value must be >= 0.");
            return value;
        }
        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(label, "Value must be finite.");
            return value;
        }
    }

    public static class CurtainFrameOpeningPlanner
    {
        private const int MaxOpenings = 4096;
        private const int MaxOutputFragments = 20000;
        private const double Epsilon = 1e-10d;

        public static IReadOnlyList<CurtainWallRect> Interrupt(
            IEnumerable<CurtainWallRect> frames,
            IEnumerable<CurtainOpeningRect> openings)
        {
            if (frames == null) throw new ArgumentNullException(nameof(frames));
            if (openings == null) throw new ArgumentNullException(nameof(openings));
            var result = new List<CurtainWallRect>();
            foreach (var frame in frames)
            {
                if (result.Count >= MaxOutputFragments) throw new InvalidOperationException("Curtain frame input exceeds safety limit " + MaxOutputFragments + ".");
                result.Add(ValidateFrame(frame));
            }
            var cuts = new List<CurtainOpeningRect>();
            foreach (var opening in openings)
            {
                if (opening == null) throw new ArgumentException("Opening collection contains null.", nameof(openings));
                if (cuts.Count >= MaxOpenings) throw new InvalidOperationException("Curtain opening input exceeds safety limit " + MaxOpenings + ".");
                cuts.Add(opening);
            }
            foreach (var opening in cuts)
            {
                var next = new List<CurtainWallRect>();
                foreach (var frame in result)
                {
                    Subtract(frame, opening, next);
                    if (next.Count > MaxOutputFragments) throw new InvalidOperationException("Curtain frame opening interruption exceeds fragment safety limit " + MaxOutputFragments + ".");
                }
                result = next;
                if (result.Count == 0) break;
            }
            return result.AsReadOnly();
        }

        private static CurtainWallRect ValidateFrame(CurtainWallRect frame)
        {
            if (frame == null) throw new ArgumentException("Frame collection contains null.", nameof(frame));
            if (double.IsNaN(frame.X_M) || double.IsInfinity(frame.X_M) ||
                double.IsNaN(frame.Z_M) || double.IsInfinity(frame.Z_M) ||
                double.IsNaN(frame.WidthM) || double.IsInfinity(frame.WidthM) ||
                double.IsNaN(frame.HeightM) || double.IsInfinity(frame.HeightM) ||
                frame.WidthM <= 0d || frame.HeightM <= 0d)
                throw new InvalidOperationException("Curtain frame rectangle is invalid.");

            if (!double.IsFinite(frame.X_M + frame.WidthM) ||
                !double.IsFinite(frame.Z_M + frame.HeightM))
            {
                throw new InvalidOperationException("Curtain frame rectangle bounds must remain finite.");
            }

            return frame;
        }

        private static void Subtract(CurtainWallRect frame, CurtainOpeningRect opening, ICollection<CurtainWallRect> output)
        {
            var left = frame.X_M;
            var right = frame.X_M + frame.WidthM;
            var bottom = frame.Z_M;
            var top = frame.Z_M + frame.HeightM;
            var cutLeft = Math.Max(left, opening.Left);
            var cutRight = Math.Min(right, opening.Right);
            var cutBottom = Math.Max(bottom, opening.Bottom);
            var cutTop = Math.Min(top, opening.Top);
            if (cutRight - cutLeft <= Epsilon || cutTop - cutBottom <= Epsilon)
            {
                output.Add(frame);
                return;
            }

            Add(output, left, bottom, cutLeft - left, top - bottom);
            Add(output, cutRight, bottom, right - cutRight, top - bottom);
            Add(output, cutLeft, bottom, cutRight - cutLeft, cutBottom - bottom);
            Add(output, cutLeft, cutTop, cutRight - cutLeft, top - cutTop);
        }

        private static void Add(ICollection<CurtainWallRect> output, double x, double z, double width, double height)
        {
            if (width <= Epsilon || height <= Epsilon) return;
            output.Add(new CurtainWallRect(x, z, width, height));
        }
    }
}
