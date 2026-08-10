using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class SlabMeshRegressionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            BottomMeshUsesTwoDirectionsAndCover();
            BothFacesRemainSeparated();
            CountModeIsDeterministic();
            ThinSlabIsRejected();
            AmbiguousDistributionIsRejected();
            OversizedAggregateMeshIsRejected();
        }

        private static void BottomMeshUsesTwoDirectionsAndCover()
        {
            var layout = RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
            {
                SpanXM = 4d,
                SpanYM = 3d,
                ThicknessM = .18d,
                CoverM = .02d,
                XDiameterMm = 10d,
                YDiameterMm = 10d,
                XSpacingMm = 200d,
                YSpacingMm = 200d,
                IncludeBottom = true,
                IncludeTop = false,
                XClosestToFace = true
            });
            True(layout.Count > 0);
            True(layout.Bars.All(x => x.Face == SlabMeshFace.Bottom));
            True(layout.Bars.Any(x => x.Direction == SlabMeshDirection.X));
            True(layout.Bars.Any(x => x.Direction == SlabMeshDirection.Y));
            var x = layout.Bars.First(b => b.Direction == SlabMeshDirection.X);
            var y = layout.Bars.First(b => b.Direction == SlabMeshDirection.Y);
            Near(3.95d, x.LengthM);
            Near(2.95d, y.LengthM);
            Near(-.065d, x.ElevationOffsetM);
            Near(-.055d, y.ElevationOffsetM);
            True(layout.XActualSpacingM > 0d && layout.XActualSpacingM <= .200000001d);
            True(layout.YActualSpacingM > 0d && layout.YActualSpacingM <= .200000001d);
        }

        private static void BothFacesRemainSeparated()
        {
            var layout = RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
            {
                SpanXM = 5d,
                SpanYM = 4d,
                ThicknessM = .22d,
                CoverM = .025d,
                XDiameterMm = 12d,
                YDiameterMm = 10d,
                XSpacingMm = 200d,
                YSpacingMm = 200d,
                IncludeBottom = true,
                IncludeTop = true
            });
            True(layout.Bars.Any(x => x.Face == SlabMeshFace.Bottom));
            True(layout.Bars.Any(x => x.Face == SlabMeshFace.Top));
            var bottomMax = layout.Bars.Where(x => x.Face == SlabMeshFace.Bottom).Max(x => x.ElevationOffsetM + x.DiameterMm / 2000d);
            var topMin = layout.Bars.Where(x => x.Face == SlabMeshFace.Top).Min(x => x.ElevationOffsetM - x.DiameterMm / 2000d);
            True(topMin > bottomMax);
        }

        private static void CountModeIsDeterministic()
        {
            var layout = RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
            {
                SpanXM = 2d,
                SpanYM = 1.5d,
                ThicknessM = .16d,
                CoverM = .02d,
                XDiameterMm = 8d,
                YDiameterMm = 8d,
                XCount = 4,
                YCount = 5,
                IncludeBottom = true
            });
            Equal(9, layout.Count);
            Equal(4, layout.Bars.Count(x => x.Direction == SlabMeshDirection.X));
            Equal(5, layout.Bars.Count(x => x.Direction == SlabMeshDirection.Y));
        }

        private static void ThinSlabIsRejected()
        {
            Throws<InvalidOperationException>(() => RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
            {
                SpanXM = 2d,
                SpanYM = 2d,
                ThicknessM = .08d,
                CoverM = .03d,
                XDiameterMm = 16d,
                YDiameterMm = 16d,
                XSpacingMm = 150d,
                YSpacingMm = 150d,
                IncludeBottom = true,
                IncludeTop = true
            }));
        }

        private static void AmbiguousDistributionIsRejected()
        {
            Throws<InvalidOperationException>(() => RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
            {
                SpanXM = 2d,
                SpanYM = 2d,
                ThicknessM = .2d,
                CoverM = .02d,
                XDiameterMm = 10d,
                YDiameterMm = 10d,
                XSpacingMm = 200d,
                XCount = 5,
                YSpacingMm = 200d,
                IncludeBottom = true
            }));
        }

        private static void OversizedAggregateMeshIsRejected()
        {
            Throws<InvalidOperationException>(() => RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
            {
                SpanXM = 100d,
                SpanYM = 100d,
                ThicknessM = .3d,
                CoverM = .02d,
                XDiameterMm = 8d,
                YDiameterMm = 8d,
                XCount = 3000,
                YCount = 2000,
                IncludeBottom = true,
                IncludeTop = true
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
