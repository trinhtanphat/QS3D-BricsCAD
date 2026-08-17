using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripQuantityEvidenceNullFailFastSmoke
    {
        private const int MaxCandidates = 10000;

        internal static void Run()
        {
            RejectsNullWithoutAdvancing();
            RejectsNullAfterValidCandidateWithoutAdvancingFurther();
            RejectsNullAtLastAllowedSlotBeforeSorting();
            PreservesCandidateBoundPrecedence();
            RejectsOversizedKnownCountBeforeEnumeration();
            PreservesOrdinaryGroupingAndDeduplication();
        }

        private static void RejectsNullWithoutAdvancing()
        {
            var advancedPastNull = false;
            var disposed = false;

            try
            {
                IfcRoundTripQuantityEvidenceSet.Create(Enumerate());
                throw new InvalidOperationException("Expected null IFC quantity evidence to be rejected.");
            }
            catch (ArgumentException ex)
            {
                AssertNullFailure(ex);
            }

            if (advancedPastNull)
                throw new InvalidOperationException("IFC quantity evidence enumeration advanced past a null candidate.");
            if (!disposed)
                throw new InvalidOperationException("IFC quantity evidence enumeration was not disposed after null rejection.");

            IEnumerable<IfcRoundTripQuantityEvidence> Enumerate()
            {
                try
                {
                    yield return null!;
                    advancedPastNull = true;
                    throw new InvalidOperationException("Enumeration advanced past null IFC quantity evidence.");
                }
                finally
                {
                    disposed = true;
                }
            }
        }

        private static void RejectsNullAfterValidCandidateWithoutAdvancingFurther()
        {
            var candidate = new IfcRoundTripQuantityEvidence("Q", 1d, "m", "SRC", "P1");
            var advancedPastNull = false;
            var disposed = false;

            try
            {
                IfcRoundTripQuantityEvidenceSet.Create(Enumerate());
                throw new InvalidOperationException("Expected a null IFC quantity evidence entry after a valid candidate to be rejected.");
            }
            catch (ArgumentException ex)
            {
                AssertNullFailure(ex);
            }

            if (advancedPastNull)
                throw new InvalidOperationException("IFC quantity evidence enumeration advanced after the first null entry.");
            if (!disposed)
                throw new InvalidOperationException("IFC quantity evidence enumeration was not disposed after a mid-stream null rejection.");

            IEnumerable<IfcRoundTripQuantityEvidence> Enumerate()
            {
                try
                {
                    yield return candidate;
                    yield return null!;
                    advancedPastNull = true;
                    throw new InvalidOperationException("Enumeration advanced after the first null entry.");
                }
                finally
                {
                    disposed = true;
                }
            }
        }

        private static void RejectsNullAtLastAllowedSlotBeforeSorting()
        {
            var candidate = new IfcRoundTripQuantityEvidence("Q", 1d, "m", "SRC", "P1");
            var observed = 0;
            var disposed = false;

            try
            {
                IfcRoundTripQuantityEvidenceSet.Create(Enumerate());
                throw new InvalidOperationException("Expected a null IFC quantity evidence entry at candidate 10000 to be rejected.");
            }
            catch (ArgumentException ex)
            {
                AssertNullFailure(ex);
            }

            if (observed != MaxCandidates)
                throw new InvalidOperationException("Null validation at the last allowed candidate must consume exactly 10000 entries.");
            if (!disposed)
                throw new InvalidOperationException("IFC quantity evidence enumeration was not disposed after the last-slot null rejection.");

            IEnumerable<IfcRoundTripQuantityEvidence> Enumerate()
            {
                try
                {
                    for (var index = 1; index < MaxCandidates; index++)
                    {
                        observed++;
                        yield return candidate;
                    }

                    observed++;
                    yield return null!;
                    throw new InvalidOperationException("Enumeration advanced after the last-slot null candidate.");
                }
                finally
                {
                    disposed = true;
                }
            }
        }

        private static void PreservesCandidateBoundPrecedence()
        {
            var candidate = new IfcRoundTripQuantityEvidence("Q", 1d, "m", "SRC", "P");
            var advancedPastBoundary = false;
            var disposed = false;
            InvalidOperationException? failure = null;

            try
            {
                IfcRoundTripQuantityEvidenceSet.Create(Enumerate());
            }
            catch (InvalidOperationException ex)
            {
                failure = ex;
            }

            if (failure == null)
                throw new InvalidOperationException("Expected the IFC quantity evidence candidate bound to reject item 10001.");
            if (failure.Message.IndexOf("supports at most 10000 candidates", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("The candidate bound must keep precedence over null validation at item 10001.", failure);
            if (advancedPastBoundary)
                throw new InvalidOperationException("IFC quantity evidence enumeration advanced beyond the first over-limit candidate.");
            if (!disposed)
                throw new InvalidOperationException("IFC quantity evidence enumeration was not disposed after the candidate bound failed.");

            IEnumerable<IfcRoundTripQuantityEvidence> Enumerate()
            {
                try
                {
                    for (var index = 0; index < MaxCandidates; index++)
                        yield return candidate;
                    yield return null!;
                    advancedPastBoundary = true;
                    throw new InvalidOperationException("Enumeration advanced beyond the first over-limit candidate.");
                }
                finally
                {
                    disposed = true;
                }
            }
        }

        private static void RejectsOversizedKnownCountBeforeEnumeration()
        {
            var source = new OversizedKnownCountCollection(MaxCandidates + 1);
            InvalidOperationException? failure = null;

            try
            {
                IfcRoundTripQuantityEvidenceSet.Create(source);
            }
            catch (InvalidOperationException ex)
            {
                failure = ex;
            }

            if (failure == null || failure.Message.IndexOf("supports at most 10000 candidates", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Known oversized IFC quantity evidence must fail with the candidate-bound diagnostic.", failure);
            if (source.EnumeratorRequested)
                throw new InvalidOperationException("Known oversized IFC quantity evidence must be rejected before requesting an enumerator.");
        }

        private static void PreservesOrdinaryGroupingAndDeduplication()
        {
            var first = new IfcRoundTripQuantityEvidence("Area", 10d, "m2", "SRC-A", "P1");
            var duplicate = new IfcRoundTripQuantityEvidence("Area", 10d, "m2", "SRC-A", "P1");
            var ambiguous = new IfcRoundTripQuantityEvidence("Area", 11d, "m2", "SRC-A", "P2");
            var other = new IfcRoundTripQuantityEvidence("Volume", 3d, "m3", "SRC-B", "P3");

            var result = IfcRoundTripQuantityEvidenceSet.Create(new[] { other, ambiguous, duplicate, first });

            if (result.CandidateCount != 3)
                throw new InvalidOperationException("IFC quantity evidence null hardening must preserve candidate deduplication.");
            if (result.Groups.Count != 2 || !result.HasAmbiguity)
                throw new InvalidOperationException("IFC quantity evidence null hardening must preserve deterministic grouping and ambiguity detection.");
            if (!string.Equals(result.Groups[0].QuantityKey, "Area", StringComparison.Ordinal)
                || result.Groups[0].Candidates.Count != 2
                || !string.Equals(result.Groups[1].QuantityKey, "Volume", StringComparison.Ordinal))
                throw new InvalidOperationException("IFC quantity evidence null hardening changed ordinary deterministic group ordering.");
        }

        private static void AssertNullFailure(ArgumentException ex)
        {
            if (!string.Equals(ex.ParamName, "evidence", StringComparison.Ordinal))
                throw new InvalidOperationException("Null IFC quantity evidence must identify the evidence parameter.", ex);
            if (ex.Message.IndexOf("cannot contain null entries", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Null IFC quantity evidence must preserve the validation message.", ex);
        }

        private sealed class OversizedKnownCountCollection : ICollection<IfcRoundTripQuantityEvidence>
        {
            internal OversizedKnownCountCollection(int count)
            {
                Count = count;
            }

            internal bool EnumeratorRequested { get; private set; }
            public int Count { get; }
            public bool IsReadOnly => true;

            public IEnumerator<IfcRoundTripQuantityEvidence> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new InvalidOperationException("Oversized known-count source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(IfcRoundTripQuantityEvidence item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(IfcRoundTripQuantityEvidence item) => false;
            public void CopyTo(IfcRoundTripQuantityEvidence[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(IfcRoundTripQuantityEvidence item) => throw new NotSupportedException();
        }
    }
}