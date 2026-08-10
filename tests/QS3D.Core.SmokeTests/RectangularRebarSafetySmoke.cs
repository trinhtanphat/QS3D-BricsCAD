using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularRebarSafetySmoke
    {
        public static void Run()
        {
            NormalPerimeterLayoutIsDeterministic();
            ExcessiveAllocationIsRejected();
            ExtremeFiniteSectionKeepsFiniteCenters();
        }

        private static void NormalPerimeterLayoutIsDeterministic()
        {
            var layout = RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 0.4d,
                DepthM = 0.5d,
                CoverM = 0.04d,
                DiameterMm = 20d,
                BarsAlongWidth = 4,
                BarsAlongDepth = 4
            });
            if (layout.BarCenters.Count != 12) throw new Exception("Expected 12 rectangular perimeter bars.");
            if (!(layout.ClearHalfWidthM > 0d) || !(layout.ClearHalfDepthM > 0d)) throw new Exception("Expected a usable rectangular rebar envelope.");
        }

        private static void ExcessiveAllocationIsRejected()
        {
            Throws<InvalidOperationException>(() => RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 100d,
                DepthM = 100d,
                CoverM = 0d,
                DiameterMm = 1d,
                BarsAlongWidth = 5001,
                BarsAlongDepth = 2
            }));
        }

        private static void ExtremeFiniteSectionKeepsFiniteCenters()
        {
            var layout = RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = double.MaxValue,
                DepthM = double.MaxValue,
                CoverM = 0d,
                DiameterMm = 1d,
                BarsAlongWidth = 2,
                BarsAlongDepth = 2
            });
            foreach (var point in layout.BarCenters)
            {
                if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                    throw new Exception("Extreme finite rectangular layout produced a non-finite center.");
            }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
