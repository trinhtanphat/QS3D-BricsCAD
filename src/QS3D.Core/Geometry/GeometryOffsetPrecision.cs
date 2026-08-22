using System;

namespace QS3D.Core.Geometry
{
    /// <summary>
    /// Fail-closed helpers for non-negative geometric offsets that must remain representable
    /// after binary64 arithmetic. A positive user request must produce a strict coordinate/span
    /// change; otherwise callers should reject the mutation instead of silently losing it.
    /// </summary>
    public static class GeometryOffsetPrecision
    {
        public static bool TryAddNonNegative(
            double origin,
            double offset,
            bool requirePositiveChange,
            out double result)
        {
            result = 0d;
            if (!Finite(origin) || !Finite(offset) || offset < 0d)
                return false;
            if (requirePositiveChange && !(offset > 0d))
                return false;

            var candidate = origin + offset;
            if (!Finite(candidate))
                return false;
            if (requirePositiveChange && !(candidate > origin))
                return false;

            result = candidate;
            return true;
        }

        public static bool TrySubtractNonNegative(
            double origin,
            double offset,
            bool requirePositiveChange,
            out double result)
        {
            result = 0d;
            if (!Finite(origin) || !Finite(offset) || offset < 0d)
                return false;
            if (requirePositiveChange && !(offset > 0d))
                return false;

            var candidate = origin - offset;
            if (!Finite(candidate))
                return false;
            if (requirePositiveChange && !(candidate < origin))
                return false;

            result = candidate;
            return true;
        }

        public static bool TryExpandBoth(
            double min,
            double max,
            double offset,
            bool requirePositiveChange,
            out double expandedMin,
            out double expandedMax,
            out double expandedSpan)
        {
            expandedMin = 0d;
            expandedMax = 0d;
            expandedSpan = 0d;
            if (!Finite(min) || !Finite(max) || !Finite(offset) || offset < 0d || max < min)
                return false;
            if (requirePositiveChange && !(offset > 0d))
                return false;

            var originalSpan = max - min;
            if (!Finite(originalSpan) || originalSpan < 0d)
                return false;
            if (!TrySubtractNonNegative(min, offset, requirePositiveChange, out var lower) ||
                !TryAddNonNegative(max, offset, requirePositiveChange, out var upper))
                return false;

            var span = upper - lower;
            if (!Finite(span) || span < 0d)
                return false;
            if (requirePositiveChange && !(span > originalSpan))
                return false;
            if (!requirePositiveChange && span < originalSpan)
                return false;

            expandedMin = lower;
            expandedMax = upper;
            expandedSpan = span;
            return true;
        }

        public static bool TryExpandLower(
            double min,
            double max,
            double offset,
            bool requirePositiveChange,
            out double expandedMin,
            out double expandedSpan)
        {
            expandedMin = 0d;
            expandedSpan = 0d;
            if (!Finite(min) || !Finite(max) || !Finite(offset) || offset < 0d || max < min)
                return false;
            if (requirePositiveChange && !(offset > 0d))
                return false;

            var originalSpan = max - min;
            if (!Finite(originalSpan) || originalSpan < 0d)
                return false;
            if (!TrySubtractNonNegative(min, offset, requirePositiveChange, out var lower))
                return false;

            var span = max - lower;
            if (!Finite(span) || span < 0d)
                return false;
            if (requirePositiveChange && !(span > originalSpan))
                return false;
            if (!requirePositiveChange && span < originalSpan)
                return false;

            expandedMin = lower;
            expandedSpan = span;
            return true;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
