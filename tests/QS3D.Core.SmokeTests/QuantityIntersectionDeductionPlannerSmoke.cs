using System;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityIntersectionDeductionPlannerSmoke
    {
        public static void Run()
        {
            AllEnabledAtExactThresholds();
            MixedFlagsAndBelowThresholdsZeroOnlyRejectedCandidates();
            MissingAndReversePairsStayEmpty();
            UnknownIntegerCodesRoundTripExactly();
            PlanningDoesNotMutateCandidate();
            RejectsMalformedCandidateEvidence();
        }

        private static void AllEnabledAtExactThresholds()
        {
            var planner = Planner(AllEnabled(901, 403));
            var plan = planner.Plan(new QuantityIntersectionCandidateMeasurement(
                901, 403, 0.0001d, 10d, 10d, 10d, 10d));

            True(plan.RuleFound);
            True(plan.HasAnyDeduction);
            Equal(901, plan.SourceCode);
            Equal(403, plan.TargetCode);
            Near(0.0001d, plan.ConcreteVolumeM3);
            Near(10d, plan.SideFormworkByConcreteAreaMm2);
            Near(10d, plan.BottomFormworkByConcreteAreaMm2);
            Near(10d, plan.SideFormworkBySideFormworkAreaMm2);
            Near(10d, plan.BottomFormworkByBottomFormworkAreaMm2);
        }

        private static void MixedFlagsAndBelowThresholdsZeroOnlyRejectedCandidates()
        {
            var rule = new QuantityIntersectionRuleSetting
            {
                Source = 901,
                Target = 403,
                SubtractConcrete = false,
                SubtractSideFormworkByConcrete = true,
                SubtractBottomFormworkByConcrete = false,
                SubtractSideFormworkBySideFormwork = true,
                SubtractBottomFormworkByBottomFormwork = true
            };
            var planner = Planner(rule);
            var plan = planner.Plan(new QuantityIntersectionCandidateMeasurement(
                901, 403, 1d, 9.999d, 100d, 10d, 9.5d));

            True(plan.RuleFound);
            False(plan.HasAnyDeduction == false);
            Near(0d, plan.ConcreteVolumeM3);
            Near(0d, plan.SideFormworkByConcreteAreaMm2);
            Near(0d, plan.BottomFormworkByConcreteAreaMm2);
            Near(10d, plan.SideFormworkBySideFormworkAreaMm2);
            Near(0d, plan.BottomFormworkByBottomFormworkAreaMm2);
        }

        private static void MissingAndReversePairsStayEmpty()
        {
            var planner = Planner(AllEnabled(901, 403));

            var reverse = planner.Plan(new QuantityIntersectionCandidateMeasurement(
                403, 901, 1d, 100d, 100d, 100d, 100d));
            False(reverse.RuleFound);
            False(reverse.HasAnyDeduction);
            AllZero(reverse);

            var missing = planner.Plan(new QuantityIntersectionCandidateMeasurement(
                777, 778, 1d, 100d, 100d, 100d, 100d));
            False(missing.RuleFound);
            False(missing.HasAnyDeduction);
            AllZero(missing);
        }

        private static void UnknownIntegerCodesRoundTripExactly()
        {
            var planner = Planner(AllEnabled(1301, 1302));
            var plan = planner.Plan(new QuantityIntersectionCandidateMeasurement(
                1301, 1302, 0.25d, 1500d, 1200d, 800d, 600d));

            True(plan.RuleFound);
            Equal(1301, plan.SourceCode);
            Equal(1302, plan.TargetCode);
            Near(0.25d, plan.ConcreteVolumeM3);
            Near(1500d, plan.SideFormworkByConcreteAreaMm2);
        }

        private static void PlanningDoesNotMutateCandidate()
        {
            var planner = Planner(AllEnabled(901, 403));
            var candidate = new QuantityIntersectionCandidateMeasurement(
                901, 403, 0.5d, 101d, 102d, 103d, 104d);

            var plan = planner.Plan(candidate);

            Near(0.5d, candidate.ConcreteVolumeM3);
            Near(101d, candidate.SideFormworkByConcreteAreaMm2);
            Near(102d, candidate.BottomFormworkByConcreteAreaMm2);
            Near(103d, candidate.SideFormworkBySideFormworkAreaMm2);
            Near(104d, candidate.BottomFormworkByBottomFormworkAreaMm2);
            Near(0.5d, plan.ConcreteVolumeM3);
        }

        private static void RejectsMalformedCandidateEvidence()
        {
            Throws<ArgumentOutOfRangeException>(() => new QuantityIntersectionCandidateMeasurement(
                -1, 403, 1d, 1d, 1d, 1d, 1d));
            Throws<ArgumentOutOfRangeException>(() => new QuantityIntersectionCandidateMeasurement(
                901, -1, 1d, 1d, 1d, 1d, 1d));
            Throws<ArgumentOutOfRangeException>(() => new QuantityIntersectionCandidateMeasurement(
                901, 403, double.NaN, 1d, 1d, 1d, 1d));
            Throws<ArgumentOutOfRangeException>(() => new QuantityIntersectionCandidateMeasurement(
                901, 403, 1d, double.PositiveInfinity, 1d, 1d, 1d));
            Throws<ArgumentOutOfRangeException>(() => new QuantityIntersectionCandidateMeasurement(
                901, 403, 1d, 1d, -0.001d, 1d, 1d));
            Throws<ArgumentNullException>(() => Planner(AllEnabled(901, 403)).Plan(null!));
        }

        private static QuantityIntersectionDeductionPlanner Planner(QuantityIntersectionRuleSetting rule)
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.MinConcreteVolumeM3 = 0.0001d;
            settings.MinSubtractAreaMm2 = 10d;
            settings.IntersectionRules.Clear();
            settings.IntersectionRules.Add(rule);
            return new QuantityIntersectionDeductionPlanner(
                new QuantityCalculationDeductionGate(
                    new QuantityCalculationRuleSet(settings)));
        }

        private static QuantityIntersectionRuleSetting AllEnabled(int source, int target) =>
            new QuantityIntersectionRuleSetting
            {
                Source = source,
                Target = target,
                SubtractConcrete = true,
                SubtractSideFormworkByConcrete = true,
                SubtractBottomFormworkByConcrete = true,
                SubtractSideFormworkBySideFormwork = true,
                SubtractBottomFormworkByBottomFormwork = true
            };

        private static void AllZero(QuantityIntersectionDeductionPlan plan)
        {
            Near(0d, plan.ConcreteVolumeM3);
            Near(0d, plan.SideFormworkByConcreteAreaMm2);
            Near(0d, plan.BottomFormworkByConcreteAreaMm2);
            Near(0d, plan.SideFormworkBySideFormworkAreaMm2);
            Near(0d, plan.BottomFormworkByBottomFormworkAreaMm2);
        }

        private static void Near(double expected, double actual, double tolerance = 1e-9) { if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void False(bool value) { if (value) throw new Exception("Expected false."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
