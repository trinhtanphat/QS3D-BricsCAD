using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Documentation
{
    public sealed class SemanticSchedulePlacementItem
    {
        public SemanticSchedulePlacementItem(string scheduleId, double widthMm, double heightMm)
        {
            ScheduleId = scheduleId;
            WidthMm = widthMm;
            HeightMm = heightMm;
        }

        public string ScheduleId { get; }
        public double WidthMm { get; }
        public double HeightMm { get; }
    }

    public sealed class SemanticSchedulePlacementOptions
    {
        public SemanticSchedulePlacementOptions(
            double marginLeftMm = 10d,
            double marginTopMm = 10d,
            double marginRightMm = 10d,
            double marginBottomMm = 10d,
            double horizontalGapMm = 8d,
            double verticalGapMm = 8d,
            double reservedBottomMm = 0d)
        {
            MarginLeftMm = marginLeftMm;
            MarginTopMm = marginTopMm;
            MarginRightMm = marginRightMm;
            MarginBottomMm = marginBottomMm;
            HorizontalGapMm = horizontalGapMm;
            VerticalGapMm = verticalGapMm;
            ReservedBottomMm = reservedBottomMm;
        }

        public double MarginLeftMm { get; }
        public double MarginTopMm { get; }
        public double MarginRightMm { get; }
        public double MarginBottomMm { get; }
        public double HorizontalGapMm { get; }
        public double VerticalGapMm { get; }
        public double ReservedBottomMm { get; }
    }

    public sealed class SemanticSchedulePlacement
    {
        internal SemanticSchedulePlacement(string scheduleId, double xMm, double yMm, double widthMm, double heightMm)
        {
            ScheduleId = scheduleId;
            Xmm = xMm;
            Ymm = yMm;
            WidthMm = widthMm;
            HeightMm = heightMm;
        }

        public string ScheduleId { get; }
        public double Xmm { get; }
        public double Ymm { get; }
        public double WidthMm { get; }
        public double HeightMm { get; }
    }

    public sealed class SemanticSchedulePlacementPlan
    {
        internal SemanticSchedulePlacementPlan(string sheetId, IReadOnlyList<SemanticSchedulePlacement> placements)
        {
            SheetId = sheetId;
            Placements = new List<SemanticSchedulePlacement>(placements).AsReadOnly();
        }

        public string SheetId { get; }
        public IReadOnlyList<SemanticSchedulePlacement> Placements { get; }
    }

    public static class SemanticSchedulePlacementPlanner
    {
        private const int MaxItems = 128;
        private const int MaxIdLength = 128;

        public static SemanticSchedulePlacementPlan Build(
            SemanticSheetPlan sheet,
            IEnumerable<SemanticScheduleDefinition> availableSchedules,
            IEnumerable<SemanticSchedulePlacementItem> items,
            SemanticSchedulePlacementOptions? options = null)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (availableSchedules == null) throw new ArgumentNullException(nameof(availableSchedules));
            if (items == null) throw new ArgumentNullException(nameof(items));
            options ??= new SemanticSchedulePlacementOptions();
            ValidateOptions(options);

            var scheduleIndex = BuildScheduleIndex(availableSchedules);
            var materialized = MaterializeItems(items);

            var sheetId = Required(sheet.Id, nameof(sheet.Id));
            PositiveFinite(sheet.WidthMm, nameof(sheet.WidthMm));
            PositiveFinite(sheet.HeightMm, nameof(sheet.HeightMm));

            var right = RetreatEdge(sheet.WidthMm, options.MarginRightMm, "semantic schedule right margin");
            var bottom = RetreatEdge(
                RetreatEdge(sheet.HeightMm, options.MarginBottomMm, "semantic schedule bottom margin"),
                options.ReservedBottomMm,
                "semantic schedule reserved bottom area");
            if (right <= options.MarginLeftMm || bottom <= options.MarginTopMm)
                throw new InvalidOperationException("Semantic schedule placement margins/reserved area leave no usable paper region.");

            var occupied = BuildOccupiedRegions(sheet);
            var uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var normalized = new List<SemanticSchedulePlacementItem>(materialized.Count);
            for (var i = 0; i < materialized.Count; i++)
            {
                var item = materialized[i] ?? throw new ArgumentException("Semantic schedule placement item cannot be null at index " + i + ".", nameof(items));
                var scheduleId = Required(item.ScheduleId, "items[" + i + "].ScheduleId");
                if (!uniqueIds.Add(scheduleId))
                    throw new InvalidOperationException("Semantic schedule placement contains duplicate schedule id: " + scheduleId + ".");
                if (!scheduleIndex.ContainsKey(scheduleId))
                    throw new InvalidOperationException("Semantic schedule placement references missing schedule id: " + scheduleId + ".");
                PositiveFinite(item.WidthMm, "items[" + i + "].WidthMm");
                PositiveFinite(item.HeightMm, "items[" + i + "].HeightMm");
                if (!FitsWithin(options.MarginLeftMm, item.WidthMm, right) ||
                    !FitsWithin(options.MarginTopMm, item.HeightMm, bottom))
                    throw new InvalidOperationException("Schedule " + scheduleId + " does not fit inside the usable paper region.");
                normalized.Add(new SemanticSchedulePlacementItem(scheduleId, item.WidthMm, item.HeightMm));
            }

            var ordered = normalized
                .OrderByDescending(x => x.HeightMm)
                .ThenByDescending(x => x.WidthMm)
                .ThenBy(x => x.ScheduleId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ScheduleId, StringComparer.Ordinal)
                .ToArray();

            var placements = new List<SemanticSchedulePlacement>(ordered.Length);
            foreach (var item in ordered)
            {
                var placement = FindPlacement(item, occupied, options, right, bottom);
                if (placement == null)
                    throw new InvalidOperationException("Schedule " + item.ScheduleId + " could not be placed without overlapping existing sheet content.");
                placements.Add(placement);
                occupied.Add(new Region(placement.Xmm, placement.Ymm, placement.WidthMm, placement.HeightMm));
            }

            return new SemanticSchedulePlacementPlan(
                sheetId,
                placements
                    .OrderBy(x => x.Ymm)
                    .ThenBy(x => x.Xmm)
                    .ThenBy(x => x.ScheduleId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.ScheduleId, StringComparer.Ordinal)
                    .ToArray());
        }

        private static Dictionary<string, SemanticScheduleDefinition> BuildScheduleIndex(IEnumerable<SemanticScheduleDefinition> schedules)
        {
            var result = new Dictionary<string, SemanticScheduleDefinition>(StringComparer.OrdinalIgnoreCase);
            var count = 0;
            foreach (var schedule in schedules)
            {
                count++;
                if (count > MaxItems)
                    throw new InvalidOperationException("Semantic schedule placement supports at most " + MaxItems + " available schedules.");
                if (schedule == null) throw new ArgumentException("Available semantic schedule cannot be null.", nameof(schedules));
                var id = Required(schedule.Id, "availableSchedules.Id");
                if (result.ContainsKey(id)) throw new InvalidOperationException("Available semantic schedules contain duplicate id: " + id + ".");
                result.Add(id, schedule);
            }
            return result;
        }

        private static List<SemanticSchedulePlacementItem> MaterializeItems(IEnumerable<SemanticSchedulePlacementItem> items)
        {
            var result = new List<SemanticSchedulePlacementItem>();
            foreach (var item in items)
            {
                if (result.Count >= MaxItems)
                    throw new InvalidOperationException("Semantic schedule placement supports at most " + MaxItems + " schedules per sheet.");
                result.Add(item);
            }
            return result;
        }

        private static List<Region> BuildOccupiedRegions(SemanticSheetPlan sheet)
        {
            var result = new List<Region>(sheet.Placements.Count);
            foreach (var placement in sheet.Placements)
            {
                if (placement == null) throw new InvalidOperationException("Semantic sheet plan contains a null view placement.");
                NonNegativeFinite(placement.Xmm, "sheet.Placements.Xmm");
                NonNegativeFinite(placement.Ymm, "sheet.Placements.Ymm");
                PositiveFinite(placement.WidthMm, "sheet.Placements.WidthMm");
                PositiveFinite(placement.HeightMm, "sheet.Placements.HeightMm");
                if (!FitsWithin(placement.Xmm, placement.WidthMm, sheet.WidthMm) ||
                    !FitsWithin(placement.Ymm, placement.HeightMm, sheet.HeightMm))
                    throw new InvalidOperationException("Existing semantic view placement lies outside the paper bounds: " + placement.ViewId + ".");
                result.Add(new Region(placement.Xmm, placement.Ymm, placement.WidthMm, placement.HeightMm));
            }
            return result;
        }

        private static SemanticSchedulePlacement? FindPlacement(
            SemanticSchedulePlacementItem item,
            IReadOnlyList<Region> occupied,
            SemanticSchedulePlacementOptions options,
            double right,
            double bottom)
        {
            var xs = new SortedSet<double> { options.MarginLeftMm };
            var ys = new SortedSet<double> { options.MarginTopMm };
            foreach (var region in occupied)
            {
                var x = AdvanceEdge(region.X, region.Width, options.HorizontalGapMm, "semantic schedule horizontal occupied edge");
                var y = AdvanceEdge(region.Y, region.Height, options.VerticalGapMm, "semantic schedule vertical occupied edge");
                if (Finite(x)) xs.Add(x);
                if (Finite(y)) ys.Add(y);
            }

            foreach (var y in ys)
            {
                if (y < options.MarginTopMm || !FitsWithin(y, item.HeightMm, bottom)) continue;
                foreach (var x in xs)
                {
                    if (x < options.MarginLeftMm || !FitsWithin(x, item.WidthMm, right)) continue;
                    var candidate = new Region(x, y, item.WidthMm, item.HeightMm);
                    if (occupied.Any(region => Conflicts(region, candidate, options.HorizontalGapMm, options.VerticalGapMm))) continue;
                    return new SemanticSchedulePlacement(item.ScheduleId, x, y, item.WidthMm, item.HeightMm);
                }
            }
            return null;
        }

        private static bool Conflicts(Region a, Region b, double horizontalGapMm, double verticalGapMm) =>
            AxisConflicts(a.X, a.Width, b.X, b.Width, horizontalGapMm) &&
            AxisConflicts(a.Y, a.Height, b.Y, b.Height, verticalGapMm);

        private static bool AxisConflicts(double aStart, double aExtent, double bStart, double bExtent, double gap)
        {
            if (aStart <= bStart)
                return SeparationViolatesGap(bStart - aStart, aExtent, gap);
            return SeparationViolatesGap(aStart - bStart, bExtent, gap);
        }

        private static bool SeparationViolatesGap(double separation, double leadingExtent, double gap)
        {
            if (!Finite(separation)) return false;
            if (separation < leadingExtent) return true;
            if (separation == leadingExtent) return gap > 0d;
            return separation - leadingExtent < gap;
        }

        private static bool FitsWithin(double start, double extent, double limit)
        {
            if (!Finite(start) || !Finite(extent) || !Finite(limit) || start > limit) return false;
            return extent <= limit - start;
        }

        private static double AdvanceEdge(double start, double extent, double gap, string label)
        {
            var edge = start + extent;
            if (double.IsInfinity(edge)) return double.PositiveInfinity;
            if (double.IsNaN(edge)) throw new InvalidOperationException(label + " produced an invalid coordinate.");
            if (extent > 0d && !(edge > start))
                throw new InvalidOperationException(label + " lost positive extent to floating-point precision.");
            if (gap == 0d) return edge;

            var advanced = edge + gap;
            if (double.IsInfinity(advanced)) return double.PositiveInfinity;
            if (double.IsNaN(advanced)) throw new InvalidOperationException(label + " produced an invalid gap coordinate.");
            if (!(advanced > edge))
                throw new InvalidOperationException(label + " lost a positive gap to floating-point precision.");
            return advanced;
        }

        private static double RetreatEdge(double start, double amount, string label)
        {
            var edge = start - amount;
            if (double.IsNaN(edge) || double.IsInfinity(edge))
                throw new InvalidOperationException(label + " produced a non-finite paper boundary.");
            if (amount > 0d && !(edge < start))
                throw new InvalidOperationException(label + " was lost to floating-point precision.");
            return edge;
        }

        private static void ValidateOptions(SemanticSchedulePlacementOptions options)
        {
            NonNegativeFinite(options.MarginLeftMm, nameof(options.MarginLeftMm));
            NonNegativeFinite(options.MarginTopMm, nameof(options.MarginTopMm));
            NonNegativeFinite(options.MarginRightMm, nameof(options.MarginRightMm));
            NonNegativeFinite(options.MarginBottomMm, nameof(options.MarginBottomMm));
            NonNegativeFinite(options.HorizontalGapMm, nameof(options.HorizontalGapMm));
            NonNegativeFinite(options.VerticalGapMm, nameof(options.VerticalGapMm));
            NonNegativeFinite(options.ReservedBottomMm, nameof(options.ReservedBottomMm));
        }

        private static string Required(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value!.Trim();
            if (!string.Equals(value, normalized, StringComparison.Ordinal))
                throw new ArgumentException("Value must not contain leading or trailing whitespace.", name);
            if (value.Length > MaxIdLength) throw new ArgumentException("Value exceeds " + MaxIdLength + " characters.", name);
            return value;
        }

        private static void PositiveFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentOutOfRangeException(name, "Value must be finite and greater than zero.");
        }

        private static void NonNegativeFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(name, "Value must be finite and non-negative.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private sealed class Region
        {
            public Region(double x, double y, double width, double height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public double X { get; }
            public double Y { get; }
            public double Width { get; }
            public double Height { get; }
        }
    }
}
