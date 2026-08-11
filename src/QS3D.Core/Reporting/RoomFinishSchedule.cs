using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    public sealed class RoomFinishScheduleRow
    {
        public string Floor { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string UnitHint { get; set; } = string.Empty;
        public int Count { get; set; }
        public double LengthM { get; set; }
        public double AreaM2 { get; set; }
        public double PrimaryQuantity { get; set; }
        public IList<string> ElementIds { get; } = new List<string>();
        public IList<string> RoomIds { get; } = new List<string>();
    }

    public static class RoomFinishScheduleBuilder
    {
        private static readonly ElementCategory[] FinishCategories =
        {
            ElementCategory.FloorFinish,
            ElementCategory.Waterproofing,
            ElementCategory.Skirting,
            ElementCategory.WallFinish,
            ElementCategory.CeilingFinish
        };

        public static IReadOnlyList<RoomFinishScheduleRow> Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Room finish schedule");
            RoomFinishIdentityService.ValidateProject(project);
            var floors = project.Floors.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = project.Families.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var rooms = project.Elements.Where(x => x.Category == ElementCategory.Room).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var units = ProjectMaterialCatalog.GetAll(project)
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First().Unit, StringComparer.OrdinalIgnoreCase);
            var rows = new Dictionary<string, RoomFinishScheduleRow>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in project.Elements.Where(x => FinishCategories.Contains(x.Category)).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (AutoRoomLifecycle.IsExcludedFromQuantity(project, element)) continue;
                families.TryGetValue(element.FamilyId, out var family);
                var material = Effective(element, family, "Material");
                var roomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, element);
                var roomLabel = RoomLabel(roomId, rooms);
                var floor = floors.TryGetValue(element.FloorId, out var floorName) ? floorName : element.FloorId;
                var familyName = family?.Name ?? element.FamilyId;
                var metrics = Metrics(element);
                var unitHint = metrics.DefaultUnit;
                if (material.Length > 0 && units.TryGetValue(material, out var unit) && SameDimension(unit, metrics.DefaultUnit)) unitHint = unit;
                var primary = Primary(unitHint, metrics.LengthM, metrics.AreaM2);

                var roomKey = roomId.Length > 0 ? roomId : "(unlinked)";
                var key = string.Join("\u001f", element.FloorId, roomKey, element.Category.ToString(), element.FamilyId, material, unitHint);
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new RoomFinishScheduleRow
                    {
                        Floor = floor,
                        Room = roomLabel,
                        Category = element.Category.ToString(),
                        FamilyName = familyName,
                        Material = material,
                        UnitHint = unitHint
                    };
                    rows[key] = row;
                    order.Add(key);
                }
                row.Count = checked(row.Count + 1);
                row.LengthM = Add(row.LengthM, metrics.LengthM, element.Id + "/finish length");
                row.AreaM2 = Add(row.AreaM2, metrics.AreaM2, element.Id + "/finish area");
                row.PrimaryQuantity = Add(row.PrimaryQuantity, primary, element.Id + "/finish primary quantity");
                row.ElementIds.Add(element.Id);
                if (roomId.Length > 0 && !row.RoomIds.Contains(roomId, StringComparer.OrdinalIgnoreCase)) row.RoomIds.Add(roomId);
            }
            return order.Select(x => rows[x]).ToList().AsReadOnly();
        }

        private sealed class FinishMetrics
        {
            public double LengthM { get; set; }
            public double AreaM2 { get; set; }
            public string DefaultUnit { get; set; } = "m²";
        }

        private static FinishMetrics Metrics(ProjectElement element)
        {
            switch (element.Category)
            {
                case ElementCategory.Skirting:
                    return new FinishMetrics
                    {
                        LengthM = FirstQuantity(element, "SkirtingLengthM", "InnerPerimeterM", "PerimeterM", "LengthM"),
                        AreaM2 = FirstQuantity(element, "AreaM2"),
                        DefaultUnit = "m"
                    };
                case ElementCategory.WallFinish:
                    return new FinishMetrics
                    {
                        AreaM2 = FirstQuantity(element, "NetFinishAreaM2", "SideAreaM2", "AreaM2"),
                        DefaultUnit = "m²"
                    };
                case ElementCategory.CeilingFinish:
                    return new FinishMetrics
                    {
                        AreaM2 = FirstQuantity(element, "TopAreaM2", "AreaM2"),
                        DefaultUnit = "m²"
                    };
                case ElementCategory.FloorFinish:
                case ElementCategory.Waterproofing:
                    return new FinishMetrics
                    {
                        AreaM2 = FirstQuantity(element, "BottomAreaM2", "AreaM2"),
                        DefaultUnit = "m²"
                    };
                default:
                    throw new InvalidOperationException("Unsupported room-finish category: " + element.Category);
            }
        }

        private static string RoomLabel(string roomId, IDictionary<string, ProjectElement> rooms)
        {
            if (roomId.Length == 0) return "(chưa liên kết phòng)";
            if (!rooms.TryGetValue(roomId, out var room)) return roomId;
            foreach (var key in new[] { "RoomName", "Name", "Number", "Mark" })
                if (room.Properties.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw)) return raw.Trim();
            return room.Id;
        }

        private static string Effective(ProjectElement element, ProjectFamily? family, string key)
        {
            if (element.Properties.TryGetValue(key, out var instance) && !string.IsNullOrWhiteSpace(instance)) return instance.Trim();
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) return inherited.Trim();
            return string.Empty;
        }

        private static double Primary(string unitHint, double lengthM, double areaM2)
        {
            var unit = NormalizeUnit(unitHint);
            if (unit == "m") return lengthM;
            if (unit == "m2") return areaM2;
            return areaM2 > 0d ? areaM2 : lengthM;
        }

        private static bool SameDimension(string left, string right) => string.Equals(NormalizeUnit(left), NormalizeUnit(right), StringComparison.Ordinal);

        private static string NormalizeUnit(string unit) =>
            (unit ?? string.Empty).Trim().ToLowerInvariant().Replace("²", "2").Replace("^", string.Empty).Replace(" ", string.Empty);

        private static double FirstQuantity(ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!element.Quantities.TryGetValue(key, out var value)) continue;
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                    throw new InvalidOperationException(element.Id + "/" + key + " must be finite and non-negative.");
                return value;
            }
            return 0d;
        }

        private static double Add(double left, double right, string label)
        {
            var result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " overflowed.");
            return result;
        }
    }
}
