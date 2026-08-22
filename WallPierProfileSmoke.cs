using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallPierProfileSmoke
    {
        public static void Run()
        {
            RectangularProfileMatchesWallVolume();
            ChamferedProfileReducesAreaAndVolume();
            RejectsImpossibleAndNonFiniteProfiles();
        }

        private static void RectangularProfileMatchesWallVolume()
        {
            var profile = WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Rectangular,
                WidthM = 0.6d,
                DepthM = 0.2d,
                HeightM = 3d
            });
            Near(0.12d, profile.CrossSectionAreaM2);
            Near(1.6d, profile.CrossSectionPerimeterM);
            Near(0.36d, profile.VolumeM3);
            Near(4.8d, profile.LateralAreaM2);
            Near(0d, profile.ChamferM);
        }

        private static void ChamferedProfileReducesAreaAndVolume()
        {
            var profile = WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Chamfered,
                WidthM = 0.6d,
                DepthM = 0.2d,
                HeightM = 3d,
                ChamferM = 0.02d
            });
            Near(0.1192d, profile.CrossSectionAreaM2);
            Near(0.3576d, profile.VolumeM3);
            if (!(profile.CrossSectionPerimeterM < 1.6d)) throw new Exception("Chamfered perimeter should be shorter than the rectangular perimeter.");
            if (!(profile.LateralAreaM2 < 4.8d)) throw new Exception("Chamfered lateral area should be lower than the rectangular lateral area.");
        }

        private static void RejectsImpossibleAndNonFiniteProfiles()
        {
            Throws<InvalidOperationException>(() => WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Chamfered,
                WidthM = 0.2d,
                DepthM = 0.2d,
                HeightM = 3d,
                ChamferM = 0.1d
            }));
            Throws<OverflowException>(() => WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                WidthM = double.NaN,
                DepthM = 0.2d,
                HeightM = 3d
            }));
            Throws<ArgumentOutOfRangeException>(() => WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                WidthM = 0.6d,
                DepthM = 0d,
                HeightM = 3d
            }));
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
