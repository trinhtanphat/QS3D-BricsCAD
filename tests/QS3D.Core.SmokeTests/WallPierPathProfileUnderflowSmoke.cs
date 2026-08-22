using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallPierPathProfileUnderflowSmoke
    {
        internal static void Run()
        {
            PositivePathVolumeUnderflowFailsClosed();
            TinyRepresentablePathProfileRemainsValid();
        }

        private static void PositivePathVolumeUnderflowFailsClosed()
        {
            Throws<OverflowException>(() => WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
            {
                Centerline = new[] { new Point2(0d, 0d), new Point2(1d, 0d) },
                ThicknessM = 1e-200d,
                HeightM = 1e-200d,
                Tolerance = 1e-210d,
                Mode = WallPierProfileMode.Rectangular
            }));
        }

        private static void TinyRepresentablePathProfileRemainsValid()
        {
            var profile = WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
            {
                Centerline = new[] { new Point2(0d, 0d), new Point2(1d, 0d) },
                ThicknessM = 1e-150d,
                HeightM = 1e-150d,
                Tolerance = 1e-160d,
                Mode = WallPierProfileMode.Rectangular
            });
            if (!(profile.FootprintAreaM2 > 0d) || !(profile.VolumeM3 > 0d) || !(profile.LateralAreaM2 > 0d))
                throw new Exception("Tiny but representable wall-pier path quantities must remain positive.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class WallPierPathProfileUnderflowRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => WallPierPathProfileUnderflowSmoke.Run();
    }
}
