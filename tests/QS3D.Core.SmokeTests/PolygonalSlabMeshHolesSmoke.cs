using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonalSlabMeshHolesSmoke
    {
        public static void Run()
        {
            EmptyHoleListPreservesSimplePolygonLayout();
            CentralHoleSplitsBarsWithCoverAndRadiusClearance();
            HoleSplitsPhysicalBarsWithoutChangingDistributedScanlines();
            TopBottomElevationsRemainStableWithHoles();
            InvalidHoleTopologyFailsBeforeLayout();
        }

        private static void EmptyHoleListPreservesSimplePolygonLayout()
        {
            var baseline = PolygonalSlabMeshPlanner.Plan(Input());
            var explicitEmpty = Input();
            explicitEmpty.HoleFootprintsM = Array.Empty<IReadOnlyList<Point2>>();
            var planned = PolygonalSlabMeshPlanner.Plan(explicitEmpty);

            Equal(baseline.Count, planned.Count);
            Near(baseline.XActualSpacingM, planned.XActualSpacingM);
            Near(baseline.YActualSpacingM, planned.YActualSpacingM);
            for (var i = 0; i < baseline.Bars.Count; i++)
            {
                Equal(baseline.Bars[i].Direction, planned.Bars[i].Direction);
                Equal(baseline.Bars[i].Face, planned.Bars[i].Face);
                Near(baseline.Bars[i].StartM.X, planned.Bars[i].StartM.X);
                Near(baseline.Bars[i].StartM.Y, planned.Bars[i].StartM.Y);
                Near(baseline.Bars[i].EndM.X, planned.Bars[i].EndM.X);
                Near(baseline.Bars[i].EndM.Y, planned.Bars[i].EndM.Y);
            }
        }

        private static void CentralHoleSplitsBarsWithCoverAndRadiusClearance()
        {
            var input = Input();
            input.HoleFootprintsM = new[] { (IReadOnlyList<Point2>)Square(4, 4, 6, 6) };
            var layout = PolygonalSlabMeshPlanner.Plan(input);
            var middleX = layout.Bars
                .Where(x => x.Face == SlabMeshFace.Bottom && x.Direction == SlabMeshDirection.X && Math.Abs(x.StartM.Y - 5d) < 1e-8)
                .OrderBy(x => x.StartM.X)
                .ToArray();

            Equal(2, middleX.Length);
            Near(0.21d, middleX[0].StartM.X);
            Near(3.79d, middleX[0].EndM.X);
            Near(6.21d, middleX[1].StartM.X);
            Near(9.79d, middleX[1].EndM.X);
            Near(5d, middleX[0].StartM.Y);
            Near(5d, middleX[1].EndM.Y);
        }

        private static void HoleSplitsPhysicalBarsWithoutChangingDistributedScanlines()
        {
            var input = Input();
            input.HoleFootprintsM = new[] { (IReadOnlyList<Point2>)Square(4, 4, 6, 6) };
            var layout = PolygonalSlabMeshPlanner.Plan(input);
            var xBars = layout.Bars.Count(x => x.Face == SlabMeshFace.Bottom && x.Direction == SlabMeshDirection.X);
            var yBars = layout.Bars.Count(x => x.Face == SlabMeshFace.Bottom && x.Direction == SlabMeshDirection.Y);

            Equal(4, xBars);
            Equal(4, yBars);
            Near(4.79d, layout.XActualSpacingM);
            Near(4.79d, layout.YActualSpacingM);
        }

        private static void TopBottomElevationsRemainStableWithHoles()
        {
            var baseline = Input();
            baseline.IncludeTop = true;
            var withHole = Input();
            withHole.IncludeTop = true;
            withHole.HoleFootprintsM = new[] { (IReadOnlyList<Point2>)Square(4, 4, 6, 6) };

            var plain = PolygonalSlabMeshPlanner.Plan(baseline);
            var holed = PolygonalSlabMeshPlanner.Plan(withHole);
            foreach (var face in new[] { SlabMeshFace.Bottom, SlabMeshFace.Top })
            foreach (var direction in new[] { SlabMeshDirection.X, SlabMeshDirection.Y })
            {
                var expected = plain.Bars.First(x => x.Face == face && x.Direction == direction).ElevationOffsetM;
                var actual = holed.Bars.First(x => x.Face == face && x.Direction == direction).ElevationOffsetM;
                Near(expected, actual);
            }
        }

        private static void InvalidHoleTopologyFailsBeforeLayout()
        {
            var outside = Input();
            outside.HoleFootprintsM = new[] { (IReadOnlyList<Point2>)Square(9, 9, 12, 12) };
            Throws<ArgumentException>(() => PolygonalSlabMeshPlanner.Plan(outside));

            var touching = Input();
            touching.HoleFootprintsM = new[] { (IReadOnlyList<Point2>)Square(0, 4, 2, 6) };
            Throws<ArgumentException>(() => PolygonalSlabMeshPlanner.Plan(touching));
        }

        private static PolygonalSlabMeshInput Input()
        {
            return new PolygonalSlabMeshInput
            {
                FootprintM = Square(0, 0, 10, 10),
                ThicknessM = 0.60d,
                CoverM = 0.20d,
                XDiameterMm = 20d,
                YDiameterMm = 20d,
                XCount = 3,
                YCount = 3,
                IncludeBottom = true,
                IncludeTop = false,
                XClosestToFace = true
            };
        }

        private static Point2[] Square(double minX, double minY, double maxX, double maxY) => new[]
        {
            new Point2(minX, minY), new Point2(maxX, minY), new Point2(maxX, maxY), new Point2(minX, maxY)
        };

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-8) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
