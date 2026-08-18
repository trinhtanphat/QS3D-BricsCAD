using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimateRevisionPrecisionLossSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            SwallowedPreviousAdjustmentFailsClosed();
            SwallowedCurrentAdjustmentFailsClosed();
            SwallowedPreviousRateFailsClosed();
            ExactLargeAdjustmentDeltaRemainsValid();
            OrdinaryDecompositionStillReconciles();
        }

        private static void SwallowedPreviousAdjustmentFailsClosed()
        {
            var previous = Line(0d, 0m, 0.1m);
            var current = Line(0d, 0m, decimal.MaxValue);
            ThrowsOverflow(() => EstimateRevisionCostImpact.Create(previous, current));
        }

        private static void SwallowedCurrentAdjustmentFailsClosed()
        {
            var previous = Line(0d, 0m, decimal.MaxValue);
            var current = Line(0d, 0m, 0.1m);
            ThrowsOverflow(() => EstimateRevisionCostImpact.Create(previous, current));
        }

        private static void SwallowedPreviousRateFailsClosed()
        {
            var previous = Line(0d, 0.1m, 0m);
            var current = Line(0d, decimal.MaxValue, 0m);
            ThrowsOverflow(() => EstimateRevisionCostImpact.Create(previous, current));
        }

        private static void ExactLargeAdjustmentDeltaRemainsValid()
        {
            var previous = Line(0d, 0m, 1m);
            var current = Line(0d, 0m, decimal.MaxValue);
            var impact = EstimateRevisionCostImpact.Create(previous, current);

            Equal(decimal.MaxValue - 1m, impact.CommercialAdjustmentQuantityDelta,
                "Exact representable commercial-adjustment delta changed.");
            Equal(decimal.MaxValue - 1m, impact.EstimatingQuantityDelta,
                "Exact representable estimating-quantity delta changed.");
            Equal(0m, impact.CostDelta, "Zero-rate exact large delta must keep zero cost impact.");
            Reconciles(impact);
        }

        private static void OrdinaryDecompositionStillReconciles()
        {
            var previous = Line(10d, 100m, 0m);
            var current = Line(12d, 120m, 0m);
            var impact = EstimateRevisionCostImpact.Create(previous, current);

            Equal(2m, impact.EstimatingQuantityDelta, "Ordinary estimating-quantity delta changed.");
            Equal(20m, impact.UnitRateDelta, "Ordinary unit-rate delta changed.");
            Equal(200m, impact.QuantityDrivenCostDelta, "Ordinary quantity-driven effect changed.");
            Equal(240m, impact.RateDrivenCostDelta, "Ordinary rate-driven effect changed.");
            Equal(440m, impact.CostDelta, "Ordinary total cost delta changed.");
            Reconciles(impact);
        }

        private static EstimateLine Line(double measuredQuantity, decimal unitRate, decimal adjustment)
        {
            var trace = new MeasurementTrace(
                "SEM",
                "SRC",
                "QTY",
                Array.Empty<MeasurementTraceFact>(),
                measuredQuantity,
                Array.Empty<MeasurementTraceAdjustment>(),
                measuredQuantity,
                "ea",
                "none");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var book = new RateBook(
                "BOOK",
                new[]
                {
                    new RateItem(
                        "RATE",
                        new CostCode("ITEM"),
                        "ea",
                        "USD",
                        unitRate,
                        Utc(2026, 1, 1),
                        "v1")
                });

            return EstimateLine.Create(
                "LINE",
                snapshot,
                "SEM",
                "SRC",
                "QTY",
                book,
                new CostCode("ITEM"),
                "USD",
                Utc(2026, 1, 2),
                adjustment,
                adjustment == 0m ? null : "Revision allowance");
        }

        private static void Reconciles(EstimateRevisionCostImpact impact)
        {
            Equal(
                impact.CostDelta,
                checked(impact.QuantityDrivenCostDelta + impact.RateDrivenCostDelta),
                "Revision cost decomposition no longer reconciles exactly.");
            Equal(
                impact.RateDrivenCostDelta,
                checked(impact.RateEffectAtCurrentQuantity + impact.RateEffectRoundingResidual),
                "Rate-effect decomposition no longer reconciles exactly.");
        }

        private static DateTime Utc(int year, int month, int day) =>
            new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void ThrowsOverflow(Action action)
        {
            try
            {
                action();
            }
            catch (OverflowException)
            {
                return;
            }

            throw new InvalidOperationException("Expected OverflowException for swallowed non-zero revision contribution.");
        }
    }
}
