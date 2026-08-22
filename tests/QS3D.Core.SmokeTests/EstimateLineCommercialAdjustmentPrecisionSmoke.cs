using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimateLineCommercialAdjustmentPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            SwallowedPositiveAdjustmentFailsClosed();
            SwallowedNegativeAdjustmentFailsClosed();
            RepresentableAdjustmentsRemainStable();
            ExactZeroAndCancellationRemainAccepted();
        }

        private static void SwallowedPositiveAdjustmentFailsClosed()
        {
            Throws<OverflowException>(() =>
                CreateLine(
                    7e28,
                    0.1m,
                    "Small positive adjustment",
                    1m));
        }

        private static void SwallowedNegativeAdjustmentFailsClosed()
        {
            Throws<OverflowException>(() =>
                CreateLine(
                    7e28,
                    -0.1m,
                    "Small negative adjustment",
                    1m));
        }

        private static void RepresentableAdjustmentsRemainStable()
        {
            var positive = CreateLine(10d, 2m, "Positive adjustment", 3m);
            Equal(10m, positive.MeasuredQuantity, "Positive adjustment measured quantity changed.");
            Equal(2m, positive.CommercialAdjustmentQuantity, "Positive adjustment identity changed.");
            Equal(12m, positive.EstimatingQuantity, "Positive adjustment estimating quantity changed.");
            Equal(36m, positive.FinalAmount, "Positive adjustment final amount changed.");

            var negative = CreateLine(10d, -2m, "Negative adjustment", 3m);
            Equal(10m, negative.MeasuredQuantity, "Negative adjustment measured quantity changed.");
            Equal(-2m, negative.CommercialAdjustmentQuantity, "Negative adjustment identity changed.");
            Equal(8m, negative.EstimatingQuantity, "Negative adjustment estimating quantity changed.");
            Equal(24m, negative.FinalAmount, "Negative adjustment final amount changed.");
        }

        private static void ExactZeroAndCancellationRemainAccepted()
        {
            var zero = CreateLine(10d, 0m, null, 3m);
            Equal(10m, zero.EstimatingQuantity, "Exact-zero adjustment changed estimating quantity.");
            Equal(30m, zero.FinalAmount, "Exact-zero adjustment changed final amount.");
            Equal<string?>(null, zero.CommercialAdjustmentReason, "Exact-zero adjustment unexpectedly retained a reason.");

            var cancellation = CreateLine(5d, -5m, "Full cancellation", 3m);
            Equal(0m, cancellation.EstimatingQuantity, "Exact cancellation must remain representable as zero.");
            Equal(0m, cancellation.FinalAmount, "Exact cancellation final amount must remain zero.");
        }

        private static EstimateLine CreateLine(
            double measuredQuantity,
            decimal commercialAdjustmentQuantity,
            string? commercialAdjustmentReason,
            decimal unitRate)
        {
            const string semanticIdentity = "SEM-ESTIMATE";
            const string sourceIdentity = "SRC-ESTIMATE";
            const string quantityKey = "volume";
            const string unit = "m3";
            const string currency = "VND";

            var trace = new MeasurementTrace(
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                new MeasurementTraceFact[0],
                measuredQuantity,
                new MeasurementTraceAdjustment[0],
                measuredQuantity,
                unit,
                "none");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var costCode = new CostCode("CC-ESTIMATE");
            var effectiveFromUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var rateItem = new RateItem(
                "RATE-ESTIMATE",
                costCode,
                unit,
                currency,
                unitRate,
                effectiveFromUtc,
                "v1");
            var rateBook = new RateBook("BOOK-ESTIMATE", new[] { rateItem });

            return EstimateLine.Create(
                "LINE-ESTIMATE",
                snapshot,
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                rateBook,
                costCode,
                currency,
                effectiveFromUtc,
                commercialAdjustmentQuantity,
                commercialAdjustmentReason);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected: " + expected + "; actual: " + actual + ".");
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
