using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripQuantityEvidenceNegativeKnownCountSmoke
    {
        private const int MaximumCandidates = 10000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            GenericNegativeCountFailsBeforeEnumeration();
            ReadOnlyNegativeCountFailsBeforeEnumeration();
            NonGenericNegativeCountFailsBeforeEnumeration();
            NegativeConflictPrefersNegativeDiagnostic();
            OversizeNegativePrefersCapacityDiagnostic();
        }

        private static void GenericNegativeCountFailsBeforeEnumeration()
        {
            var source = new NegativeGenericCountSource();
            var error = Capture<InvalidOperationException>(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Equal(1, source.CountReads, "ICollection<T>.Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative ICollection<T>.Count must fail before enumeration.");
            Contains("invalid negative known Count value", error.Message, "Negative generic Count must fail closed explicitly.");
        }

        private static void ReadOnlyNegativeCountFailsBeforeEnumeration()
        {
            var source = new NegativeReadOnlyCountSource();
            var error = Capture<InvalidOperationException>(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Equal(1, source.CountReads, "IReadOnlyCollection<T>.Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative IReadOnlyCollection<T>.Count must fail before enumeration.");
            Contains("invalid negative known Count value", error.Message, "Negative read-only Count must fail closed explicitly.");
        }

        private static void NonGenericNegativeCountFailsBeforeEnumeration()
        {
            var source = new NegativeNonGenericCountSource();
            var error = Capture<InvalidOperationException>(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Equal(1, source.CountReads, "ICollection.Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative ICollection.Count must fail before enumeration.");
            Contains("invalid negative known Count value", error.Message, "Negative non-generic Count must fail closed explicitly.");
        }

        private static void NegativeConflictPrefersNegativeDiagnostic()
        {
            var source = new MultiCountNeverEnumerated(-1, 2, 2);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Equal(1, source.GenericCountReads, "Negative/conflict validation must inspect ICollection<T>.Count exactly once.");
            Equal(1, source.ReadOnlyCountReads, "Negative/conflict validation must inspect IReadOnlyCollection<T>.Count exactly once.");
            Equal(1, source.NonGenericCountReads, "Negative/conflict validation must inspect ICollection.Count exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative/conflicting known Counts must fail before enumeration.");
            Contains("invalid negative known Count value", error.Message, "Negative known Count must take precedence over in-bound conflict.");
        }

        private static void OversizeNegativePrefersCapacityDiagnostic()
        {
            var source = new MultiCountNeverEnumerated(-1, MaximumCandidates + 1, -1);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripQuantityEvidenceSet.Create(source));

            Equal(1, source.GenericCountReads, "Oversize/negative validation must inspect ICollection<T>.Count exactly once.");
            Equal(1, source.ReadOnlyCountReads, "Oversize/negative validation must inspect IReadOnlyCollection<T>.Count exactly once.");
            Equal(1, source.NonGenericCountReads, "Oversize/negative validation must inspect ICollection.Count exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Oversize known Count must fail before enumeration even when another Count is negative.");
            Contains("at most 10000 candidates", error.Message, "Oversize known Count must preserve capacity-diagnostic precedence.");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
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
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class NegativeGenericCountSource : ICollection<IfcRoundTripQuantityEvidence>
        {
            public int Count
            {
                get
                {
                    CountReads++;
                    return -1;
                }
            }

            public bool IsReadOnly => true;
            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<IfcRoundTripQuantityEvidence> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Negative-count source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(IfcRoundTripQuantityEvidence item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(IfcRoundTripQuantityEvidence item) => false;
            public void CopyTo(IfcRoundTripQuantityEvidence[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(IfcRoundTripQuantityEvidence item) => throw new NotSupportedException();
        }

        private sealed class NegativeReadOnlyCountSource : IReadOnlyCollection<IfcRoundTripQuantityEvidence>
        {
            public int Count
            {
                get
                {
                    CountReads++;
                    return -1;
                }
            }

            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<IfcRoundTripQuantityEvidence> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Negative-count source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NegativeNonGenericCountSource : IEnumerable<IfcRoundTripQuantityEvidence>, ICollection
        {
            public int Count
            {
                get
                {
                    CountReads++;
                    return -1;
                }
            }

            public bool IsSynchronized => false;
            public object SyncRoot => this;
            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<IfcRoundTripQuantityEvidence> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Negative-count source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class MultiCountNeverEnumerated :
            ICollection<IfcRoundTripQuantityEvidence>,
            IReadOnlyCollection<IfcRoundTripQuantityEvidence>,
            ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal MultiCountNeverEnumerated(int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
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

            bool ICollection<IfcRoundTripQuantityEvidence>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int GenericCountReads { get; private set; }
            internal int ReadOnlyCountReads { get; private set; }
            internal int NonGenericCountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<IfcRoundTripQuantityEvidence> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Known-count source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<IfcRoundTripQuantityEvidence>.Add(IfcRoundTripQuantityEvidence item) => throw new NotSupportedException();
            void ICollection<IfcRoundTripQuantityEvidence>.Clear() => throw new NotSupportedException();
            bool ICollection<IfcRoundTripQuantityEvidence>.Contains(IfcRoundTripQuantityEvidence item) => false;
            void ICollection<IfcRoundTripQuantityEvidence>.CopyTo(IfcRoundTripQuantityEvidence[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<IfcRoundTripQuantityEvidence>.Remove(IfcRoundTripQuantityEvidence item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}
