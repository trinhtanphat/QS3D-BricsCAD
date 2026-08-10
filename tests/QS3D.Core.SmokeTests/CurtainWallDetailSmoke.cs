using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallDetailSmoke
    {
        public static void Run()
        {
            DetailGridMatchesClearGlassArea();
            SinglePanelKeepsOnlyPerimeterFrames();
            NativeDetailCapRejectsHugeGrid();
        }

        private static CurtainWallLayoutInput Standard() => new CurtainWallLayoutInput
        {
            LengthM = 6d,
            HeightM = 3d,
            MaxPanelWidthM = 1.5d,
            MaxPanelHeightM = 1.5d,
            PerimeterFrameWidthM = 0.05d,
            MullionWidthM = 0.05d,
            TransomWidthM = 0.05d
        };

        private static void DetailGridMatchesClearGlassArea()
        {
            var detail = CurtainWallDetailPlanner.Plan(Standard());
            Equal(8, detail.Panels.Count);
            Equal(5, detail.VerticalFrames.Count);
            Equal(3, detail.HorizontalFrames.Count);
            Equal(16, detail.DetailSolidCount);
            var area = 0d;
            foreach (var panel in detail.Panels) area += panel.AreaM2;
            Near(detail.Layout.ClearGlassAreaM2, area);
            Near(0.05d, detail.VerticalFrames[0].WidthM);
            Near(0.05d, detail.HorizontalFrames[0].HeightM);
        }

        private static void SinglePanelKeepsOnlyPerimeterFrames()
        {
            var detail = CurtainWallDetailPlanner.Plan(new CurtainWallLayoutInput
            {
                LengthM = 1d,
                HeightM = 1d,
                MaxPanelWidthM = 2d,
                MaxPanelHeightM = 2d,
                PerimeterFrameWidthM = 0.05d,
                MullionWidthM = 0.05d,
                TransomWidthM = 0.05d
            });
            Equal(1, detail.Panels.Count);
            Equal(2, detail.VerticalFrames.Count);
            Equal(2, detail.HorizontalFrames.Count);
            Near(0.05d, detail.Panels[0].X_M);
            Near(0.05d, detail.Panels[0].Z_M);
            Near(0.9d, detail.Panels[0].WidthM);
            Near(0.9d, detail.Panels[0].HeightM);
        }

        private static void NativeDetailCapRejectsHugeGrid()
        {
            Throws<InvalidOperationException>(() => CurtainWallDetailPlanner.Plan(new CurtainWallLayoutInput
            {
                LengthM = 200d,
                HeightM = 200d,
                MaxPanelWidthM = 1d,
                MaxPanelHeightM = 1d,
                PerimeterFrameWidthM = 0.02d,
                MullionWidthM = 0.02d,
                TransomWidthM = 0.02d
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
