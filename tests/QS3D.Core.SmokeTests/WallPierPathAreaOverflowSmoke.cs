using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallPierPathAreaOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var profile = WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
            {
                Centerline = new[]
                {
                    new Point2(0d, 0d),
                    new Point2(1e160, 1e160)
                },
                ThicknessM = 1e145,
                HeightM = 1d,
                Mode = WallPierProfileMode.Rectangular
            });

            if (profile.Polygon.Count != 4)
                throw new Exception("Expected a four-vertex rectangular WallPier path profile.");
            if (!Finite(profile.CenterlineLengthM) || !(profile.CenterlineLengthM > 0d))
                throw new Exception("Expected a finite positive WallPier path centerline length.");
            if (!Finite(profile.FootprintAreaM2) || !(profile.FootprintAreaM2 > 0d))
                throw new Exception("Expected a finite positive WallPier path footprint area.");
            if (!Finite(profile.FootprintPerimeterM) || !(profile.FootprintPerimeterM > 0d))
                throw new Exception("Expected a finite positive WallPier path footprint perimeter.");
            if (!Finite(profile.VolumeM3) || profile.VolumeM3 != profile.FootprintAreaM2)
                throw new Exception("Expected finite WallPier volume to equal footprint area at unit height.");
            if (!Finite(profile.LateralAreaM2) || profile.LateralAreaM2 != profile.FootprintPerimeterM)
                throw new Exception("Expected finite WallPier lateral area to equal footprint perimeter at unit height.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
