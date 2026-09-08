using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    public sealed class RoomFinishScheduleRow
    {
        public string ProjectId { get; set; } = string.Empty;
        public string DrawingFingerprint { get; set; } = string.Empty;
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
        public IList<string> SourceHandles { get; } = new List<string>();
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
            var snapshot = CaptureProjectRevision(project);
            var rows = new Dictionary<string, RoomFinishScheduleRow>(StringComparer.OrdinalIgnoreCase);
            var aggregations = new Dictionary<string, FinishAggregationState>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var item in snapshot.WorkItems)
            {
                var roomKey = item.RoomId.Length > 0 ? item.RoomId : "(unlinked)";
                var key = GroupKey(item.FloorId, roomKey, item.Category, item.FamilyId, item.Material, item.UnitHint);
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new RoomFinishScheduleRow
                    {
                        ProjectId = snapshot.ProjectId,
                        DrawingFingerprint = snapshot.DrawingFingerprint,
                        Floor = item.Floor,
                        Room = item.Room,
                        Category = item.Category,
                        FamilyName = item.FamilyName,
                        Material = item.Material,
                        UnitHint = item.UnitHint
                    };
                    rows[key] = row;
                    aggregations[key] = new FinishAggregationState();
                    order.Add(key);
                }

                var aggregation = aggregations[key];
                row.Count = checked(row.Count + 1);
                aggregation.LengthM.Add(item.LengthM, item.ElementId + "/finish length");
                aggregation.AreaM2.Add(item.AreaM2, item.ElementId + "/finish area");
                aggregation.PrimaryQuantity.Add(item.PrimaryQuantity, item.ElementId + "/finish primary quantity");
                row.ElementIds.Add(item.ElementId);
                ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, item.SourceHandles);
                if (item.RoomId.Length > 0 && !row.RoomIds.Contains(item.RoomId, StringComparer.OrdinalIgnoreCase)) row.RoomIds.Add(item.RoomId);
                EnsureProjectRevision(project, snapshot);
            }

            EnsureProjectRevision(project, snapshot);
            foreach (var key in order)
            {
                var row = rows[key];
                var aggregation = aggregations[key];
                row.LengthM = aggregation.LengthM.Value("room finish/LengthM");
                row.AreaM2 = aggregation.AreaM2.Value("room finish/AreaM2");
                row.PrimaryQuantity = aggregation.PrimaryQuantity.Value("room finish/PrimaryQuantity");
            }
            EnsureProjectRevision(project, snapshot);
            return order.Select(x => rows[x]).ToList().AsReadOnly();
        }

        private static RoomFinishScheduleSnapshot CaptureProjectRevision(ProjectState project)
        {
            var sourceElements = project.Elements.ToList().AsReadOnly();
            var sourceFloors = project.Floors.ToList().AsReadOnly();
            var sourceFamilies = project.Families.ToList().AsReadOnly();
            var elementSnapshots = sourceElements.Select(RoomFinishElementSnapshot.Capture).ToList().AsReadOnly();
            var floorSnapshots = sourceFloors.Select(RoomFinishFloorSnapshot.Capture).ToList().AsReadOnly();
            var familySnapshots = sourceFamilies.Select(RoomFinishFamilySnapshot.Capture).ToList().AsReadOnly();
            var materialUnits = CaptureMaterialUnits(project);
            var floors = sourceFloors.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = sourceFamilies.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var rooms = sourceElements.Where(x => x.Category == ElementCategory.Room).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var units = materialUnits.ToDictionary(x => x.Name, x => x.Unit, StringComparer.OrdinalIgnoreCase);
            var workItems = new List<RoomFinishWorkItemSnapshot>();

            foreach (var element in sourceElements.Where(x => FinishCategories.Contains(x.Category)).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (AutoRoomLifecycle.IsExcludedFromQuantity(project, element)) continue;
                var floorId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FloorId);
                var familyId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FamilyId);
                families.TryGetValue(familyId, out var family);
                if (family != null && family.Category != element.Category)
                    throw new InvalidOperationException("Room finish schedule element " + element.Id + " category " + element.Category + " does not match Family " + family.Id + " category " + family.Category + ". Repair the Family relation before reporting.");
                var material = Effective(element, family, "Material");
                var roomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, element);
                var roomLabel = RoomLabel(roomId, rooms);
                var floor = floors.TryGetValue(floorId, out var floorName) ? floorName : floorId;
                var familyName = family?.Name ?? familyId;
                var metrics = Metrics(element);
                var unitHint = metrics.DefaultUnit;
                if (material.Length > 0 && units.TryGetValue(material, out var unit) && SameDimension(unit, metrics.DefaultUnit)) unitHint = unit;
                var primary = Primary(unitHint, metrics.LengthM, metrics.AreaM2);
                workItems.Add(new RoomFinishWorkItemSnapshot(element.Id, floorId, floor, roomId, roomLabel, element.Category.ToString(), familyId, familyName, material, unitHint, metrics.LengthM, metrics.AreaM2, primary, element.SourceHandles.ToList().AsReadOnly()));
            }

            return new RoomFinishScheduleSnapshot(project.ChangeVersion, project.ProjectId, project.DrawingFingerprint, sourceElements, sourceFloors, sourceFamilies, elementSnapshots, floorSnapshots, familySnapshots, materialUnits, workItems.AsReadOnly());
        }

        private static void EnsureProjectRevision(ProjectState project, RoomFinishScheduleSnapshot snapshot)
        {
            if (project.ChangeVersion != snapshot.Version ||
                !string.Equals(project.ProjectId, snapshot.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(project.DrawingFingerprint, snapshot.DrawingFingerprint, StringComparison.Ordinal) ||
                !SameInstances(project.Elements, snapshot.SourceElements) ||
                !SameInstances(project.Floors, snapshot.SourceFloors) ||
                !SameInstances(project.Families, snapshot.SourceFamilies) ||
                !SameElements(project.Elements, snapshot.Elements) ||
                !SameFloors(project.Floors, snapshot.Floors) ||
                !SameFamilies(project.Families, snapshot.Families) ||
                !SameMaterialUnits(project, snapshot.MaterialUnits))
                throw new InvalidOperationException("Project changed while the room finish schedule was being built; recompute the schedule against the current project state.");
        }

        private static bool SameInstances<T>(IList<T> current, IReadOnlyList<T> source) where T : class
        {
            if (current.Count != source.Count) return false;
            for (var index = 0; index < current.Count; index++)
                if (!ReferenceEquals(current[index], source[index])) return false;
            return true;
        }

        private static bool SameElements(IList<ProjectElement> current, IReadOnlyList<RoomFinishElementSnapshot> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++) if (!snapshot[index].Matches(current[index])) return false;
            return true;
        }

        private static bool SameFloors(IList<FloorDefinition> current, IReadOnlyList<RoomFinishFloorSnapshot> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++) if (!snapshot[index].Matches(current[index])) return false;
            return true;
        }

        private static bool SameFamilies(IList<ProjectFamily> current, IReadOnlyList<RoomFinishFamilySnapshot> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++) if (!snapshot[index].Matches(current[index])) return false;
            return true;
        }

        private static IReadOnlyList<MaterialUnitSnapshot> CaptureMaterialUnits(ProjectState project)
        {
            return ProjectMaterialCatalog.GetAll(project).Select(x => new MaterialUnitSnapshot(x.Name, x.Unit)).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Unit, StringComparer.Ordinal).ToList().AsReadOnly();
        }

        private static bool SameMaterialUnits(ProjectState project, IReadOnlyList<MaterialUnitSnapshot> snapshot)
        {
            var current = CaptureMaterialUnits(project);
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++) if (!current[index].Matches(snapshot[index])) return false;
            return true;
        }

        private static Dictionary<string, string> CaptureProperties(IDictionary<string, string> source)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source) result[item.Key] = item.Value ?? string.Empty;
            return result;
        }

        private static Dictionary<string, double> CaptureQuantities(IDictionary<string, double> source)
        {
            var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source) result[item.Key] = item.Value;
            return result;
        }

        private static bool SameProperties(IDictionary<string, string> current, IReadOnlyDictionary<string, string> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            foreach (var item in snapshot)
                if (!current.TryGetValue(item.Key, out var value) || !string.Equals(value ?? string.Empty, item.Value, StringComparison.Ordinal)) return false;
            return true;
        }

        private static bool SameQuantities(IDictionary<string, double> current, IReadOnlyDictionary<string, double> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            foreach (var item in snapshot)
                if (!current.TryGetValue(item.Key, out var value) || !value.Equals(item.Value)) return false;
            return true;
        }

        private static bool SameStrings(IList<string> current, IReadOnlyList<string> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++) if (!string.Equals(current[index], snapshot[index], StringComparison.Ordinal)) return false;
            return true;
        }

        private static string GroupKey(params string[] tokens)
        {
            var key = new StringBuilder();
            foreach (var raw in tokens)
            {
                var token = raw ?? string.Empty;
                key.Append(token.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(token);
            }
            return key.ToString();
        }

        private sealed class FinishAggregationState
        {
            internal CompensatedTotal LengthM { get; } = new CompensatedTotal();
            internal CompensatedTotal AreaM2 { get; } = new CompensatedTotal();
            internal CompensatedTotal PrimaryQuantity { get; } = new CompensatedTotal();
        }

        private sealed class CompensatedTotal
        {
            private double _sum;
            private double _compensation;

            internal void Add(double value, string label)
            {
                var incoming = QuantityReportMath.NonNegative(value, label);
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var nextSum = _sum + incoming;
                if (double.IsNaN(nextSum) || double.IsInfinity(nextSum)) throw new OverflowException("Room finish schedule total overflow: " + label);
                var correction = Math.Abs(_sum) >= Math.Abs(incoming) ? (_sum - nextSum) + incoming : (incoming - nextSum) + _sum;
                var nextCompensation = _compensation + correction;
                if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation)) throw new OverflowException("Room finish schedule compensation overflow: " + label);
                _sum = nextSum == 0d ? 0d : nextSum;
                _compensation = nextCompensation == 0d ? 0d : nextCompensation;
            }

            internal double Value(string label)
            {
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var result = _sum + _compensation;
                if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Room finish schedule total overflow: " + label);
                if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation)) throw new OverflowException("Room finish schedule total lost a non-zero compensation at floating-point precision: " + label);
                if (_sum != 0d && result == _compensation) throw new OverflowException("Room finish schedule total lost a non-zero accumulated value at floating-point precision: " + label);
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
                    return new FinishMetrics { LengthM = FirstQuantity(element, "SkirtingLengthM", "InnerPerimeterM", "PerimeterM", "LengthM"), AreaM2 = FirstQuantity(element, "AreaM2"), DefaultUnit = "m" };
                case ElementCategory.WallFinish:
                    return new FinishMetrics { AreaM2 = FirstQuantity(element, "NetFinishAreaM2", "SideAreaM2", "AreaM2"), DefaultUnit = "m²" };
                case ElementCategory.CeilingFinish:
                    return new FinishMetrics { AreaM2 = FirstQuantity(element, "TopAreaM2", "AreaM2"), DefaultUnit = "m²" };
                case ElementCategory.FloorFinish:
                case ElementCategory.Waterproofing:
                    return new FinishMetrics { AreaM2 = FirstQuantity(element, "BottomAreaM2", "AreaM2"), DefaultUnit = "m²" };
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

        private static string NormalizeUnit(string unit) => (unit ?? string.Empty).Trim().ToLowerInvariant().Replace("²", "2").Replace("^", string.Empty).Replace(" ", string.Empty);

        private static double FirstQuantity(ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!element.Quantities.TryGetValue(key, out var value)) continue;
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new InvalidOperationException(element.Id + "/" + key + " must be finite and non-negative.");
                return value;
            }
            return 0d;
        }

        private sealed class RoomFinishScheduleSnapshot
        {
            internal RoomFinishScheduleSnapshot(long version, string projectId, string drawingFingerprint, IReadOnlyList<ProjectElement> sourceElements, IReadOnlyList<FloorDefinition> sourceFloors, IReadOnlyList<ProjectFamily> sourceFamilies, IReadOnlyList<RoomFinishElementSnapshot> elements, IReadOnlyList<RoomFinishFloorSnapshot> floors, IReadOnlyList<RoomFinishFamilySnapshot> families, IReadOnlyList<MaterialUnitSnapshot> materialUnits, IReadOnlyList<RoomFinishWorkItemSnapshot> workItems)
            { Version = version; ProjectId = projectId; DrawingFingerprint = drawingFingerprint; SourceElements = sourceElements; SourceFloors = sourceFloors; SourceFamilies = sourceFamilies; Elements = elements; Floors = floors; Families = families; MaterialUnits = materialUnits; WorkItems = workItems; }
            internal long Version { get; }
            internal string ProjectId { get; }
            internal string DrawingFingerprint { get; }
            internal IReadOnlyList<ProjectElement> SourceElements { get; }
            internal IReadOnlyList<FloorDefinition> SourceFloors { get; }
            internal IReadOnlyList<ProjectFamily> SourceFamilies { get; }
            internal IReadOnlyList<RoomFinishElementSnapshot> Elements { get; }
            internal IReadOnlyList<RoomFinishFloorSnapshot> Floors { get; }
            internal IReadOnlyList<RoomFinishFamilySnapshot> Families { get; }
            internal IReadOnlyList<MaterialUnitSnapshot> MaterialUnits { get; }
            internal IReadOnlyList<RoomFinishWorkItemSnapshot> WorkItems { get; }
        }

        private sealed class RoomFinishWorkItemSnapshot
        {
            internal RoomFinishWorkItemSnapshot(string elementId, string floorId, string floor, string roomId, string room, string category, string familyId, string familyName, string material, string unitHint, double lengthM, double areaM2, double primaryQuantity, IReadOnlyList<string> sourceHandles)
            { ElementId = elementId; FloorId = floorId; Floor = floor; RoomId = roomId; Room = room; Category = category; FamilyId = familyId; FamilyName = familyName; Material = material; UnitHint = unitHint; LengthM = lengthM; AreaM2 = areaM2; PrimaryQuantity = primaryQuantity; SourceHandles = sourceHandles; }
            internal string ElementId { get; }
            internal string FloorId { get; }
            internal string Floor { get; }
            internal string RoomId { get; }
            internal string Room { get; }
            internal string Category { get; }
            internal string FamilyId { get; }
            internal string FamilyName { get; }
            internal string Material { get; }
            internal string UnitHint { get; }
            internal double LengthM { get; }
            internal double AreaM2 { get; }
            internal double PrimaryQuantity { get; }
            internal IReadOnlyList<string> SourceHandles { get; }
        }

        private sealed class RoomFinishElementSnapshot
        {
            private RoomFinishElementSnapshot(string id, ElementCategory category, string familyId, string floorId, string zoneId, string drawingFingerprint, ElementDirtyFlags dirty, DateTime updatedUtc, IReadOnlyDictionary<string, string> properties, IReadOnlyDictionary<string, double> quantities, IReadOnlyList<string> sourceHandles, IReadOnlyList<string> dependsOn)
            { Id = id; Category = category; FamilyId = familyId; FloorId = floorId; ZoneId = zoneId; DrawingFingerprint = drawingFingerprint; Dirty = dirty; UpdatedUtc = updatedUtc; Properties = properties; Quantities = quantities; SourceHandles = sourceHandles; DependsOn = dependsOn; }
            internal string Id { get; }
            internal ElementCategory Category { get; }
            internal string FamilyId { get; }
            internal string FloorId { get; }
            internal string ZoneId { get; }
            internal string DrawingFingerprint { get; }
            internal ElementDirtyFlags Dirty { get; }
            internal DateTime UpdatedUtc { get; }
            internal IReadOnlyDictionary<string, string> Properties { get; }
            internal IReadOnlyDictionary<string, double> Quantities { get; }
            internal IReadOnlyList<string> SourceHandles { get; }
            internal IReadOnlyList<string> DependsOn { get; }

            internal static RoomFinishElementSnapshot Capture(ProjectElement element) => new RoomFinishElementSnapshot(element.Id, element.Category, ReportingProjectIdentityGuard.NormalizeReferenceId(element.FamilyId), ReportingProjectIdentityGuard.NormalizeReferenceId(element.FloorId), ReportingProjectIdentityGuard.NormalizeReferenceId(element.ZoneId), element.DrawingFingerprint ?? string.Empty, element.Dirty, element.UpdatedUtc, CaptureProperties(element.Properties), CaptureQuantities(element.Quantities), element.SourceHandles.ToList().AsReadOnly(), element.DependsOn.ToList().AsReadOnly());

            internal bool Matches(ProjectElement current) => string.Equals(current.Id, Id, StringComparison.Ordinal) && current.Category == Category && string.Equals(ReportingProjectIdentityGuard.NormalizeReferenceId(current.FamilyId), FamilyId, StringComparison.Ordinal) && string.Equals(ReportingProjectIdentityGuard.NormalizeReferenceId(current.FloorId), FloorId, StringComparison.Ordinal) && string.Equals(ReportingProjectIdentityGuard.NormalizeReferenceId(current.ZoneId), ZoneId, StringComparison.Ordinal) && string.Equals(current.DrawingFingerprint ?? string.Empty, DrawingFingerprint, StringComparison.Ordinal) && current.Dirty == Dirty && current.UpdatedUtc == UpdatedUtc && SameProperties(current.Properties, Properties) && SameQuantities(current.Quantities, Quantities) && SameStrings(current.SourceHandles, SourceHandles) && SameStrings(current.DependsOn, DependsOn);
        }

        private sealed class RoomFinishFloorSnapshot
        {
            private RoomFinishFloorSnapshot(string id, string name, double elevationM) { Id = id; Name = name; ElevationM = elevationM; }
            internal string Id { get; }
            internal string Name { get; }
            internal double ElevationM { get; }
            internal static RoomFinishFloorSnapshot Capture(FloorDefinition floor) => new RoomFinishFloorSnapshot(floor.Id, floor.Name, floor.ElevationM);
            internal bool Matches(FloorDefinition current) => string.Equals(current.Id, Id, StringComparison.Ordinal) && string.Equals(current.Name, Name, StringComparison.Ordinal) && current.ElevationM.Equals(ElevationM);
        }

        private sealed class RoomFinishFamilySnapshot
        {
            private RoomFinishFamilySnapshot(string id, string name, ElementCategory category, IReadOnlyDictionary<string, string> properties) { Id = id; Name = name; Category = category; Properties = properties; }
            internal string Id { get; }
            internal string Name { get; }
            internal ElementCategory Category { get; }
            internal IReadOnlyDictionary<string, string> Properties { get; }
            internal static RoomFinishFamilySnapshot Capture(ProjectFamily family) => new RoomFinishFamilySnapshot(family.Id, family.Name, family.Category, CaptureProperties(family.Properties));
            internal bool Matches(ProjectFamily current) => string.Equals(current.Id, Id, StringComparison.Ordinal) && string.Equals(current.Name, Name, StringComparison.Ordinal) && current.Category == Category && SameProperties(current.Properties, Properties);
        }

        private sealed class MaterialUnitSnapshot
        {
            internal MaterialUnitSnapshot(string name, string unit) { Name = name ?? string.Empty; Unit = unit ?? string.Empty; }
            internal string Name { get; }
            internal string Unit { get; }
            internal bool Matches(MaterialUnitSnapshot current) => string.Equals(current.Name, Name, StringComparison.Ordinal) && string.Equals(current.Unit, Unit, StringComparison.Ordinal);
        }
    }
}
