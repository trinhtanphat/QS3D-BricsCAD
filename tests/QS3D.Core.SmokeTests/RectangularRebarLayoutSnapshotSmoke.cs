using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularRebarLayoutSnapshotSmoke
    {
        public static void Run()
        {
            ConstructorOwnsCenterSnapshot();
            PlannerTwoByTwoCornersRemainStable();
        }

        private static void ConstructorOwnsCenterSnapshot()
        {
            var source = new List<Point2>
            {
                new Point2(-1d, -2d),
                new Point2(1d, 2d)
            };
            var layout = new RectangularRebarLayout(source, 1d, 2d);

            source[0] = new Point2(99d, 99d);
            source.Clear();

            if (layout.BarCenters.Count != 2 ||
                Math.Abs(layout.BarCenters[0].X + 1d) > 1e-12d ||
                Math.Abs(layout.BarCenters[0].Y + 2d) > 1e-12d ||
                Math.Abs(layout.BarCenters[1].X - 1d) > 1e-12d ||
                Math.Abs(layout.BarCenters[1].Y - 2d) > 1e-12d)
                throw new InvalidOperationException("Rectangular rebar layout changed after mutating its source center list.");
        }

        private static void PlannerTwoByTwoCornersRemainStable()
        {
            var layout = RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
            {
                WidthM = 4d,
                DepthM = 2d,
                CoverM = 0.1d,
                DiameterMm = 20d,
                BarsAlongWidth = 2,
                BarsAlongDepth = 2
            });

            if (layout.BarCenters.Count != 4 ||
                Math.Abs(layout.ClearHalfWidthM - 1.89d) > 1e-12d ||
                Math.Abs(layout.ClearHalfDepthM - 0.89d) > 1e-12d)
                throw new InvalidOperationException("Normal 2x2 rectangular rebar layout changed unexpectedly.");

            AssertPoint(layout.BarCenters[0], -1.89d, -0.89d, "first corner");
            AssertPoint(layout.BarCenters[1], -1.89d, 0.89d, "second corner");
            AssertPoint(layout.BarCenters[2], 1.89d, -0.89d, "third corner");
            AssertPoint(layout.BarCenters[3], 1.89d, 0.89d, "fourth corner");
        }

        private static void AssertPoint(Point2 point, double x, double y, string label)
        {
            if (Math.Abs(point.X - x) > 1e-12d || Math.Abs(point.Y - y) > 1e-12d)
                throw new InvalidOperationException("Rectangular rebar " + label + " changed unexpectedly.");
        }
    }

    internal static class RectangularRebarLayoutSnapshotSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RectangularRebarLayoutSnapshotSmoke.Run();
        }
    }
}
