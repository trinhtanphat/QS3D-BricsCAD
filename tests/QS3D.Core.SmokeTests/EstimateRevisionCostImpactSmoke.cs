using System;
using System.Collections.Generic;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimateRevisionCostImpactSmoke
    {
        internal static void Run()
        {
            QuantityOnlyChangeReconciles();
            RateOnlyChangeReconciles();
            SimultaneousQuantityAndRateChangeReconciles();
            CommercialAdjustmentChangeRemainsQuantityDriven();
            UnchangedStateIsZero();
            ComparableScopeIsStrict();
            MeasurementIdentityScopeIsStrict();
            DecompositionOverflowFailsClosed();
        }

        private static void QuantityOnlyChangeReconciles()
        {
            var previous = Line("LINE", 10d, "m3", "CONC", "USD", 100m);
            var current = Line("LINE", 12d, "m3", "CONC", "USD", 100m);
            var impact = EstimateRevisionCostImpact.Create(previous, current);

            Equal(10m, impact.PreviousMeasuredQuantity, "Previous measured quantity mismatch.");
            Equal(12m, impact.CurrentMeasuredQuantity, "Current measured quantity mismatch.");
            Equal(2m, impact.MeasuredQuantityDelta, "Measured quantity delta mismatch.");
            Equal(2m, impact.EstimatingQuantityDelta, "Estimating quantity delta mismatch.");
            Equal(0m, impact.UnitRateDelta, "Quantity-only rate delta must be zero.");
            Equal(200m, impact.QuantityDrivenCostDelta, "Quantity-only cost effect mismatch.");
            Equal(0m, impact.RateDrivenCostDelta, "Quantity-only reconciled rate effect must be zero.");
            Equal(0m, impact.RateEffectAtCurrentQuantity, "Quantity-only diagnostic rate effect must be zero.");
            Equal(0m, impact.RateEffectRoundingResidual, "Quantity-only rounding residual must be zero.");
            Equal(200m, impact.CostDelta, "Quantity-only total cost delta mismatch.");
            Reconciles(impact);
        }

        private static void RateOnlyChangeReconciles()
        {
            var previous = Line("LINE", 10d, "m3", "CONC", "USD", 100m);
            var current = Line("LINE", 10d, "m3", "CONC", "USD", 120m);
            var impact = EstimateRevisionCostImpact.Create(previous, current);

            Equal(0m, impact.EstimatingQuantityDelta, "Rate-only estimating quantity delta must be zero.");
            Equal(20m, impact.UnitRateDelta, "Rate-only unit-rate delta mismatch.");
            Equal(0m, impact.QuantityDrivenCostDelta, "Rate-only quantity effect must be zero.");
            Equal(200m, impact.RateEffectAtCurrentQuantity, "Rate-only diagnostic rate effect mismatch.");
            Equal(200m, impact.RateDrivenCostDelta, "Rate-only reconciled rate effect mismatch.");
            Equal(0m, impact.RateEffectRoundingResidual, "Rate-only rounding residual mismatch.");
            Equal(200m, impact.CostDelta, "Rate-only total cost delta mismatch.");
            Reconciles(impact);
        }

        private static void SimultaneousQuantityAndRateChangeReconciles()
        {
            var previous = Line("LINE", 10d, "m3", "CONC", "USD", 100m);
            var current = Line("LINE", 12d, "m3", "conc", "USD", 120m);
            var impact = EstimateRevisionCostImpact.Create(previous, current);

            Equal(2m, impact.EstimatingQuantityDelta, "Combined estimating quantity delta mismatch.");
            Equal(20m, impact.UnitRateDelta, "Combined unit-rate delta mismatch.");
            Equal(200m, impact.QuantityDrivenCostDelta, "Combined quantity-driven effect mismatch.");
            Equal(240m, impact.RateEffectAtCurrentQuantity, "Combined diagnostic rate effect mismatch.");
            Equal(240m, impact.RateDrivenCostDelta, "Combined reconciled rate effect mismatch.");
            Equal(0m, impact.RateEffectRoundingResidual, "Simple combined case should have zero rounding residual.");
            Equal(440m, impact.CostDelta, "Combined total cost delta mismatch.");
            Equal("m3", impact.Unit, "Impact unit mismatch.");
            Equal("USD", impact.Currency, "Impact currency mismatch.");
            True(impact.CostCode.Equals(new CostCode("conc")), "Cost-code comparison should preserve canonical case-insensitive identity.");
            Reconciles(impact);
        }

        private static void CommercialAdjustmentChangeRemainsQuantityDriven()
        {
            var previous = Line("LINE", 10d, "ea", "ITEM", "USD", 5m);
            var current = Line("LINE", 10d, "ea", "ITEM", "USD", 5m, 2m, "Approved allowance");
            var impact = EstimateRevisionCostImpact.Create(previous, current);

            Equal(0m, impact.MeasuredQuantityDelta, "Adjustment-only change must not rewrite measured quantity delta.");
            Equal(2m, impact.CommercialAdjustmentQuantityDelta, "Commercial adjustment delta mismatch.");
            Equal(2m, impact.EstimatingQuantityDelta, "Adjustment-only estimating quantity delta mismatch.");
            Equal(10m, impact.QuantityDrivenCostDelta, "Adjustment-only quantity-driven effect mismatch.");
            Equal(0m, impact.RateDrivenCostDelta, "Adjustment-only rate effect must be zero.");
            Equal(10m, impact.CostDelta, "Adjustment-only cost delta mismatch.");
            Reconciles(impact);
        }

        private static void UnchangedStateIsZero()
        {
            var previous = Line("LINE", 3d, "ea", "ITEM", "USD", 7m, 1m, "Allowance");
            var current = Line("LINE", 3d, "ea", "ITEM", "USD", 7m, 1m, "Allowance");
            var impact = EstimateRevisionCostImpact.Create(previous, current);

            Equal(0m, impact.MeasuredQuantityDelta, "Unchanged measured delta must be zero.");
            Equal(0m, impact.CommercialAdjustmentQuantityDelta, "Unchanged adjustment delta must be zero.");
            Equal(0m, impact.EstimatingQuantityDelta, "Unchanged estimating delta must be zero.");
            Equal(0m, impact.UnitRateDelta, "Unchanged rate delta must be zero.");
            Equal(0m, impact.QuantityDrivenCostDelta, "Unchanged quantity effect must be zero.");
            Equal(0m, impact.RateDrivenCostDelta, "Unchanged rate effect must be zero.");
            Equal(0m, impact.CostDelta, "Unchanged cost delta must be zero.");
            Reconciles(impact);
        }

        private static void ComparableScopeIsStrict()
        {
            var baseline = Line("LINE", 1d, "ea", "ITEM", "USD", 1m);

            Throws<ArgumentException>(() => EstimateRevisionCostImpact.Create(
                baseline,
                Line("OTHER", 1d, "ea", "ITEM", "USD", 1m)));
            Throws<ArgumentException>(() => EstimateRevisionCostImpact.Create(
                baseline,
                Line("LINE", 1d, "m3", "ITEM", "USD", 1m)));
            Throws<ArgumentException>(() => EstimateRevisionCostImpact.Create(
                baseline,
                Line("LINE", 1d, "ea", "ITEM", "VND", 1m)));
            Throws<ArgumentException>(() => EstimateRevisionCostImpact.Create(
                baseline,
                Line("LINE", 1d, "ea", "OTHER", "USD", 1m)));
            Throws<ArgumentNullException>(() => EstimateRevisionCostImpact.Create(null!, baseline));
            Throws<ArgumentNullException>(() => EstimateRevisionCostImpact.Create(baseline, null!));
        }

        private static void MeasurementIdentityScopeIsStrict()
        {
            var baseline = Line("LINE", 1d, "ea", "ITEM", "USD", 1m);

            Throws<ArgumentException>(() => EstimateRevisionCostImpact.Create(
                baseline,
                Line("LINE", 1d, "ea", "ITEM", "USD", 1m, semanticIdentity: "sem")));
            Throws<ArgumentException>(() => EstimateRevisionCostImpact.Create(
                baseline,
                Line("LINE", 1d, "ea", "ITEM", "USD", 1m, sourceIdentity: "SRC-OTHER")));
            Throws<ArgumentException>(() => EstimateRevisionCostImpact.Create(
                baseline,
                Line("LINE", 1d, "ea", "ITEM", "USD", 1m, quantityKey: "QTY-OTHER")));
        }

        private static void DecompositionOverflowFailsClosed()
        {
            var previous = Line("LINE", 1d, "m3", "CONC", "USD", decimal.MaxValue);
            var current = Line("LINE", 7e28d, "m3", "CONC", "USD", 1m);
            Throws<OverflowException>(() => EstimateRevisionCostImpact.Create(previous, current));
        }

        private static EstimateLine Line(
            string lineId,
            double measuredQuantity,
            string unit,
            string costCode,
            string currency,
            decimal unitRate,
            decimal adjustment = 0m,
            string? adjustmentReason = null,
            string semanticIdentity = "SEM",
            string sourceIdentity = "SRC",
            string quantityKey = "QTY")
        {
            var snapshot = new MeasurementSnapshot(new[]
            {
                new MeasurementTrace(
                    semanticIdentity,
                    sourceIdentity,
                    quantityKey,
                    Array.Empty<MeasurementTraceFact>(),
                    measuredQuantity,
                    Array.Empty<MeasurementTraceAdjustment>(),
                    measuredQuantity,
                    unit,
                    "none")
            });
            var book = new RateBook("BOOK-" + lineId + "-" + costCode + "-" + unitRate, new[]
            {
                new RateItem(
                    "RATE-" + lineId + "-" + costCode + "-" + unitRate,
                    new CostCode(costCode),
                    unit,
                    currency,
                    unitRate,
                    Utc(2026, 1, 1),
                    "v1")
            });

            return EstimateLine.Create(
                lineId,
                snapshot,
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                book,
                new CostCode(costCode),
                currency,
                Utc(2026, 1, 2),
                adjustment,
                adjustmentReason);
        }

        private static void Reconciles(EstimateRevisionCostImpact impact)
        {
            Equal(
                impact.CostDelta,
                impact.QuantityDrivenCostDelta + impact.RateDrivenCostDelta,
                "Revision impact components must reconcile exactly to total cost delta.");
            Equal(
                impact.RateDrivenCostDelta,
                impact.RateEffectAtCurrentQuantity + impact.RateEffectRoundingResidual,
                "Reconciled rate effect must expose its current-quantity formula plus rounding residual.");
        }

        private static DateTime Utc(int year, int month, int day) =>
            new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
