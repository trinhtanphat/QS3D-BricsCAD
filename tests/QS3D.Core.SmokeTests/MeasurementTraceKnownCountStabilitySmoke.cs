using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTraceKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            EnumeratorAcquisitionDriftFailsBeforeTraversal();
            MoveNextDriftFailsBeforeCurrent();
            CurrentDriftFailsBeforeFurtherTraversal();
            AdjustmentAndMessageSurfacesUseTheSameBoundary();
            StableCountedAndStreamingControlsRemainAccepted();
        }

        private static void EnumeratorAcquisitionDriftFailsBeforeTraversal()
        {
            var source = new CountDriftCollection<MeasurementTraceFact>(
                new MeasurementTraceFact("GrossAreaM2", 1d, "m2", "SRC-WALL"),
                DriftStage.GetEnumerator);

            Throws<ArgumentException>(() => CreateTrace(source, Array.Empty<MeasurementTraceAdjustment>()));
            Equal(0, source.MoveNextCalls, "Enumerator-acquisition Count drift must be rejected before the first MoveNext call.");
            Equal(0, source.CurrentCalls, "Enumerator-acquisition Count drift must be rejected before Current is observed.");
        }

        private static void MoveNextDriftFailsBeforeCurrent()
        {
            var source = new CountDriftCollection<MeasurementTraceFact>(
                new MeasurementTraceFact("GrossAreaM2", 1d, "m2", "SRC-WALL"),
                DriftStage.MoveNext);

            Throws<ArgumentException>(() => CreateTrace(source, Array.Empty<MeasurementTraceAdjustment>()));
            Equal(1, source.MoveNextCalls, "MoveNext-induced Count drift must fail on the first traversal call.");
            Equal(0, source.CurrentCalls, "MoveNext-induced Count drift must be rejected before Current is observed.");
        }

        private static void CurrentDriftFailsBeforeFurtherTraversal()
        {
            var source = new CountDriftCollection<MeasurementTraceFact>(
                new MeasurementTraceFact("GrossAreaM2", 1d, "m2", "SRC-WALL"),
                DriftStage.Current);

            Throws<ArgumentException>(() => CreateTrace(source, Array.Empty<MeasurementTraceAdjustment>()));
            Equal(1, source.MoveNextCalls, "Current-induced Count drift must fail before a second MoveNext can occur.");
            Equal(1, source.CurrentCalls, "Current-induced Count drift should be detected immediately after the hostile Current read.");
        }

        private static void AdjustmentAndMessageSurfacesUseTheSameBoundary()
        {
            var adjustments = new CountDriftCollection<MeasurementTraceAdjustment>(
                new MeasurementTraceAdjustment(
                    MeasurementTraceAdjustmentKind.Addition,
                    0d,
                    "m2",
                    "zero-control",
                    "SRC-ADJ"),
                DriftStage.GetEnumerator);

            Throws<ArgumentException>(() => CreateTrace(Array.Empty<MeasurementTraceFact>(), adjustments));
            Equal(0, adjustments.MoveNextCalls, "Adjustment Count drift during enumerator acquisition must fail before traversal.");

            var warnings = new CountDriftCollection<string>("source-present", DriftStage.GetEnumerator);
            Throws<ArgumentException>(() => CreateTrace(
                Array.Empty<MeasurementTraceFact>(),
                Array.Empty<MeasurementTraceAdjustment>(),
                warnings));
            Equal(0, warnings.MoveNextCalls, "Message Count drift during enumerator acquisition must fail before traversal.");
        }

        private static void StableCountedAndStreamingControlsRemainAccepted()
        {
            var stableFacts = new CountDriftCollection<MeasurementTraceFact>(
                new MeasurementTraceFact("GrossAreaM2", 1d, "m2", "SRC-WALL"),
                DriftStage.None);
            var stable = CreateTrace(stableFacts, Array.Empty<MeasurementTraceAdjustment>());
            Equal(1, stable.InputFacts.Count, "Stable counted facts must remain accepted.");

            var streaming = CreateTrace(
                Stream(new MeasurementTraceFact("GrossAreaM2", 1d, "m2", "SRC-WALL")),
                Stream<MeasurementTraceAdjustment>(),
                Stream("source-present"));
            Equal(1, streaming.InputFacts.Count, "Unknown-count streaming facts must remain accepted.");
            Equal(1, streaming.Warnings.Count, "Unknown-count streaming messages must remain accepted.");
        }

        private static MeasurementTrace CreateTrace(
            IEnumerable<MeasurementTraceFact> facts,
            IEnumerable<MeasurementTraceAdjustment> adjustments,
            IEnumerable<string>? warnings = null)
        {
            return new MeasurementTrace(
                "SEM-WALL-COUNT",
                "SRC-WALL",
                "NetAreaM2",
                facts,
                1d,
                adjustments,
                1d,
                "m2",
                "none",
                warnings);
        }

        private static IEnumerable<T> Stream<T>(params T[] values)
        {
            for (var i = 0; i < values.Length; i++) yield return values[i];
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private enum DriftStage
        {
            None,
            GetEnumerator,
            MoveNext,
            Current
        }

        private sealed class CountDriftCollection<T> : ICollection<T>
        {
            private readonly List<T> _items;
            private readonly DriftStage _stage;
            private int _reportedCount;

            internal CountDriftCollection(T item, DriftStage stage)
            {
                _items = new List<T> { item };
                _stage = stage;
                _reportedCount = _items.Count;
            }

            public int Count => _reportedCount;
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                if (_stage == DriftStage.GetEnumerator)
                    _reportedCount = _items.Count + 1;
                return new HostileEnumerator(this, _items.GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Contains(T item) => _items.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class HostileEnumerator : IEnumerator<T>
            {
                private readonly CountDriftCollection<T> _owner;
                private readonly IEnumerator<T> _inner;

                internal HostileEnumerator(CountDriftCollection<T> owner, IEnumerator<T> inner)
                {
                    _owner = owner;
                    _inner = inner;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentCalls++;
                        if (_owner._stage == DriftStage.Current)
                            _owner._reportedCount = _owner._items.Count + 1;
                        return _inner.Current;
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_owner._stage == DriftStage.MoveNext)
                        _owner._reportedCount = _owner._items.Count + 1;
                    return _inner.MoveNext();
                }

                public void Reset() => _inner.Reset();
                public void Dispose() => _inner.Dispose();
            }
        }
    }
}
