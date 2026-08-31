using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class DeepCostKnownCountNoOverreadSmoke
    {
        internal static void Run()
        {
            RateReferencesRejectBeforeUnexpectedCurrent();
            BuildUpRejectsBeforeUnexpectedCurrent();
            TradeAnalysisRejectsBeforeUnexpectedCurrent();
            BqCatalogRejectsBeforeUnexpectedCurrent();
            BqImportRejectsBeforeUnexpectedCurrent();
            StableCountedInputsRemainAccepted();
        }

        private static void RateReferencesRejectBeforeUnexpectedCurrent()
        {
            var source = new CountedSource<RateReferenceEdge>(1,
                new RateReferenceEdge("R1", RateReferenceTargetKind.BillItem, "B1"),
                new RateReferenceEdge("R2", RateReferenceTargetKind.UnitRate, "R3"));
            Throws<ArgumentException>(() => new RateReferenceGraph(source));
            AssertNoOverread(source);
        }

        private static void BuildUpRejectsBeforeUnexpectedCurrent()
        {
            var source = new CountedSource<BuildUpRateSnapshot>(1,
                new BuildUpRateSnapshot("R1", 1m),
                new BuildUpRateSnapshot("R2", 2m));
            var graph = new RateReferenceGraph(Array.Empty<RateReferenceEdge>());
            Throws<InvalidOperationException>(() => new BuildUpAnalysisService().Analyze(source, graph, adoptedOnly: false));
            AssertNoOverread(source);
        }

        private static void TradeAnalysisRejectsBeforeUnexpectedCurrent()
        {
            var source = new CountedSource<TradeCostItem>(1,
                new TradeCostItem("B1", "Arch", 1m),
                new TradeCostItem("B2", "Struct", 2m));
            Throws<InvalidOperationException>(() => new TradeCostAnalysisService().Analyze(source, 10m));
            AssertNoOverread(source);
        }

        private static void BqCatalogRejectsBeforeUnexpectedCurrent()
        {
            var source = new CountedSource<BqLibraryEntry>(1, Entry("B1"), Entry("B2"));
            Throws<InvalidOperationException>(() => new BqLibraryCatalog("LIB", source));
            AssertNoOverread(source);
        }

        private static void BqImportRejectsBeforeUnexpectedCurrent()
        {
            var catalog = new BqLibraryCatalog("LIB", new[] { Entry("BASE") });
            var source = new CountedSource<BqLibraryEntry>(1, Entry("B1"), Entry("B2"));
            Throws<InvalidOperationException>(() => catalog.ImportFromProject(source, replaceExisting: true));
            AssertNoOverread(source);
        }

        private static void StableCountedInputsRemainAccepted()
        {
            var edges = new CountedSource<RateReferenceEdge>(1,
                new RateReferenceEdge("R1", RateReferenceTargetKind.BillItem, "B1"));
            Equal(1, new RateReferenceGraph(edges).Edges.Count);
            Equal(1, edges.CurrentAccesses);

            var graph = new RateReferenceGraph(Array.Empty<RateReferenceEdge>());
            var rates = new CountedSource<BuildUpRateSnapshot>(1, new BuildUpRateSnapshot("R1", 1m));
            Equal(1, new BuildUpAnalysisService().Analyze(rates, graph, adoptedOnly: false).Count);

            var items = new CountedSource<TradeCostItem>(1, new TradeCostItem("B1", "Arch", 1m));
            Equal(1, new TradeCostAnalysisService().Analyze(items, 10m).Count);

            var entries = new CountedSource<BqLibraryEntry>(1, Entry("B1"));
            var catalog = new BqLibraryCatalog("LIB", entries);
            Equal(1, catalog.Entries.Count);

            var imported = new CountedSource<BqLibraryEntry>(1, Entry("B2"));
            Equal(2, catalog.ImportFromProject(imported, replaceExisting: true).Entries.Count);
        }

        private static BqLibraryEntry Entry(string itemCode) =>
            new BqLibraryEntry(itemCode, "Description " + itemCode, "m", "Trade", 1m);

        private static void AssertNoOverread<T>(CountedSource<T> source)
        {
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentAccesses);
        }

        private sealed class CountedSource<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly IReadOnlyList<T> _items;
            private readonly int _count;

            internal CountedSource(int count, params T[] items)
            {
                _count = count;
                _items = items;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentAccesses { get; private set; }
            int ICollection<T>.Count => _count;
            int IReadOnlyCollection<T>.Count => _count;
            int ICollection.Count => _count;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => throw new NotSupportedException();
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CountedSource<T> _owner;
                private int _index = -1;
                internal Enumerator(CountedSource<T> owner) => _owner = owner;
                public T Current
                {
                    get
                    {
                        _owner.CurrentAccesses++;
                        return _owner._items[_index];
                    }
                }
                object? IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    var next = _index + 1;
                    if (next >= _owner._items.Count) return false;
                    _index = next;
                    return true;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException error) { return error; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class DeepCostKnownCountNoOverreadSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DeepCostKnownCountNoOverreadSmoke.Run();
    }
}
