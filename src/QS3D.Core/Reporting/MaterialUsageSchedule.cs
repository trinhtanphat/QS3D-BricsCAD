using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    public sealed class MaterialUsageRow
    {
        public string ProjectId { get; set; } = string.Empty;
        public string DrawingFingerprint { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string UnitHint { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public int ElementCount { get; set; }
        public double LengthM { get; set; }
        public double AreaM2 { get; set; }
        public double VolumeM3 { get; set; }
        public double MassKg { get; set; }
        public IList<string> ElementIds { get; } = new List<string>();
        public IList<string> SourceHandles { get; } = new List<string>();

        public double PrimaryQuantity
        {
            get
            {
                var unit = NormalizeUnit(UnitHint);
                if (unit == "m") return LengthM;
                if (unit == "m2") return AreaM2;
                if (unit == "m3") return VolumeM3;
                if (unit == "kg") return MassKg;
                return 0d;
            }
        }

        private static string NormalizeUnit(string value)
        {
            var unit = (value ?? string.Empty).Trim().ToLowerInvariant();
            return unit.Replace("²", "2").Replace("³", "3").Replace("^", string.Empty).Replace(" ", string.Empty);
        }
    }

    public static class MaterialUsageScheduleBuilder
    {
        public static IReadOnlyList<MaterialUsageRow> Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Material usage schedule");
            RoomFinishIdentityService.ValidateProject(project);
            var floors = project.Floors.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = project.Families.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var units = ProjectMaterialCatalog.GetAll(project)
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First().Unit, StringComparer.OrdinalIgnoreCase);
            var rows = new Dictionary<string, MaterialUsageRow>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in project.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (AutoRoomLifecycle.IsExcludedFromQuantity(project, element)) continue;
                families.TryGetValue(element.FamilyId, out var family);
                var material = Effective(element, family, "Material");
                if (material.Length > 0)
                    Add(project, element, family, floors, units, rows, order, material, "Material", MetricsForMainMaterial(element));

                if (element.Category == ElementCategory.GlassWall)
                {
                    var frameMaterial = Effective(element, family, "CurtainFrameMaterial");
                    if (frameMaterial.Length > 0)
                    {
                        var frame = new UsageMetrics
                        {
                            LengthM = Q(element, "CurtainFrameLengthM"),
                            AreaM2 = Q(element, "CurtainFrameFaceAreaM2")
                        };
                        Add(project, element, family, floors, units, rows, order, frameMaterial, "CurtainFrame", frame);
                    }
                }
            }
            return order.Select(x => rows[x]).ToList().AsReadOnly();
        }

        private sealed class UsageMetrics
        {
            public double LengthM { get; set; }
            public double AreaM2 { get; set; }
            public double VolumeM3 { get; set; }
            public double MassKg { get; set; }
        }

        private static UsageMetrics MetricsForMainMaterial(ProjectElement element)
        {
            var metrics = new UsageMetrics
            {
                LengthM = Q(element, "LengthM"),
                VolumeM3 = QFirst(element, "NetVolumeM3", "VolumeM3"),
                MassKg = Q(element, "WeightKg")
            };

            switch (element.Category)
            {
                case ElementCategory.GlassWall:
                    metrics.AreaM2 = QFirst(element, "CurtainNetGlassAreaM2", "NetWallAreaM2");
                    break;
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.WallPier:
                case ElementCategory.StructuralWall:
                    metrics.AreaM2 = QFirst(element, "NetWallAreaM2", "SideAreaM2");
                    break;
                case ElementCategory.WallFinish:
                    metrics.AreaM2 = QFirst(element, "NetFinishAreaM2", "SideAreaM2", "AreaM2");
                    break;
                case ElementCategory.FloorFinish:
                case ElementCategory.Waterproofing:
                    metrics.AreaM2 = QFirst(element, "BottomAreaM2", "AreaM2");
                    break;
                case ElementCategory.CeilingFinish:
                    metrics.AreaM2 = QFirst(element, "TopAreaM2", "AreaM2");
                    break;
                case ElementCategory.Door:
                case ElementCategory.WallOpening:
                    metrics.AreaM2 = QFirst(element, "OpeningAreaM2", "AreaM2");
                    break;
                case ElementCategory.Skirting:
                    metrics.LengthM = QFirst(element, "SkirtingLengthM", "InnerPerimeterM", "PerimeterM", "LengthM");
                    metrics.AreaM2 = Q(element, "AreaM2");
                    break;
                default:
                    metrics.AreaM2 = QFirst(element, "AreaM2", "BottomAreaM2");
                    break;
            }
            return metrics;
        }

        private static void Add(
            ProjectState project,
            ProjectElement element,
            ProjectFamily? family,
            IDictionary<string, string> floors,
            IDictionary<string, string> units,
            IDictionary<string, MaterialUsageRow> rows,
            IList<string> order,
            string material,
            string component,
            UsageMetrics metrics)
        {
            var floor = floors.TryGetValue(element.FloorId, out var floorName) ? floorName : element.FloorId;
            var familyName = family?.Name ?? element.FamilyId;
            var category = element.Category.ToString();
            var key = element.FloorId + "\u001f" + material + "\u001f" + component + "\u001f" + category + "\u001f" + element.FamilyId;
            if (!rows.TryGetValue(key, out var row))
            {
                row = new MaterialUsageRow
                {
                    ProjectId = project.ProjectId,
                    DrawingFingerprint = project.DrawingFingerprint,
                    Floor = floor,
                    MaterialName = material,
                    UnitHint = units.TryGetValue(material, out var unit) ? unit : string.Empty,
                    Component = component,
                    Category = category,
                    FamilyName = familyName
                };
                rows[key] = row;
                order.Add(key);
            }
            row.ElementCount = QuantityReportMath.AddCount(row.ElementCount, 1);
            row.LengthM = QuantityReportMath.Add(row.LengthM, metrics.LengthM, element.Id + "/material length");
            row.AreaM2 = QuantityReportMath.Add(row.AreaM2, metrics.AreaM2, element.Id + "/material area");
            row.VolumeM3 = QuantityReportMath.Add(row.VolumeM3, metrics.VolumeM3, element.Id + "/material volume");
            row.MassKg = QuantityReportMath.Add(row.MassKg, metrics.MassKg, element.Id + "/material mass");
            row.ElementIds.Add(element.Id);
            ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);
        }

        private static string Effective(ProjectElement element, ProjectFamily? family, string key)
        {
            if (element.Properties.TryGetValue(key, out var instance) && !string.IsNullOrWhiteSpace(instance)) return instance.Trim();
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) return inherited.Trim();
            return string.Empty;
        }

        private static double QFirst(ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
                if (element.Quantities.ContainsKey(key)) return Q(element, key);
            return 0d;
        }

        private static double Q(ProjectElement element, string key, double fallback = 0d)
        {
            var value = element.Quantities.TryGetValue(key, out var stored) ? stored : fallback;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(element.Id + "/" + key + " must be finite and non-negative.");
            return value;
        }
    }
}
