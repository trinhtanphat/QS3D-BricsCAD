using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class SectionDetailVolumePlannerSmoke
    {
        internal static void Run()
        {
            OrdinaryBoundsExpandOnEverySide();
            DirectConstructionRejectsInvalidStates();
            DirectConstructionAcceptsCanonicalState();
            LargeCoordinatePaddingCollapseFailsClosed();
            SpanOverflowFailsClosed();
            NonFiniteInputFailsClosed();
            RepresentableLargeCoordinatesRemainSupported();
        }

        private static void OrdinaryBoundsExpandOnEverySide()
        {
            if (!SectionDetailVolumePlanner.TryCreate(0d, 0d, 0d, 10d, 20d, 30d, 1e-9d, out var plan))
                throw new InvalidOperationException("Ordinary BIM Detail bounds should produce a stable padded volume.");

            if (!(plan.FirstX < 0d) || !(plan.FirstY < 0d) || !(plan.BaseZ < 0d) ||
                !(plan.OppositeX > 10d) || !(plan.OppositeY > 20d) || !(plan.Height > 30d) ||
                !(plan.BaseZ + plan.Height > 30d))
                throw new InvalidOperationException("Ordinary BIM Detail volume did not strictly expand every requested bound.");
        }

        private static void DirectConstructionRejectsInvalidStates()
        {
            Throws<ArgumentOutOfRangeException>(() => new SectionDetailVolumePlan(
                double.NaN, 0d, 0d, 1d, 1d, 1d),
                "Direct BIM Detail plans must reject non-finite coordinates.");
            Throws<ArgumentOutOfRangeException>(() => new SectionDetailVolumePlan(
                0d, 0d, 0d, double.PositiveInfinity, 1d, 1d),
                "Direct BIM Detail plans must reject non-finite opposite coordinates.");
            Throws<ArgumentOutOfRangeException>(() => new SectionDetailVolumePlan(
                1d, 0d, 0d, 1d, 1d, 1d),
                "Direct BIM Detail plans must reject zero-width X extents.");
            Throws<ArgumentOutOfRangeException>(() => new SectionDetailVolumePlan(
                2d, 0d, 0d, 1d, 1d, 1d),
                "Direct BIM Detail plans must reject reversed X extents.");
            Throws<ArgumentOutOfRangeException>(() => new SectionDetailVolumePlan(
                0d, 1d, 0d, 1d, 1d, 1d),
                "Direct BIM Detail plans must reject zero-width Y extents.");
            Throws<ArgumentOutOfRangeException>(() => new SectionDetailVolumePlan(
                0d, 2d, 0d, 1d, 1d, 1d),
                "Direct BIM Detail plans must reject reversed Y extents.");
            Throws<ArgumentOutOfRangeException>(() => new SectionDetailVolumePlan(
                0d, 0d, 0d, 1d, 1d, 0d),
                "Direct BIM Detail plans must reject zero height.");
            Throws<ArgumentOutOfRangeException>(() => new SectionDetailVolumePlan(
                0d, 0d, 0d, 1d, 1d, -1d),
                "Direct BIM Detail plans must reject negative height.");
            Throws<ArgumentOutOfRangeException>(() => new SectionDetailVolumePlan(
                0d, 0d, double.MaxValue, 1d, 1d, double.MaxValue),
                "Direct BIM Detail plans must reject represented-top overflow.");
            Throws<ArgumentOutOfRangeException>(() => new SectionDetailVolumePlan(
                0d, 0d, 1e16d, 1d, 1d, 1d),
                "Direct BIM Detail plans must reject a positive height that collapses at the represented base Z.");
        }

        private static void DirectConstructionAcceptsCanonicalState()
        {
            var plan = new SectionDetailVolumePlan(-1d, -2d, -3d, 4d, 5d, 9d);
            if (plan.FirstX != -1d || plan.FirstY != -2d || plan.BaseZ != -3d ||
                plan.OppositeX != 4d || plan.OppositeY != 5d || plan.Height != 9d)
                throw new InvalidOperationException("Canonical direct BIM Detail plan construction changed stored coordinates.");
        }

        private static void LargeCoordinatePaddingCollapseFailsClosed()
        {
            const double large = 1e16d;
            if (SectionDetailVolumePlanner.TryCreate(large, 0d, 0d, large + 2d, 2d, 2d, 1e-9d, out _))
                throw new InvalidOperationException("A positive BIM Detail padding that collapses at a large coordinate must fail closed.");
        }

        private static void SpanOverflowFailsClosed()
        {
            if (SectionDetailVolumePlanner.TryCreate(-double.MaxValue, 0d, 0d, double.MaxValue, 1d, 1d, 1e-9d, out _))
                throw new InvalidOperationException("Finite endpoints whose derived span overflows must fail closed.");
        }

        private static void NonFiniteInputFailsClosed()
        {
            if (SectionDetailVolumePlanner.TryCreate(0d, 0d, 0d, 1d, 1d, double.PositiveInfinity, 1e-9d, out _))
                throw new InvalidOperationException("Non-finite BIM Detail bounds must fail closed.");
        }

        private static void RepresentableLargeCoordinatesRemainSupported()
        {
            const double large = 1e12d;
            if (!SectionDetailVolumePlanner.TryCreate(large, large, large, large + 1000d, large + 1200d, large + 800d, 1e-9d, out var plan))
                throw new InvalidOperationException("Representable large-coordinate BIM Detail bounds should remain supported.");

            if (!(plan.FirstX < large) || !(plan.FirstY < large) || !(plan.BaseZ < large) ||
                !(plan.OppositeX > large + 1000d) || !(plan.OppositeY > large + 1200d) ||
                !(plan.BaseZ + plan.Height > large + 800d))
                throw new InvalidOperationException("Representable large-coordinate BIM Detail volume lost a requested expansion.");
        }

        private static void Throws<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
