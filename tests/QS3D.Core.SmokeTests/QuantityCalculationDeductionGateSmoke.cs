using System;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationDeductionGateSmoke
    {
        public static void Run()
        {
            ExactThresholdsAndAllFlags();
            BelowThresholdAndDisabledFlags();
            DirectedAndMissingPairsStayDistinct();
            NativeCompatibilityUsesEstablishedRuleSetMapping();
            SnapshotIsDefensive();
            RejectsMalformedCandidates();
        }

        private static void ExactThresholdsAndAllFlags()
        {
            var settings = Settings();
            settings.IntersectionRules.Add(AllEnabled(901, 403));
            var gate = new QuantityCalculationDeductionGate(new QuantityCalculationRuleSet(settings));

            True(gate.AllowsFormworkArea(1000d));
            FoundAllowed(gate.TryAllowConcreteDeduction(901, 403, 0.0001d, out var concrete), concrete);
            FoundAllowed(gate.TryAllowSideFormworkByConcreteDeduction(901, 403, 10d, out var sideConcrete), sideConcrete);
            FoundAllowed(gate.TryAllowBottomFormworkByConcreteDeduction(901, 403, 10d, out var bottomConcrete), bottomConcrete);
            FoundAllowed(gate.TryAllowSideFormworkBySideFormworkDeduction(901, 403, 10d, out var sideSide), sideSide);
            FoundAllowed(gate.TryAllowBottomFormworkByBottomFormworkDeduction(901, 403, 10d, out var bottomBottom), bottomBottom);
        }

        private static void BelowThresholdAndDisabledFlags()
        {
            var settings = Settings();
            settings.IntersectionRules.Add(AllEnabled(901, 403));
            settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting { Source = 403, Target = 901 });
            var gate = new QuantityCalculationDeductionGate(new QuantityCalculationRuleSet(settings));

            False(gate.AllowsFormworkArea(999.999d));
            FoundDenied(gate.TryAllowConcreteDeduction(901, 403, 0.000099d, out var concreteSmall), concreteSmall);
            FoundDenied(gate.TryAllowSideFormworkByConcreteDeduction(901, 403, 9.999d, out var areaSmall), areaSmall);
            FoundDenied(gate.TryAllowConcreteDeduction(403, 901, 1d, out var concreteDisabled), concreteDisabled);
            FoundDenied(gate.TryAllowBottomFormworkByBottomFormworkDeduction(403, 901, 10000d, out var areaDisabled), areaDisabled);
        }

        private static void DirectedAndMissingPairsStayDistinct()
        {
            var settings = Settings();
            settings.IntersectionRules.Add(AllEnabled(901, 403));
            var gate = new QuantityCalculationDeductionGate(new QuantityCalculationRuleSet(settings));

            FoundAllowed(gate.TryAllowConcreteDeduction(901, 403, 1d, out var forward), forward);
            False(gate.TryAllowConcreteDeduction(403, 901, 1d, out var reverse));
            False(reverse);
            False(gate.TryAllowSideFormworkByConcreteDeduction(1301, 1302, 100d, out var missing));
            False(missing);
        }

        private static void NativeCompatibilityUsesEstablishedRuleSetMapping()
        {
            var settings = Settings();
            settings.IntersectionRules.Add(AllEnabled(201, 601));
            var gate = new QuantityCalculationDeductionGate(new QuantityCalculationRuleSet(settings));

            FoundAllowed(
                gate.TryAllowConcreteDeduction(ElementCategory.Room, ElementCategory.Column, 1d, out var allowed),
                allowed);
            False(gate.TryAllowConcreteDeduction(ElementCategory.Column, ElementCategory.Room, 1d, out var reverse));
            False(reverse);
        }

        private static void SnapshotIsDefensive()
        {
            var settings = Settings();
            settings.IntersectionRules.Add(AllEnabled(901, 403));
            var rules = new QuantityCalculationRuleSet(settings);
            var gate = new QuantityCalculationDeductionGate(rules);

            settings.MinConcreteVolumeM3 = 100d;
            settings.MinSubtractAreaMm2 = 100000d;
            settings.IntersectionRules[0].SubtractConcrete = false;

            FoundAllowed(gate.TryAllowConcreteDeduction(901, 403, 0.0001d, out var concrete), concrete);
            FoundAllowed(gate.TryAllowSideFormworkByConcreteDeduction(901, 403, 10d, out var area), area);
        }

        private static void RejectsMalformedCandidates()
        {
            var settings = Settings();
            settings.IntersectionRules.Add(AllEnabled(901, 403));
            var gate = new QuantityCalculationDeductionGate(new QuantityCalculationRuleSet(settings));

            Throws<ArgumentOutOfRangeException>(() => gate.AllowsFormworkArea(double.NaN));
            Throws<ArgumentOutOfRangeException>(() => gate.AllowsFormworkArea(-1d));
            Throws<ArgumentOutOfRangeException>(() => gate.TryAllowConcreteDeduction(901, 403, double.PositiveInfinity, out _));
            Throws<ArgumentOutOfRangeException>(() => gate.TryAllowSideFormworkByConcreteDeduction(901, 403, -0.001d, out _));
        }

        private static QuantityCalculationSettings Settings()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.MinSubtractAreaMm2 = 10d;
            settings.MinFormworkAreaMm2 = 1000d;
            settings.MinConcreteVolumeM3 = 0.0001d;
            settings.IntersectionRules.Clear();
            return settings;
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

        private static void FoundAllowed(bool found, bool allowed) { True(found); True(allowed); }
        private static void FoundDenied(bool found, bool allowed) { True(found); False(allowed); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void False(bool value) { if (value) throw new Exception("Expected false."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
