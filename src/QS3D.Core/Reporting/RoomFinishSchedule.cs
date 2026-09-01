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
            var floors = project.Floors.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = project.Families.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var rooms = project.Elements.Where(x => x.Category == ElementCategory.Room).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var units = ProjectMaterialCatalog.GetAll(project)
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First().Unit, StringComparer.OrdinalIgnoreCase);
            var rows = new Dictionary<string, RoomFinishScheduleRow>(StringComparer.OrdinalIgnoreCase);
            var aggregations = new Dictionary<string, FinishAggregationState>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in project.Elements.Where(x => FinishCategories.Contains(x.Category)).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
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

                var roomKey = roomId.Length > 0 ? roomId : "(unlinked)";
                var key = GroupKey(
                    floorId,
                    roomKey,
                    element.Category.ToString(),
                    familyId,
                    material,
                    unitHint);
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new RoomFinishScheduleRow
                    {
                        ProjectId = project.ProjectId,
                        DrawingFingerprint = project.DrawingFingerprint,
                        Floor = floor,
                        Room = roomLabel,
                        Category = element.Category.ToString(),
                        FamilyName = familyName,
                        Material = material,
                        UnitHint = unitHint
                    };
                    rows[key] = row;
                    aggregations[key] = new FinishAggregationState();
                    order.Add(key);
                }

                var aggregation = aggregations[key];
                row.Count = checked(row.Count + 1);
                aggregation.LengthM.Add(metrics.LengthM, element.Id + "/finish length");
                aggregation.AreaM2.Add(metrics.AreaM2, element.Id + "/finish area");
                aggregation.PrimaryQuantity.Add(primary, element.Id + "/finish primary quantity");
                row.ElementIds.Add(element.Id);
                ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);
                if (roomId.Length > 0 && !row.RoomIds.Contains(roomId, StringComparer.OrdinalIgnoreCase)) row.RoomIds.Add(roomId);
            }

            foreach (var key in order)
            {
                var row = rows[key];
                var aggregation = aggregations[key];
                row.LengthM = aggregation.LengthM.Value("room finish/LengthM");
                row.AreaM2 = aggregation.AreaM2.Value("room finish/AreaM2");
                row.PrimaryQuantity = aggregation.PrimaryQuantity.Value("room finish/PrimaryQuantity");
            }

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
                if (double.IsNaN(nextSum) || double.IsInfinity(nextSum))
                    throw new OverflowException("Room finish schedule total overflow: " + label);

                var correction = Math.Abs(_sum) >= Math.Abs(incoming)
                    ? (_sum - nextSum) + incoming
                    : (incoming - nextSum) + _sum;
                var nextCompensation = _compensation + correction;
                if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation))
                    throw new OverflowException("Room finish schedule compensation overflow: " + label);

                _sum = nextSum == 0d ? 0d : nextSum;
                _compensation = nextCompensation == 0d ? 0d : nextCompensation;
            }

            internal double Value(string label)
            {
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var result = _sum + _compensation;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Room finish schedule total overflow: " + label);
                if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation))
                    throw new OverflowException("Room finish schedule total lost a non-zero compensation at floating-point precision: " + label);
                if (_sum != 0d && result == _compensation)
                    throw new OverflowException("Room finish schedule total lost a non-zero accumulated value at floating-point precision: " + label);
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
    }
}