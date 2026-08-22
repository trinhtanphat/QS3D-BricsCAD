using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallLayoutSmoke
    {
        public static void Run()
        {
            UniformGridProducesStableQuantities();
            SinglePanelUsesPerimeterFramesOnly();
            RejectsImpossibleFramesAndExcessiveGrid();
        }

        private static void UniformGridProducesStableQuantities()
        {
            var layout = CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
            {
                LengthM = 6d,
                HeightM = 3d,
                MaxPanelWidthM = 1.5d,
                MaxPanelHeightM = 1.5d,
                PerimeterFrameWidthM = 0.05d,
                MullionWidthM = 0.05d,
                TransomWidthM = 0.05d
            });

            Equal(4, layout.Columns);
            Equal(2, layout.Rows);
            Equal(8, layout.PanelCount);
            Equal(5, layout.VerticalFrameCount);
            Equal(3, layout.HorizontalFrameCount);
            Near(1.5d, layout.BayWidthM);
            Near(1.5d, layout.BayHeightM);
            Near(1.425d, layout.MinimumClearPanelWidthM);
            Near(1.45d, layout.MaximumClearPanelWidthM);
            Near(1.425d, layout.MinimumClearPanelHeightM);
            Near(1.425d, layout.MaximumClearPanelHeightM);
            Near(18d, layout.GrossAreaM2);
            Near(16.3875d, layout.ClearGlassAreaM2);
            Near(1.6125d, layout.FrameFaceAreaM2);
            Near(15d, layout.VerticalFrameLengthM);
            Near(18d, layout.HorizontalFrameLengthM);
            Near(33d, layout.TotalFrameLengthM);
        }

        private static void SinglePanelUsesPerimeterFramesOnly()
        {
            var layout = CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
            {
                LengthM = 1d,
                HeightM = 1d,
                MaxPanelWidthM = 2d,
                MaxPanelHeightM = 2d,
                PerimeterFrameWidthM = 0.05d,
                MullionWidthM = 0.05d,
                TransomWidthM = 0.05d
            });
            Equal(1, layout.Columns);
            Equal(1, layout.Rows);
            Equal(1, layout.PanelCount);
            Near(0.9d, layout.MinimumClearPanelWidthM);
            Near(0.9d, layout.MinimumClearPanelHeightM);
            Near(0.81d, layout.ClearGlassAreaM2);
        }

        private static void RejectsImpossibleFramesAndExcessiveGrid()
        {
            Throws<InvalidOperationException>(() => CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
            {
                LengthM = 1d,
                HeightM = 1d,
                MaxPanelWidthM = 2d,
                MaxPanelHeightM = 2d,
                PerimeterFrameWidthM = 0.6d,
                MullionWidthM = 0.05d,
                TransomWidthM = 0.05d
            }));
            Throws<InvalidOperationException>(() => CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
            {
                LengthM = 1000d,
                HeightM = 3d,
                MaxPanelWidthM = 0.0001d,
                MaxPanelHeightM = 1d,
                PerimeterFrameWidthM = 0.01d,
                MullionWidthM = 0.01d,
                TransomWidthM = 0.01d
            }));
            Throws<ArgumentOutOfRangeException>(() => CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
            {
                LengthM = double.NaN,
                HeightM = 3d,
                MaxPanelWidthM = 1d,
                MaxPanelHeightM = 1d
            }));
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
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
