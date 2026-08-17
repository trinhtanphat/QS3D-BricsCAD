using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TbqWorkspaceKnownCountSmoke
    {
        private const int MaximumOrdinaryEntries = 10000;
        private const int MaximumRateReferences = 50000;

        internal static void Run()
        {
            OversizedBillItemsFailBeforeEnumeration();
            OversizedBuildUpRatesFailBeforeEnumeration();
            OversizedRateReferencesFailBeforeEnumeration();
            OversizedLibraryEntriesFailBeforeEnumeration();
            NegativeBillItemCountFailsBeforeEnumeration();
            NegativeBuildUpRateCountFailsBeforeEnumeration();
            NegativeRateReferenceCountFailsBeforeEnumeration();
            NegativeLibraryEntryCountFailsBeforeEnumeration();
            ConflictingBillItemCountsFailBeforeEnumeration();
            ConflictingRateReferenceCountsAboveTenThousandFailBeforeEnumeration();
        }

        private static void OversizedBillItemsFailBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<TbqBillItem>(MaximumOrdinaryEntries + 1);
            var error = Capture<InvalidOperationException>(() => Workspace(billItems: source));
            Equal(0, source.GetEnumeratorCalls, "Oversized counted TBQ bill items must fail before enumeration.");
            Contains("at most 10000 bill items", error.Message, "TBQ bill-item oversize must report the workspace bound.");
        }

        private static void OversizedBuildUpRatesFailBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<BuildUpRateSnapshot>(MaximumOrdinaryEntries + 1);
            var error = Capture<InvalidOperationException>(() => Workspace(buildUpRates: source));
            Equal(0, source.GetEnumeratorCalls, "Oversized counted TBQ build-up rates must fail before enumeration.");
            Contains("at most 10000 build-up rates", error.Message, "TBQ build-up oversize must report the workspace bound.");
        }

        private static void OversizedRateReferencesFailBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<RateReferenceEdge>(MaximumRateReferences + 1);
            var error = Capture<InvalidOperationException>(() => Workspace(rateReferences: source));
            Equal(0, source.GetEnumeratorCalls, "Oversized counted TBQ rate references must fail before enumeration.");
            Contains("at most 50000 rate references", error.Message, "TBQ rate-reference oversize must report the workspace bound.");
        }

        private static void OversizedLibraryEntriesFailBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<BqLibraryEntry>(MaximumOrdinaryEntries + 1);
            var error = Capture<InvalidOperationException>(() => Workspace(libraryEntries: source));
            Equal(0, source.GetEnumeratorCalls, "Oversized counted TBQ library entries must fail before enumeration.");
            Contains("at most 10000 BQ library entries", error.Message, "TBQ library oversize must report the workspace bound.");
        }

        private static void NegativeBillItemCountFailsBeforeEnumeration()
        {
            var source = new NegativeReadOnlyCount<TbqBillItem>();
            var error = Capture<InvalidOperationException>(() => Workspace(billItems: source));
            Equal(1, source.CountReads, "TBQ bill-item Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative TBQ bill-item Count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative TBQ bill-item Count must fail closed explicitly.");
        }

        private static void NegativeBuildUpRateCountFailsBeforeEnumeration()
        {
            var source = new NegativeReadOnlyCount<BuildUpRateSnapshot>();
            var error = Capture<InvalidOperationException>(() => Workspace(buildUpRates: source));
            Equal(1, source.CountReads, "TBQ build-up Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative TBQ build-up Count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative TBQ build-up Count must fail closed explicitly.");
        }

        private static void NegativeRateReferenceCountFailsBeforeEnumeration()
        {
            var source = new NegativeReadOnlyCount<RateReferenceEdge>();
            var error = Capture<InvalidOperationException>(() => Workspace(rateReferences: source));
            Equal(1, source.CountReads, "TBQ rate-reference Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative TBQ rate-reference Count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative TBQ rate-reference Count must fail closed explicitly.");
        }

        private static void NegativeLibraryEntryCountFailsBeforeEnumeration()
        {
            var source = new NegativeReadOnlyCount<BqLibraryEntry>();
            var error = Capture<InvalidOperationException>(() => Workspace(libraryEntries: source));
            Equal(1, source.CountReads, "TBQ library Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative TBQ library Count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative TBQ library Count must fail closed explicitly.");
        }

        private static void ConflictingBillItemCountsFailBeforeEnumeration()
        {
            var source = new MultiCountNeverEnumerated<TbqBillItem>(1, 2, 1);
            var error = Capture<InvalidOperationException>(() => Workspace(billItems: source));
            AssertAllCountContractsReadOnce(source, "TBQ bill-item conflicting Count validation");
            Equal(0, source.GetEnumeratorCalls, "Conflicting TBQ bill-item Counts must fail before enumeration.");
            Contains("conflicting known counts", error.Message, "Conflicting TBQ bill-item Count contracts must fail closed.");
        }

        private static void ConflictingRateReferenceCountsAboveTenThousandFailBeforeEnumeration()
        {
            var source = new MultiCountNeverEnumerated<RateReferenceEdge>(20000, 30000, 20000);
            var error = Capture<InvalidOperationException>(() => Workspace(rateReferences: source));
            AssertAllCountContractsReadOnce(source, "TBQ rate-reference conflicting Count validation");
            Equal(0, source.GetEnumeratorCalls, "Conflicting in-bound TBQ rate-reference Counts must fail before enumeration.");
            Contains("conflicting known counts", error.Message, "Rate-reference Count conflicts below 50,000 must not be hidden by a 10,000-entry helper threshold.");
        }

        private static TbqProjectWorkspaceState Workspace(
            IEnumerable<TbqBillItem>? billItems = null,
            IEnumerable<BuildUpRateSnapshot>? buildUpRates = null,
            IEnumerable<RateReferenceEdge>? rateReferences = null,
            IEnumerable<BqLibraryEntry>? libraryEntries = null)
        {
            return new TbqProjectWorkspaceState(
                "VND",
                0m,
                billItems ?? Array.Empty<TbqBillItem>(),
                buildUpRates ?? Array.Empty<BuildUpRateSnapshot>(),
                rateReferences ?? Array.Empty<RateReferenceEdge>(),
                "LIB",
                libraryEntries ?? Array.Empty<BqLibraryEntry>());
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

        private sealed class CountedNeverEnumerated<T> : IReadOnlyCollection<T>
        {
            internal CountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Counted TBQ source must not be enumerated after known-count rejection.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NegativeReadOnlyCount<T> : IReadOnlyCollection<T>
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

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Negative-count TBQ source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
                throw new InvalidOperationException("Conflicting-count TBQ source must not be enumerated.");
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

    internal static class TbqWorkspaceKnownCountRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TbqWorkspaceKnownCountSmoke.Run();
        }
    }
}