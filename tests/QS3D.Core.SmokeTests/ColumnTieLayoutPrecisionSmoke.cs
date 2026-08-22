using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieLayoutPrecisionSmoke
    {
        internal static void Run()
        {
            OrdinaryLayoutRemainsStable();
            ZeroCoverAndClearancesRemainValid();
            LostPositiveCoverFailsClosed();
            LostPositiveRadiusFailsClosed();
            LostPositiveTopClearanceFailsClosed();
        }

        private static void OrdinaryLayoutRemainsStable()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.4d,
                DepthM = 0.5d,
                HeightM = 3d,
                CoverM = 0.04d,
                DiameterMm = 16d,
                SpacingMm = 200d,
                BottomClearanceM = 0.05d,
                TopClearanceM = 0.05d
            });

            Assert(layout.ClosedPath.Count == 5, "Ordinary column tie path shape changed unexpectedly.");
            Assert(layout.ElevationsM.Count > 1, "Ordinary column tie layout should contain multiple ties.");
            Assert(Math.Abs(layout.ClosedPath[0].X + 0.152d) <= 1e-12d, "Ordinary half-width changed unexpectedly.");
            Assert(Math.Abs(layout.ClosedPath[0].Y + 0.202d) <= 1e-12d, "Ordinary half-depth changed unexpectedly.");
            Assert(Math.Abs(layout.ElevationsM[0] - 0.098d) <= 1e-12d, "Ordinary start elevation changed unexpectedly.");
            Assert(Math.Abs(layout.ElevationsM[layout.ElevationsM.Count - 1] - 2.902d) <= 1e-12d, "Ordinary end elevation changed unexpectedly.");
        }

        private static void ZeroCoverAndClearancesRemainValid()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.4d,
                DepthM = 0.5d,
                HeightM = 1d,
                CoverM = 0d,
                DiameterMm = 8d,
                SpacingMm = 200d,
                BottomClearanceM = 0d,
                TopClearanceM = 0d
            });

            Assert(layout.ElevationsM.Count > 1, "Zero cover/clearance compatibility changed unexpectedly.");
            Assert(Math.Abs(layout.ElevationsM[0] - 0.004d) <= 1e-12d, "Tie radius must still define the first center when cover/clearance are zero.");
            Assert(Math.Abs(layout.ElevationsM[layout.ElevationsM.Count - 1] - 0.996d) <= 1e-12d, "Tie radius must still define the last center when cover/clearance are zero.");
        }

        private static void LostPositiveCoverFailsClosed()
        {
            var error = Capture<OverflowException>(() => ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 2e16d,
                DepthM = 2e16d,
                HeightM = 1d,
                CoverM = 1d,
                DiameterMm = 16d,
                SpacingMm = 200d
            }));

            Assert(error.Message.Contains("half-width cover", StringComparison.Ordinal), "Lost positive cover should fail at the column tie half-width boundary.");
        }

        private static void LostPositiveRadiusFailsClosed()
        {
            var error = Capture<OverflowException>(() => ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 2e16d,
                DepthM = 2e16d,
                HeightM = 1d,
                CoverM = 0d,
                DiameterMm = 16d,
                SpacingMm = 200d
            }));

            Assert(error.Message.Contains("half-width radius", StringComparison.Ordinal), "Lost positive tie radius should fail at the column tie half-width boundary.");
        }

        private static void LostPositiveTopClearanceFailsClosed()
        {
            var error = Capture<OverflowException>(() => ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 1d,
                DepthM = 1d,
                HeightM = 1e16d,
                CoverM = 0d,
                DiameterMm = 16d,
                SpacingMm = 200d,
                BottomClearanceM = 0d,
                TopClearanceM = 1d
            }));

            Assert(error.Message.Contains("end top clearance", StringComparison.Ordinal), "Lost positive top clearance should fail at the column tie end boundary.");
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
