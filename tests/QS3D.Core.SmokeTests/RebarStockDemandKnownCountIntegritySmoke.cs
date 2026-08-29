using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockDemandKnownCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectKnownCountOverrunBeforeUnexpectedCurrent();
            RejectKnownCountUnderYield();
            RejectPostTraversalCountDrift();
            RejectPostTraversalCountConflict();
            RejectPostTraversalNegativeCount();
            StableCountedInputRemainsAccepted();
        }

        private static void RejectKnownCountOverrunBeforeUnexpectedCurrent()
        {
            var source = Source(1, 1, 1, 1, 1, 1, Cut("C1"), Cut("C2"));
            ExpectInvalidOperation(() => CreateDemand(source), "yielded more entries");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentAccesses);
        }

        private static void RejectKnownCountUnderYield()
        {
            var source = Source(2, 2, 2, 2, 2, 2, Cut("C1"));
            ExpectInvalidOperation(() => CreateDemand(source), "yielded fewer entries");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentAccesses);
        }

        private static void RejectPostTraversalCountDrift()
        {
            var source = Source(1, 1, 1, 2, 2, 2, Cut("C1"));
            ExpectInvalidOperation(() => CreateDemand(source), "changed known Count");
            Equal(1, source.CurrentAccesses);
        }

        private static void RejectPostTraversalCountConflict()
        {
            var source = Source(1, 1, 1, 1, 2, 1, Cut("C1"));
            ExpectInvalidOperation(() => CreateDemand(source), "conflicting known Count values");
            Equal(1, source.CurrentAccesses);
        }

        private static void RejectPostTraversalNegativeCount()
        {
            var source = Source(1, 1, 1, -1, -1, -1, Cut("C1"));
            ExpectInvalidOperation(() => CreateDemand(source), "invalid negative known Count");
            Equal(1, source.CurrentAccesses);
        }

        private static void StableCountedInputRemainsAccepted()
        {
            var source = Source(1, 1, 1, 1, 1, 1, Cut("C1"));
            var demand = CreateDemand(source);
            Equal(1, demand.RequiredCuts.Count);
            Equal(2L, demand.RequiredCutCount);
            Near(4d, demand.RequiredCutLengthM, "required cut length");
            Equal(1, source.CurrentAccesses);
        }

        private static RebarCutRequirement Cut(string id) => new RebarCutRequirement(id, 2d, 2);

        private static RebarStockDemand CreateDemand(IReadOnlyList<RebarCutRequirement> cuts) =>
            new RebarStockDemand("GROUP-COUNT", "CB400-V", 16d, 11.7d, cuts, new RebarCutAllowancePolicy());

        private static MutableCountList<RebarCutRequirement> Source(
            int initialReadOnly,
            int initialGeneric,
            int initialNonGeneric,
            int postReadOnly,
            int postGeneric,
            int postNonGeneric,
            params RebarCutRequirement[] items) =>
            new MutableCountList<RebarCutRequirement>(
                initialReadOnly,
                initialGeneric,
                initialNonGeneric,
                postReadOnly,
                postGeneric,
                postNonGeneric,
                items);

        private static void ExpectInvalidOperation(Action action, string messageFragment)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(messageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Unexpected diagnostic: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException("Expected InvalidOperationException containing: " + messageFragment);
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new InvalidOperationException("Unexpected " + label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }

        private sealed class MutableCountList<T> : IReadOnlyList<T>, ICollection<T>, ICollection
        {
            private readonly IReadOnlyList<T> _items;
            private readonly int _initialReadOnly;
            private readonly int _initialGeneric;
            private readonly int _initialNonGeneric;
            private readonly int _postReadOnly;
            private readonly int _postGeneric;
            private readonly int _postNonGeneric;
            private bool _completed;

            internal MutableCountList(
                int initialReadOnly,
                int initialGeneric,
                int initialNonGeneric,
                int postReadOnly,
                int postGeneric,
                int postNonGeneric,
                params T[] items)
            {
                _initialReadOnly = initialReadOnly;
                _initialGeneric = initialGeneric;
                _initialNonGeneric = initialNonGeneric;
                _postReadOnly = postReadOnly;
                _postGeneric = postGeneric;
                _postNonGeneric = postNonGeneric;
                _items = items;
            }

            public int Count => _completed ? _postReadOnly : _initialReadOnly;
            int ICollection<T>.Count => _completed ? _postGeneric : _initialGeneric;
            int ICollection.Count => _completed ? _postNonGeneric : _initialNonGeneric;
            public T this[int index] => _items[index];
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentAccesses { get; private set; }

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
                private readonly MutableCountList<T> _owner;
                private int _index = -1;
                internal Enumerator(MutableCountList<T> owner) => _owner = owner;
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
                    if (next >= _owner._items.Count)
                    {
                        _owner._completed = true;
                        return false;
                    }
                    _index = next;
                    return true;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
