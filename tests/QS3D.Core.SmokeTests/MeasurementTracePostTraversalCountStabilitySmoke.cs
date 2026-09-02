using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTracePostTraversalCountStabilitySmoke
    {
        internal static void Run()
        {
            FactCountDriftAfterExactTraversalFailsClosed();
            AdjustmentCountDriftAfterExactTraversalFailsClosed();
            MessageCountDriftAfterExactTraversalFailsClosed();
            NegativeFinalCountFailsClosed();
            MultiInterfaceConflictAfterTraversalFailsClosed();
            StableCountedChildrenRemainAccepted();
            PureStreamingChildrenRemainAccepted();
        }

        private static void FactCountDriftAfterExactTraversalFailsClosed()
        {
            var facts = new PhaseReadOnlyCollection<MeasurementTraceFact>(
                new[] { new MeasurementTraceFact("length", 2d, "m", "SRC-F1") }, 1, 0);

            var error = Capture<ArgumentException>(() => Trace(facts, Array.Empty<MeasurementTraceAdjustment>()));
            Equal(7, facts.CountReads, "Fact Count must be rebound across enumerator acquisition, MoveNext, Current, and terminal traversal.");
            Contains("count changed during enumeration", error.Message, "Fact Count drift must fail closed.");
        }

        private static void AdjustmentCountDriftAfterExactTraversalFailsClosed()
        {
            var adjustments = new PhaseReadOnlyCollection<MeasurementTraceAdjustment>(
                new[] { new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, 1d, "m3", "opening", "SRC-A1") }, 1, 2);

            var error = Capture<ArgumentException>(() => Trace(Array.Empty<MeasurementTraceFact>(), adjustments));
            Equal(7, adjustments.CountReads, "Adjustment Count must be rebound across enumerator acquisition, MoveNext, Current, and terminal traversal.");
            Contains("count changed during enumeration", error.Message, "Adjustment Count drift must fail closed.");
        }

        private static void MessageCountDriftAfterExactTraversalFailsClosed()
        {
            var warnings = new PhaseReadOnlyCollection<string>(new[] { "review opening" }, 1, 0);
            var error = Capture<ArgumentException>(() => Trace(Array.Empty<MeasurementTraceFact>(), Array.Empty<MeasurementTraceAdjustment>(), warnings));
            Equal(7, warnings.CountReads, "Message Count must be rebound across enumerator acquisition, MoveNext, Current, and terminal traversal.");
            Contains("count changed during enumeration", error.Message, "Message Count drift must fail closed.");
        }

        private static void NegativeFinalCountFailsClosed()
        {
            var facts = new PhaseReadOnlyCollection<MeasurementTraceFact>(
                new[] { new MeasurementTraceFact("area", 3d, "m2", "SRC-F2") }, 1, -1);
            var error = Capture<ArgumentException>(() => Trace(facts, Array.Empty<MeasurementTraceAdjustment>()));
            Equal(7, facts.CountReads, "Negative terminal Count must be observed by the post-MoveNext rebound before publication.");
            Contains("count cannot be negative", error.Message, "Negative post-traversal Count must fail through canonical Count validation.");
        }

        private static void MultiInterfaceConflictAfterTraversalFailsClosed()
        {
            var facts = new PhaseMultiCollection<MeasurementTraceFact>(
                new[] { new MeasurementTraceFact("length", 2d, "m", "SRC-F3") },
                initialCount: 1,
                finalGenericCount: 1,
                finalReadOnlyCount: 2,
                finalNonGenericCount: 1);

            var error = Capture<ArgumentException>(() => Trace(facts, Array.Empty<MeasurementTraceAdjustment>()));
            Contains("count contracts disagree", error.Message, "Post-traversal Count-interface conflict must fail closed.");
            Equal(7, facts.GenericCountReads, "Generic Count must be rebound through terminal traversal.");
            Equal(7, facts.ReadOnlyCountReads, "Read-only Count must expose the terminal conflict.");
        }

        private static void StableCountedChildrenRemainAccepted()
        {
            var facts = new PhaseReadOnlyCollection<MeasurementTraceFact>(
                new[] { new MeasurementTraceFact("length", 2d, "m", "SRC-F1") }, 1, 1);
            var adjustments = new PhaseReadOnlyCollection<MeasurementTraceAdjustment>(
                new[] { new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, 1d, "m3", "opening", "SRC-A1") }, 1, 1);
            var warnings = new PhaseReadOnlyCollection<string>(new[] { "review opening" }, 1, 1);

            var trace = Trace(facts, adjustments, warnings);
            Equal(1, trace.InputFacts.Count, "Stable counted facts must remain accepted.");
            Equal(1, trace.Adjustments.Count, "Stable counted adjustments must remain accepted.");
            Equal(1, trace.Warnings.Count, "Stable counted messages must remain accepted.");
            Equal(8, facts.CountReads, "Stable fact Count must include the final post-traversal publication rebound.");
            Equal(8, adjustments.CountReads, "Stable adjustment Count must include the final post-traversal publication rebound.");
            Equal(8, warnings.CountReads, "Stable message Count must include the final post-traversal publication rebound.");
        }

        private static void PureStreamingChildrenRemainAccepted()
        {
            var trace = Trace(
                Stream(new MeasurementTraceFact("length", 2d, "m", "SRC-F1")),
                Stream(new MeasurementTraceAdjustment(MeasurementTraceAdjustmentKind.Deduction, 1d, "m3", "opening", "SRC-A1")),
                Stream("review opening"));

            Equal(1, trace.InputFacts.Count, "Pure-streaming facts must remain accepted.");
            Equal(1, trace.Adjustments.Count, "Pure-streaming adjustments must remain accepted.");
            Equal(1, trace.Warnings.Count, "Pure-streaming messages must remain accepted.");
        }

        private static MeasurementTrace Trace(IEnumerable<MeasurementTraceFact> facts, IEnumerable<MeasurementTraceAdjustment> adjustments, IEnumerable<string>? warnings = null)
        {
            return new MeasurementTrace("E-1", "SRC-1", "volume", facts, 10d, adjustments, 9d, "m3", "nearest", warnings, Array.Empty<string>());
        }

        private static IEnumerable<T> Stream<T>(params T[] values)
        {
            for (var i = 0; i < values.Length; i++) yield return values[i];
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
            public int Count { get { CountReads++; return _enumerated ? _finalCount : _initialCount; } }

            public IEnumerator<T> GetEnumerator()
            {
                try { for (var i = 0; i < _items.Length; i++) yield return _items[i]; }
                finally { _enumerated = true; }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class PhaseMultiCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private readonly int _finalGenericCount;
            private readonly int _finalReadOnlyCount;
            private readonly int _finalNonGenericCount;
            private bool _enumerated;

            internal PhaseMultiCollection(T[] items, int initialCount, int finalGenericCount, int finalReadOnlyCount, int finalNonGenericCount)
            {
                _items = items;
                _initialCount = initialCount;
                _finalGenericCount = finalGenericCount;
                _finalReadOnlyCount = finalReadOnlyCount;
                _finalNonGenericCount = finalNonGenericCount;
            }

            internal int GenericCountReads { get; private set; }
            internal int ReadOnlyCountReads { get; private set; }
            internal int NonGenericCountReads { get; private set; }
            int ICollection<T>.Count { get { GenericCountReads++; return _enumerated ? _finalGenericCount : _initialCount; } }
            int IReadOnlyCollection<T>.Count { get { ReadOnlyCountReads++; return _enumerated ? _finalReadOnlyCount : _initialCount; } }
            int ICollection.Count { get { NonGenericCountReads++; return _enumerated ? _finalNonGenericCount : _initialCount; } }
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                try { for (var i = 0; i < _items.Length; i++) yield return _items[i]; }
                finally { _enumerated = true; }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            bool ICollection<T>.Contains(T item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException error) { return error; }
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
    }

    internal static class MeasurementTracePostTraversalCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MeasurementTracePostTraversalCountStabilitySmoke.Run();
    }
}
