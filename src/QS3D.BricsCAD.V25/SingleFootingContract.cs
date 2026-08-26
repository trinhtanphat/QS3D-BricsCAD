using System;
using System.Globalization;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;

namespace QS3D.BricsCAD.V25
{
    internal static class SingleFootingContract
    {
        public const string SubtypeName = "Móng đơn";
        public const string MarkerKey = "SingleFootingSubtype";
        public const string MarkerValue = "MongDon";
        public const string L1Key = "SingleFootingL1M";
        public const string W1Key = "SingleFootingW1M";
        public const string L2Key = "SingleFootingL2M";
        public const string W2Key = "SingleFootingW2M";
        public const string H1Key = "SingleFootingH1M";
        public const string H2Key = "SingleFootingH2M";
        public const string VolumeKey = "SingleFootingVolumeM3";
        public const string GeneratedMode = "SingleFootingLoft";

        public static SingleFootingDimensions Defaults =>
            new SingleFootingDimensions(1.6d, 1.6d, 1d, 1d, 1d, 0d);

        public static bool IsSingleFooting(ProjectFamily? family)
        {
            if (family == null || family.Category != ElementCategory.Foundation) return false;
            if (family.Properties.TryGetValue(MarkerKey, out var marker) &&
                string.Equals((marker ?? string.Empty).Trim(), MarkerValue, StringComparison.OrdinalIgnoreCase)) return true;

            var name = (family.Name ?? string.Empty).Trim();
            if (!name.StartsWith(SubtypeName, StringComparison.CurrentCultureIgnoreCase)) return false;
            return name.Length == SubtypeName.Length ||
                   name[SubtypeName.Length] == '-' ||
                   name[SubtypeName.Length] == '_' ||
                   char.IsWhiteSpace(name[SubtypeName.Length]);
        }

        public static SingleFootingDimensions Read(ProjectFamily family)
        {
            if (family == null) throw new ArgumentNullException(nameof(family));
            if (!IsSingleFooting(family))
                throw new InvalidOperationException("Active Foundation Family is not a Móng đơn Family.");

            return new SingleFootingDimensions(
                ReadNumber(family, L1Key),
                ReadNumber(family, W1Key),
                ReadNumber(family, L2Key),
                ReadNumber(family, W2Key),
                ReadNumber(family, H1Key),
                ReadNumber(family, H2Key));
        }

        public static void Apply(ProjectFamily family, SingleFootingDimensions dimensions)
        {
            if (family == null) throw new ArgumentNullException(nameof(family));
            if (dimensions == null) throw new ArgumentNullException(nameof(dimensions));
            if (family.Category != ElementCategory.Foundation)
                throw new InvalidOperationException("Móng đơn settings can only be applied to a Foundation Family.");

            family.Properties[MarkerKey] = MarkerValue;
            family.Properties[L1Key] = Encode(dimensions.L1M);
            family.Properties[W1Key] = Encode(dimensions.W1M);
            family.Properties[L2Key] = Encode(dimensions.L2M);
            family.Properties[W2Key] = Encode(dimensions.W2M);
            family.Properties[H1Key] = Encode(dimensions.H1M);
            family.Properties[H2Key] = Encode(dimensions.H2M);
            family.Properties[VolumeKey] = Encode(dimensions.VolumeM3);
            family.Properties["ThicknessM"] = Encode(dimensions.TotalHeightM);
            if (!family.Properties.ContainsKey("Material")) family.Properties["Material"] = "Bê tông";
        }

        public static void Apply(ProjectElement element, SingleFootingDimensions dimensions)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (dimensions == null) throw new ArgumentNullException(nameof(dimensions));
            if (element.Category != ElementCategory.Foundation)
                throw new InvalidOperationException("Móng đơn settings can only be applied to a Foundation element.");

            element.Properties[MarkerKey] = MarkerValue;
            element.Properties[L1Key] = Encode(dimensions.L1M);
            element.Properties[W1Key] = Encode(dimensions.W1M);
            element.Properties[L2Key] = Encode(dimensions.L2M);
            element.Properties[W2Key] = Encode(dimensions.W2M);
            element.Properties[H1Key] = Encode(dimensions.H1M);
            element.Properties[H2Key] = Encode(dimensions.H2M);
            element.Properties[VolumeKey] = Encode(dimensions.VolumeM3);
            element.Properties["VolumeM3"] = Encode(dimensions.VolumeM3);
            element.Properties["ThicknessM"] = Encode(dimensions.TotalHeightM);
        }

        private static double ReadNumber(ProjectFamily family, string key)
        {
            if (!family.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw) ||
                !double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Móng đơn Family thiếu hoặc sai tham số " + key + ". Bấm Add và nhập lại kích thước.");
            return value;
        }

        private static string Encode(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}