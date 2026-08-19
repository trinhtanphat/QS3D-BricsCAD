using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarScheduleKnownCountIntegritySmoke
    {
        private const int MaxRowCount = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectNegativeKnownCountBeforeEnumeration();
            RejectConflictingKnownCountsBeforeEnumeration();
            RejectOversizedKnownCountBeforeEnumeration();
            RejectKnownCountUnderEnumeration();
            RejectKnownCountOverEnumeration();
            AcceptConsistentKnownCounts();
            AcceptExactKnownBound();
            AcceptPureStreamingSource();
            PreserveStreamingRowBoundaryForDishonestCount();
        }

        private static void RejectNegativeKnownCountBeforeEnumeration()
        {
            var source = new MultiCountSource(Array.Empty<RebarScheduleInput>(), 0, -1, 0, throwOnEnumeration: true);
            ExpectInvalidOperation(
                () => RebarScheduleBuilder.Build(source),
                "invalid negative known Count",
                "Negative known rebar-schedule Count must fail closed before enumeration.");
            AssertNotEnumerated(source, "negative known Count");
        }

        private static void RejectConflictingKnownCountsBeforeEnumeration()
        {
            var source = new MultiCountSource(Array.Empty<RebarScheduleInput>(), 1, 2, 1, throwOnEnumeration: true);
            ExpectInvalidOperation(
                () => RebarScheduleBuilder.Build(source),
                "conflicting known Count values",
                "Conflicting rebar-schedule Count contracts must fail closed before enumeration.");
            AssertNotEnumerated(source, "conflicting known Counts");
        }

        private static void RejectOversizedKnownCountBeforeEnumeration()
        {
            var source = new MultiCountSource(Array.Empty<RebarScheduleInput>(), 1, 1, MaxRowCount + 1, throwOnEnumeration: true);
            ExpectArgumentOutOfRange(
                () => RebarScheduleBuilder.Build(source),
                "exceeds the supported row bound",
                "Oversized known rebar-schedule Count must fail before enumeration.");
            AssertNotEnumerated(source, "oversized known Count");
        }

        private static void RejectKnownCountUnderEnumeration()
        {
            var source = new MultiCountSource(
                new[] { ValidInput("UNDER-1") },
                2,
                2,
                2,
                throwOnEnumeration: false);
            ExpectInvalidOperation(
                () => RebarScheduleBuilder.Build(source),
                "known Count does not match traversal",
                "Rebar schedule must reject Count=2 when traversal yields one valid input.");
        }

        private static void RejectKnownCountOverEnumeration()
        {
            var source = new MultiCountSource(
                new[] { ValidInput("OVER-1"), ValidInput("OVER-2") },
                1,
                1,
                1,
                throwOnEnumeration: false);
            ExpectInvalidOperation(
                () => RebarScheduleBuilder.Build(source),
                "known Count does not match traversal",
                "Rebar schedule must reject Count=1 when traversal yields two valid inputs.");
        }

        private static void AcceptConsistentKnownCounts()
        {
            var source = new MultiCountSource(Array.Empty<RebarScheduleInput>(), 0, 0, 0, throwOnEnumeration: false);
            var rows = RebarScheduleBuilder.Build(source);
            if (!source.EnumeratorRequested)
                throw new InvalidOperationException("Consistent rebar-schedule Count contracts must reach enumeration.");
            if (rows.Count != 0)
                throw new InvalidOperationException("Consistent empty rebar-schedule input produced unexpected rows.");
        }

        private static void AcceptExactKnownBound()
        {
            var source = new DishonestReadOnlyCollection(MaxRowCount, reportedCount: MaxRowCount);
            var rows = RebarScheduleBuilder.Build(source);
            if (rows.Count != MaxRowCount)
                throw new InvalidOperationException("Honest exact-bound rebar-schedule source must produce exactly " + MaxRowCount + " rows.");
            if (source.MoveNextCalls != MaxRowCount + 1)
                throw new InvalidOperationException(
                    "Honest exact-bound rebar-schedule traversal must terminate after exactly one final MoveNext. MoveNext calls: " + source.MoveNextCalls + ".");
        }

        private static void AcceptPureStreamingSource()
        {
            var rows = RebarScheduleBuilder.Build(PureStreamingInputs());
            if (rows.Count != 2)
                throw new InvalidOperationException("Pure streaming rebar-schedule input without known Count metadata must remain supported.");
        }

        private static IEnumerable<RebarScheduleInput> PureStreamingInputs()
        {
            yield return ValidInput("PURE-1");
            yield return ValidInput("PURE-2");
        }

        private static void PreserveStreamingRowBoundaryForDishonestCount()
        {
            var source = new DishonestReadOnlyCollection(MaxRowCount + 1, reportedCount: 1);
            ExpectArgumentOutOfRange(
                () => RebarScheduleBuilder.Build(source),
                "exceeds the supported row bound",
                "Dishonest known Count must still stop at the existing streaming row boundary.");
            if (source.MoveNextCalls != MaxRowCount + 1)
                throw new InvalidOperationException(
                    "Rebar schedule streaming guard must stop after observing input 10,001 without requesting another item. MoveNext calls: " + source.MoveNextCalls + ".");
        }

        private static RebarScheduleInput ValidInput(string id)
        {
            return new RebarScheduleInput
            {
                ElementId = id,
                Notation = "1D8",
                CuttingLengthM = 1d
            };
        }

        private static void AssertNotEnumerated(MultiCountSource source, string label)
        {
            if (source.EnumeratorRequested)
                throw new InvalidOperationException("Rebar schedule enumerated input after " + label + " was already invalid from known Count contracts.");
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment, string failureMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(failureMessage + " Actual diagnostic: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException(failureMessage);
        }

        private static void ExpectArgumentOutOfRange(Action action, string expectedMessageFragment, string failureMessage)
        {
            try
            {
                action();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(failureMessage + " Actual diagnostic: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException(failureMessage);
        }

        private sealed class MultiCountSource : ICollection<RebarScheduleInput>, IReadOnlyCollection<RebarScheduleInput>, ICollection
        {
            private readonly RebarScheduleInput[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            internal MultiCountSource(
                RebarScheduleInput[] items,
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                bool throwOnEnumeration)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            internal bool EnumeratorRequested { get; private set; }
            int ICollection<RebarScheduleInput>.Count => _genericCount;
            int IReadOnlyCollection<RebarScheduleInput>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<RebarScheduleInput>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<RebarScheduleInput> GetEnumerator()
            {
                EnumeratorRequested = true;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Malformed known Count contracts must fail before rebar-schedule enumeration.");
                return ((IEnumerable<RebarScheduleInput>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<RebarScheduleInput>.Add(RebarScheduleInput item) => throw new NotSupportedException();
            void ICollection<RebarScheduleInput>.Clear() => throw new NotSupportedException();
            bool ICollection<RebarScheduleInput>.Contains(RebarScheduleInput item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<RebarScheduleInput>.CopyTo(RebarScheduleInput[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<RebarScheduleInput>.Remove(RebarScheduleInput item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }

        private sealed class DishonestReadOnlyCollection : IReadOnlyCollection<RebarScheduleInput>
        {
            private readonly int _actualCount;
            private readonly int _reportedCount;

            internal DishonestReadOnlyCollection(int actualCount, int reportedCount)
            {
                _actualCount = actualCount;
                _reportedCount = reportedCount;
            }

            public int Count => _reportedCount;
            internal int MoveNextCalls { get; private set; }
            public IEnumerator<RebarScheduleInput> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<RebarScheduleInput>
            {
                private readonly DishonestReadOnlyCollection _owner;
                private int _index = -1;

                internal Enumerator(DishonestReadOnlyCollection owner) { _owner = owner; }
                public RebarScheduleInput Current { get; private set; } = null!;
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index >= _owner._actualCount) return false;
                    Current = ValidInput("STREAM-" + _index);
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
