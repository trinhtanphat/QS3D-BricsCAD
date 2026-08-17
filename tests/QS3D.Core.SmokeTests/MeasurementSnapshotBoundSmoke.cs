using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementSnapshotBoundSmoke
    {
        private const int MaximumTraces = 10000;

        internal static void Run()
        {
            CountedOversizeFailsBeforeEnumeration();
            NonGenericCountedOversizeFailsBeforeEnumeration();
            StreamingOversizeStopsAtFirstDisallowedTrace();
            ExactBoundaryRemainsAccepted();
            CanonicalOrderingAndValidationRemainStable();
        }

        private static void CountedOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated(MaximumTraces + 1);
            var error = Capture<ArgumentException>(() => new MeasurementSnapshot(source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted snapshot input must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted snapshot oversize failure must report the trace bound.");
        }

        private static void NonGenericCountedOversizeFailsBeforeEnumeration()
        {
            var source = new NonGenericCountedNeverEnumerated(MaximumTraces + 1);
            var error = Capture<ArgumentException>(() => new MeasurementSnapshot(source));

            Equal(0, source.GetEnumeratorCalls, "Oversized non-generic ICollection snapshot input must fail before enumeration.");
            Contains("at most 10000", error.Message, "Non-generic counted snapshot oversize failure must report the trace bound.");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedTrace()
        {
            var source = new StreamingTraces(MaximumTraces + 2);
            var error = Capture<ArgumentException>(() => new MeasurementSnapshot(source));

            Equal(
                MaximumTraces + 1,
                source.YieldedCount,
                "Streaming snapshot ingestion must stop immediately after observing trace 10,001.");
            Contains("at most 10000", error.Message, "Streaming snapshot oversize failure must report the trace bound.");
        }

        private static void ExactBoundaryRemainsAccepted()
        {
            var traces = new MeasurementTrace[MaximumTraces];
            for (var i = 0; i < traces.Length; i++)
                traces[i] = Trace(i);

            var snapshot = new MeasurementSnapshot(traces);
            Equal(MaximumTraces, snapshot.Traces.Count, "Measurement snapshot must accept exactly 10,000 traces.");
            Equal("SEM-00000", snapshot.Traces[0].SemanticIdentity, "Boundary snapshot first identity changed.");
            Equal("SEM-09999", snapshot.Traces[snapshot.Traces.Count - 1].SemanticIdentity, "Boundary snapshot final identity changed.");
        }

        private static void CanonicalOrderingAndValidationRemainStable()
        {
            var snapshot = new MeasurementSnapshot(new[] { Trace(2), Trace(0), Trace(1) });
            Equal("SEM-00000", snapshot.Traces[0].SemanticIdentity, "Snapshot canonical ordering changed at first trace.");
            Equal("SEM-00001", snapshot.Traces[1].SemanticIdentity, "Snapshot canonical ordering changed at second trace.");
            Equal("SEM-00002", snapshot.Traces[2].SemanticIdentity, "Snapshot canonical ordering changed at third trace.");

            Capture<ArgumentException>(() => new MeasurementSnapshot(new[] { Trace(7), Trace(7) }));
            Capture<ArgumentException>(() => new MeasurementSnapshot(new MeasurementTrace[] { null! }));
        }

        private static MeasurementTrace Trace(int index)
        {
            var suffix = index.ToString("D5", CultureInfo.InvariantCulture);
            return new MeasurementTrace(
                "SEM-" + suffix,
                "SRC-" + suffix,
                "QTY-" + suffix,
                Array.Empty<MeasurementTraceFact>(),
                1d,
                Array.Empty<MeasurementTraceAdjustment>(),
                1d,
                "m",
                "none");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountedNeverEnumerated : IReadOnlyCollection<MeasurementTrace>
        {
            internal CountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<MeasurementTrace> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericCountedNeverEnumerated : ICollection, IEnumerable<MeasurementTrace>
        {
            internal NonGenericCountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot { get; } = new object();
            internal int GetEnumeratorCalls { get; private set; }

            public void CopyTo(Array array, int index)
            {
                throw new InvalidOperationException("Oversized counted source must not be copied.");
            }

            public IEnumerator<MeasurementTrace> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingTraces : IEnumerable<MeasurementTrace>
        {
            private readonly int _count;

            internal StreamingTraces(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<MeasurementTrace> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return Trace(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class MeasurementSnapshotBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasurementSnapshotBoundSmoke.Run();
        }
    }
}
