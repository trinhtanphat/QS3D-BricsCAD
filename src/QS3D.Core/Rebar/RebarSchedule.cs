using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Rebar
{
    public enum RebarShape
    {
        Straight,
        LBar,
        UBar,
        StirrupRect,
        Custom
    }

    public sealed class RebarScheduleRow
    {
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
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new ArgumentOutOfRangeException(name);
        }
    }

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
    }
}
