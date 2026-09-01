using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Documentation
{
    public sealed class SemanticSheetAutoLayoutItem
    {
        public SemanticSheetAutoLayoutItem(string viewId, double widthMm, double heightMm)
        {
            ViewId = viewId;
            WidthMm = widthMm;
            HeightMm = heightMm;
        }

        public string ViewId { get; }
        public double WidthMm { get; }
        public double HeightMm { get; }
    }

    public sealed class SemanticSheetAutoLayoutOptions
    {
        public SemanticSheetAutoLayoutOptions(
            string sheetIdPrefix,
            string sheetNumberPrefix,
            string sheetNamePrefix,
            double paperWidthMm,
            double paperHeightMm,
            double marginLeftMm = 10d,
            double marginTopMm = 10d,
            double marginRightMm = 10d,
            double marginBottomMm = 10d,
            double horizontalGapMm = 8d,
            double verticalGapMm = 8d,
            double reservedBottomMm = 0d,
            string? titleBlockName = null)
        {
            SheetIdPrefix = sheetIdPrefix;
            SheetNumberPrefix = sheetNumberPrefix;
            SheetNamePrefix = sheetNamePrefix;
            PaperWidthMm = paperWidthMm;
            PaperHeightMm = paperHeightMm;
            MarginLeftMm = marginLeftMm;
            MarginTopMm = marginTopMm;
            MarginRightMm = marginRightMm;
            MarginBottomMm = marginBottomMm;
            HorizontalGapMm = horizontalGapMm;
            VerticalGapMm = verticalGapMm;
            ReservedBottomMm = reservedBottomMm;
            TitleBlockName = titleBlockName;
        }

        public string SheetIdPrefix { get; }
        public string SheetNumberPrefix { get; }
        public string SheetNamePrefix { get; }
        public double PaperWidthMm { get; }
        public double PaperHeightMm { get; }
        public double MarginLeftMm { get; }
        public double MarginTopMm { get; }
        public double MarginRightMm { get; }
        public double MarginBottomMm { get; }
        public double HorizontalGapMm { get; }
        public double VerticalGapMm { get; }
        public double ReservedBottomMm { get; }
        public string? TitleBlockName { get; }
    }

    public static class SemanticSheetAutoLayoutPlanner
    {
        private const int MaxItems = 10000;
        private const int MaxSheetNumberLength = 64;
        private const int MaxSheetOrdinalLength = 5;
        private const int MaxSheetNumberPrefixLength = MaxSheetNumberLength - MaxSheetOrdinalLength;
        private const int MaxTitleBlockLength = 160;

        public static IReadOnlyList<SemanticSheetPlan> Build(
            IEnumerable<SemanticSheetAutoLayoutItem> items,
            IEnumerable<SemanticViewPlan> availableViews,
            SemanticSheetAutoLayoutOptions options)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (availableViews == null) throw new ArgumentNullException(nameof(availableViews));
            if (options == null) throw new ArgumentNullException(nameof(options));

            ValidateOptions(options);
            var views = BuildViewIndex(availableViews);
            var materialized = MaterializeItemsBounded(items);
            if (materialized.Count == 0) return Array.Empty<SemanticSheetPlan>();

            var uniqueItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var normalized = new List<SemanticSheetAutoLayoutItem>(materialized.Count);
            for (var i = 0; i < materialized.Count; i++)
            {
                var item = materialized[i] ?? throw new ArgumentException("Automatic sheet layout item cannot be null at index " + i + ".", nameof(items));
                var viewId = Required(item.ViewId, "items[" + i + "].ViewId");
                if (!uniqueItemIds.Add(viewId))
                    throw new InvalidOperationException("Automatic sheet layout contains duplicate view id: " + viewId + ".");
                if (!views.ContainsKey(viewId))
                    throw new InvalidOperationException("Automatic sheet layout references missing view id: " + viewId + ".");
                PositiveFinite(item.WidthMm, "items[" + i + "].WidthMm");
                PositiveFinite(item.HeightMm, "items[" + i + "].HeightMm");
                normalized.Add(new SemanticSheetAutoLayoutItem(viewId, item.WidthMm, item.HeightMm));
            }

            var usableWidth = RetreatEdge(
                RetreatEdge(options.PaperWidthMm, options.MarginRightMm, "automatic sheet right margin"),
                options.MarginLeftMm,
                "automatic sheet left margin");
            var usableHeight = RetreatEdge(
                RetreatEdge(
                    RetreatEdge(options.PaperHeightMm, options.MarginBottomMm, "automatic sheet bottom margin"),
                    options.ReservedBottomMm,
                    "automatic sheet reserved bottom area"),
                options.MarginTopMm,
                "automatic sheet top margin");
            if (usableWidth <= 0d || usableHeight <= 0d)
                throw new InvalidOperationException("Automatic sheet layout margins/reserved area leave no usable paper region.");

            foreach (var item in normalized)
            {
                if (item.WidthMm > usableWidth || item.HeightMm > usableHeight)
                    throw new InvalidOperationException("View " + item.ViewId + " does not fit inside the usable paper region.");
            }

            var ordered = normalized
                .OrderByDescending(x => x.HeightMm)
                .ThenByDescending(x => x.WidthMm)
                .ThenBy(x => x.ViewId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ViewId, StringComparer.Ordinal)
                .ToArray();

            var pages = new List<PageState>();
            foreach (var item in ordered)
            {
                var placed = false;
                foreach (var page in pages)
                {
                    if (!page.TryPlace(item, options, usableWidth, usableHeight)) continue;
                    placed = true;
                    break;
                }

                if (placed) continue;
                var next = new PageState();
                if (!next.TryPlace(item, options, usableWidth, usableHeight))
                    throw new InvalidOperationException("View " + item.ViewId + " could not be placed on a new sheet.");
                pages.Add(next);
            }

            var result = new List<SemanticSheetPlan>(pages.Count);
            for (var i = 0; i < pages.Count; i++)
            {
                var ordinal = i + 1;
                var definition = new SemanticSheetDefinition(
                    Required(options.SheetIdPrefix, nameof(options.SheetIdPrefix)) + "-" + ordinal.ToString("D2"),
                    Required(options.SheetNumberPrefix, nameof(options.SheetNumberPrefix)) + ordinal.ToString("D2"),
                    Required(options.SheetNamePrefix, nameof(options.SheetNamePrefix)) + " " + ordinal,
                    options.PaperWidthMm,
                    options.PaperHeightMm,
                    pages[i].Placements,
                    options.TitleBlockName);
                result.Add(SemanticSheetPlanner.Build(definition, views.Values));
            }
            return result.AsReadOnly();
        }

        private static List<SemanticSheetAutoLayoutItem> MaterializeItemsBounded(IEnumerable<SemanticSheetAutoLayoutItem> items)
        {
            var knownCount = RequireKnownCountsWithinLimit(items, "automatic sheet layout items");
            var result = new List<SemanticSheetAutoLayoutItem>(Math.Min(MaxItems, 256));
            using (var enumerator = items.GetEnumerator())
            {
                while (true)
                {
                    RequireKnownCountStillMatches(items, knownCount, "automatic sheet layout items");
                    var moved = enumerator.MoveNext();
                    RequireKnownCountStillMatches(items, knownCount, "automatic sheet layout items");
                    if (!moved) break;
                    if (result.Count >= MaxItems)
                        throw new InvalidOperationException("Automatic sheet layout supports at most " + MaxItems + " views.");
                    var item = enumerator.Current;
                    RequireKnownCountStillMatches(items, knownCount, "automatic sheet layout items");
                    result.Add(item);
                }
            }

            RequireKnownCountStillMatches(items, knownCount, "automatic sheet layout items");
            RequireTraversalMatchesKnownCount(knownCount, result.Count, "automatic sheet layout items");
            return result;
        }

        private static Dictionary<string, SemanticViewPlan> BuildViewIndex(IEnumerable<SemanticViewPlan> availableViews)
        {
            var knownCount = RequireKnownCountsWithinLimit(availableViews, "automatic sheet layout available views");
            var result = new Dictionary<string, SemanticViewPlan>(StringComparer.OrdinalIgnoreCase);
            var count = 0;
            using (var enumerator = availableViews.GetEnumerator())
            {
                while (true)
                {
                    RequireKnownCountStillMatches(availableViews, knownCount, "automatic sheet layout available views");
                    var moved = enumerator.MoveNext();
                    RequireKnownCountStillMatches(availableViews, knownCount, "automatic sheet layout available views");
                    if (!moved) break;
                    if (count >= MaxItems)
                        throw new InvalidOperationException("Automatic sheet layout supports at most " + MaxItems + " available views.");
                    var view = enumerator.Current;
                    RequireKnownCountStillMatches(availableViews, knownCount, "automatic sheet layout available views");
                    count++;
                    if (view == null) throw new ArgumentException("Available semantic view cannot be null.", nameof(availableViews));
                    var id = Required(view.Id, "availableViews.Id");
                    if (result.ContainsKey(id)) throw new InvalidOperationException("Available semantic views contain duplicate id: " + id + ".");
                    result.Add(id, view);
                }
            }

            RequireKnownCountStillMatches(availableViews, knownCount, "automatic sheet layout available views");
            RequireTraversalMatchesKnownCount(knownCount, count, "automatic sheet layout available views");
            return result;
        }

        private static int? RequireKnownCountsWithinLimit<T>(IEnumerable<T> values, string label)
        {
            var counts = new List<int>(3);
            if (values is ICollection<T> collection) counts.Add(collection.Count);
            if (values is IReadOnlyCollection<T> readOnlyCollection) counts.Add(readOnlyCollection.Count);
            if (values is ICollection nonGenericCollection) counts.Add(nonGenericCollection.Count);

            if (counts.Count == 0) return null;

            var expected = counts[0];
            var maximum = expected;
            var hasNegative = expected < 0;
            var hasConflict = false;
            for (var i = 1; i < counts.Count; i++)
            {
                if (counts[i] < 0) hasNegative = true;
                if (counts[i] != expected) hasConflict = true;
                if (counts[i] > maximum) maximum = counts[i];
            }

            if (maximum > MaxItems)
                throw new InvalidOperationException("Automatic sheet layout supports at most " + MaxItems + " " + label + ".");
            if (hasNegative)
                throw new InvalidOperationException("Automatic sheet layout received an invalid negative known count for " + label + ".");
            if (hasConflict)
                throw new InvalidOperationException("Automatic sheet layout received conflicting known counts for " + label + ".");
            return expected;
        }

        private static void RequireKnownCountStillMatches<T>(IEnumerable<T> values, int? admittedCount, string label)
        {
            var currentCount = RequireKnownCountsWithinLimit(values, label);
            if (admittedCount.HasValue != currentCount.HasValue ||
                (admittedCount.HasValue && currentCount!.Value != admittedCount.Value))
                throw new InvalidOperationException("Automatic sheet layout " + label + " known Count changed during traversal.");
        }

        private static void RequireTraversalMatchesKnownCount(int? knownCount, int observedCount, string label)
        {
            if (knownCount.HasValue && observedCount != knownCount.Value)
                throw new InvalidOperationException("Automatic sheet layout " + label + " known Count does not match traversal cardinality.");
        }

        private static void ValidateOptions(SemanticSheetAutoLayoutOptions options)
        {
            Required(options.SheetIdPrefix, nameof(options.SheetIdPrefix));
            Required(options.SheetNumberPrefix, nameof(options.SheetNumberPrefix), MaxSheetNumberPrefixLength);
            Required(options.SheetNamePrefix, nameof(options.SheetNamePrefix));
            ValidateOptional(options.TitleBlockName, nameof(options.TitleBlockName), MaxTitleBlockLength);
            PositiveFinite(options.PaperWidthMm, nameof(options.PaperWidthMm));
            PositiveFinite(options.PaperHeightMm, nameof(options.PaperHeightMm));
            NonNegativeFinite(options.MarginLeftMm, nameof(options.MarginLeftMm));
            NonNegativeFinite(options.MarginTopMm, nameof(options.MarginTopMm));
            NonNegativeFinite(options.MarginRightMm, nameof(options.MarginRightMm));
            NonNegativeFinite(options.MarginBottomMm, nameof(options.MarginBottomMm));
            NonNegativeFinite(options.HorizontalGapMm, nameof(options.HorizontalGapMm));
            NonNegativeFinite(options.VerticalGapMm, nameof(options.VerticalGapMm));
            NonNegativeFinite(options.ReservedBottomMm, nameof(options.ReservedBottomMm));
        }

        private static string Required(string? value, string name, int maxLength = 120)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value!.Trim();
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }

        private static void ValidateOptional(string? value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (value!.Trim().Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
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

        private static double RetreatEdge(double start, double amount, string label)
        {
            var edge = start - amount;
            if (double.IsNaN(edge) || double.IsInfinity(edge))
                throw new InvalidOperationException(label + " produced a non-finite usable paper boundary.");
            if (amount > 0d && !(edge < start))
                throw new InvalidOperationException(label + " was lost to floating-point precision.");
            return edge;
        }

        private static bool FitsWithin(double start, double extent, double limit)
        {
            if (double.IsNaN(start) || double.IsInfinity(start) || start > limit) return false;
            return extent <= limit - start;
        }

        private static double AdvanceEdge(double start, double extent, string label)
        {
            var edge = start + extent;
            if (double.IsInfinity(edge)) return double.PositiveInfinity;
            if (double.IsNaN(edge)) throw new InvalidOperationException(label + " produced an invalid coordinate.");
            if (extent > 0d && !(edge > start))
                throw new InvalidOperationException(label + " lost positive extent to floating-point precision.");
            return edge;
        }

        private static double AdvanceGap(double edge, double gap, string label)
        {
            if (gap == 0d) return edge;
            var advanced = edge + gap;
            if (double.IsInfinity(advanced)) return double.PositiveInfinity;
            if (double.IsNaN(advanced)) throw new InvalidOperationException(label + " produced an invalid gap coordinate.");
            if (!(advanced > edge))
                throw new InvalidOperationException(label + " lost a positive gap to floating-point precision.");
            return advanced;
        }

        private static double TranslateCoordinate(double origin, double offset, string label)
        {
            if (offset == 0d) return origin;
            var translated = origin + offset;
            if (double.IsNaN(translated) || double.IsInfinity(translated))
                throw new InvalidOperationException(label + " produced a non-finite placement coordinate.");
            if (!(translated > origin))
                throw new InvalidOperationException(label + " lost a positive placement offset to floating-point precision.");
            return translated;
        }

        private sealed class PageState
        {
            private double _cursorX;
            private double _cursorY;
            private double _rowHeight;
            private bool _started;

            public IList<SemanticSheetPlacementDefinition> Placements { get; } = new List<SemanticSheetPlacementDefinition>();

            public bool TryPlace(
                SemanticSheetAutoLayoutItem item,
                SemanticSheetAutoLayoutOptions options,
                double usableWidth,
                double usableHeight)
            {
                if (Placements.Count >= SemanticSheetPlanner.MaxPlacements) return false;

                var localX = 0d;
                var localY = _started ? _cursorY : 0d;
                var rowHeight = _started ? _rowHeight : 0d;

                if (_started)
                {
                    var wrapRow = !FitsWithin(_cursorX, item.WidthMm, usableWidth);
                    if (!wrapRow)
                    {
                        localX = AdvanceGap(_cursorX, options.HorizontalGapMm, "automatic sheet horizontal gap");
                        wrapRow = !FitsWithin(localX, item.WidthMm, usableWidth);
                    }

                    if (wrapRow)
                    {
                        var rowBottom = AdvanceEdge(_cursorY, rowHeight, "automatic sheet row height");
                        if (!FitsWithin(rowBottom, item.HeightMm, usableHeight)) return false;
                        localX = 0d;
                        localY = AdvanceGap(rowBottom, options.VerticalGapMm, "automatic sheet vertical gap");
                        rowHeight = 0d;
                    }
                }

                if (!FitsWithin(localY, item.HeightMm, usableHeight)) return false;

                Placements.Add(new SemanticSheetPlacementDefinition(
                    item.ViewId,
                    TranslateCoordinate(options.MarginLeftMm, localX, "automatic sheet horizontal placement origin"),
                    TranslateCoordinate(options.MarginTopMm, localY, "automatic sheet vertical placement origin"),
                    item.WidthMm,
                    item.HeightMm));
                _cursorX = AdvanceEdge(localX, item.WidthMm, "automatic sheet horizontal item edge");
                _cursorY = localY;
                _rowHeight = Math.Max(rowHeight, item.HeightMm);
                _started = true;
                return true;
            }
        }
    }
}