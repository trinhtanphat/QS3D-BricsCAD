using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieAxialOverlapRegressionSmoke
    {
        public static void Run()
        {
            OverlappingTiesAreRejected();
            NormalSpacingStillSucceeds();
            TangentSpacingBoundaryStillSucceeds();
            SingleTieCollapsedRangeStillSucceeds();
        }

        private static void OverlappingTiesAreRejected()
        {
            Throws<InvalidOperationException>(() => ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.30d,
                DepthM = 0.30d,
                HeightM = 0.30d,
                CoverM = 0.04d,
                DiameterMm = 8d,
                SpacingMm = 4d,
                BottomClearanceM = 0d,
                TopClearanceM = 0d
            }));
        }

        private static void NormalSpacingStillSucceeds()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.30d,
                DepthM = 0.30d,
                HeightM = 0.30d,
                CoverM = 0.04d,
                DiameterMm = 8d,
                SpacingMm = 100d,
                BottomClearanceM = 0d,
                TopClearanceM = 0d
            });

            if (layout.ElevationsM.Count < 2 || layout.ActualSpacingM < 0.008d - 1e-12d)
                throw new InvalidOperationException("Normal column tie spacing no longer produces a non-overlapping layout.");
        }

        private static void TangentSpacingBoundaryStillSucceeds()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.30d,
                DepthM = 0.30d,
                HeightM = 0.168d,
                CoverM = 0.04d,
                DiameterMm = 8d,
                SpacingMm = 8d,
                BottomClearanceM = 0d,
                TopClearanceM = 0d
            });

            if (layout.ElevationsM.Count != 11 || Math.Abs(layout.ActualSpacingM - 0.008d) > 1e-12d)
                throw new InvalidOperationException("Exact one-diameter column tie spacing should remain supported.");
        }

        private static void SingleTieCollapsedRangeStillSucceeds()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.30d,
                DepthM = 0.30d,
                HeightM = 0.088d,
                CoverM = 0.04d,
                DiameterMm = 8d,
                SpacingMm = 8d,
                BottomClearanceM = 0d,
                TopClearanceM = 0d
            });

            if (layout.ElevationsM.Count != 1 || Math.Abs(layout.ActualSpacingM) > 1e-12d)
                throw new InvalidOperationException("Collapsed column tie range should remain a single non-overlapping tie.");
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
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class ColumnTieAxialOverlapSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ColumnTieAxialOverlapRegressionSmoke.Run();
        }
    }
}
