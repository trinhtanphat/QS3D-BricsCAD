using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurvedOpeningStationOverflowSmoke
    {
        internal static void Run()
        {
            DerivedStationOverflowFailsClosed();
            NormalScaleStationPlanningRemainsStable();
        }

        private static void DerivedStationOverflowFailsClosed()
        {
            var input = new CurvedOpeningFootprintInput
            {
                Centerline = new[]
                {
                    new Point2(0d, 0d),
                    new Point2(6.5e307d, 0d),
                    new Point2(1.3e308d, 0d)
                },
                OpeningPoint = new Point2(1.0e308d, 0d),
                OpeningWidthM = 1.7e308d,
                HostThicknessM = 0.2d,
                ClearanceM = 0d,
                MaximumCenterlineOffsetM = 1d,
                AmbiguityMarginM = 0d,
                ToleranceM = 5.0e307d
            };

            Throws<OverflowException>(() => CurvedOpeningFootprintPlanner.Plan(input));
        }

        private static void NormalScaleStationPlanningRemainsStable()
        {
            var plan = CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = new[] { new Point2(0d, 0d), new Point2(5d, 0d) },
                OpeningPoint = new Point2(2.5d, 0d),
                OpeningWidthM = 1d,
                HostThicknessM = 0.2d,
                ClearanceM = 0d,
                MaximumCenterlineOffsetM = 0.1d,
                AmbiguityMarginM = 0d,
                ToleranceM = 1e-9d
            });

            Near(2d, plan.StartStationM);
            Near(3d, plan.EndStationM);
            Near(5d, plan.HostCenterlineLengthM);
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
