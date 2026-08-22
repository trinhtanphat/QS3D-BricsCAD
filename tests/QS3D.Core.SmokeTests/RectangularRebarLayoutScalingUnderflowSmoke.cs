using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularRebarLayoutScalingUnderflowSmoke
    {
        internal static void Run()
        {
            OrdinaryLayoutRemainsStable();
            DiameterUnitScalingUnderflowFailsClosed();
            RadiusScalingUnderflowFailsClosed();
        }

        private static void OrdinaryLayoutRemainsStable()
        {
            var layout = RectangularRebarLayoutPlanner.Plan(Input(8d));

            Assert(layout.BarCenters.Count == 4, "Ordinary rectangular rebar layout count changed unexpectedly.");
            Near(0.446d, layout.ClearHalfWidthM, "Ordinary rectangular rebar clear half-width changed unexpectedly.");
            Near(0.446d, layout.ClearHalfDepthM, "Ordinary rectangular rebar clear half-depth changed unexpectedly.");
        }

        private static void DiameterUnitScalingUnderflowFailsClosed()
        {
            var error = Capture<OverflowException>(() => RectangularRebarLayoutPlanner.Plan(Input(double.Epsilon)));
            Assert(
                error.Message == "Rebar division underflow: rectangular rebar diameter",
                "Rectangular rebar diameter scaling underflow must fail with the shared rebar arithmetic contract.");
        }

        private static void RadiusScalingUnderflowFailsClosed()
        {
            var diameterMm = double.Epsilon * 1024d;
            var error = Capture<OverflowException>(() => RectangularRebarLayoutPlanner.Plan(Input(diameterMm)));
            Assert(
                error.Message == "Rebar division underflow: rectangular rebar radius",
                "Rectangular rebar radius scaling underflow must fail with the shared rebar arithmetic contract.");
        }

        private static RectangularRebarLayoutInput Input(double diameterMm)
        {
            return new RectangularRebarLayoutInput
            {
                WidthM = 1d,
                DepthM = 1d,
                CoverM = 0.05d,
                DiameterMm = diameterMm,
                BarsAlongWidth = 2,
                BarsAlongDepth = 2
            };
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12d) throw new InvalidOperationException(message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
