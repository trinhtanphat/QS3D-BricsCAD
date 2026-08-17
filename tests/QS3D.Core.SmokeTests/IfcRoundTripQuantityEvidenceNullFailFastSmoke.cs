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
            IfcRoundTripExchangeResultKnownCountSmoke.Run();
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

    internal static class IfcRoundTripExchangeResultKnownCountSmoke
    {
        private const int MaxResults = IfcRoundTripExchangeResultSet.MaxResultsPerCollection;

        internal static void Run()
        {
            RejectsNegativeGenericKnownCountBeforeEnumeration();
            RejectsConflictingKnownCountsBeforeEnumeration();
            RejectsOversizedNonGenericKnownCountBeforeEnumeration();
            AcceptsConsistentKnownCountsAndPreservesCanonicalOrdering();
            PreservesNullRejection();
            PreservesExactBoundAndDuplicateCollapse();
            PreservesStreamingBound();
        }

        private static void RejectsNegativeGenericKnownCountBeforeEnumeration()
        {
            var source = new NegativeKnownCountCollection();
            var failure = CaptureInvalidOperation(() => IfcRoundTripExchangeResultSet.Create(source));

            if (failure.Message.IndexOf("invalid negative known Count", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Negative IFC exchange-result Count must fail with the malformed-count diagnostic.", failure);
            if (source.EnumeratorRequested)
                throw new InvalidOperationException("Negative IFC exchange-result Count must be rejected before requesting an enumerator.");
        }

        private static void RejectsConflictingKnownCountsBeforeEnumeration()
        {
            var source = new ConflictingKnownCountCollection();
            var failure = CaptureInvalidOperation(() => IfcRoundTripExchangeResultSet.Create(source));

            if (failure.Message.IndexOf("conflicting known Count", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Conflicting IFC exchange-result Counts must fail with the malformed-count diagnostic.", failure);
            if (source.EnumeratorRequested)
                throw new InvalidOperationException("Conflicting IFC exchange-result Counts must be rejected before requesting an enumerator.");
        }

        private static void RejectsOversizedNonGenericKnownCountBeforeEnumeration()
        {
            var source = new OversizedNonGenericKnownCountCollection();
            var failure = CaptureInvalidOperation(() => IfcRoundTripExchangeResultSet.Create(source));

            if (failure.Message.IndexOf("cannot exceed 10000 input records", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Oversized non-generic IFC exchange-result Count must preserve the collection-bound diagnostic.", failure);
            if (source.EnumeratorRequested)
                throw new InvalidOperationException("Oversized non-generic IFC exchange-result Count must be rejected before requesting an enumerator.");
        }

        private static void AcceptsConsistentKnownCountsAndPreservesCanonicalOrdering()
        {
            var source = new ConsistentKnownCountCollection();
            var set = IfcRoundTripExchangeResultSet.Create(source);

            if (set.Items.Count != 2)
                throw new InvalidOperationException("Consistent known Count contracts must preserve ordinary IFC exchange-result ingestion.");
            if (!string.Equals(set.Items[0].ExternalObjectId, "A", StringComparison.Ordinal)
                || !string.Equals(set.Items[1].ExternalObjectId, "B", StringComparison.Ordinal))
                throw new InvalidOperationException("Known-count hardening changed canonical IFC exchange-result ordering.");
        }

        private static void PreservesNullRejection()
        {
            var values = new IfcRoundTripExchangeResult[] { NewResult("A"), null! };

            try
            {
                IfcRoundTripExchangeResultSet.Create(values);
            }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, "results", StringComparison.Ordinal)
                    || ex.Message.IndexOf("cannot contain null entries", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Known-count hardening changed IFC exchange-result null validation.", ex);
                return;
            }

            throw new InvalidOperationException("Known-count hardening must preserve IFC exchange-result null rejection.");
        }

        private static void PreservesExactBoundAndDuplicateCollapse()
        {
            var duplicate = NewResult("DUP");
            var exactBound = new IfcRoundTripExchangeResult[MaxResults];
            for (var index = 0; index < exactBound.Length; index++)
                exactBound[index] = duplicate;

            var set = IfcRoundTripExchangeResultSet.Create(exactBound);
            if (set.Items.Count != 1)
                throw new InvalidOperationException("Exact-bound IFC exchange-result ingestion must preserve duplicate identity collapse.");
            if (set.Items[0].State != IfcRoundTripResultState.InvalidOrAmbiguous
                || !string.Equals(set.Items[0].StateDetail, IfcRoundTripExchangeResultSet.DuplicateExternalIdentityDetail, StringComparison.Ordinal))
                throw new InvalidOperationException("Known-count hardening changed duplicate external-identity semantics.");
        }

        private static void PreservesStreamingBound()
        {
            var yielded = 0;
            var disposed = false;
            var failure = CaptureInvalidOperation(() => IfcRoundTripExchangeResultSet.Create(Enumerate()));

            if (failure.Message.IndexOf("cannot exceed 10000 input records", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Lazy IFC exchange-result input must preserve the streaming collection-bound diagnostic.", failure);
            if (yielded != MaxResults + 1)
                throw new InvalidOperationException("Lazy IFC exchange-result input must fail exactly on input record 10001.");
            if (!disposed)
                throw new InvalidOperationException("Lazy IFC exchange-result enumerator must be disposed after the streaming bound fails.");

            IEnumerable<IfcRoundTripExchangeResult> Enumerate()
            {
                try
                {
                    for (var index = 0; index <= MaxResults; index++)
                    {
                        yielded++;
                        yield return NewResult("STREAM");
                    }
                }
                finally
                {
                    disposed = true;
                }
            }
        }

        private static InvalidOperationException CaptureInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected IFC exchange-result ingestion to fail closed.");
        }

        private static IfcRoundTripExchangeResult NewResult(string externalObjectId)
        {
            return new IfcRoundTripExchangeResult(externalObjectId, IfcRoundTripResultState.Unmapped, null);
        }

        private sealed class NegativeKnownCountCollection : ICollection<IfcRoundTripExchangeResult>
        {
            internal bool EnumeratorRequested { get; private set; }
            public int Count => -1;
            public bool IsReadOnly => true;

            public IEnumerator<IfcRoundTripExchangeResult> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new InvalidOperationException("Negative known-count source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(IfcRoundTripExchangeResult item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(IfcRoundTripExchangeResult item) => false;
            public void CopyTo(IfcRoundTripExchangeResult[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(IfcRoundTripExchangeResult item) => throw new NotSupportedException();
        }

        private sealed class ConflictingKnownCountCollection : ICollection<IfcRoundTripExchangeResult>, IReadOnlyCollection<IfcRoundTripExchangeResult>
        {
            internal bool EnumeratorRequested { get; private set; }
            int ICollection<IfcRoundTripExchangeResult>.Count => 1;
            int IReadOnlyCollection<IfcRoundTripExchangeResult>.Count => 2;
            public bool IsReadOnly => true;

            public IEnumerator<IfcRoundTripExchangeResult> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new InvalidOperationException("Conflicting known-count source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(IfcRoundTripExchangeResult item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(IfcRoundTripExchangeResult item) => false;
            public void CopyTo(IfcRoundTripExchangeResult[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(IfcRoundTripExchangeResult item) => throw new NotSupportedException();
        }

        private sealed class OversizedNonGenericKnownCountCollection : IEnumerable<IfcRoundTripExchangeResult>, ICollection
        {
            internal bool EnumeratorRequested { get; private set; }
            int ICollection.Count => MaxResults + 1;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<IfcRoundTripExchangeResult> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new InvalidOperationException("Oversized non-generic known-count source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ConsistentKnownCountCollection : ICollection<IfcRoundTripExchangeResult>, IReadOnlyCollection<IfcRoundTripExchangeResult>, ICollection
        {
            int ICollection<IfcRoundTripExchangeResult>.Count => 2;
            int IReadOnlyCollection<IfcRoundTripExchangeResult>.Count => 2;
            int ICollection.Count => 2;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<IfcRoundTripExchangeResult> GetEnumerator()
            {
                yield return NewResult("B");
                yield return NewResult("A");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(IfcRoundTripExchangeResult item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(IfcRoundTripExchangeResult item) => false;
            public void CopyTo(IfcRoundTripExchangeResult[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(IfcRoundTripExchangeResult item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}
