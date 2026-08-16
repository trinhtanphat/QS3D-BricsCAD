using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.Reporting
{
    public static class ProjectQuantityReportBuilder
    {
        public static IReadOnlyList<QuantityReportRow> Group(ProjectState project) => Build(project, null, false);

        public static IReadOnlyList<QuantityReportRow> Group(ProjectState project, IEnumerable<string> elementIds)
        {
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            return Build(project, elementIds, false);
        }

        public static IReadOnlyList<QuantityReportRow> Detail(ProjectState project) => Build(project, null, true);

        public static IReadOnlyList<QuantityReportRow> Detail(ProjectState project, IEnumerable<string> elementIds)
        {
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            return Build(project, elementIds, true);
        }

        private static IReadOnlyList<QuantityReportRow> Build(ProjectState project, IEnumerable<string>? elementIds, bool detail)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ReportingProjectIdentityGuard.RequireUniqueElementIds(project, detail ? "Quantity detail report" : "Quantity report");
            RoomFinishIdentityService.ValidateProject(project);
            var selectedIds = ResolveSelection(project, elementIds);
            var floors = project.Floors.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var zones = project.Zones.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = project.Families.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var rows = new Dictionary<string, QuantityReportRow>(StringComparer.OrdinalIgnoreCase);
            var noteValues = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in project.Elements)
            {
                var elementId = element.Id.Trim();
                if (selectedIds != null && !selectedIds.Contains(elementId)) continue;
                if (AutoRoomLifecycle.IsExcludedFromQuantity(project, element)) continue;
                var floorId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FloorId);
                var zoneId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.ZoneId);
                var familyId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FamilyId);
                var floor = floors.TryGetValue(floorId, out var floorName) ? floorName : floorId;
                var zone = zones.TryGetValue(zoneId, out var zoneName) ? zoneName : zoneId;
                families.TryGetValue(familyId, out var family);
                if (family != null && family.Category != element.Category)
                    throw new InvalidOperationException("Quantity report element " + element.Id + " category " + element.Category + " does not match Family " + family.Id + " category " + family.Category + ". Repair the Family relation before reporting.");
                if (family != null) familyId = family.Id;
                var familyName = family != null ? family.Name : familyId;
                var elementName = FirstInstanceProperty(element, "Name", "TenCauKien");
                if (elementName.Length == 0) elementName = familyName;
                var material = Effective(element, family, "Material");
                var note = Effective(element, family, "Note");
                if (note.Length == 0) note = Effective(element, family, "GhiChu");
                var densityKgM3 = EffectiveDensity(element, family);
                var massKg = EffectiveMass(element, densityKgM3);
                var category = element.Category.ToString();
                var key = detail
                    ? "ELEMENT\u001f" + elementId
                    : CanonicalGroupKey(floorId, zoneId, category, familyId, material, DensityKey(densityKgM3));
                var created = false;
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new QuantityReportRow
                    {
                        Floor = floor,
                        Zone = zone,
                        Category = category,
                        FamilyId = familyId,
                        FamilyName = familyName,
                        ElementName = detail ? elementName : familyName,
                        Material = material,
                        Note = note,
                        DensityKgM3 = densityKgM3,
                        MassKg = massKg,
                        DrawingFingerprint = project.DrawingFingerprint
                    };
                    rows[key] = row;
                    var distinctNotes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (note.Length != 0) distinctNotes.Add(note);
                    noteValues.Add(key, distinctNotes);
                    order.Add(key);
                    created = true;
                }
                else
                {
                    if (note.Length != 0 && noteValues[key].Add(note))
                        row.Note = AppendText(row.Note, note);
                    row.MassKg = AddHomogeneousMass(row.MassKg, massKg, element.Id + "/MassKg");
                }

                row.Count = QuantityReportMath.AddCount(row.Count, 1);
                row.ElementIds.Add(elementId);
                AddHandles(row.SourceHandles, SourceHandleResolver.Resolve(project, new[] { elementId }));
                var gross = QFirst(element, "GrossConcreteM3", "GrossVolumeM3");
                var net = QFirstOrFallback(element, gross, "NetConcreteM3", "NetVolumeM3");
                row.GrossConcreteM3 = QuantityReportMath.Add(row.GrossConcreteM3, gross, element.Id + "/GrossConcreteM3");
                row.NetConcreteM3 = QuantityReportMath.Add(row.NetConcreteM3, net, element.Id + "/NetConcreteM3");
                row.DeductionM3 = QuantityReportMath.Add(row.DeductionM3, Q(element, "DeductionM3", Math.Max(0d, gross - net)), element.Id + "/DeductionM3");
                row.FormworkM2 = QuantityReportMath.Add(row.FormworkM2, Q(element, "FormworkM2"), element.Id + "/FormworkM2");
                row.LengthM = QuantityReportMath.Add(row.LengthM, Q(element, "LengthM"), element.Id + "/LengthM");
                row.OuterPerimeterM = QuantityReportMath.Add(row.OuterPerimeterM,
                    element.Category == ElementCategory.Room ? QFirst(element, "OuterPerimeterM", "PerimeterM") : Q(element, "OuterPerimeterM"),
                    element.Id + "/OuterPerimeterM");
                row.InnerPerimeterM = QuantityReportMath.Add(row.InnerPerimeterM,
                    element.Category == ElementCategory.Skirting ? QFirst(element, "InnerPerimeterM", "PerimeterM") : Q(element, "InnerPerimeterM"),
                    element.Id + "/InnerPerimeterM");
                row.DoorAreaM2 = QuantityReportMath.Add(row.DoorAreaM2,
                    element.Category == ElementCategory.Door || element.Category == ElementCategory.WallOpening ? Q(element, "OpeningAreaM2") : Q(element, "DoorAreaM2"),
                    element.Id + "/DoorAreaM2");
                row.SideAreaM2 = QuantityReportMath.Add(row.SideAreaM2,
                    element.Category == ElementCategory.WallFinish ? QFirst(element, "NetFinishAreaM2", "SideAreaM2") : Q(element, "SideAreaM2"),
                    element.Id + "/SideAreaM2");
                row.BottomAreaM2 = QuantityReportMath.Add(row.BottomAreaM2,
                    element.Category == ElementCategory.FloorFinish || element.Category == ElementCategory.Waterproofing ? QFirst(element, "BottomAreaM2", "AreaM2") : Q(element, "BottomAreaM2"),
                    element.Id + "/BottomAreaM2");
                row.TopAreaM2 = QuantityReportMath.Add(row.TopAreaM2,
                    element.Category == ElementCategory.CeilingFinish ? QFirst(element, "TopAreaM2", "AreaM2") : Q(element, "TopAreaM2"),
                    element.Id + "/TopAreaM2");
                row.OtherAreaM2 = QuantityReportMath.Add(row.OtherAreaM2, QFirst(element, "OtherAreaM2", "MeasuredSurfaceAreaM2"), element.Id + "/OtherAreaM2");
                if (created && row.MassKg.HasValue)
                    row.MassKg = QuantityReportMath.NonNegative(row.MassKg.Value, element.Id + "/MassKg");
            }

            return order.Select(x => rows[x]).ToList().AsReadOnly();
        }

        private static HashSet<string>? ResolveSelection(ProjectState project, IEnumerable<string>? elementIds)
        {
            if (elementIds == null) return null;
            var selectionVersion = project.ChangeVersion;
            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var selectedInstances = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in elementIds)
            {
                var id = (raw ?? string.Empty).Trim();
                if (id.Length == 0) throw new ArgumentException("Quantity report element ids must not be blank.", nameof(elementIds));
                if (!selected.Add(id))
                    throw new ArgumentException("Quantity report element ids must be unique. Duplicate id: " + id + ".", nameof(elementIds));
                var element = project.FindElement(id) ?? throw new KeyNotFoundException("Unknown quantity report element: " + id);
                selectedInstances.Add(id, element);
            }

            if (project.ChangeVersion != selectionVersion)
                throw new InvalidOperationException("Project changed while quantity report element ids were being enumerated; recompute the selection against the current project state.");

            ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Quantity report selection");
            foreach (var selectedInstance in selectedInstances)
            {
                var current = project.FindElement(selectedInstance.Key);
                if (current == null || !ReferenceEquals(current, selectedInstance.Value))
                    throw new InvalidOperationException("Quantity report selection became stale while element ids were being enumerated; recompute the selection against the current project state.");
            }
            return selected;
        }

        private static void AddHandles(IList<string> destination, IEnumerable<string> source)
        {
            foreach (var handle in source.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
                if (!destination.Contains(handle, StringComparer.OrdinalIgnoreCase)) destination.Add(handle);
        }

        private static string FirstInstanceProperty(ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
                if (element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            return string.Empty;
        }

        private static string Effective(ProjectElement element, ProjectFamily? family, string key)
        {
            if (element.Properties.TryGetValue(key, out var instance) && !string.IsNullOrWhiteSpace(instance)) return instance.Trim();
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) return inherited.Trim();
            return string.Empty;
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

        private static double? EffectiveMass(ProjectElement element, double? densityKgM3)
        {
            var explicitMass = OptionalNonNegativeQuantity(element, "WeightKg", "MassKg");
            if (explicitMass.HasValue) return explicitMass;
            if (!densityKgM3.HasValue) return null;

            var volume = OptionalNonNegativeQuantity(
                element,
                "NetConcreteM3",
                "NetVolumeM3",
                "GrossConcreteM3",
                "GrossVolumeM3",
                "VolumeM3",
                "MeasuredVolumeM3");
            if (!volume.HasValue) return null;
            var mass = checked(volume.Value * densityKgM3.Value);
            if (double.IsNaN(mass) || double.IsInfinity(mass))
                throw new OverflowException("Quantity report mass overflow: " + element.Id + "/volume*density.");
            if (mass == 0d && volume.Value > 0d && densityKgM3.Value > 0d)
                throw new InvalidOperationException("Quantity report mass underflow: " + element.Id + "/volume*density rounded positive finite inputs to zero.");
            return mass;
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

        private static string DensityKey(double? densityKgM3) => densityKgM3.HasValue
            ? densityKgM3.Value.ToString("R", CultureInfo.InvariantCulture)
            : "<none>";

        private static string CanonicalGroupKey(params string[] parts)
        {
            return string.Join("|", (parts ?? Array.Empty<string>())
                .Select(part =>
                {
                    var value = part ?? string.Empty;
                    return value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
                }));
        }

        private static double? AddHomogeneousMass(double? current, double? value, string label)
        {
            if (!current.HasValue || !value.HasValue) return null;
            return QuantityReportMath.Add(current.Value, value.Value, label);
        }

        private static string AppendText(string current, string value)
        {
            var existing = (current ?? string.Empty).Trim();
            var incoming = (value ?? string.Empty).Trim();
            if (incoming.Length == 0) return existing;
            if (existing.Length == 0) return incoming;
            return existing + " | " + incoming;
        }

        private static double QFirst(ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
                if (element.Quantities.ContainsKey(key)) return Q(element, key);
            return 0d;
        }

        private static double QFirstOrFallback(ProjectElement element, double fallback, params string[] keys)
        {
            foreach (var key in keys)
                if (element.Quantities.ContainsKey(key)) return Q(element, key);
            return QuantityReportMath.NonNegative(fallback, element.Id + "/fallback");
        }

        private static double Q(ProjectElement element, string name, double fallback = 0d)
        {
            var value = element.Quantities.TryGetValue(name, out var stored) ? stored : fallback;
            return QuantityReportMath.NonNegative(value, element.Id + "/" + name);
        }
    }
}
