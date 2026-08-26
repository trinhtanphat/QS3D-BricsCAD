using System;
using System.Globalization;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;

namespace QS3D.BricsCAD.V25
{
    internal static class SingleFootingContract
    {
        public const string SubtypeName = "Móng đơn";
        public const string CategoryCode = "Foundation.SingleFooting";
        public const string TreeTag = CategoryCode;
        public const string CategoryCodeKey = "CategoryCode";
        public const string MarkerKey = "SingleFootingSubtype";
        public const string MarkerValue = "MongDon";

        // Canonical persisted parameter keys. Legacy preview keys are still mirrored so 10221-era
        // families/elements remain readable while every new write converges on this stable schema.
        public const string L1Key = "SINGLE_FOOTING_L1";
        public const string W1Key = "SINGLE_FOOTING_W1";
        public const string L2Key = "SINGLE_FOOTING_L2";
        public const string W2Key = "SINGLE_FOOTING_W2";
        public const string H1Key = "SINGLE_FOOTING_H1";
        public const string H2Key = "SINGLE_FOOTING_H2";
        public const string VolumeKey = "SingleFootingVolumeM3";
        public const string BaseElevationKey = "SingleFootingBaseElevationM";
        public const string GeneratedMode = "SingleFootingLoft";

        private const string LegacyL1Key = "SingleFootingL1M";
        private const string LegacyW1Key = "SingleFootingW1M";
        private const string LegacyL2Key = "SingleFootingL2M";
        private const string LegacyW2Key = "SingleFootingW2M";
        private const string LegacyH1Key = "SingleFootingH1M";
        private const string LegacyH2Key = "SingleFootingH2M";

        public static SingleFootingDimensions Defaults =>
            new SingleFootingDimensions(1.6d, 1.6d, 1d, 1d, 1d, 0d);

        public static bool IsSingleFooting(ProjectFamily? family)
        {
            if (family == null || family.Category != ElementCategory.Foundation) return false;
            if (family.Properties.TryGetValue(CategoryCodeKey, out var categoryCode) &&
                string.Equals((categoryCode ?? string.Empty).Trim(), CategoryCode, StringComparison.OrdinalIgnoreCase)) return true;
            if (family.Properties.TryGetValue(MarkerKey, out var marker) &&
                string.Equals((marker ?? string.Empty).Trim(), MarkerValue, StringComparison.OrdinalIgnoreCase)) return true;

            // Compatibility only for families persisted before the stable CategoryCode existed.
            var name = (family.Name ?? string.Empty).Trim();
            if (!name.StartsWith(SubtypeName, StringComparison.CurrentCultureIgnoreCase)) return false;
            return name.Length == SubtypeName.Length ||
                   name[SubtypeName.Length] == '-' ||
                   name[SubtypeName.Length] == '_' ||
                   char.IsWhiteSpace(name[SubtypeName.Length]);
        }

        public static bool IsSingleFooting(ProjectElement? element)
        {
            if (element == null || element.Category != ElementCategory.Foundation) return false;
            if (element.Properties.TryGetValue(CategoryCodeKey, out var categoryCode) &&
                string.Equals((categoryCode ?? string.Empty).Trim(), CategoryCode, StringComparison.OrdinalIgnoreCase)) return true;
            return element.Properties.TryGetValue(MarkerKey, out var marker) &&
                   string.Equals((marker ?? string.Empty).Trim(), MarkerValue, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDimensionKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            return string.Equals(key, L1Key, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, W1Key, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, L2Key, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, W2Key, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, H1Key, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, H2Key, StringComparison.OrdinalIgnoreCase);
        }

        public static SingleFootingDimensions Read(ProjectFamily family)
        {
            if (family == null) throw new ArgumentNullException(nameof(family));
            if (!IsSingleFooting(family))
                throw new InvalidOperationException("Active Foundation Family is not a Móng đơn Family.");

            return new SingleFootingDimensions(
                ReadNumber(family, L1Key, LegacyL1Key),
                ReadNumber(family, W1Key, LegacyW1Key),
                ReadNumber(family, L2Key, LegacyL2Key),
                ReadNumber(family, W2Key, LegacyW2Key),
                ReadNumber(family, H1Key, LegacyH1Key),
                ReadNumber(family, H2Key, LegacyH2Key));
        }

        public static SingleFootingDimensions WithDimension(SingleFootingDimensions current, string key, double valueM)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (!IsDimensionKey(key)) throw new ArgumentException("Unknown Móng đơn dimension key: " + key, nameof(key));

            return new SingleFootingDimensions(
                string.Equals(key, L1Key, StringComparison.OrdinalIgnoreCase) ? valueM : current.L1M,
                string.Equals(key, W1Key, StringComparison.OrdinalIgnoreCase) ? valueM : current.W1M,
                string.Equals(key, L2Key, StringComparison.OrdinalIgnoreCase) ? valueM : current.L2M,
                string.Equals(key, W2Key, StringComparison.OrdinalIgnoreCase) ? valueM : current.W2M,
                string.Equals(key, H1Key, StringComparison.OrdinalIgnoreCase) ? valueM : current.H1M,
                string.Equals(key, H2Key, StringComparison.OrdinalIgnoreCase) ? valueM : current.H2M);
        }

        public static void Apply(ProjectFamily family, SingleFootingDimensions dimensions)
        {
            if (family == null) throw new ArgumentNullException(nameof(family));
            if (dimensions == null) throw new ArgumentNullException(nameof(dimensions));
            if (family.Category != ElementCategory.Foundation)
                throw new InvalidOperationException("Móng đơn settings can only be applied to a Foundation Family.");

            family.Properties[CategoryCodeKey] = CategoryCode;
            family.Properties[MarkerKey] = MarkerValue;
            WriteDimensions(family.Properties, dimensions);
            family.Properties[VolumeKey] = Encode(dimensions.VolumeM3);
            // Quantity compatibility only. Dedicated Workspace UI never exposes this derived value
            // as the editable Móng đơn geometry field.
            family.Properties["ThicknessM"] = Encode(dimensions.TotalHeightM);
            if (!family.Properties.ContainsKey("Material")) family.Properties["Material"] = "Bê tông";
        }

        public static void Apply(ProjectElement element, SingleFootingDimensions dimensions)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (dimensions == null) throw new ArgumentNullException(nameof(dimensions));
            if (element.Category != ElementCategory.Foundation)
                throw new InvalidOperationException("Móng đơn settings can only be applied to a Foundation element.");

            element.Properties[CategoryCodeKey] = CategoryCode;
            element.Properties[MarkerKey] = MarkerValue;
            WriteDimensions(element.Properties, dimensions);
            element.Properties[VolumeKey] = Encode(dimensions.VolumeM3);
            element.Properties["VolumeM3"] = Encode(dimensions.VolumeM3);
            element.Properties["ThicknessM"] = Encode(dimensions.TotalHeightM);
        }

        private static void WriteDimensions(System.Collections.Generic.IDictionary<string, string> properties, SingleFootingDimensions dimensions)
        {
            properties[L1Key] = Encode(dimensions.L1M);
            properties[W1Key] = Encode(dimensions.W1M);
            properties[L2Key] = Encode(dimensions.L2M);
            properties[W2Key] = Encode(dimensions.W2M);
            properties[H1Key] = Encode(dimensions.H1M);
            properties[H2Key] = Encode(dimensions.H2M);

            properties[LegacyL1Key] = Encode(dimensions.L1M);
            properties[LegacyW1Key] = Encode(dimensions.W1M);
            properties[LegacyL2Key] = Encode(dimensions.L2M);
            properties[LegacyW2Key] = Encode(dimensions.W2M);
            properties[LegacyH1Key] = Encode(dimensions.H1M);
            properties[LegacyH2Key] = Encode(dimensions.H2M);
        }

        private static double ReadNumber(ProjectFamily family, string key, string legacyKey)
        {
            if (TryReadNumber(family, key, out var value) || TryReadNumber(family, legacyKey, out value)) return value;
            throw new InvalidOperationException("Móng đơn Family thiếu hoặc sai tham số " + key + ". Bấm Add và nhập lại kích thước.");
        }

        private static bool TryReadNumber(ProjectFamily family, string key, out double value)
        {
            value = 0d;
            return family.Properties.TryGetValue(key, out var raw) &&
                   !string.IsNullOrWhiteSpace(raw) &&
                   double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   !double.IsNaN(value) &&
                   !double.IsInfinity(value);
        }

        private static string Encode(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
