using System;
using System.Collections;
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

        internal double BaseRight => X_M + WidthM;
        internal double BaseTop => Z_M + HeightM;
        internal double Left => X_M - ClearanceM;
        internal double Bottom => Z_M - ClearanceM;
        internal double Right => BaseRight + ClearanceM;
        internal double Top => BaseTop + ClearanceM;

        private void EnsureFiniteBounds()
        {
            var baseRight = BaseRight;
            var baseTop = BaseTop;
            var left = Left;
            var bottom = Bottom;
            var right = Right;
            var top = Top;

            if (!IsFinite(baseRight) ||
                !IsFinite(baseTop) ||
                !IsFinite(left) ||
                !IsFinite(bottom) ||
                !IsFinite(right) ||
                !IsFinite(top))
            {
                throw new OverflowException("Curtain opening bounds must remain finite after applying size and clearance.");
            }

            if (!(baseRight > X_M))
                throw new OverflowException("Curtain opening width is below the representable coordinate resolution.");
            if (!(baseTop > Z_M))
                throw new OverflowException("Curtain opening height is below the representable coordinate resolution.");

            if (ClearanceM > 0d)
            {
                if (!(left < X_M) || !(right > baseRight))
                    throw new OverflowException("Curtain opening horizontal clearance is below the representable coordinate resolution.");
                if (!(bottom < Z_M) || !(top > baseTop))
                    throw new OverflowException("Curtain opening vertical clearance is below the representable coordinate resolution.");
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
        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public static class CurtainFrameOpeningPlanner
    {
        private const int MaxOpenings = 4096;
        private const int MaxOutputFragments = 20000;

        public static IReadOnlyList<CurtainWallRect> Interrupt(
            IEnumerable<CurtainWallRect> frames,
            IEnumerable<CurtainOpeningRect> openings)
        {
            if (frames == null) throw new ArgumentNullException(nameof(frames));
            if (openings == null) throw new ArgumentNullException(nameof(openings));

            var frameKnownCount = SnapshotKnownCount(frames, MaxOutputFragments, "frame", out var frameKnownCountSources);
            var result = frameKnownCount.HasValue
                ? new List<CurtainWallRect>(frameKnownCount.Value)
                : new List<CurtainWallRect>();
            using (var frameEnumerator = frames.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownCount(frames, frameKnownCount, frameKnownCountSources, MaxOutputFragments, "frame");
                    if (!frameEnumerator.MoveNext())
                        break;
                    RequireStableKnownCount(frames, frameKnownCount, frameKnownCountSources, MaxOutputFragments, "frame");
                    if (frameKnownCount.HasValue && result.Count >= frameKnownCount.Value)
                        throw new InvalidOperationException("Curtain frame collection count changed during enumeration.");
                    if (result.Count >= MaxOutputFragments)
                        throw new InvalidOperationException("Curtain frame input exceeds safety limit " + MaxOutputFragments + ".");
                    var frame = frameEnumerator.Current;
                    result.Add(ValidateFrame(frame));
                }
            }
            RequireObservedCount(frameKnownCount, result.Count, "frame");
            RequireStableKnownCount(frames, frameKnownCount, frameKnownCountSources, MaxOutputFragments, "frame");

            var openingKnownCount = SnapshotKnownCount(openings, MaxOpenings, "opening", out var openingKnownCountSources);
            var cuts = openingKnownCount.HasValue
                ? new List<CurtainOpeningRect>(openingKnownCount.Value)
                : new List<CurtainOpeningRect>();
            using (var openingEnumerator = openings.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownCount(openings, openingKnownCount, openingKnownCountSources, MaxOpenings, "opening");
                    if (!openingEnumerator.MoveNext())
                        break;
                    RequireStableKnownCount(openings, openingKnownCount, openingKnownCountSources, MaxOpenings, "opening");
                    if (openingKnownCount.HasValue && cuts.Count >= openingKnownCount.Value)
                        throw new InvalidOperationException("Curtain opening collection count changed during enumeration.");
                    if (cuts.Count >= MaxOpenings)
                        throw new InvalidOperationException("Curtain opening input exceeds safety limit " + MaxOpenings + ".");
                    var opening = openingEnumerator.Current;
                    if (opening == null) throw new ArgumentException("Opening collection contains null.", nameof(openings));
                    cuts.Add(opening);
                }
            }
            RequireObservedCount(openingKnownCount, cuts.Count, "opening");
            RequireStableKnownCount(openings, openingKnownCount, openingKnownCountSources, MaxOpenings, "opening");

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

        private static int? SnapshotKnownCount<T>(IEnumerable<T> values, int maximum, string subject, out int knownCountSources)
        {
            int? knownCount = null;
            var sources = 0;
            if (values is ICollection<T> genericCollection)
            {
                sources |= 1;
                AcceptKnownCount(genericCollection.Count, maximum, subject, ref knownCount);
            }
            if (values is IReadOnlyCollection<T> readOnlyCollection)
            {
                sources |= 2;
                AcceptKnownCount(readOnlyCollection.Count, maximum, subject, ref knownCount);
            }
            if (values is ICollection nonGenericCollection)
            {
                sources |= 4;
                AcceptKnownCount(nonGenericCollection.Count, maximum, subject, ref knownCount);
            }
            knownCountSources = sources;
            return knownCount;
        }

        private static void AcceptKnownCount(int count, int maximum, string subject, ref int? knownCount)
        {
            if (count < 0)
                throw new InvalidOperationException("Curtain " + subject + " collection exposes an invalid negative count.");
            if (count > maximum)
                throw new InvalidOperationException("Curtain " + subject + " input exceeds safety limit " + maximum + ".");
            if (knownCount.HasValue && knownCount.Value != count)
                throw new InvalidOperationException("Curtain " + subject + " collection exposes conflicting known counts.");
            knownCount = count;
        }

        private static void RequireObservedCount(int? knownCount, int observedCount, string subject)
        {
            if (knownCount.HasValue && knownCount.Value != observedCount)
                throw new InvalidOperationException("Curtain " + subject + " collection count changed during enumeration.");
        }

        private static void RequireStableKnownCount<T>(
            IEnumerable<T> values,
            int? initialKnownCount,
            int initialKnownCountSources,
            int maximum,
            string subject)
        {
            var currentKnownCount = SnapshotKnownCount(values, maximum, subject, out var currentKnownCountSources);
            if (currentKnownCount != initialKnownCount || currentKnownCountSources != initialKnownCountSources)
                throw new InvalidOperationException("Curtain " + subject + " collection count changed during enumeration.");
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

            var right = frame.X_M + frame.WidthM;
            var top = frame.Z_M + frame.HeightM;
            if (!IsFinite(right) || !IsFinite(top))
                throw new InvalidOperationException("Curtain frame rectangle bounds must remain finite.");
            if (!(right > frame.X_M))
                throw new InvalidOperationException("Curtain frame rectangle width is below the representable coordinate resolution.");
            if (!(top > frame.Z_M))
                throw new InvalidOperationException("Curtain frame rectangle height is below the representable coordinate resolution.");

            return frame;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

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
            if (cutRight - cutLeft <= 0d || cutTop - cutBottom <= 0d)
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
            if (width <= 0d || height <= 0d) return;
            output.Add(new CurtainWallRect(x, z, width, height));
        }
    }
}
