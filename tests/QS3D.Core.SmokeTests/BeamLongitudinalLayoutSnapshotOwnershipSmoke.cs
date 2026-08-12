using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamLongitudinalLayoutSnapshotOwnershipSmoke
    {
        public static void Run()
        {
            ConstructorOwnsTopAndBottomCenters();
            PlannerOutputRemainsDeterministic();
        }

        private static void ConstructorOwnsTopAndBottomCenters()
        {
            var top = new List<Point2> { new Point2(-1d, 1d), new Point2(1d, 1d) };
            var bottom = new List<Point2> { new Point2(-1d, -1d), new Point2(1d, -1d) };
            var layout = new BeamLongitudinalRebarLayout(top, bottom, 1d, -1d);

            top[0] = new Point2(99d, 99d);
            top.Clear();
            bottom[0] = new Point2(99d, 99d);
            bottom.Clear();

            if (layout.TopBarCenters.Count != 2 || layout.BottomBarCenters.Count != 2 || layout.Count != 4)
                throw new InvalidOperationException("Beam longitudinal layout counts changed after caller-owned lists were mutated.");
            Near(layout.TopBarCenters[0].X, -1d, "top first X");
            Near(layout.TopBarCenters[0].Y, 1d, "top first Y");
            Near(layout.BottomBarCenters[0].X, -1d, "bottom first X");
            Near(layout.BottomBarCenters[0].Y, -1d, "bottom first Y");
        }

        private static void PlannerOutputRemainsDeterministic()
        {
            var layout = BeamLongitudinalRebarPlanner.Plan(new BeamLongitudinalRebarLayoutInput
            {
                WidthM = 0.4d,
                HeightM = 0.5d,
                CoverM = 0.04d,
                DiameterMm = 16d,
                TopCount = 3,
                BottomCount = 3
            });

            if (layout.TopBarCenters.Count != 3 || layout.BottomBarCenters.Count != 3 || layout.Count != 6)
                throw new InvalidOperationException("Beam longitudinal planner cardinality changed unexpectedly.");
            Near(layout.TopElevationM, 0.202d, "top elevation");
            Near(layout.BottomElevationM, -0.202d, "bottom elevation");
            Near(layout.TopBarCenters[0].X, -0.152d, "top first X");
            Near(layout.TopBarCenters[1].X, 0d, "top middle X");
            Near(layout.TopBarCenters[2].X, 0.152d, "top last X");
            Near(layout.BottomBarCenters[0].Y, -0.202d, "bottom first Y");
        }

        private static void Near(double actual, double expected, string label)
        {
            if (Math.Abs(actual - expected) > 1e-12d)
                throw new InvalidOperationException("Beam longitudinal " + label + " changed unexpectedly.");
        }
    }

    internal static class BeamLongitudinalLayoutSnapshotOwnershipSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            BeamLongitudinalLayoutSnapshotOwnershipSmoke.Run();
        }
    }
}
