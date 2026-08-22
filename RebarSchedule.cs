using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Rebar
{
<<<<<<< origin/main
    public sealed class RebarScheduleInput
    {
        public string ElementId { get; set; } = string.Empty;
        public string BarMark { get; set; } = string.Empty;
        public string ShapeCode { get; set; } = "00";
        public string Notation { get; set; } = string.Empty;
        public double CuttingLengthM { get; set; }
        public double DistributionLengthM { get; set; }
        public double LapLengthM { get; set; }
        public double AnchorLengthM { get; set; }
        public double HookAllowanceM { get; set; }
        public double WastePercent { get; set; }
        public int? CountOverride { get; set; }
=======
    public enum RebarShape
    {
        Straight,
        LBar,
        UBar,
        StirrupRect,
        Custom
>>>>>>> origin/agent/full-domain-20260810
    }

    public sealed class RebarScheduleRow
    {
<<<<<<< origin/main
        public string ElementId { get; set; } = string.Empty;
        public string BarMark { get; set; } = string.Empty;
        public string ShapeCode { get; set; } = string.Empty;
        public string Notation { get; set; } = string.Empty;
        public double DiameterMm { get; set; }
        public int Quantity { get; set; }
        public double CuttingLengthM { get; set; }
        public double TotalLengthM { get; set; }
        public double UnitWeightKgM { get; set; }
        public double NetWeightKg { get; set; }
        public double WastePercent { get; set; }
        public double TotalWeightKg { get; set; }
    }

    public static class RebarScheduleBuilder
    {
        public static IReadOnlyList<RebarScheduleRow> Build(IEnumerable<RebarScheduleInput> inputs)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            var rows = new List<RebarScheduleRow>();
            foreach (var input in inputs) Append(input ?? throw new ArgumentException("Rebar schedule input cannot contain null.", nameof(inputs)), rows);
            return rows;
        }

        private static void Append(RebarScheduleInput input, ICollection<RebarScheduleRow> rows)
        {
            if (string.IsNullOrWhiteSpace(input.Notation)) throw new ArgumentException("Rebar notation is required.", nameof(input));
            EnsureFiniteNonNegative(input.CuttingLengthM, nameof(input.CuttingLengthM));
            EnsureFiniteNonNegative(input.DistributionLengthM, nameof(input.DistributionLengthM));
            EnsureFiniteNonNegative(input.LapLengthM, nameof(input.LapLengthM));
            EnsureFiniteNonNegative(input.AnchorLengthM, nameof(input.AnchorLengthM));
            EnsureFiniteNonNegative(input.HookAllowanceM, nameof(input.HookAllowanceM));
            EnsureFiniteNonNegative(input.WastePercent, nameof(input.WastePercent));
            if (input.CountOverride.HasValue && input.CountOverride.Value <= 0) throw new ArgumentOutOfRangeException(nameof(input.CountOverride));

            var groups = RebarNotationParser.Parse(input.Notation);
            if (input.CountOverride.HasValue && groups.Count > 1) throw new InvalidOperationException("CountOverride is ambiguous for compound rebar notation.");
            var cuttingLength = input.CuttingLengthM + input.LapLengthM + input.AnchorLengthM + input.HookAllowanceM;
            if (cuttingLength <= 0d) throw new InvalidOperationException("Rebar cutting length must be greater than zero.");
            var baseMark = string.IsNullOrWhiteSpace(input.BarMark) ? (string.IsNullOrWhiteSpace(input.ElementId) ? "BAR" : input.ElementId) : input.BarMark.Trim();

            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                var quantity = ResolveQuantity(group, input);
                var mark = groups.Count == 1 ? baseMark : baseMark + "-" + (index + 1).ToString(CultureInfo.InvariantCulture);
                var unitWeight = RebarWeight.KilogramsPerMeter(group.DiameterMm);
                var totalLength = cuttingLength * quantity;
                var netWeight = unitWeight * totalLength;
                rows.Add(new RebarScheduleRow
                {
                    ElementId = input.ElementId ?? string.Empty,
                    BarMark = mark,
                    ShapeCode = string.IsNullOrWhiteSpace(input.ShapeCode) ? "00" : input.ShapeCode.Trim(),
                    Notation = group.ToString(),
                    DiameterMm = group.DiameterMm,
                    Quantity = quantity,
                    CuttingLengthM = cuttingLength,
                    TotalLengthM = totalLength,
                    UnitWeightKgM = unitWeight,
                    NetWeightKg = netWeight,
                    WastePercent = input.WastePercent,
                    TotalWeightKg = netWeight * (1d + input.WastePercent / 100d)
                });
            }
        }

        private static int ResolveQuantity(RebarGroup group, RebarScheduleInput input)
        {
            if (input.CountOverride.HasValue) return input.CountOverride.Value;
            if (group.Quantity.HasValue && group.Quantity.Value > 0) return group.Quantity.Value;
            if (group.SpacingMm.HasValue)
            {
                if (group.SpacingMm.Value <= 0d) throw new InvalidOperationException("Rebar spacing must be greater than zero.");
                if (input.DistributionLengthM <= 0d) throw new InvalidOperationException("Rebar distribution length must be greater than zero for spacing notation.");
                return checked((int)Math.Ceiling(input.DistributionLengthM * 1000d / group.SpacingMm.Value) + 1);
            }
            throw new InvalidOperationException("Rebar quantity cannot be inferred. Provide count notation, spacing + distribution length, or CountOverride.");
        }

        private static void EnsureFiniteNonNegative(double value, string name)
=======
        public string Mark { get; set; } = string.Empty;
        public string HostElementId { get; set; } = string.Empty;
        public string FloorId { get; set; } = string.Empty;
        public string ZoneId { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public RebarShape Shape { get; set; }
        public double DiameterMm { get; set; }
        public int Quantity { get; set; }
        public double CutLengthM { get; set; }
        public double TotalLengthM { get; set; }
        public double UnitWeightKgPerM { get; set; }
        public double TotalWeightKg { get; set; }
        public IList<string> ElementIds { get; } = new List<string>();
    }

    public static class RebarLengthCalculator
    {
        public static double FromSegments(IEnumerable<double> segmentsM, double hookLengthM = 0d, int hookCount = 0, double lapLengthM = 0d, int lapCount = 0)
        {
            if (segmentsM == null) throw new ArgumentNullException(nameof(segmentsM));
            if (hookCount < 0 || lapCount < 0) throw new ArgumentOutOfRangeException(nameof(hookCount));
            Positive(hookLengthM, nameof(hookLengthM)); Positive(lapLengthM, nameof(lapLengthM));
            var total = 0d;
            foreach (var segment in segmentsM) { Positive(segment, nameof(segmentsM)); total += segment; }
            return total + hookLengthM * hookCount + lapLengthM * lapCount;
        }

        public static double RectangularStirrup(double widthM, double heightM, double coverM, double hookLengthM)
        {
            Positive(widthM, nameof(widthM)); Positive(heightM, nameof(heightM)); Positive(coverM, nameof(coverM)); Positive(hookLengthM, nameof(hookLengthM));
            var innerWidth = Math.Max(0d, widthM - 2d * coverM);
            var innerHeight = Math.Max(0d, heightM - 2d * coverM);
            return 2d * (innerWidth + innerHeight) + 2d * hookLengthM;
        }

        private static void Positive(double value, string name)
>>>>>>> origin/agent/full-domain-20260810
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new ArgumentOutOfRangeException(name);
        }
    }

<<<<<<< origin/main
    public static class ProjectRebarScheduleBuilder
    {
        public static IReadOnlyList<RebarScheduleRow> Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var inputs = new List<RebarScheduleInput>();
            foreach (var element in project.Elements)
            {
                if (!element.Properties.TryGetValue("RebarNotation", out var notation) || string.IsNullOrWhiteSpace(notation)) continue;
                inputs.Add(new RebarScheduleInput
                {
                    ElementId = element.Id,
                    BarMark = Text(element, "RebarBarMark", element.Id),
                    ShapeCode = Text(element, "RebarShapeCode", "00"),
                    Notation = notation,
                    CuttingLengthM = Number(element, "RebarCuttingLengthM", Number(element, "LengthM", Quantity(element, "LengthM"))),
                    DistributionLengthM = Number(element, "RebarDistributionLengthM"),
                    LapLengthM = Number(element, "RebarLapLengthM"),
                    AnchorLengthM = Number(element, "RebarAnchorLengthM"),
                    HookAllowanceM = Number(element, "RebarHookAllowanceM"),
                    WastePercent = Number(element, "RebarWastePercent"),
                    CountOverride = Integer(element, "RebarCount")
                });
            }
            return RebarScheduleBuilder.Build(inputs);
        }

        private static string Text(ProjectElement element, string key, string fallback) => element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;

        private static double Number(ProjectElement element, string key, double fallback = 0d)
        {
            if (!element.Properties.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value)) return fallback;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || double.IsNaN(result) || double.IsInfinity(result) || result < 0d) throw new FormatException("Invalid rebar numeric property " + key + " on " + element.Id + ": " + value);
            return result;
        }

        private static int? Integer(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value)) return null;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result <= 0) throw new FormatException("Invalid rebar integer property " + key + " on " + element.Id + ": " + value);
            return result;
        }

        private static double Quantity(ProjectElement element, string key) => element.Quantities.TryGetValue(key, out var value) && value >= 0d && !double.IsNaN(value) && !double.IsInfinity(value) ? value : 0d;
=======
    public sealed class RebarScheduleBuilder
    {
        public IReadOnlyList<RebarScheduleRow> Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var raw = project.Elements.Where(x => x.Category == ElementCategory.Rebar).Select(BuildElement).ToList();
            return raw.GroupBy(GroupKey, StringComparer.OrdinalIgnoreCase).Select(group =>
            {
                var first = group.First();
                var row = new RebarScheduleRow
                {
                    Mark = first.Mark, HostElementId = first.HostElementId, FloorId = first.FloorId, ZoneId = first.ZoneId, Grade = first.Grade,
                    Shape = first.Shape, DiameterMm = first.DiameterMm, Quantity = group.Sum(x => x.Quantity), CutLengthM = first.CutLengthM,
                    UnitWeightKgPerM = first.UnitWeightKgPerM
                };
                row.TotalLengthM = row.Quantity * row.CutLengthM;
                row.TotalWeightKg = row.TotalLengthM * row.UnitWeightKgPerM;
                foreach (var id in group.SelectMany(x => x.ElementIds)) row.ElementIds.Add(id);
                return row;
            }).OrderBy(x => x.FloorId, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Mark, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.DiameterMm).ToList();
        }

        public RebarScheduleRow BuildElement(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Category != ElementCategory.Rebar) throw new ArgumentException("Element is not Rebar.", nameof(element));
            var diameter = D(element, "DiameterMm");
            var quantity = I(element, "Quantity");
            var spacing = D(element, "SpacingMm");
            if ((diameter <= 0d || quantity <= 0) && element.Properties.TryGetValue("Notation", out var notation) && !string.IsNullOrWhiteSpace(notation))
            {
                var parsed = RebarNotationParser.Parse(notation);
                if (parsed.Count > 0)
                {
                    var parsedGroup = parsed[0];
                    if (diameter <= 0d) diameter = parsedGroup.DiameterMm;
                    if (quantity <= 0 && parsedGroup.Quantity is int parsedQuantity) quantity = parsedQuantity;
                    if (spacing <= 0d && parsedGroup.SpacingMm is double parsedSpacing) spacing = parsedSpacing;
                }
            }
            if (quantity <= 0 && spacing > 0d)
            {
                var distribution = D(element, "DistributionLengthM");
                if (distribution > 0d) quantity = Math.Max(1, (int)Math.Floor(distribution / (spacing / 1000d)) + 1);
            }
            if (quantity <= 0) quantity = 1;
            if (diameter <= 0d) throw new InvalidOperationException("Rebar DiameterMm is required for " + element.Id + ".");

            var shape = ParseShape(S(element, "Shape"));
            var cutLength = D(element, "CutLengthM");
            if (cutLength <= 0d)
            {
                if (shape == RebarShape.StirrupRect)
                    cutLength = RebarLengthCalculator.RectangularStirrup(D(element, "WidthM"), D(element, "HeightM"), D(element, "CoverM"), D(element, "HookLengthM"));
                else
                {
                    var segments = new[] { D(element, "A_M"), D(element, "B_M"), D(element, "C_M"), D(element, "D_M"), D(element, "E_M"), D(element, "F_M") }.Where(x => x > 0d);
                    cutLength = RebarLengthCalculator.FromSegments(segments, D(element, "HookLengthM"), I(element, "HookCount"), D(element, "LapLengthM"), I(element, "LapCount"));
                }
            }
            if (cutLength <= 0d) throw new InvalidOperationException("Rebar CutLengthM or shape dimensions are required for " + element.Id + ".");
            var unitWeight = RebarWeight.KilogramsPerMeter(diameter);
            var row = new RebarScheduleRow
            {
                Mark = S(element, "Mark", element.Id), HostElementId = S(element, "HostElementId"), FloorId = element.FloorId, ZoneId = element.ZoneId,
                Grade = S(element, "Grade", "CB400-V"), Shape = shape, DiameterMm = diameter, Quantity = quantity, CutLengthM = cutLength,
                TotalLengthM = cutLength * quantity, UnitWeightKgPerM = unitWeight, TotalWeightKg = cutLength * quantity * unitWeight
            };
            row.ElementIds.Add(element.Id);
            return row;
        }

        private static string GroupKey(RebarScheduleRow row) => string.Join("\u001f", row.FloorId, row.ZoneId, row.HostElementId, row.Mark, row.Grade, row.Shape.ToString(), row.DiameterMm.ToString("R", CultureInfo.InvariantCulture), row.CutLengthM.ToString("R", CultureInfo.InvariantCulture));
        private static RebarShape ParseShape(string value) => Enum.TryParse(value, true, out RebarShape result) ? result : RebarShape.Straight;
        private static string S(ProjectElement e, string name, string fallback = "") => e.Properties.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
        private static double D(ProjectElement e, string name) => e.Properties.TryGetValue(name, out var value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0d;
        private static int I(ProjectElement e, string name) => e.Properties.TryGetValue(name, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
>>>>>>> origin/agent/full-domain-20260810
    }
}
