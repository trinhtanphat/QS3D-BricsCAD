using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateReferenceGraphKnownCountSmoke
    {
        internal static void Run()
        {
            EmptyGraphRemainsValid();
            OversizedKnownCountFailsBeforeEnumeration();
            NegativeKnownCountFailsBeforeEnumeration();
            ConflictingInBoundKnownCountsFailBeforeEnumeration();
        }

        private static void EmptyGraphRemainsValid()
        {
            var graph = new RateReferenceGraph(Array.Empty<RateReferenceEdge>());
            Equal(0, graph.Edges.Count, "Empty rate-reference graph must remain valid.");
        }

        private static void OversizedKnownCountFailsBeforeEnumeration()
        {
            var source = new MultiCountNeverEnumerated<RateReferenceEdge>(50001, 50001, 50001);
            var error = Capture<InvalidOperationException>(() => new RateReferenceGraph(source));
            AssertAllCountContractsReadOnce(source, "Oversized rate-reference Count validation");
            Equal(0, source.GetEnumeratorCalls, "Oversized rate-reference Count must fail before enumeration.");
            Contains("supports at most 50000 entries", error.Message, "Rate-reference oversize must preserve the graph capacity error.");
        }

        private static void NegativeKnownCountFailsBeforeEnumeration()
        {
            var source = new MultiCountNeverEnumerated<RateReferenceEdge>(-1, -1, -1);
            var error = Capture<ArgumentException>(() => new RateReferenceGraph(source));
            AssertAllCountContractsReadOnce(source, "Negative rate-reference Count validation");
            Equal(0, source.GetEnumeratorCalls, "Negative rate-reference Count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative rate-reference Count must fail closed explicitly.");
        }

        private static void ConflictingInBoundKnownCountsFailBeforeEnumeration()
        {
            var source = new MultiCountNeverEnumerated<RateReferenceEdge>(20000, 30000, 20000);
            var error = Capture<ArgumentException>(() => new RateReferenceGraph(source));
            AssertAllCountContractsReadOnce(source, "Conflicting rate-reference Count validation");
            Equal(0, source.GetEnumeratorCalls, "Conflicting in-bound rate-reference Counts must fail before enumeration.");
            Contains("conflicting known counts", error.Message, "Count conflicts between 10,001 and 50,000 must not be hidden by the ordinary advanced-cost ceiling.");
        }

        private static void AssertAllCountContractsReadOnce<T>(MultiCountNeverEnumerated<T> source, string message)
        {
            Equal(1, source.GenericCountReads, message + " must inspect ICollection<T>.Count exactly once.");
            Equal(1, source.ReadOnlyCountReads, message + " must inspect IReadOnlyCollection<T>.Count exactly once.");
            Equal(1, source.NonGenericCountReads, message + " must inspect ICollection.Count exactly once.");
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
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class MultiCountNeverEnumerated<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
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

            int ICollection<T>.Count
            {
                get
                {
                    GenericCountReads++;
                    return _genericCount;
                }
            }

            int IReadOnlyCollection<T>.Count
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

            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int GenericCountReads { get; private set; }
            internal int ReadOnlyCountReads { get; private set; }
            internal int NonGenericCountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Malformed rate-reference source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => false;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }

    internal static class RateReferenceGraphKnownCountRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RateReferenceGraphKnownCountSmoke.Run();
        }
    }
}
