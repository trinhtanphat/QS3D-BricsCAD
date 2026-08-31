using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Features
{
    public readonly struct FloatingToolBounds : IEquatable<FloatingToolBounds>
    {
        public FloatingToolBounds(double left, double top, double width, double height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public double Left { get; }
        public double Top { get; }
        public double Width { get; }
        public double Height { get; }
        public double Right => Left + Width;
        public double Bottom => Top + Height;

        public bool Equals(FloatingToolBounds other)
        {
            return Left.Equals(other.Left)
                && Top.Equals(other.Top)
                && Width.Equals(other.Width)
                && Height.Equals(other.Height);
        }

        public override bool Equals(object? obj) => obj is FloatingToolBounds other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + Left.GetHashCode();
                hash = (hash * 31) + Top.GetHashCode();
                hash = (hash * 31) + Width.GetHashCode();
                hash = (hash * 31) + Height.GetHashCode();
                return hash;
            }
        }
    }

    public static class FloatingToolWindowPolicy
    {
        public const double DefaultWidth = 720d;
        public const double DefaultHeight = 520d;
        public const double MinimumWidth = 320d;
        public const double MinimumHeight = 240d;
        public const int MaximumVisibleWorkAreas = 64;

        public static FloatingToolBounds Normalize(
            FloatingToolBounds requested,
            IEnumerable<FloatingToolBounds> visibleWorkAreas)
        {
            if (visibleWorkAreas == null) throw new ArgumentNullException(nameof(visibleWorkAreas));
            var areas = MaterializeVisibleWorkAreas(visibleWorkAreas);
            if (areas.Length == 0)
                throw new InvalidOperationException("At least one valid visible work area is required for a floating tool.");

            var validRequest = IsFinite(requested.Left)
                && IsFinite(requested.Top)
                && IsFinite(requested.Width)
                && IsFinite(requested.Height)
                && requested.Width > 0d
                && requested.Height > 0d;

            var area = validRequest
                ? areas.OrderByDescending(x => IntersectionArea(requested, x)).First()
                : areas[0];

            var requestedWidth = validRequest ? requested.Width : DefaultWidth;
            var requestedHeight = validRequest ? requested.Height : DefaultHeight;
            var width = Math.Min(area.Width, Math.Max(Math.Min(MinimumWidth, area.Width), requestedWidth));
            var height = Math.Min(area.Height, Math.Max(Math.Min(MinimumHeight, area.Height), requestedHeight));

            var requestedLeft = validRequest ? requested.Left : area.Left + ((area.Width - width) / 2d);
            var requestedTop = validRequest ? requested.Top : area.Top + ((area.Height - height) / 2d);
            var left = Clamp(requestedLeft, area.Left, area.Right - width);
            var top = Clamp(requestedTop, area.Top, area.Bottom - height);
            return new FloatingToolBounds(left, top, width, height);
        }

        private static FloatingToolBounds[] MaterializeVisibleWorkAreas(IEnumerable<FloatingToolBounds> visibleWorkAreas)
        {
            var knownCount = GetKnownCount(visibleWorkAreas);
            if (knownCount.HasValue && knownCount.Value > MaximumVisibleWorkAreas)
            {
                throw new InvalidOperationException(
                    "Floating tool normalization supports at most " + MaximumVisibleWorkAreas + " visible work areas.");
            }

            var validAreas = new List<FloatingToolBounds>(knownCount ?? 4);
            var traversed = 0;
            foreach (var candidate in visibleWorkAreas)
            {
                traversed++;
                if (knownCount.HasValue && traversed > knownCount.Value)
                {
                    throw new InvalidOperationException(
                        "Visible work-area collection yielded more entries than its deterministic Count value.");
                }

                if (traversed > MaximumVisibleWorkAreas)
                {
                    throw new InvalidOperationException(
                        "Floating tool normalization supports at most " + MaximumVisibleWorkAreas + " visible work areas.");
                }

                if (IsValidWorkArea(candidate))
                    validAreas.Add(candidate);
            }

            var reboundCount = GetKnownCount(visibleWorkAreas);
            if (knownCount.HasValue)
            {
                if (!reboundCount.HasValue || reboundCount.Value != knownCount.Value)
                    throw new InvalidOperationException("Visible work-area collection Count changed during enumeration.");
                if (traversed != knownCount.Value)
                    throw new InvalidOperationException("Visible work-area collection traversal did not match its deterministic Count value.");
            }
            else if (reboundCount.HasValue)
            {
                throw new InvalidOperationException("Visible work-area collection exposed Count metadata only after enumeration.");
            }

            return validAreas.ToArray();
        }

        private static int? GetKnownCount(IEnumerable<FloatingToolBounds> visibleWorkAreas)
        {
            int? count = null;
            if (visibleWorkAreas is ICollection<FloatingToolBounds> collection)
                count = collection.Count;

            if (visibleWorkAreas is IReadOnlyCollection<FloatingToolBounds> readOnlyCollection)
            {
                if (count.HasValue && count.Value != readOnlyCollection.Count)
                    throw new InvalidOperationException("Visible work-area collection exposes conflicting Count values.");
                count = readOnlyCollection.Count;
            }

            if (visibleWorkAreas is System.Collections.ICollection nonGenericCollection)
            {
                if (count.HasValue && count.Value != nonGenericCollection.Count)
                    throw new InvalidOperationException("Visible work-area collection exposes conflicting Count values.");
                count = nonGenericCollection.Count;
            }

            if (count.HasValue && count.Value < 0)
                throw new InvalidOperationException("Visible work-area collection exposes a negative Count value.");

            return count;
        }

        private static bool IsValidWorkArea(FloatingToolBounds bounds)
        {
            if (!IsFinite(bounds.Left)
                || !IsFinite(bounds.Top)
                || !IsFinite(bounds.Width)
                || !IsFinite(bounds.Height)
                || bounds.Width <= 0d
                || bounds.Height <= 0d)
            {
                return false;
            }

            var right = bounds.Right;
            var bottom = bounds.Bottom;
            return IsFinite(right)
                && IsFinite(bottom)
                && right > bounds.Left
                && bottom > bounds.Top;
        }

        private static double IntersectionArea(FloatingToolBounds left, FloatingToolBounds right)
        {
            var width = Math.Max(0d, Math.Min(left.Right, right.Right) - Math.Max(left.Left, right.Left));
            var height = Math.Max(0d, Math.Min(left.Bottom, right.Bottom) - Math.Max(left.Top, right.Top));
            return width * height;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (maximum < minimum) return minimum;
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}