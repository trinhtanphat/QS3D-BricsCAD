using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationRuleSetDefinedCategorySmoke
    {
        public static void Run()
        {
            UndefinedEnumCannotCollideWithLegacyCategoryCode();
            UndefinedEnumCannotCollideWithLegacyIntersectionCode();
        }

        private static void UndefinedEnumCannotCollideWithLegacyCategoryCode()
        {
            var rules = CreateLegacyRoomRuleSet();

            if (!rules.TryGetCategoryRule(201, out var legacy) || legacy.Category != 201)
                throw new InvalidOperationException("Legacy integer category code 201 no longer resolves.");
            if (!rules.TryGetCategoryRule(ElementCategory.Room, out var roomFallback) || roomFallback.Category != 201)
                throw new InvalidOperationException("Valid native Room lookup no longer falls back to legacy code 201.");

            Throws<ArgumentOutOfRangeException>(() =>
            {
                QuantityCategoryRuleSetting ignored;
                rules.TryGetCategoryRule((ElementCategory)201, out ignored);
            });
        }

        private static void UndefinedEnumCannotCollideWithLegacyIntersectionCode()
        {
            var rules = CreateLegacyRoomRuleSet();
            var beamCode = (int)ElementCategory.Beam;

            if (!rules.TryGetIntersectionRule(201, beamCode, out var legacy) || legacy.Source != 201 || legacy.Target != beamCode)
                throw new InvalidOperationException("Legacy integer intersection code no longer resolves.");

            Throws<ArgumentOutOfRangeException>(() =>
            {
                QuantityIntersectionRuleSetting ignored;
                rules.TryGetIntersectionRule((ElementCategory)201, ElementCategory.Beam, out ignored);
            });
        }

        private static QuantityCalculationRuleSet CreateLegacyRoomRuleSet()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.CategoryRules.RemoveAll(x => x.Category == (int)ElementCategory.Room);
            settings.CategoryRules.Add(new QuantityCategoryRuleSetting
            {
                Category = 201,
                ExtractSide = true,
                FaceAngleThresholdDeg = 30d
            });
            settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting
            {
                Source = 201,
                Target = (int)ElementCategory.Beam,
                SubtractConcrete = true
            });
            return new QuantityCalculationRuleSet(settings);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class QuantityCalculationRuleSetDefinedCategorySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            QuantityCalculationRuleSetDefinedCategorySmoke.Run();
        }
    }
}
