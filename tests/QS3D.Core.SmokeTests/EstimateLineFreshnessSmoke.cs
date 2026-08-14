using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimateLineFreshnessSmoke
    {
        private static readonly DateTime EffectiveUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime AsOfUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize()
        {
            EquivalentInputsRemainCurrent();
            MeasurementChangesRemainVisible();
            MissingMeasurementRemainsVisible();
            RateBookProvenanceChangesRemainVisible();
            UnavailableRatesRemainVisible();
            ChangedRatesRemainVisible();
            CombinedFindingsAreDeterministic();
            RateItemIdentityCasingUsesRateBookIdentitySemantics();
            NullArgumentsFailClosed();
        }

        private static void EquivalentInputsRemainCurrent()
        {
            var frozenSnapshot = Snapshot(Trace(10d));
            var frozenRateBook = RateBook("rates", "rate-1", 25m, "v1");
            var line = Line(frozenSnapshot, frozenRateBook);

            var current = EstimateLineFreshnessEvaluator.Evaluate(
                line,
                Snapshot(Trace(10d)),
                RateBook("rates", "rate-1", 25m, "v1"));

            True(current.IsCurrent);
            Equal(0, current.Findings.Count);
            True(current.CurrentMeasurementTrace != null);
            True(current.CurrentRateItem != null);
        }

        private static void MeasurementChangesRemainVisible()
        {
            var frozenSnapshot = Snapshot(Trace(10d));
            var rateBook = RateBook("rates", "rate-1", 25m, "v1");
            var line = Line(frozenSnapshot, rateBook);

            var current = EstimateLineFreshnessEvaluator.Evaluate(line, Snapshot(Trace(12d)), rateBook);

            False(current.IsCurrent);
            Findings(current, EstimateLineFreshnessFindingKind.MeasurementChanged);
        }

        private static void MissingMeasurementRemainsVisible()
        {
            var frozenSnapshot = Snapshot(Trace(10d));
            var rateBook = RateBook("rates", "rate-1", 25m, "v1");
            var line = Line(frozenSnapshot, rateBook);

            var current = EstimateLineFreshnessEvaluator.Evaluate(
                line,
                Snapshot(Trace(10d, semanticIdentity: "other-element")),
                rateBook);

            Findings(current, EstimateLineFreshnessFindingKind.MeasurementMissing);
            True(current.CurrentMeasurementTrace == null);
        }

        private static void RateBookProvenanceChangesRemainVisible()
        {
            var frozenSnapshot = Snapshot(Trace(10d));
            var frozenRateBook = RateBook("rates", "rate-1", 25m, "v1");
            var line = Line(frozenSnapshot, frozenRateBook);

            var current = EstimateLineFreshnessEvaluator.Evaluate(
                line,
                Snapshot(Trace(10d)),
                RateBook("rates-next", "rate-1", 25m, "v1"));

            Findings(current, EstimateLineFreshnessFindingKind.RateBookChanged);
        }

        private static void UnavailableRatesRemainVisible()
        {
            var frozenSnapshot = Snapshot(Trace(10d));
            var frozenRateBook = RateBook("rates", "rate-1", 25m, "v1");
            var line = Line(frozenSnapshot, frozenRateBook);
            var currentRateBook = new RateBook("rates", Array.Empty<RateItem>());

            var current = EstimateLineFreshnessEvaluator.Evaluate(line, Snapshot(Trace(10d)), currentRateBook);

            Findings(current, EstimateLineFreshnessFindingKind.RateUnavailable);
            True(current.CurrentRateItem == null);
        }

        private static void ChangedRatesRemainVisible()
        {
            var frozenSnapshot = Snapshot(Trace(10d));
            var frozenRateBook = RateBook("rates", "rate-1", 25m, "v1");
            var line = Line(frozenSnapshot, frozenRateBook);

            var current = EstimateLineFreshnessEvaluator.Evaluate(
                line,
                Snapshot(Trace(10d)),
                RateBook("rates", "rate-1", 30m, "v2"));

            Findings(current, EstimateLineFreshnessFindingKind.RateChanged);
        }

        private static void CombinedFindingsAreDeterministic()
        {
            var frozenSnapshot = Snapshot(Trace(10d));
            var frozenRateBook = RateBook("rates", "rate-1", 25m, "v1");
            var line = Line(frozenSnapshot, frozenRateBook);

            var current = EstimateLineFreshnessEvaluator.Evaluate(
                line,
                Snapshot(Trace(10d, semanticIdentity: "other-element")),
                RateBook("rates-next", "rate-1", 30m, "v2"));

            Findings(
                current,
                EstimateLineFreshnessFindingKind.MeasurementMissing,
                EstimateLineFreshnessFindingKind.RateBookChanged,
                EstimateLineFreshnessFindingKind.RateChanged);
        }

        private static void RateItemIdentityCasingUsesRateBookIdentitySemantics()
        {
            var frozenSnapshot = Snapshot(Trace(10d));
            var frozenRateBook = RateBook("rates", "rate-1", 25m, "v1");
            var line = Line(frozenSnapshot, frozenRateBook);

            var current = EstimateLineFreshnessEvaluator.Evaluate(
                line,
                Snapshot(Trace(10d)),
                RateBook("rates", "RATE-1", 25m, "v1"));

            True(current.IsCurrent);
        }

        private static void NullArgumentsFailClosed()
        {
            var snapshot = Snapshot(Trace(10d));
            var rateBook = RateBook("rates", "rate-1", 25m, "v1");
            var line = Line(snapshot, rateBook);

            Throws<ArgumentNullException>(() => EstimateLineFreshnessEvaluator.Evaluate(null!, snapshot, rateBook));
            Throws<ArgumentNullException>(() => EstimateLineFreshnessEvaluator.Evaluate(line, null!, rateBook));
            Throws<ArgumentNullException>(() => EstimateLineFreshnessEvaluator.Evaluate(line, snapshot, null!));
        }

        private static EstimateLine Line(MeasurementSnapshot snapshot, RateBook rateBook)
        {
            return EstimateLine.Create(
                "estimate-1",
                snapshot,
                "element-1",
                "source-1",
                "NetVolumeM3",
                rateBook,
                new CostCode("C001"),
                "USD",
                AsOfUtc);
        }

        private static MeasurementSnapshot Snapshot(MeasurementTrace trace) =>
            new MeasurementSnapshot(new[] { trace });

        private static MeasurementTrace Trace(double value, string semanticIdentity = "element-1")
        {
            return new MeasurementTrace(
                semanticIdentity,
                "source-1",
                "NetVolumeM3",
                Array.Empty<MeasurementTraceFact>(),
                value,
                Array.Empty<MeasurementTraceAdjustment>(),
                value,
                "m3",
                "none",
                ruleId: "measure-rule",
                ruleVersion: "v1");
        }

        private static RateBook RateBook(string rateBookId, string rateItemId, decimal unitRate, string version)
        {
            return new RateBook(rateBookId, new[]
            {
                new RateItem(
                    rateItemId,
                    new CostCode("C001"),
                    "m3",
                    "USD",
                    unitRate,
                    EffectiveUtc,
                    version)
            });
        }

        private static void Findings(EstimateLineFreshnessResult result, params EstimateLineFreshnessFindingKind[] expected)
        {
            Equal(expected.Length, result.Findings.Count);
            for (var i = 0; i < expected.Length; i++) Equal(expected[i], result.Findings[i]);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected true.");
        }

        private static void False(bool value)
        {
            if (value) throw new InvalidOperationException("Expected false.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
