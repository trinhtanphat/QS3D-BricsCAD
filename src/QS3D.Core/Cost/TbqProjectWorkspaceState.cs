using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Cost
{
    public sealed class TbqBillItem
    {
        public TbqBillItem(
            string itemCode,
            string description,
            string unit,
            string? tradeCode,
            decimal quantity,
            decimal unitRate,
            string? rateCode = null)
        {
            ItemCode = RateBookContract.RequireToken(itemCode, nameof(itemCode));
            Description = RequireText(description, nameof(description), "TBQ bill item description");
            Unit = RateBookContract.RequireLowerToken(unit, nameof(unit));
            TradeCode = string.IsNullOrWhiteSpace(tradeCode)
                ? "Unclassified"
                : RequireText(tradeCode!, nameof(tradeCode), "TBQ trade code");
            if (quantity < 0m) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (unitRate < 0m) throw new ArgumentOutOfRangeException(nameof(unitRate));
            Quantity = quantity == 0m ? 0m : quantity;
            UnitRate = unitRate == 0m ? 0m : unitRate;
            RateCode = string.IsNullOrWhiteSpace(rateCode)
                ? string.Empty
                : RateBookContract.RequireToken(rateCode!, nameof(rateCode));
        }

        public string ItemCode { get; }
        public string Description { get; }
        public string Unit { get; }
        public string TradeCode { get; }
        public decimal Quantity { get; }
        public decimal UnitRate { get; }
        public string RateCode { get; }
        public decimal TotalCost
        {
            get
            {
                try { return checked(Quantity * UnitRate); }
                catch (OverflowException ex) { throw new OverflowException("TBQ bill item total cost overflowed decimal arithmetic.", ex); }
            }
        }

        private static string RequireText(string value, string parameterName, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(label + " is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException(label + " must not contain surrounding whitespace.", parameterName);
            for (var i = 0; i < value.Length; i++)
                if (char.IsControl(value[i])) throw new ArgumentException(label + " must not contain control characters.", parameterName);
            return value;
        }
    }

    public sealed class TbqProjectWorkspaceState
    {
        public TbqProjectWorkspaceState(
            string currency,
            decimal cfaM2,
            IEnumerable<TbqBillItem> billItems,
            IEnumerable<BuildUpRateSnapshot> buildUpRates,
            IEnumerable<RateReferenceEdge> rateReferences,
            string libraryId,
            IEnumerable<BqLibraryEntry> libraryEntries,
            decimal adjustmentRatioPercent = 0m,
            decimal markupRatioPercent = 0m)
        {
            Currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            if (cfaM2 < 0m) throw new ArgumentOutOfRangeException(nameof(cfaM2));
            CfaM2 = cfaM2 == 0m ? 0m : cfaM2;
            LibraryId = RateBookContract.RequireToken(libraryId, nameof(libraryId));

            BillItems = SnapshotBillItems(billItems);
            BuildUpRates = SnapshotBuildUpRates(buildUpRates);
            RateReferences = new RateReferenceGraph(rateReferences ?? throw new ArgumentNullException(nameof(rateReferences)));
            Library = new BqLibraryCatalog(LibraryId, libraryEntries ?? throw new ArgumentNullException(nameof(libraryEntries)));

            new CostAdjustmentService().AdjustByRatios(0m, adjustmentRatioPercent, markupRatioPercent);
            AdjustmentRatioPercent = adjustmentRatioPercent == 0m ? 0m : adjustmentRatioPercent;
            MarkupRatioPercent = markupRatioPercent == 0m ? 0m : markupRatioPercent;
        }

        public string Currency { get; }
        public decimal CfaM2 { get; }
        public IReadOnlyList<TbqBillItem> BillItems { get; }
        public IReadOnlyList<BuildUpRateSnapshot> BuildUpRates { get; }
        public RateReferenceGraph RateReferences { get; }
        public string LibraryId { get; }
        public BqLibraryCatalog Library { get; }
        public decimal AdjustmentRatioPercent { get; }
        public decimal MarkupRatioPercent { get; }

        public decimal BaseTotal
        {
            get
            {
                decimal total = 0m;
                try
                {
                    for (var i = 0; i < BillItems.Count; i++) total = checked(total + BillItems[i].TotalCost);
                    return total;
                }
                catch (OverflowException ex)
                {
                    throw new OverflowException("TBQ workspace base total overflowed decimal arithmetic.", ex);
                }
            }
        }

        public CostAdjustmentResult PreviewAdjustment()
        {
            return new CostAdjustmentService().AdjustByRatios(BaseTotal, AdjustmentRatioPercent, MarkupRatioPercent);
        }

        public TbqProjectWorkspaceState WithAdjustment(decimal adjustmentRatioPercent, decimal markupRatioPercent)
        {
            return new TbqProjectWorkspaceState(
                Currency,
                CfaM2,
                BillItems,
                BuildUpRates,
                RateReferences.Edges,
                LibraryId,
                Library.Entries,
                adjustmentRatioPercent,
                markupRatioPercent);
        }

        public IReadOnlyList<BuildUpAnalysisLine> AnalyzeBuildUps(bool adoptedOnly = true)
        {
            return new BuildUpAnalysisService().Analyze(BuildUpRates, RateReferences, adoptedOnly);
        }

        public IReadOnlyList<TradeCostAnalysisRow> AnalyzeTrades()
        {
            var adjustedItems = new List<TradeCostItem>(BillItems.Count);
            var adjuster = new CostAdjustmentService();
            for (var i = 0; i < BillItems.Count; i++)
            {
                var item = BillItems[i];
                var adjusted = adjuster.AdjustByRatios(item.TotalCost, AdjustmentRatioPercent, MarkupRatioPercent);
                adjustedItems.Add(new TradeCostItem(item.ItemCode, item.TradeCode, adjusted.AdjustedTotal));
            }
            return new TradeCostAnalysisService().Analyze(adjustedItems, CfaM2);
        }

        private static IReadOnlyList<TbqBillItem> SnapshotBillItems(IEnumerable<TbqBillItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var snapshot = new List<TbqBillItem>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var item in items)
            {
                if (item == null) throw new ArgumentException("TBQ workspace contains a null bill item at index " + index + ".", nameof(items));
                if (!ids.Add(item.ItemCode)) throw new ArgumentException("Duplicate TBQ bill item code: " + item.ItemCode + ".", nameof(items));
                snapshot.Add(item);
                index++;
            }
            snapshot.Sort(CompareBillItems);
            return new ReadOnlyCollection<TbqBillItem>(snapshot.ToArray());
        }

        private static IReadOnlyList<BuildUpRateSnapshot> SnapshotBuildUpRates(IEnumerable<BuildUpRateSnapshot> rates)
        {
            if (rates == null) throw new ArgumentNullException(nameof(rates));
            var snapshot = new List<BuildUpRateSnapshot>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var rate in rates)
            {
                if (rate == null) throw new ArgumentException("TBQ workspace contains a null build-up rate at index " + index + ".", nameof(rates));
                if (!ids.Add(rate.RateCode)) throw new ArgumentException("Duplicate TBQ build-up rate code: " + rate.RateCode + ".", nameof(rates));
                snapshot.Add(rate);
                index++;
            }
            snapshot.Sort(CompareBuildUps);
            return new ReadOnlyCollection<BuildUpRateSnapshot>(snapshot.ToArray());
        }

        private static int CompareBillItems(TbqBillItem left, TbqBillItem right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.ItemCode, right.ItemCode);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.ItemCode, right.ItemCode);
        }

        private static int CompareBuildUps(BuildUpRateSnapshot left, BuildUpRateSnapshot right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.RateCode, right.RateCode);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.RateCode, right.RateCode);
        }
    }
}
