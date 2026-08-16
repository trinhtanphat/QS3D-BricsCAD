using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Cost
{
    public enum RateReferenceTargetKind
    {
        BillItem = 0,
        UnitRate = 1
    }

    public sealed class RateReferenceEdge
    {
        public RateReferenceEdge(string sourceRateCode, RateReferenceTargetKind targetKind, string targetId)
        {
            SourceRateCode = RateBookContract.RequireToken(sourceRateCode, nameof(sourceRateCode));
            if (!Enum.IsDefined(typeof(RateReferenceTargetKind), targetKind))
                throw new ArgumentOutOfRangeException(nameof(targetKind));
            TargetKind = targetKind;
            TargetId = RateBookContract.RequireToken(targetId, nameof(targetId));
        }

        public string SourceRateCode { get; }
        public RateReferenceTargetKind TargetKind { get; }
        public string TargetId { get; }
    }

    public sealed class RateReferenceMark
    {
        internal RateReferenceMark(string rateCode, bool usedInBillItems, bool usedInUnitRates)
        {
            RateCode = rateCode;
            UsedInBillItems = usedInBillItems;
            UsedInUnitRates = usedInUnitRates;
        }

        public string RateCode { get; }
        public bool UsedInBillItems { get; }
        public bool UsedInUnitRates { get; }
        public bool IsUnused => !UsedInBillItems && !UsedInUnitRates;
    }

    public sealed class RateReferenceGraph
    {
        private readonly IReadOnlyList<RateReferenceEdge> _edges;

        public RateReferenceGraph(IEnumerable<RateReferenceEdge> edges)
        {
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            var snapshot = new List<RateReferenceEdge>();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var edge in edges)
            {
                if (edge == null)
                    throw new ArgumentException("Rate reference graph contains a null edge at index " + index + ".", nameof(edges));
                var key = edge.SourceRateCode + "\u001f" + ((int)edge.TargetKind) + "\u001f" + edge.TargetId;
                if (!keys.Add(key))
                    throw new ArgumentException("Duplicate rate reference edge: " + key + ".", nameof(edges));
                snapshot.Add(edge);
                index++;
            }
            snapshot.Sort(CompareEdges);
            _edges = new ReadOnlyCollection<RateReferenceEdge>(snapshot.ToArray());
        }

        public IReadOnlyList<RateReferenceEdge> Edges => _edges;

        public RateReferenceMark GetMark(string rateCode)
        {
            rateCode = RateBookContract.RequireToken(rateCode, nameof(rateCode));
            var usedInBillItems = false;
            var usedInUnitRates = false;
            for (var i = 0; i < _edges.Count; i++)
            {
                var edge = _edges[i];
                if (!StringComparer.OrdinalIgnoreCase.Equals(edge.SourceRateCode, rateCode)) continue;
                if (edge.TargetKind == RateReferenceTargetKind.BillItem) usedInBillItems = true;
                if (edge.TargetKind == RateReferenceTargetKind.UnitRate) usedInUnitRates = true;
            }
            return new RateReferenceMark(rateCode, usedInBillItems, usedInUnitRates);
        }

        public IReadOnlyList<string> GetReverseReferences(string rateCode, RateReferenceTargetKind kind)
        {
            rateCode = RateBookContract.RequireToken(rateCode, nameof(rateCode));
            if (!Enum.IsDefined(typeof(RateReferenceTargetKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            var result = new List<string>();
            for (var i = 0; i < _edges.Count; i++)
            {
                var edge = _edges[i];
                if (edge.TargetKind == kind && StringComparer.OrdinalIgnoreCase.Equals(edge.SourceRateCode, rateCode))
                    result.Add(edge.TargetId);
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return new ReadOnlyCollection<string>(result.ToArray());
        }

        private static int CompareEdges(RateReferenceEdge left, RateReferenceEdge right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.SourceRateCode, right.SourceRateCode);
            if (compare != 0) return compare;
            compare = left.TargetKind.CompareTo(right.TargetKind);
            if (compare != 0) return compare;
            return StringComparer.OrdinalIgnoreCase.Compare(left.TargetId, right.TargetId);
        }
    }

    public sealed class BuildUpRateSnapshot
    {
        public BuildUpRateSnapshot(string rateCode, decimal unitRate)
        {
            RateCode = RateBookContract.RequireToken(rateCode, nameof(rateCode));
            if (unitRate < 0m) throw new ArgumentOutOfRangeException(nameof(unitRate));
            UnitRate = unitRate;
        }

        public string RateCode { get; }
        public decimal UnitRate { get; }
    }

    public sealed class BuildUpAnalysisLine
    {
        internal BuildUpAnalysisLine(
            BuildUpRateSnapshot rate,
            RateReferenceMark mark,
            IReadOnlyList<string> billItems,
            IReadOnlyList<string> unitRates)
        {
            Rate = rate;
            Mark = mark;
            BillItems = billItems;
            UnitRates = unitRates;
        }

        public BuildUpRateSnapshot Rate { get; }
        public RateReferenceMark Mark { get; }
        public IReadOnlyList<string> BillItems { get; }
        public IReadOnlyList<string> UnitRates { get; }
    }

    public sealed class BuildUpAnalysisService
    {
        public IReadOnlyList<BuildUpAnalysisLine> Analyze(
            IEnumerable<BuildUpRateSnapshot> rates,
            RateReferenceGraph references,
            bool adoptedOnly = true)
        {
            if (rates == null) throw new ArgumentNullException(nameof(rates));
            if (references == null) throw new ArgumentNullException(nameof(references));
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<BuildUpAnalysisLine>();
            var index = 0;
            foreach (var rate in rates)
            {
                if (rate == null)
                    throw new ArgumentException("Build-up analysis contains a null rate at index " + index + ".", nameof(rates));
                if (!ids.Add(rate.RateCode))
                    throw new ArgumentException("Duplicate build-up rate code: " + rate.RateCode + ".", nameof(rates));
                var mark = references.GetMark(rate.RateCode);
                if (!adoptedOnly || !mark.IsUnused)
                {
                    result.Add(new BuildUpAnalysisLine(
                        rate,
                        mark,
                        references.GetReverseReferences(rate.RateCode, RateReferenceTargetKind.BillItem),
                        references.GetReverseReferences(rate.RateCode, RateReferenceTargetKind.UnitRate)));
                }
                index++;
            }
            result.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Rate.RateCode, right.Rate.RateCode));
            return new ReadOnlyCollection<BuildUpAnalysisLine>(result.ToArray());
        }
    }

    public sealed class CostAdjustmentResult
    {
        internal CostAdjustmentResult(
            decimal baseTotal,
            decimal adjustmentRatioPercent,
            decimal markupRatioPercent,
            decimal adjustedTotal,
            decimal combinedRatioPercent)
        {
            BaseTotal = baseTotal;
            AdjustmentRatioPercent = adjustmentRatioPercent;
            MarkupRatioPercent = markupRatioPercent;
            AdjustedTotal = adjustedTotal;
            CombinedRatioPercent = combinedRatioPercent;
        }

        public decimal BaseTotal { get; }
        public decimal AdjustmentRatioPercent { get; }
        public decimal MarkupRatioPercent { get; }
        public decimal AdjustedTotal { get; }
        public decimal CombinedRatioPercent { get; }
    }

    public sealed class CostAdjustmentService
    {
        public CostAdjustmentResult AdjustByRatios(decimal baseTotal, decimal adjustmentRatioPercent, decimal markupRatioPercent)
        {
            if (baseTotal < 0m) throw new ArgumentOutOfRangeException(nameof(baseTotal));
            if (adjustmentRatioPercent < -100m) throw new ArgumentOutOfRangeException(nameof(adjustmentRatioPercent));
            if (markupRatioPercent < -100m) throw new ArgumentOutOfRangeException(nameof(markupRatioPercent));
            var adjustmentRatio = ScaleRatioPercent(adjustmentRatioPercent, nameof(adjustmentRatioPercent));
            var markupRatio = ScaleRatioPercent(markupRatioPercent, nameof(markupRatioPercent));
            checked
            {
                var afterAdjustment = CostDecimalMath.MultiplyPreservingNonZero(
                    baseTotal,
                    1m + adjustmentRatio,
                    "cost adjustment after adjustment ratio");
                var adjustedTotal = CostDecimalMath.MultiplyPreservingNonZero(
                    afterAdjustment,
                    1m + markupRatio,
                    "cost adjustment after markup ratio");
                var combined = baseTotal == 0m
                    ? (adjustedTotal == 0m ? 0m : throw new InvalidOperationException("A zero base total cannot produce a non-zero adjusted total."))
                    : CalculateCombinedRatioPercent(baseTotal, adjustedTotal);
                return new CostAdjustmentResult(baseTotal, adjustmentRatioPercent, markupRatioPercent, adjustedTotal, combined);
            }
        }

        public CostAdjustmentResult AdjustToTotal(decimal baseTotal, decimal adjustedTotal)
        {
            if (baseTotal < 0m) throw new ArgumentOutOfRangeException(nameof(baseTotal));
            if (adjustedTotal < 0m) throw new ArgumentOutOfRangeException(nameof(adjustedTotal));
            if (baseTotal == 0m && adjustedTotal != 0m)
                throw new InvalidOperationException("A zero base total cannot produce a non-zero adjusted total.");
            var combined = baseTotal == 0m ? 0m : CalculateCombinedRatioPercent(baseTotal, adjustedTotal);
            return new CostAdjustmentResult(baseTotal, combined, 0m, adjustedTotal, combined);
        }

        private static decimal ScaleRatioPercent(decimal value, string paramName)
        {
            var ratio = value / 100m;
            if (value != 0m && ratio == 0m)
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    "Non-zero percentage is too small to preserve at decimal precision.");
            return ratio;
        }

        private static decimal CalculateCombinedRatioPercent(decimal baseTotal, decimal adjustedTotal)
        {
            var delta = adjustedTotal - baseTotal;
            if (delta == 0m) return 0m;

            try
            {
                var scaledDelta = CostDecimalMath.MultiplyPreservingNonZero(delta, 100m, "cost adjustment combined ratio scaled delta");
                return CostDecimalMath.DividePreservingNonZero(scaledDelta, baseTotal, "cost adjustment combined ratio percent");
            }
            catch (OverflowException)
            {
                var ratio = CostDecimalMath.DividePreservingNonZero(delta, baseTotal, "cost adjustment combined ratio ratio");
                return CostDecimalMath.MultiplyPreservingNonZero(ratio, 100m, "cost adjustment combined ratio percent");
            }
        }
    }

    public sealed class TradeCostItem
    {
        public TradeCostItem(string itemCode, string? tradeCode, decimal cost)
        {
            ItemCode = RateBookContract.RequireToken(itemCode, nameof(itemCode));
            if (cost < 0m) throw new ArgumentOutOfRangeException(nameof(cost));
            var normalizedTradeCode = tradeCode?.Trim();
            TradeCode = normalizedTradeCode == null || normalizedTradeCode.Length == 0 ? "Unclassified" : normalizedTradeCode;
            Cost = cost;
        }

        public string ItemCode { get; }
        public string TradeCode { get; }
        public decimal Cost { get; }
    }

    public sealed class TradeCostAnalysisRow
    {
        internal TradeCostAnalysisRow(string tradeCode, int itemCount, decimal totalCost, decimal cfaM2)
        {
            TradeCode = tradeCode;
            ItemCount = itemCount;
            TotalCost = totalCost;
            CostPerCfaM2 = cfaM2 == 0m
                ? (decimal?)null
                : CostDecimalMath.DividePreservingNonZero(totalCost, cfaM2, "trade cost per CFA");
        }

        public string TradeCode { get; }
        public int ItemCount { get; }
        public decimal TotalCost { get; }
        public decimal? CostPerCfaM2 { get; }
    }

    public sealed class TradeCostAnalysisService
    {
        public IReadOnlyList<TradeCostAnalysisRow> Analyze(IEnumerable<TradeCostItem> items, decimal cfaM2)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (cfaM2 < 0m) throw new ArgumentOutOfRangeException(nameof(cfaM2));
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var totals = new Dictionary<string, TradeAggregate>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var item in items)
            {
                if (item == null)
                    throw new ArgumentException("Trade analysis contains a null item at index " + index + ".", nameof(items));
                if (!ids.Add(item.ItemCode))
                    throw new ArgumentException("Duplicate trade-analysis item code: " + item.ItemCode + ".", nameof(items));
                if (!totals.TryGetValue(item.TradeCode, out var aggregate))
                {
                    aggregate = new TradeAggregate(item.TradeCode);
                    totals.Add(item.TradeCode, aggregate);
                }
                else if (string.CompareOrdinal(item.TradeCode, aggregate.TradeCode) < 0)
                {
                    aggregate.TradeCode = item.TradeCode;
                }
                checked
                {
                    aggregate.ItemCount++;
                    aggregate.TotalCost += item.Cost;
                }
                index++;
            }
            var rows = new List<TradeCostAnalysisRow>(totals.Count);
            foreach (var aggregate in totals.Values)
                rows.Add(new TradeCostAnalysisRow(aggregate.TradeCode, aggregate.ItemCount, aggregate.TotalCost, cfaM2));
            rows.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.TradeCode, right.TradeCode));
            return new ReadOnlyCollection<TradeCostAnalysisRow>(rows.ToArray());
        }

        private sealed class TradeAggregate
        {
            internal TradeAggregate(string tradeCode) { TradeCode = tradeCode; }
            internal string TradeCode { get; set; }
            internal int ItemCount { get; set; }
            internal decimal TotalCost { get; set; }
        }
    }

    public sealed class BqLibraryEntry
    {
        public BqLibraryEntry(
            string itemCode,
            string description,
            string unit,
            string categoryPath,
            decimal? referenceUnitRate = null)
        {
            ItemCode = RateBookContract.RequireToken(itemCode, nameof(itemCode));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("BQ library description is required.", nameof(description));
            Description = description.Trim();
            Unit = RateBookContract.RequireLowerToken(unit, nameof(unit));
            if (string.IsNullOrWhiteSpace(categoryPath))
                throw new ArgumentException("BQ library category path is required.", nameof(categoryPath));
            CategoryPath = categoryPath.Trim();
            if (referenceUnitRate.HasValue && referenceUnitRate.Value < 0m)
                throw new ArgumentOutOfRangeException(nameof(referenceUnitRate));
            ReferenceUnitRate = referenceUnitRate;
        }

        public string ItemCode { get; }
        public string Description { get; }
        public string Unit { get; }
        public string CategoryPath { get; }
        public decimal? ReferenceUnitRate { get; }
    }

    public sealed class BqLibraryCatalog
    {
        private readonly IReadOnlyList<BqLibraryEntry> _entries;

        public BqLibraryCatalog(string libraryId, IEnumerable<BqLibraryEntry> entries)
        {
            LibraryId = RateBookContract.RequireToken(libraryId, nameof(libraryId));
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var snapshot = new List<BqLibraryEntry>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var entry in entries)
            {
                if (entry == null)
                    throw new ArgumentException("BQ library contains a null entry at index " + index + ".", nameof(entries));
                if (!ids.Add(entry.ItemCode))
                    throw new ArgumentException("Duplicate BQ library item code: " + entry.ItemCode + ".", nameof(entries));
                snapshot.Add(entry);
                index++;
            }
            snapshot.Sort(CompareEntries);
            _entries = new ReadOnlyCollection<BqLibraryEntry>(snapshot.ToArray());
        }

        public string LibraryId { get; }
        public IReadOnlyList<BqLibraryEntry> Entries => _entries;

        public BqLibraryCatalog ImportFromProject(IEnumerable<BqLibraryEntry> projectEntries, bool replaceExisting)
        {
            if (projectEntries == null) throw new ArgumentNullException(nameof(projectEntries));
            var merged = new Dictionary<string, BqLibraryEntry>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < _entries.Count; i++) merged.Add(_entries[i].ItemCode, _entries[i]);
            var incomingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var entry in projectEntries)
            {
                if (entry == null)
                    throw new ArgumentException("Project import contains a null BQ entry at index " + index + ".", nameof(projectEntries));
                if (!incomingIds.Add(entry.ItemCode))
                    throw new ArgumentException("Project import contains duplicate BQ item code: " + entry.ItemCode + ".", nameof(projectEntries));
                if (merged.ContainsKey(entry.ItemCode) && !replaceExisting)
                    throw new InvalidOperationException("BQ library import would overwrite existing item " + entry.ItemCode + ".");
                merged[entry.ItemCode] = entry;
                index++;
            }
            return new BqLibraryCatalog(LibraryId, merged.Values);
        }

        private static int CompareEntries(BqLibraryEntry left, BqLibraryEntry right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.CategoryPath, right.CategoryPath);
            if (compare != 0) return compare;
            return StringComparer.OrdinalIgnoreCase.Compare(left.ItemCode, right.ItemCode);
        }
    }
}
