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
            RejectKnownCountOverEnumerationBeforeCurrent();
            RejectPostTraversalCountDrift();
            RejectPostTraversalNegativeCount();
            RejectPostTraversalCountConflict();
            AcceptConsistentKnownCounts();
            AcceptExactKnownBound();
            AcceptPureStreamingSource();
            PreserveStreamingRowBoundaryBeforeCurrent();
        }

        private static void RejectNegativeKnownCountBeforeEnumeration()
        {
            var source = new MultiCountSource(Array.Empty<RebarScheduleInput>(), 0, -1, 0, throwOnEnumeration: true);
            ExpectInvalidOperation(() => RebarScheduleBuilder.Build(source), "invalid negative known Count", "Negative known rebar-schedule Count must fail closed before enumeration.");
            AssertNotEnumerated(source, "negative known Count");
        }

        private static void RejectConflictingKnownCountsBeforeEnumeration()
        {
            var source = new MultiCountSource(Array.Empty<RebarScheduleInput>(), 1, 2, 1, throwOnEnumeration: true);
            ExpectInvalidOperation(() => RebarScheduleBuilder.Build(source), "conflicting known Count values", "Conflicting rebar-schedule Count contracts must fail closed before enumeration.");
            AssertNotEnumerated(source, "conflicting known Counts");
        }

        private static void RejectOversizedKnownCountBeforeEnumeration()
        {
            var source = new MultiCountSource(Array.Empty<RebarScheduleInput>(), 1, 1, MaxRowCount + 1, throwOnEnumeration: true);
            ExpectArgumentOutOfRange(() => RebarScheduleBuilder.Build(source), "exceeds the supported row bound", "Oversized known rebar-schedule Count must fail before enumeration.");
            AssertNotEnumerated(source, "oversized known Count");
        }

        private static void RejectKnownCountUnderEnumeration()
        {
            var source = new MultiCountSource(new[] { ValidInput("UNDER-1") }, 2, 2, 2, throwOnEnumeration: false);
            ExpectInvalidOperation(() => RebarScheduleBuilder.Build(source), "known Count does not match traversal", "Rebar schedule must reject Count=2 when traversal yields one valid input.");
        }

        private static void RejectKnownCountOverEnumerationBeforeCurrent()
        {
            var source = new CurrentCountingReadOnlyCollection(actualCount: 2, reportedCount: 1, throwOnUnexpectedCurrent: true);
            ExpectInvalidOperation(() => RebarScheduleBuilder.Build(source), "known Count does not match traversal", "Count=1 must reject the second item before reading IEnumerator.Current.");
            if (source.MoveNextCalls != 2)
                throw new InvalidOperationException("Known-Count overrun must stop on the N+1 MoveNext. MoveNext calls: " + source.MoveNextCalls + ".");
            if (source.CurrentReads != 1)
                throw new InvalidOperationException("Known-Count overrun read unexpected Current. Current reads: " + source.CurrentReads + ".");
        }

        private static void RejectPostTraversalCountDrift()
        {
            var source = new MultiCountSource(new[] { ValidInput("DRIFT-1") }, 1, 1, 1, false, finalGenericCount: 2, finalReadOnlyCount: 2, finalNonGenericCount: 2);
            ExpectInvalidOperation(() => RebarScheduleBuilder.Build(source), "changed during traversal", "Rebar schedule must reject deterministic Count drift after traversal.");
        }

        private static void RejectPostTraversalNegativeCount()
        {
            var source = new MultiCountSource(new[] { ValidInput("NEGATIVE-AFTER-1") }, 1, 1, 1, false, finalGenericCount: -1, finalReadOnlyCount: -1, finalNonGenericCount: -1);
            ExpectInvalidOperation(() => RebarScheduleBuilder.Build(source), "invalid negative known Count", "Rebar schedule must rebind and reject negative Count evidence after traversal.");
        }

        private static void RejectPostTraversalCountConflict()
        {
            var source = new MultiCountSource(new[] { ValidInput("CONFLICT-AFTER-1") }, 1, 1, 1, false, finalGenericCount: 1, finalReadOnlyCount: 2, finalNonGenericCount: 1);
            ExpectInvalidOperation(() => RebarScheduleBuilder.Build(source), "conflicting known Count values", "Rebar schedule must rebind and reject conflicting Count evidence after traversal.");
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
            var source = new CurrentCountingReadOnlyCollection(MaxRowCount, reportedCount: MaxRowCount, throwOnUnexpectedCurrent: false);
            var rows = RebarScheduleBuilder.Build(source);
            if (rows.Count != MaxRowCount)
                throw new InvalidOperationException("Honest exact-bound rebar-schedule source must produce exactly " + MaxRowCount + " rows.");
            if (source.MoveNextCalls != MaxRowCount + 1 || source.CurrentReads != MaxRowCount)
                throw new InvalidOperationException("Honest exact-bound traversal consumed unexpected enumerator state.");
        }

        private static void AcceptPureStreamingSource()
        {
            var rows = RebarScheduleBuilder.Build(PureStreamingInputs(2));
            if (rows.Count != 2)
                throw new InvalidOperationException("Pure streaming rebar-schedule input without known Count metadata must remain supported.");
        }

        private static void PreserveStreamingRowBoundaryBeforeCurrent()
        {
            var source = new CurrentCountingStreamingSource(MaxRowCount + 1, throwOnUnexpectedCurrent: true);
            ExpectArgumentOutOfRange(() => RebarScheduleBuilder.Build(source), "exceeds the supported row bound", "Pure streaming input must retain the independent 10,000-row boundary.");
            if (source.MoveNextCalls != MaxRowCount + 1)
                throw new InvalidOperationException("Streaming guard must stop on MoveNext 10,001. MoveNext calls: " + source.MoveNextCalls + ".");
            if (source.CurrentReads != MaxRowCount)
                throw new InvalidOperationException("Streaming row-bound guard read Current 10,001. Current reads: " + source.CurrentReads + ".");
        }

        private static IEnumerable<RebarScheduleInput> PureStreamingInputs(int count)
        {
            for (var index = 0; index < count; index++)
                yield return ValidInput("PURE-" + index);
        }

        private static RebarScheduleInput ValidInput(string id)
        {
            return new RebarScheduleInput { ElementId = id, Notation = "1D8", CuttingLengthM = 1d };
        }

        private static void AssertNotEnumerated(MultiCountSource source, string label)
        {
            if (source.EnumeratorRequested)
                throw new InvalidOperationException("Rebar schedule enumerated input after " + label + " was already invalid from known Count contracts.");
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment, string failureMessage)
        {
            try { action(); }
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
            try { action(); }
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
            private readonly int? _finalGenericCount;
            private readonly int? _finalReadOnlyCount;
            private readonly int? _finalNonGenericCount;

            internal MultiCountSource(RebarScheduleInput[] items, int genericCount, int readOnlyCount, int nonGenericCount, bool throwOnEnumeration, int? finalGenericCount = null, int? finalReadOnlyCount = null, int? finalNonGenericCount = null)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
                _finalGenericCount = finalGenericCount;
                _finalReadOnlyCount = finalReadOnlyCount;
                _finalNonGenericCount = finalNonGenericCount;
            }

            internal bool EnumeratorRequested { get; private set; }
            internal bool TraversalCompleted { get; private set; }
            int ICollection<RebarScheduleInput>.Count => TraversalCompleted && _finalGenericCount.HasValue ? _finalGenericCount.Value : _genericCount;
            int IReadOnlyCollection<RebarScheduleInput>.Count => TraversalCompleted && _finalReadOnlyCount.HasValue ? _finalReadOnlyCount.Value : _readOnlyCount;
            int ICollection.Count => TraversalCompleted && _finalNonGenericCount.HasValue ? _finalNonGenericCount.Value : _nonGenericCount;
            bool ICollection<RebarScheduleInput>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<RebarScheduleInput> GetEnumerator()
            {
                EnumeratorRequested = true;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Malformed known Count contracts must fail before rebar-schedule enumeration.");
                return new CompletionEnumerator(this);
            }

            private sealed class CompletionEnumerator : IEnumerator<RebarScheduleInput>
            {
                private readonly MultiCountSource _owner;
                private int _index = -1;
                internal CompletionEnumerator(MultiCountSource owner) { _owner = owner; }
                public RebarScheduleInput Current => _owner._items[_index];
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _index++;
                    if (_index < _owner._items.Length) return true;
                    _owner.TraversalCompleted = true;
                    return false;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<RebarScheduleInput>.Add(RebarScheduleInput item) => throw new NotSupportedException();
            void ICollection<RebarScheduleInput>.Clear() => throw new NotSupportedException();
            bool ICollection<RebarScheduleInput>.Contains(RebarScheduleInput item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<RebarScheduleInput>.CopyTo(RebarScheduleInput[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<RebarScheduleInput>.Remove(RebarScheduleInput item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }

        private sealed class CurrentCountingReadOnlyCollection : IReadOnlyCollection<RebarScheduleInput>
        {
            private readonly int _actualCount;
            private readonly bool _throwOnUnexpectedCurrent;
            internal CurrentCountingReadOnlyCollection(int actualCount, int reportedCount, bool throwOnUnexpectedCurrent)
            {
                _actualCount = actualCount;
                Count = reportedCount;
                _throwOnUnexpectedCurrent = throwOnUnexpectedCurrent;
            }
            public int Count { get; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<RebarScheduleInput> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<RebarScheduleInput>
            {
                private readonly CurrentCountingReadOnlyCollection _owner;
                private int _index = -1;
                internal Enumerator(CurrentCountingReadOnlyCollection owner) { _owner = owner; }
                public RebarScheduleInput Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._throwOnUnexpectedCurrent && _owner.CurrentReads > _owner.Count)
                            throw new InvalidOperationException("Unexpected rebar-schedule Current read beyond admitted Count.");
                        return ValidInput("COUNTED-" + _index);
                    }
                }
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._actualCount;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class CurrentCountingStreamingSource : IEnumerable<RebarScheduleInput>
        {
            private readonly int _actualCount;
            private readonly bool _throwOnUnexpectedCurrent;
            internal CurrentCountingStreamingSource(int actualCount, bool throwOnUnexpectedCurrent)
            {
                _actualCount = actualCount;
                _throwOnUnexpectedCurrent = throwOnUnexpectedCurrent;
            }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<RebarScheduleInput> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<RebarScheduleInput>
            {
                private readonly CurrentCountingStreamingSource _owner;
                private int _index = -1;
                internal Enumerator(CurrentCountingStreamingSource owner) { _owner = owner; }
                public RebarScheduleInput Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._throwOnUnexpectedCurrent && _owner.CurrentReads > MaxRowCount)
                            throw new InvalidOperationException("Unexpected streaming Current read beyond rebar row bound.");
                        return ValidInput("STREAM-" + _index);
                    }
                }
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._actualCount;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
