using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Rebar
{
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
    }

    public sealed class RebarScheduleRow
    {
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
            var cuttingLength = FiniteAdd(input.CuttingLengthM, input.LapLengthM, "Rebar cutting length");
            cuttingLength = FiniteAdd(cuttingLength, input.AnchorLengthM, "Rebar cutting length");
            cuttingLength = FiniteAdd(cuttingLength, input.HookAllowanceM, "Rebar cutting length");
            if (cuttingLength <= 0d) throw new InvalidOperationException("Rebar cutting length must be greater than zero.");
            var baseMark = string.IsNullOrWhiteSpace(input.BarMark) ? (string.IsNullOrWhiteSpace(input.ElementId) ? "BAR" : input.ElementId) : input.BarMark.Trim();

            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                var quantity = ResolveQuantity(group, input);
                var mark = groups.Count == 1 ? baseMark : baseMark + "-" + (index + 1).ToString(CultureInfo.InvariantCulture);
                var unitWeight = RebarWeight.KilogramsPerMeter(group.DiameterMm);
                var totalLength = FiniteProduct(cuttingLength, quantity, "Rebar total length");
                var netWeight = RebarWeight.TotalKilograms(group.DiameterMm, totalLength);
                var totalWeight = RebarWeight.TotalKilograms(group.DiameterMm, totalLength, input.WastePercent);
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
                    TotalWeightKg = totalWeight
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
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new ArgumentOutOfRangeException(name);
        }

        private static double FiniteAdd(double left, double right, string label)
        {
            var result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " is not finite.");
            return result;
        }

        private static double FiniteProduct(double left, double right, string label)
        {
            var result = left * right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " is not finite.");
            return result;
        }
    }

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
    }
}
