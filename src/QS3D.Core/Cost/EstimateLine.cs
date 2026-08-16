using System;
using QS3D.Core.Measurement;

namespace QS3D.Core.Cost
{
    public sealed class EstimateLine
    {
        private EstimateLine(
            string estimateLineId,
            MeasurementSnapshot measurementSnapshot,
            MeasurementTrace measurementTrace,
            RateBook rateBook,
            RateItem rateItem,
            DateTime rateAsOfUtc,
            decimal measuredQuantity,
            decimal commercialAdjustmentQuantity,
            string? commercialAdjustmentReason,
            decimal estimatingQuantity,
            decimal finalAmount)
        {
            EstimateLineId = estimateLineId;
            MeasurementSnapshot = measurementSnapshot;
            MeasurementTrace = measurementTrace;
            RateBook = rateBook;
            RateItem = rateItem;
            RateAsOfUtc = rateAsOfUtc;
            MeasuredQuantity = measuredQuantity;
            CommercialAdjustmentQuantity = commercialAdjustmentQuantity;
            CommercialAdjustmentReason = commercialAdjustmentReason;
            EstimatingQuantity = estimatingQuantity;
            FinalAmount = finalAmount;
        }

        public string EstimateLineId { get; }
        public MeasurementSnapshot MeasurementSnapshot { get; }
        public MeasurementTrace MeasurementTrace { get; }
        public RateBook RateBook { get; }
        public RateItem RateItem { get; }
        public DateTime RateAsOfUtc { get; }
        public decimal MeasuredQuantity { get; }
        public decimal CommercialAdjustmentQuantity { get; }
        public string? CommercialAdjustmentReason { get; }
        public decimal EstimatingQuantity { get; }
        public string Unit => MeasurementTrace.Unit;
        public CostCode CostCode => RateItem.CostCode;
        public string Currency => RateItem.Currency;
        public decimal UnitRate => RateItem.UnitRate;
        public decimal FinalAmount { get; }

        public static EstimateLine Create(
            string estimateLineId,
            MeasurementSnapshot measurementSnapshot,
            string semanticIdentity,
            string sourceIdentity,
            string quantityKey,
            RateBook rateBook,
            CostCode costCode,
            string currency,
            DateTime rateAsOfUtc,
            decimal commercialAdjustmentQuantity = 0m,
            string? commercialAdjustmentReason = null)
        {
            var canonicalLineId = RateBookContract.RequireToken(estimateLineId, nameof(estimateLineId));
            if (measurementSnapshot == null) throw new ArgumentNullException(nameof(measurementSnapshot));
            if (rateBook == null) throw new ArgumentNullException(nameof(rateBook));
            if (costCode == null) throw new ArgumentNullException(nameof(costCode));

            var canonicalSemanticIdentity = RateBookContract.RequireToken(semanticIdentity, nameof(semanticIdentity));
            var canonicalSourceIdentity = RateBookContract.RequireToken(sourceIdentity, nameof(sourceIdentity));
            var canonicalQuantityKey = RateBookContract.RequireToken(quantityKey, nameof(quantityKey));
            var trace = FindTrace(
                measurementSnapshot,
                canonicalSemanticIdentity,
                canonicalSourceIdentity,
                canonicalQuantityKey);

            var resolution = rateBook.Resolve(costCode, trace.Unit, currency, rateAsOfUtc);
            if (!resolution.IsMatched || resolution.Item == null)
                throw new InvalidOperationException(
                    "Estimate line cannot resolve a rate for " + costCode.Value + "/" + trace.Unit + "/" + resolution.Currency +
                    " as of " + resolution.AsOfUtc.ToString("O") + ".");

            var canonicalCommercialAdjustmentQuantity = commercialAdjustmentQuantity == 0m ? 0m : commercialAdjustmentQuantity;
            var adjustmentReason = RequireAdjustmentReason(canonicalCommercialAdjustmentQuantity, commercialAdjustmentReason);
            var measuredQuantity = ConvertMeasuredQuantity(trace.NetValue);

            decimal estimatingQuantity;
            try
            {
                estimatingQuantity = checked(measuredQuantity + canonicalCommercialAdjustmentQuantity);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException("Estimate line estimating quantity overflowed decimal arithmetic.", ex);
            }

            if (estimatingQuantity < 0m)
                throw new ArgumentOutOfRangeException(
                    nameof(commercialAdjustmentQuantity),
                    "Commercial adjustment cannot reduce estimating quantity below zero.");

            decimal finalAmount;
            try
            {
                finalAmount = checked(estimatingQuantity * resolution.Item.UnitRate);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException("Estimate line final amount overflowed decimal arithmetic.", ex);
            }

            if (finalAmount == 0m && estimatingQuantity != 0m && resolution.Item.UnitRate != 0m)
                throw new OverflowException("Estimate line final amount underflowed decimal arithmetic.");

            return new EstimateLine(
                canonicalLineId,
                measurementSnapshot,
                trace,
                rateBook,
                resolution.Item,
                resolution.AsOfUtc,
                measuredQuantity,
                canonicalCommercialAdjustmentQuantity,
                adjustmentReason,
                estimatingQuantity,
                finalAmount);
        }

        private static MeasurementTrace FindTrace(
            MeasurementSnapshot snapshot,
            string semanticIdentity,
            string sourceIdentity,
            string quantityKey)
        {
            for (var i = 0; i < snapshot.Traces.Count; i++)
            {
                var trace = snapshot.Traces[i];
                if (string.Equals(trace.SemanticIdentity, semanticIdentity, StringComparison.Ordinal) &&
                    string.Equals(trace.SourceIdentity, sourceIdentity, StringComparison.Ordinal) &&
                    string.Equals(trace.QuantityKey, quantityKey, StringComparison.Ordinal))
                    return trace;
            }

            throw new InvalidOperationException(
                "Estimate line measurement trace was not found in the supplied snapshot: " +
                semanticIdentity + "/" + sourceIdentity + "/" + quantityKey + ".");
        }

        private static decimal ConvertMeasuredQuantity(double value)
        {
            decimal converted;
            try
            {
                converted = checked((decimal)value);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException("Estimate line measured quantity cannot be represented as decimal.", ex);
            }

            if (value != 0d && converted == 0m)
                throw new OverflowException("Estimate line measured quantity underflowed a non-zero measurement to decimal zero.");

            return converted;
        }

        private static string? RequireAdjustmentReason(decimal adjustmentQuantity, string? reason)
        {
            if (adjustmentQuantity == 0m) return null;
            if (reason == null || string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException(
                    "A non-zero commercial adjustment requires an explicit reason.",
                    nameof(reason));

            if (!string.Equals(reason, reason.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Commercial adjustment reason must not contain surrounding whitespace.", nameof(reason));

            for (var i = 0; i < reason.Length; i++)
            {
                if (char.IsControl(reason[i]))
                    throw new ArgumentException("Commercial adjustment reason must not contain control characters.", nameof(reason));
            }

            return reason;
        }
    }
}
