using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                families.TryGetValue(element.FamilyId, out var family);
                var widthM = Positive(Number(element, family, "WidthM", 0.9d), element.Id + "/WidthM");
                var heightM = Positive(Number(element, family, "HeightM", 2.2d), element.Id + "/HeightM");
                var sillM = NonNegative(Number(element, family, "SillHeightM", Number(element, family, "BottomOffsetM", 0d)), element.Id + "/SillHeightM");
                var thicknessM = NonNegative(Number(element, family, "ThicknessM", 0d), element.Id + "/ThicknessM");
                var material = Text(element, family, "Material");
                var areaM2 = element.Quantities.TryGetValue("OpeningAreaM2", out var storedArea)
                    ? NonNegative(storedArea, element.Id + "/OpeningAreaM2")
                    : Multiply(widthM, heightM, element.Id + "/OpeningAreaM2");
                var floor = floors.TryGetValue(element.FloorId, out var floorName) ? floorName : element.FloorId;
                var familyName = family?.Name ?? element.FamilyId;
                var category = ScheduleCategory(element);
                var hostId = element.Properties.TryGetValue("HostWallId", out var hostRaw) ? (hostRaw ?? string.Empty).Trim() : string.Empty;
                var key = string.Join("\u001f",
                    element.FloorId,
                    category,
                    element.FamilyId,
                    widthM.ToString("R", CultureInfo.InvariantCulture),
                    heightM.ToString("R", CultureInfo.InvariantCulture),
                    sillM.ToString("R", CultureInfo.InvariantCulture),
                    thicknessM.ToString("R", CultureInfo.InvariantCulture),
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
