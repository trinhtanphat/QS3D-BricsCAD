using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallDivisionCountBoundarySmoke
    {
        public static void Run()
        {
            SlightlyOverWidthBoundaryAddsColumn();
            SlightlyOverHeightBoundaryAddsRow();
            ExactBoundariesStayExact();
        }

        private static void SlightlyOverWidthBoundaryAddsColumn()
        {
            var layout = Plan(1d + 5e-13d, 1d);

            Equal(2, layout.Columns);
            Equal(1, layout.Rows);
            AtMost(1d, layout.BayWidthM);
        }

        private static void SlightlyOverHeightBoundaryAddsRow()
        {
            var layout = Plan(1d, 1d + 5e-13d);

            Equal(1, layout.Columns);
            Equal(2, layout.Rows);
            AtMost(1d, layout.BayHeightM);
        }

        private static void ExactBoundariesStayExact()
        {
            var layout = Plan(2d, 3d);

            Equal(2, layout.Columns);
            Equal(3, layout.Rows);
            Equal(6, layout.PanelCount);
            Equal(1d, layout.BayWidthM);
            Equal(1d, layout.BayHeightM);
        }

        private static CurtainWallLayout Plan(double lengthM, double heightM)
        {
            return CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
            {
                LengthM = lengthM,
                HeightM = heightM,
                MaxPanelWidthM = 1d,
                MaxPanelHeightM = 1d,
                PerimeterFrameWidthM = 0d,
                MullionWidthM = 0d,
                TransomWidthM = 0d
            });
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(double expected, double actual)
        {
            if (expected != actual)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void AtMost(double maximum, double actual)
        {
            if (actual > maximum)
                throw new Exception("Expected at most " + maximum + ", got " + actual + ".");
        }
    }
}
