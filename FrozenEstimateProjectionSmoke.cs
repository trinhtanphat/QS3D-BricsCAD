using System;
using System.Collections.Generic;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class FrozenEstimateProjectionSmoke
    {
        internal static void Run()
        {
            CopiesCanonicalEstimateStateInDeterministicOrder();
            MaterializesDetachedReadOnlyRows();
            InvalidProjectionInputsFailClosed();
        }

        private static void CopiesCanonicalEstimateStateInDeterministicOrder()
        {
            var later = Line("Z-LINE", 4d, 9m, 0m, null);
            var earlier = Line("A-LINE", 10d, 7m, 2.5m, "Explicit estimating allowance");

            var projection = FrozenEstimateProjection.Create(new[] { later, earlier });

            Equal(2, projection.Rows.Count, "Projection row count mismatch.");
            Equal("A-LINE", projection.Rows[0].EstimateLineId, "Projection order must be canonical by line identity.");
            Equal("Z-LINE", projection.Rows[1].EstimateLineId, "Projection order must be independent of input enumeration order.");

            var row = projection.Rows[0];
            Equal(earlier.EstimateLineId, row.EstimateLineId, "Estimate line identity must be copied exactly.");
            Equal(earlier.MeasurementTrace.SemanticIdentity, row.SemanticIdentity, "Semantic identity must be copied exactly.");
            Equal(earlier.MeasurementTrace.SourceIdentity, row.SourceIdentity, "Source identity must be copied exactly.");
            Equal(earlier.MeasurementTrace.QuantityKey, row.QuantityKey, "Quantity key must be copied exactly.");
            Equal(earlier.RateBook.RateBookId, row.RateBookId, "Rate-book identity must be copied exactly.");
            Equal(earlier.RateItem.RateItemId, row.RateItemId, "Rate-item identity must be copied exactly.");
            Equal(earlier.RateItem.Version, row.RateVersion, "Rate version must be copied exactly.");
            Equal(earlier.RateAsOfUtc, row.RateAsOfUtc, "Rate as-of timestamp must be copied exactly.");
            Equal(earlier.CostCode.Value, row.CostCode, "Cost code must be copied exactly.");
            Equal(earlier.Unit, row.Unit, "Unit must be copied exactly.");
            Equal(earlier.Currency, row.Currency, "Currency must be copied exactly.");
            Equal(earlier.MeasuredQuantity, row.MeasuredQuantity, "Measured quantity must be copied exactly.");
            Equal(earlier.CommercialAdjustmentQuantity, row.CommercialAdjustmentQuantity, "Commercial adjustment must be copied exactly.");
            Equal(earlier.CommercialAdjustmentReason, row.CommercialAdjustmentReason, "Commercial adjustment reason must be copied exactly.");
            Equal(earlier.EstimatingQuantity, row.EstimatingQuantity, "Estimating quantity must be copied exactly.");
            Equal(earlier.UnitRate, row.UnitRate, "Unit rate must be copied exactly.");
            Equal(earlier.FinalAmount, row.FinalAmount, "Final amount must be copied exactly rather than recreated by the projection.");
        }

        private static void MaterializesDetachedReadOnlyRows()
        {
            var source = new List<EstimateLine>
            {
                Line("B-LINE", 2d, 5m, 0m, null),
                Line("A-LINE", 3d, 6m, 0m, null)
            };

            var projection = FrozenEstimateProjection.Create(source);
            source.Clear();

            Equal(2, projection.Rows.Count, "Projection must not remain backed by the caller collection.");
            Equal("A-LINE", projection.Rows[0].EstimateLineId, "Detached projection must retain its canonical row order.");

            var rows = (IList<FrozenEstimateProjectionRow>)projection.Rows;
            Throws<NotSupportedException>(() => rows.Clear());

            var empty = FrozenEstimateProjection.Create(Array.Empty<EstimateLine>());
            Equal(0, empty.Rows.Count, "Empty canonical estimate state should project to an empty frozen row set.");
        }

        private static void InvalidProjectionInputsFailClosed()
        {
            Throws<ArgumentNullException>(() => FrozenEstimateProjection.Create(null!));

            var line = Line("LINE", 1d, 1m, 0m, null);
            Throws<ArgumentException>(() => FrozenEstimateProjection.Create(new EstimateLine[] { line, null! }));
            Throws<ArgumentException>(() => FrozenEstimateProjection.Create(new[] { line, line }));

            var upper = Line("CASE-LINE", 1d, 1m, 0m, null);
            var lower = Line("case-line", 1d, 1m, 0m, null);
            Throws<ArgumentException>(() => FrozenEstimateProjection.Create(new[] { upper, lower }));
        }

        private static EstimateLine Line(
            string lineId,
            double measuredQuantity,
            decimal unitRate,
            decimal adjustment,
            string? adjustmentReason)
        {
            var semanticIdentity = "SEM-" + lineId;
            var sourceIdentity = "SRC-" + lineId;
            var trace = new MeasurementTrace(
                semanticIdentity,
                sourceIdentity,
                "Count",
                Array.Empty<MeasurementTraceFact>(),
                measuredQuantity,
                Array.Empty<MeasurementTraceAdjustment>(),
                measuredQuantity,
                "ea",
                "none");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var rateBook = new RateBook("BOOK-" + lineId, new[]
            {
                new RateItem(
                    "RATE-" + lineId,
                    new CostCode("ITEM"),
                    "ea",
                    "USD",
                    unitRate,
                    Utc(2026, 1, 1),
                    "v1")
            });

            return EstimateLine.Create(
                lineId,
                snapshot,
                semanticIdentity,
                sourceIdentity,
                "Count",
                rateBook,
                new CostCode("ITEM"),
                "USD",
                Utc(2026, 1, 2),
                adjustment,
                adjustmentReason);
        }

        private static DateTime Utc(int year, int month, int day) =>
            new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

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