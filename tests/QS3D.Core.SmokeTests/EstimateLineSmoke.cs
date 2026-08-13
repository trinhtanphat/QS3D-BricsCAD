using System;
using System.Collections.Generic;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimateLineSmoke
    {
        internal static void Run()
        {
            BindsFrozenMeasurementAndEffectiveRate();
            SupportsExplicitAdditiveCommercialAdjustment();
            CanonicalZeroCommercialAdjustment();
            MissingMeasurementOrRateFailsClosed();
            InvalidAdjustmentAndCanonicalInputsFailClosed();
            DecimalPrecisionLossAndOverflowFailClosed();
        }

        private static void BindsFrozenMeasurementAndEffectiveRate()
        {
            var trace = Trace("SEM-1", "SRC-1", "NetVolumeM3", 10d, "m3");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var rateBook = new RateBook("BOOK-2026", new[]
            {
                Rate("RATE-JAN", "CONC", "m3", "VND", 100m, Utc(2026, 1, 1), "v1"),
                Rate("RATE-FEB", "CONC", "m3", "VND", 120m, Utc(2026, 2, 1), "v2")
            });

            var line = EstimateLine.Create(
                "LINE-1",
                snapshot,
                "SEM-1",
                "SRC-1",
                "NetVolumeM3",
                rateBook,
                new CostCode("CONC"),
                "VND",
                Utc(2026, 2, 15),
                1.5m,
                "Explicit waste allowance");

            Equal("LINE-1", line.EstimateLineId, "Estimate line identity mismatch.");
            True(ReferenceEquals(snapshot, line.MeasurementSnapshot), "Estimate line must retain the frozen measurement snapshot reference.");
            True(ReferenceEquals(trace, line.MeasurementTrace), "Estimate line must retain the exact selected measurement trace.");
            True(ReferenceEquals(rateBook, line.RateBook), "Estimate line must retain the frozen rate-book reference.");
            Equal("RATE-FEB", line.RateItem.RateItemId, "Estimate line must select the latest eligible effective rate.");
            Equal("v2", line.RateItem.Version, "Estimate line rate version mismatch.");
            Equal(Utc(2026, 2, 15), line.RateAsOfUtc, "Estimate line rate as-of timestamp mismatch.");
            Equal(10m, line.MeasuredQuantity, "Measured quantity mismatch.");
            Equal(1.5m, line.CommercialAdjustmentQuantity, "Commercial adjustment quantity mismatch.");
            Equal("Explicit waste allowance", line.CommercialAdjustmentReason, "Commercial adjustment reason mismatch.");
            Equal(11.5m, line.EstimatingQuantity, "Estimating quantity mismatch.");
            Equal("m3", line.Unit, "Estimate line unit mismatch.");
            Equal("CONC", line.CostCode.Value, "Estimate line cost code mismatch.");
            Equal("VND", line.Currency, "Estimate line currency mismatch.");
            Equal(120m, line.UnitRate, "Estimate line unit rate mismatch.");
            Equal(1380m, line.FinalAmount, "Estimate line final amount mismatch.");
        }

        private static void SupportsExplicitAdditiveCommercialAdjustment()
        {
            var snapshot = new MeasurementSnapshot(new[] { Trace("SEM-1", "SRC-1", "Count", 10d, "ea") });
            var rateBook = new RateBook("BOOK", new[]
            {
                Rate("RATE", "ITEM", "ea", "USD", 3m, Utc(2026, 1, 1), "v1")
            });

            var zero = EstimateLine.Create(
                "ZERO",
                snapshot,
                "SEM-1",
                "SRC-1",
                "Count",
                rateBook,
                new CostCode("ITEM"),
                "USD",
                Utc(2026, 1, 2));
            Equal(10m, zero.MeasuredQuantity, "Zero-adjustment measured quantity mismatch.");
            Equal(0m, zero.CommercialAdjustmentQuantity, "Zero adjustment mismatch.");
            True(zero.CommercialAdjustmentReason == null, "Zero adjustment should not invent a reason.");
            Equal(10m, zero.EstimatingQuantity, "Zero-adjustment estimating quantity mismatch.");
            Equal(30m, zero.FinalAmount, "Zero-adjustment final amount mismatch.");

            var deduction = EstimateLine.Create(
                "DEDUCTION",
                snapshot,
                "SEM-1",
                "SRC-1",
                "Count",
                rateBook,
                new CostCode("ITEM"),
                "USD",
                Utc(2026, 1, 2),
                -2m,
                "Explicit commercial deduction");
            Equal(10m, deduction.MeasuredQuantity, "Deduction must not rewrite measured quantity.");
            Equal(-2m, deduction.CommercialAdjustmentQuantity, "Deduction quantity mismatch.");
            Equal(8m, deduction.EstimatingQuantity, "Deduction estimating quantity mismatch.");
            Equal(24m, deduction.FinalAmount, "Deduction final amount mismatch.");
        }

        private static void CanonicalZeroCommercialAdjustment()
        {
            var snapshot = new MeasurementSnapshot(new[] { Trace("SEM-ZERO", "SRC-ZERO", "Count", 10d, "ea") });
            var rateBook = new RateBook("BOOK-ZERO", new[]
            {
                Rate("RATE-ZERO", "ITEM", "ea", "USD", 3m, Utc(2026, 1, 1), "v1")
            });
            var negativeZero = new decimal(0, 0, 0, true, 0);
            var line = EstimateLine.Create(
                "ZERO-SIGN",
                snapshot,
                "SEM-ZERO",
                "SRC-ZERO",
                "Count",
                rateBook,
                new CostCode("ITEM"),
                "USD",
                Utc(2026, 1, 2),
                negativeZero);
            var expectedBits = decimal.GetBits(0m);
            var actualBits = decimal.GetBits(line.CommercialAdjustmentQuantity);

            Equal(0m, line.CommercialAdjustmentQuantity, "Signed-zero adjustment value mismatch.");
            Equal(expectedBits.Length, actualBits.Length, "Decimal bit-vector length mismatch.");
            for (var i = 0; i < expectedBits.Length; i++)
                Equal(expectedBits[i], actualBits[i], "Commercial adjustment zero must use canonical positive decimal representation at bit index " + i + ".");
            True(line.CommercialAdjustmentReason == null, "Canonical zero adjustment must not require or invent a reason.");
            Equal(10m, line.EstimatingQuantity, "Canonical zero adjustment must not change estimating quantity.");
            Equal(30m, line.FinalAmount, "Canonical zero adjustment must not change final amount.");
        }

        private static void MissingMeasurementOrRateFailsClosed()
        {
            var snapshot = new MeasurementSnapshot(new[] { Trace("SEM-1", "SRC-1", "NetVolumeM3", 1d, "m3") });
            var rateBook = new RateBook("BOOK", new[]
            {
                Rate("RATE", "CONC", "m3", "VND", 100m, Utc(2026, 2, 1), "v1")
            });

            Throws<InvalidOperationException>(() => EstimateLine.Create(
                "LINE",
                snapshot,
                "SEM-MISSING",
                "SRC-1",
                "NetVolumeM3",
                rateBook,
                new CostCode("CONC"),
                "VND",
                Utc(2026, 2, 2)));

            Throws<InvalidOperationException>(() => EstimateLine.Create(
                "LINE",
                snapshot,
                "SEM-1",
                "SRC-1",
                "NetVolumeM3",
                rateBook,
                new CostCode("STEEL"),
                "VND",
                Utc(2026, 2, 2)));

            Throws<InvalidOperationException>(() => EstimateLine.Create(
                "LINE",
                snapshot,
                "SEM-1",
                "SRC-1",
                "NetVolumeM3",
                rateBook,
                new CostCode("CONC"),
                "VND",
                Utc(2026, 1, 1)));
        }

        private static void InvalidAdjustmentAndCanonicalInputsFailClosed()
        {
            var snapshot = new MeasurementSnapshot(new[] { Trace("SEM-1", "SRC-1", "Count", 1d, "ea") });
            var rateBook = new RateBook("BOOK", new[]
            {
                Rate("RATE", "ITEM", "ea", "USD", 1m, Utc(2026, 1, 1), "v1")
            });

            Throws<ArgumentException>(() => EstimateLine.Create(
                "LINE",
                snapshot,
                "SEM-1",
                "SRC-1",
                "Count",
                rateBook,
                new CostCode("ITEM"),
                "USD",
                Utc(2026, 1, 2),
                1m));

            Throws<ArgumentException>(() => EstimateLine.Create(
                "LINE",
                snapshot,
                "SEM-1",
                "SRC-1",
                "Count",
                rateBook,
                new CostCode("ITEM"),
                "USD",
                Utc(2026, 1, 2),
                1m,
                " padded "));

            Throws<ArgumentOutOfRangeException>(() => EstimateLine.Create(
                "LINE",
                snapshot,
                "SEM-1",
                "SRC-1",
                "Count",
                rateBook,
                new CostCode("ITEM"),
                "USD",
                Utc(2026, 1, 2),
                -2m,
                "Too large deduction"));

            Throws<ArgumentException>(() => EstimateLine.Create(
                " LINE",
                snapshot,
                "SEM-1",
                "SRC-1",
                "Count",
                rateBook,
                new CostCode("ITEM"),
                "USD",
                Utc(2026, 1, 2)));

            Throws<ArgumentException>(() => EstimateLine.Create(
                "LINE",
                snapshot,
                "SEM-1",
                "SRC-1",
                "Count",
                rateBook,
                new CostCode("ITEM"),
                "usd",
                Utc(2026, 1, 2)));

            Throws<ArgumentException>(() => EstimateLine.Create(
                "LINE",
                snapshot,
                "SEM-1",
                "SRC-1",
                "Count",
                rateBook,
                new CostCode("ITEM"),
                "USD",
                new DateTime(2026, 1, 2)));

            Throws<ArgumentNullException>(() => EstimateLine.Create(
                "LINE",
                null!,
                "SEM-1",
                "SRC-1",
                "Count",
                rateBook,
                new CostCode("ITEM"),
                "USD",
                Utc(2026, 1, 2)));
        }

        private static void DecimalPrecisionLossAndOverflowFailClosed()
        {
            var tinySnapshot = new MeasurementSnapshot(new[] { Trace("SEM-TINY", "SRC-TINY", "NetVolumeM3", 1e-29d, "m3") });
            var tinyBook = new RateBook("TINY", new[]
            {
                Rate("TINY-RATE", "CONC", "m3", "USD", 1m, Utc(2026, 1, 1), "v1")
            });
            Throws<OverflowException>(() => EstimateLine.Create(
                "TINY-LINE",
                tinySnapshot,
                "SEM-TINY",
                "SRC-TINY",
                "NetVolumeM3",
                tinyBook,
                new CostCode("CONC"),
                "USD",
                Utc(2026, 1, 2)));

            var largeSnapshot = new MeasurementSnapshot(new[] { Trace("SEM-LARGE", "SRC-LARGE", "NetVolumeM3", 7e28d, "m3") });
            var largeBook = new RateBook("LARGE", new[]
            {
                Rate("LARGE-RATE", "CONC", "m3", "USD", 2m, Utc(2026, 1, 1), "v1")
            });
            Throws<OverflowException>(() => EstimateLine.Create(
                "LARGE-LINE",
                largeSnapshot,
                "SEM-LARGE",
                "SRC-LARGE",
                "NetVolumeM3",
                largeBook,
                new CostCode("CONC"),
                "USD",
                Utc(2026, 1, 2)));
        }

        private static MeasurementTrace Trace(
            string semanticIdentity,
            string sourceIdentity,
            string quantityKey,
            double netValue,
            string unit) =>
            new MeasurementTrace(
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                Array.Empty<MeasurementTraceFact>(),
                netValue,
                Array.Empty<MeasurementTraceAdjustment>(),
                netValue,
                unit,
                "none");

        private static RateItem Rate(
            string id,
            string costCode,
            string unit,
            string currency,
            decimal unitRate,
            DateTime effectiveFromUtc,
            string version) =>
            new RateItem(id, new CostCode(costCode), unit, currency, unitRate, effectiveFromUtc, version);

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
