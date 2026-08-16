using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class OpeningCutPlannerClearancePrecisionSmoke
    {
        public static void Run()
        {
            RejectsCollapsedVerticalClearanceAtLargeElevation();
            RejectsCollapsedTopClearanceAtLargeElevation();
            RejectsCollapsedDimensionExpansionAtLargeMagnitude();
            RejectsSubnormalPositiveClearanceCollapse();
            PreservesOrdinaryPositiveClearance();
            PreservesZeroClearance();
        }

        private static void RejectsCollapsedVerticalClearanceAtLargeElevation()
        {
            var input = OrdinaryInput();
            input.HostHeightM = 10000000000000008d;
            input.OpeningHeightM = 4d;
            input.SillHeightM = 10000000000000000d;
            input.ClearanceM = 1d;

            Throws<OverflowException>(() => OpeningCutPlanner.Plan(input));
        }

        private static void RejectsCollapsedTopClearanceAtLargeElevation()
        {
            var input = OrdinaryInput();
            input.HostHeightM = 9007199254741000d;
            input.OpeningHeightM = 4d;
            input.SillHeightM = 9007199254740992d;
            input.ClearanceM = 1d;

            Throws<OverflowException>(() => OpeningCutPlanner.Plan(input));
        }

        private static void RejectsCollapsedDimensionExpansionAtLargeMagnitude()
        {
            var input = OrdinaryInput();
            input.HostThicknessM = 10000000000000000d;
            input.ClearanceM = 0.25d;

            Throws<OverflowException>(() => OpeningCutPlanner.Plan(input));
        }

        private static void RejectsSubnormalPositiveClearanceCollapse()
        {
            var input = OrdinaryInput();
            input.ClearanceM = double.Epsilon;

            Throws<OverflowException>(() => OpeningCutPlanner.Plan(input));
        }

        private static void PreservesOrdinaryPositiveClearance()
        {
            var input = OrdinaryInput();
            var plan = OpeningCutPlanner.Plan(input);

            Near(1.02d, plan.CutterWidthM);
            Near(0.22d, plan.CutterDepthM);
            Near(2.02d, plan.CutterHeightM);
            Near(0.49d, plan.BaseElevationM);
            Near(2.51d, plan.TopElevationM);
        }

        private static void PreservesZeroClearance()
        {
            var input = OrdinaryInput();
            input.ClearanceM = 0d;
            var plan = OpeningCutPlanner.Plan(input);

            Equal(input.OpeningWidthM, plan.CutterWidthM);
            Equal(input.HostThicknessM, plan.CutterDepthM);
            Equal(input.OpeningHeightM, plan.CutterHeightM);
            Equal(input.SillHeightM, plan.BaseElevationM);
            Equal(input.SillHeightM + input.OpeningHeightM, plan.TopElevationM);
        }

        private static OpeningCutInput OrdinaryInput() => new OpeningCutInput
        {
            HostLengthM = 10d,
            HostThicknessM = 0.2d,
            HostHeightM = 4d,
            OpeningWidthM = 1d,
            OpeningHeightM = 2d,
            SillHeightM = 0.5d,
            CenterAlongHostM = 5d,
            ClearanceM = 0.01d
        };

        private static void Near(double expected, double actual, double tolerance = 1e-12)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(double expected, double actual)
        {
            if (expected != actual)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
