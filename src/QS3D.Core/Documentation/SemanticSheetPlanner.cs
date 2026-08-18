using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Documentation
{
    public sealed class SemanticSheetPlacementDefinition
    {
        public SemanticSheetPlacementDefinition(string viewId, double xMm, double yMm, double widthMm, double heightMm)
        {
            ViewId = viewId;
            Xmm = xMm;
            Ymm = yMm;
            WidthMm = widthMm;
            HeightMm = heightMm;
        }

        public string ViewId { get; }
        public double Xmm { get; }
        public double Ymm { get; }
        public double WidthMm { get; }
        public double HeightMm { get; }
    }

    public sealed class SemanticSheetDefinition
    {
        public SemanticSheetDefinition(
            string id,
            string number,
            string name,
            double widthMm,
            double heightMm,
            IEnumerable<SemanticSheetPlacementDefinition> placements,
            string? titleBlockName = null)
        {
            Id = id;
            Number = number;
            Name = name;
            WidthMm = widthMm;
            HeightMm = heightMm;
            TitleBlockName = titleBlockName;
            Placements = SnapshotPlacements(placements ?? throw new ArgumentNullException(nameof(placements)));
        }

        public string Id { get; }
        public string Number { get; }
        public string Name { get; }
        public double WidthMm { get; }
        public double HeightMm { get; }
        public string? TitleBlockName { get; }
        public IReadOnlyList<SemanticSheetPlacementDefinition> Placements { get; }

        private static IReadOnlyList<SemanticSheetPlacementDefinition> SnapshotPlacements(IEnumerable<SemanticSheetPlacementDefinition> placements)
        {
            var result = new List<SemanticSheetPlacementDefinition>(SemanticSheetPlanner.MaxPlacements);
            using (var enumerator = placements.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= SemanticSheetPlanner.MaxPlacements)
                        throw new InvalidOperationException("Semantic sheet supports at most " + SemanticSheetPlanner.MaxPlacements + " view placements.");
                    result.Add(enumerator.Current);
                }
            }
            return result.AsReadOnly();
        }
    }

    public sealed class SemanticSheetPlacementPlan
    {
        internal SemanticSheetPlacementPlan(string viewId, double xMm, double yMm, double widthMm, double heightMm)
        {
            ViewId = viewId;
            Xmm = xMm;
            Ymm = yMm;
            WidthMm = widthMm;
            HeightMm = heightMm;
        }

        public string ViewId { get; }
        public double Xmm { get; }
        public double Ymm { get; }
        public double WidthMm { get; }
        public double HeightMm { get; }
    }

    public sealed class SemanticSheetPlan
    {
        internal SemanticSheetPlan(
            string id,
            string number,
            string name,
            double widthMm,
            double heightMm,
            string? titleBlockName,
            IReadOnlyList<SemanticSheetPlacementPlan> placements)
        {
            Id = id;
            Number = number;
            Name = name;
            WidthMm = widthMm;
            HeightMm = heightMm;
            TitleBlockName = titleBlockName;
            Placements = new List<SemanticSheetPlacementPlan>(placements).AsReadOnly();
        }

        public string Id { get; }
        public string Number { get; }
        public string Name { get; }
        public double WidthMm { get; }
        public double HeightMm { get; }
        public string? TitleBlockName { get; }
        public IReadOnlyList<SemanticSheetPlacementPlan> Placements { get; }
    }

    public static class SemanticSheetPlanner
    {
        private const int MaxCatalogSheets = 10000;
        private const int MaxAvailableViews = 10000;
        internal const int MaxPlacements = 128;
        private const int MaxIdLength = 128;
        private const int MaxNameLength = 160;
        private const int MaxNumberLength = 64;
        private const int MaxTitleBlockLength = 160;

        public static SemanticSheetPlan Build(SemanticSheetDefinition definition, IEnumerable<SemanticViewPlan> availableViews)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (availableViews == null) throw new ArgumentNullException(nameof(availableViews));

            var id = Required(definition.Id, nameof(definition.Id), MaxIdLength);
            var number = Required(definition.Number, nameof(definition.Number), MaxNumberLength);
            var name = Required(definition.Name, nameof(definition.Name), MaxNameLength);
            var titleBlockName = Optional(definition.TitleBlockName, nameof(definition.TitleBlockName), MaxTitleBlockLength);
            PositiveFinite(definition.WidthMm, nameof(definition.WidthMm));
            PositiveFinite(definition.HeightMm, nameof(definition.HeightMm));

            var views = MaterializeAvailableViewsBounded(availableViews);
            var viewIndex = BuildUniqueViewIndex(views);
            return BuildValidated(definition, viewIndex, id, number, name, titleBlockName);
        }

        public static IReadOnlyList<SemanticSheetPlan> BuildCatalog(
            IEnumerable<SemanticSheetDefinition> definitions,
            IEnumerable<SemanticViewPlan> availableViews)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (availableViews == null) throw new ArgumentNullException(nameof(availableViews));

            var views = MaterializeAvailableViewsBounded(availableViews);
            var viewIndex = BuildUniqueViewIndex(views);
            var materialized = MaterializeCatalogBounded(definitions);

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var numbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var plans = new List<SemanticSheetPlan>(materialized.Count);
            foreach (var definition in materialized)
            {
                if (definition == null) throw new ArgumentException("Semantic sheet definition cannot be null.", nameof(definitions));
                var plan = BuildCore(definition, viewIndex);
                if (!ids.Add(plan.Id)) throw new InvalidOperationException("Semantic sheet catalog contains duplicate sheet id: " + plan.Id + ".");
                if (!numbers.Add(plan.Number)) throw new InvalidOperationException("Semantic sheet catalog contains duplicate sheet number: " + plan.Number + ".");
                plans.Add(plan);
            }

            return plans
                .OrderBy(x => x.Number, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        private static SemanticSheetPlan BuildCore(
            SemanticSheetDefinition definition,
            Dictionary<string, SemanticViewPlan> viewIndex)
        {
            var id = Required(definition.Id, nameof(definition.Id), MaxIdLength);
            var number = Required(definition.Number, nameof(definition.Number), MaxNumberLength);
            var name = Required(definition.Name, nameof(definition.Name), MaxNameLength);
            var titleBlockName = Optional(definition.TitleBlockName, nameof(definition.TitleBlockName), MaxTitleBlockLength);
            PositiveFinite(definition.WidthMm, nameof(definition.WidthMm));
            PositiveFinite(definition.HeightMm, nameof(definition.HeightMm));
            return BuildValidated(definition, viewIndex, id, number, name, titleBlockName);
        }

        private static SemanticSheetPlan BuildValidated(
            SemanticSheetDefinition definition,
            Dictionary<string, SemanticViewPlan> viewIndex,
            string id,
            string number,
            string name,
            string? titleBlockName)
        {
            if (definition.Placements.Count > MaxPlacements)
                throw new InvalidOperationException("Semantic sheet supports at most " + MaxPlacements + " view placements.");

            var placedViews = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var placements = new List<SemanticSheetPlacementPlan>(definition.Placements.Count);
            for (var i = 0; i < definition.Placements.Count; i++)
            {
                var placement = definition.Placements[i] ?? throw new ArgumentException("Semantic sheet placement cannot be null at index " + i + ".", nameof(definition));
                var viewId = Required(placement.ViewId, "placements[" + i + "].ViewId", MaxIdLength);
                if (!viewIndex.TryGetValue(viewId, out var view)) throw new InvalidOperationException("Semantic sheet references missing view id: " + viewId + ".");
                if (view.Kind == SemanticViewKind.Schedule)
                    throw new InvalidOperationException("Semantic sheet cannot place schedule view id as a sheet view: " + viewId + ".");
                if (!placedViews.Add(viewId)) throw new InvalidOperationException("Semantic sheet cannot place the same view more than once: " + viewId + ".");

                NonNegativeFinite(placement.Xmm, "placements[" + i + "].Xmm");
                NonNegativeFinite(placement.Ymm, "placements[" + i + "].Ymm");
                PositiveFinite(placement.WidthMm, "placements[" + i + "].WidthMm");
                PositiveFinite(placement.HeightMm, "placements[" + i + "].HeightMm");

                if (!FitsWithin(placement.Xmm, placement.WidthMm, definition.WidthMm) ||
                    !FitsWithin(placement.Ymm, placement.HeightMm, definition.HeightMm))
                    throw new InvalidOperationException("Semantic sheet placement is outside the paper bounds for view id: " + viewId + ".");

                var plan = new SemanticSheetPlacementPlan(viewId, placement.Xmm, placement.Ymm, placement.WidthMm, placement.HeightMm);
                foreach (var existing in placements)
                    if (Overlaps(existing, plan))
                        throw new InvalidOperationException("Semantic sheet view placements overlap: " + existing.ViewId + " and " + viewId + ".");
                placements.Add(plan);
            }

            var ordered = placements
                .OrderBy(x => x.Ymm)
                .ThenBy(x => x.Xmm)
                .ThenBy(x => x.ViewId, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new SemanticSheetPlan(id, number, name, definition.WidthMm, definition.HeightMm, titleBlockName, ordered);
        }

        private static List<SemanticSheetDefinition> MaterializeCatalogBounded(IEnumerable<SemanticSheetDefinition> definitions)
        {
            RequireKnownCountsWithinLimit(definitions, MaxCatalogSheets, "Semantic sheet catalog", "sheets");

            var result = new List<SemanticSheetDefinition>(Math.Min(MaxCatalogSheets, 256));
            using (var enumerator = definitions.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= MaxCatalogSheets)
                        throw new InvalidOperationException("Semantic sheet catalog supports at most " + MaxCatalogSheets + " sheets.");
                    result.Add(enumerator.Current);
                }
            }
            return result;
        }

        private static List<SemanticViewPlan> MaterializeAvailableViewsBounded(IEnumerable<SemanticViewPlan> availableViews)
        {
            RequireKnownCountsWithinLimit(availableViews, MaxAvailableViews, "Semantic sheet planner", "available views");

            var result = new List<SemanticViewPlan>(Math.Min(MaxAvailableViews, 256));
            using (var enumerator = availableViews.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= MaxAvailableViews)
                        throw new InvalidOperationException("Semantic sheet planner supports at most " + MaxAvailableViews + " available views.");
                    result.Add(enumerator.Current);
                }
            }
            return result;
        }

        private static void RequireKnownCountsWithinLimit<T>(IEnumerable<T> values, int limit, string owner, string itemLabel)
        {
            var knownCounts = new List<int>(3);
            if (values is ICollection<T> collection) knownCounts.Add(collection.Count);
            if (values is IReadOnlyCollection<T> readOnlyCollection) knownCounts.Add(readOnlyCollection.Count);
            if (values is ICollection nonGenericCollection) knownCounts.Add(nonGenericCollection.Count);

            for (var i = 0; i < knownCounts.Count; i++)
            {
                var count = knownCounts[i];
                if (count < 0)
                    throw new InvalidOperationException(owner + " received an invalid negative known count for " + itemLabel + ".");
                if (count > limit)
                    throw new InvalidOperationException(owner + " supports at most " + limit + " " + itemLabel + ".");
            }

            if (knownCounts.Count <= 1) return;
            var expected = knownCounts[0];
            for (var i = 1; i < knownCounts.Count; i++)
                if (knownCounts[i] != expected)
                    throw new InvalidOperationException(owner + " received conflicting known counts for " + itemLabel + ".");
        }

        private static Dictionary<string, SemanticViewPlan> BuildUniqueViewIndex(IEnumerable<SemanticViewPlan> views)
        {
            var result = new Dictionary<string, SemanticViewPlan>(StringComparer.OrdinalIgnoreCase);
            foreach (var view in views)
            {
                if (view == null) throw new ArgumentException("Available semantic view cannot be null.", nameof(views));
                var id = Required(view.Id, "availableViews.Id", MaxIdLength);
                if (result.ContainsKey(id)) throw new InvalidOperationException("Available semantic views contain duplicate id: " + id + ".");
                result.Add(id, view);
            }
            return result;
        }

        private static bool Overlaps(SemanticSheetPlacementPlan a, SemanticSheetPlacementPlan b) =>
            AxisOverlaps(a.Xmm, a.WidthMm, b.Xmm, b.WidthMm) &&
            AxisOverlaps(a.Ymm, a.HeightMm, b.Ymm, b.HeightMm);

        private static bool AxisOverlaps(double aStart, double aExtent, double bStart, double bExtent)
        {
            if (aStart <= bStart) return SeparationWithinExtent(bStart - aStart, aExtent);
            return SeparationWithinExtent(aStart - bStart, bExtent);
        }

        private static bool SeparationWithinExtent(double separation, double leadingExtent) =>
            !double.IsNaN(separation) && !double.IsInfinity(separation) && separation < leadingExtent;

        private static bool FitsWithin(double start, double extent, double limit)
        {
            if (start > limit) return false;
            return extent <= limit - start;
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

        private static string Required(string? value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value!.Trim();
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }

        private static string? Optional(string? value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var normalized = value!.Trim();
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }
    }
}
