using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PhysicalOpeningCutTargetKnownCountSmoke
    {
        private const int MaxOpeningIds = 4096;

        public static void Run()
        {
            InvalidKnownCountsFailBeforeEnumeration();
            KnownCountTraversalMismatchFailsClosed();
            TransientCountDriftFailsAtCallerBoundaries();
            WriteMismatchDoesNotMutateHost();
            HonestCountedAndStreamingInputsRemainAccepted();
            ExactBoundRemainsAccepted();
            KnownOverYieldStopsBeforeUnexpectedCurrent();
            PureStreamingStillStopsAtBoundary();
        }

        private static void InvalidKnownCountsFailBeforeEnumeration()
        {
            var negative = new MultiCountCollection(new[] { "NEG" }, -1, -1, -1, throwOnEnumeration: true);
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Normalize(negative),
                "invalid negative opening id count",
                "Negative physical-opening target Count must fail before enumeration.");
            if (negative.EnumerationRequested)
                throw new Exception("Negative physical-opening target Count reached caller enumeration.");

            var oversized = new NonGenericCountEnumerable(MaxOpeningIds + 1);
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Normalize(oversized),
                "exceeds the 4096 opening id limit",
                "Oversized non-generic physical-opening target Count must fail before enumeration.");
            if (oversized.EnumerationRequested)
                throw new Exception("Oversized non-generic physical-opening target Count reached caller enumeration.");

            var conflicting = new MultiCountCollection(new[] { "CONFLICT" }, 1, 2, 1, throwOnEnumeration: true);
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Normalize(conflicting),
                "conflicting known opening id counts",
                "Conflicting physical-opening target Count contracts must fail before enumeration.");
            if (conflicting.EnumerationRequested)
                throw new Exception("Conflicting physical-opening target Counts reached caller enumeration.");
        }

        private static void KnownCountTraversalMismatchFailsClosed()
        {
            var under = new MultiCountCollection(new[] { "UNDER" }, 2, 2, 2, throwOnEnumeration: false);
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Normalize(under),
                "count changed during enumeration",
                "Physical-opening target under-enumeration must fail closed.");
            if (under.EnumerationRequestCount != 1)
                throw new Exception("Under-enumeration must inspect the physical-opening target source exactly once.");

            var over = new MultiCountCollection(new[] { "OVER-A", "OVER-B" }, 1, 1, 1, throwOnEnumeration: false);
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Normalize(over),
                "exceeds its advertised opening id Count",
                "Physical-opening target over-enumeration must fail before consuming unexpected Current.");
            if (over.EnumerationRequestCount != 1)
                throw new Exception("Over-enumeration must inspect the physical-opening target source exactly once.");
        }

        private static void TransientCountDriftFailsAtCallerBoundaries()
        {
            var moveNextDrift = new TransientCountDriftCollection("MOVE", driftAfterMoveNext: true, driftAfterCurrent: false);
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Normalize(moveNextDrift),
                "count changed during enumeration at after MoveNext",
                "Transient Count drift triggered by MoveNext must fail immediately.");
            if (moveNextDrift.CurrentReads != 0)
                throw new Exception("MoveNext-time Count drift must fail before caller Current is observed.");

            var currentDrift = new TransientCountDriftCollection("CURRENT", driftAfterMoveNext: false, driftAfterCurrent: true);
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Normalize(currentDrift),
                "count changed during enumeration at after Current",
                "Transient Count drift triggered by Current must fail before retention.");
            if (currentDrift.CurrentReads != 1)
                throw new Exception("Current-time Count drift must inspect caller Current exactly once before rejection.");
        }

        private static void WriteMismatchDoesNotMutateHost()
        {
            var host = new ProjectElement("HOST", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey] = "KEEP";
            var source = new MultiCountCollection(new[] { "OPENING" }, 2, 2, 2, throwOnEnumeration: false);

            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Write(host, source),
                "count changed during enumeration",
                "Write must reject Count/traversal mismatch before publishing physical-opening target-state.");

            if (!host.Properties.TryGetValue(PhysicalOpeningCutTargetStateCodec.OpeningIdsKey, out var retained) ||
                !string.Equals(retained, "KEEP", StringComparison.Ordinal))
                throw new Exception("Rejected physical-opening target serialization mutated the host property.");
        }

        private static void HonestCountedAndStreamingInputsRemainAccepted()
        {
            var counted = new MultiCountCollection(new[] { "B", "A" }, 2, 2, 2, throwOnEnumeration: false);
            var countedResult = PhysicalOpeningCutTargetStateCodec.Normalize(counted);
            if (countedResult.Count != 2 || countedResult[0] != "A" || countedResult[1] != "B")
                throw new Exception("Honest counted physical-opening targets lost canonical ordering.");

            var streamingResult = PhysicalOpeningCutTargetStateCodec.Normalize(Stream("B", "A"));
            if (streamingResult.Count != 2 || streamingResult[0] != "A" || streamingResult[1] != "B")
                throw new Exception("Pure streaming physical-opening targets must remain supported and canonically ordered.");
        }

        private static void ExactBoundRemainsAccepted()
        {
            var ids = new List<string>(MaxOpeningIds);
            for (var index = 0; index < MaxOpeningIds; index++)
                ids.Add("OPENING-" + index.ToString("D4"));

            var normalized = PhysicalOpeningCutTargetStateCodec.Normalize(ids);
            if (normalized.Count != MaxOpeningIds)
                throw new Exception("The exact 4,096 physical-opening target boundary must remain accepted.");
        }

        private static void KnownOverYieldStopsBeforeUnexpectedCurrent()
        {
            var source = new DishonestReadOnlyCollection(actualCount: 2, reportedCount: 1);
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Normalize(source),
                "exceeds its advertised opening id Count",
                "Known physical-opening target over-yield must fail before unexpected Current.");
            if (source.MoveNextCalls != 2 || source.CurrentReads != 1)
                throw new Exception("Known over-yield must stop on the second MoveNext before reading the second Current.");
        }

        private static void PureStreamingStillStopsAtBoundary()
        {
            var source = new StreamingEnumerable(MaxOpeningIds + 1);
            ExpectInvalidOperation(
                () => PhysicalOpeningCutTargetStateCodec.Normalize(source),
                "exceeds the 4096 opening id limit",
                "Pure-streaming physical-opening targets must remain bounded at item 4,097.");
            if (source.MoveNextCalls != MaxOpeningIds + 1 || source.CurrentReads != MaxOpeningIds)
                throw new Exception("Streaming bound must reject item 4,097 before reading its Current value.");
        }

        private static IEnumerable<string> Stream(params string[] values)
        {
            foreach (var value in values)
                yield return value;
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception(message + " Actual diagnostic: " + ex.Message);
                return;
            }

            throw new Exception(message);
        }

        private sealed class MultiCountCollection : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly string[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            public MultiCountCollection(string[] items, int genericCount, int readOnlyCount, int nonGenericCount, bool throwOnEnumeration)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            public bool EnumerationRequested { get; private set; }
            public int EnumerationRequestCount { get; private set; }
            int ICollection<string>.Count => _genericCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<string>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationRequested = true;
                EnumerationRequestCount++;
                if (_throwOnEnumeration) throw new Exception("Enumerator must not be requested.");
                return ((IEnumerable<string>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Contains(string item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<string>.CopyTo(string[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }

        private sealed class NonGenericCountEnumerable : IEnumerable<string>, ICollection
        {
            private readonly int _count;

            public NonGenericCountEnumerable(int count) { _count = count; }
            public bool EnumerationRequested { get; private set; }
            public int Count => _count;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationRequested = true;
                throw new Exception("Enumerator must not be requested for oversized known-count input.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class TransientCountDriftCollection : IReadOnlyCollection<string>
        {
            private readonly string _value;
            private readonly bool _driftAfterMoveNext;
            private readonly bool _driftAfterCurrent;
            private bool _driftArmed;

            public TransientCountDriftCollection(string value, bool driftAfterMoveNext, bool driftAfterCurrent)
            {
                _value = value;
                _driftAfterMoveNext = driftAfterMoveNext;
                _driftAfterCurrent = driftAfterCurrent;
            }

            public int Count
            {
                get
                {
                    if (!_driftArmed) return 1;
                    _driftArmed = false;
                    return 2;
                }
            }

            public int CurrentReads { get; private set; }
            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly TransientCountDriftCollection _owner;
                private int _state;

                public Enumerator(TransientCountDriftCollection owner) { _owner = owner; }
                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._driftAfterCurrent) _owner._driftArmed = true;
                        return _owner._value;
                    }
                }
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_state != 0) return false;
                    _state = 1;
                    if (_owner._driftAfterMoveNext) _owner._driftArmed = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class DishonestReadOnlyCollection : IReadOnlyCollection<string>
        {
            private readonly int _actualCount;
            private readonly int _reportedCount;

            public DishonestReadOnlyCollection(int actualCount, int reportedCount)
            {
                _actualCount = actualCount;
                _reportedCount = reportedCount;
            }

            public int Count => _reportedCount;
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly DishonestReadOnlyCollection _owner;
                private int _index = -1;

                public Enumerator(DishonestReadOnlyCollection owner) { _owner = owner; }
                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return "COUNTED-" + _index;
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

        private sealed class StreamingEnumerable : IEnumerable<string>
        {
            private readonly int _actualCount;
            public StreamingEnumerable(int actualCount) { _actualCount = actualCount; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly StreamingEnumerable _owner;
                private int _index = -1;

                public Enumerator(StreamingEnumerable owner) { _owner = owner; }
                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return "STREAM-" + _index.ToString("D4");
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