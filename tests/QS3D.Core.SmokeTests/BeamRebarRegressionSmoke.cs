using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamRebarRegressionSmoke
    {
        public static void Run()
        {
            SymmetricFourBarLayout();
            AsymmetricLayerCounts();
            RejectsOvercrowdedLayer();
            RejectsCollapsedVerticalEnvelope();
        }

        private static void SymmetricFourBarLayout()
        {
            var layout = BeamLongitudinalRebarPlanner.Plan(new BeamLongitudinalRebarLayoutInput
            {
                WidthM = 0.3d, HeightM = 0.5d, CoverM = 0.04d, DiameterMm = 20d, TopCount = 2, BottomCount = 2
            });
            Equal(4, layout.Count);
            Equal(2, layout.TopBarCenters.Count);
            Equal(2, layout.BottomBarCenters.Count);
            Near(0.2d, layout.TopElevationM);
            Near(-0.2d, layout.BottomElevationM);
            Near(-0.1d, layout.TopBarCenters[0].X);
            Near(0.1d, layout.TopBarCenters[1].X);
        }

        private static void AsymmetricLayerCounts()
        {
            var layout = BeamLongitudinalRebarPlanner.Plan(new BeamLongitudinalRebarLayoutInput
            {
                WidthM = 0.4d, HeightM = 0.6d, CoverM = 0.04d, DiameterMm = 16d, TopCount = 3, BottomCount = 4
            });
            Equal(7, layout.Count);
            Near(0d, layout.TopBarCenters[1].X);
            True(layout.BottomBarCenters[0].X < layout.BottomBarCenters[1].X);
            True(layout.TopElevationM > 0d && layout.BottomElevationM < 0d);
        }

        private static void RejectsOvercrowdedLayer() => Throws<InvalidOperationException>(() => BeamLongitudinalRebarPlanner.Plan(new BeamLongitudinalRebarLayoutInput
        {
            WidthM = 0.12d, HeightM = 0.4d, CoverM = 0.04d, DiameterMm = 20d, TopCount = 4, BottomCount = 2
        }));

        private static void RejectsCollapsedVerticalEnvelope() => Throws<InvalidOperationException>(() => BeamLongitudinalRebarPlanner.Plan(new BeamLongitudinalRebarLayoutInput
        {
            WidthM = 0.3d, HeightM = 0.1d, CoverM = 0.04d, DiameterMm = 20d, TopCount = 2, BottomCount = 2
        }));

        private static void Near(double expected, double actual, double tolerance = 1e-9) { if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
