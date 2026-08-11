using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class LinearRebarLayoutSnapshotOwnershipSmoke
    {
        public static void Run()
        {
            ConstructorOwnsOffsetsSnapshot();
            PlannerOutputRemainsDeterministic();
        }

        private static void ConstructorOwnsOffsetsSnapshot()
        {
            var source = new List<double> { -1d, 0d, 1d };
            var layout = new LinearRebarLayout(source, 2d, 1d);

            source[0] = 99d;
            source.Clear();

            if (layout.Count != 3 || layout.OffsetsM.Count != 3)
                throw new InvalidOperationException("Linear rebar layout count changed after mutating the caller-owned source list.");
            if (layout.OffsetsM[0] != -1d || layout.OffsetsM[1] != 0d || layout.OffsetsM[2] != 1d)
                throw new InvalidOperationException("Linear rebar layout offsets changed after mutating the caller-owned source list.");
        }

        private static void PlannerOutputRemainsDeterministic()
        {
            var layout = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 1d,
                CoverM = 0.05d,
                DiameterMm = 10d,
                Count = 3
            });

            if (layout.Count != 3)
                throw new InvalidOperationException("Linear rebar planner count changed unexpectedly.");
            Near(layout.UsableSpanM, 0.89d, "usable span");
            Near(layout.ActualSpacingM, 0.445d, "actual spacing");
            Near(layout.OffsetsM[0], -0.445d, "first offset");
            Near(layout.OffsetsM[1], 0d, "middle offset");
            Near(layout.OffsetsM[2], 0.445d, "last offset");
        }

        private static void Near(double actual, double expected, string label)
        {
            if (Math.Abs(actual - expected) > 1e-12d)
                throw new InvalidOperationException("Linear rebar " + label + " changed unexpectedly.");
        }
    }

    internal static class LinearRebarLayoutSnapshotOwnershipSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LinearRebarLayoutSnapshotOwnershipSmoke.Run();
        }
    }
}
