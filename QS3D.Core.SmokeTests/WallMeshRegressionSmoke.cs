using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class WallMeshRegressionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            TwoFaceMeshUsesBothDirections();
            SingleFaceCountModeIsDeterministic();
            ThinWallIsRejected();
            AmbiguousDistributionIsRejected();
        }

        private static void TwoFaceMeshUsesBothDirections()
        {
            var layout = RectangularWallMeshPlanner.Plan(new RectangularWallMeshInput
            {
                LengthM = 4d,
                HeightM = 3d,
                ThicknessM = .2d,
                CoverM = .02d,
                HorizontalDiameterMm = 10d,
                VerticalDiameterMm = 12d,
                HorizontalSpacingMm = 200d,
                VerticalSpacingMm = 200d,
                IncludeNear = true,
                IncludeFar = true,
                HorizontalClosestToFace = true
            });
            True(layout.Count > 0);
            True(layout.Bars.Any(x => x.Face == WallMeshFace.Near));
            True(layout.Bars.Any(x => x.Face == WallMeshFace.Far));
            True(layout.Bars.Any(x => x.Direction == WallMeshDirection.Horizontal));
            True(layout.Bars.Any(x => x.Direction == WallMeshDirection.Vertical));
            var nearH = layout.Bars.First(x => x.Face == WallMeshFace.Near && x.Direction == WallMeshDirection.Horizontal);
            var nearV = layout.Bars.First(x => x.Face == WallMeshFace.Near && x.Direction == WallMeshDirection.Vertical);
            Near(-.075d, nearH.FaceOffsetM);
            Near(-.064d, nearV.FaceOffsetM);
            Near(3.95d, nearH.LengthM);
            Near(2.948d, nearV.LengthM);
            True(layout.HorizontalActualSpacingM > 0d && layout.HorizontalActualSpacingM <= .200000001d);
            True(layout.VerticalActualSpacingM > 0d && layout.VerticalActualSpacingM <= .200000001d);
        }

        private static void SingleFaceCountModeIsDeterministic()
        {
            var layout = RectangularWallMeshPlanner.Plan(new RectangularWallMeshInput
            {
                LengthM = 2.5d,
                HeightM = 2.8d,
                ThicknessM = .18d,
                CoverM = .02d,
                HorizontalDiameterMm = 8d,
                VerticalDiameterMm = 8d,
                HorizontalCount = 5,
                VerticalCount = 4,
                IncludeNear = true,
                IncludeFar = false
            });
            Equal(9, layout.Count);
            Equal(5, layout.Bars.Count(x => x.Direction == WallMeshDirection.Horizontal));
            Equal(4, layout.Bars.Count(x => x.Direction == WallMeshDirection.Vertical));
            True(layout.Bars.All(x => x.Face == WallMeshFace.Near));
        }

        private static void ThinWallIsRejected()
        {
            Throws<InvalidOperationException>(() => RectangularWallMeshPlanner.Plan(new RectangularWallMeshInput
            {
                LengthM = 3d,
                HeightM = 3d,
                ThicknessM = .09d,
                CoverM = .03d,
                HorizontalDiameterMm = 16d,
                VerticalDiameterMm = 16d,
                HorizontalSpacingMm = 150d,
                VerticalSpacingMm = 150d,
                IncludeNear = true,
                IncludeFar = true
            }));
        }

        private static void AmbiguousDistributionIsRejected()
        {
            Throws<InvalidOperationException>(() => RectangularWallMeshPlanner.Plan(new RectangularWallMeshInput
            {
                LengthM = 3d,
                HeightM = 3d,
                ThicknessM = .2d,
                CoverM = .02d,
                HorizontalDiameterMm = 10d,
                VerticalDiameterMm = 10d,
                HorizontalSpacingMm = 200d,
                HorizontalCount = 10,
                VerticalSpacingMm = 200d,
                IncludeNear = true
            }));
        }

        private static void Near(double expected, double actual, double tolerance = 1e-9d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
