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
            BillItemTraversalMustMatchKnownCount();
            BuildUpTraversalMustMatchKnownCount();
            RateReferenceTraversalMustMatchKnownCount();
            LibraryTraversalMustMatchKnownCount();
            BillItemCountDriftFailsAfterTraversal();
            BuildUpCountDriftFailsAfterTraversal();
            RateReferenceCountDriftFailsAfterTraversal();
            LibraryCountDriftFailsAfterTraversal();
            NegativeCountAfterTraversalFailsClosed();
            ConflictingCountsAfterTraversalFailClosed();
            StableMultiInterfaceCountsRemainAccepted();
            ExactKnownCountsRemainAccepted();
            PureStreamingSourcesRemainAccepted();
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

        private static void BillItemTraversalMustMatchKnownCount()
        {
            var under = new CountedSequence<TbqBillItem>(2, Bill("B1"));
            var underError = Capture<InvalidOperationException>(() => Workspace(billItems: under));
            AssertTraversalMismatch(under, "bill items", 2, 1, 5, underError);

            var over = new CountedSequence<TbqBillItem>(1, Bill("B1"), Bill("B2"));
            var overError = Capture<InvalidOperationException>(() => Workspace(billItems: over));
            AssertTraversalMismatch(over, "bill items", 1, 2, 6, overError);
        }

        private static void BuildUpTraversalMustMatchKnownCount()
        {
            var under = new CountedSequence<BuildUpRateSnapshot>(2, BuildUp("R1"));
            var underError = Capture<InvalidOperationException>(() => Workspace(buildUpRates: under));
            AssertTraversalMismatch(under, "build-up rates", 2, 1, 5, underError);

            var over = new CountedSequence<BuildUpRateSnapshot>(1, BuildUp("R1"), BuildUp("R2"));
            var overError = Capture<InvalidOperationException>(() => Workspace(buildUpRates: over));
            AssertTraversalMismatch(over, "build-up rates", 1, 2, 6, overError);
        }

        private static void RateReferenceTraversalMustMatchKnownCount()
        {
            var under = new CountedSequence<RateReferenceEdge>(2, Reference("R1"));
            var underError = Capture<InvalidOperationException>(() => Workspace(rateReferences: under));
            AssertTraversalMismatch(under, "rate references", 2, 1, 5, underError);

            var over = new CountedSequence<RateReferenceEdge>(1, Reference("R1"), Reference("R2"));
            var overError = Capture<InvalidOperationException>(() => Workspace(rateReferences: over));
            AssertTraversalMismatch(over, "rate references", 1, 2, 6, overError);
        }

        private static void LibraryTraversalMustMatchKnownCount()
        {
            var under = new CountedSequence<BqLibraryEntry>(2, Library("L1"));
            var underError = Capture<InvalidOperationException>(() => Workspace(libraryEntries: under));
            AssertTraversalMismatch(under, "BQ library entries", 2, 1, 5, underError);

            var over = new CountedSequence<BqLibraryEntry>(1, Library("L1"), Library("L2"));
            var overError = Capture<InvalidOperationException>(() => Workspace(libraryEntries: over));
            AssertTraversalMismatch(over, "BQ library entries", 1, 2, 6, overError);
        }

        private static void BillItemCountDriftFailsAfterTraversal()
        {
            var source = new DriftingReadOnlyCollection<TbqBillItem>(1, 2, Bill("B1"));
            var error = Capture<InvalidOperationException>(() => Workspace(billItems: source));
            AssertPostTraversalDrift(source, "bill items", error);
        }

        private static void BuildUpCountDriftFailsAfterTraversal()
        {
            var source = new DriftingReadOnlyCollection<BuildUpRateSnapshot>(1, 2, BuildUp("R1"));
            var error = Capture<InvalidOperationException>(() => Workspace(buildUpRates: source));
            AssertPostTraversalDrift(source, "build-up rates", error);
        }

        private static void RateReferenceCountDriftFailsAfterTraversal()
        {
            var source = new DriftingReadOnlyCollection<RateReferenceEdge>(1, 2, Reference("R1"));
            var error = Capture<InvalidOperationException>(() => Workspace(rateReferences: source));
            AssertPostTraversalDrift(source, "rate references", error);
        }

        private static void LibraryCountDriftFailsAfterTraversal()
        {
            var source = new DriftingReadOnlyCollection<BqLibraryEntry>(1, 2, Library("L1"));
            var error = Capture<InvalidOperationException>(() => Workspace(libraryEntries: source));
            AssertPostTraversalDrift(source, "BQ library entries", error);
        }

        private static void NegativeCountAfterTraversalFailsClosed()
        {
            var source = new DriftingReadOnlyCollection<TbqBillItem>(1, -1, Bill("B1"));
            var error = Capture<InvalidOperationException>(() => Workspace(billItems: source));
            Equal(6, source.CountReads, "Post-traversal negative TBQ Count must be rebound throughout traversal.");
            Equal(1, source.GetEnumeratorCalls, "Post-traversal negative TBQ source must traverse exactly once.");
            Contains("negative known count", error.Message, "Post-traversal negative TBQ Count must fail closed explicitly.");
        }

        private static void ConflictingCountsAfterTraversalFailClosed()
        {
            var source = new MultiCountSequence<TbqBillItem>(1, 1, 1, 1, 2, 1, Bill("B1"));
            var error = Capture<InvalidOperationException>(() => Workspace(billItems: source));
            Equal(6, source.GenericCountReads, "Post-traversal conflict must rebind ICollection<T>.Count throughout traversal.");
            Equal(6, source.ReadOnlyCountReads, "Post-traversal conflict must rebind IReadOnlyCollection<T>.Count throughout traversal.");
            Equal(6, source.NonGenericCountReads, "Post-traversal conflict must rebind ICollection.Count throughout traversal.");
            Equal(1, source.GetEnumeratorCalls, "Post-traversal conflict source must traverse exactly once.");
            Contains("conflicting known counts", error.Message, "Post-traversal multi-interface Count conflict must fail closed.");
        }

        private static void StableMultiInterfaceCountsRemainAccepted()
        {
            var source = new MultiCountSequence<TbqBillItem>(1, 1, 1, 1, 1, 1, Bill("B1"));
            var workspace = Workspace(billItems: source);
            Equal(1, workspace.BillItems.Count, "Stable multi-interface TBQ source must remain accepted.");
            Equal(6, source.GenericCountReads, "Stable ICollection<T>.Count must be rebound throughout traversal.");
            Equal(6, source.ReadOnlyCountReads, "Stable IReadOnlyCollection<T>.Count must be rebound throughout traversal.");
            Equal(6, source.NonGenericCountReads, "Stable ICollection.Count must be rebound throughout traversal.");
            Equal(1, source.GetEnumeratorCalls, "Stable multi-interface TBQ source must traverse exactly once.");
        }

        private static void ExactKnownCountsRemainAccepted()
        {
            var billItems = new CountedSequence<TbqBillItem>(1, Bill("B1"));
            var buildUps = new CountedSequence<BuildUpRateSnapshot>(1, BuildUp("R1"));
            var references = new CountedSequence<RateReferenceEdge>(1, Reference("R1"));
            var library = new CountedSequence<BqLibraryEntry>(1, Library("L1"));
            var workspace = Workspace(billItems, buildUps, references, library);

            Equal(1, workspace.BillItems.Count, "Exact counted bill-item traversal must remain accepted.");
            Equal(1, workspace.BuildUpRates.Count, "Exact counted build-up traversal must remain accepted.");
            Equal(1, workspace.RateReferences.Edges.Count, "Exact counted rate-reference traversal must remain accepted.");
            Equal(1, workspace.Library.Entries.Count, "Exact counted library traversal must remain accepted.");
            Equal(6, billItems.CountReads, "Exact bill-item Count must be rebound throughout traversal.");
            Equal(6, buildUps.CountReads, "Exact build-up Count must be rebound throughout traversal.");
            Equal(6, references.CountReads, "Exact rate-reference Count must be rebound throughout traversal.");
            Equal(6, library.CountReads, "Exact library Count must be rebound throughout traversal.");
        }

        private static void PureStreamingSourcesRemainAccepted()
        {
            var workspace = Workspace(
                Stream(Bill("B1")),
                Stream(BuildUp("R1")),
                Stream(Reference("R1")),
                Stream(Library("L1")));

            Equal(1, workspace.BillItems.Count, "Pure streaming bill items must remain accepted.");
            Equal(1, workspace.BuildUpRates.Count, "Pure streaming build-ups must remain accepted.");
            Equal(1, workspace.RateReferences.Edges.Count, "Pure streaming rate references must remain accepted.");
            Equal(1, workspace.Library.Entries.Count, "Pure streaming library entries must remain accepted.");
        }

        private static TbqBillItem Bill(string id) => new TbqBillItem(id, "Item " + id, "m", "TRADE", 1m, 1m);

        private static BuildUpRateSnapshot BuildUp(string id) => new BuildUpRateSnapshot(id, 1m);

        private static RateReferenceEdge Reference(string id) =>
            new RateReferenceEdge(id, RateReferenceTargetKind.BillItem, "ITEM-" + id);

        private static BqLibraryEntry Library(string id) => new BqLibraryEntry(id, "Item " + id, "m", "CAT");

        private static IEnumerable<T> Stream<T>(params T[] items)
        {
            for (var i = 0; i < items.Length; i++)
                yield return items[i];
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

        private static void AssertTraversalMismatch<T>(
            CountedSequence<T> source,
            string label,
            int expectedCount,
            int observedCount,
            int expectedCountReads,
            InvalidOperationException error)
        {
            Equal(expectedCountReads, source.CountReads, "TBQ " + label + " Count must be rebound throughout traversal before mismatch.");
            Equal(1, source.GetEnumeratorCalls, "TBQ " + label + " mismatch source must be enumerated exactly once.");
            Contains(label + " traversal produced " + observedCount, error.Message, "TBQ " + label + " mismatch must report observed traversal count.");
            Contains("known count reported " + expectedCount, error.Message, "TBQ " + label + " mismatch must report snapshotted Count.");
        }

        private static void AssertPostTraversalDrift<T>(DriftingReadOnlyCollection<T> source, string label, InvalidOperationException error)
        {
            Equal(6, source.CountReads, "TBQ " + label + " Count must be rebound throughout exact traversal.");
            Equal(1, source.GetEnumeratorCalls, "TBQ " + label + " drift source must traverse exactly once.");
            Contains(label + " known count changed during traversal", error.Message, "TBQ " + label + " Count drift must fail closed before publication.");
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

        private sealed class CountedSequence<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _advertisedCount;

            internal CountedSequence(int advertisedCount, params T[] items)
            {
                _advertisedCount = advertisedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _advertisedCount;
                }
            }

            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class DriftingReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly int _initialCount;
            private readonly int _finalCount;
            private readonly T[] _items;
            private bool _traversalCompleted;

            internal DriftingReadOnlyCollection(int initialCount, int finalCount, params T[] items)
            {
                _initialCount = initialCount;
                _finalCount = finalCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _traversalCompleted ? _finalCount : _initialCount;
                }
            }

            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return Enumerate().GetEnumerator();
            }

            private IEnumerable<T> Enumerate()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i];
                }
                finally
                {
                    _traversalCompleted = true;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class MultiCountSequence<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly int _initialGenericCount;
            private readonly int _initialReadOnlyCount;
            private readonly int _initialNonGenericCount;
            private readonly int _finalGenericCount;
            private readonly int _finalReadOnlyCount;
            private readonly int _finalNonGenericCount;
            private readonly T[] _items;
            private bool _traversalCompleted;

            internal MultiCountSequence(
                int initialGenericCount,
                int initialReadOnlyCount,
                int initialNonGenericCount,
                int finalGenericCount,
                int finalReadOnlyCount,
                int finalNonGenericCount,
                params T[] items)
            {
                _initialGenericCount = initialGenericCount;
                _initialReadOnlyCount = initialReadOnlyCount;
                _initialNonGenericCount = initialNonGenericCount;
                _finalGenericCount = finalGenericCount;
                _finalReadOnlyCount = finalReadOnlyCount;
                _finalNonGenericCount = finalNonGenericCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            int ICollection<T>.Count
            {
                get
                {
                    GenericCountReads++;
                    return _traversalCompleted ? _finalGenericCount : _initialGenericCount;
                }
            }

            int IReadOnlyCollection<T>.Count
            {
                get
                {
                    ReadOnlyCountReads++;
                    return _traversalCompleted ? _finalReadOnlyCount : _initialReadOnlyCount;
                }
            }

            int ICollection.Count
            {
                get
                {
                    NonGenericCountReads++;
                    return _traversalCompleted ? _finalNonGenericCount : _initialNonGenericCount;
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
                return Enumerate().GetEnumerator();
            }

            private IEnumerable<T> Enumerate()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i];
                }
                finally
                {
                    _traversalCompleted = true;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => false;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
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