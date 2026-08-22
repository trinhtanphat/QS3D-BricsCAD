using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class WallQuantityNullOpeningSmoke
    {
        internal static void Run()
        {
            NullCollectionStillMeansNoOpenings();
            NullEntryFailsClosed();
            ValidOpeningsRemainUnchanged();
        }

        private static void NullCollectionStillMeansNoOpenings()
        {
            var result = WallQuantityCalculator.Calculate(5d, 3d, 0.2d, null);
            Near(15d, result.GrossAreaM2, 1e-12, "null collection gross area");
            Near(0d, result.OpeningAreaM2, 0d, "null collection opening area");
            Near(15d, result.NetAreaM2, 1e-12, "null collection net area");
            Near(3d, result.NetVolumeM3, 1e-12, "null collection net volume");
        }

        private static void NullEntryFailsClosed()
        {
            var openings = new OpeningCut[] { new OpeningCut { WidthM = 0.9d, HeightM = 2d }, null! };
            Throws<ArgumentException>(() => WallQuantityCalculator.Calculate(5d, 3d, 0.2d, openings), "null opening entry");
        }

        private static void ValidOpeningsRemainUnchanged()
        {
            var openings = new[] { new OpeningCut { WidthM = 0.9d, HeightM = 2.2d } };
            var result = WallQuantityCalculator.Calculate(5d, 3d, 0.2d, openings);
            Near(1.98d, result.OpeningAreaM2, 1e-12, "valid opening area");
            Near(13.02d, result.NetAreaM2, 1e-12, "valid net area");
            Near(2.604d, result.NetVolumeM3, 1e-12, "valid net volume");

            var oversized = WallQuantityCalculator.Calculate(
                5d,
                3d,
                0.2d,
                new[] { new OpeningCut { WidthM = 10d, HeightM = 10d } });
            Near(15d, oversized.OpeningAreaM2, 1e-12, "oversized opening clamp");
            Near(0d, oversized.NetAreaM2, 0d, "oversized net area");
            Near(0d, oversized.NetVolumeM3, 0d, "oversized net volume");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("WallQuantityNullOpeningSmoke " + label + ": expected " + typeof(TException).Name + ".");
        }

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new Exception("WallQuantityNullOpeningSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class WallQuantityNullOpeningSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => WallQuantityNullOpeningSmoke.Run();
    }
}
