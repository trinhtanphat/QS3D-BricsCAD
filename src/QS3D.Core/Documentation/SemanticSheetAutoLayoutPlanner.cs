using System;
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

            var usableWidth = options.PaperWidthMm - options.MarginLeftMm - options.MarginRightMm;
            var usableHeight = options.PaperHeightMm - options.MarginTopMm - options.MarginBottomMm - options.ReservedBottomMm;
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
            return result;
        }

        private static List<SemanticSheetAutoLayoutItem> MaterializeItemsBounded(IEnumerable<SemanticSheetAutoLayoutItem> items)
        {
            var result = new List<SemanticSheetAutoLayoutItem>(Math.Min(MaxItems, 256));
            using (var enumerator = items.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= MaxItems)
                        throw new InvalidOperationException("Automatic sheet layout supports at most " + MaxItems + " views.");
                    result.Add(enumerator.Current);
                }
            }
            return result;
        }

        private static Dictionary<string, SemanticViewPlan> BuildViewIndex(IEnumerable<SemanticViewPlan> availableViews)
        {
            var result = new Dictionary<string, SemanticViewPlan>(StringComparer.OrdinalIgnoreCase);
            foreach (var view in availableViews)
            {
                if (view == null) throw new ArgumentException("Available semantic view cannot be null.", nameof(availableViews));
                var id = Required(view.Id, "availableViews.Id");
                if (result.ContainsKey(id)) throw new InvalidOperationException("Available semantic views contain duplicate id: " + id + ".");
                result.Add(id, view);
            }
            return result;
        }

        private static void ValidateOptions(SemanticSheetAutoLayoutOptions options)
        {
            Required(options.SheetIdPrefix, nameof(options.SheetIdPrefix));
            Required(options.SheetNumberPrefix, nameof(options.SheetNumberPrefix));
            Required(options.SheetNamePrefix, nameof(options.SheetNamePrefix));
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

        private static string Required(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value!.Trim();
            if (normalized.Length > 120) throw new ArgumentException("Value exceeds 120 characters.", name);
            return normalized;
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
                var localX = _started ? _cursorX : 0d;
                var localY = _started ? _cursorY : 0d;
                var rowHeight = _started ? _rowHeight : 0d;

                if (_started && localX + item.WidthMm > usableWidth)
                {
                    localX = 0d;
                    localY += rowHeight + options.VerticalGapMm;
                    rowHeight = 0d;
                }

                if (localY + item.HeightMm > usableHeight) return false;

                Placements.Add(new SemanticSheetPlacementDefinition(
                    item.ViewId,
                    options.MarginLeftMm + localX,
                    options.MarginTopMm + localY,
                    item.WidthMm,
                    item.HeightMm));
                _cursorX = localX + item.WidthMm + options.HorizontalGapMm;
                _cursorY = localY;
                _rowHeight = Math.Max(rowHeight, item.HeightMm);
                _started = true;
                return true;
            }
        }
    }
}
