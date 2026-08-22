using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripQuantityEvidenceKnownCountConflictSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsOversizedSecondaryKnownCountBeforeEnumeration();
            RejectsOversizedKnownCountBeforeNegativeDiagnostic();
            RejectsNegativeGenericKnownCountBeforeEnumeration();
            RejectsNegativeReadOnlyKnownCountBeforeEnumeration();
            RejectsNegativeNonGenericKnownCountBeforeEnumeration();
            RejectsConflictingInBoundKnownCountsBeforeEnumeration();
            EqualKnownCountContractsRemainAccepted();
        }

        private static void RejectsOversizedSecondaryKnownCountBeforeEnumeration()
        {
            var source = new MultiCountCollection(1, 10001, 1, true, Array.Empty<IfcRoundTripQuantityEvidence>());
            var failure = CaptureFailure(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Contains(failure.Message, "supports at most 10000 candidates", "oversized secondary Count diagnostic");
            AssertAllCountsReadExactlyOnceWithoutEnumeration(source, "oversized secondary Count");
        }

        private static void RejectsOversizedKnownCountBeforeNegativeDiagnostic()
        {
            var source = new MultiCountCollection(-1, 10001, -1, true, Array.Empty<IfcRoundTripQuantityEvidence>());
            var failure = CaptureFailure(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Contains(failure.Message, "supports at most 10000 candidates", "oversized Count precedence over negative Count");
            AssertAllCountsReadExactlyOnceWithoutEnumeration(source, "oversized Count with negative peer");
        }

        private static void RejectsNegativeGenericKnownCountBeforeEnumeration()
        {
            var source = new MultiCountCollection(-1, 2, 2, true, Array.Empty<IfcRoundTripQuantityEvidence>());
            var failure = CaptureFailure(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Equal(
                "IFC round-trip quantity evidence source exposes an invalid negative known Count value.",
                failure.Message,
                "negative generic Count diagnostic");
            AssertAllCountsReadExactlyOnceWithoutEnumeration(source, "negative generic Count");
        }

        private static void RejectsNegativeReadOnlyKnownCountBeforeEnumeration()
        {
            var source = new MultiCountCollection(2, -1, 2, true, Array.Empty<IfcRoundTripQuantityEvidence>());
            var failure = CaptureFailure(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Equal(
                "IFC round-trip quantity evidence source exposes an invalid negative known Count value.",
                failure.Message,
                "negative read-only Count diagnostic");
            AssertAllCountsReadExactlyOnceWithoutEnumeration(source, "negative read-only Count");
        }

        private static void RejectsNegativeNonGenericKnownCountBeforeEnumeration()
        {
            var source = new MultiCountCollection(2, 2, -1, true, Array.Empty<IfcRoundTripQuantityEvidence>());
            var failure = CaptureFailure(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Equal(
                "IFC round-trip quantity evidence source exposes an invalid negative known Count value.",
                failure.Message,
                "negative non-generic Count diagnostic");
            AssertAllCountsReadExactlyOnceWithoutEnumeration(source, "negative non-generic Count");
        }

        private static void RejectsConflictingInBoundKnownCountsBeforeEnumeration()
        {
            var source = new MultiCountCollection(1, 2, 2, true, Array.Empty<IfcRoundTripQuantityEvidence>());
            var failure = CaptureFailure(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Equal(
                "IFC round-trip quantity evidence source exposes conflicting known Count values.",
                failure.Message,
                "in-bound conflicting Count diagnostic");
            AssertAllCountsReadExactlyOnceWithoutEnumeration(source, "in-bound conflicting Count");
        }

        private static void EqualKnownCountContractsRemainAccepted()
        {
            var candidate = new IfcRoundTripQuantityEvidence("Area", 12d, "m2", "SRC", "P1");
            var source = new MultiCountCollection(1, 1, 1, false, new[] { candidate });
            var result = IfcRoundTripQuantityEvidenceSet.Create(source);

            Equal(1, result.CandidateCount, "equal Count candidate count");
            Equal(1, result.Groups.Count, "equal Count group count");
            Equal(1, source.GenericCountReads, "equal Count generic contract read count");
            Equal(1, source.ReadOnlyCountReads, "equal Count read-only contract read count");
            Equal(1, source.NonGenericCountReads, "equal Count non-generic contract read count");
            True(source.EnumeratorRequested, "equal Count source should be enumerated");
        }

        private static InvalidOperationException CaptureFailure(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                return ex;
            }

            throw new Exception("Expected IFC quantity evidence known-count validation to fail.");
        }

        private static void AssertAllCountsReadExactlyOnceWithoutEnumeration(MultiCountCollection source, string label)
        {
            Equal(1, source.GenericCountReads, label + " generic Count read count");
            Equal(1, source.ReadOnlyCountReads, label + " read-only Count read count");
            Equal(1, source.NonGenericCountReads, label + " non-generic Count read count");
            True(!source.EnumeratorRequested, label + " requested an enumerator");
        }

        private sealed class MultiCountCollection :
            ICollection<IfcRoundTripQuantityEvidence>,
            IReadOnlyCollection<IfcRoundTripQuantityEvidence>,
            ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;
            private readonly IfcRoundTripQuantityEvidence[] _items;

            internal MultiCountCollection(
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                bool throwOnEnumeration,
                IfcRoundTripQuantityEvidence[] items)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
                _items = items;
            }

            int ICollection<IfcRoundTripQuantityEvidence>.Count
            {
                get
                {
                    GenericCountReads++;
                    return _genericCount;
                }
            }

            int IReadOnlyCollection<IfcRoundTripQuantityEvidence>.Count
            {
                get
                {
                    ReadOnlyCountReads++;
                    return _readOnlyCount;
                }
            }

            int ICollection.Count
            {
                get
                {
                    NonGenericCountReads++;
                    return _nonGenericCount;
                }
            }

            internal int GenericCountReads { get; private set; }
            internal int ReadOnlyCountReads { get; private set; }
            internal int NonGenericCountReads { get; private set; }
            internal bool EnumeratorRequested { get; private set; }

            public bool IsReadOnly => true;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<IfcRoundTripQuantityEvidence> GetEnumerator()
            {
                EnumeratorRequested = true;
                if (_throwOnEnumeration)
                    throw new Exception("Known-count validation must fail before IFC evidence enumeration.");
                return ((IEnumerable<IfcRoundTripQuantityEvidence>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(IfcRoundTripQuantityEvidence item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(IfcRoundTripQuantityEvidence item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(IfcRoundTripQuantityEvidence[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public bool Remove(IfcRoundTripQuantityEvidence item) => throw new NotSupportedException();
            public void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        }

        private static void Contains(string actual, string expected, string label)
        {
            if (actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("IfcRoundTripQuantityEvidenceKnownCountConflictSmoke " + label + ": actual=" + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("IfcRoundTripQuantityEvidenceKnownCountConflictSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool condition, string label)
        {
            if (!condition)
                throw new Exception("IfcRoundTripQuantityEvidenceKnownCountConflictSmoke " + label + ".");
        }
    }
}
