using System;
using System.Collections.Generic;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementSnapshotDeltaReasonSmoke
    {
        internal static void Run()
        {
            LifecycleReasonsAreExplicit();
            RuleVersionChangeIsVisible();
            AdjustmentRuleProvenanceIsVisible();
            AdjustmentRuleAssociationIsVisible();
            CanonicalEvidenceDimensionsAreVisible();
            MultipleReasonsUseStableOrder();
            NumericOnlyChangeIsUnresolved();
            InvalidInputFailsClosed();
        }

        private static void LifecycleReasonsAreExplicit()
        {
            var trace = Trace("SEM-A", 1d);
            AssertReasons(
                Line(new MeasurementSnapshot(Array.Empty<MeasurementTrace>()), new MeasurementSnapshot(new[] { trace })),
                MeasurementSnapshotDeltaReasonKind.Added);
            AssertReasons(
                Line(new MeasurementSnapshot(new[] { trace }), new MeasurementSnapshot(Array.Empty<MeasurementTrace>())),
                MeasurementSnapshotDeltaReasonKind.Removed);
            AssertReasons(
                Line(new MeasurementSnapshot(new[] { trace }), new MeasurementSnapshot(new[] { trace })),
                MeasurementSnapshotDeltaReasonKind.Unchanged);
        }

        private static void RuleVersionChangeIsVisible()
        {
            var before = Trace("SEM-A", 1d, ruleId: "wall-area", ruleVersion: "1");
            var after = Trace("SEM-A", 1d, ruleId: "wall-area", ruleVersion: "2");
            AssertReasons(ChangedLine(before, after), MeasurementSnapshotDeltaReasonKind.RuleProvenanceChanged);
        }

        private static void AdjustmentRuleProvenanceIsVisible()
        {
            var before = Trace(
                "SEM-A",
                9d,
                adjustments: new[] { Adjustment(1d, "opening", "OPENING-1", "opening-deduction", "1") });
            var after = Trace(
                "SEM-A",
                9d,
                adjustments: new[] { Adjustment(1d, "opening", "OPENING-1", "opening-deduction", "2") });

            AssertReasons(
                ChangedLine(before, after),
                MeasurementSnapshotDeltaReasonKind.RuleProvenanceChanged,
                MeasurementSnapshotDeltaReasonKind.AdjustmentsChanged);
        }

        private static void AdjustmentRuleAssociationIsVisible()
        {
            var before = Trace(
                "SEM-A",
                7d,
                adjustments: new[]
                {
                    Adjustment(1d, "opening-a", "OPENING-A", "rule-a", "1"),
                    Adjustment(2d, "opening-b", "OPENING-B", "rule-b", "1")
                });
            var after = Trace(
                "SEM-A",
                7d,
                adjustments: new[]
                {
                    Adjustment(1d, "opening-a", "OPENING-A", "rule-b", "1"),
                    Adjustment(2d, "opening-b", "OPENING-B", "rule-a", "1")
                });

            AssertReasons(
                ChangedLine(before, after),
                MeasurementSnapshotDeltaReasonKind.RuleProvenanceChanged,
                MeasurementSnapshotDeltaReasonKind.AdjustmentsChanged);

            var amountBefore = Trace(
                "SEM-A",
                9d,
                adjustments: new[] { Adjustment(1d, "opening", "OPENING-1", "opening-deduction", "1") });
            var amountAfter = Trace(
                "SEM-A",
                9d,
                adjustments: new[] { Adjustment(2d, "opening", "OPENING-1", "opening-deduction", "1") });
            AssertReasons(
                ChangedLine(amountBefore, amountAfter),
                MeasurementSnapshotDeltaReasonKind.AdjustmentsChanged);
        }

        private static void CanonicalEvidenceDimensionsAreVisible()
        {
            var factBefore = Trace("SEM-A", 10d, facts: new[] { new MeasurementTraceFact("Width", 1d, "m", "SRC-A") });
            var factAfter = Trace("SEM-A", 10d, facts: new[] { new MeasurementTraceFact("Width", 2d, "m", "SRC-A") });
            AssertReasons(ChangedLine(factBefore, factAfter), MeasurementSnapshotDeltaReasonKind.InputFactsChanged);

            var adjustmentBefore = Trace("SEM-A", 9d, adjustments: new[] { Adjustment(1d, "opening", "OPENING-1") });
            var adjustmentAfter = Trace("SEM-A", 9d, adjustments: new[] { Adjustment(2d, "opening", "OPENING-1") });
            AssertReasons(ChangedLine(adjustmentBefore, adjustmentAfter), MeasurementSnapshotDeltaReasonKind.AdjustmentsChanged);

            var annotationBefore = Trace("SEM-A", 10d, roundingPolicy: "none", warnings: new[] { "old warning" });
            var annotationAfter = Trace("SEM-A", 10d, roundingPolicy: "bankers-2dp", assumptions: new[] { "new assumption" });
            AssertReasons(
                ChangedLine(annotationBefore, annotationAfter),
                MeasurementSnapshotDeltaReasonKind.RoundingPolicyChanged,
                MeasurementSnapshotDeltaReasonKind.AnnotationsChanged);
        }

        private static void MultipleReasonsUseStableOrder()
        {
            var before = Trace(
                "SEM-A",
                9d,
                facts: new[] { new MeasurementTraceFact("Width", 1d, "m", "SRC-A") },
                adjustments: new[] { Adjustment(1d, "opening", "OPENING-1", "opening-deduction", "1") },
                roundingPolicy: "none",
                warnings: new[] { "old warning" },
                ruleId: "wall-area",
                ruleVersion: "1");
            var after = Trace(
                "SEM-A",
                9d,
                facts: new[] { new MeasurementTraceFact("Width", 2d, "m", "SRC-A") },
                adjustments: new[] { Adjustment(2d, "opening", "OPENING-1", "opening-deduction", "2") },
                roundingPolicy: "bankers-2dp",
                assumptions: new[] { "new assumption" },
                ruleId: "wall-area",
                ruleVersion: "2");

            AssertReasons(
                ChangedLine(before, after),
                MeasurementSnapshotDeltaReasonKind.RuleProvenanceChanged,
                MeasurementSnapshotDeltaReasonKind.InputFactsChanged,
                MeasurementSnapshotDeltaReasonKind.AdjustmentsChanged,
                MeasurementSnapshotDeltaReasonKind.RoundingPolicyChanged,
                MeasurementSnapshotDeltaReasonKind.AnnotationsChanged);
        }

        private static void NumericOnlyChangeIsUnresolved()
        {
            var before = Trace("SEM-A", 1d, grossValue: 1d);
            var after = Trace("SEM-A", 2d, grossValue: 2d);
            AssertReasons(ChangedLine(before, after), MeasurementSnapshotDeltaReasonKind.Unresolved);
        }

        private static void InvalidInputFailsClosed()
        {
            Throws<ArgumentNullException>(() => MeasurementSnapshotDeltaReasonClassifier.Classify(null!));
        }

        private static MeasurementSnapshotDeltaLine ChangedLine(MeasurementTrace before, MeasurementTrace after)
        {
            var line = Line(new MeasurementSnapshot(new[] { before }), new MeasurementSnapshot(new[] { after }));
            Equal(MeasurementSnapshotChangeKind.Changed, line.ChangeKind, "Test fixture must produce a changed line.");
            return line;
        }

        private static MeasurementSnapshotDeltaLine Line(MeasurementSnapshot before, MeasurementSnapshot after)
        {
            var delta = new MeasurementSnapshotDelta(before, after);
            Equal(1, delta.Lines.Count, "Test fixture must produce exactly one delta line.");
            return delta.Lines[0];
        }

        private static MeasurementTrace Trace(
            string semanticIdentity,
            double netValue,
            IEnumerable<MeasurementTraceFact>? facts = null,
            IEnumerable<MeasurementTraceAdjustment>? adjustments = null,
            string roundingPolicy = "none",
            IEnumerable<string>? warnings = null,
            IEnumerable<string>? assumptions = null,
            string? ruleId = null,
            string? ruleVersion = null,
            double? grossValue = null)
        {
            var adjustmentItems = adjustments == null
                ? new List<MeasurementTraceAdjustment>()
                : new List<MeasurementTraceAdjustment>(adjustments);
            var resolvedGrossValue = grossValue ?? netValue;
            if (!grossValue.HasValue && string.Equals(roundingPolicy, "none", StringComparison.Ordinal))
            {
                for (var i = 0; i < adjustmentItems.Count; i++)
                {
                    var adjustment = adjustmentItems[i];
                    resolvedGrossValue = adjustment.Kind == MeasurementTraceAdjustmentKind.Deduction
                        ? resolvedGrossValue + adjustment.Amount
                        : resolvedGrossValue - adjustment.Amount;
                }
            }

            return new MeasurementTrace(
                semanticIdentity,
                "SRC-A",
                "NetAreaM2",
                facts ?? Array.Empty<MeasurementTraceFact>(),
                resolvedGrossValue,
                adjustmentItems,
                netValue,
                "m2",
                roundingPolicy,
                warnings,
                assumptions,
                ruleId,
                ruleVersion);
        }

        private static MeasurementTraceAdjustment Adjustment(
            double amount,
            string reason,
            string sourceIdentity,
            string? ruleId = null,
            string? ruleVersion = null)
        {
            return new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Deduction,
                amount,
                "m2",
                reason,
                sourceIdentity,
                ruleId,
                ruleVersion);
        }

        private static void AssertReasons(
            MeasurementSnapshotDeltaLine line,
            params MeasurementSnapshotDeltaReasonKind[] expected)
        {
            var actual = MeasurementSnapshotDeltaReasonClassifier.Classify(line);
            Equal(expected.Length, actual.Count, "Unexpected number of delta reason evidence items.");
            for (var i = 0; i < expected.Length; i++)
                Equal(expected[i], actual[i], "Delta reason evidence must use deterministic enum order.");
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
