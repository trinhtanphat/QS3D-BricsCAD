using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Measurement
{
    public enum MeasurementSnapshotDeltaReasonKind
    {
        Added = 0,
        Removed = 1,
        Unchanged = 2,
        RuleProvenanceChanged = 3,
        InputFactsChanged = 4,
        AdjustmentsChanged = 5,
        RoundingPolicyChanged = 6,
        AnnotationsChanged = 7,
        Unresolved = 8
    }

    /// <summary>
    /// Classifies only canonical evidence visible in an existing measurement delta line.
    /// It intentionally does not infer geometry, property, mapping, or other causes that
    /// are not represented explicitly in MeasurementTrace.
    /// </summary>
    public static class MeasurementSnapshotDeltaReasonClassifier
    {
        public static IReadOnlyList<MeasurementSnapshotDeltaReasonKind> Classify(MeasurementSnapshotDeltaLine line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            switch (line.ChangeKind)
            {
                case MeasurementSnapshotChangeKind.Added:
                    return Single(MeasurementSnapshotDeltaReasonKind.Added);
                case MeasurementSnapshotChangeKind.Removed:
                    return Single(MeasurementSnapshotDeltaReasonKind.Removed);
                case MeasurementSnapshotChangeKind.Unchanged:
                    return Single(MeasurementSnapshotDeltaReasonKind.Unchanged);
                case MeasurementSnapshotChangeKind.Changed:
                    return ClassifyChanged(line);
                default:
                    throw new ArgumentOutOfRangeException(nameof(line), "Unknown measurement snapshot change kind.");
            }
        }

        private static IReadOnlyList<MeasurementSnapshotDeltaReasonKind> ClassifyChanged(MeasurementSnapshotDeltaLine line)
        {
            var previous = line.PreviousTrace;
            var current = line.CurrentTrace;
            if (previous == null || current == null)
                throw new InvalidOperationException("Changed measurement delta lines require both previous and current traces.");

            var reasons = new List<MeasurementSnapshotDeltaReasonKind>();

            if (TopLevelRuleChanged(previous, current) || AdjustmentRuleProvenanceChanged(previous, current))
                reasons.Add(MeasurementSnapshotDeltaReasonKind.RuleProvenanceChanged);

            if (!MeasurementTraceContract.SequenceEqual(previous.InputFacts, current.InputFacts))
                reasons.Add(MeasurementSnapshotDeltaReasonKind.InputFactsChanged);

            if (!MeasurementTraceContract.SequenceEqual(previous.Adjustments, current.Adjustments))
                reasons.Add(MeasurementSnapshotDeltaReasonKind.AdjustmentsChanged);

            if (!string.Equals(previous.RoundingPolicy, current.RoundingPolicy, StringComparison.Ordinal))
                reasons.Add(MeasurementSnapshotDeltaReasonKind.RoundingPolicyChanged);

            if (!MeasurementTraceContract.SequenceEqual(previous.Warnings, current.Warnings) ||
                !MeasurementTraceContract.SequenceEqual(previous.Assumptions, current.Assumptions))
                reasons.Add(MeasurementSnapshotDeltaReasonKind.AnnotationsChanged);

            if (reasons.Count == 0)
                reasons.Add(MeasurementSnapshotDeltaReasonKind.Unresolved);

            return new ReadOnlyCollection<MeasurementSnapshotDeltaReasonKind>(reasons.ToArray());
        }

        private static bool TopLevelRuleChanged(MeasurementTrace previous, MeasurementTrace current)
        {
            return !string.Equals(previous.RuleId, current.RuleId, StringComparison.Ordinal) ||
                   !string.Equals(previous.RuleVersion, current.RuleVersion, StringComparison.Ordinal);
        }

        private static bool AdjustmentRuleProvenanceChanged(MeasurementTrace previous, MeasurementTrace current)
        {
            var before = RuleProvenanceTokens(previous.Adjustments);
            var after = RuleProvenanceTokens(current.Adjustments);
            return !MeasurementTraceContract.SequenceEqual(before, after);
        }

        private static IReadOnlyList<string> RuleProvenanceTokens(IReadOnlyList<MeasurementTraceAdjustment> adjustments)
        {
            var tokens = new List<string>();
            for (var i = 0; i < adjustments.Count; i++)
            {
                var adjustment = adjustments[i];
                if (adjustment.RuleId == null) continue;
                tokens.Add(adjustment.RuleId + "\u001f" + adjustment.RuleVersion);
            }
            tokens.Sort(StringComparer.Ordinal);
            return tokens;
        }

        private static IReadOnlyList<MeasurementSnapshotDeltaReasonKind> Single(MeasurementSnapshotDeltaReasonKind reason)
        {
            return new ReadOnlyCollection<MeasurementSnapshotDeltaReasonKind>(new[] { reason });
        }
    }
}
