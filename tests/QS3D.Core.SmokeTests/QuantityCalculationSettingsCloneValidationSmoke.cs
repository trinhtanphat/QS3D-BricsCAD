using System;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationSettingsCloneValidationSmoke
    {
        private const string NullCategoryRuleMessage = "CategoryRules cannot contain null entries.";
        private const string NullIntersectionRuleMessage = "IntersectionRules cannot contain null entries.";

        public static void Run()
        {
            ValidRulesAreDeepCloned();
            NullCollectionsRetainEmptyCloneBehavior();
            NullCategoryEntriesFailExplicitly();
            NullIntersectionEntriesFailExplicitly();
        }

        private static void ValidRulesAreDeepCloned()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            var originalCategory = settings.CategoryRules[0].ExtractSide;
            var originalIntersection = settings.IntersectionRules[0].SubtractConcrete;

            var clone = settings.Clone();

            False(ReferenceEquals(settings.CategoryRules, clone.CategoryRules));
            False(ReferenceEquals(settings.IntersectionRules, clone.IntersectionRules));
            False(ReferenceEquals(settings.CategoryRules[0], clone.CategoryRules[0]));
            False(ReferenceEquals(settings.IntersectionRules[0], clone.IntersectionRules[0]));

            clone.CategoryRules[0].ExtractSide = !originalCategory;
            clone.IntersectionRules[0].SubtractConcrete = !originalIntersection;
            Equal(originalCategory, settings.CategoryRules[0].ExtractSide);
            Equal(originalIntersection, settings.IntersectionRules[0].SubtractConcrete);
        }

        private static void NullCollectionsRetainEmptyCloneBehavior()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.CategoryRules = null!;
            settings.IntersectionRules = null!;

            var clone = settings.Clone();

            Equal(0, clone.CategoryRules.Count);
            Equal(0, clone.IntersectionRules.Count);
        }

        private static void NullCategoryEntriesFailExplicitly()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.CategoryRules.Add(null!);

            ThrowsInvalid(() => settings.Clone(), NullCategoryRuleMessage);
            ThrowsInvalid(() => new QuantityCalculationRuleSet(settings), NullCategoryRuleMessage);
        }

        private static void NullIntersectionEntriesFailExplicitly()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.IntersectionRules.Add(null!);

            ThrowsInvalid(() => settings.Clone(), NullIntersectionRuleMessage);
            ThrowsInvalid(() => new QuantityCalculationRuleSet(settings), NullIntersectionRuleMessage);
        }

        private static void ThrowsInvalid(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                Equal(expectedMessage, ex.Message);
                return;
            }

            throw new Exception("Expected InvalidOperationException: " + expectedMessage);
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected false.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
