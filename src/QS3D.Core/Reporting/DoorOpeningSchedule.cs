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
        private static readonly string[] RelevantPropertyKeys =
        {
            "WidthM", "HeightM", "SillHeightM", "BottomOffsetM", "ThicknessM",
            "Material", "OpeningUsage", "HostWallId"
        };

        public static IReadOnlyList<DoorOpeningScheduleRow> Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Door/opening schedule");
            var snapshot = CaptureProjectRevision(project);
            EnsureProjectRevision(project, snapshot);

            var floors = snapshot.Floors.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = snapshot.Families.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var elementsById = snapshot.Elements.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var rows = new Dictionary<string, DoorOpeningScheduleRow>(StringComparer.OrdinalIgnoreCase);
            var areaAggregations = new Dictionary<string, CompensatedAreaTotal>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in snapshot.Elements
                .Where(x => x.Category == ElementCategory.Door || x.Category == ElementCategory.WallOpening)
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                EnsureProjectRevision(project, snapshot);
                var floorId = element.FloorId;
                var familyId = element.FamilyId;
                families.TryGetValue(familyId, out var family);
                if (family != null && family.Category != element.Category)
                    throw new InvalidOperationException("Door/opening schedule element " + element.Id + " category " + element.Category + " does not match Family " + family.Id + " category " + family.Category + ". Repair the Family relation before reporting.");
                var widthM = Positive(Number(element, family, "WidthM", 0.9d), element.Id + "/WidthM");
                var heightM = Positive(Number(element, family, "HeightM", 2.2d), element.Id + "/HeightM");
                var sillM = NonNegative(Number(element, family, "SillHeightM", Number(element, family, "BottomOffsetM", 0d)), element.Id + "/SillHeightM");
                var thicknessM = NonNegative(Number(element, family, "ThicknessM", 0d), element.Id + "/ThicknessM");
                var material = Text(element, family, "Material");
                var areaM2 = element.HasOpeningAreaM2
                    ? NonNegative(element.OpeningAreaM2, element.Id + "/OpeningAreaM2")
                    : Multiply(widthM, heightM, element.Id + "/OpeningAreaM2");
                var floor = floors.TryGetValue(floorId, out var floorName) ? floorName : floorId;
                var familyName = family?.Name ?? familyId;
                var category = ScheduleCategory(element);
                var hostId = element.Properties.TryGetValue("HostWallId", out var hostRaw)
                    ? CanonicalOptionalHostId(elementsById, hostRaw, element.Id)
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
                        ProjectId = snapshot.ProjectId,
                        DrawingFingerprint = snapshot.DrawingFingerprint,
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
                    areaAggregations[key] = new CompensatedAreaTotal();
                    order.Add(key);
                }
                row.Count = checked(row.Count + 1);
                areaAggregations[key].Add(areaM2, element.Id + "/opening schedule area");
                row.ElementIds.Add(element.Id);
                ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);
                if (hostId.Length > 0 && !row.HostIds.Contains(hostId, StringComparer.OrdinalIgnoreCase)) row.HostIds.Add(hostId);
                EnsureProjectRevision(project, snapshot);
            }

            EnsureProjectRevision(project, snapshot);
            foreach (var key in order)
            {
                var row = rows[key];
                row.OpeningAreaM2 = areaAggregations[key].Value("door/opening schedule/OpeningAreaM2");
                row.HostCount = row.HostIds.Count;
            }
            EnsureProjectRevision(project, snapshot);
            return order.Select(x => rows[x]).ToList().AsReadOnly();
        }

        private static DoorOpeningScheduleSnapshot CaptureProjectRevision(ProjectState project)
        {
            return new DoorOpeningScheduleSnapshot(
                project.ChangeVersion,
                project.ProjectId,
                project.DrawingFingerprint,
                project.Elements.Select(DoorOpeningElementSnapshot.Capture).ToList().AsReadOnly(),
                project.Floors.Select(x => new DoorOpeningFloorSnapshot(x.Id, x.Name)).ToList().AsReadOnly(),
                project.Families.Select(DoorOpeningFamilySnapshot.Capture).ToList().AsReadOnly());
        }

        private static void EnsureProjectRevision(ProjectState project, DoorOpeningScheduleSnapshot snapshot)
        {
            if (project.ChangeVersion != snapshot.Version ||
                !string.Equals(project.ProjectId, snapshot.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(project.DrawingFingerprint, snapshot.DrawingFingerprint, StringComparison.Ordinal) ||
                !SameElements(project.Elements, snapshot.Elements) ||
                !SameFloors(project.Floors, snapshot.Floors) ||
                !SameFamilies(project.Families, snapshot.Families))
                throw new InvalidOperationException("Project changed while the door/opening schedule was being built; recompute the schedule against the current project state.");
        }

        private static bool SameElements(IList<ProjectElement> current, IReadOnlyList<DoorOpeningElementSnapshot> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++)
                if (!snapshot[index].Matches(current[index])) return false;
            return true;
        }

        private static bool SameFloors(IList<FloorDefinition> current, IReadOnlyList<DoorOpeningFloorSnapshot> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++)
                if (!snapshot[index].Matches(current[index])) return false;
            return true;
        }

        private static bool SameFamilies(IList<ProjectFamily> current, IReadOnlyList<DoorOpeningFamilySnapshot> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++)
                if (!snapshot[index].Matches(current[index])) return false;
            return true;
        }

        private static Dictionary<string, string> CaptureProperties(IDictionary<string, string> source)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in RelevantPropertyKeys)
                if (source.TryGetValue(key, out var value)) result[key] = value ?? string.Empty;
            return result;
        }

        private static bool SameProperties(IDictionary<string, string> current, IReadOnlyDictionary<string, string> snapshot)
        {
            var captured = CaptureProperties(current);
            if (captured.Count != snapshot.Count) return false;
            foreach (var item in snapshot)
                if (!captured.TryGetValue(item.Key, out var value) || !string.Equals(value, item.Value, StringComparison.Ordinal)) return false;
            return true;
        }

        private static bool SameSourceHandles(IList<string> current, IReadOnlyList<string> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++)
                if (!string.Equals(current[index], snapshot[index], StringComparison.Ordinal)) return false;
            return true;
        }

        private sealed class CompensatedAreaTotal
        {
            private double _sum;
            private double _compensation;

            internal void Add(double value, string label)
            {
                var incoming = QuantityReportMath.NonNegative(value, label);
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var nextSum = _sum + incoming;
                if (double.IsNaN(nextSum) || double.IsInfinity(nextSum)) throw new OverflowException("Door/opening schedule area total overflow: " + label);
                var correction = Math.Abs(_sum) >= Math.Abs(incoming) ? (_sum - nextSum) + incoming : (incoming - nextSum) + _sum;
                var nextCompensation = _compensation + correction;
                if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation)) throw new OverflowException("Door/opening schedule area compensation overflow: " + label);
                _sum = nextSum == 0d ? 0d : nextSum;
                _compensation = nextCompensation == 0d ? 0d : nextCompensation;
            }

            internal double Value(string label)
            {
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var result = _sum + _compensation;
                if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Door/opening schedule area total overflow: " + label);
                if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation)) throw new OverflowException("Door/opening schedule area total lost a non-zero compensation at floating-point precision: " + label);
                if (_sum != 0d && result == _compensation) throw new OverflowException("Door/opening schedule area total lost a non-zero accumulated value at floating-point precision: " + label);
                return result == 0d ? 0d : result;
            }

            private static bool IsStrictlyBelowHalfUlp(double current, double compensation)
            {
                if (current <= 0d || compensation == 0d) return false;
                var currentBits = BitConverter.DoubleToInt64Bits(current);
                var adjacentBits = compensation > 0d ? currentBits + 1L : currentBits - 1L;
                var adjacent = BitConverter.Int64BitsToDouble(adjacentBits);
                return Math.Abs(compensation) < Math.Abs(adjacent - current) / 2d;
            }
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

        private static string CanonicalNumber(double value) => (value == 0d ? 0d : value).ToString("R", CultureInfo.InvariantCulture);

        private static string ScheduleCategory(DoorOpeningElementSnapshot element)
        {
            if (element.Category != ElementCategory.WallOpening) return element.Category.ToString();
            if (!element.Properties.TryGetValue("OpeningUsage", out var raw) || string.IsNullOrWhiteSpace(raw)) return ElementCategory.WallOpening.ToString();
            return string.Equals(raw.Trim(), "Window", StringComparison.OrdinalIgnoreCase) ? "Window" : ElementCategory.WallOpening.ToString();
        }

        private static string CanonicalOptionalHostId(IReadOnlyDictionary<string, DoorOpeningElementSnapshot> elements, string? value, string elementId)
        {
            var raw = value ?? string.Empty;
            if (raw.Length == 0) return string.Empty;
            if (string.IsNullOrWhiteSpace(raw) || !string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Door/opening schedule requires canonical HostWallId without surrounding whitespace on element: " + elementId + ".");
            if (!elements.TryGetValue(raw, out var host))
                throw new InvalidOperationException("Door/opening schedule cannot report missing HostWallId target '" + raw + "' on element: " + elementId + ".");
            if (!IsWall(host.Category))
                throw new InvalidOperationException("Door/opening schedule requires HostWallId target '" + raw + "' to be a wall for element: " + elementId + ".");
            return raw;
        }

        private static bool IsWall(ElementCategory category) => category == ElementCategory.ArchitecturalWall || category == ElementCategory.GlassWall || category == ElementCategory.WallPier || category == ElementCategory.StructuralWall;

        private static double Number(DoorOpeningElementSnapshot element, DoorOpeningFamilySnapshot? family, string key, double fallback)
        {
            if (element.Properties.TryGetValue(key, out var instanceRaw) && !string.IsNullOrWhiteSpace(instanceRaw)) return Parse(instanceRaw, element.Id + "/" + key);
            if (family != null && family.Properties.TryGetValue(key, out var familyRaw) && !string.IsNullOrWhiteSpace(familyRaw)) return Parse(familyRaw, family.Id + "/" + key);
            return fallback;
        }

        private static string Text(DoorOpeningElementSnapshot element, DoorOpeningFamilySnapshot? family, string key)
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

        private static double Positive(double value, string label) { if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d) throw new InvalidOperationException(label + " must be finite and > 0."); return value; }
        private static double NonNegative(double value, string label) { if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new InvalidOperationException(label + " must be finite and >= 0."); return value; }
        private static double Multiply(double left, double right, string label)
        {
            var value = left * right;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " overflowed.");
            if (value == 0d && left > 0d && right > 0d) throw new InvalidOperationException(label + " underflowed: positive finite dimensions rounded to zero area.");
            return value;
        }

        private sealed class DoorOpeningScheduleSnapshot
        {
            internal DoorOpeningScheduleSnapshot(long version, string projectId, string drawingFingerprint, IReadOnlyList<DoorOpeningElementSnapshot> elements, IReadOnlyList<DoorOpeningFloorSnapshot> floors, IReadOnlyList<DoorOpeningFamilySnapshot> families)
            { Version = version; ProjectId = projectId; DrawingFingerprint = drawingFingerprint; Elements = elements; Floors = floors; Families = families; }
            internal long Version { get; }
            internal string ProjectId { get; }
            internal string DrawingFingerprint { get; }
            internal IReadOnlyList<DoorOpeningElementSnapshot> Elements { get; }
            internal IReadOnlyList<DoorOpeningFloorSnapshot> Floors { get; }
            internal IReadOnlyList<DoorOpeningFamilySnapshot> Families { get; }
        }

        private sealed class DoorOpeningFloorSnapshot
        {
            internal DoorOpeningFloorSnapshot(string id, string name) { Id = id; Name = name; }
            internal string Id { get; }
            internal string Name { get; }
            internal bool Matches(FloorDefinition current) => string.Equals(current.Id, Id, StringComparison.Ordinal) && string.Equals(current.Name, Name, StringComparison.Ordinal);
        }

        private sealed class DoorOpeningFamilySnapshot
        {
            private DoorOpeningFamilySnapshot(string id, string name, ElementCategory category, IReadOnlyDictionary<string, string> properties)
            { Id = id; Name = name; Category = category; Properties = properties; }
            internal string Id { get; }
            internal string Name { get; }
            internal ElementCategory Category { get; }
            internal IReadOnlyDictionary<string, string> Properties { get; }
            internal static DoorOpeningFamilySnapshot Capture(ProjectFamily family) => new DoorOpeningFamilySnapshot(family.Id, family.Name, family.Category, CaptureProperties(family.Properties));
            internal bool Matches(ProjectFamily current) => string.Equals(current.Id, Id, StringComparison.Ordinal) && string.Equals(current.Name, Name, StringComparison.Ordinal) && current.Category == Category && SameProperties(current.Properties, Properties);
        }

        private sealed class DoorOpeningElementSnapshot
        {
            private DoorOpeningElementSnapshot(string id, ElementCategory category, string familyId, string floorId, DateTime updatedUtc, IReadOnlyDictionary<string, string> properties, bool hasOpeningAreaM2, double openingAreaM2, IReadOnlyList<string> sourceHandles)
            { Id = id; Category = category; FamilyId = familyId; FloorId = floorId; UpdatedUtc = updatedUtc; Properties = properties; HasOpeningAreaM2 = hasOpeningAreaM2; OpeningAreaM2 = openingAreaM2; SourceHandles = sourceHandles; }
            internal string Id { get; }
            internal ElementCategory Category { get; }
            internal string FamilyId { get; }
            internal string FloorId { get; }
            internal DateTime UpdatedUtc { get; }
            internal IReadOnlyDictionary<string, string> Properties { get; }
            internal bool HasOpeningAreaM2 { get; }
            internal double OpeningAreaM2 { get; }
            internal IReadOnlyList<string> SourceHandles { get; }

            internal static DoorOpeningElementSnapshot Capture(ProjectElement element)
            {
                var hasArea = element.Quantities.TryGetValue("OpeningAreaM2", out var area);
                if (hasArea) NonNegative(area, element.Id + "/OpeningAreaM2");
                return new DoorOpeningElementSnapshot(
                    element.Id,
                    element.Category,
                    ReportingProjectIdentityGuard.NormalizeReferenceId(element.FamilyId),
                    ReportingProjectIdentityGuard.NormalizeReferenceId(element.FloorId),
                    element.UpdatedUtc,
                    CaptureProperties(element.Properties),
                    hasArea,
                    hasArea ? area : 0d,
                    element.SourceHandles.ToList().AsReadOnly());
            }

            internal bool Matches(ProjectElement current)
            {
                var hasArea = current.Quantities.TryGetValue("OpeningAreaM2", out var area);
                return string.Equals(current.Id, Id, StringComparison.Ordinal) &&
                    current.Category == Category &&
                    string.Equals(ReportingProjectIdentityGuard.NormalizeReferenceId(current.FamilyId), FamilyId, StringComparison.Ordinal) &&
                    string.Equals(ReportingProjectIdentityGuard.NormalizeReferenceId(current.FloorId), FloorId, StringComparison.Ordinal) &&
                    current.UpdatedUtc == UpdatedUtc &&
                    SameProperties(current.Properties, Properties) &&
                    hasArea == HasOpeningAreaM2 &&
                    (!hasArea || area.Equals(OpeningAreaM2)) &&
                    SameSourceHandles(current.SourceHandles, SourceHandles);
            }
        }
    }
}
