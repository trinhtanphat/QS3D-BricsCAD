using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallPierProfileUnderflowSmoke
    {
        internal static void Run()
        {
            CrossSectionUnderflowFailsClosed();
            VolumeUnderflowFailsClosed();
            ChamferContributionUnderflowFailsClosed();
            TinyRepresentableProfileRemainsValid();
        }

        private static void CrossSectionUnderflowFailsClosed()
        {
            Throws<OverflowException>(() => WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Rectangular,
                WidthM = 1e-200d,
                DepthM = 1e-200d,
                HeightM = 1d
            }));
        }

        private static void VolumeUnderflowFailsClosed()
        {
            Throws<OverflowException>(() => WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Rectangular,
                WidthM = 1e-200d,
                DepthM = 1e-100d,
                HeightM = 1e-100d
            }));
        }

        private static void ChamferContributionUnderflowFailsClosed()
        {
            Throws<OverflowException>(() => WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Chamfered,
                WidthM = 1d,
                DepthM = 1d,
                HeightM = 1d,
                ChamferM = 1e-200d
            }));
        }

        private static void TinyRepresentableProfileRemainsValid()
        {
            var profile = WallPierProfilePlanner.Plan(new WallPierProfileInput
            {
                Mode = WallPierProfileMode.Rectangular,
                WidthM = 1e-150d,
                DepthM = 1e-150d,
                HeightM = 1d
            });
            if (!(profile.CrossSectionAreaM2 > 0d) || !(profile.VolumeM3 > 0d) || !(profile.LateralAreaM2 > 0d))
                throw new Exception("Tiny but representable wall-pier profile quantities must remain positive.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class WallPierProfileUnderflowRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => WallPierProfileUnderflowSmoke.Run();
    }
}
