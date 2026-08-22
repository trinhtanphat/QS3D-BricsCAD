using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Rebar
{
    public sealed class RebarScheduleInput
    {
        public string ElementId { get; set; } = string.Empty; public string BarMark { get; set; } = string.Empty; public string ShapeCode { get; set; } = "00"; public string Notation { get; set; } = string.Empty; public double CuttingLengthM { get; set; } public double DistributionLengthM { get; set; } public double LapLengthM { get; set; } public double AnchorLengthM { get; set; } public double HookAllowanceM { get; set; } public double WastePercent { get; set; } public int? CountOverride { get; set; }
    }
    public sealed class RebarScheduleRow
    {
        public string ElementId { get; set; } = string.Empty; public string BarMark { get; set; } = string.Empty; public string ShapeCode { get; set; } = string.Empty; public string Notation { get; set; } = string.Empty; public double DiameterMm { get; set; } public int Quantity { get; set; } public double CuttingLengthM { get; set; } public double TotalLengthM { get; set; } public double UnitWeightKgM { get; set; } public double NetWeightKg { get; set; } public double WastePercent { get; set; } public double TotalWeightKg { get; set; }
    }
    public static class RebarScheduleBuilder
    {
        public static IReadOnlyList<RebarScheduleRow> Build(IEnumerable<RebarScheduleInput> inputs)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs)); var result = new List<RebarScheduleRow>(); foreach (var input in inputs) BuildInput(input, result); return result.OrderBy(x => x.BarMark, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.DiameterMm).ToList();
        }
        private static void BuildInput(RebarScheduleInput input, ICollection<RebarScheduleRow> rows)
        {
            if (input == null) return; if (string.IsNullOrWhiteSpace(input.Notation)) return; if (input.CuttingLengthM < 0d || input.DistributionLengthM < 0d || input.LapLengthM < 0d || input.AnchorLengthM < 0d || input.HookAllowanceM < 0d || input.WastePercent < 0d) throw new ArgumentOutOfRangeException(nameof(input), "Rebar dimensions/waste must be non-negative.");
            var groups = RebarNotationParser.Parse(input.Notation); if (groups.Count == 0) return;
            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index]; var quantity = Quantity(group, input); var cuttingLength = input.CuttingLengthM + input.LapLengthM + input.AnchorLengthM + input.HookAllowanceM; if (quantity > 0 && cuttingLength <= 0d) throw new InvalidOperationException("CuttingLengthM must be positive for quantified bars."); var totalLength = quantity * cuttingLength; var unitWeight = RebarWeight.KilogramsPerMeter(group.DiameterMm); var netWeight = totalLength * unitWeight; var waste = input.WastePercent;
                rows.Add(new RebarScheduleRow { ElementId = input.ElementId ?? string.Empty, BarMark = groups.Count == 1 ? (input.BarMark ?? string.Empty) : (input.BarMark ?? string.Empty) + "-" + (index + 1).ToString(CultureInfo.InvariantCulture), ShapeCode = input.ShapeCode ?? string.Empty, Notation = group.ToString(), DiameterMm = group.DiameterMm, Quantity = quantity, CuttingLengthM = cuttingLength, TotalLengthM = totalLength, UnitWeightKgM = unitWeight, NetWeightKg = netWeight, WastePercent = waste, TotalWeightKg = netWeight * (1d + waste / 100d) });
            }
        }
        private static int Quantity(RebarGroup group, RebarScheduleInput input)
        {
<<<<<<< origin/main
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
=======
            if (input.CountOverride.HasValue) { if (input.CountOverride.Value < 0) throw new ArgumentOutOfRangeException(nameof(input.CountOverride)); return input.CountOverride.Value; }
            if (group.Quantity.HasValue) return group.Quantity.Value; if (group.SpacingMm.HasValue) { if (input.DistributionLengthM <= 0d) throw new InvalidOperationException("DistributionLengthM is required for spacing notation."); var spacingM = group.SpacingMm.Value / 1000d; if (spacingM <= 0d) throw new InvalidOperationException("Spacing must be positive."); return Math.Max(1, (int)Math.Floor(input.DistributionLengthM / spacingM + 1e-12) + 1); } throw new InvalidOperationException("Rebar quantity could not be determined.");
>>>>>>> origin/ci/full-domain-integration-final-20260810
        }
    }
    public static class ProjectRebarScheduleBuilder
    {
        public static IReadOnlyList<RebarScheduleRow> Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project)); var rows = new List<RebarScheduleRow>(); foreach (var element in project.Elements) rows.AddRange(BuildElement(element)); return rows.OrderBy(x => x.BarMark, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.DiameterMm).ToList();
        }
        public static IReadOnlyList<RebarScheduleRow> BuildElement(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element)); if (!element.Properties.TryGetValue("RebarNotation", out var notation) || string.IsNullOrWhiteSpace(notation)) return Array.Empty<RebarScheduleRow>();
            var cuttingLength = Number(element, "RebarCuttingLengthM", Number(element, "LengthM", Quantity(element, "LengthM"))); var distributionLength = Number(element, "RebarDistributionLengthM", Number(element, "WidthM", Number(element, "LengthM", 0d))); var input = new RebarScheduleInput { ElementId = element.Id, BarMark = Text(element, "RebarBarMark", element.Id), ShapeCode = Text(element, "RebarShapeCode", "00"), Notation = notation, CuttingLengthM = cuttingLength, DistributionLengthM = distributionLength, LapLengthM = Number(element, "RebarLapLengthM", 0d), AnchorLengthM = Number(element, "RebarAnchorLengthM", 0d), HookAllowanceM = Number(element, "RebarHookAllowanceM", 0d), WastePercent = Number(element, "RebarWastePercent", 0d), CountOverride = NullableInt(element, "RebarCount") }; return RebarScheduleBuilder.Build(new[] { input });
        }
        public static bool TryBuildElement(ProjectElement element, out IReadOnlyList<RebarScheduleRow> rows, out string error)
        {
            try { rows = BuildElement(element); error = string.Empty; return true; } catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is InvalidOperationException || ex is OverflowException) { rows = Array.Empty<RebarScheduleRow>(); error = ex.Message; return false; }
        }
        private static string Text(ProjectElement element, string key, string fallback) => element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
<<<<<<< origin/main

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
        private static double Number(ProjectElement element, string key, double fallback) => element.Properties.TryGetValue(key, out var value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : fallback;
        private static double Quantity(ProjectElement element, string key) => element.Quantities.TryGetValue(key, out var value) ? value : 0d;
        private static int? NullableInt(ProjectElement element, string key) => element.Properties.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : (int?)null;
>>>>>>> origin/ci/full-domain-integration-final-20260810
    }
}
