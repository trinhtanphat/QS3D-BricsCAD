using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TbqWorkspaceTransientCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            BillItemsRejectTransientCountBeforeCurrent();
            BuildUpRatesRejectTransientCountBeforeCurrent();
            RateReferencesRejectTransientCountBeforeCurrent();
            LibraryEntriesRejectTransientCountBeforeCurrent();
            KnownCountOverYieldDoesNotReadExtraCurrent();
            StableCountedAndStreamingControlsSucceed();
            Console.WriteLine("PASS TBQ workspace transient Count stability");
        }

        private static void BillItemsRejectTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<TbqBillItem>(
                Bill("BILL-TRANSIENT"),
                2);

            ExpectCountFailure(
                () => Workspace(source, StableBuildUps(), StableReferences(), StableLibraryEntries()),
                "TBQ bill-item transient Count growth");
            Require(source.CurrentReads == 0,
                "TBQ bill-item traversal must reject transient Count drift before Current");
        }

        private static void BuildUpRatesRejectTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<BuildUpRateSnapshot>(
                new BuildUpRateSnapshot("RATE-TRANSIENT", 2m),
                0);

            ExpectCountFailure(
                () => Workspace(StableBills(), source, StableReferences(), StableLibraryEntries()),
                "TBQ build-up transient Count shrink");
            Require(source.CurrentReads == 0,
                "TBQ build-up traversal must reject transient Count drift before Current");
        }

        private static void RateReferencesRejectTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<RateReferenceEdge>(
                new RateReferenceEdge("RATE-REF", RateReferenceTargetKind.BillItem, "BILL-STABLE"),
                -1);

            ExpectCountFailure(
                () => Workspace(StableBills(), StableBuildUps(), source, StableLibraryEntries()),
                "TBQ rate-reference transient negative Count");
            Require(source.CurrentReads == 0,
                "TBQ rate-reference wrapper must reject transient Count drift before Current");
        }

        private static void LibraryEntriesRejectTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<BqLibraryEntry>(
                LibraryEntry("LIB-TRANSIENT"),
                2);

            ExpectCountFailure(
                () => Workspace(StableBills(), StableBuildUps(), StableReferences(), source),
                "TBQ library-entry transient Count growth");
            Require(source.CurrentReads == 0,
                "TBQ library-entry wrapper must reject transient Count drift before Current");
        }

        private static void KnownCountOverYieldDoesNotReadExtraCurrent()
        {
            var source = new OverYieldCountCollection<TbqBillItem>(
                Bill("BILL-ONE"),
                Bill("BILL-TWO"));

            ExpectCountFailure(
                () => Workspace(source, StableBuildUps(), StableReferences(), StableLibraryEntries()),
                "TBQ bill-item known-Count over-yield");
            Require(source.CurrentReads == 1,
                "TBQ known-Count over-yield must reject before reading the N+1 Current value");
        }

        private static void StableCountedAndStreamingControlsSucceed()
        {
            var counted = Workspace(
                StableBills(),
                StableBuildUps(),
                StableReferences(),
                StableLibraryEntries());
            Require(counted.BillItems.Count == 1, "stable counted TBQ workspace must retain its bill item");
            Require(counted.BuildUpRates.Count == 1, "stable counted TBQ workspace must retain its build-up rate");
            Require(counted.RateReferences.Edges.Count == 1, "stable counted TBQ workspace must retain its reference");
            Require(counted.Library.Entries.Count == 1, "stable counted TBQ workspace must retain its library entry");

            var streaming = Workspace(
                StreamBills(),
                StreamBuildUps(),
                StreamReferences(),
                StreamLibraryEntries());
            Require(streaming.BillItems.Count == 1, "pure streaming TBQ workspace must retain its bill item");
            Require(streaming.BuildUpRates.Count == 1, "pure streaming TBQ workspace must retain its build-up rate");
            Require(streaming.RateReferences.Edges.Count == 1, "pure streaming TBQ workspace must retain its reference");
            Require(streaming.Library.Entries.Count == 1, "pure streaming TBQ workspace must retain its library entry");
        }

        private static TbqProjectWorkspaceState Workspace(
            IEnumerable<TbqBillItem> bills,
            IEnumerable<BuildUpRateSnapshot> buildUps,
            IEnumerable<RateReferenceEdge> references,
            IEnumerable<BqLibraryEntry> libraryEntries)
        {
            return new TbqProjectWorkspaceState(
                "USD",
                1m,
                bills,
                buildUps,
                references,
                "LIB-TBQ",
                libraryEntries);
        }

        private static TbqBillItem Bill(string code) =>
            new TbqBillItem(code, "Description", "m", "Trade", 1m, 2m, "RATE-STABLE");

        private static BqLibraryEntry LibraryEntry(string code) =>
            new BqLibraryEntry(code, "Description", "m", "Category", 1m);

        private static IEnumerable<TbqBillItem> StableBills() => new[] { Bill("BILL-STABLE") };
        private static IEnumerable<BuildUpRateSnapshot> StableBuildUps() =>
            new[] { new BuildUpRateSnapshot("RATE-STABLE", 2m) };
        private static IEnumerable<RateReferenceEdge> StableReferences() =>
            new[] { new RateReferenceEdge("RATE-STABLE", RateReferenceTargetKind.BillItem, "BILL-STABLE") };
        private static IEnumerable<BqLibraryEntry> StableLibraryEntries() =>
            new[] { LibraryEntry("LIB-STABLE") };

        private static IEnumerable<TbqBillItem> StreamBills()
        {
            yield return Bill("BILL-STREAM");
        }

        private static IEnumerable<BuildUpRateSnapshot> StreamBuildUps()
        {
            yield return new BuildUpRateSnapshot("RATE-STREAM", 2m);
        }

        private static IEnumerable<RateReferenceEdge> StreamReferences()
        {
            yield return new RateReferenceEdge("RATE-STREAM", RateReferenceTargetKind.BillItem, "BILL-STREAM");
        }

        private static IEnumerable<BqLibraryEntry> StreamLibraryEntries()
        {
            yield return LibraryEntry("LIB-STREAM");
        }

        private static void ExpectCountFailure(Action action, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException(label + " was accepted unexpectedly.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class TransientCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T _item;
            private readonly int _transientCount;
            private bool _emitTransientCount;

            internal TransientCountCollection(T item, int transientCount)
            {
                _item = item;
                _transientCount = transientCount;
            }

            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    if (_emitTransientCount)
                    {
                        _emitTransientCount = false;
                        return _transientCount;
                    }
                    return 1;
                }
            }

            int IReadOnlyCollection<T>.Count => 1;
            int ICollection.Count => 1;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
            public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_item, index);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly TransientCountCollection<T> _owner;
                private int _state;

                internal Enumerator(TransientCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    if (_state != 0)
                    {
                        _state = 2;
                        return false;
                    }

                    _state = 1;
                    _owner._emitTransientCount = true;
                    return true;
                }

                public T Current
                {
                    get
                    {
                        if (_state != 1) throw new InvalidOperationException();
                        _owner.CurrentReads++;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class OverYieldCountCollection<T> : ICollection<T>
        {
            private readonly T[] _items;

            internal OverYieldCountCollection(params T[] items)
            {
                _items = items;
            }

            internal int CurrentReads { get; private set; }
            public int Count => 1;
            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly OverYieldCountCollection<T> _owner;
                private int _index = -1;

                internal Enumerator(OverYieldCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    if (_index + 1 >= _owner._items.Length)
                    {
                        _index = _owner._items.Length;
                        return false;
                    }
                    _index++;
                    return true;
                }

                public T Current
                {
                    get
                    {
                        if (_index < 0 || _index >= _owner._items.Length) throw new InvalidOperationException();
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
