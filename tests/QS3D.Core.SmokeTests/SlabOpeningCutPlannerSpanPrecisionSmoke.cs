using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class SlabOpeningCutPlannerSpanPrecisionSmoke
    {
        public static void Run()
        {
            UsesEndpointCanonicalSpanForDecimalInputs();
            PreservesEndpointDefinitions();
        }

        private static void UsesEndpointCanonicalSpanForDecimalInputs()
        {
            var plan = SlabOpeningCutPlanner.Plan(new SlabOpeningCutInput
            {
                HostBottomM = 0.1d,
                HostThicknessM = 0.2d,
                ClearanceM = 0.2d
            });

            Equal(0.5d, plan.CutterTopM);
            Equal(-0.1d, plan.CutterBottomM);
            Equal(plan.CutterBottomM - plan.CutterTopM, plan.ExtrusionZM);
            Equal(-plan.ExtrusionZM, plan.CutterHeightM);
            Equal(plan.CutterTopM - plan.CutterBottomM, plan.CutterHeightM);
        }

        private static void PreservesEndpointDefinitions()
        {
            var input = new SlabOpeningCutInput
            {
                HostBottomM = 2.5d,
                HostThicknessM = 0.2d,
                ClearanceM = 0.01d
            };
            var expectedTop = (input.HostBottomM + input.HostThicknessM) + input.ClearanceM;
            var expectedBottom = input.HostBottomM - input.ClearanceM;

            var plan = SlabOpeningCutPlanner.Plan(input);

            Equal(expectedTop, plan.CutterTopM);
            Equal(expectedBottom, plan.CutterBottomM);
        }

        private static void Equal(double expected, double actual)
        {
            if (expected != actual)
                throw new Exception("Expected " + expected.ToString("R") + ", got " + actual.ToString("R") + ".");
        }
    }
}
