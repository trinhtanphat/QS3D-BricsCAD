using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimateLineFreshnessBoundarySweepSmoke
    {
        private static readonly DateTime T0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Run()
        {
            NullArgumentsFailClosed();
            ExactCurrentEvidenceHasNoFindings();
            MeasurementMissingAndChangedRemainDistinct();
            RateBookIdentityChangeIsIndependent();
            RateUnavailableBoundariesAreReported();
            RateChangesAreDetectedByEvidenceField();
            CombinedFindingsAreDeterministicAndImmutable();
        }

        private static void NullArgumentsFailClosed()
        {
            var frozen = Frozen();
            Throws<ArgumentNullException>(
                () => EstimateLineFreshnessEvaluator.Evaluate(null!, frozen.Snapshot, frozen.Book),
                "null line");
            Throws<ArgumentNullException>(
                () => EstimateLineFreshnessEvaluator.Evaluate(frozen.Line, null!, frozen.Book),
                "null measurement snapshot");
            Throws<ArgumentNullException>(
                () => EstimateLineFreshnessEvaluator.Evaluate(frozen.Line, frozen.Snapshot, null!),
                "null rate book");
        }

        private static void ExactCurrentEvidenceHasNoFindings()
        {
            var frozen = Frozen();
            var result = EstimateLineFreshnessEvaluator.Evaluate(
                frozen.Line,
                frozen.Snapshot,
                frozen.Book);

            True(result.IsCurrent, "exact frozen evidence must remain current");
            Equal(0, result.Findings.Count, "exact frozen evidence finding count");
            Same(frozen.Line, result.Line, "result preserves frozen line identity");
            Same(frozen.Trace, result.CurrentMeasurementTrace!, "exact current trace reference");
            Same(frozen.Rate, result.CurrentRateItem!, "exact current rate reference");
        }

        private static void MeasurementMissingAndChangedRemainDistinct()
        {
            var frozen = Frozen();

            var missing = EstimateLineFreshnessEvaluator.Evaluate(
                frozen.Line,
                Snapshot(Trace("sem-1", "SRC-1", "QTY-1", 2d)),
                frozen.Book);
            Findings(missing, EstimateLineFreshnessFindingKind.MeasurementMissing);
            True(missing.CurrentMeasurementTrace == null,
                "case-changed measurement identity must remain missing under exact identity semantics");
            Same(frozen.Rate, missing.CurrentRateItem!,
                "measurement miss must not disturb independent matching rate evidence");

            var changedTrace = Trace("SEM-1", "SRC-1", "QTY-1", 3d);
            var changed = EstimateLineFreshnessEvaluator.Evaluate(
                frozen.Line,
                Snapshot(changedTrace),
                frozen.Book);
            Findings(changed, EstimateLineFreshnessFindingKind.MeasurementChanged);
            Same(changedTrace, changed.CurrentMeasurementTrace!,
                "changed measurement result must expose the current same-identity trace");
        }

        private static void RateBookIdentityChangeIsIndependent()
        {
            var frozen = Frozen();
            var replacementRate = Rate("RATE-1", 12.5m, T0, "v1");
            var replacementBook = new RateBook("BOOK-2", new[] { replacementRate });

            var result = EstimateLineFreshnessEvaluator.Evaluate(
                frozen.Line,
                frozen.Snapshot,
                replacementBook);

            Findings(result, EstimateLineFreshnessFindingKind.RateBookChanged);
            Same(replacementRate, result.CurrentRateItem!,
                "rate-book identity change must still expose matching current rate evidence");
        }

        private static void RateUnavailableBoundariesAreReported()
        {
            var frozen = Frozen();

            var futureRate = Rate("RATE-FUTURE", 12.5m, T0.AddDays(2), "v2");
            var futureBook = new RateBook("BOOK-1", new[] { futureRate });
            var beforeEffective = EstimateLineFreshnessEvaluator.Evaluate(
                frozen.Line,
                frozen.Snapshot,
                futureBook);
            Findings(beforeEffective, EstimateLineFreshnessFindingKind.RateUnavailable);
            True(beforeEffective.CurrentRateItem == null,
                "rate before first current effective timestamp must expose no current item");

            var otherScope = new RateItem(
                "RATE-OTHER",
                new CostCode("OTHER"),
                "m3",
                "USD",
                12.5m,
                T0,
                "v1");
            var otherBook = new RateBook("BOOK-1", new[] { otherScope });
            var missingScope = EstimateLineFreshnessEvaluator.Evaluate(
                frozen.Line,
                frozen.Snapshot,
                otherBook);
            Findings(missingScope, EstimateLineFreshnessFindingKind.RateUnavailable);
            True(missingScope.CurrentRateItem == null,
                "missing current rate scope must expose no current item");
        }

        private static void RateChangesAreDetectedByEvidenceField()
        {
            AssertRateChanged(Rate("RATE-2", 12.5m, T0, "v1"), "rate item id");
            AssertRateChanged(Rate("RATE-1", 13m, T0, "v1"), "unit rate");
            AssertRateChanged(Rate("RATE-1", 12.5m, T0.AddHours(12), "v1"), "effective timestamp");
            AssertRateChanged(Rate("RATE-1", 12.5m, T0, "v2"), "version");

            var frozen = Frozen();
            var caseOnlyId = Rate("rate-1", 12.5m, T0, "v1");
            var caseOnlyResult = EstimateLineFreshnessEvaluator.Evaluate(
                frozen.Line,
                frozen.Snapshot,
                new RateBook("BOOK-1", new[] { caseOnlyId }));
            True(caseOnlyResult.IsCurrent,
                "rate item id casing alone must follow RateBook case-insensitive identity semantics");
            Same(caseOnlyId, caseOnlyResult.CurrentRateItem!,
                "case-only rate id control must expose current resolved item");
        }

        private static void CombinedFindingsAreDeterministicAndImmutable()
        {
            var frozen = Frozen();
            var changedTrace = Trace("SEM-1", "SRC-1", "QTY-1", 4d);
            var unmatchedBook = new RateBook(
                "BOOK-2",
                new[]
                {
                    new RateItem(
                        "RATE-OTHER",
                        new CostCode("OTHER"),
                        "m3",
                        "USD",
                        9m,
                        T0,
                        "v9")
                });

            var result = EstimateLineFreshnessEvaluator.Evaluate(
                frozen.Line,
                Snapshot(changedTrace),
                unmatchedBook);

            Findings(
                result,
                EstimateLineFreshnessFindingKind.MeasurementChanged,
                EstimateLineFreshnessFindingKind.RateBookChanged,
                EstimateLineFreshnessFindingKind.RateUnavailable);
            True(!result.IsCurrent, "combined stale evidence must not be current");
            Same(changedTrace, result.CurrentMeasurementTrace!,
                "combined result must retain changed current trace evidence");
            True(result.CurrentRateItem == null,
                "combined unmatched rate evidence must remain absent");

            var collection = result.Findings as ICollection<EstimateLineFreshnessFindingKind>;
            True(collection != null && collection.IsReadOnly,
                "findings must expose an immutable snapshot collection");
            Throws<NotSupportedException>(
                () => collection!.Add(EstimateLineFreshnessFindingKind.RateChanged),
                "findings mutation");
            Equal(3, result.Findings.Count,
                "failed caller mutation must not alter findings snapshot");
        }

        private static void AssertRateChanged(RateItem currentRate, string field)
        {
            var frozen = Frozen();
            var result = EstimateLineFreshnessEvaluator.Evaluate(
                frozen.Line,
                frozen.Snapshot,
                new RateBook("BOOK-1", new[] { currentRate }));

            Findings(result, EstimateLineFreshnessFindingKind.RateChanged);
            Same(currentRate, result.CurrentRateItem!,
                field + " change must expose current resolved rate item");
        }

        private static FrozenContext Frozen()
        {
            var trace = Trace("SEM-1", "SRC-1", "QTY-1", 2d);
            var snapshot = Snapshot(trace);
            var rate = Rate("RATE-1", 12.5m, T0, "v1");
            var book = new RateBook("BOOK-1", new[] { rate });
            var line = EstimateLine.Create(
                "LINE-1",
                snapshot,
                "SEM-1",
                "SRC-1",
                "QTY-1",
                book,
                new CostCode("COST-1"),
                "USD",
                T0.AddDays(1));
            return new FrozenContext(line, snapshot, trace, book, rate);
        }

        private static MeasurementSnapshot Snapshot(MeasurementTrace trace) =>
            new MeasurementSnapshot(new[] { trace });

        private static MeasurementTrace Trace(
            string semanticIdentity,
            string sourceIdentity,
            string quantityKey,
            double value)
        {
            return new MeasurementTrace(
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                Array.Empty<MeasurementTraceFact>(),
                value,
                Array.Empty<MeasurementTraceAdjustment>(),
                value,
                "m3",
                "none");
        }

        private static RateItem Rate(
            string id,
            decimal unitRate,
            DateTime effectiveFromUtc,
            string version)
        {
            return new RateItem(
                id,
                new CostCode("COST-1"),
                "m3",
                "USD",
                unitRate,
                effectiveFromUtc,
                version);
        }

        private static void Findings(
            EstimateLineFreshnessResult result,
            params EstimateLineFreshnessFindingKind[] expected)
        {
            Equal(expected.Length, result.Findings.Count, "finding count");
            for (var i = 0; i < expected.Length; i++)
            {
                if (result.Findings[i] != expected[i])
                {
                    throw new InvalidOperationException(
                        "EstimateLineFreshness regression: finding order mismatch at index " + i +
                        ". Expected=" + expected[i] + ", actual=" + result.Findings[i] + ".");
                }
            }
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException(
                "EstimateLineFreshness regression: expected " + typeof(T).Name + " for " + message + ".");
        }

        private static void Same(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual))
                throw new InvalidOperationException("EstimateLineFreshness regression: " + message + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("EstimateLineFreshness regression: " + message + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
            {
                throw new InvalidOperationException(
                    "EstimateLineFreshness regression: " + message +
                    ". Expected=" + expected + ", actual=" + actual + ".");
            }
        }

        private sealed class FrozenContext
        {
            internal FrozenContext(
                EstimateLine line,
                MeasurementSnapshot snapshot,
                MeasurementTrace trace,
                RateBook book,
                RateItem rate)
            {
                Line = line;
                Snapshot = snapshot;
                Trace = trace;
                Book = book;
                Rate = rate;
            }

            internal EstimateLine Line { get; }
            internal MeasurementSnapshot Snapshot { get; }
            internal MeasurementTrace Trace { get; }
            internal RateBook Book { get; }
            internal RateItem Rate { get; }
        }
    }
}
