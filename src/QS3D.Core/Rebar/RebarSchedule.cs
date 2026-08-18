using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Diagnostics;
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
        public string FabricationStatus { get; set; } = string.Empty;
        public string FabricationStandardCode { get; set; } = string.Empty;
        public string FabricationDetailingRevision { get; set; } = string.Empty;
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
        public string FabricationStatus { get; set; } = string.Empty;
        public string FabricationStandardCode { get; set; } = string.Empty;
        public string FabricationDetailingRevision { get; set; } = string.Empty;
    }

    public static class RebarScheduleBuilder
    {
        private const int MaxRowCount = 10000;

        public static IReadOnlyList<RebarScheduleRow> Build(IEnumerable<RebarScheduleInput> inputs)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            var rows = new List<RebarScheduleRow>();
            foreach (var input in inputs) Append(input ?? throw new ArgumentException("Rebar schedule input cannot contain null.", nameof(inputs)), rows, nameof(inputs));
            ValidateAggregate(rows);
            return rows.AsReadOnly();
        }

        private static void Append(RebarScheduleInput input, ICollection<RebarScheduleRow> rows, string inputParameterName)
        {
            var elementId = RequireCanonicalElementId(input.ElementId, nameof(input.ElementId));
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

            var cuttingLengthParts = new CompensatedFiniteSum();
            cuttingLengthParts.Add(input.CuttingLengthM, "cutting length");
            cuttingLengthParts.Add(input.LapLengthM, "cutting + lap length");
            cuttingLengthParts.Add(input.AnchorLengthM, "cutting + anchor length");
            cuttingLengthParts.Add(input.HookAllowanceM, "cutting + hook allowance");
            var cuttingLength = cuttingLengthParts.Value;
            if (cuttingLength <= 0d) throw new InvalidOperationException("Rebar cutting length must be greater than zero.");
            var baseMark = string.IsNullOrWhiteSpace(input.BarMark) ? elementId : input.BarMark.Trim();
            var fabricationStatus = Normalize(input.FabricationStatus);
            var fabricationStandardCode = Normalize(input.FabricationStandardCode);
            var fabricationDetailingRevision = Normalize(input.FabricationDetailingRevision);

            for (var index = 0; index < groups.Count; index++)
            {
                if (rows.Count >= MaxRowCount)
                    throw new ArgumentOutOfRangeException(inputParameterName, "Rebar schedule exceeds the supported row bound of " + MaxRowCount + ".");
                var group = groups[index];
                var quantity = ResolveQuantity(group, input);
                var mark = groups.Count == 1 ? baseMark : baseMark + "-" + (index + 1).ToString(CultureInfo.InvariantCulture);
                var unitWeight = RebarWeight.KilogramsPerMeter(group.DiameterMm);
                var totalLength = RebarMath.Multiply(cuttingLength, quantity, mark + "/total length");
                var netWeight = RebarMath.Multiply(unitWeight, totalLength, mark + "/net weight");
                var totalWeight = RebarWeight.TotalKilograms(group.DiameterMm, totalLength, input.WastePercent);
                rows.Add(new RebarScheduleRow
                {
                    ElementId = elementId,
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
                    TotalWeightKg = totalWeight,
                    FabricationStatus = fabricationStatus,
                    FabricationStandardCode = fabricationStandardCode,
                    FabricationDetailingRevision = fabricationDetailingRevision
                });
            }
        }

        internal static string RequireCanonicalElementId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Rebar schedule element id is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Rebar schedule element id must not contain surrounding whitespace.", parameterName);
            for (var index = 0; index < value.Length; index++)
            {
                if (char.IsControl(value[index]))
                    throw new ArgumentException("Rebar schedule element id must not contain control characters.", parameterName);
            }
            return value;
        }

        private static void ValidateAggregate(IReadOnlyList<RebarScheduleRow> rows)
        {
            var quantity = 0;
            var totalLength = 0d;
            var totalWeight = 0d;
            foreach (var row in rows)
            {
                if (row == null) throw new InvalidOperationException("BBS row cannot be null.");
                try { quantity = checked(quantity + row.Quantity); }
                catch (OverflowException ex) { throw new OverflowException("BBS total bar quantity exceeds Int32 capacity.", ex); }
                totalLength = RebarMath.Add(totalLength, row.TotalLengthM, "BBS aggregate length");
                totalWeight = RebarMath.Add(totalWeight, row.TotalWeightKg, "BBS aggregate weight");
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
                var millimeters = RebarMath.Multiply(input.DistributionLengthM, 1000d, "spacing distribution length");
                var intervals = RebarMath.Divide(millimeters, group.SpacingMm.Value, "spacing interval count");
                var rounded = RebarMath.CeilingNearInteger(intervals, "spacing interval count");
                if (rounded > int.MaxValue - 1d) throw new OverflowException("Rebar spacing produces too many bars.");
                return checked((int)rounded + 1);
            }
            throw new InvalidOperationException("Rebar quantity cannot be inferred. Provide count notation, spacing + distribution length, or CountOverride.");
        }

        private struct CompensatedFiniteSum
        {
            private double _sum;
            private double _compensation;
            private string _lastLabel;

            public void Add(double value, string label)
            {
                var next = _sum + value;
                EnsureFinite(next, label);

                var correction = Math.Abs(_sum) >= Math.Abs(value)
                    ? (_sum - next) + value
                    : (value - next) + _sum;
                var compensation = _compensation + correction;
                EnsureFinite(compensation, label);

                _sum = next;
                _compensation = compensation;
                _lastLabel = label;
            }

            public double Value
            {
                get
                {
                    var result = _sum + _compensation;
                    EnsureFinite(result, _lastLabel ?? "cutting + hook allowance");
                    return result;
                }
            }

            private static void EnsureFinite(double value, string label)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    throw new OverflowException("Rebar addition overflow: " + label);
            }
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        private static void EnsureFiniteNonNegative(double value, string name) => RebarMath.NonNegative(value, name);
    }

    public static class ProjectRebarScheduleBuilder
    {
        public static IReadOnlyList<RebarScheduleRow> Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var inputs = new List<RebarScheduleInput>();
            foreach (var element in ValidateProjectElements(project))
            {
                if (!element.Properties.TryGetValue("RebarNotation", out var notation) || string.IsNullOrWhiteSpace(notation)) continue;
                inputs.Add(new RebarScheduleInput
                {
                    ElementId = element.Id,
                    BarMark = Text(element, "RebarBarMark", element.Id),
                    ShapeCode = Text(element, "RebarShapeCode", "00"),
                    Notation = notation,
                    CuttingLengthM = ResolveCuttingLength(element),
                    DistributionLengthM = Number(element, "RebarDistributionLengthM"),
                    LapLengthM = Number(element, "RebarLapLengthM"),
                    AnchorLengthM = Number(element, "RebarAnchorLengthM"),
                    HookAllowanceM = Number(element, "RebarHookAllowanceM"),
                    WastePercent = Number(element, "RebarWastePercent"),
                    CountOverride = Integer(element, "RebarCount"),
                    FabricationStatus = Text(element, RebarFabricationQualificationHealthService.StatusPropertyKey, string.Empty),
                    FabricationStandardCode = Text(element, RebarFabricationQualificationHealthService.StandardCodePropertyKey, string.Empty),
                    FabricationDetailingRevision = Text(element, RebarFabricationQualificationHealthService.DetailingRevisionPropertyKey, string.Empty)
                });
            }
            return RebarScheduleBuilder.Build(inputs);
        }

        private static IReadOnlyList<ProjectElement> ValidateProjectElements(ProjectState project)
        {
            var elements = new List<ProjectElement>(project.Elements.Count);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element entry.");
                if (string.IsNullOrWhiteSpace(element.Id))
                    throw new InvalidOperationException("Project contains a semantic element with a blank id.");
                string elementId;
                try
                {
                    elementId = RebarScheduleBuilder.RequireCanonicalElementId(element.Id, nameof(element.Id));
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException("Project contains a semantic element with a noncanonical id.", ex);
                }
                if (!ids.Add(elementId)) throw new InvalidOperationException("Project contains duplicate semantic element id: " + elementId);
                elements.Add(element);
            }
            return elements.AsReadOnly();
        }

        private static double ResolveCuttingLength(ProjectElement element)
        {
            if (HasValue(element, "RebarCuttingLengthM")) return Number(element, "RebarCuttingLengthM");
            if (HasValue(element, "LengthM")) return Number(element, "LengthM");
            if (element.Quantities.TryGetValue("LengthM", out var quantity))
            {
                if (double.IsNaN(quantity) || double.IsInfinity(quantity) || quantity < 0d) throw new FormatException("Invalid rebar quantity LengthM on " + element.Id + ": " + quantity.ToString("R", CultureInfo.InvariantCulture));
                return quantity;
            }
            return 0d;
        }

        private static bool HasValue(ProjectElement element, string key) => element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value);

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
    }
}
