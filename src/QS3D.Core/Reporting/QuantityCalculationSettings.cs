using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    [DataContract]
    public sealed class QuantityCalculationSettings
    {
        public const int CurrentSchemaVersion = 2;
        private const string NullCategoryRuleMessage = "CategoryRules cannot contain null entries.";
        private const string NullIntersectionRuleMessage = "IntersectionRules cannot contain null entries.";

        [DataMember(Order = 1)] public int SchemaVersion { get; set; }
        [DataMember(Order = 2)] public double FormworkTolerance { get; set; }
        [DataMember(Order = 3)] public double BlindingConcreteOffset { get; set; }
        [DataMember(Order = 4)] public double MinSubtractAreaMm2 { get; set; }
        [DataMember(Order = 5)] public double MinFormworkAreaMm2 { get; set; }
        [DataMember(Order = 6)] public double MinConcreteVolumeM3 { get; set; }
        [DataMember(Order = 7)] public double EngulfRelPercent { get; set; }
        [DataMember(Order = 8)] public double EngulfMinAreaMm2 { get; set; }
        [DataMember(Order = 9)] public double RoomGapFillMm { get; set; }
        [DataMember(Order = 10)] public double RoomSearchRadiusMm { get; set; }
        [DataMember(Order = 11)] public string DimColor { get; set; } = "#FFFFFF";
        [DataMember(Order = 12)] public double DimTextHeight { get; set; }
        [DataMember(Order = 13)] public List<QuantityCategoryRuleSetting> CategoryRules { get; set; } = new List<QuantityCategoryRuleSetting>();
        [DataMember(Order = 14)] public List<QuantityIntersectionRuleSetting> IntersectionRules { get; set; } = new List<QuantityIntersectionRuleSetting>();

        public static QuantityCalculationSettings CreateDefault()
        {
            var result = new QuantityCalculationSettings
            {
                SchemaVersion = CurrentSchemaVersion,
                FormworkTolerance = 10d,
                BlindingConcreteOffset = 100d,
                MinSubtractAreaMm2 = 10d,
                MinFormworkAreaMm2 = 1000d,
                MinConcreteVolumeM3 = 0.0001d,
                EngulfRelPercent = 1d,
                EngulfMinAreaMm2 = 1000d,
                RoomGapFillMm = 50d,
                RoomSearchRadiusMm = 40000d,
                DimColor = "#FFFFFF",
                DimTextHeight = 30d
            };

            foreach (ElementCategory category in Enum.GetValues(typeof(ElementCategory)))
            {
                if (category == ElementCategory.Grid) continue;
                var extractSide = category == ElementCategory.Beam ||
                                  category == ElementCategory.Slab ||
                                  category == ElementCategory.Column ||
                                  category == ElementCategory.StructuralWall ||
                                  category == ElementCategory.WallPier ||
                                  category == ElementCategory.Stair ||
                                  category == ElementCategory.Foundation;
                var extractBottom = category == ElementCategory.Beam ||
                                    category == ElementCategory.Slab ||
                                    category == ElementCategory.Stair ||
                                    category == ElementCategory.Foundation;
                result.CategoryRules.Add(new QuantityCategoryRuleSetting
                {
                    Category = (int)category,
                    ExtractSide = extractSide,
                    ExtractBottom = extractBottom,
                    FaceAngleThresholdDeg = 30d
                });
            }

            var categoryCodes = result.CategoryRules.Select(x => x.Category).ToArray();
            foreach (var source in categoryCodes)
            foreach (var target in categoryCodes)
                result.IntersectionRules.Add(new QuantityIntersectionRuleSetting { Source = source, Target = target });

            return result;
        }

        public QuantityCalculationSettings Clone()
        {
            return new QuantityCalculationSettings
            {
                SchemaVersion = SchemaVersion,
                FormworkTolerance = FormworkTolerance,
                BlindingConcreteOffset = BlindingConcreteOffset,
                MinSubtractAreaMm2 = MinSubtractAreaMm2,
                MinFormworkAreaMm2 = MinFormworkAreaMm2,
                MinConcreteVolumeM3 = MinConcreteVolumeM3,
                EngulfRelPercent = EngulfRelPercent,
                EngulfMinAreaMm2 = EngulfMinAreaMm2,
                RoomGapFillMm = RoomGapFillMm,
                RoomSearchRadiusMm = RoomSearchRadiusMm,
                DimColor = DimColor,
                DimTextHeight = DimTextHeight,
                CategoryRules = (CategoryRules ?? new List<QuantityCategoryRuleSetting>()).Select(CloneCategoryRule).ToList(),
                IntersectionRules = (IntersectionRules ?? new List<QuantityIntersectionRuleSetting>()).Select(CloneIntersectionRule).ToList()
            };
        }

        public void NormalizeAndValidate()
        {
            if (SchemaVersion <= 0) SchemaVersion = CurrentSchemaVersion;
            if (SchemaVersion > CurrentSchemaVersion)
                throw new InvalidOperationException("Quantity settings schema " + SchemaVersion + " is newer than supported schema " + CurrentSchemaVersion + ".");

            CategoryRules = CategoryRules ?? new List<QuantityCategoryRuleSetting>();
            IntersectionRules = IntersectionRules ?? new List<QuantityIntersectionRuleSetting>();
            DimColor = string.IsNullOrWhiteSpace(DimColor) ? "#FFFFFF" : DimColor.Trim().ToUpperInvariant();

            RequireFiniteNonNegative(FormworkTolerance, nameof(FormworkTolerance));
            RequireFiniteNonNegative(BlindingConcreteOffset, nameof(BlindingConcreteOffset));
            RequireFiniteNonNegative(MinSubtractAreaMm2, nameof(MinSubtractAreaMm2));
            RequireFiniteNonNegative(MinFormworkAreaMm2, nameof(MinFormworkAreaMm2));
            RequireFiniteNonNegative(MinConcreteVolumeM3, nameof(MinConcreteVolumeM3));
            RequireFiniteNonNegative(EngulfRelPercent, nameof(EngulfRelPercent));
            RequireFiniteNonNegative(EngulfMinAreaMm2, nameof(EngulfMinAreaMm2));
            RequireFiniteNonNegative(RoomGapFillMm, nameof(RoomGapFillMm));
            RequireFiniteNonNegative(RoomSearchRadiusMm, nameof(RoomSearchRadiusMm));
            RequireFiniteNonNegative(DimTextHeight, nameof(DimTextHeight));
            if (DimTextHeight <= 0d) throw new InvalidOperationException("DimTextHeight must be greater than zero.");
            if (!IsHexColor(DimColor)) throw new InvalidOperationException("DimColor must use #RRGGBB format.");

            var categoryCodes = new HashSet<int>();
            foreach (var rule in CategoryRules)
            {
                if (rule == null) throw new InvalidOperationException(NullCategoryRuleMessage);
                if (rule.Category < 0) throw new InvalidOperationException("Category code cannot be negative.");
                if (!categoryCodes.Add(rule.Category)) throw new InvalidOperationException("Duplicate category rule for code " + rule.Category + ".");
                RequireFiniteNonNegative(rule.FaceAngleThresholdDeg, nameof(rule.FaceAngleThresholdDeg));
                if (rule.FaceAngleThresholdDeg > 90d) throw new InvalidOperationException("FaceAngleThresholdDeg must be between 0 and 90 degrees.");
            }

            var pairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in IntersectionRules)
            {
                if (rule == null) throw new InvalidOperationException(NullIntersectionRuleMessage);
                if (rule.Source < 0 || rule.Target < 0) throw new InvalidOperationException("Intersection category codes cannot be negative.");
                var key = rule.Source + ":" + rule.Target;
                if (!pairs.Add(key)) throw new InvalidOperationException("Duplicate intersection rule for " + key + ".");
            }
        }

        public QuantityCategoryRuleSetting? FindCategoryRule(int categoryCode)
        {
            return (CategoryRules ?? new List<QuantityCategoryRuleSetting>()).FirstOrDefault(x => x != null && x.Category == categoryCode);
        }

        public QuantityIntersectionRuleSetting? FindIntersectionRule(int sourceCode, int targetCode)
        {
            return (IntersectionRules ?? new List<QuantityIntersectionRuleSetting>()).FirstOrDefault(x => x != null && x.Source == sourceCode && x.Target == targetCode);
        }

        private static QuantityCategoryRuleSetting CloneCategoryRule(QuantityCategoryRuleSetting? rule)
        {
            if (rule == null) throw new InvalidOperationException(NullCategoryRuleMessage);
            return rule.Clone();
        }

        private static QuantityIntersectionRuleSetting CloneIntersectionRule(QuantityIntersectionRuleSetting? rule)
        {
            if (rule == null) throw new InvalidOperationException(NullIntersectionRuleMessage);
            return rule.Clone();
        }

        private static void RequireFiniteNonNegative(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(name + " must be a finite non-negative number.");
        }

        private static bool IsHexColor(string value)
        {
            if (value == null || value.Length != 7 || value[0] != '#') return false;
            for (var i = 1; i < value.Length; i++)
            {
                var c = value[i];
                var digit = c >= '0' && c <= '9';
                var hex = c >= 'A' && c <= 'F';
                if (!digit && !hex) return false;
            }
            return true;
        }
    }

    [DataContract]
    public sealed class QuantityCategoryRuleSetting
    {
        [DataMember(Order = 1)] public int Category { get; set; }
        [DataMember(Order = 2)] public bool ExtractSide { get; set; }
        [DataMember(Order = 3)] public bool ExtractBottom { get; set; }
        [DataMember(Order = 4)] public double FaceAngleThresholdDeg { get; set; }

        public QuantityCategoryRuleSetting Clone()
        {
            return new QuantityCategoryRuleSetting
            {
                Category = Category,
                ExtractSide = ExtractSide,
                ExtractBottom = ExtractBottom,
                FaceAngleThresholdDeg = FaceAngleThresholdDeg
            };
        }
    }

    [DataContract]
    public sealed class QuantityIntersectionRuleSetting
    {
        [DataMember(Order = 1)] public int Source { get; set; }
        [DataMember(Order = 2)] public int Target { get; set; }
        [DataMember(Order = 3)] public bool SubtractConcrete { get; set; }
        [DataMember(Order = 4)] public bool SubtractSideFormworkByConcrete { get; set; }
        [DataMember(Order = 5)] public bool SubtractBottomFormworkByConcrete { get; set; }
        [DataMember(Order = 6)] public bool SubtractSideFormworkBySideFormwork { get; set; }
        [DataMember(Order = 7)] public bool SubtractBottomFormworkByBottomFormwork { get; set; }

        public QuantityIntersectionRuleSetting Clone()
        {
            return new QuantityIntersectionRuleSetting
            {
                Source = Source,
                Target = Target,
                SubtractConcrete = SubtractConcrete,
                SubtractSideFormworkByConcrete = SubtractSideFormworkByConcrete,
                SubtractBottomFormworkByConcrete = SubtractBottomFormworkByConcrete,
                SubtractSideFormworkBySideFormwork = SubtractSideFormworkBySideFormwork,
                SubtractBottomFormworkByBottomFormwork = SubtractBottomFormworkByBottomFormwork
            };
        }
    }
}
