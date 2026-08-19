using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QS3D.Core.Domain
{
    public sealed class ProjectFamilyQuickSchema
    {
        internal ProjectFamilyQuickSchema(
            ElementCategory category,
            IEnumerable<string> formKeys,
            IEnumerable<string> identityKeys,
            IDictionary<string, double> defaultsM,
            string defaultMaterial)
        {
            Category = category;
            FormKeys = formKeys.ToArray();
            IdentityKeys = identityKeys.ToArray();
            DefaultsM = new Dictionary<string, double>(defaultsM, StringComparer.OrdinalIgnoreCase);
            DefaultMaterial = defaultMaterial ?? string.Empty;
        }

        public ElementCategory Category { get; }
        public IReadOnlyList<string> FormKeys { get; }
        public IReadOnlyList<string> IdentityKeys { get; }
        public IReadOnlyDictionary<string, double> DefaultsM { get; }
        public string DefaultMaterial { get; }
        public bool SupportsQuickForm => FormKeys.Count > 0;

        public bool Contains(string key) => FormKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
        public bool IsIdentityKey(string key) => IdentityKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
    }

    public static class ProjectFamilyQuickSchemaService
    {
        public const double MillimetersPerMeter = 1000d;
        private const double IdentityToleranceM = 1e-9;

        private static readonly ProjectFamilyQuickSchema Empty = Schema(
            default,
            Array.Empty<string>(),
            Array.Empty<string>(),
            new Dictionary<string, double>(),
            string.Empty);

        private static readonly ProjectFamilyQuickSchema FloorFinish = Schema(
            ElementCategory.FloorFinish,
            new[] { "ThicknessM", "BottomOffsetM" },
            new[] { "ThicknessM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["ThicknessM"] = 0.02d,
                ["BottomOffsetM"] = 0d
            },
            "Hoàn thiện sàn");

        private static readonly ProjectFamilyQuickSchema Waterproofing = Schema(
            ElementCategory.Waterproofing,
            new[] { "ThicknessM", "BottomOffsetM" },
            new[] { "ThicknessM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["ThicknessM"] = 0.003d,
                ["BottomOffsetM"] = 0d
            },
            "Chống thấm");

        private static readonly ProjectFamilyQuickSchema Skirting = Schema(
            ElementCategory.Skirting,
            new[] { "HeightM", "ThicknessM", "BottomOffsetM" },
            new[] { "HeightM", "ThicknessM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["HeightM"] = 0.1d,
                ["ThicknessM"] = 0.015d,
                ["BottomOffsetM"] = 0d
            },
            "Hoàn thiện chân tường");

        private static readonly ProjectFamilyQuickSchema WallFinish = Schema(
            ElementCategory.WallFinish,
            new[] { "ThicknessM", "HeightM", "BottomOffsetM" },
            new[] { "ThicknessM", "HeightM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["ThicknessM"] = 0.015d,
                ["HeightM"] = 3d,
                ["BottomOffsetM"] = 0d
            },
            "Hoàn thiện tường");

        private static readonly ProjectFamilyQuickSchema CeilingFinish = Schema(
            ElementCategory.CeilingFinish,
            new[] { "ThicknessM", "BottomOffsetM" },
            new[] { "ThicknessM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["ThicknessM"] = 0.012d,
                ["BottomOffsetM"] = 2.7d
            },
            "Hoàn thiện trần");

        private static readonly ProjectFamilyQuickSchema Railing = Schema(
            ElementCategory.Railing,
            new[] { "HeightM", "WidthM", "BottomOffsetM" },
            new[] { "HeightM", "WidthM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["HeightM"] = 1.1d,
                ["WidthM"] = 0.05d,
                ["BottomOffsetM"] = 0d
            },
            "Thép");

        private static readonly ProjectFamilyQuickSchema WallOpening = Schema(
            ElementCategory.WallOpening,
            new[] { "WidthM", "HeightM", "BottomOffsetM" },
            new[] { "WidthM", "HeightM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["WidthM"] = 1d,
                ["HeightM"] = 2.1d,
                ["BottomOffsetM"] = 0d
            },
            string.Empty);

        private static readonly ProjectFamilyQuickSchema Beam = Schema(
            ElementCategory.Beam,
            new[] { "WidthM", "HeightM", "BottomOffsetM" },
            new[] { "WidthM", "HeightM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["WidthM"] = 0.3d,
                ["HeightM"] = 0.5d,
                ["BottomOffsetM"] = 0d
            },
            "Bê tông");

        private static readonly ProjectFamilyQuickSchema Column = Schema(
            ElementCategory.Column,
            new[] { "WidthM", "DepthM", "HeightM", "BottomOffsetM" },
            new[] { "WidthM", "DepthM", "HeightM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["WidthM"] = 0.4d,
                ["DepthM"] = 0.4d,
                ["HeightM"] = 3.6d,
                ["BottomOffsetM"] = 0d
            },
            "Bê tông");

        private static readonly ProjectFamilyQuickSchema ArchitecturalWall = Schema(
            ElementCategory.ArchitecturalWall,
            new[] { "ThicknessM", "HeightM", "BottomOffsetM" },
            new[] { "ThicknessM", "HeightM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["ThicknessM"] = 0.2d,
                ["HeightM"] = 3.6d,
                ["BottomOffsetM"] = 0d
            },
            "Gạch");

        private static readonly ProjectFamilyQuickSchema StructuralWall = Schema(
            ElementCategory.StructuralWall,
            new[] { "ThicknessM", "HeightM", "BottomOffsetM" },
            new[] { "ThicknessM", "HeightM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["ThicknessM"] = 0.2d,
                ["HeightM"] = 3.6d,
                ["BottomOffsetM"] = 0d
            },
            "Bê tông");

        private static readonly ProjectFamilyQuickSchema WallPier = Schema(
            ElementCategory.WallPier,
            new[] { "ThicknessM", "HeightM", "BottomOffsetM" },
            new[] { "ThicknessM", "HeightM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["ThicknessM"] = 0.2d,
                ["HeightM"] = 3.6d,
                ["BottomOffsetM"] = 0d
            },
            "Bê tông");

        private static readonly ProjectFamilyQuickSchema GlassWall = Schema(
            ElementCategory.GlassWall,
            new[] { "ThicknessM", "HeightM", "BottomOffsetM" },
            new[] { "ThicknessM", "HeightM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["ThicknessM"] = 0.012d,
                ["HeightM"] = 3.6d,
                ["BottomOffsetM"] = 0d
            },
            "Kính");

        private static readonly ProjectFamilyQuickSchema Slab = Schema(
            ElementCategory.Slab,
            new[] { "ThicknessM", "BottomOffsetM" },
            new[] { "ThicknessM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["ThicknessM"] = 0.12d,
                ["BottomOffsetM"] = 0d
            },
            "Bê tông");

        private static readonly ProjectFamilyQuickSchema Door = Schema(
            ElementCategory.Door,
            new[] { "WidthM", "HeightM", "BottomOffsetM" },
            new[] { "WidthM", "HeightM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["WidthM"] = 0.9d,
                ["HeightM"] = 2.2d,
                ["BottomOffsetM"] = 0d
            },
            "Gỗ");

        private static readonly ProjectFamilyQuickSchema Stair = Schema(
            ElementCategory.Stair,
            new[] { "WidthM", "HeightM", "DepthM", "BottomOffsetM" },
            new[] { "WidthM", "HeightM", "DepthM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["WidthM"] = 1.2d,
                ["HeightM"] = 3.6d,
                ["DepthM"] = 0.3d,
                ["BottomOffsetM"] = 0d
            },
            "Bê tông");

        private static readonly ProjectFamilyQuickSchema Foundation = Schema(
            ElementCategory.Foundation,
            new[] { "ThicknessM", "BottomOffsetM" },
            new[] { "ThicknessM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["ThicknessM"] = 0.5d,
                ["BottomOffsetM"] = 0d
            },
            "Bê tông");

        private static readonly ProjectFamilyQuickSchema Earthwork = Schema(
            ElementCategory.Earthwork,
            new[] { "LengthM", "WidthM", "DepthM", "BottomOffsetM" },
            new[] { "LengthM", "WidthM", "DepthM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["LengthM"] = 1d,
                ["WidthM"] = 1d,
                ["DepthM"] = 0.5d,
                ["BottomOffsetM"] = 0d
            },
            "Đất");

        private static readonly ProjectFamilyQuickSchema CustomQuantity = Schema(
            ElementCategory.CustomQuantity,
            new[] { "LengthM", "WidthM", "HeightM" },
            new[] { "LengthM", "WidthM", "HeightM" },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["LengthM"] = 1d,
                ["WidthM"] = 1d,
                ["HeightM"] = 1d
            },
            string.Empty);

        public static ProjectFamilyQuickSchema GetSchema(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.FloorFinish: return FloorFinish;
                case ElementCategory.Waterproofing: return Waterproofing;
                case ElementCategory.Skirting: return Skirting;
                case ElementCategory.WallFinish: return WallFinish;
                case ElementCategory.CeilingFinish: return CeilingFinish;
                case ElementCategory.Railing: return Railing;
                case ElementCategory.WallOpening: return WallOpening;
                case ElementCategory.Beam: return Beam;
                case ElementCategory.Column: return Column;
                case ElementCategory.ArchitecturalWall: return ArchitecturalWall;
                case ElementCategory.StructuralWall: return StructuralWall;
                case ElementCategory.WallPier: return WallPier;
                case ElementCategory.GlassWall: return GlassWall;
                case ElementCategory.Slab: return Slab;
                case ElementCategory.Door: return Door;
                case ElementCategory.Stair: return Stair;
                case ElementCategory.Foundation: return Foundation;
                case ElementCategory.Earthwork: return Earthwork;
                case ElementCategory.CustomQuantity: return CustomQuantity;
                default: return Empty;
            }
        }

        public static double ParseUiMillimetersToMeters(string key, string text, CultureInfo culture, bool positive)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));
            var raw = (text ?? string.Empty).Trim();
            double valueMm;
            var parsed = double.TryParse(raw, NumberStyles.Float, culture, out valueMm) ||
                         double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out valueMm);
            if (!parsed || double.IsNaN(valueMm) || double.IsInfinity(valueMm))
                throw new InvalidOperationException((key ?? "Giá trị") + " phải là số hữu hạn hợp lệ (mm). Giá trị hiện tại: “" + raw + "”.");
            if (positive && valueMm <= 0d)
                throw new InvalidOperationException((key ?? "Giá trị") + " phải lớn hơn 0 mm.");

            var valueMeters = valueMm / MillimetersPerMeter;
            if (positive && valueMeters <= 0d)
                throw new InvalidOperationException((key ?? "Giá trị") + " quá nhỏ để biểu diễn giá trị dương theo đơn vị nội bộ (m).");
            if (valueMm != 0d && valueMeters == 0d)
                throw new InvalidOperationException((key ?? "Giá trị") + " quá nhỏ để biểu diễn giá trị khác 0 theo đơn vị nội bộ (m).");
            return valueMeters;
        }

        public static string FormatInternalMetersAsMillimeters(string key, string internalMeters, CultureInfo culture)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));
            var raw = (internalMeters ?? string.Empty).Trim();
            if (!TryParseInternalMeters(raw, culture, out var meters))
                throw new InvalidOperationException((key ?? "Giá trị") + " đang có giá trị nội bộ không hợp lệ: “" + raw + "”.");
            return FormatMetersAsMillimeters(
                meters,
                culture,
                (key ?? "Giá trị") + " không thể biểu diễn an toàn theo mm với định dạng hiện tại. Giá trị nội bộ: “" + raw + "”.");
        }

        public static string SuggestName(ElementCategory category, IReadOnlyDictionary<string, string> internalValues)
        {
            if (internalValues == null) throw new ArgumentNullException(nameof(internalValues));
            string Mm(string key)
            {
                if (!internalValues.TryGetValue(key, out var raw) || !TryParseInternalMeters(raw, CultureInfo.InvariantCulture, out var meters))
                    throw new InvalidOperationException("Thiếu hoặc sai giá trị " + key + " để tự đặt tên Family.");
                return FormatMetersAsMillimeters(
                    meters,
                    CultureInfo.InvariantCulture,
                    "Giá trị " + key + " không thể biểu diễn an toàn theo mm khi tự đặt tên Family.");
            }

            switch (category)
            {
                case ElementCategory.FloorFinish: return "HTS" + Mm("ThicknessM");
                case ElementCategory.Waterproofing: return "CT" + Mm("ThicknessM");
                case ElementCategory.Skirting: return "ChanTuong" + Mm("HeightM") + "x" + Mm("ThicknessM");
                case ElementCategory.WallFinish: return "HTT" + Mm("ThicknessM") + "xH" + Mm("HeightM");
                case ElementCategory.CeilingFinish: return "Tran" + Mm("ThicknessM");
                case ElementCategory.Railing: return "LanCanH" + Mm("HeightM") + "x" + Mm("WidthM");
                case ElementCategory.WallOpening: return "LoTuong" + Mm("WidthM") + "x" + Mm("HeightM");
                case ElementCategory.Beam: return "D" + Mm("WidthM") + "x" + Mm("HeightM");
                case ElementCategory.Column: return "C" + Mm("WidthM") + "x" + Mm("DepthM");
                case ElementCategory.ArchitecturalWall: return "T" + Mm("ThicknessM");
                case ElementCategory.StructuralWall: return "VT" + Mm("ThicknessM");
                case ElementCategory.WallPier: return "TV" + Mm("ThicknessM");
                case ElementCategory.GlassWall: return "TK" + Mm("ThicknessM");
                case ElementCategory.Slab: return "S" + Mm("ThicknessM");
                case ElementCategory.Door: return "Cua" + Mm("WidthM") + "x" + Mm("HeightM");
                case ElementCategory.Stair: return "CauThang" + Mm("WidthM") + "xH" + Mm("HeightM") + "xD" + Mm("DepthM");
                case ElementCategory.Foundation: return "Móng BTCT H" + Mm("ThicknessM");
                case ElementCategory.Earthwork: return "DaoDap" + Mm("LengthM") + "x" + Mm("WidthM") + "x" + Mm("DepthM");
                case ElementCategory.CustomQuantity: return "Khac" + Mm("LengthM") + "x" + Mm("WidthM") + "x" + Mm("HeightM");
                default: return category + " Auto";
            }
        }

        public static IReadOnlyList<ProjectFamily> FindIdentityMatches(
            ProjectState project,
            ElementCategory category,
            IReadOnlyDictionary<string, string> internalValues,
            string material)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (internalValues == null) throw new ArgumentNullException(nameof(internalValues));
            var schema = GetSchema(category);
            if (!schema.SupportsQuickForm || schema.IdentityKeys.Count == 0) return Array.Empty<ProjectFamily>();

            var expected = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in schema.IdentityKeys)
            {
                if (!internalValues.TryGetValue(key, out var raw) || !TryParseInternalMeters(raw, CultureInfo.InvariantCulture, out var meters))
                    throw new InvalidOperationException("Thiếu hoặc sai giá trị identity " + key + " cho Auto Family.");
                expected[key] = meters;
            }

            var normalizedMaterial = (material ?? string.Empty).Trim();
            var matches = new List<ProjectFamily>();
            foreach (var family in project.Families.Where(x => x.Category == category))
            {
                var same = true;
                foreach (var pair in expected)
                {
                    if (!family.Properties.TryGetValue(pair.Key, out var raw) ||
                        !TryParseInternalMeters(raw, CultureInfo.InvariantCulture, out var actual) ||
                        Math.Abs(actual - pair.Value) > IdentityToleranceM)
                    {
                        same = false;
                        break;
                    }
                }
                if (!same) continue;

                if (normalizedMaterial.Length > 0)
                {
                    if (!family.Properties.TryGetValue("Material", out var existingMaterial) ||
                        !string.Equals((existingMaterial ?? string.Empty).Trim(), normalizedMaterial, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                matches.Add(family);
            }
            return matches.AsReadOnly();
        }

        public static string MakeUniqueName(ProjectState project, ElementCategory category, string baseName)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = (baseName ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Family name is required.", nameof(baseName));
            if (!project.Families.Any(x => x.Category == category && string.Equals(x.Name, normalized, StringComparison.OrdinalIgnoreCase)))
                return normalized;

            for (var index = 2; index <= 10000; index++)
            {
                var candidate = normalized + " " + index.ToString(CultureInfo.InvariantCulture);
                if (!project.Families.Any(x => x.Category == category && string.Equals(x.Name, candidate, StringComparison.OrdinalIgnoreCase)))
                    return candidate;
            }
            throw new InvalidOperationException("Không tìm được tên Family duy nhất cho " + normalized + ".");
        }

        private static ProjectFamilyQuickSchema Schema(
            ElementCategory category,
            IEnumerable<string> formKeys,
            IEnumerable<string> identityKeys,
            IDictionary<string, double> defaultsM,
            string defaultMaterial) =>
            new ProjectFamilyQuickSchema(category, formKeys, identityKeys, defaultsM, defaultMaterial);

        private static string FormatMetersAsMillimeters(double meters, CultureInfo culture, string invalidMessage)
        {
            var millimeters = meters * MillimetersPerMeter;
            if (double.IsNaN(millimeters) || double.IsInfinity(millimeters))
                throw new InvalidOperationException(invalidMessage);

            var formatted = millimeters.ToString("0.###", culture);
            if (millimeters != 0d &&
                double.TryParse(formatted, NumberStyles.Float, culture, out var formattedMillimeters) &&
                formattedMillimeters == 0d)
                throw new InvalidOperationException(invalidMessage);
            return formatted;
        }

        private static bool TryParseInternalMeters(string raw, CultureInfo fallbackCulture, out double meters)
        {
            var parsed = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out meters) ||
                         double.TryParse(raw, NumberStyles.Float, fallbackCulture, out meters);
            return parsed && !double.IsNaN(meters) && !double.IsInfinity(meters);
        }
    }
}