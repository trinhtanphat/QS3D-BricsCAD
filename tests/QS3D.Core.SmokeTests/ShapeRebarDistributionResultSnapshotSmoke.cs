using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ShapeRebarDistributionResultSnapshotSmoke
    {
        public static void Run()
        {
            ConstructorOwnsOffsetSnapshot();
            PlannerCenteredOffsetsRemainStable();
        }

        private static void ConstructorOwnsOffsetSnapshot()
        {
            var source = new List<double> { -1d, 0d, 1d };
            var result = new ShapeRebarDistributionResult(0.25d, source);

            source[0] = 99d;
            source.Clear();

            if (result.Offsets.Count != 3 ||
                Math.Abs(result.Offsets[0] + 1d) > 1e-12d ||
                Math.Abs(result.Offsets[1]) > 1e-12d ||
                Math.Abs(result.Offsets[2] - 1d) > 1e-12d)
                throw new InvalidOperationException("Shape rebar distribution result changed after mutating its source offsets list.");
        }

        private static void PlannerCenteredOffsetsRemainStable()
        {
            var result = ShapeRebarDistributionPlanner.Plan(new ShapeRebarDistributionInput
            {
                Span = 10d,
                Cover = 1d,
                Radius = 0.5d,
                Count = 3,
                Centered = true
            });

            if (result.Offsets.Count != 3 ||
                Math.Abs(result.CenterClearance - 1.5d) > 1e-12d ||
                Math.Abs(result.Offsets[0] + 3.5d) > 1e-12d ||
                Math.Abs(result.Offsets[1]) > 1e-12d ||
                Math.Abs(result.Offsets[2] - 3.5d) > 1e-12d)
                throw new InvalidOperationException("Normal centered shape rebar distribution changed unexpectedly.");
        }
    }

    internal static class ShapeRebarDistributionResultSnapshotSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ShapeRebarDistributionResultSnapshotSmoke.Run();
        }
    }
}
