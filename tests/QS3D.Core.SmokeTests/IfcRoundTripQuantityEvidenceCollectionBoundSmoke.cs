using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripQuantityEvidenceCollectionBoundSmoke
    {
        private const int MaximumCandidates = 10000;

        internal static void Run()
        {
            CountedOversizeFailsBeforeEnumeration();
            StreamingOversizeStopsAtFirstDisallowedCandidate();
            ExactBoundaryRemainsAcceptedWithExistingGroupingSemantics();
        }

        private static void CountedOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated(MaximumCandidates + 1);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted IFC quantity-evidence input must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted oversize failure must report the IFC quantity-evidence bound.");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedCandidate()
        {
            var source = new StreamingEvidence(MaximumCandidates + 2);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Equal(
                MaximumCandidates + 1,
                source.YieldedCount,
                "Streaming IFC quantity-evidence ingestion must stop immediately after observing candidate 10,001.");
            Contains("at most 10000", error.Message, "Streaming oversize failure must report the IFC quantity-evidence bound.");
        }

        private static void ExactBoundaryRemainsAcceptedWithExistingGroupingSemantics()
        {
            var evidence = new IfcRoundTripQuantityEvidence[MaximumCandidates];
            for (var index = 0; index < evidence.Length; index++)
                evidence[index] = Candidate(index);

            var set = IfcRoundTripQuantityEvidenceSet.Create(evidence);

            Equal(MaximumCandidates, set.CandidateCount, "IFC quantity-evidence set must accept exactly 10,000 valid candidates.");
            Equal(1, set.Groups.Count, "Boundary-sized evidence changed existing identity grouping semantics.");
            Require(set.Groups[0].IsAmbiguous, "Distinct boundary candidates sharing one evidence identity must remain ambiguous.");
            Equal(
                "source:00000",
                set.Groups[0].Candidates[0].ProvenanceIdentity,
                "Boundary-sized evidence lost canonical first-candidate ordering.");
            Equal(
                "source:09999",
                set.Groups[0].Candidates[set.Groups[0].Candidates.Count - 1].ProvenanceIdentity,
                "Boundary-sized evidence lost canonical final-candidate ordering.");
        }

        private static IfcRoundTripQuantityEvidence Candidate(int index)
        {
            var suffix = index.ToString("D5", CultureInfo.InvariantCulture);
            return new IfcRoundTripQuantityEvidence(
                "NetVolume",
                1d,
                "m3",
                "ifc-qto-volume",
                "source:" + suffix);
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class CountedNeverEnumerated : IReadOnlyCollection<IfcRoundTripQuantityEvidence>
        {
            internal CountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<IfcRoundTripQuantityEvidence> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingEvidence : IEnumerable<IfcRoundTripQuantityEvidence>
        {
            private readonly int _count;

            internal StreamingEvidence(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<IfcRoundTripQuantityEvidence> GetEnumerator()
            {
                for (var index = 0; index < _count; index++)
                {
                    YieldedCount++;
                    yield return Candidate(index);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class IfcRoundTripQuantityEvidenceCollectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            IfcRoundTripQuantityEvidenceCollectionBoundSmoke.Run();
        }
    }
}
