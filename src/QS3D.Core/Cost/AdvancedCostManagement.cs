using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Cost
{
    internal static class CostDecimalMath
    {
        public static decimal MultiplyPreservingNonZero(decimal left, decimal right, string label)
        {
            var result = checked(left * right);
            if (left != 0m && right != 0m && result == 0m)
                throw new OverflowException("Cost multiplication underflow: " + label + ".");
            return result;
        }

        public static decimal DividePreservingNonZero(decimal numerator, decimal denominator, string label)
        {
            var result = numerator / denominator;
            if (numerator != 0m && result == 0m)
                throw new OverflowException("Cost division underflow: " + label + ".");
            return result;
        }

        public static decimal AddPreservingNonZeroContribution(decimal left, decimal right, string label)
        {
            var result = checked(left + right);
            if ((right != 0m && result == left) || (left != 0m && result == right))
                throw new OverflowException("Cost addition precision loss: " + label + ".");
            return result;
        }
    }

    internal static class AdvancedCostCollectionContract
    {
        internal const int MaximumEntries = 10000;

        internal static bool TryGetKnownCount<T>(IEnumerable<T> items, out int count)
        {
            var hasKnownCount = false;
            count = 0;

            if (items is ICollection<T> collection)
                ObserveKnownCount(collection.Count, ref hasKnownCount, ref count);
            if (items is IReadOnlyCollection<T> readOnlyCollection)
                ObserveKnownCount(readOnlyCollection.Count, ref hasKnownCount, ref count);
            if (items is ICollection nonGenericCollection)
                ObserveKnownCount(nonGenericCollection.Count, ref hasKnownCount, ref count);

            return hasKnownCount;
        }

        private static void ObserveKnownCount(int candidate, ref bool hasKnownCount, ref int count)
        {
            if (!hasKnownCount)
            {
                count = candidate;
                hasKnownCount = true;
                return;
            }

            if (candidate == count)
                return;

            if (candidate > MaximumEntries || count > MaximumEntries)
            {
                count = Math.Max(count, candidate);
                return;
            }

            throw new InvalidOperationException("Collection reports conflicting known counts.");
        }

        internal static void ThrowTooManyEntries(string collectionLabel)
        {
            throw new InvalidOperationException(
                collectionLabel + " supports at most " + MaximumEntries + " entries.");
        }
    }

    public sealed class CostResourceComponent
    {
        public CostResourceComponent(
            string resourceCode,
            string description,
            string unit,
            decimal quantityPerBillUnit,
            decimal unitRate)
        {
            ResourceCode = RateBookContract.RequireToken(resourceCode, nameof(resourceCode));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Resource description is required.", nameof(description));
            Description = description.Trim();
            Unit = RateBookContract.RequireLowerToken(unit, nameof(unit));
            if (quantityPerBillUnit < 0m)
                throw new ArgumentOutOfRangeException(nameof(quantityPerBillUnit));
            if (unitRate < 0m)
                throw new ArgumentOutOfRangeException(nameof(unitRate));
            QuantityPerBillUnit = quantityPerBillUnit;
            UnitRate = unitRate;
        }

        public string ResourceCode { get; }
        public string Description { get; }
        public string Unit { get; }
        public decimal QuantityPerBillUnit { get; }
        public decimal UnitRate { get; }
        public decimal ExtendedUnitCost => CostDecimalMath.MultiplyPreservingNonZero(QuantityPerBillUnit, UnitRate, "resource extended unit cost");
    }

    public sealed class CostRateBuildUp
    {
        public CostRateBuildUp(
            string buildUpId,
            CostCode costCode,
            string billUnit,
            string currency,
            IEnumerable<CostResourceComponent> components,
            decimal overheadPercent = 0m,
            decimal profitPercent = 0m)
        {
            BuildUpId = RateBookContract.RequireToken(buildUpId, nameof(buildUpId));
            CostCode = costCode ?? throw new ArgumentNullException(nameof(costCode));
            BillUnit = RateBookContract.RequireLowerToken(billUnit, nameof(billUnit));
            Currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            if (components == null) throw new ArgumentNullException(nameof(components));
            ValidatePercentageForScaling(overheadPercent, nameof(overheadPercent));
            ValidatePercentageForScaling(profitPercent, nameof(profitPercent));
            if (AdvancedCostCollectionContract.TryGetKnownCount(components, out var knownComponentCount) &&
                knownComponentCount > AdvancedCostCollectionContract.MaximumEntries)
                AdvancedCostCollectionContract.ThrowTooManyEntries("Rate build-up component collection");

            var snapshot = new List<CostResourceComponent>();
            var resourceCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var component in components)
            {
                if (index == AdvancedCostCollectionContract.MaximumEntries)
                    AdvancedCostCollectionContract.ThrowTooManyEntries("Rate build-up component collection");
                if (component == null)
                    throw new ArgumentException("Rate build-up contains a null component at index " + index + ".", nameof(components));
                if (!resourceCodes.Add(component.ResourceCode))
                    throw new ArgumentException("Duplicate rate build-up resource code: " + component.ResourceCode + ".", nameof(components));
                snapshot.Add(component);
                index++;
            }
            snapshot.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.ResourceCode, right.ResourceCode));
            Components = new ReadOnlyCollection<CostResourceComponent>(snapshot.ToArray());
            OverheadPercent = overheadPercent;
            ProfitPercent = profitPercent;
            decimal direct = 0m;
            checked
            {
                for (var i = 0; i < snapshot.Count; i++)
                {
                    direct = CostDecimalMath.AddPreservingNonZeroContribution(
                        direct,
                        snapshot[i].ExtendedUnitCost,
                        "rate build-up direct unit cost");
                }
                DirectUnitCost = direct;
                OverheadUnitCost = CostDecimalMath.MultiplyPreservingNonZero(direct, OverheadPercent / 100m, "overhead unit cost");
                var subtotal = CostDecimalMath.AddPreservingNonZeroContribution(
                    direct,
                    OverheadUnitCost,
                    "rate build-up subtotal");
                ProfitUnitCost = CostDecimalMath.MultiplyPreservingNonZero(subtotal, ProfitPercent / 100m, "profit unit cost");
                UnitRate = CostDecimalMath.AddPreservingNonZeroContribution(
                    subtotal,
                    ProfitUnitCost,
                    "rate build-up unit rate");
            }
        }

        private static void ValidatePercentageForScaling(decimal value, string paramName)
        {
            if (value < 0m || value > 100m)
                throw new ArgumentOutOfRangeException(paramName, value, "Percentage must be between 0 and 100.");
            if (value > 0m && value / 100m == 0m)
                throw new ArgumentOutOfRangeException(paramName, value, "Positive percentage is too small to preserve at decimal precision.");
        }

        public string BuildUpId { get; }
        public CostCode CostCode { get; }
        public string BillUnit { get; }
        public string Currency { get; }
        public IReadOnlyList<CostResourceComponent> Components { get; }
        public decimal OverheadPercent { get; }
        public decimal ProfitPercent { get; }
        public decimal DirectUnitCost { get; }
        public decimal OverheadUnitCost { get; }
        public decimal ProfitUnitCost { get; }
        public decimal UnitRate { get; }
    }

    public sealed class HistoricalCostRecord
    {
        public HistoricalCostRecord(
            string recordId,
            string benchmarkKey,
            string dimensionKey,
            decimal quantity,
            decimal totalCost,
            string currency,
            DateTime asOfUtc)
        {
            RecordId = RateBookContract.RequireToken(recordId, nameof(recordId));
            BenchmarkKey = RateBookContract.RequireToken(benchmarkKey, nameof(benchmarkKey));
            DimensionKey = RateBookContract.RequireToken(dimensionKey, nameof(dimensionKey));
            if (quantity <= 0m) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (totalCost < 0m) throw new ArgumentOutOfRangeException(nameof(totalCost));
            Quantity = quantity;
            TotalCost = totalCost;
            Currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            AsOfUtc = RateBookContract.RequireUtc(asOfUtc, nameof(asOfUtc));
        }

        public string RecordId { get; }
        public string BenchmarkKey { get; }
        public string DimensionKey { get; }
        public decimal Quantity { get; }
        public decimal TotalCost { get; }
        public string Currency { get; }
        public DateTime AsOfUtc { get; }
        public decimal UnitCost => CostDecimalMath.DividePreservingNonZero(TotalCost, Quantity, "historical unit cost");
    }

    public sealed class HistoricalCostCatalog
    {
        private readonly IReadOnlyList<HistoricalCostRecord> _records;

        public HistoricalCostCatalog(IEnumerable<HistoricalCostRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (AdvancedCostCollectionContract.TryGetKnownCount(records, out var knownRecordCount) &&
                knownRecordCount > AdvancedCostCollectionContract.MaximumEntries)
                AdvancedCostCollectionContract.ThrowTooManyEntries("Historical cost catalog");

            var snapshot = new List<HistoricalCostRecord>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var record in records)
            {
                if (index == AdvancedCostCollectionContract.MaximumEntries)
                    AdvancedCostCollectionContract.ThrowTooManyEntries("Historical cost catalog");
                if (record == null)
                    throw new ArgumentException("Historical cost catalog contains a null record at index " + index + ".", nameof(records));
                if (!ids.Add(record.RecordId))
                    throw new ArgumentException("Duplicate historical cost record id: " + record.RecordId + ".", nameof(records));
                snapshot.Add(record);
                index++;
            }
            snapshot.Sort(CompareHistoricalRecords);
            _records = new ReadOnlyCollection<HistoricalCostRecord>(snapshot.ToArray());
        }

        public IReadOnlyList<HistoricalCostRecord> Records => _records;

        public IReadOnlyList<HistoricalCostRecord> Query(string benchmarkKey, string dimensionKey, string currency)
        {
            benchmarkKey = RateBookContract.RequireToken(benchmarkKey, nameof(benchmarkKey));
            dimensionKey = RateBookContract.RequireToken(dimensionKey, nameof(dimensionKey));
            currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            var result = new List<HistoricalCostRecord>();
            for (var i = 0; i < _records.Count; i++)
            {
                var record = _records[i];
                if (StringComparer.OrdinalIgnoreCase.Equals(record.BenchmarkKey, benchmarkKey) &&
                    StringComparer.OrdinalIgnoreCase.Equals(record.DimensionKey, dimensionKey) &&
                    StringComparer.Ordinal.Equals(record.Currency, currency))
                    result.Add(record);
            }
            return new ReadOnlyCollection<HistoricalCostRecord>(result.ToArray());
        }

        private static int CompareHistoricalRecords(HistoricalCostRecord left, HistoricalCostRecord right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.BenchmarkKey, right.BenchmarkKey);
            if (compare != 0) return compare;
            compare = StringComparer.OrdinalIgnoreCase.Compare(left.DimensionKey, right.DimensionKey);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.Currency, right.Currency);
            if (compare != 0) return compare;
            compare = left.AsOfUtc.CompareTo(right.AsOfUtc);
            if (compare != 0) return compare;
            return StringComparer.OrdinalIgnoreCase.Compare(left.RecordId, right.RecordId);
        }
    }

    public sealed class CostBenchmarkResult
    {
        internal CostBenchmarkResult(
            int sampleCount,
            decimal minimumUnitCost,
            decimal maximumUnitCost,
            decimal averageUnitCost,
            decimal medianUnitCost,
            decimal currentUnitCost,
            decimal? deviationFromAveragePercent)
        {
            SampleCount = sampleCount;
            MinimumUnitCost = minimumUnitCost;
            MaximumUnitCost = maximumUnitCost;
            AverageUnitCost = averageUnitCost;
            MedianUnitCost = medianUnitCost;
            CurrentUnitCost = currentUnitCost;
            DeviationFromAveragePercent = deviationFromAveragePercent;
        }

        public int SampleCount { get; }
        public decimal MinimumUnitCost { get; }
        public decimal MaximumUnitCost { get; }
        public decimal AverageUnitCost { get; }
        public decimal MedianUnitCost { get; }
        public decimal CurrentUnitCost { get; }
        public decimal? DeviationFromAveragePercent { get; }
    }

    public sealed class CostBenchmarkService
    {
        public CostBenchmarkResult Analyze(
            HistoricalCostCatalog catalog,
            string benchmarkKey,
            string dimensionKey,
            string currency,
            decimal currentUnitCost)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (currentUnitCost < 0m) throw new ArgumentOutOfRangeException(nameof(currentUnitCost));
            var records = catalog.Query(benchmarkKey, dimensionKey, currency);
            if (records.Count == 0)
                throw new InvalidOperationException("No historical cost samples match the requested benchmark dimensions.");

            var values = new List<decimal>(records.Count);
            for (var i = 0; i < records.Count; i++)
                values.Add(records[i].UnitCost);
            values.Sort();

            var average = values[0];
            for (var i = 1; i < values.Count; i++)
            {
                var contribution = CostDecimalMath.DividePreservingNonZero(
                    checked(values[i] - average),
                    (decimal)(i + 1),
                    "benchmark average contribution");
                average = CostDecimalMath.AddPreservingNonZeroContribution(
                    average,
                    contribution,
                    "benchmark average");
            }

            decimal median;
            if (values.Count % 2 == 1)
            {
                median = values[values.Count / 2];
            }
            else
            {
                var lowerMiddle = values[(values.Count / 2) - 1];
                var upperMiddle = values[values.Count / 2];
                var medianContribution = CostDecimalMath.DividePreservingNonZero(
                    checked(upperMiddle - lowerMiddle),
                    2m,
                    "benchmark median contribution");
                median = CostDecimalMath.AddPreservingNonZeroContribution(
                    lowerMiddle,
                    medianContribution,
                    "benchmark median");
            }
            decimal? deviation = average == 0m
                ? (currentUnitCost == 0m ? 0m : (decimal?)null)
                : CalculateDeviationPercent(currentUnitCost, average);
            return new CostBenchmarkResult(
                values.Count,
                values[0],
                values[values.Count - 1],
                average,
                median,
                currentUnitCost,
                deviation);
        }

        private static decimal CalculateDeviationPercent(decimal currentUnitCost, decimal averageUnitCost)
        {
            var delta = checked(currentUnitCost - averageUnitCost);
            if (delta == 0m) return 0m;

            try
            {
                var scaledDelta = CostDecimalMath.MultiplyPreservingNonZero(delta, 100m, "benchmark deviation scaled delta");
                return CostDecimalMath.DividePreservingNonZero(scaledDelta, averageUnitCost, "benchmark deviation percent");
            }
            catch (OverflowException)
            {
                var ratio = CostDecimalMath.DividePreservingNonZero(delta, averageUnitCost, "benchmark deviation ratio");
                return CostDecimalMath.MultiplyPreservingNonZero(ratio, 100m, "benchmark deviation percent");
            }
        }
    }

    public sealed class TenderRequirement
    {
        public TenderRequirement(string itemCode, string description, string unit, decimal quantity)
        {
            ItemCode = RateBookContract.RequireToken(itemCode, nameof(itemCode));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Tender item description is required.", nameof(description));
            Description = description.Trim();
            Unit = RateBookContract.RequireLowerToken(unit, nameof(unit));
            if (quantity < 0m) throw new ArgumentOutOfRangeException(nameof(quantity));
            Quantity = quantity;
        }

        public string ItemCode { get; }
        public string Description { get; }
        public string Unit { get; }
        public decimal Quantity { get; }
    }

    public sealed class TenderQuoteLine
    {
        public TenderQuoteLine(string itemCode, decimal unitRate)
        {
            ItemCode = RateBookContract.RequireToken(itemCode, nameof(itemCode));
            if (unitRate < 0m) throw new ArgumentOutOfRangeException(nameof(unitRate));
            UnitRate = unitRate;
        }

        public string ItemCode { get; }
        public decimal UnitRate { get; }
    }

    public sealed class TenderBid
    {
        public TenderBid(string bidId, string bidder, string currency, IEnumerable<TenderQuoteLine> lines)
        {
            BidId = RateBookContract.RequireToken(bidId, nameof(bidId));
            if (string.IsNullOrWhiteSpace(bidder)) throw new ArgumentException("Bidder is required.", nameof(bidder));
            Bidder = bidder.Trim();
            Currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            if (AdvancedCostCollectionContract.TryGetKnownCount(lines, out var knownLineCount) &&
                knownLineCount > AdvancedCostCollectionContract.MaximumEntries)
                AdvancedCostCollectionContract.ThrowTooManyEntries("Tender quote line collection");

            var byItem = new Dictionary<string, TenderQuoteLine>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var line in lines)
            {
                if (index == AdvancedCostCollectionContract.MaximumEntries)
                    AdvancedCostCollectionContract.ThrowTooManyEntries("Tender quote line collection");
                if (line == null)
                    throw new ArgumentException("Tender bid contains a null line at index " + index + ".", nameof(lines));
                if (byItem.ContainsKey(line.ItemCode))
                    throw new ArgumentException("Duplicate tender quote item code: " + line.ItemCode + ".", nameof(lines));
                byItem.Add(line.ItemCode, line);
                index++;
            }
            Lines = new ReadOnlyDictionary<string, TenderQuoteLine>(byItem);
        }

        public string BidId { get; }
        public string Bidder { get; }
        public string Currency { get; }
        public IReadOnlyDictionary<string, TenderQuoteLine> Lines { get; }
    }

    public sealed class TenderEvaluationResult
    {
        internal TenderEvaluationResult(
            string bidId,
            string bidder,
            string currency,
            decimal evaluatedTotal,
            IReadOnlyList<string> missingItemCodes,
            int rank)
        {
            BidId = bidId;
            Bidder = bidder;
            Currency = currency;
            EvaluatedTotal = evaluatedTotal;
            MissingItemCodes = missingItemCodes;
            Rank = rank;
        }

        public string BidId { get; }
        public string Bidder { get; }
        public string Currency { get; }
        public decimal EvaluatedTotal { get; }
        public IReadOnlyList<string> MissingItemCodes { get; }
        public bool IsComplete => MissingItemCodes.Count == 0;
        public int Rank { get; }
    }

    public sealed class TenderEvaluationService
    {
        public IReadOnlyList<TenderEvaluationResult> Evaluate(
            IEnumerable<TenderRequirement> requirements,
            IEnumerable<TenderBid> bids)
        {
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            if (bids == null) throw new ArgumentNullException(nameof(bids));

            var requirementList = SnapshotRequirements(requirements);
            var bidList = SnapshotBids(bids);
            if (bidList.Count == 0)
                return new ReadOnlyCollection<TenderEvaluationResult>(Array.Empty<TenderEvaluationResult>());

            var currency = bidList[0].Currency;
            for (var i = 1; i < bidList.Count; i++)
            {
                if (!StringComparer.Ordinal.Equals(currency, bidList[i].Currency))
                    throw new InvalidOperationException("Tender bids must use the same currency before they can be ranked.");
            }

            var working = new List<EvaluationBuilder>(bidList.Count);
            for (var i = 0; i < bidList.Count; i++)
            {
                var bid = bidList[i];
                decimal total = 0m;
                var missing = new List<string>();
                for (var j = 0; j < requirementList.Count; j++)
                {
                    var requirement = requirementList[j];
                    if (!bid.Lines.TryGetValue(requirement.ItemCode, out var quote))
                    {
                        missing.Add(requirement.ItemCode);
                        continue;
                    }
                    var lineCost = CostDecimalMath.MultiplyPreservingNonZero(
                        requirement.Quantity,
                        quote.UnitRate,
                        "tender evaluated line cost");
                    total = CostDecimalMath.AddPreservingNonZeroContribution(
                        total,
                        lineCost,
                        "tender evaluated total");
                }
                missing.Sort(StringComparer.OrdinalIgnoreCase);
                working.Add(new EvaluationBuilder(bid, total, missing));
            }

            var complete = new List<EvaluationBuilder>();
            for (var i = 0; i < working.Count; i++)
                if (working[i].Missing.Count == 0) complete.Add(working[i]);
            complete.Sort((left, right) =>
            {
                var compare = left.Total.CompareTo(right.Total);
                if (compare != 0) return compare;
                return StringComparer.OrdinalIgnoreCase.Compare(left.Bid.BidId, right.Bid.BidId);
            });
            for (var i = 0; i < complete.Count; i++)
                complete[i].Rank = i + 1;

            working.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Bid.BidId, right.Bid.BidId));
            var result = new List<TenderEvaluationResult>(working.Count);
            for (var i = 0; i < working.Count; i++)
            {
                var item = working[i];
                result.Add(new TenderEvaluationResult(
                    item.Bid.BidId,
                    item.Bid.Bidder,
                    item.Bid.Currency,
                    item.Total,
                    new ReadOnlyCollection<string>(item.Missing.ToArray()),
                    item.Rank));
            }
            return new ReadOnlyCollection<TenderEvaluationResult>(result.ToArray());
        }

        private static List<TenderRequirement> SnapshotRequirements(IEnumerable<TenderRequirement> requirements)
        {
            if (AdvancedCostCollectionContract.TryGetKnownCount(requirements, out var knownRequirementCount) &&
                knownRequirementCount > AdvancedCostCollectionContract.MaximumEntries)
                AdvancedCostCollectionContract.ThrowTooManyEntries("Tender requirement collection");

            var result = new List<TenderRequirement>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var requirement in requirements)
            {
                if (index == AdvancedCostCollectionContract.MaximumEntries)
                    AdvancedCostCollectionContract.ThrowTooManyEntries("Tender requirement collection");
                if (requirement == null)
                    throw new ArgumentException("Tender requirements contain a null item at index " + index + ".", nameof(requirements));
                if (!ids.Add(requirement.ItemCode))
                    throw new ArgumentException("Duplicate tender requirement item code: " + requirement.ItemCode + ".", nameof(requirements));
                result.Add(requirement);
                index++;
            }
            result.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.ItemCode, right.ItemCode));
            return result;
        }

        private static List<TenderBid> SnapshotBids(IEnumerable<TenderBid> bids)
        {
            if (AdvancedCostCollectionContract.TryGetKnownCount(bids, out var knownBidCount) &&
                knownBidCount > AdvancedCostCollectionContract.MaximumEntries)
                AdvancedCostCollectionContract.ThrowTooManyEntries("Tender bid collection");

            var result = new List<TenderBid>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var bid in bids)
            {
                if (index == AdvancedCostCollectionContract.MaximumEntries)
                    AdvancedCostCollectionContract.ThrowTooManyEntries("Tender bid collection");
                if (bid == null)
                    throw new ArgumentException("Tender comparison contains a null bid at index " + index + ".", nameof(bids));
                if (!ids.Add(bid.BidId))
                    throw new ArgumentException("Duplicate tender bid id: " + bid.BidId + ".", nameof(bids));
                result.Add(bid);
                index++;
            }
            result.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.BidId, right.BidId));
            return result;
        }

        private sealed class EvaluationBuilder
        {
            internal EvaluationBuilder(TenderBid bid, decimal total, List<string> missing)
            {
                Bid = bid;
                Total = total;
                Missing = missing;
            }

            internal TenderBid Bid { get; }
            internal decimal Total { get; }
            internal List<string> Missing { get; }
            internal int Rank { get; set; }
        }
    }

    public sealed class ProgressContractItem
    {
        public ProgressContractItem(string itemCode, string unit, decimal contractQuantity, decimal unitRate)
        {
            ItemCode = RateBookContract.RequireToken(itemCode, nameof(itemCode));
            Unit = RateBookContract.RequireLowerToken(unit, nameof(unit));
            if (contractQuantity < 0m) throw new ArgumentOutOfRangeException(nameof(contractQuantity));
            if (unitRate < 0m) throw new ArgumentOutOfRangeException(nameof(unitRate));
            ContractQuantity = contractQuantity;
            UnitRate = unitRate;
        }

        public string ItemCode { get; }
        public string Unit { get; }
        public decimal ContractQuantity { get; }
        public decimal UnitRate { get; }
    }

    public sealed class ProgressClaimLine
    {
        public ProgressClaimLine(string itemCode, decimal previousCertifiedQuantity, decimal claimedThisPeriodQuantity)
        {
            ItemCode = RateBookContract.RequireToken(itemCode, nameof(itemCode));
            if (previousCertifiedQuantity < 0m) throw new ArgumentOutOfRangeException(nameof(previousCertifiedQuantity));
            if (claimedThisPeriodQuantity < 0m) throw new ArgumentOutOfRangeException(nameof(claimedThisPeriodQuantity));
            PreviousCertifiedQuantity = previousCertifiedQuantity;
            ClaimedThisPeriodQuantity = claimedThisPeriodQuantity;
        }

        public string ItemCode { get; }
        public decimal PreviousCertifiedQuantity { get; }
        public decimal ClaimedThisPeriodQuantity { get; }
    }

    public sealed class ProgressClaimLineResult
    {
        internal ProgressClaimLineResult(
            string itemCode,
            decimal previousCertifiedQuantity,
            decimal claimedThisPeriodQuantity,
            decimal certifiedThisPeriodQuantity,
            decimal rejectedQuantity,
            decimal remainingQuantity,
            decimal certifiedThisPeriodValue)
        {
            ItemCode = itemCode;
            PreviousCertifiedQuantity = previousCertifiedQuantity;
            ClaimedThisPeriodQuantity = claimedThisPeriodQuantity;
            CertifiedThisPeriodQuantity = certifiedThisPeriodQuantity;
            RejectedQuantity = rejectedQuantity;
            RemainingQuantity = remainingQuantity;
            CertifiedThisPeriodValue = certifiedThisPeriodValue;
        }

        public string ItemCode { get; }
        public decimal PreviousCertifiedQuantity { get; }
        public decimal ClaimedThisPeriodQuantity { get; }
        public decimal CertifiedThisPeriodQuantity { get; }
        public decimal RejectedQuantity { get; }
        public decimal RemainingQuantity { get; }
        public decimal CertifiedThisPeriodValue { get; }
    }

    public sealed class ProgressClaimResult
    {
        internal ProgressClaimResult(
            IReadOnlyList<ProgressClaimLineResult> lines,
            decimal grossCertifiedThisPeriod,
            decimal retentionPercent,
            decimal retentionThisPeriod,
            decimal netCertifiedThisPeriod)
        {
            Lines = lines;
            GrossCertifiedThisPeriod = grossCertifiedThisPeriod;
            RetentionPercent = retentionPercent;
            RetentionThisPeriod = retentionThisPeriod;
            NetCertifiedThisPeriod = netCertifiedThisPeriod;
        }

        public IReadOnlyList<ProgressClaimLineResult> Lines { get; }
        public decimal GrossCertifiedThisPeriod { get; }
        public decimal RetentionPercent { get; }
        public decimal RetentionThisPeriod { get; }
        public decimal NetCertifiedThisPeriod { get; }
    }

    public sealed class ProgressClaimService
    {
        public ProgressClaimResult Evaluate(
            IEnumerable<ProgressContractItem> contractItems,
            IEnumerable<ProgressClaimLine> claimLines,
            decimal retentionPercent = 0m)
        {
            if (contractItems == null) throw new ArgumentNullException(nameof(contractItems));
            if (claimLines == null) throw new ArgumentNullException(nameof(claimLines));
            if (retentionPercent < 0m || retentionPercent > 100m)
                throw new ArgumentOutOfRangeException(nameof(retentionPercent));
            if (retentionPercent > 0m && retentionPercent / 100m == 0m)
                throw new ArgumentOutOfRangeException(nameof(retentionPercent), retentionPercent, "Positive retention percentage is too small to preserve at decimal precision.");
            if (AdvancedCostCollectionContract.TryGetKnownCount(contractItems, out var knownContractCount) &&
                knownContractCount > AdvancedCostCollectionContract.MaximumEntries)
                AdvancedCostCollectionContract.ThrowTooManyEntries("Progress contract item collection");
            if (AdvancedCostCollectionContract.TryGetKnownCount(claimLines, out var knownClaimCount) &&
                knownClaimCount > AdvancedCostCollectionContract.MaximumEntries)
                AdvancedCostCollectionContract.ThrowTooManyEntries("Progress claim line collection");

            var contracts = new Dictionary<string, ProgressContractItem>(StringComparer.OrdinalIgnoreCase);
            var contractIndex = 0;
            foreach (var item in contractItems)
            {
                if (contractIndex == AdvancedCostCollectionContract.MaximumEntries)
                    AdvancedCostCollectionContract.ThrowTooManyEntries("Progress contract item collection");
                if (item == null) throw new ArgumentException("Progress contract contains a null item.", nameof(contractItems));
                if (contracts.ContainsKey(item.ItemCode))
                    throw new ArgumentException("Duplicate progress contract item code: " + item.ItemCode + ".", nameof(contractItems));
                contracts.Add(item.ItemCode, item);
                contractIndex++;
            }

            var claims = new Dictionary<string, ProgressClaimLine>(StringComparer.OrdinalIgnoreCase);
            var claimIndex = 0;
            foreach (var line in claimLines)
            {
                if (claimIndex == AdvancedCostCollectionContract.MaximumEntries)
                    AdvancedCostCollectionContract.ThrowTooManyEntries("Progress claim line collection");
                if (line == null) throw new ArgumentException("Progress claim contains a null line.", nameof(claimLines));
                if (claims.ContainsKey(line.ItemCode))
                    throw new ArgumentException("Duplicate progress claim item code: " + line.ItemCode + ".", nameof(claimLines));
                if (!contracts.ContainsKey(line.ItemCode))
                    throw new InvalidOperationException("Progress claim references an unknown contract item: " + line.ItemCode + ".");
                claims.Add(line.ItemCode, line);
                claimIndex++;
            }

            var itemCodes = new List<string>(contracts.Keys);
            itemCodes.Sort(StringComparer.OrdinalIgnoreCase);
            var results = new List<ProgressClaimLineResult>(itemCodes.Count);
            decimal gross = 0m;
            checked
            {
                for (var i = 0; i < itemCodes.Count; i++)
                {
                    var item = contracts[itemCodes[i]];
                    claims.TryGetValue(item.ItemCode, out var claim);
                    var previous = claim?.PreviousCertifiedQuantity ?? 0m;
                    var requested = claim?.ClaimedThisPeriodQuantity ?? 0m;
                    if (previous > item.ContractQuantity)
                        throw new InvalidOperationException("Previous certified quantity exceeds the contract quantity for item " + item.ItemCode + ".");
                    var available = item.ContractQuantity - previous;
                    var certified = requested <= available ? requested : available;
                    var rejected = requested - certified;
                    var remaining = available - certified;
                    var value = CostDecimalMath.MultiplyPreservingNonZero(certified, item.UnitRate, "progress certified line value");
                    gross = CostDecimalMath.AddPreservingNonZeroContribution(
                        gross,
                        value,
                        "progress gross certified this period");
                    results.Add(new ProgressClaimLineResult(
                        item.ItemCode,
                        previous,
                        requested,
                        certified,
                        rejected,
                        remaining,
                        value));
                }
                var retention = CostDecimalMath.MultiplyPreservingNonZero(gross, retentionPercent / 100m, "progress retention value");
                var net = gross - retention;
                return new ProgressClaimResult(
                    new ReadOnlyCollection<ProgressClaimLineResult>(results.ToArray()),
                    gross,
                    retentionPercent,
                    retention,
                    net);
            }
        }
    }
}
