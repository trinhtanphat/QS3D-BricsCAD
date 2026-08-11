using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonalSlabMultiRegionMeshPlannerSmoke
    {
        internal static void Run()
        {
            PlansEachIslandIndependently();
            CountModeKeepsPerRegionSpacingSemantics();
            HoleSplittingStaysInsideRegionLayout();
            InvalidRegionTopologyFailsBeforeMeshPlanning();
        }

        private static void PlansEachIslandIndependently()
        {
            var layout = PolygonalSlabMultiRegionMeshPlanner.Plan(Input(new[]
            {
                Region("B", Square(20, 0, 28, 4)),
                Region("A", Square(0, 0, 4, 4))
            }));

            Equal(2, layout.Regions.Count);
            Equal("A", layout.Regions[0].RegionId);
            Equal("B", layout.Regions[1].RegionId);
            True(layout.Regions.All(x => x.Count > 0));
            Equal(layout.Regions.Sum(x => x.Count), layout.TotalBarCount);
            True(layout.Regions[0].Layout.Bars.All(x => x.StartM.X < 10 && x.EndM.X < 10));
            True(layout.Regions[1].Layout.Bars.All(x => x.StartM.X > 10 && x.EndM.X > 10));
        }

        private static void CountModeKeepsPerRegionSpacingSemantics()
        {
            var layout = PolygonalSlabMultiRegionMeshPlanner.Plan(Input(new[]
            {
                Region("small", Square(0, 0, 4, 4)),
                Region("wide", Square(20, 0, 28, 4))
            }));

            var small = layout.Regions.Single(x => x.RegionId == "small").Layout;
            var wide = layout.Regions.Single(x => x.RegionId == "wide").Layout;
            True(wide.YActualSpacingM > small.YActualSpacingM);
            Near(small.XActualSpacingM, wide.XActualSpacingM);
        }

        private static void HoleSplittingStaysInsideRegionLayout()
        {
            var withHole = Region("holed", Square(0, 0, 10, 10));
            withHole.HoleFootprintsM = new[] { Square(4, 4, 6, 6) };
            var layout = PolygonalSlabMultiRegionMeshPlanner.Plan(Input(new[]
            {
                withHole,
                Region("plain", Square(20, 0, 30, 10))
            }));

            var holed = layout.Regions.Single(x => x.RegionId == "holed");
            var plain = layout.Regions.Single(x => x.RegionId == "plain");
            True(holed.Count >= plain.Count);
            True(holed.Layout.Bars.All(x => x.StartM.X < 15 && x.EndM.X < 15));
            True(plain.Layout.Bars.All(x => x.StartM.X > 15 && x.EndM.X > 15));
        }

        private static void InvalidRegionTopologyFailsBeforeMeshPlanning()
        {
            Throws<ArgumentException>(() => PolygonalSlabMultiRegionMeshPlanner.Plan(Input(new[]
            {
                Region("A", Square(0, 0, 4, 4)),
                Region("B", Square(4, 1, 8, 3))
            })));
        }

        private static PolygonalSlabMultiRegionMeshInput Input(PolygonalSlabMeshRegionInput[] regions) => new PolygonalSlabMultiRegionMeshInput
        {
            Regions = regions,
            ThicknessM = 0.2,
            CoverM = 0.05,
            XDiameterMm = 12,
            YDiameterMm = 12,
            XCount = 2,
            YCount = 2,
            IncludeBottom = true,
            IncludeTop = false,
            XClosestToFace = true
        };

        private static PolygonalSlabMeshRegionInput Region(string id, Point2[] footprint) => new PolygonalSlabMeshRegionInput
        {
            RegionId = id,
            FootprintM = footprint
        };

        private static Point2[] Square(double minX, double minY, double maxX, double maxY) => new[]
        {
            new Point2(minX, minY),
            new Point2(maxX, minY),
            new Point2(maxX, maxY),
            new Point2(minX, maxY)
        };

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class PolygonalSlabMultiRegionMeshPlannerSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => PolygonalSlabMultiRegionMeshPlannerSmoke.Run();
    }
}
