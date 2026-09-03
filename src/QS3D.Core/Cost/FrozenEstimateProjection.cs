using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Cost
{
    public sealed class FrozenEstimateProjection
    {
        internal const int MaxLines = 10000;

        private FrozenEstimateProjection(List<FrozenEstimateProjectionRow> rows)
        {
            Rows = new ReadOnlyCollection<FrozenEstimateProjectionRow>(rows.ToArray());
        }

        public IReadOnlyList<FrozenEstimateProjectionRow> Rows { get; }

        public static FrozenEstimateProjection Create(IEnumerable<EstimateLine> lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            var hasKnownCount = TryGetKnownCount(lines, out var knownCount);
            if (hasKnownCount && knownCount > MaxLines)
                ThrowTooManyLines();

            var rows = new List<FrozenEstimateProjectionRow>();
            var lineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            using (var enumerator = lines.GetEnumerator())
            {
                // GetEnumerator() itself is user code for arbitrary IEnumerable implementations.
                // Re-admit a known Count before the first traversal call so an enumerator-induced
                // drift is rejected with zero MoveNext/Current reads.
                if (hasKnownCount)
                    RequireStableKnownCount(lines, knownCount);

                while (enumerator.MoveNext())
                {
                    if (hasKnownCount)
                        RequireStableKnownCount(lines, knownCount);
                    if (hasKnownCount && index >= knownCount)
                        throw new InvalidOperationException("Frozen estimate projection source Count does not match source traversal.");
                    if (index >= MaxLines)
                        ThrowTooManyLines();
                    var line = enumerator.Current;
                    if (line == null)
                        throw new ArgumentException("Estimate projection contains a null line at index " + index + ".", nameof(lines));
                    if (!lineIds.Add(line.EstimateLineId))
                        throw new ArgumentException("Duplicate estimate line id: " + line.EstimateLineId + ".", nameof(lines));

                    rows.Add(FrozenEstimateProjectionRow.From(line));
                    index++;
                    if (hasKnownCount)
                        RequireStableKnownCount(lines, knownCount);
                }
            }

            if (hasKnownCount && rows.Count != knownCount)
                throw new InvalidOperationException("Frozen estimate projection source Count does not match source traversal.");
            if (hasKnownCount)
            {
                RequireStableKnownCount(lines, knownCount);
                RequireStableProjectionGeneration(lines, knownCount, rows);
            }

            rows.Sort(CompareRows);
            return new FrozenEstimateProjection(rows);
        }

        private static void RequireStableProjectionGeneration(
            IEnumerable<EstimateLine> lines,
            int knownCount,
            IReadOnlyList<FrozenEstimateProjectionRow> admittedRows)
        {
            var index = 0;
            using (var enumerator = lines.GetEnumerator())
            {
                // A replay enumerator is also caller code. Re-admit Count after acquisition,
                // then surround every traversal observation with the existing Count contract.
                RequireStableKnownCount(lines, knownCount);
                while (true)
                {
                    RequireStableKnownCount(lines, knownCount);
                    if (!enumerator.MoveNext())
                        break;
                    RequireStableKnownCount(lines, knownCount);
                    if (index >= admittedRows.Count)
                        ThrowProjectionContentChanged();

                    var line = enumerator.Current;
                    RequireStableKnownCount(lines, knownCount);
                    if (line == null)
                        throw new InvalidOperationException("Frozen estimate projection content changed during enumeration.");

                    var replayedRow = FrozenEstimateProjectionRow.From(line);
                    if (!FrozenProjectionRowStateEquals(admittedRows[index], replayedRow))
                        ThrowProjectionContentChanged();
                    index++;
                }
            }

            if (index != admittedRows.Count)
                ThrowProjectionContentChanged();
            RequireStableKnownCount(lines, knownCount);
        }

        private static bool FrozenProjectionRowStateEquals(
            FrozenEstimateProjectionRow left,
            FrozenEstimateProjectionRow right)
        {
            return string.Equals(left.EstimateLineId, right.EstimateLineId, StringComparison.Ordinal) &&
                   string.Equals(left.SemanticIdentity, right.SemanticIdentity, StringComparison.Ordinal) &&
                   string.Equals(left.SourceIdentity, right.SourceIdentity, StringComparison.Ordinal) &&
                   string.Equals(left.QuantityKey, right.QuantityKey, StringComparison.Ordinal) &&
                   string.Equals(left.RateBookId, right.RateBookId, StringComparison.Ordinal) &&
                   string.Equals(left.RateItemId, right.RateItemId, StringComparison.Ordinal) &&
                   string.Equals(left.RateVersion, right.RateVersion, StringComparison.Ordinal) &&
                   left.RateAsOfUtc == right.RateAsOfUtc &&
                   string.Equals(left.CostCode, right.CostCode, StringComparison.Ordinal) &&
                   string.Equals(left.Unit, right.Unit, StringComparison.Ordinal) &&
                   string.Equals(left.Currency, right.Currency, StringComparison.Ordinal) &&
                   left.MeasuredQuantity == right.MeasuredQuantity &&
                   left.CommercialAdjustmentQuantity == right.CommercialAdjustmentQuantity &&
                   string.Equals(left.CommercialAdjustmentReason, right.CommercialAdjustmentReason, StringComparison.Ordinal) &&
                   left.EstimatingQuantity == right.EstimatingQuantity &&
                   left.UnitRate == right.UnitRate &&
                   left.FinalAmount == right.FinalAmount;
        }

        private static void ThrowProjectionContentChanged()
        {
            throw new InvalidOperationException("Frozen estimate projection content changed during enumeration.");
        }

        private static bool TryGetKnownCount(IEnumerable<EstimateLine> lines, out int count)
        {
            var counts = new List<int>(3);
            if (lines is ICollection<EstimateLine> collection)
                counts.Add(collection.Count);
            if (lines is IReadOnlyCollection<EstimateLine> readOnlyCollection)
                counts.Add(readOnlyCollection.Count);
            if (lines is ICollection nonGenericCollection)
                counts.Add(nonGenericCollection.Count);

            if (counts.Count == 0)
            {
                count = 0;
                return false;
            }

            count = counts[0];
            var maximumCount = count;
            var hasNegative = count < 0;
            var hasConflict = false;
            for (var i = 1; i < counts.Count; i++)
            {
                if (counts[i] < 0)
                    hasNegative = true;
                if (counts[i] != count)
                    hasConflict = true;
                if (counts[i] > maximumCount)
                    maximumCount = counts[i];
            }

            if (maximumCount > MaxLines)
            {
                count = maximumCount;
                return true;
            }

            if (hasNegative)
                throw new InvalidOperationException("Frozen estimate projection source reports an invalid negative known count.");

            if (hasConflict)
                throw new InvalidOperationException("Frozen estimate projection source reports conflicting known counts.");

            return true;
        }

        private static void RequireStableKnownCount(IEnumerable<EstimateLine> lines, int expectedCount)
        {
            if (!TryGetKnownCount(lines, out var observedCount) || observedCount != expectedCount)
                throw new InvalidOperationException("Frozen estimate projection source Count changed during enumeration.");
        }

        private static void ThrowTooManyLines()
        {
            throw new InvalidOperationException(
                "Frozen estimate projection supports at most " + MaxLines + " estimate lines.");
        }

        private static int CompareRows(FrozenEstimateProjectionRow left, FrozenEstimateProjectionRow right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.EstimateLineId, right.EstimateLineId);
            return compare != 0
                ? compare
                : StringComparer.Ordinal.Compare(left.EstimateLineId, right.EstimateLineId);
        }
    }

    public sealed class FrozenEstimateProjectionRow
    {
        private FrozenEstimateProjectionRow(
            string estimateLineId,
            string semanticIdentity,
            string sourceIdentity,
            string quantityKey,
            string rateBookId,
            string rateItemId,
            string rateVersion,
            DateTime rateAsOfUtc,
            string costCode,
            string unit,
            string currency,
            decimal measuredQuantity,
            decimal commercialAdjustmentQuantity,
            string? commercialAdjustmentReason,
            decimal estimatingQuantity,
            decimal unitRate,
            decimal finalAmount)
        {
            EstimateLineId = estimateLineId;
            SemanticIdentity = semanticIdentity;
            SourceIdentity = sourceIdentity;
            QuantityKey = quantityKey;
            RateBookId = rateBookId;
            RateItemId = rateItemId;
            RateVersion = rateVersion;
            RateAsOfUtc = rateAsOfUtc;
            CostCode = costCode;
            Unit = unit;
            Currency = currency;
            MeasuredQuantity = measuredQuantity;
            CommercialAdjustmentQuantity = commercialAdjustmentQuantity;
            CommercialAdjustmentReason = commercialAdjustmentReason;
            EstimatingQuantity = estimatingQuantity;
            UnitRate = unitRate;
            FinalAmount = finalAmount;
        }

        public string EstimateLineId { get; }
        public string SemanticIdentity { get; }
        public string SourceIdentity { get; }
        public string QuantityKey { get; }
        public string RateBookId { get; }
        public string RateItemId { get; }
        public string RateVersion { get; }
        public DateTime RateAsOfUtc { get; }
        public string CostCode { get; }
        public string Unit { get; }
        public string Currency { get; }
        public decimal MeasuredQuantity { get; }
        public decimal CommercialAdjustmentQuantity { get; }
        public string? CommercialAdjustmentReason { get; }
        public decimal EstimatingQuantity { get; }
        public decimal UnitRate { get; }
        public decimal FinalAmount { get; }

        internal static FrozenEstimateProjectionRow From(EstimateLine line)
        {
            return new FrozenEstimateProjectionRow(
                line.EstimateLineId,
                line.MeasurementTrace.SemanticIdentity,
                line.MeasurementTrace.SourceIdentity,
                line.MeasurementTrace.QuantityKey,
                line.RateBook.RateBookId,
                line.RateItem.RateItemId,
                line.RateItem.Version,
                line.RateAsOfUtc,
                line.CostCode.Value,
                line.Unit,
                line.Currency,
                line.MeasuredQuantity,
                line.CommercialAdjustmentQuantity,
                line.CommercialAdjustmentReason,
                line.EstimatingQuantity,
                line.UnitRate,
                line.FinalAmount);
        }
    }
}
