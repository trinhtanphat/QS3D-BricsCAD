using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using QS3D.Core.Measurement;

namespace QS3D.Core.Cost
{
    public enum EstimateLineFreshnessFindingKind
    {
        MeasurementMissing = 0,
        MeasurementChanged = 1,
        RateBookChanged = 2,
        RateUnavailable = 3,
        RateChanged = 4
    }

    /// <summary>
    /// Read-only freshness projection for one frozen estimate line. This result reports
    /// changed/missing canonical inputs only; it never recalculates quantity or cost.
    /// </summary>
    public sealed class EstimateLineFreshnessResult
    {
        internal EstimateLineFreshnessResult(
            EstimateLine line,
            MeasurementTrace? currentMeasurementTrace,
            RateItem? currentRateItem,
            IEnumerable<EstimateLineFreshnessFindingKind> findings)
        {
            Line = line ?? throw new ArgumentNullException(nameof(line));
            CurrentMeasurementTrace = currentMeasurementTrace;
            CurrentRateItem = currentRateItem;
            Findings = new ReadOnlyCollection<EstimateLineFreshnessFindingKind>(
                new List<EstimateLineFreshnessFindingKind>(findings ?? throw new ArgumentNullException(nameof(findings))).ToArray());
        }

        public EstimateLine Line { get; }
        public MeasurementTrace? CurrentMeasurementTrace { get; }
        public RateItem? CurrentRateItem { get; }
        public IReadOnlyList<EstimateLineFreshnessFindingKind> Findings { get; }
        public bool IsCurrent => Findings.Count == 0;
    }

    /// <summary>
    /// Evaluates whether an existing frozen estimate line still references the same
    /// canonical measurement and rate evidence. No business quantity/rate formulas are
    /// duplicated here: MeasurementTrace equality and RateBook resolution remain the
    /// authoritative input contracts.
    /// </summary>
    public static class EstimateLineFreshnessEvaluator
    {
        public static EstimateLineFreshnessResult Evaluate(
            EstimateLine line,
            MeasurementSnapshot currentMeasurementSnapshot,
            RateBook currentRateBook)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));
            if (currentMeasurementSnapshot == null) throw new ArgumentNullException(nameof(currentMeasurementSnapshot));
            if (currentRateBook == null) throw new ArgumentNullException(nameof(currentRateBook));

            var findings = new List<EstimateLineFreshnessFindingKind>();
            var currentTrace = FindMeasurementTrace(line.MeasurementTrace, currentMeasurementSnapshot);
            if (currentTrace == null)
            {
                findings.Add(EstimateLineFreshnessFindingKind.MeasurementMissing);
            }
            else if (!line.MeasurementTrace.Equals(currentTrace))
            {
                findings.Add(EstimateLineFreshnessFindingKind.MeasurementChanged);
            }

            if (!string.Equals(line.RateBook.RateBookId, currentRateBook.RateBookId, StringComparison.Ordinal))
                findings.Add(EstimateLineFreshnessFindingKind.RateBookChanged);

            var rateResolution = currentRateBook.Resolve(
                line.CostCode,
                line.Unit,
                line.Currency,
                line.RateAsOfUtc);

            RateItem? currentRateItem = null;
            if (!rateResolution.IsMatched || rateResolution.Item == null)
            {
                findings.Add(EstimateLineFreshnessFindingKind.RateUnavailable);
            }
            else
            {
                currentRateItem = rateResolution.Item;
                if (!SameRateItem(line.RateItem, currentRateItem))
                    findings.Add(EstimateLineFreshnessFindingKind.RateChanged);
            }

            return new EstimateLineFreshnessResult(line, currentTrace, currentRateItem, findings);
        }

        private static MeasurementTrace? FindMeasurementTrace(
            MeasurementTrace frozenTrace,
            MeasurementSnapshot currentSnapshot)
        {
            for (var i = 0; i < currentSnapshot.Traces.Count; i++)
            {
                var candidate = currentSnapshot.Traces[i];
                if (string.Equals(candidate.SemanticIdentity, frozenTrace.SemanticIdentity, StringComparison.Ordinal) &&
                    string.Equals(candidate.SourceIdentity, frozenTrace.SourceIdentity, StringComparison.Ordinal) &&
                    string.Equals(candidate.QuantityKey, frozenTrace.QuantityKey, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }

        private static bool SameRateItem(RateItem frozen, RateItem current)
        {
            return string.Equals(frozen.RateItemId, current.RateItemId, StringComparison.OrdinalIgnoreCase) &&
                   frozen.CostCode.Equals(current.CostCode) &&
                   string.Equals(frozen.Unit, current.Unit, StringComparison.Ordinal) &&
                   string.Equals(frozen.Currency, current.Currency, StringComparison.Ordinal) &&
                   frozen.UnitRate == current.UnitRate &&
                   frozen.EffectiveFromUtc == current.EffectiveFromUtc &&
                   string.Equals(frozen.Version, current.Version, StringComparison.Ordinal);
        }
    }
}
