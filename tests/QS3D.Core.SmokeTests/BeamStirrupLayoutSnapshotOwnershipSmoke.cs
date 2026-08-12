using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamStirrupLayoutSnapshotOwnershipSmoke
    {
        public static void Run()
        {
            ConstructorOwnsStationsAndSectionLoop();
            PlannerLegacyOutputRemainsDeterministic();
        }

        private static void ConstructorOwnsStationsAndSectionLoop()
        {
            var stations = new List<double> { -0.5d, 0.5d };
            var loop = new List<Point2>
            {
                new Point2(-1d, -1d),
                new Point2(1d, -1d),
                new Point2(1d, 1d),
                new Point2(-1d, 1d),
                new Point2(-1d, -1d)
            };
            var layout = new BeamStirrupLayout(stations, loop, 1d);

            stations[0] = 99d;
            stations.Clear();
            loop[0] = new Point2(99d, 99d);
            loop.Clear();

            if (layout.Count != 2 || layout.StationOffsetsM.Count != 2 || layout.SectionLoop.Count != 5)
                throw new InvalidOperationException("Beam stirrup layout collection counts changed after caller-owned lists were mutated.");
            Near(layout.StationOffsetsM[0], -0.5d, "first station");
            Near(layout.StationOffsetsM[1], 0.5d, "second station");
            Near(layout.SectionLoop[0].X, -1d, "first section X");
            Near(layout.SectionLoop[0].Y, -1d, "first section Y");
            Near(layout.CenterlineLengthM, 8d, "legacy centerline length");
            Near(layout.PolylineLengthM, 8d, "legacy polyline length");
        }

        private static void PlannerLegacyOutputRemainsDeterministic()
        {
            var layout = BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
            {
                LengthM = 1d,
                WidthM = 0.4d,
                HeightM = 0.5d,
                SectionCoverM = 0.04d,
                EndCoverM = 0.05d,
                DiameterMm = 8d,
                Count = 3,
                BendRadiusM = 0d,
                HookLengthM = 0d,
                HookTailAngleDeg = 0d
            });

            if (layout.Count != 3 || layout.SectionLoop.Count != 5 || layout.HasHookTails)
                throw new InvalidOperationException("Beam stirrup legacy planner cardinality/state changed unexpectedly.");
            Near(layout.CenterlineLengthM, 1.448d, "planner centerline length");
            Near(layout.PolylineLengthM, 1.448d, "planner polyline length");
            Near(layout.ActualSpacingM, 0.446d, "planner actual spacing");
        }

        private static void Near(double actual, double expected, string label)
        {
            if (Math.Abs(actual - expected) > 1e-12d)
                throw new InvalidOperationException("Beam stirrup " + label + " changed unexpectedly.");
        }
    }

    internal static class BeamStirrupLayoutSnapshotOwnershipSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            BeamStirrupLayoutSnapshotOwnershipSmoke.Run();
        }
    }
}
