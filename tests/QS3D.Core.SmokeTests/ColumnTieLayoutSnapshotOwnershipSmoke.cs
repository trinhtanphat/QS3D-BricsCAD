using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieLayoutSnapshotOwnershipSmoke
    {
        public static void Run()
        {
            ConstructorOwnsPathAndElevations();
            PlannerOutputRemainsValid();
        }

        private static void ConstructorOwnsPathAndElevations()
        {
            var path = new List<Point2>
            {
                new Point2(0d, 0d),
                new Point2(1d, 0d),
                new Point2(0d, 0d)
            };
            var elevations = new List<double> { 0.1d, 0.2d };
            var layout = new ColumnTieLayout(path, elevations, 0.1d, 2d);

            path[0] = new Point2(99d, 99d);
            path.Clear();
            elevations[0] = 99d;
            elevations.Clear();

            if (layout.ClosedPath.Count != 3 || layout.ElevationsM.Count != 2)
                throw new InvalidOperationException("Column tie layout collection counts changed after caller-owned lists were mutated.");
            Near(layout.ClosedPath[0].X, 0d, "first path X");
            Near(layout.ClosedPath[0].Y, 0d, "first path Y");
            Near(layout.ClosedPath[1].X, 1d, "second path X");
            Near(layout.ElevationsM[0], 0.1d, "first elevation");
            Near(layout.ElevationsM[1], 0.2d, "second elevation");
        }

        private static void PlannerOutputRemainsValid()
        {
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = 0.4d,
                DepthM = 0.5d,
                HeightM = 1d,
                CoverM = 0.04d,
                DiameterMm = 8d,
                SpacingMm = 200d,
                BottomClearanceM = 0d,
                TopClearanceM = 0d
            });

            if (layout.ClosedPath.Count != 5 || layout.ElevationsM.Count != 6)
                throw new InvalidOperationException("Column tie planner cardinality changed unexpectedly.");
            Near(layout.ClosedPath[0].X, layout.ClosedPath[4].X, "closed path X");
            Near(layout.ClosedPath[0].Y, layout.ClosedPath[4].Y, "closed path Y");
            if (!(layout.ActualSpacingM > 0d) || layout.ActualSpacingM > 0.2d + 1e-12d)
                throw new InvalidOperationException("Column tie planner spacing changed unexpectedly.");
            Near(layout.PathPerimeterM, 1.448d, "path perimeter");
        }

        private static void Near(double actual, double expected, string label)
        {
            if (Math.Abs(actual - expected) > 1e-12d)
                throw new InvalidOperationException("Column tie " + label + " changed unexpectedly.");
        }
    }

    internal static class ColumnTieLayoutSnapshotOwnershipSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ColumnTieLayoutSnapshotOwnershipSmoke.Run();
        }
    }
}
