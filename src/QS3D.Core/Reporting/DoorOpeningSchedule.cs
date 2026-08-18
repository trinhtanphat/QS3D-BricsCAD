using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    public sealed class DoorOpeningScheduleRow
    {
        public string ProjectId { get; set; } = string.Empty;
        public string DrawingFingerprint { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public double WidthM { get; set; }
        public double HeightM { get; set; }
        public double SillHeightM { get; set; }
        public double ThicknessM { get; set; }
        public int Count { get; set; }
        public double OpeningAreaM2 { get; set; }
        public int HostCount { get; set; }
        public IList<string> ElementIds { get; } = new List<string>();
        public IList<string> HostIds { get; } = new List<string>();
        public IList<string> SourceHandles { get; } = new List<string>();
    }

    public static class DoorOpeningScheduleBuilder
    {
        public static IReadOnlyList<DoorOpeningScheduleRow> Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Door/opening schedule");
            var floors = project.Floors.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = project.Families.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var rows = new Dictionary<string, DoorOpeningScheduleRow>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in project.Elements
                .Where(x => x.Category == ElementCategory.Door || x.Category == ElementCategory.WallOpening)
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var floorId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FloorId);
                var familyId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FamilyId);
                families.TryGetValue(familyId, out var family);
                if (family != null && family.Category != element.Category)
                    throw new InvalidOperationException("Door/opening schedule element " + element.Id + " category " + element.Category + " does not match Family " + family.Id + " category " + family.Category + ". Repair the Family relation before reporting.");
                var widthM = Positive(Number(element, family, "WidthM", 0.9d), element.Id + "/WidthM");
                var heightM = Positive(Number(element, family, "HeightM", 2.2d), element.Id + "/HeightM");
                var sillM = NonNegative(Number(element, family, "SillHeightM", Number(element, family, "BottomOffsetM", 0d)), element.Id + "/SillHeightM");
                var thicknessM = NonNegative(Number(element, family, "ThicknessM", 0d), element.Id + "/ThicknessM");
                var material = Text(element, family, "Material");
                var areaM2 = element.Quantities.TryGetValue("OpeningAreaM2", out var storedArea)
                    ? NonNegative(storedArea, element.Id + "/OpeningAreaM2")
                    : Multiply(widthM, heightM, element.Id + "/OpeningAreaM2");
                var floor = floors.TryGetValue(floorId, out var floorName) ? floorName : floorId;
                var familyName = family?.Name ?? familyId;
                var category = ScheduleCategory(element);
                var hostId = element.Properties.TryGetValue("HostWallId", out var hostRaw)
                    ? CanonicalOptionalHostId(project, hostRaw, element.Id)
                    : string.Empty;
                var key = GroupKey(
                    floorId,
                    category,
                    familyId,
                    CanonicalNumber(widthM),
                    CanonicalNumber(heightM),
                    CanonicalNumber(sillM),
                    CanonicalNumber(thicknessM),
                    material);
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new DoorOpeningScheduleRow
                    {
                        ProjectId = project.ProjectId,
                        DrawingFingerprint = project.DrawingFingerprint,
                        Floor = floor,
                        Category = category,
                        FamilyName = familyName,
                        Material = material,
                        WidthM = widthM,
                        HeightM = heightM,
                        SillHeightM = sillM,
                        ThicknessM = thicknessM
                    };
                    rows[key] = row;
                    order.Add(key);
                }
                row.Count = checked(row.Count + 1);
                row.OpeningAreaM2 = Add(row.OpeningAreaM2, areaM2, element.Id + "/opening schedule area");
                row.ElementIds.Add(element.Id);
                ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);
                if (hostId.Length > 0 && !row.HostIds.Contains(hostId, StringComparer.OrdinalIgnoreCase)) row.HostIds.Add(hostId);
            }

            foreach (var row in rows.Values) row.HostCount = row.HostIds.Count;
            return order.Select(x => rows[x]).ToList().AsReadOnly();
        }

        private static string GroupKey(params string[] tokens)
        {
            var key = new StringBuilder();
            foreach (var raw in tokens)
            {
                var token = raw ?? string.Empty;
                key.Append(token.Length.ToString(CultureInfo.InvariantCulture))
                    .Append(':')
                    .Append(token);
            }
            return key.ToString();
        }

        private static string CanonicalNumber(double value)
        {
            return (value == 0d ? 0d : value).ToString("R", CultureInfo.InvariantCulture);
        }

        private static string ScheduleCategory(ProjectElement element)
        {
            if (element.Category != ElementCategory.WallOpening) return element.Category.ToString();
            if (!element.Properties.TryGetValue("OpeningUsage", out var raw) || string.IsNullOrWhiteSpace(raw))
                return ElementCategory.WallOpening.ToString();
            var usage = raw.Trim();
            return string.Equals(usage, "Window", StringComparison.OrdinalIgnoreCase)
                ? "Window"
                : ElementCategory.WallOpening.ToString();
        }

        private static string CanonicalOptionalHostId(ProjectState project, string? value, string elementId)
        {
            var raw = value ?? string.Empty;
            if (raw.Length == 0) return string.Empty;
            if (string.IsNullOrWhiteSpace(raw) || !string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Door/opening schedule requires canonical HostWallId without surrounding whitespace on element: " + elementId + ".");

            var host = project.FindElement(raw);
            if (host == null)
                throw new InvalidOperationException("Door/opening schedule cannot report missing HostWallId target '" + raw + "' on element: " + elementId + ".");
            if (!IsWall(host.Category))
                throw new InvalidOperationException("Door/opening schedule requires HostWallId target '" + raw + "' to be a wall for element: " + elementId + ".");
            return raw;
        }

        private static bool IsWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier ||
            category == ElementCategory.StructuralWall;

        private static double Number(ProjectElement element, ProjectFamily? family, string key, double fallback)
        {
            if (element.Properties.TryGetValue(key, out var instanceRaw) && !string.IsNullOrWhiteSpace(instanceRaw)) return Parse(instanceRaw, element.Id + "/" + key);
            if (family != null && family.Properties.TryGetValue(key, out var familyRaw) && !string.IsNullOrWhiteSpace(familyRaw)) return Parse(familyRaw, family.Id + "/" + key);
            return fallback;
        }

        private static string Text(ProjectElement element, ProjectFamily? family, string key)
        {
            if (element.Properties.TryGetValue(key, out var instance) && !string.IsNullOrWhiteSpace(instance)) return instance.Trim();
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) return inherited.Trim();
            return string.Empty;
        }

        private static double Parse(string raw, string label)
        {
            if (!double.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(label + " must be a finite invariant numeric value.");
            return value;
        }

        private static double Positive(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d) throw new InvalidOperationException(label + " must be finite and > 0.");
            return value;
        }

        private static double NonNegative(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new InvalidOperationException(label + " must be finite and >= 0.");
            return value;
        }

        private static double Multiply(double left, double right, string label)
        {
            var value = left * right;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " overflowed.");
            if (value == 0d && left > 0d && right > 0d)
                throw new InvalidOperationException(label + " underflowed: positive finite dimensions rounded to zero area.");
            return value;
        }

        private static double Add(double left, double right, string label)
        {
            var value = left + right;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " overflowed.");
            return value;
        }
    }
}
