using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    /// <summary>
    /// Immutable runtime lookup snapshot for quantity calculation settings.
    ///
    /// Integer-code lookup is always exact. Native ElementCategory lookup first
    /// tries the native enum value, then falls back only to legacy BLT codes whose
    /// Vietnamese category label is an exact match for the existing native QS3D
    /// label in the Setup & Rules UI. Missing rules are never synthesized or mirrored.
    /// </summary>
    public sealed class QuantityCalculationRuleSet
    {
        private readonly QuantityCalculationSettings _settings;
        private readonly Dictionary<int, QuantityCategoryRuleSetting> _categoryRules;
        private readonly Dictionary<long, QuantityIntersectionRuleSetting> _intersectionRules;

        public QuantityCalculationRuleSet(QuantityCalculationSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            _settings = settings.Clone();
            _settings.NormalizeAndValidate();

            _categoryRules = new Dictionary<int, QuantityCategoryRuleSetting>();
            foreach (var rule in _settings.CategoryRules)
                _categoryRules.Add(rule.Category, rule.Clone());

            _intersectionRules = new Dictionary<long, QuantityIntersectionRuleSetting>();
            foreach (var rule in _settings.IntersectionRules)
                _intersectionRules.Add(PairKey(rule.Source, rule.Target), rule.Clone());
        }

        public QuantityCalculationSettings Snapshot => _settings.Clone();

        public bool TryGetCategoryRule(int categoryCode, out QuantityCategoryRuleSetting rule)
        {
            if (_categoryRules.TryGetValue(categoryCode, out var stored))
            {
                rule = stored.Clone();
                return true;
            }

            rule = null!;
            return false;
        }

        public bool TryGetCategoryRule(ElementCategory category, out QuantityCategoryRuleSetting rule)
        {
            foreach (var code in LookupCodes(category))
                if (TryGetCategoryRule(code, out rule))
                    return true;

            rule = null!;
            return false;
        }

        public bool TryGetIntersectionRule(int sourceCode, int targetCode, out QuantityIntersectionRuleSetting rule)
        {
            if (_intersectionRules.TryGetValue(PairKey(sourceCode, targetCode), out var stored))
            {
                rule = stored.Clone();
                return true;
            }

            rule = null!;
            return false;
        }

        public bool TryGetIntersectionRule(ElementCategory source, ElementCategory target, out QuantityIntersectionRuleSetting rule)
        {
            var sourceCodes = LookupCodes(source);
            var targetCodes = LookupCodes(target);

            // The nested order deliberately prefers native->native, then mixed
            // native/compatibility candidates, and finally compatibility->compatibility.
            foreach (var sourceCode in sourceCodes)
            foreach (var targetCode in targetCodes)
                if (TryGetIntersectionRule(sourceCode, targetCode, out rule))
                    return true;

            rule = null!;
            return false;
        }

        private static int[] LookupCodes(ElementCategory category)
        {
            if (!Enum.IsDefined(typeof(ElementCategory), category))
                throw new ArgumentOutOfRangeException(nameof(category), "Quantity rule lookup category must be defined.");

            var native = (int)category;
            switch (category)
            {
                // These are the only legacy fallbacks whose Vietnamese labels are
                // exact matches in QuantityCategoryDisplayName.Native/Compatibility.
                case ElementCategory.Room: return new[] { native, 201 };
                case ElementCategory.FloorFinish: return new[] { native, 202 };
                case ElementCategory.Skirting: return new[] { native, 204 };
                case ElementCategory.WallFinish: return new[] { native, 205 };
                case ElementCategory.Railing: return new[] { native, 207 };
                case ElementCategory.Column: return new[] { native, 601 };
                case ElementCategory.StructuralWall: return new[] { native, 701 };
                default: return new[] { native };
            }
        }

        private static long PairKey(int sourceCode, int targetCode)
        {
            return ((long)(uint)sourceCode << 32) | (uint)targetCode;
        }
    }
}
