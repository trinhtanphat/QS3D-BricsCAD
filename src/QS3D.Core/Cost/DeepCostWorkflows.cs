using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;

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
        private const int MaximumEdges = TbqProjectWorkspaceState.MaxRateReferences;
        private readonly IReadOnlyList<RateReferenceEdge> _edges;

        public RateReferenceGraph(IEnumerable<RateReferenceEdge> edges)
        {
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            var knownCount = ValidateKnownCount(edges);
            var snapshot = new List<RateReferenceEdge>();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            using (var edgeEnumerator = edges.GetEnumerator())
            {
                while (true)
                {
                    if (knownCount.HasValue) RequireKnownCountStableDuringTraversal(edges, knownCount.Value);
                    if (!edgeEnumerator.MoveNext()) break;
                    if (knownCount.HasValue) RequireKnownCountStableDuringTraversal(edges, knownCount.Value);
                    if (knownCount.HasValue && index == knownCount.Value)
                        throw new ArgumentException("Rate reference edge collection contains more entries than its known count.", nameof(edges));
                    if (index == MaximumEdges) ThrowTooManyEdges();
                    var edge = edgeEnumerator.Current;
                    if (knownCount.HasValue) RequireKnownCountStableDuringTraversal(edges, knownCount.Value);
                    if (edge == null) throw new ArgumentException("Rate reference graph contains a null edge at index " + index + ".", nameof(edges));
                    var key = edge.SourceRateCode + "\u001f" + ((int)edge.TargetKind) + "\u001f" + edge.TargetId;
                    if (!keys.Add(key)) throw new ArgumentException("Duplicate rate reference edge: " + key + ".", nameof(edges));
                    snapshot.Add(edge);
                    index++;
                }
            }
            if (knownCount.HasValue && index != knownCount.Value)
                throw new ArgumentException("Rate reference edge collection known count does not match the observed traversal.", nameof(edges));
            if (knownCount.HasValue)
            {
                RequireKnownCountStableAfterTraversal(edges, knownCount.Value);
                RequireStableEdgeGeneration(edges, knownCount.Value, snapshot);
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
            if (!Enum.IsDefined(typeof(RateReferenceTargetKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            var result = new List<string>();
            for (var i = 0; i < _edges.Count; i++)
            {
                var edge = _edges[i];
                if (edge.TargetKind == kind && StringComparer.OrdinalIgnoreCase.Equals(edge.SourceRateCode, rateCode)) result.Add(edge.TargetId);
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return new ReadOnlyCollection<string>(result.ToArray());
        }

        private static int? ValidateKnownCount(IEnumerable<RateReferenceEdge> edges)
        {
            var counts = new List<int>(3);
            if (edges is ICollection<RateReferenceEdge> collection) counts.Add(collection.Count);
            if (edges is IReadOnlyCollection<RateReferenceEdge> readOnlyCollection) counts.Add(readOnlyCollection.Count);
            if (edges is ICollection nonGenericCollection) counts.Add(nonGenericCollection.Count);
            if (counts.Count == 0) return null;
            var expected = counts[0];
            var maximumReported = expected;
            var hasNegative = expected < 0;
            var hasConflict = false;
            for (var i = 1; i < counts.Count; i++)
            {
                var current = counts[i];
                if (current < 0) hasNegative = true;
                if (current != expected) hasConflict = true;
                if (current > maximumReported) maximumReported = current;
            }
            if (maximumReported > MaximumEdges) ThrowTooManyEdges();
            if (hasNegative) throw new ArgumentException("Rate reference edge collection reports an invalid negative known count.", nameof(edges));
            if (hasConflict) throw new ArgumentException("Rate reference edge collection reports conflicting known counts.", nameof(edges));
            return expected;
        }

        private static void RequireKnownCountStableDuringTraversal(IEnumerable<RateReferenceEdge> edges, int admittedKnownCount)
        {
            var reboundKnownCount = ValidateKnownCount(edges);
            if (!reboundKnownCount.HasValue || reboundKnownCount.Value != admittedKnownCount)
                throw new ArgumentException("Rate reference edge collection known count changed during traversal.", nameof(edges));
        }

        private static void RequireKnownCountStableAfterTraversal(IEnumerable<RateReferenceEdge> edges, int admittedKnownCount)
        {
            RequireKnownCountStableDuringTraversal(edges, admittedKnownCount);
        }

        private static void RequireStableEdgeGeneration(IEnumerable<RateReferenceEdge> edges, int admittedKnownCount, IReadOnlyList<RateReferenceEdge> admittedEdges)
        {
            var index = 0;
            using (var edgeEnumerator = edges.GetEnumerator())
            {
                RequireKnownCountStableDuringTraversal(edges, admittedKnownCount);
                while (true)
                {
                    RequireKnownCountStableDuringTraversal(edges, admittedKnownCount);
                    if (!edgeEnumerator.MoveNext()) break;
                    RequireKnownCountStableDuringTraversal(edges, admittedKnownCount);
                    if (index >= admittedEdges.Count) ThrowEdgeContentChanged();
                    var edge = edgeEnumerator.Current;
                    RequireKnownCountStableDuringTraversal(edges, admittedKnownCount);
                    if (edge == null || !SameEdgeState(admittedEdges[index], edge)) ThrowEdgeContentChanged();
                    index++;
                }
            }
            if (index != admittedEdges.Count) ThrowEdgeContentChanged();
            RequireKnownCountStableDuringTraversal(edges, admittedKnownCount);
        }

        private static bool SameEdgeState(RateReferenceEdge left, RateReferenceEdge right) =>
            string.Equals(left.SourceRateCode, right.SourceRateCode, StringComparison.Ordinal) &&
            left.TargetKind == right.TargetKind &&
            string.Equals(left.TargetId, right.TargetId, StringComparison.Ordinal);

        private static void ThrowEdgeContentChanged() => throw new InvalidOperationException("Rate reference edge source content changed during traversal.");

        private static int CompareEdges(RateReferenceEdge left, RateReferenceEdge right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.SourceRateCode, right.SourceRateCode);
            if (compare != 0) return compare;
            compare = left.TargetKind.CompareTo(right.TargetKind);
            if (compare != 0) return compare;
            return StringComparer.OrdinalIgnoreCase.Compare(left.TargetId, right.TargetId);
        }

        private static void ThrowTooManyEdges() => throw new InvalidOperationException("Rate reference edge collection supports at most " + MaximumEdges + " entries.");
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
        internal BuildUpAnalysisLine(BuildUpRateSnapshot rate, RateReferenceMark mark, IReadOnlyList<string> billItems, IReadOnlyList<string> unitRates)
        {
            Rate = rate; Mark = mark; BillItems = billItems; UnitRates = unitRates;
        }
        public BuildUpRateSnapshot Rate { get; }
        public RateReferenceMark Mark { get; }
        public IReadOnlyList<string> BillItems { get; }
        public IReadOnlyList<string> UnitRates { get; }
    }

    public sealed class BuildUpAnalysisService
    {
        public IReadOnlyList<BuildUpAnalysisLine> Analyze(IEnumerable<BuildUpRateSnapshot> rates, RateReferenceGraph references, bool adoptedOnly = true)
        {
            if (rates == null) throw new ArgumentNullException(nameof(rates));
            if (references == null) throw new ArgumentNullException(nameof(references));
            var hasKnownRateCount = AdvancedCostCollectionContract.TryGetKnownCount(rates, out var knownRateCount);
            if (hasKnownRateCount && knownRateCount > AdvancedCostCollectionContract.MaximumEntries)
                AdvancedCostCollectionContract.ThrowTooManyEntries("Build-up analysis rate collection");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<BuildUpAnalysisLine>();
            var admittedRates = hasKnownRateCount ? new List<BuildUpRateSnapshot>(knownRateCount) : null;
            var index = 0;
            using (var rateEnumerator = rates.GetEnumerator())
            {
                while (true)
                {
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(rates, hasKnownRateCount, knownRateCount, "Build-up analysis rate collection");
                    if (!rateEnumerator.MoveNext()) break;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(rates, hasKnownRateCount, knownRateCount, "Build-up analysis rate collection");
                    AdvancedCostCollectionContract.RequireCanProcessNext(hasKnownRateCount, knownRateCount, index, "Build-up analysis rate collection");
                    var rate = rateEnumerator.Current;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(rates, hasKnownRateCount, knownRateCount, "Build-up analysis rate collection");
                    if (rate == null) throw new ArgumentException("Build-up analysis contains a null rate at index " + index + ".", nameof(rates));
                    if (!ids.Add(rate.RateCode)) throw new ArgumentException("Duplicate build-up rate code: " + rate.RateCode + ".", nameof(rates));
                    admittedRates?.Add(rate);
                    var mark = references.GetMark(rate.RateCode);
                    if (!adoptedOnly || !mark.IsUnused)
                        result.Add(new BuildUpAnalysisLine(rate, mark, references.GetReverseReferences(rate.RateCode, RateReferenceTargetKind.BillItem), references.GetReverseReferences(rate.RateCode, RateReferenceTargetKind.UnitRate)));
                    index++;
                }
            }
            AdvancedCostCollectionContract.RequireKnownCountStableAfterTraversal(rates, hasKnownRateCount, knownRateCount, index, "Build-up analysis rate collection");
            if (hasKnownRateCount) RequireStableBuildUpGeneration(rates, knownRateCount, admittedRates!);
            result.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Rate.RateCode, right.Rate.RateCode));
            return new ReadOnlyCollection<BuildUpAnalysisLine>(result.ToArray());
        }

        private static void RequireStableBuildUpGeneration(IEnumerable<BuildUpRateSnapshot> rates, int admittedKnownCount, IReadOnlyList<BuildUpRateSnapshot> admittedRates)
        {
            var index = 0;
            using (var enumerator = rates.GetEnumerator())
            {
                while (true)
                {
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(rates, true, admittedKnownCount, "Build-up analysis rate collection");
                    if (!enumerator.MoveNext()) break;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(rates, true, admittedKnownCount, "Build-up analysis rate collection");
                    if (index >= admittedRates.Count) throw new InvalidOperationException("Build-up analysis rate collection content changed during traversal.");
                    var rate = enumerator.Current;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(rates, true, admittedKnownCount, "Build-up analysis rate collection");
                    if (rate == null || !SameBuildUpRateState(admittedRates[index], rate)) throw new InvalidOperationException("Build-up analysis rate collection content changed during traversal.");
                    index++;
                }
            }
            if (index != admittedRates.Count) throw new InvalidOperationException("Build-up analysis rate collection content changed during traversal.");
            AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(rates, true, admittedKnownCount, "Build-up analysis rate collection");
        }

        private static bool SameBuildUpRateState(BuildUpRateSnapshot left, BuildUpRateSnapshot right) =>
            string.Equals(left.RateCode, right.RateCode, StringComparison.Ordinal) && left.UnitRate == right.UnitRate;
    }

    public sealed class CostAdjustmentResult
    {
        internal CostAdjustmentResult(decimal baseTotal, decimal adjustmentRatioPercent, decimal markupRatioPercent, decimal adjustedTotal, decimal combinedRatioPercent)
        {
            BaseTotal = baseTotal; AdjustmentRatioPercent = adjustmentRatioPercent; MarkupRatioPercent = markupRatioPercent; AdjustedTotal = adjustedTotal; CombinedRatioPercent = combinedRatioPercent;
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
                var afterAdjustment = ApplyRatio(baseTotal, adjustmentRatioPercent, adjustmentRatio, nameof(adjustmentRatioPercent), "cost adjustment after adjustment ratio");
                var adjustedTotal = ApplyRatio(afterAdjustment, markupRatioPercent, markupRatio, nameof(markupRatioPercent), "cost adjustment after markup ratio");
                var combined = baseTotal == 0m ? (adjustedTotal == 0m ? 0m : throw new InvalidOperationException("A zero base total cannot produce a non-zero adjusted total.")) : CalculateCombinedRatioPercent(baseTotal, adjustedTotal);
                return new CostAdjustmentResult(baseTotal, adjustmentRatioPercent, markupRatioPercent, adjustedTotal, combined);
            }
        }

        public CostAdjustmentResult AdjustToTotal(decimal baseTotal, decimal adjustedTotal)
        {
            if (baseTotal < 0m) throw new ArgumentOutOfRangeException(nameof(baseTotal));
            if (adjustedTotal < 0m) throw new ArgumentOutOfRangeException(nameof(adjustedTotal));
            if (baseTotal == 0m && adjustedTotal != 0m) throw new InvalidOperationException("A zero base total cannot produce a non-zero adjusted total.");
            var combined = baseTotal == 0m ? 0m : CalculateCombinedRatioPercent(baseTotal, adjustedTotal);
            return new CostAdjustmentResult(baseTotal, combined, 0m, adjustedTotal, combined);
        }

        private static decimal ScaleRatioPercent(decimal value, string paramName)
        {
            var ratio = value / 100m;
            if (value != 0m && ratio == 0m) throw new ArgumentOutOfRangeException(paramName, value, "Non-zero percentage is too small to preserve at decimal precision.");
            return ratio;
        }

        private static decimal ApplyRatio(decimal value, decimal ratioPercent, decimal ratio, string paramName, string operation)
        {
            var result = CostDecimalMath.MultiplyPreservingNonZero(value, 1m + ratio, operation);
            if (value != 0m && ratioPercent != 0m && result == value) throw new ArgumentOutOfRangeException(paramName, ratioPercent, "Non-zero percentage is too small to affect the value at decimal precision.");
            return result;
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
            TradeCode = string.IsNullOrWhiteSpace(tradeCode) ? "Unclassified" : AdvancedCostTextContract.RequireCanonicalText(tradeCode!, nameof(tradeCode), "Trade code");
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
            TradeCode = tradeCode; ItemCount = itemCount; TotalCost = totalCost;
            CostPerCfaM2 = cfaM2 == 0m ? (decimal?)null : CostDecimalMath.DividePreservingNonZero(totalCost, cfaM2, "trade cost per CFA");
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
            var hasKnownItemCount = AdvancedCostCollectionContract.TryGetKnownCount(items, out var knownItemCount);
            if (hasKnownItemCount && knownItemCount > AdvancedCostCollectionContract.MaximumEntries)
                AdvancedCostCollectionContract.ThrowTooManyEntries("Trade analysis item collection");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var totals = new Dictionary<string, TradeAggregate>(StringComparer.OrdinalIgnoreCase);
            var admittedItems = hasKnownItemCount ? new List<TradeCostItem>(knownItemCount) : null;
            var index = 0;
            using (var itemEnumerator = items.GetEnumerator())
            {
                while (true)
                {
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(items, hasKnownItemCount, knownItemCount, "Trade analysis item collection");
                    if (!itemEnumerator.MoveNext()) break;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(items, hasKnownItemCount, knownItemCount, "Trade analysis item collection");
                    AdvancedCostCollectionContract.RequireCanProcessNext(hasKnownItemCount, knownItemCount, index, "Trade analysis item collection");
                    var item = itemEnumerator.Current;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(items, hasKnownItemCount, knownItemCount, "Trade analysis item collection");
                    if (item == null) throw new ArgumentException("Trade analysis contains a null item at index " + index + ".", nameof(items));
                    if (!ids.Add(item.ItemCode)) throw new ArgumentException("Duplicate trade-analysis item code: " + item.ItemCode + ".", nameof(items));
                    admittedItems?.Add(item);
                    if (!totals.TryGetValue(item.TradeCode, out var aggregate))
                    {
                        aggregate = new TradeAggregate(item.TradeCode);
                        totals.Add(item.TradeCode, aggregate);
                    }
                    else if (string.CompareOrdinal(item.TradeCode, aggregate.TradeCode) < 0) aggregate.TradeCode = item.TradeCode;
                    checked
                    {
                        aggregate.ItemCount++;
                        aggregate.TotalCost.Add(item.Cost);
                    }
                    index++;
                }
            }
            AdvancedCostCollectionContract.RequireKnownCountStableAfterTraversal(items, hasKnownItemCount, knownItemCount, index, "Trade analysis item collection");
            if (hasKnownItemCount) RequireStableTradeGeneration(items, knownItemCount, admittedItems!);
            var rows = new List<TradeCostAnalysisRow>(totals.Count);
            foreach (var aggregate in totals.Values) rows.Add(new TradeCostAnalysisRow(aggregate.TradeCode, aggregate.ItemCount, aggregate.TotalCost.ToDecimal(), cfaM2));
            rows.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.TradeCode, right.TradeCode));
            return new ReadOnlyCollection<TradeCostAnalysisRow>(rows.ToArray());
        }

        private static void RequireStableTradeGeneration(IEnumerable<TradeCostItem> items, int admittedKnownCount, IReadOnlyList<TradeCostItem> admittedItems)
        {
            var index = 0;
            using (var enumerator = items.GetEnumerator())
            {
                while (true)
                {
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(items, true, admittedKnownCount, "Trade analysis item collection");
                    if (!enumerator.MoveNext()) break;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(items, true, admittedKnownCount, "Trade analysis item collection");
                    if (index >= admittedItems.Count) throw new InvalidOperationException("Trade analysis item collection content changed during traversal.");
                    var item = enumerator.Current;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(items, true, admittedKnownCount, "Trade analysis item collection");
                    if (item == null || !SameTradeItemState(admittedItems[index], item)) throw new InvalidOperationException("Trade analysis item collection content changed during traversal.");
                    index++;
                }
            }
            if (index != admittedItems.Count) throw new InvalidOperationException("Trade analysis item collection content changed during traversal.");
            AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(items, true, admittedKnownCount, "Trade analysis item collection");
        }

        private static bool SameTradeItemState(TradeCostItem left, TradeCostItem right) =>
            string.Equals(left.ItemCode, right.ItemCode, StringComparison.Ordinal) &&
            string.Equals(left.TradeCode, right.TradeCode, StringComparison.Ordinal) &&
            left.Cost == right.Cost;

        private sealed class TradeAggregate
        {
            internal TradeAggregate(string tradeCode) { TradeCode = tradeCode; TotalCost = new ExactNonNegativeDecimalAccumulator(); }
            internal string TradeCode { get; set; }
            internal int ItemCount { get; set; }
            internal ExactNonNegativeDecimalAccumulator TotalCost { get; }
        }

        private sealed class ExactNonNegativeDecimalAccumulator
        {
            private static readonly BigInteger MaxDecimalCoefficient = (BigInteger.One << 96) - BigInteger.One;
            private BigInteger _coefficient;
            private int _scale;
            private bool _hasValue;
            internal void Add(decimal value)
            {
                if (value < 0m) throw new InvalidOperationException("Trade cost aggregate cannot contain a negative value.");
                var bits = decimal.GetBits(value);
                var flags = bits[3];
                if ((flags & int.MinValue) != 0) throw new InvalidOperationException("Trade cost aggregate cannot contain a negative value.");
                var scale = (flags >> 16) & 0x7f;
                var coefficient = new BigInteger((uint)bits[0]) | (new BigInteger((uint)bits[1]) << 32) | (new BigInteger((uint)bits[2]) << 64);
                if (!_hasValue) { _coefficient = coefficient; _scale = scale; _hasValue = true; return; }
                if (scale > _scale) { _coefficient *= PowerOfTen(scale - _scale); _scale = scale; }
                else if (scale < _scale) coefficient *= PowerOfTen(_scale - scale);
                _coefficient += coefficient;
            }
            internal decimal ToDecimal()
            {
                if (!_hasValue || _coefficient.IsZero) return 0m;
                var coefficient = _coefficient;
                var scale = _scale;
                while (scale > 0 && coefficient % 10 == 0) { coefficient /= 10; scale--; }
                if (coefficient < BigInteger.Zero || coefficient > MaxDecimalCoefficient) throw new OverflowException("Trade cost aggregate total exceeds the representable decimal range.");
                var low = unchecked((int)(uint)(coefficient & uint.MaxValue));
                var mid = unchecked((int)(uint)((coefficient >> 32) & uint.MaxValue));
                var high = unchecked((int)(uint)((coefficient >> 64) & uint.MaxValue));
                return new decimal(low, mid, high, false, (byte)scale);
            }
            private static BigInteger PowerOfTen(int exponent)
            {
                var result = BigInteger.One;
                for (var i = 0; i < exponent; i++) result *= 10;
                return result;
            }
        }
    }

    public sealed class BqLibraryEntry
    {
        public BqLibraryEntry(string itemCode, string description, string unit, string categoryPath, decimal? referenceUnitRate = null)
        {
            ItemCode = RateBookContract.RequireToken(itemCode, nameof(itemCode));
            Description = AdvancedCostTextContract.RequireCanonicalText(description, nameof(description), "BQ library description");
            Unit = RateBookContract.RequireLowerToken(unit, nameof(unit));
            CategoryPath = AdvancedCostTextContract.RequireCanonicalText(categoryPath, nameof(categoryPath), "BQ library category path");
            if (referenceUnitRate.HasValue && referenceUnitRate.Value < 0m) throw new ArgumentOutOfRangeException(nameof(referenceUnitRate));
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
            var hasKnownEntryCount = AdvancedCostCollectionContract.TryGetKnownCount(entries, out var knownEntryCount);
            if (hasKnownEntryCount && knownEntryCount > AdvancedCostCollectionContract.MaximumEntries)
                AdvancedCostCollectionContract.ThrowTooManyEntries("BQ library entry collection");
            var snapshot = new List<BqLibraryEntry>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            using (var entryEnumerator = entries.GetEnumerator())
            {
                while (true)
                {
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(entries, hasKnownEntryCount, knownEntryCount, "BQ library entry collection");
                    if (!entryEnumerator.MoveNext()) break;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(entries, hasKnownEntryCount, knownEntryCount, "BQ library entry collection");
                    AdvancedCostCollectionContract.RequireCanProcessNext(hasKnownEntryCount, knownEntryCount, index, "BQ library entry collection");
                    var entry = entryEnumerator.Current;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(entries, hasKnownEntryCount, knownEntryCount, "BQ library entry collection");
                    if (entry == null) throw new ArgumentException("BQ library contains a null entry at index " + index + ".", nameof(entries));
                    if (!ids.Add(entry.ItemCode)) throw new ArgumentException("Duplicate BQ library item code: " + entry.ItemCode + ".", nameof(entries));
                    snapshot.Add(entry);
                    index++;
                }
            }
            AdvancedCostCollectionContract.RequireKnownCountStableAfterTraversal(entries, hasKnownEntryCount, knownEntryCount, index, "BQ library entry collection");
            if (hasKnownEntryCount) RequireStableEntryGeneration(entries, knownEntryCount, snapshot, "BQ library entry collection");
            snapshot.Sort(CompareEntries);
            _entries = new ReadOnlyCollection<BqLibraryEntry>(snapshot.ToArray());
        }
        public string LibraryId { get; }
        public IReadOnlyList<BqLibraryEntry> Entries => _entries;

        public BqLibraryCatalog ImportFromProject(IEnumerable<BqLibraryEntry> projectEntries, bool replaceExisting)
        {
            if (projectEntries == null) throw new ArgumentNullException(nameof(projectEntries));
            var hasKnownProjectEntryCount = AdvancedCostCollectionContract.TryGetKnownCount(projectEntries, out var knownProjectEntryCount);
            if (hasKnownProjectEntryCount && knownProjectEntryCount > AdvancedCostCollectionContract.MaximumEntries)
                AdvancedCostCollectionContract.ThrowTooManyEntries("BQ project import collection");
            var merged = new Dictionary<string, BqLibraryEntry>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < _entries.Count; i++) merged.Add(_entries[i].ItemCode, _entries[i]);
            var incomingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var admittedEntries = new List<BqLibraryEntry>();
            var index = 0;
            using (var projectEntryEnumerator = projectEntries.GetEnumerator())
            {
                while (true)
                {
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(projectEntries, hasKnownProjectEntryCount, knownProjectEntryCount, "BQ project import collection");
                    if (!projectEntryEnumerator.MoveNext()) break;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(projectEntries, hasKnownProjectEntryCount, knownProjectEntryCount, "BQ project import collection");
                    AdvancedCostCollectionContract.RequireCanProcessNext(hasKnownProjectEntryCount, knownProjectEntryCount, index, "BQ project import collection");
                    var entry = projectEntryEnumerator.Current;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(projectEntries, hasKnownProjectEntryCount, knownProjectEntryCount, "BQ project import collection");
                    if (entry == null) throw new ArgumentException("Project import contains a null BQ entry at index " + index + ".", nameof(projectEntries));
                    if (!incomingIds.Add(entry.ItemCode)) throw new ArgumentException("Project import contains duplicate BQ item code: " + entry.ItemCode + ".", nameof(projectEntries));
                    if (merged.ContainsKey(entry.ItemCode) && !replaceExisting) throw new InvalidOperationException("BQ library import would overwrite existing item " + entry.ItemCode + ".");
                    admittedEntries.Add(entry);
                    merged[entry.ItemCode] = entry;
                    index++;
                }
            }
            AdvancedCostCollectionContract.RequireKnownCountStableAfterTraversal(projectEntries, hasKnownProjectEntryCount, knownProjectEntryCount, index, "BQ project import collection");
            if (hasKnownProjectEntryCount) RequireStableEntryGeneration(projectEntries, knownProjectEntryCount, admittedEntries, "BQ project import collection");
            return new BqLibraryCatalog(LibraryId, merged.Values);
        }

        private static void RequireStableEntryGeneration(IEnumerable<BqLibraryEntry> entries, int admittedKnownCount, IReadOnlyList<BqLibraryEntry> admittedEntries, string collectionLabel)
        {
            var index = 0;
            using (var entryEnumerator = entries.GetEnumerator())
            {
                AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(entries, true, admittedKnownCount, collectionLabel);
                while (true)
                {
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(entries, true, admittedKnownCount, collectionLabel);
                    if (!entryEnumerator.MoveNext()) break;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(entries, true, admittedKnownCount, collectionLabel);
                    if (index >= admittedEntries.Count) ThrowEntryContentChanged(collectionLabel);
                    var entry = entryEnumerator.Current;
                    AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(entries, true, admittedKnownCount, collectionLabel);
                    if (entry == null || !SameEntryState(admittedEntries[index], entry)) ThrowEntryContentChanged(collectionLabel);
                    index++;
                }
            }
            if (index != admittedEntries.Count) ThrowEntryContentChanged(collectionLabel);
            AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(entries, true, admittedKnownCount, collectionLabel);
        }

        private static bool SameEntryState(BqLibraryEntry left, BqLibraryEntry right) =>
            string.Equals(left.ItemCode, right.ItemCode, StringComparison.Ordinal) &&
            string.Equals(left.Description, right.Description, StringComparison.Ordinal) &&
            string.Equals(left.Unit, right.Unit, StringComparison.Ordinal) &&
            string.Equals(left.CategoryPath, right.CategoryPath, StringComparison.Ordinal) &&
            left.ReferenceUnitRate == right.ReferenceUnitRate;

        private static void ThrowEntryContentChanged(string collectionLabel) => throw new InvalidOperationException(collectionLabel + " content changed during traversal.");

        private static int CompareEntries(BqLibraryEntry left, BqLibraryEntry right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.CategoryPath, right.CategoryPath);
            if (compare != 0) return compare;
            return StringComparer.OrdinalIgnoreCase.Compare(left.ItemCode, right.ItemCode);
        }
    }
}
