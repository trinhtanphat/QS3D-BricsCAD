using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularRebarSafetySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        public static void Run()
        {
            NormalLayoutStillWorks();
            ExcessiveLayoutIsRejectedBeforeAllocation();
            HugeInterpolationIsRejectedCleanly();
        }

        private static void NormalLayoutStillWorks()
        {
            var layout = RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 0.4d,
                DepthM = 0.4d,
                CoverM = 0.04d,
                DiameterMm = 20d,
                BarsAlongWidth = 4,
                BarsAlongDepth = 4
            });
            Equal(12, layout.BarCenters.Count);
        }

        private static void ExcessiveLayoutIsRejectedBeforeAllocation()
        {
            Throws<InvalidOperationException>(() => RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 100d,
                DepthM = 100d,
                CoverM = 0.04d,
                DiameterMm = 20d,
                BarsAlongWidth = RectangularRebarLayoutPlanner.MaxBars,
                BarsAlongDepth = RectangularRebarLayoutPlanner.MaxBars
            }));
        }

        private static void HugeInterpolationIsRejectedCleanly()
        {
            Throws<OverflowException>(() => RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = double.MaxValue,
                DepthM = 1d,
                CoverM = 0d,
                DiameterMm = 10d,
                BarsAlongWidth = 3,
                BarsAlongDepth = 2
            }));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
