using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GeometryOffsetPrecisionSmoke
    {
        internal static void Run()
        {
            LargeCoordinateAddCollapseFailsClosed();
            LargeCoordinateSubtractCollapseFailsClosed();
            ExpandBothRejectsLostPositiveOffset();
            ExpandLowerRejectsLostPositiveDepth();
            OrdinaryAndZeroOffsetsRemainSupported();
            OverflowAndNonFiniteInputsFailClosed();
        }

        private static void LargeCoordinateAddCollapseFailsClosed()
        {
            const double large = 1e16d;
            if (GeometryOffsetPrecision.TryAddNonNegative(large, 1d, true, out _))
                throw new InvalidOperationException("A positive offset rounded away at a large coordinate must fail closed.");
            if (!GeometryOffsetPrecision.TryAddNonNegative(large, 2d, true, out var represented) || !(represented > large))
                throw new InvalidOperationException("A representable positive offset should remain supported.");
        }

        private static void LargeCoordinateSubtractCollapseFailsClosed()
        {
            const double large = 1e16d;
            if (GeometryOffsetPrecision.TrySubtractNonNegative(large, 1d, true, out _))
                throw new InvalidOperationException("A positive subtraction rounded away at a large coordinate must fail closed.");
            if (!GeometryOffsetPrecision.TrySubtractNonNegative(large, 2d, true, out var represented) || !(represented < large))
                throw new InvalidOperationException("A representable positive subtraction should remain supported.");
        }

        private static void ExpandBothRejectsLostPositiveOffset()
        {
            const double large = 1e16d;
            if (GeometryOffsetPrecision.TryExpandBoth(large, large + 2d, 1d, true, out _, out _, out _))
                throw new InvalidOperationException("Two-sided positive clearance that collapses on a large bound must fail closed.");
        }

        private static void ExpandLowerRejectsLostPositiveDepth()
        {
            const double large = 1e16d;
            if (GeometryOffsetPrecision.TryExpandLower(large, large + 2d, 1d, true, out _, out _))
                throw new InvalidOperationException("Positive lower-bound depth that collapses at a large elevation must fail closed.");
        }

        private static void OrdinaryAndZeroOffsetsRemainSupported()
        {
            if (!GeometryOffsetPrecision.TryExpandBoth(10d, 20d, 2d, true, out var min, out var max, out var span) ||
                min != 8d || max != 22d || span != 14d)
                throw new InvalidOperationException("Ordinary two-sided expansion changed semantics.");

            if (!GeometryOffsetPrecision.TryExpandBoth(10d, 20d, 0d, false, out min, out max, out span) ||
                min != 10d || max != 20d || span != 10d)
                throw new InvalidOperationException("Zero-clearance expansion should remain an exact no-op.");

            if (!GeometryOffsetPrecision.TryExpandLower(10d, 20d, 2d, true, out min, out span) ||
                min != 8d || span != 12d)
                throw new InvalidOperationException("Ordinary lower-bound expansion changed semantics.");
        }

        private static void OverflowAndNonFiniteInputsFailClosed()
        {
            if (GeometryOffsetPrecision.TryExpandBoth(-double.MaxValue, double.MaxValue, 1d, true, out _, out _, out _))
                throw new InvalidOperationException("Overflowing derived spans must fail closed.");
            if (GeometryOffsetPrecision.TryAddNonNegative(double.PositiveInfinity, 1d, true, out _))
                throw new InvalidOperationException("Non-finite origins must fail closed.");
            if (GeometryOffsetPrecision.TrySubtractNonNegative(0d, double.NaN, true, out _))
                throw new InvalidOperationException("Non-finite offsets must fail closed.");
        }
    }
}
