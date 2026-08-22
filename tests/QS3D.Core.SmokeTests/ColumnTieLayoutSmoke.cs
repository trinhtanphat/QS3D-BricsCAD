using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieLayoutSmoke
    {
        public static void Run()
        {
            SpacingIsMaximumNotMinimum();
            NearIntegerSpacingDoesNotAddPhantomTie();
            TrueSpacingOverrunStillAddsTie();
            SingleTieWhenUsableRangeCollapses();
            RejectsImpossibleCoverAndBadSpacing();
        }

        private static void SpacingIsMaximumNotMinimum()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.4d,
                DepthM = 0.5d,
                HeightM = 3d,
                CoverM = 0.04d,
                DiameterMm = 8d,
                SpacingMm = 150d,
                BottomClearanceM = 0d,
                TopClearanceM = 0d
            });
            if (layout.ElevationsM.Count < 2) throw new Exception("Expected multiple ties.");
            if (layout.ActualSpacingM > 0.150000000001d) throw new Exception("Tie spacing exceeded requested maximum.");
            Near(0.044d, layout.ElevationsM[0], 1e-12d);
            Near(2.956d, layout.ElevationsM[layout.ElevationsM.Count - 1], 1e-12d);
            // Centerline envelope: 2 * ((0.4 - 2 * 0.044) + (0.5 - 2 * 0.044)).
            Near(1.448d, layout.PathPerimeterM, 1e-12d);
            if (layout.ClosedPath.Count != 5) throw new Exception("Expected closed rectangular path with repeated start point.");
        }

        private static void NearIntegerSpacingDoesNotAddPhantomTie()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.3d,
                DepthM = 0.3d,
                HeightM = 1.056d,
                CoverM = 0.025d,
                DiameterMm = 6d,
                SpacingMm = 100d
            });

            if (layout.ElevationsM.Count != 11) throw new Exception("Near-integer spacing added a phantom column tie.");
            Near(0.1d, layout.ActualSpacingM, 1e-12d);
            Near(0.028d, layout.ElevationsM[0], 1e-12d);
            Near(1.028d, layout.ElevationsM[layout.ElevationsM.Count - 1], 1e-12d);
        }

        private static void TrueSpacingOverrunStillAddsTie()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.3d,
                DepthM = 0.3d,
                HeightM = 1.0560000001d,
                CoverM = 0.025d,
                DiameterMm = 6d,
                SpacingMm = 100d
            });

            if (layout.ElevationsM.Count != 12) throw new Exception("A real column-tie spacing overrun was incorrectly snapped down.");
            if (layout.ActualSpacingM > 0.100000000001d) throw new Exception("Tie spacing exceeded requested maximum after a real overrun.");
        }

        private static void SingleTieWhenUsableRangeCollapses()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.3d,
                DepthM = 0.3d,
                HeightM = 0.088d,
                CoverM = 0.04d,
                DiameterMm = 8d,
                SpacingMm = 100d
            });
            if (layout.ElevationsM.Count != 1) throw new Exception("Expected one tie at the only usable elevation.");
            Near(0d, layout.ActualSpacingM, 1e-12d);
        }

        private static void RejectsImpossibleCoverAndBadSpacing()
        {
            Throws<InvalidOperationException>(() => ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.2d, DepthM = 0.2d, HeightM = 3d,
                CoverM = 0.1d, DiameterMm = 20d, SpacingMm = 150d
            }));
            Throws<ArgumentOutOfRangeException>(() => ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.3d, DepthM = 0.3d, HeightM = 3d,
                CoverM = 0.04d, DiameterMm = 8d, SpacingMm = 0d
            }));
            Throws<InvalidOperationException>(() => ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.3d, DepthM = 0.3d, HeightM = 3d,
                CoverM = 0.04d, DiameterMm = 8d, SpacingMm = 0.1d
            }));
        }

        private static void Near(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
