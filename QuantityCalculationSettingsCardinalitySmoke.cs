using System;
using System.Collections.Generic;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationSettingsCardinalitySmoke
    {
        public static void Run()
        {
            DefaultSettingsRemainValid();
            ImportedTwentyEightCodeMatrixRemainsValid();
            ExactUnknownIntegerCodesRemainValid();
            ExactCategoryUniverseBoundaryRemainsValid();
            CategoryRuleOverflowFailsClosed();
            DirectedRuleCountOverflowFailsClosed();
            SparseDistinctObservedCodeOverflowFailsClosed();
        }

        private static void DefaultSettingsRemainValid()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.NormalizeAndValidate();
            var clone = settings.Clone();
            True(settings.CategoryRules.Count < QuantityCalculationSettings.MaxObservedCategoryCodeCount);
            True(settings.IntersectionRules.Count < QuantityCalculationSettings.MaxDirectedIntersectionRuleCount);
            Equal(settings.CategoryRules.Count, clone.CategoryRules.Count);
            Equal(settings.IntersectionRules.Count, clone.IntersectionRules.Count);
        }

        private static void ImportedTwentyEightCodeMatrixRemainsValid()
        {
            var settings = EmptySettings();
            for (var i = 0; i < 28; i++)
                settings.CategoryRules.Add(CategoryRule(1000 + i));

            for (var source = 0; source < 28; source++)
            for (var target = 0; target < 28; target++)
                settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting
                {
                    Source = 1000 + source,
                    Target = 1000 + target
                });

            settings.NormalizeAndValidate();
            var clone = settings.Clone();
            Equal(28, settings.CategoryRules.Count);
            Equal(28 * 28, settings.IntersectionRules.Count);
            Equal(28 * 28, clone.IntersectionRules.Count);
            True(settings.FindIntersectionRule(1000, 1027) != null);
        }

        private static void ExactUnknownIntegerCodesRemainValid()
        {
            var settings = EmptySettings();
            settings.CategoryRules.Add(CategoryRule(1301));
            settings.CategoryRules.Add(CategoryRule(int.MaxValue));
            settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting { Source = 1301, Target = int.MaxValue });
            settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting { Source = int.MaxValue, Target = 1301 });

            settings.NormalizeAndValidate();
            var clone = settings.Clone();

            True(settings.FindCategoryRule(1301) != null);
            True(settings.FindCategoryRule(int.MaxValue) != null);
            True(settings.FindIntersectionRule(1301, int.MaxValue) != null);
            True(settings.FindIntersectionRule(int.MaxValue, 1301) != null);
            True(clone.FindIntersectionRule(1301, int.MaxValue) != null);
            True(clone.FindIntersectionRule(int.MaxValue, 1301) != null);
        }

        private static void ExactCategoryUniverseBoundaryRemainsValid()
        {
            var settings = EmptySettings();
            for (var i = 0; i < QuantityCalculationSettings.MaxObservedCategoryCodeCount; i++)
                settings.CategoryRules.Add(CategoryRule(10000 + i));

            settings.NormalizeAndValidate();
            var clone = settings.Clone();
            Equal(QuantityCalculationSettings.MaxObservedCategoryCodeCount, settings.CategoryRules.Count);
            Equal(QuantityCalculationSettings.MaxObservedCategoryCodeCount, clone.CategoryRules.Count);
        }

        private static void CategoryRuleOverflowFailsClosed()
        {
            var settings = EmptySettings();
            for (var i = 0; i <= QuantityCalculationSettings.MaxObservedCategoryCodeCount; i++)
                settings.CategoryRules.Add(CategoryRule(20000 + i));

            Throws<InvalidOperationException>(() => settings.Clone());
            Throws<InvalidOperationException>(() => settings.NormalizeAndValidate());
        }

        private static void DirectedRuleCountOverflowFailsClosed()
        {
            var settings = EmptySettings();
            var repeated = new QuantityIntersectionRuleSetting { Source = 1, Target = 1 };
            settings.IntersectionRules = new List<QuantityIntersectionRuleSetting>(
                QuantityCalculationSettings.MaxDirectedIntersectionRuleCount + 1);
            for (var i = 0; i <= QuantityCalculationSettings.MaxDirectedIntersectionRuleCount; i++)
                settings.IntersectionRules.Add(repeated);

            Throws<InvalidOperationException>(() => settings.Clone());
            Throws<InvalidOperationException>(() => settings.NormalizeAndValidate());
        }

        private static void SparseDistinctObservedCodeOverflowFailsClosed()
        {
            var settings = EmptySettings();
            var pairCount = QuantityCalculationSettings.MaxObservedCategoryCodeCount / 2 + 1;
            for (var i = 0; i < pairCount; i++)
                settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting
                {
                    Source = 30000 + i * 2,
                    Target = 30001 + i * 2
                });

            True(settings.IntersectionRules.Count < QuantityCalculationSettings.MaxDirectedIntersectionRuleCount);
            var clone = settings.Clone();
            Equal(settings.IntersectionRules.Count, clone.IntersectionRules.Count);
            Throws<InvalidOperationException>(() => settings.NormalizeAndValidate());
        }

        private static QuantityCalculationSettings EmptySettings()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.CategoryRules.Clear();
            settings.IntersectionRules.Clear();
            return settings;
        }

        private static QuantityCategoryRuleSetting CategoryRule(int code) =>
            new QuantityCategoryRuleSetting
            {
                Category = code,
                FaceAngleThresholdDeg = 30d
            };

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
