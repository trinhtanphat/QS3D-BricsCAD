using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
                if (unit.Length == 0) return 0d;
                throw new InvalidOperationException("Material Usage primary quantity does not support unit: " + UnitHint + ".");
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
            var generation = MaterialUsageGenerationSnapshot.CaptureStable(project);
            var result = BuildDetached(generation.DetachedProject);
            generation.Revalidate(project);
            return result;
        }

        private static IReadOnlyList<MaterialUsageRow> BuildDetached(ProjectState detachedProject)
        {
            ReportingProjectIdentityGuard.RequireUniqueElementIds(detachedProject, "Material usage schedule");
            RoomFinishIdentityService.ValidateProject(detachedProject);
            var floors = detachedProject.Floors.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = detachedProject.Families.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var units = ProjectMaterialCatalog.GetAll(detachedProject)
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First().Unit, StringComparer.OrdinalIgnoreCase);
            var rows = new Dictionary<string, UsageGroup>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in detachedProject.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (AutoRoomLifecycle.IsExcludedFromQuantity(detachedProject, element)) continue;
                var familyId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FamilyId);
                families.TryGetValue(familyId, out var family);
                if (family != null && family.Category != element.Category)
                    throw new InvalidOperationException("Material usage element " + element.Id + " category " + element.Category + " does not match Family " + family.Id + " category " + family.Category + ". Repair the Family relation before reporting.");
                var material = Effective(element, family, "Material");
                if (material.Length > 0)
                    Add(detachedProject, element, family, floors, units, rows, order, material, "Material", MetricsForMainMaterial(element, family));

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
                        Add(detachedProject, element, family, floors, units, rows, order, frameMaterial, "CurtainFrame", frame);
                    }
                }
            }

            foreach (var key in order) rows[key].FinalizeQuantities();
            return order.Select(x => rows[x].Row).ToList().AsReadOnly();
        }

        private sealed class MaterialUsageGenerationSnapshot
        {
            private MaterialUsageGenerationSnapshot(ProjectState detachedProject, string semanticSignature)
            {
                DetachedProject = detachedProject;
                _semanticSignature = semanticSignature;
            }

            private readonly string _semanticSignature;
            internal ProjectState DetachedProject { get; }

            internal static MaterialUsageGenerationSnapshot CaptureStable(ProjectState project)
            {
                ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Material usage schedule");
                RoomFinishIdentityService.ValidateProject(project);
                var before = SemanticSignature(project);
                var detached = CloneProject(project);
                var after = SemanticSignature(project);
                if (!string.Equals(before, after, StringComparison.Ordinal))
                    throw new InvalidOperationException("Material usage source generation changed during snapshot capture.");
                return new MaterialUsageGenerationSnapshot(detached, after);
            }

            internal void Revalidate(ProjectState project)
            {
                if (!string.Equals(_semanticSignature, SemanticSignature(project), StringComparison.Ordinal))
                    throw new InvalidOperationException("Material usage source generation changed during aggregation.");
            }

            private static ProjectState CloneProject(ProjectState source)
            {
                var clone = new ProjectState(source.ProjectId, source.Name)
                {
                    DrawingPath = source.DrawingPath,
                    DrawingFingerprint = source.DrawingFingerprint
                };
                foreach (var pair in source.Metadata) clone.Metadata[pair.Key] = pair.Value;
                foreach (var zone in source.Zones) clone.Zones.Add(new ZoneDefinition(zone.Id, zone.Name));
                foreach (var floor in source.Floors) clone.Floors.Add(new FloorDefinition(floor.Id, floor.Name, floor.ElevationM));
                foreach (var family in source.Families)
                {
                    var copy = new ProjectFamily(family.Id, family.Name, family.Category);
                    foreach (var pair in family.Properties) copy.Properties[pair.Key] = pair.Value;
                    clone.Families.Add(copy);
                }
                foreach (var element in source.Elements)
                {
                    if (element == null) throw new InvalidOperationException("Material usage source contains a null element.");
                    var copy = new ProjectElement(element.Id, element.Category, element.FamilyId, element.FloorId, element.ZoneId)
                    {
                        DrawingFingerprint = element.DrawingFingerprint
                    };
                    foreach (var pair in element.Properties) copy.Properties[pair.Key] = pair.Value;
                    foreach (var pair in element.Quantities) copy.Quantities[pair.Key] = pair.Value;
                    foreach (var handle in element.SourceHandles) copy.AddSourceHandlePersistenceValue(handle);
                    foreach (var dependency in element.DependsOn) copy.AddDependencyPersistenceValue(dependency);
                    clone.Elements.Add(copy);
                }
                if (source.ActiveZoneId.Length > 0) clone.ActiveZoneId = source.ActiveZoneId;
                if (source.ActiveFloorId.Length > 0) clone.ActiveFloorId = source.ActiveFloorId;
                return clone;
            }

            private static string SemanticSignature(ProjectState project)
            {
                var b = new StringBuilder();
                Token(b, project.ProjectId); Token(b, project.Name); Token(b, project.DrawingFingerprint);
                Token(b, project.ActiveZoneId); Token(b, project.ActiveFloorId); Pairs(b, project.Metadata);
                var zones = project.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
                Token(b, zones.Count.ToString(CultureInfo.InvariantCulture));
                foreach (var x in zones) { Token(b, x.Id); Token(b, x.Name); }
                var floors = project.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
                Token(b, floors.Count.ToString(CultureInfo.InvariantCulture));
                foreach (var x in floors) { Token(b, x.Id); Token(b, x.Name); Token(b, x.ElevationM.ToString("R", CultureInfo.InvariantCulture)); }
                var families = project.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
                Token(b, families.Count.ToString(CultureInfo.InvariantCulture));
                foreach (var x in families) { Token(b, x.Id); Token(b, x.Name); Token(b, ((int)x.Category).ToString(CultureInfo.InvariantCulture)); Pairs(b, x.Properties); }
                var elements = project.Elements.ToList();
                Token(b, elements.Count.ToString(CultureInfo.InvariantCulture));
                foreach (var x in elements.OrderBy(x => x == null ? string.Empty : x.Id, StringComparer.OrdinalIgnoreCase).ThenBy(x => x == null ? string.Empty : x.Id, StringComparer.Ordinal))
                {
                    if (x == null) { Token(b, "<null>"); continue; }
                    Token(b, x.Id); Token(b, ((int)x.Category).ToString(CultureInfo.InvariantCulture));
                    Token(b, x.FamilyId); Token(b, x.FloorId); Token(b, x.ZoneId); Token(b, x.DrawingFingerprint);
                    Pairs(b, x.Properties); Quantities(b, x.Quantities); Strings(b, x.SourceHandles); Strings(b, x.DependsOn);
                }
                return b.ToString();
            }

            private static void Pairs(StringBuilder b, IEnumerable<KeyValuePair<string, string>> source)
            {
                var values = source.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Key, StringComparer.Ordinal).ToList();
                Token(b, values.Count.ToString(CultureInfo.InvariantCulture));
                foreach (var x in values) { Token(b, x.Key); Token(b, x.Value); }
            }

            private static void Quantities(StringBuilder b, IEnumerable<KeyValuePair<string, double>> source)
            {
                var values = source.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Key, StringComparer.Ordinal).ToList();
                Token(b, values.Count.ToString(CultureInfo.InvariantCulture));
                foreach (var x in values) { Token(b, x.Key); Token(b, BitConverter.DoubleToInt64Bits(x.Value).ToString("X16", CultureInfo.InvariantCulture)); }
            }

            private static void Strings(StringBuilder b, IEnumerable<string> source)
            {
                var values = source.ToList();
                Token(b, values.Count.ToString(CultureInfo.InvariantCulture));
                foreach (var x in values) Token(b, x);
            }

            private static void Token(StringBuilder b, string value)
            {
                var text = value ?? string.Empty;
                b.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text).Append(';');
            }
        }

        private sealed class UsageMetrics
        {
            public double LengthM { get; set; }
            public double AreaM2 { get; set; }
            public double VolumeM3 { get; set; }
            public double MassKg { get; set; }
        }

        private sealed class UsageGroup
        {
            private readonly StableAccumulator _length = new StableAccumulator();
            private readonly StableAccumulator _area = new StableAccumulator();
            private readonly StableAccumulator _volume = new StableAccumulator();
            private readonly StableAccumulator _mass = new StableAccumulator();

            public UsageGroup(MaterialUsageRow row)
            {
                Row = row ?? throw new ArgumentNullException(nameof(row));
            }

            public MaterialUsageRow Row { get; }

            public void Add(UsageMetrics metrics, string label)
            {
                if (metrics == null) throw new ArgumentNullException(nameof(metrics));
                _length.Add(metrics.LengthM, label + "/material length");
                _area.Add(metrics.AreaM2, label + "/material area");
                _volume.Add(metrics.VolumeM3, label + "/material volume");
                _mass.Add(metrics.MassKg, label + "/material mass");
            }

            public void FinalizeQuantities()
            {
                Row.LengthM = _length.Value("material length");
                Row.AreaM2 = _area.Value("material area");
                Row.VolumeM3 = _volume.Value("material volume");
                Row.MassKg = _mass.Value("material mass");
            }
        }

        private sealed class StableAccumulator
        {
            private double _sum;
            private double _compensation;
            private bool _sawSwallowedContribution;

            public void Add(double value, string label)
            {
                var incoming = QuantityReportMath.NonNegative(value, label);
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");

                var result = _sum + incoming;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Material usage aggregate overflow: " + label + ".");

                if ((incoming != 0d && result == _sum) || (_sum != 0d && result == incoming))
                    _sawSwallowedContribution = true;

                var correction = Math.Abs(_sum) >= Math.Abs(incoming)
                    ? (_sum - result) + incoming
                    : (incoming - result) + _sum;
                var nextCompensation = _compensation + correction;
                if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation))
                    throw new OverflowException("Material usage aggregate compensation overflow: " + label + ".");

                _sum = result == 0d ? 0d : result;
                _compensation = nextCompensation == 0d ? 0d : nextCompensation;
            }

            public double Value(string label)
            {
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var result = _sum + _compensation;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Material usage aggregate overflow: " + label + ".");
                if (_sawSwallowedContribution && _compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation))
                    throw new OverflowException("Material usage aggregate lost a non-zero swallowed contribution at floating-point precision: " + label + ".");
                if (_sum != 0d && result == _compensation)
                    throw new OverflowException("Material usage aggregate lost a non-zero accumulated value at floating-point precision: " + label + ".");
                return result == 0d ? 0d : result;
            }

            private static bool IsStrictlyBelowHalfUlp(double current, double compensation)
            {
                if (current <= 0d || compensation == 0d) return false;
                var currentBits = BitConverter.DoubleToInt64Bits(current);
                var adjacentBits = compensation > 0d ? currentBits + 1L : currentBits - 1L;
                var adjacent = BitConverter.Int64BitsToDouble(adjacentBits);
                var spacing = Math.Abs(adjacent - current);
                return Math.Abs(compensation) < spacing / 2d;
            }
        }

        private static UsageMetrics MetricsForMainMaterial(ProjectElement element, ProjectFamily? family)
        {
            var metrics = new UsageMetrics
            {
                LengthM = Q(element, "LengthM"),
                VolumeM3 = QFirst(element, "NetVolumeM3", "VolumeM3"),
                MassKg = EffectiveMass(element, family)
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

        private static double EffectiveMass(ProjectElement element, ProjectFamily? family)
        {
            var explicitMass = OptionalNonNegativeQuantity(element, "WeightKg", "MassKg");
            if (explicitMass.HasValue) return explicitMass.Value;

            var densityKgM3 = EffectiveDensity(element, family);
            if (!densityKgM3.HasValue) return 0d;

            var volume = OptionalNonNegativeQuantity(
                element,
                "NetConcreteM3",
                "NetVolumeM3",
                "GrossConcreteM3",
                "GrossVolumeM3",
                "VolumeM3",
                "MeasuredVolumeM3");
            if (!volume.HasValue) return 0d;

            var mass = checked(volume.Value * densityKgM3.Value);
            if (double.IsNaN(mass) || double.IsInfinity(mass))
                throw new OverflowException("Material usage mass overflow: " + element.Id + "/volume*density.");
            if (mass == 0d && volume.Value > 0d && densityKgM3.Value > 0d)
                throw new InvalidOperationException("Material usage mass underflow: " + element.Id + "/volume*density rounded positive finite inputs to zero.");
            if (volume.Value != 0d && densityKgM3.Value != 0d)
            {
                if (densityKgM3.Value != 1d && mass == volume.Value)
                    throw new InvalidOperationException("Material usage mass lost the density contribution at double precision: " + element.Id + "/volume*density.");
                if (volume.Value != 1d && mass == densityKgM3.Value)
                    throw new InvalidOperationException("Material usage mass lost the volume contribution at double precision: " + element.Id + "/volume*density.");
            }
            return mass;
        }

        private static double? EffectiveDensity(ProjectElement element, ProjectFamily? family)
        {
            if (element.Properties.TryGetValue("DensityKgM3", out var instance) && !string.IsNullOrWhiteSpace(instance))
                return PositiveInvariant(instance, element.Id + "/DensityKgM3");
            if (family != null && family.Properties.TryGetValue("DensityKgM3", out var inherited) && !string.IsNullOrWhiteSpace(inherited))
                return PositiveInvariant(inherited, "Family " + family.Id + "/DensityKgM3");
            return null;
        }

        private static double PositiveInvariant(string value, string label)
        {
            if (!double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ||
                double.IsNaN(parsed) || double.IsInfinity(parsed) || parsed <= 0d)
                throw new InvalidOperationException(label + " must be an invariant finite number greater than zero.");
            return parsed;
        }

        private static double? OptionalNonNegativeQuantity(ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!element.Quantities.ContainsKey(key)) continue;
                return Q(element, key);
            }
            return null;
        }

        private static void Add(
            ProjectState project,
            ProjectElement element,
            ProjectFamily? family,
            IDictionary<string, string> floors,
            IDictionary<string, string> units,
            IDictionary<string, UsageGroup> rows,
            IList<string> order,
            string material,
            string component,
            UsageMetrics metrics)
        {
            var floorId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FloorId);
            var familyId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FamilyId);
            var floor = floors.TryGetValue(floorId, out var floorName) ? floorName : floorId;
            var familyName = family?.Name ?? familyId;
            var category = element.Category.ToString();
            var key = GroupKey(floorId, material, component, category, familyId);
            if (!rows.TryGetValue(key, out var group))
            {
                var row = new MaterialUsageRow
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
                group = new UsageGroup(row);
                rows[key] = group;
                order.Add(key);
            }
            group.Row.ElementCount = QuantityReportMath.AddCount(group.Row.ElementCount, 1);
            group.Add(metrics, element.Id);
            group.Row.ElementIds.Add(element.Id);
            ReportingRowProvenance.AppendSourceHandles(group.Row.SourceHandles, element.SourceHandles);
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
