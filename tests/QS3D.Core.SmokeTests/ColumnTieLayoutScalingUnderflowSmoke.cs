using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieLayoutScalingUnderflowSmoke
    {
        internal static void Run()
        {
            OrdinaryLayoutRemainsStable();
            DiameterUnitScalingUnderflowFailsClosed();
            RadiusScalingUnderflowFailsClosed();
        }

        private static void OrdinaryLayoutRemainsStable()
        {
            var layout = ColumnTieLayoutPlanner.Plan(Input(8d));

            Assert(layout.ElevationsM.Count == 7, "Ordinary column tie layout count changed unexpectedly.");
            Assert(Math.Abs(layout.PathPerimeterM - 3.568d) <= 1e-12d, "Ordinary column tie perimeter changed unexpectedly.");
            Assert(layout.ActualSpacingM <= 0.15d + 1e-12d, "Ordinary column tie spacing exceeds the requested maximum.");
        }

        private static void DiameterUnitScalingUnderflowFailsClosed()
        {
            var error = Capture<OverflowException>(() => ColumnTieLayoutPlanner.Plan(Input(double.Epsilon)));
            Assert(error.Message == "Rebar division underflow: column tie diameter", "Column tie diameter scaling underflow must fail with the shared rebar arithmetic contract.");
        }

        private static void RadiusScalingUnderflowFailsClosed()
        {
            var diameterMm = double.Epsilon * 1024d;
            var error = Capture<OverflowException>(() => ColumnTieLayoutPlanner.Plan(Input(diameterMm)));
            Assert(error.Message == "Rebar division underflow: column tie radius", "Column tie radius scaling underflow must fail with the shared rebar arithmetic contract.");
        }

        private static ColumnTieLayoutInput Input(double diameterMm)
        {
            return new ColumnTieLayoutInput
            {
                WidthM = 1d,
                DepthM = 1d,
                HeightM = 1d,
                CoverM = 0.05d,
                DiameterMm = diameterMm,
                SpacingMm = 150d,
                BottomClearanceM = 0d,
                TopClearanceM = 0d
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

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
