using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class OrthogonalRebarMatSmoke
    {
        public static void Run()
        {
            BottomMat();
            BothFaces();
            RejectsThinHost();
            RejectsOvercrowdedSpacing();
            RejectsDisabledFaces();
        }

        private static void BottomMat()
        {
            var layout = OrthogonalRebarMatPlanner.Plan(new OrthogonalRebarMatInput
            {
                WidthM = 4d,
                DepthM = 3d,
                ThicknessM = .18d,
                CoverM = .025d,
                XDiameterMm = 12d,
                YDiameterMm = 10d,
                XSpacingMm = 200d,
                YSpacingMm = 200d,
                BottomEnabled = true,
                TopEnabled = false
            });
            Require(layout.Count > 20, "bottom mat should create both directions");
            Require(layout.XActualSpacingM <= .2d + 1e-12d, "X spacing should not exceed requested maximum");
            Require(layout.YActualSpacingM <= .2d + 1e-12d, "Y spacing should not exceed requested maximum");
            foreach (var bar in layout.Bars)
            {
                Require(bar.Face == OrthogonalRebarMatFace.Bottom, "bottom-only plan emitted top bar");
                Require(bar.ElevationFromBottomM > 0d && bar.ElevationFromBottomM < .18d, "bottom bar elevation outside host");
            }
        }

        private static void BothFaces()
        {
            var layout = OrthogonalRebarMatPlanner.Plan(new OrthogonalRebarMatInput
            {
                WidthM = 5d,
                DepthM = 4d,
                ThicknessM = .3d,
                CoverM = .03d,
                XDiameterMm = 16d,
                YDiameterMm = 14d,
                XSpacingMm = 150d,
                YSpacingMm = 175d,
                BottomEnabled = true,
                TopEnabled = true
            });
            var bottom = 0;
            var top = 0;
            foreach (var bar in layout.Bars)
            {
                if (bar.Face == OrthogonalRebarMatFace.Bottom) bottom++;
                else top++;
            }
            Require(bottom > 0 && bottom == top, "both-face mat should produce symmetric face counts");
        }

        private static void RejectsThinHost()
        {
            ExpectInvalid(() => OrthogonalRebarMatPlanner.Plan(new OrthogonalRebarMatInput
            {
                WidthM = 1d,
                DepthM = 1d,
                ThicknessM = .07d,
                CoverM = .025d,
                XDiameterMm = 16d,
                YDiameterMm = 16d,
                XSpacingMm = 150d,
                YSpacingMm = 150d,
                BottomEnabled = true,
                TopEnabled = true
            }));
        }

        private static void RejectsOvercrowdedSpacing()
        {
            ExpectInvalid(() => OrthogonalRebarMatPlanner.Plan(new OrthogonalRebarMatInput
            {
                WidthM = .5d,
                DepthM = .5d,
                ThicknessM = .2d,
                CoverM = .02d,
                XDiameterMm = 20d,
                YDiameterMm = 20d,
                XSpacingMm = 10d,
                YSpacingMm = 10d,
                BottomEnabled = true
            }));
        }

        private static void RejectsDisabledFaces()
        {
            ExpectInvalid(() => OrthogonalRebarMatPlanner.Plan(new OrthogonalRebarMatInput
            {
                WidthM = 1d,
                DepthM = 1d,
                ThicknessM = .2d,
                CoverM = .02d,
                XDiameterMm = 10d,
                YDiameterMm = 10d,
                XSpacingMm = 200d,
                YSpacingMm = 200d,
                BottomEnabled = false,
                TopEnabled = false
            }));
        }

        private static void ExpectInvalid(Action action)
        {
            try { action(); }
            catch (Exception) { return; }
            throw new InvalidOperationException("Expected orthogonal rebar mat validation failure.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("OrthogonalRebarMatSmoke: " + message);
        }
    }
}
