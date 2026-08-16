using System;

namespace QS3D.Core.Cost
{
    public sealed class EstimateRevisionCostImpact
    {
        private EstimateRevisionCostImpact(
            EstimateLine previous,
            EstimateLine current,
            decimal measuredQuantityDelta,
            decimal commercialAdjustmentQuantityDelta,
            decimal estimatingQuantityDelta,
            decimal unitRateDelta,
            decimal quantityDrivenCostDelta,
            decimal rateDrivenCostDelta,
            decimal rateEffectAtCurrentQuantity,
            decimal rateEffectRoundingResidual,
            decimal costDelta)
        {
            Previous = previous;
            Current = current;
            MeasuredQuantityDelta = measuredQuantityDelta;
            CommercialAdjustmentQuantityDelta = commercialAdjustmentQuantityDelta;
            EstimatingQuantityDelta = estimatingQuantityDelta;
            UnitRateDelta = unitRateDelta;
            QuantityDrivenCostDelta = quantityDrivenCostDelta;
            RateDrivenCostDelta = rateDrivenCostDelta;
            RateEffectAtCurrentQuantity = rateEffectAtCurrentQuantity;
            RateEffectRoundingResidual = rateEffectRoundingResidual;
            CostDelta = costDelta;
        }

        public EstimateLine Previous { get; }
        public EstimateLine Current { get; }

        public decimal PreviousMeasuredQuantity => Previous.MeasuredQuantity;
        public decimal CurrentMeasuredQuantity => Current.MeasuredQuantity;
        public decimal MeasuredQuantityDelta { get; }

        public decimal PreviousCommercialAdjustmentQuantity => Previous.CommercialAdjustmentQuantity;
        public decimal CurrentCommercialAdjustmentQuantity => Current.CommercialAdjustmentQuantity;
        public decimal CommercialAdjustmentQuantityDelta { get; }

        public decimal PreviousEstimatingQuantity => Previous.EstimatingQuantity;
        public decimal CurrentEstimatingQuantity => Current.EstimatingQuantity;
        public decimal EstimatingQuantityDelta { get; }

        public decimal PreviousUnitRate => Previous.UnitRate;
        public decimal CurrentUnitRate => Current.UnitRate;
        public decimal UnitRateDelta { get; }

        public decimal PreviousAmount => Previous.FinalAmount;
        public decimal CurrentAmount => Current.FinalAmount;
        public decimal QuantityDrivenCostDelta { get; }
        public decimal RateDrivenCostDelta { get; }
        public decimal RateEffectAtCurrentQuantity { get; }
        public decimal RateEffectRoundingResidual { get; }
        public decimal CostDelta { get; }

        public string Unit => Previous.Unit;
        public string Currency => Previous.Currency;
        public CostCode CostCode => Previous.CostCode;

        public static EstimateRevisionCostImpact Create(EstimateLine previous, EstimateLine current)
        {
            if (previous == null) throw new ArgumentNullException(nameof(previous));
            if (current == null) throw new ArgumentNullException(nameof(current));

            RequireComparable(previous, current);

            var measuredQuantityDelta = Subtract(
                current.MeasuredQuantity,
                previous.MeasuredQuantity,
                "measured quantity delta");
            var commercialAdjustmentQuantityDelta = Subtract(
                current.CommercialAdjustmentQuantity,
                previous.CommercialAdjustmentQuantity,
                "commercial adjustment quantity delta");
            var estimatingQuantityDelta = Subtract(
                current.EstimatingQuantity,
                previous.EstimatingQuantity,
                "estimating quantity delta");
            var unitRateDelta = Subtract(
                current.UnitRate,
                previous.UnitRate,
                "unit rate delta");
            var costDelta = Subtract(
                current.FinalAmount,
                previous.FinalAmount,
                "cost delta");

            var quantityDrivenCostDelta = Multiply(
                estimatingQuantityDelta,
                previous.UnitRate,
                "quantity-driven cost delta");
            var rateEffectAtCurrentQuantity = Multiply(
                current.EstimatingQuantity,
                unitRateDelta,
                "rate effect at current quantity");
            var rateDrivenCostDelta = Subtract(
                costDelta,
                quantityDrivenCostDelta,
                "reconciled rate-driven cost delta");
            var rateEffectRoundingResidual = Subtract(
                rateDrivenCostDelta,
                rateEffectAtCurrentQuantity,
                "rate-effect rounding residual");

            var reconciled = Add(
                quantityDrivenCostDelta,
                rateDrivenCostDelta,
                "revision cost reconciliation");
            if (reconciled != costDelta)
                throw new InvalidOperationException("Estimate revision cost impact failed exact decimal reconciliation.");

            return new EstimateRevisionCostImpact(
                previous,
                current,
                measuredQuantityDelta,
                commercialAdjustmentQuantityDelta,
                estimatingQuantityDelta,
                unitRateDelta,
                quantityDrivenCostDelta,
                rateDrivenCostDelta,
                rateEffectAtCurrentQuantity,
                rateEffectRoundingResidual,
                costDelta);
        }

        private static void RequireComparable(EstimateLine previous, EstimateLine current)
        {
            if (!string.Equals(previous.EstimateLineId, current.EstimateLineId, StringComparison.Ordinal))
                throw new ArgumentException("Estimate revision comparison requires the same exact EstimateLineId.", nameof(current));

            var previousTrace = previous.MeasurementTrace;
            var currentTrace = current.MeasurementTrace;
            if (!string.Equals(previousTrace.SemanticIdentity, currentTrace.SemanticIdentity, StringComparison.Ordinal) ||
                !string.Equals(previousTrace.SourceIdentity, currentTrace.SourceIdentity, StringComparison.Ordinal) ||
                !string.Equals(previousTrace.QuantityKey, currentTrace.QuantityKey, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Estimate revision comparison requires the same exact measurement trace identity.",
                    nameof(current));
            }

            if (!string.Equals(previous.Unit, current.Unit, StringComparison.Ordinal))
                throw new ArgumentException("Estimate revision comparison requires the same unit.", nameof(current));
            if (!string.Equals(previous.Currency, current.Currency, StringComparison.Ordinal))
                throw new ArgumentException("Estimate revision comparison requires the same currency.", nameof(current));
            if (!previous.CostCode.Equals(current.CostCode))
                throw new ArgumentException("Estimate revision comparison requires the same cost code.", nameof(current));
        }

        private static decimal Add(decimal left, decimal right, string operation)
        {
            try
            {
                return checked(left + right);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException("Estimate revision " + operation + " overflowed decimal arithmetic.", ex);
            }
        }

        private static decimal Subtract(decimal left, decimal right, string operation)
        {
            try
            {
                return checked(left - right);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException("Estimate revision " + operation + " overflowed decimal arithmetic.", ex);
            }
        }

        private static decimal Multiply(decimal left, decimal right, string operation)
        {
            try
            {
                return CostDecimalMath.MultiplyPreservingNonZero(left, right, "estimate revision " + operation);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException("Estimate revision " + operation + " overflowed decimal arithmetic.", ex);
            }
        }
    }
}
