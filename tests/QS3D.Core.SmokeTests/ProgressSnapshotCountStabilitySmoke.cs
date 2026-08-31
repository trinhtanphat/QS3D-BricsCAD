using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;
using QS3D.Core.Progress;

namespace QS3D.Core.SmokeTests
{
    internal static class ProgressSnapshotCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownCountOverrunStopsBeforeCurrentAndLaterTail();
            UnderYieldFailsClosed();
            TransientCountGrowthFailsBeforeCurrent();
            TransientCountShrinkFailsBeforeCurrent();
            TransientNegativeCountFailsBeforeCurrent();
            PostTraversalUniformCountDriftFailsClosed();
            PostTraversalSingleSurfaceConflictFailsClosed();
            StableCountedSourceAndStreamingSourceRemainSupported();
        }

        private static void KnownCountOverrunStopsBeforeCurrentAndLaterTail()
        {
            var source = new OverrunThenThrowMeasurements(Measurement("pm-a"), Measurement("pm-b"));
            ArgumentContains(() => Snapshot(source), "reported known count", "known Count overrun");
            Equal(2, source.MoveNextCalls, "overrun must stop at the first unexpected MoveNext");
            Equal(1, source.CurrentReads, "unexpected item must be rejected before Current is read");
        }

        private static void UnderYieldFailsClosed()
        {
            var source = new CountedMeasurements(2, 2, 2, new[] { Measurement("pm-a") });
            ArgumentContains(() => Snapshot(source), "reported known count", "under-yield mismatch");
        }

        private static void TransientCountGrowthFailsBeforeCurrent()
        {
            var source = new TransientCountMeasurements(
                new[] { Measurement("pm-a") },
                countByRead: read => read >= 3 ? 2 : 1);
            ArgumentContains(() => Snapshot(source), "known count changed during traversal", "transient Count growth");
            Equal(1, source.MoveNextCalls, "transient growth must be observed after the successful MoveNext");
            Equal(0, source.CurrentReads, "transient growth must fail before Current");
        }

        private static void TransientCountShrinkFailsBeforeCurrent()
        {
            var source = new TransientCountMeasurements(
                new[] { Measurement("pm-a"), Measurement("pm-b") },
                countByRead: read => read >= 3 ? 1 : 2);
            ArgumentContains(() => Snapshot(source), "known count changed during traversal", "transient Count shrink");
            Equal(1, source.MoveNextCalls, "transient shrink must be observed after the successful MoveNext");
            Equal(0, source.CurrentReads, "transient shrink must fail before Current");
        }

        private static void TransientNegativeCountFailsBeforeCurrent()
        {
            var source = new TransientCountMeasurements(
                new[] { Measurement("pm-a") },
                countByRead: read => read >= 3 ? -1 : 1);
            ArgumentContains(() => Snapshot(source), "negative known count", "transient negative Count");
            Equal(1, source.MoveNextCalls, "transient invalid Count must be observed after the successful MoveNext");
            Equal(0, source.CurrentReads, "transient invalid Count must fail before Current");
        }

        private static void PostTraversalUniformCountDriftFailsClosed()
        {
            var source = new CountedMeasurements(1, 1, 1, new[] { Measurement("pm-a") }, 2, 2, 2);
            ArgumentContains(() => Snapshot(source), "known count changed during traversal", "uniform Count drift");
            Equal(6, source.GenericCountReads, "generic Count must be rebound at every traversal boundary and publication");
            Equal(6, source.ReadOnlyCountReads, "read-only Count must be rebound at every traversal boundary and publication");
            Equal(6, source.NonGenericCountReads, "non-generic Count must be rebound at every traversal boundary and publication");
        }

        private static void PostTraversalSingleSurfaceConflictFailsClosed()
        {
            var source = new CountedMeasurements(1, 1, 1, new[] { Measurement("pm-a") }, 1, 2, 1);
            ArgumentContains(() => Snapshot(source), "conflicting known counts", "post-traversal Count conflict");
        }

        private static void StableCountedSourceAndStreamingSourceRemainSupported()
        {
            var counted = Snapshot(new CountedMeasurements(1, 1, 1, new[] { Measurement("pm-a") }));
            Equal(1, counted.Measurements.Count, "stable counted source");
            var streaming = Snapshot(Stream(Measurement("pm-b")));
            Equal(1, streaming.Measurements.Count, "streaming source");
        }

        private static ProgressSnapshot Snapshot(IEnumerable<ProgressMeasurement> measurements) =>
            new ProgressSnapshot(
                "ps-count-stability",
                1,
                new ProjectDate(2026, 8, 29),
                new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc),
                measurements);

        private static IEnumerable<ProgressMeasurement> Stream(ProgressMeasurement item)
        {
            yield return item;
        }

        private static ProgressMeasurement Measurement(string id)
        {
            var trace = new MeasurementTrace(
                id + "-semantic",
                "source-1",
                "NetVolumeM3",
                Array.Empty<MeasurementTraceFact>(),
                1d,
                Array.Empty<MeasurementTraceAdjustment>(),
                1d,
                "m3",
                "none",
                ruleId: "rule-count",
                ruleVersion: "1");
            return new ProgressMeasurement(id, new ProjectDate(2026, 8, 29), trace, 1m, 1m);
        }

        private static void ArgumentContains(Action action, string fragment, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new Exception(label + " produced wrong diagnostic: " + ex.Message);
            }
            throw new Exception(label + " expected ArgumentException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual)) throw new Exception(label + ": expected " + expected + ", got " + actual + ".");
        }

        private sealed class CountedMeasurements : ICollection<ProgressMeasurement>, IReadOnlyCollection<ProgressMeasurement>, ICollection
        {
            private readonly ProgressMeasurement[] _items;
            private readonly int _beforeGeneric;
            private readonly int _beforeReadOnly;
            private readonly int _beforeNonGeneric;
            private readonly int _afterGeneric;
            private readonly int _afterReadOnly;
            private readonly int _afterNonGeneric;
            private bool _traversed;

            internal CountedMeasurements(int generic, int readOnly, int nonGeneric, ProgressMeasurement[] items, int? postGeneric = null, int? postReadOnly = null, int? postNonGeneric = null)
            {
                _beforeGeneric = generic;
                _beforeReadOnly = readOnly;
                _beforeNonGeneric = nonGeneric;
                _afterGeneric = postGeneric ?? generic;
                _afterReadOnly = postReadOnly ?? readOnly;
                _afterNonGeneric = postNonGeneric ?? nonGeneric;
                _items = items;
            }

            public int GenericCountReads { get; private set; }
            public int ReadOnlyCountReads { get; private set; }
            public int NonGenericCountReads { get; private set; }

            int ICollection<ProgressMeasurement>.Count { get { GenericCountReads++; return _traversed ? _afterGeneric : _beforeGeneric; } }
            int IReadOnlyCollection<ProgressMeasurement>.Count { get { ReadOnlyCountReads++; return _traversed ? _afterReadOnly : _beforeReadOnly; } }
            int ICollection.Count { get { NonGenericCountReads++; return _traversed ? _afterNonGeneric : _beforeNonGeneric; } }
            bool ICollection<ProgressMeasurement>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ProgressMeasurement> GetEnumerator()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++) yield return _items[i];
                }
                finally
                {
                    _traversed = true;
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<ProgressMeasurement>.Add(ProgressMeasurement item) => throw new NotSupportedException();
            void ICollection<ProgressMeasurement>.Clear() => throw new NotSupportedException();
            bool ICollection<ProgressMeasurement>.Contains(ProgressMeasurement item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<ProgressMeasurement>.CopyTo(ProgressMeasurement[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<ProgressMeasurement>.Remove(ProgressMeasurement item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }

        private sealed class TransientCountMeasurements : IEnumerable<ProgressMeasurement>, IReadOnlyCollection<ProgressMeasurement>
        {
            private readonly ProgressMeasurement[] _items;
            private readonly Func<int, int> _countByRead;

            internal TransientCountMeasurements(ProgressMeasurement[] items, Func<int, int> countByRead)
            {
                _items = items;
                _countByRead = countByRead;
            }

            public int CountReads { get; private set; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count
            {
                get
                {
                    CountReads++;
                    return _countByRead(CountReads);
                }
            }

            public IEnumerator<ProgressMeasurement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProgressMeasurement>
            {
                private readonly TransientCountMeasurements _owner;
                private int _index = -1;
                internal Enumerator(TransientCountMeasurements owner) { _owner = owner; }
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Length;
                }
                public ProgressMeasurement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }
                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class OverrunThenThrowMeasurements : IEnumerable<ProgressMeasurement>, IReadOnlyCollection<ProgressMeasurement>
        {
            private readonly ProgressMeasurement _first;
            private readonly ProgressMeasurement _second;
            internal OverrunThenThrowMeasurements(ProgressMeasurement first, ProgressMeasurement second) { _first = first; _second = second; }
            public int Count => 1;
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public IEnumerator<ProgressMeasurement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProgressMeasurement>
            {
                private readonly OverrunThenThrowMeasurements _owner;
                private int _index = -1;
                internal Enumerator(OverrunThenThrowMeasurements owner) { _owner = owner; }
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index < 2) return true;
                    throw new InvalidOperationException("later tail must never win");
                }
                public ProgressMeasurement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _index == 0 ? _owner._first : _owner._second;
                    }
                }
                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
