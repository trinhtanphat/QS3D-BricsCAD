using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementCommercialUtf16IntegritySmoke
    {
        internal static void Run()
        {
            MalformedMeasurementTextFailsClosed();
            MalformedCommercialReasonFailsClosed();
            ValidSupplementaryUnicodeIsPreserved();
        }

        private static void MalformedMeasurementTextFailsClosed()
        {
            var high = "\uD800";
            var low = "\uDC00";

            Throws<ArgumentException>(() => new MeasurementTraceFact("Width" + high, 1d, "m"));
            Throws<ArgumentException>(() => new MeasurementTraceFact("Width", 1d, "m", "SRC" + low));
            Throws<ArgumentException>(() => new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Addition,
                1d,
                "m2",
                "allowance " + high,
                "SRC-ALLOWANCE"));
            Throws<ArgumentException>(() => new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Addition,
                1d,
                "m2",
                "allowance",
                "SRC" + low));

            Throws<ArgumentException>(() => CreateTrace("SEM" + high, "SRC", "NetAreaM2", null, null));
            Throws<ArgumentException>(() => CreateTrace("SEM", "SRC" + low, "NetAreaM2", null, null));
            Throws<ArgumentException>(() => CreateTrace("SEM", "SRC", "NetArea" + high, null, null));
            Throws<ArgumentException>(() => CreateTrace("SEM", "SRC", "NetAreaM2", new[] { "warning " + high }, null));
            Throws<ArgumentException>(() => CreateTrace("SEM", "SRC", "NetAreaM2", null, new[] { "assumption " + low }));
        }

        private static void MalformedCommercialReasonFailsClosed()
        {
            var high = "\uD800";
            var low = "\uDC00";
            var fixture = CreateEstimateFixture();

            Throws<ArgumentException>(() => EstimateLine.Create(
                "LINE-HIGH",
                fixture.Snapshot,
                fixture.Trace.SemanticIdentity,
                fixture.Trace.SourceIdentity,
                fixture.Trace.QuantityKey,
                fixture.Book,
                new CostCode("CONC"),
                "VND",
                fixture.AsOfUtc,
                1m,
                "commercial allowance " + high));

            Throws<ArgumentException>(() => EstimateLine.Create(
                "LINE-LOW",
                fixture.Snapshot,
                fixture.Trace.SemanticIdentity,
                fixture.Trace.SourceIdentity,
                fixture.Trace.QuantityKey,
                fixture.Book,
                new CostCode("CONC"),
                "VND",
                fixture.AsOfUtc,
                1m,
                "commercial allowance " + low));
        }

        private static void ValidSupplementaryUnicodeIsPreserved()
        {
            var supplementary = char.ConvertFromUtf32(0x1F680);
            var fact = new MeasurementTraceFact("Width" + supplementary, 1d, "m", "SRC" + supplementary);
            var adjustment = new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Addition,
                1d,
                "m2",
                "allowance " + supplementary,
                "SRC-ALLOWANCE" + supplementary);
            var trace = new MeasurementTrace(
                "SEM" + supplementary,
                "SRC" + supplementary,
                "NetAreaM2" + supplementary,
                new[] { fact },
                10d,
                new[] { adjustment },
                11d,
                "m2",
                "none",
                new[] { "warning " + supplementary },
                new[] { "assumption " + supplementary },
                "rule" + supplementary,
                "v1" + supplementary);

            Equal("SEM" + supplementary, trace.SemanticIdentity, "Supplementary semantic identity must remain exact.");
            Equal("Width" + supplementary, trace.InputFacts[0].Name, "Supplementary fact identity must remain exact.");
            Equal("allowance " + supplementary, trace.Adjustments[0].Reason, "Supplementary adjustment reason must remain exact.");
            Equal("warning " + supplementary, trace.Warnings[0], "Supplementary warning must remain exact.");
            True(trace.ToCanonicalString().Contains(supplementary), "Canonical trace must preserve valid supplementary text.");

            var fixture = CreateEstimateFixture();
            var reason = "commercial allowance " + supplementary;
            var line = EstimateLine.Create(
                "LINE" + supplementary,
                fixture.Snapshot,
                fixture.Trace.SemanticIdentity,
                fixture.Trace.SourceIdentity,
                fixture.Trace.QuantityKey,
                fixture.Book,
                new CostCode("CONC"),
                "VND",
                fixture.AsOfUtc,
                1m,
                reason);

            Equal(reason, line.CommercialAdjustmentReason, "Supplementary commercial adjustment reason must remain exact.");
            Equal(11m, line.EstimatingQuantity, "Commercial adjustment quantity semantics changed.");
            Equal(22m, line.FinalAmount, "Commercial adjustment amount semantics changed.");
        }

        private static MeasurementTrace CreateTrace(
            string semanticIdentity,
            string sourceIdentity,
            string quantityKey,
            IEnumerable<string>? warnings,
            IEnumerable<string>? assumptions)
        {
            return new MeasurementTrace(
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                Array.Empty<MeasurementTraceFact>(),
                10d,
                Array.Empty<MeasurementTraceAdjustment>(),
                10d,
                "m2",
                "none",
                warnings,
                assumptions);
        }

        private static EstimateFixture CreateEstimateFixture()
        {
            var asOfUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            var trace = CreateTrace("SEM-WALL", "SRC-WALL", "NetAreaM2", null, null);
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var book = new RateBook(
                "BOOK",
                new[]
                {
                    new RateItem(
                        "RATE-CONC",
                        new CostCode("CONC"),
                        "m2",
                        "VND",
                        2m,
                        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        "v1")
                });
            return new EstimateFixture(trace, snapshot, book, asOfUtc);
        }

        private sealed class EstimateFixture
        {
            internal EstimateFixture(MeasurementTrace trace, MeasurementSnapshot snapshot, RateBook book, DateTime asOfUtc)
            {
                Trace = trace;
                Snapshot = snapshot;
                Book = book;
                AsOfUtc = asOfUtc;
            }

            internal MeasurementTrace Trace { get; }
            internal MeasurementSnapshot Snapshot { get; }
            internal RateBook Book { get; }
            internal DateTime AsOfUtc { get; }
        }

        private static void Throws<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }

    internal static class MeasurementCommercialUtf16IntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasurementCommercialUtf16IntegritySmoke.Run();
        }
    }
}
