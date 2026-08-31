using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementSnapshotKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            TransientMoveNextCountDriftFailsBeforeCurrent();
            TransientCurrentCountDriftFailsBeforeRetention();
            CountDriftAfterExactTraversalFailsClosed();
            NegativeCountAfterExactTraversalFailsClosed();
            MultiInterfaceConflictAfterTraversalFailsClosed();
            StableMultiInterfaceCountRemainsAccepted();
            StableReadOnlyCountIsReboundAroundTraversal();
            KnownCountOverrunWinsBeforeInvalidExtraTrace();
            KnownCountUnderYieldStillFailsClosed();
            PureStreamingSourceRemainsAccepted();
        }

        private static void TransientMoveNextCountDriftFailsBeforeCurrent()
        {
            var source = new TransientMoveNextReadOnlyCollection<MeasurementTrace>(
                new[] { Trace("A") },
                declaredCount: 1);

            var error = Capture<ArgumentException>(() => new MeasurementSnapshot(source));
            Contains("count changed during enumeration", error.Message, "MoveNext-induced Count drift must fail closed.");
            Equal(3, source.CountReads, "MoveNext drift must be observed immediately after the callback.");
            Equal(0, source.CurrentReads, "MoveNext drift must fail before Current is read.");
        }

        private static void TransientCurrentCountDriftFailsBeforeRetention()
        {
            var source = new TransientCurrentReadOnlyCollection<MeasurementTrace>(
                new[] { Trace("A") },
                declaredCount: 1);

            var error = Capture<ArgumentException>(() => new MeasurementSnapshot(source));
            Contains("count changed during enumeration", error.Message, "Current-induced Count drift must fail closed.");
            Equal(4, source.CountReads, "Current drift must be observed immediately after the callback.");
            Equal(1, source.CurrentReads, "Current may be read once but its trace must not be retained before Count rebound.");
        }

        private static void CountDriftAfterExactTraversalFailsClosed()
        {
            var source = new PhaseReadOnlyCollection<MeasurementTrace>(
                new[] { Trace("A"), Trace("B") },
                initialCount: 2,
                finalCount: 1);

            var error = Capture<ArgumentException>(() => new MeasurementSnapshot(source));
            Equal(9, source.CountReads, "Measurement snapshot must re-read deterministic Count around traversal callbacks.");
            Contains("count changed during enumeration", error.Message, "Post-traversal Count drift must fail closed.");
        }

        private static void NegativeCountAfterExactTraversalFailsClosed()
        {
            var source = new PhaseReadOnlyCollection<MeasurementTrace>(
                new[] { Trace("A") },
                initialCount: 1,
                finalCount: -1);

            var error = Capture<ArgumentException>(() => new MeasurementSnapshot(source));
            Equal(6, source.CountReads, "Post-traversal negative Count must be observed at the terminal MoveNext rebound.");
            Contains("count cannot be negative", error.Message, "Negative post-traversal Count must fail closed explicitly.");
        }

        private static void MultiInterfaceConflictAfterTraversalFailsClosed()
        {
            var source = new PhaseMultiCollection<MeasurementTrace>(
                new[] { Trace("A"), Trace("B") },
                initialCount: 2,
                finalCollectionCount: 2,
                finalReadOnlyCount: 3,
                finalNonGenericCount: 2);

            var error = Capture<ArgumentException>(() => new MeasurementSnapshot(source));
            Contains("count contracts disagree", error.Message, "Conflicting post-traversal Count surfaces must fail closed.");
            Equal(9, source.GenericCountReads, "Generic Count must be inspected around every traversal callback.");
            Equal(9, source.ReadOnlyCountReads, "Read-only Count must expose the terminal conflict immediately.");
            Equal(8, source.NonGenericCountReads, "Terminal conflict must fail before the final non-generic Count is trusted.");
        }

        private static void StableMultiInterfaceCountRemainsAccepted()
        {
            var source = new PhaseMultiCollection<MeasurementTrace>(
                new[] { Trace("B"), Trace("A") },
                initialCount: 2,
                finalCollectionCount: 2,
                finalReadOnlyCount: 2,
                finalNonGenericCount: 2);

            var snapshot = new MeasurementSnapshot(source);
            Equal(2, snapshot.Traces.Count, "Stable multi-interface sources must remain accepted.");
            Equal("A", snapshot.Traces[0].SemanticIdentity, "Canonical ordering must remain ordinal after Count hardening.");
            Equal("B", snapshot.Traces[1].SemanticIdentity, "Canonical ordering must remain deterministic.");
            Equal(10, source.GenericCountReads, "Generic Count must be rebound around traversal and before publication.");
            Equal(10, source.ReadOnlyCountReads, "Read-only Count must be rebound around traversal and before publication.");
            Equal(10, source.NonGenericCountReads, "Non-generic Count must be rebound around traversal and before publication.");
        }

        private static void StableReadOnlyCountIsReboundAroundTraversal()
        {
            var source = new PhaseReadOnlyCollection<MeasurementTrace>(
                new[] { Trace("A") },
                initialCount: 1,
                finalCount: 1);

            var snapshot = new MeasurementSnapshot(source);
            Equal(1, snapshot.Traces.Count, "Stable counted source must remain accepted.");
            Equal(7, source.CountReads, "Stable deterministic Count must be read at admission, around traversal callbacks, and before publication.");
        }

        private static void KnownCountOverrunWinsBeforeInvalidExtraTrace()
        {
            var source = new FixedReadOnlyCollection<MeasurementTrace?>(
                new MeasurementTrace?[] { Trace("A"), null },
                declaredCount: 1);

            var error = Capture<ArgumentException>(() => new MeasurementSnapshot(CastNullable(source)));
            Contains("count changed during enumeration", error.Message, "Known Count overrun must win before null-entry validation for the extra trace.");
            DoesNotContain("null entries", error.Message, "Extra trace payload validation must not outrank admitted cardinality.");
        }

        private static void KnownCountUnderYieldStillFailsClosed()
        {
            var source = new FixedReadOnlyCollection<MeasurementTrace>(
                new[] { Trace("A") },
                declaredCount: 2);

            var error = Capture<ArgumentException>(() => new MeasurementSnapshot(source));
            Contains("count changed during enumeration", error.Message, "Known Count under-yield must remain rejected.");
        }

        private static void PureStreamingSourceRemainsAccepted()
        {
            var snapshot = new MeasurementSnapshot(Stream(Trace("B"), Trace("A")));
            Equal(2, snapshot.Traces.Count, "Pure streaming sources must remain supported.");
            Equal("A", snapshot.Traces[0].SemanticIdentity, "Streaming source must retain canonical ordering.");
            Equal("B", snapshot.Traces[1].SemanticIdentity, "Streaming source must retain canonical ordering.");
        }

        private static MeasurementTrace Trace(string id)
        {
            return new MeasurementTrace(
                id,
                "SRC-" + id,
                "volume",
                Array.Empty<MeasurementTraceFact>(),
                1d,
                Array.Empty<MeasurementTraceAdjustment>(),
                1d,
                "m3",
                "none");
        }

        private static IEnumerable<MeasurementTrace> Stream(params MeasurementTrace[] traces)
        {
            for (var i = 0; i < traces.Length; i++) yield return traces[i];
        }

        private static IEnumerable<MeasurementTrace> CastNullable(IEnumerable<MeasurementTrace?> source)
        {
            return new NullablePassThrough(source);
        }

        private sealed class NullablePassThrough : IEnumerable<MeasurementTrace>, IReadOnlyCollection<MeasurementTrace>
        {
            private readonly IEnumerable<MeasurementTrace?> _source;

            internal NullablePassThrough(IEnumerable<MeasurementTrace?> source)
            {
                _source = source;
            }

            public int Count => _source is IReadOnlyCollection<MeasurementTrace?> counted ? counted.Count : 0;

            public IEnumerator<MeasurementTrace> GetEnumerator()
            {
                foreach (var item in _source) yield return item!;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class TransientMoveNextReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _declaredCount;
            private bool _drifted;

            internal TransientMoveNextReadOnlyCollection(T[] items, int declaredCount)
            {
                _items = items;
                _declaredCount = declaredCount;
            }

            internal int CountReads { get; private set; }
            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _drifted ? _declaredCount + 1 : _declaredCount;
                }
            }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly TransientMoveNextReadOnlyCollection<T> _owner;
                private int _index = -1;

                internal Enumerator(TransientMoveNextReadOnlyCollection<T> owner)
                {
                    _owner = owner;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._drifted = false;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    if (_index + 1 >= _owner._items.Length)
                    {
                        _owner._drifted = false;
                        return false;
                    }

                    _index++;
                    _owner._drifted = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class TransientCurrentReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _declaredCount;
            private bool _drifted;

            internal TransientCurrentReadOnlyCollection(T[] items, int declaredCount)
            {
                _items = items;
                _declaredCount = declaredCount;
            }

            internal int CountReads { get; private set; }
            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _drifted ? _declaredCount + 1 : _declaredCount;
                }
            }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly TransientCurrentReadOnlyCollection<T> _owner;
                private int _index = -1;

                internal Enumerator(TransientCurrentReadOnlyCollection<T> owner)
                {
                    _owner = owner;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._drifted = true;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner._drifted = false;
                    if (_index + 1 >= _owner._items.Length) return false;
                    _index++;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class PhaseReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private readonly int _finalCount;
            private bool _enumerated;

            internal PhaseReadOnlyCollection(T[] items, int initialCount, int finalCount)
            {
                _items = items;
                _initialCount = initialCount;
                _finalCount = finalCount;
            }

            internal int CountReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _enumerated ? _finalCount : _initialCount;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++) yield return _items[i];
                }
                finally
                {
                    _enumerated = true;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class PhaseMultiCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private readonly int _finalCollectionCount;
            private readonly int _finalReadOnlyCount;
            private readonly int _finalNonGenericCount;
            private bool _enumerated;

            internal PhaseMultiCollection(
                T[] items,
                int initialCount,
                int finalCollectionCount,
                int finalReadOnlyCount,
                int finalNonGenericCount)
            {
                _items = items;
                _initialCount = initialCount;
                _finalCollectionCount = finalCollectionCount;
                _finalReadOnlyCount = finalReadOnlyCount;
                _finalNonGenericCount = finalNonGenericCount;
            }

            internal int GenericCountReads { get; private set; }
            internal int ReadOnlyCountReads { get; private set; }
            internal int NonGenericCountReads { get; private set; }

            int ICollection<T>.Count
            {
                get
                {
                    GenericCountReads++;
                    return _enumerated ? _finalCollectionCount : _initialCount;
                }
            }

            int IReadOnlyCollection<T>.Count
            {
                get
                {
                    ReadOnlyCountReads++;
                    return _enumerated ? _finalReadOnlyCount : _initialCount;
                }
            }

            int ICollection.Count
            {
                get
                {
                    NonGenericCountReads++;
                    return _enumerated ? _finalNonGenericCount : _initialCount;
                }
            }

            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++) yield return _items[i];
                }
                finally
                {
                    _enumerated = true;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            bool ICollection<T>.Contains(T item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
        }

        private sealed class FixedReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _declaredCount;

            internal FixedReadOnlyCollection(T[] items, int declaredCount)
            {
                _items = items;
                _declaredCount = declaredCount;
            }

            public int Count => _declaredCount;
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + " was not thrown.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + " Actual=" + actual + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Message=" + actual);
        }

        private static void DoesNotContain(string unexpected, string actual, string message)
        {
            if (actual.IndexOf(unexpected, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException(message + " Message=" + actual);
        }
    }

    internal static class MeasurementSnapshotKnownCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasurementSnapshotKnownCountStabilitySmoke.Run();
        }
    }
}
