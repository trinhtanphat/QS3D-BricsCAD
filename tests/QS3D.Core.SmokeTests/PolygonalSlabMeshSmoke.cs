using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonalSlabMeshSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            RectangleMatchesLegacyLengthsAndCount();
            ConcaveFootprintSplitsBarsDeterministically();
            SlopedBoundaryRespectsEuclideanCover();
            SelfIntersectionFailsClosed();
            ImpossibleCoverFailsClosed();
            AggregateBarLimitFailsClosed();
        }

        private static void RectangleMatchesLegacyLengthsAndCount()
        {
            var input = BaseInput(new[]
            {
                new Point2(0d, 0d), new Point2(4d, 0d), new Point2(4d, 3d), new Point2(0d, 3d)
            });
            input.XSpacingMm = 200d;
            input.YSpacingMm = 200d;

            var polygonal = PolygonalSlabMeshPlanner.Plan(input);
            var legacy = RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
            {
                SpanXM = 4d,
                SpanYM = 3d,
                ThicknessM = input.ThicknessM,
                CoverM = input.CoverM,
                XDiameterMm = input.XDiameterMm,
                YDiameterMm = input.YDiameterMm,
                XSpacingMm = input.XSpacingMm,
                YSpacingMm = input.YSpacingMm,
                IncludeBottom = true,
                XClosestToFace = true
            });

            Equal(legacy.Count, polygonal.Count);
            Near(legacy.XActualSpacingM, polygonal.XActualSpacingM);
            Near(legacy.YActualSpacingM, polygonal.YActualSpacingM);
            True(polygonal.Bars.Where(bar => bar.Direction == SlabMeshDirection.X).All(bar => Math.Abs(bar.LengthM - 3.95d) <= 1e-9d));
            True(polygonal.Bars.Where(bar => bar.Direction == SlabMeshDirection.Y).All(bar => Math.Abs(bar.LengthM - 2.95d) <= 1e-9d));
        }

        private static void ConcaveFootprintSplitsBarsDeterministically()
        {
            var input = BaseInput(new[]
            {
                new Point2(0d, 0d), new Point2(5d, 0d), new Point2(5d, 1d),
                new Point2(2d, 1d), new Point2(2d, 4d), new Point2(5d, 4d),
                new Point2(5d, 5d), new Point2(0d, 5d)
            });
            input.XCount = 3;
            input.YCount = 3;

            var layout = PolygonalSlabMeshPlanner.Plan(input);
            var middleVertical = layout.Bars
                .Where(bar => bar.Direction == SlabMeshDirection.Y && Math.Abs(bar.StartM.X - 2.5d) <= 1e-9d)
                .OrderBy(bar => bar.StartM.Y)
                .ToArray();
            Equal(2, middleVertical.Length);
            True(middleVertical[0].EndM.Y < middleVertical[1].StartM.Y);
            True(layout.Bars.All(bar => bar.LengthM > 0d));
        }

        private static void SlopedBoundaryRespectsEuclideanCover()
        {
            var footprint = new[]
            {
                new Point2(0d, 0d), new Point2(4d, 0d), new Point2(5d, 2d), new Point2(4d, 4d), new Point2(0d, 4d)
            };
            var input = BaseInput(footprint);
            input.XCount = 5;
            input.YCount = 5;
            var layout = PolygonalSlabMeshPlanner.Plan(input);

            var xClearance = input.CoverM + input.XDiameterMm / 2000d;
            var yClearance = input.CoverM + input.YDiameterMm / 2000d;
            foreach (var bar in layout.Bars)
            {
                var required = bar.Direction == SlabMeshDirection.X ? xClearance : yClearance;
                AssertBoundaryDistance(footprint, bar.StartM, required);
                AssertBoundaryDistance(footprint, bar.EndM, required);
                AssertBoundaryDistance(footprint, new Point2((bar.StartM.X + bar.EndM.X) / 2d, (bar.StartM.Y + bar.EndM.Y) / 2d), required);
            }
        }

        private static void SelfIntersectionFailsClosed()
        {
            var input = BaseInput(new[]
            {
                new Point2(0d, 0d), new Point2(3d, 3d), new Point2(0d, 3d), new Point2(3d, 0d)
            });
            input.XCount = 3;
            input.YCount = 3;
            Throws<ArgumentException>(() => PolygonalSlabMeshPlanner.Plan(input));
        }

        private static void ImpossibleCoverFailsClosed()
        {
            var input = BaseInput(new[]
            {
                new Point2(0d, 0d), new Point2(.04d, 0d), new Point2(.04d, .04d), new Point2(0d, .04d)
            });
            input.CoverM = .03d;
            input.XCount = 1;
            input.YCount = 1;
            Throws<InvalidOperationException>(() => PolygonalSlabMeshPlanner.Plan(input));
        }

        private static void AggregateBarLimitFailsClosed()
        {
            var input = BaseInput(new[]
            {
                new Point2(0d, 0d), new Point2(100d, 0d), new Point2(100d, 100d), new Point2(0d, 100d)
            });
            input.XCount = 5000;
            input.YCount = 4000;
            Throws<InvalidOperationException>(() => PolygonalSlabMeshPlanner.Plan(input));
        }

        private static PolygonalSlabMeshInput BaseInput(Point2[] footprint) => new PolygonalSlabMeshInput
        {
            FootprintM = footprint,
            ThicknessM = .18d,
            CoverM = .02d,
            XDiameterMm = 10d,
            YDiameterMm = 10d,
            IncludeBottom = true,
            IncludeTop = false,
            XClosestToFace = true
        };

        private static void AssertBoundaryDistance(Point2[] footprint, Point2 point, double minimum)
        {
            var actual = double.PositiveInfinity;
            for (var index = 0; index < footprint.Length; index++)
                actual = Math.Min(actual, DistanceToSegment(point, footprint[index], footprint[(index + 1) % footprint.Length]));
            if (actual + 1e-8d < minimum)
                throw new Exception("Bar centerline violates polygon boundary cover. Required " + minimum + ", got " + actual + ".");
        }

        private static double DistanceToSegment(Point2 point, Point2 a, Point2 b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var squared = dx * dx + dy * dy;
            var t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / squared;
            t = Math.Max(0d, Math.Min(1d, t));
            var x = a.X + dx * t;
            var y = a.Y + dy * t;
            var px = point.X - x;
            var py = point.Y - y;
            return Math.Sqrt(px * px + py * py);
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
