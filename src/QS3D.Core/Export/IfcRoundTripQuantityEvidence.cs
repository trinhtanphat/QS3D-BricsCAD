using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Export
{
    public sealed class IfcRoundTripQuantityEvidence
    {
        public IfcRoundTripQuantityEvidence(
            string quantityKey,
            double value,
            string unit,
            string externalSourceIdentity,
            string provenanceIdentity)
        {
            QuantityKey = IfcRoundTripProjectionContract.RequireCanonicalToken(quantityKey, nameof(quantityKey));
            Value = IfcRoundTripProjectionContract.RequireFinite(value, nameof(value));
            Unit = IfcRoundTripProjectionContract.RequireCanonicalToken(unit, nameof(unit));
            ExternalSourceIdentity = IfcRoundTripProjectionContract.RequireCanonicalToken(externalSourceIdentity, nameof(externalSourceIdentity));
            ProvenanceIdentity = IfcRoundTripProjectionContract.RequireCanonicalToken(provenanceIdentity, nameof(provenanceIdentity));
        }

        public string QuantityKey { get; }
        public double Value { get; }
        public string Unit { get; }
        public string ExternalSourceIdentity { get; }
        public string ProvenanceIdentity { get; }
    }

    public sealed class IfcRoundTripQuantityEvidenceGroup
    {
        internal IfcRoundTripQuantityEvidenceGroup(
            string quantityKey,
            string externalSourceIdentity,
            IReadOnlyList<IfcRoundTripQuantityEvidence> candidates)
        {
            QuantityKey = quantityKey;
            ExternalSourceIdentity = externalSourceIdentity;
            Candidates = candidates;
        }

        public string QuantityKey { get; }
        public string ExternalSourceIdentity { get; }
        public IReadOnlyList<IfcRoundTripQuantityEvidence> Candidates { get; }
        public bool IsAmbiguous => Candidates.Count > 1;
    }

    public sealed class IfcRoundTripQuantityEvidenceSet
    {
        internal const int MaxCandidates = 10000;

        private IfcRoundTripQuantityEvidenceSet(IReadOnlyList<IfcRoundTripQuantityEvidenceGroup> groups)
        {
            Groups = groups;
        }

        public IReadOnlyList<IfcRoundTripQuantityEvidenceGroup> Groups { get; }
        public bool HasAmbiguity => Groups.Any(group => group.IsAmbiguous);
        public int CandidateCount => Groups.Sum(group => group.Candidates.Count);

        public static IfcRoundTripQuantityEvidenceSet Create(IEnumerable<IfcRoundTripQuantityEvidence> evidence)
        {
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));
            var knownCount = TryGetKnownCount(evidence, out var conflictingKnownCounts);
            if (knownCount.HasValue && knownCount.Value > MaxCandidates)
                ThrowTooManyCandidates();
            if (conflictingKnownCounts)
                throw new InvalidOperationException("IFC round-trip quantity evidence source exposes conflicting known Count values.");

            var candidates = new List<IfcRoundTripQuantityEvidence>();
            foreach (var candidate in evidence)
            {
                if (candidates.Count == MaxCandidates)
                    ThrowTooManyCandidates();
                if (candidate == null)
                    throw new ArgumentException("Quantity evidence collection cannot contain null entries.", nameof(evidence));
                candidates.Add(candidate);
            }

            candidates.Sort(IfcRoundTripQuantityEvidenceComparer.Instance);
            var groups = new List<IfcRoundTripQuantityEvidenceGroup>();

            var candidateIndex = 0;
            while (candidateIndex < candidates.Count)
            {
                var first = candidates[candidateIndex];
                var unique = new List<IfcRoundTripQuantityEvidence> { first };
                candidateIndex++;

                while (candidateIndex < candidates.Count && SameIdentity(first, candidates[candidateIndex]))
                {
                    var candidate = candidates[candidateIndex];
                    if (!SameCandidate(unique[unique.Count - 1], candidate))
                        unique.Add(candidate);
                    candidateIndex++;
                }

                groups.Add(new IfcRoundTripQuantityEvidenceGroup(
                    first.QuantityKey,
                    first.ExternalSourceIdentity,
                    Array.AsReadOnly(unique.ToArray())));
            }

            return new IfcRoundTripQuantityEvidenceSet(Array.AsReadOnly(groups.ToArray()));
        }

        private static int? TryGetKnownCount(
            IEnumerable<IfcRoundTripQuantityEvidence> evidence,
            out bool conflictingKnownCounts)
        {
            conflictingKnownCounts = false;
            int? knownCount = null;

            if (evidence is ICollection<IfcRoundTripQuantityEvidence> collection)
                knownCount = ObserveKnownCount(knownCount, collection.Count, ref conflictingKnownCounts);
            if (evidence is IReadOnlyCollection<IfcRoundTripQuantityEvidence> readOnlyCollection)
                knownCount = ObserveKnownCount(knownCount, readOnlyCollection.Count, ref conflictingKnownCounts);
            if (evidence is ICollection nonGenericCollection)
                knownCount = ObserveKnownCount(knownCount, nonGenericCollection.Count, ref conflictingKnownCounts);

            return knownCount;
        }

        private static int ObserveKnownCount(int? current, int observed, ref bool conflictingKnownCounts)
        {
            if (current.HasValue && current.Value != observed)
                conflictingKnownCounts = true;
            return !current.HasValue || observed > current.Value ? observed : current.Value;
        }

        private static void ThrowTooManyCandidates()
        {
            throw new InvalidOperationException(
                "IFC round-trip quantity evidence set supports at most " + MaxCandidates + " candidates.");
        }

        private static bool SameIdentity(IfcRoundTripQuantityEvidence left, IfcRoundTripQuantityEvidence right)
        {
            return string.Equals(left.QuantityKey, right.QuantityKey, StringComparison.Ordinal)
                && string.Equals(left.ExternalSourceIdentity, right.ExternalSourceIdentity, StringComparison.Ordinal);
        }

        private static bool SameCandidate(IfcRoundTripQuantityEvidence left, IfcRoundTripQuantityEvidence right)
        {
            return string.Equals(left.QuantityKey, right.QuantityKey, StringComparison.Ordinal)
                && left.Value.Equals(right.Value)
                && string.Equals(left.Unit, right.Unit, StringComparison.Ordinal)
                && string.Equals(left.ExternalSourceIdentity, right.ExternalSourceIdentity, StringComparison.Ordinal)
                && string.Equals(left.ProvenanceIdentity, right.ProvenanceIdentity, StringComparison.Ordinal);
        }
    }

    internal static class IfcRoundTripQuantityEvidenceSetComparer
    {
        internal static bool AreEquivalent(
            IfcRoundTripQuantityEvidenceSet expected,
            IfcRoundTripQuantityEvidenceSet actual,
            double absoluteTolerance)
        {
            if (expected.Groups.Count != actual.Groups.Count) return false;

            for (var groupIndex = 0; groupIndex < expected.Groups.Count; groupIndex++)
            {
                var leftGroup = expected.Groups[groupIndex];
                var rightGroup = actual.Groups[groupIndex];
                if (!string.Equals(leftGroup.QuantityKey, rightGroup.QuantityKey, StringComparison.Ordinal)) return false;
                if (!string.Equals(leftGroup.ExternalSourceIdentity, rightGroup.ExternalSourceIdentity, StringComparison.Ordinal)) return false;
                if (leftGroup.IsAmbiguous != rightGroup.IsAmbiguous) return false;
                if (leftGroup.Candidates.Count != rightGroup.Candidates.Count) return false;

                for (var candidateIndex = 0; candidateIndex < leftGroup.Candidates.Count; candidateIndex++)
                {
                    var left = leftGroup.Candidates[candidateIndex];
                    var right = rightGroup.Candidates[candidateIndex];
                    if (!string.Equals(left.QuantityKey, right.QuantityKey, StringComparison.Ordinal)) return false;
                    if (!string.Equals(left.Unit, right.Unit, StringComparison.Ordinal)) return false;
                    if (!string.Equals(left.ExternalSourceIdentity, right.ExternalSourceIdentity, StringComparison.Ordinal)) return false;
                    if (!string.Equals(left.ProvenanceIdentity, right.ProvenanceIdentity, StringComparison.Ordinal)) return false;
                    if (!WithinTolerance(left.Value, right.Value, absoluteTolerance)) return false;
                }
            }

            return true;
        }

        private static bool WithinTolerance(double left, double right, double absoluteTolerance)
        {
            if (left.Equals(right)) return true;
            return Math.Abs(left - right) <= absoluteTolerance;
        }
    }

    internal sealed class IfcRoundTripQuantityEvidenceComparer : IComparer<IfcRoundTripQuantityEvidence>
    {
        internal static readonly IfcRoundTripQuantityEvidenceComparer Instance = new IfcRoundTripQuantityEvidenceComparer();

        private IfcRoundTripQuantityEvidenceComparer()
        {
        }

        public int Compare(IfcRoundTripQuantityEvidence? x, IfcRoundTripQuantityEvidence? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var byKey = StringComparer.Ordinal.Compare(x.QuantityKey, y.QuantityKey);
            if (byKey != 0) return byKey;
            var bySource = StringComparer.Ordinal.Compare(x.ExternalSourceIdentity, y.ExternalSourceIdentity);
            if (bySource != 0) return bySource;
            var byUnit = StringComparer.Ordinal.Compare(x.Unit, y.Unit);
            if (byUnit != 0) return byUnit;
            var byValue = x.Value.CompareTo(y.Value);
            if (byValue != 0) return byValue;
            return StringComparer.Ordinal.Compare(x.ProvenanceIdentity, y.ProvenanceIdentity);
        }
    }
}
