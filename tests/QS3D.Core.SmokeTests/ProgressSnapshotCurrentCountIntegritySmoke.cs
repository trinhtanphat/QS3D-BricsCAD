using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;
using QS3D.Core.Progress;

namespace QS3D.Core.SmokeTests
{
    internal static class ProgressSnapshotCurrentCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CurrentCountDriftPreemptsNullValidation();
            StableCountedAndStreamingSourcesRemainSupported();
        }

        private static void CurrentCountDriftPreemptsNullValidation()
        {
            var source = new CurrentDriftMeasurements(admittedCount: 1, driftedCount: 2);

            ArgumentContains(
                () => Snapshot(source),
                "known count changed during traversal",
                "Current-induced Count drift");

            Equal(1, source.MoveNextCalls, "Current drift MoveNext calls");
            Equal(1, source.CurrentReads, "Current drift Current reads");
        }

        private static void StableCountedAndStreamingSourcesRemainSupported()
        {
            var counted = Snapshot(new StableCountedMeasurements(Measurement("pm-current-counted")));
            Equal(1, counted.Measurements.Count, "stable counted source");

            var streaming = Snapshot(Stream(Measurement("pm-current-stream")));
            Equal(1, streaming.Measurements.Count, "streaming source");
        }

        private static ProgressSnapshot Snapshot(IEnumerable<ProgressMeasurement> measurements) =>
            new ProgressSnapshot(
                "ps-current-count-integrity",
                1,
                new ProjectDate(2026, 8, 31),
                new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
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
                ruleId: "rule-current-count",
                ruleVersion: "1");
            return new ProgressMeasurement(id, new ProjectDate(2026, 8, 31), trace, 1m, 1m);
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
            if (!Equals(expected, actual))
                throw new Exception(label + ": expected " + expected + ", got " + actual + ".");
        }

        private sealed class CurrentDriftMeasurements : IEnumerable<ProgressMeasurement>, IReadOnlyCollection<ProgressMeasurement>
        {
            private readonly int _driftedCount;
            private int _count;

            internal CurrentDriftMeasurements(int admittedCount, int driftedCount)
            {
                _count = admittedCount;
                _driftedCount = driftedCount;
            }

            public int Count => _count;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<ProgressMeasurement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProgressMeasurement>
            {
                private readonly CurrentDriftMeasurements _owner;
                private bool _moved;

                internal Enumerator(CurrentDriftMeasurements owner) => _owner = owner;

                public ProgressMeasurement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._count = _owner._driftedCount;
                        return null!;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StableCountedMeasurements : IEnumerable<ProgressMeasurement>, IReadOnlyCollection<ProgressMeasurement>
        {
            private readonly ProgressMeasurement _item;

            internal StableCountedMeasurements(ProgressMeasurement item) => _item = item;

            public int Count => 1;
            public IEnumerator<ProgressMeasurement> GetEnumerator()
            {
                yield return _item;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
