using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationRuleCollectionKnownCountNoOverreadSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            KnownCountOverrunRejectsBeforeExtraCurrent();
            StreamingCeilingRejectsBeforeOverflowCurrent();
            UnderYieldAndCountDriftReject();
            ConflictingAndNegativeCountsRejectBeforeTraversal();
            HonestCountedAndStreamingInputsRemainAccepted();
        }

        private static void KnownCountOverrunRejectsBeforeExtraCurrent()
        {
            var source = new CountProbeCollection<CoordinationRule>(1, Rule("R1"), Rule("R2"));
            var error = Capture<InvalidOperationException>(() => new CoordinationRuleProfile("P", 1, source));
            Contains("more entries", error.Message, "known Count overrun");
            Equal(2, source.MoveNextCalls, "known Count overrun MoveNext");
            Equal(1, source.CurrentReads, "known Count overrun Current");
        }

        private static void StreamingCeilingRejectsBeforeOverflowCurrent()
        {
            var source = new StreamingProbe<CoordinationRule>(10001, i => Rule("R" + i));
            var error = Capture<InvalidOperationException>(() => new CoordinationRuleProfile("P", 1, source));
            Contains("at most 10000", error.Message, "streaming ceiling");
            Equal(10001, source.MoveNextCalls, "streaming ceiling MoveNext");
            Equal(10000, source.CurrentReads, "streaming ceiling Current");
        }

        private static void UnderYieldAndCountDriftReject()
        {
            var underYield = new CountProbeCollection<CoordinationRule>(2, Rule("R1"));
            Contains("known count reported 2", Capture<InvalidOperationException>(() => new CoordinationRuleProfile("P", 1, underYield)).Message, "under-yield");
            Equal(1, underYield.CurrentReads, "under-yield Current");

            var drift = new SequencedCountCollection<CoordinationRule>(new[] { 1, 2 }, Rule("R1"));
            Contains("changed during traversal", Capture<InvalidOperationException>(() => new CoordinationRuleProfile("P", 1, drift)).Message, "Count drift");
            Equal(2, drift.CountReads, "Count drift rebind");
        }

        private static void ConflictingAndNegativeCountsRejectBeforeTraversal()
        {
            var conflict = new DualCountCollection<CoordinationRule>(1, 2, Rule("R1"));
            Contains("conflicting", Capture<InvalidOperationException>(() => new CoordinationRuleProfile("P", 1, conflict)).Message, "conflicting Count");
            Equal(0, conflict.MoveNextCalls, "conflicting Count traversal");

            var negative = new CountProbeCollection<CoordinationRule>(-1, Rule("R1"));
            Contains("negative", Capture<InvalidOperationException>(() => new CoordinationRuleProfile("P", 1, negative)).Message, "negative Count");
            Equal(0, negative.MoveNextCalls, "negative Count traversal");
        }

        private static void HonestCountedAndStreamingInputsRemainAccepted()
        {
            var counted = new CountProbeCollection<CoordinationRule>(1, Rule("R1"));
            var countedProfile = new CoordinationRuleProfile("P1", 1, counted);
            Equal(1, countedProfile.Rules.Count, "honest counted result");
            Equal(7, counted.CountReads, "honest counted Count rebind");

            var streaming = new StreamingProbe<CoordinationRule>(2, i => Rule("S" + i));
            var streamingProfile = new CoordinationRuleProfile("P2", 1, streaming);
            Equal(2, streamingProfile.Rules.Count, "honest streaming result");
            Equal(2, streaming.CurrentReads, "honest streaming Current");
        }

        private static CoordinationRule Rule(string id) =>
            new CoordinationRule(id, 1, "Pipe", "Beam", CoordinationRuleKind.HardClash, "High", 0d);

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException error) { return error; }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string label)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(label + ": actual='" + actual + "'.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountProbeCollection<T> : ICollection<T>
        {
            private readonly int _count;
            private readonly T[] _items;
            internal CountProbeCollection(int count, params T[] items) { _count = count; _items = items; }
            public int Count { get { CountReads++; return _count; } }
            public bool IsReadOnly => true;
            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly CountProbeCollection<T> _owner;
                private int _index = -1;
                internal ProbeEnumerator(CountProbeCollection<T> owner) { _owner = owner; }
                public T Current { get { _owner.CurrentReads++; return _owner._items[_index]; } }
                object IEnumerator.Current => Current!;
                public bool MoveNext() { _owner.MoveNextCalls++; _index++; return _index < _owner._items.Length; }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class SequencedCountCollection<T> : ICollection<T>
        {
            private readonly int[] _counts;
            private readonly T[] _items;
            internal SequencedCountCollection(int[] counts, params T[] items) { _counts = counts; _items = items; }
            public int Count { get { var index = CountReads < _counts.Length ? CountReads : _counts.Length - 1; CountReads++; return _counts[index]; } }
            public bool IsReadOnly => true;
            internal int CountReads { get; private set; }
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class DualCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly T[] _items;
            internal DualCountCollection(int genericCount, int readOnlyCount, params T[] items) { _genericCount = genericCount; _readOnlyCount = readOnlyCount; _items = items; }
            public int Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly DualCountCollection<T> _owner;
                private int _index = -1;
                internal ProbeEnumerator(DualCountCollection<T> owner) { _owner = owner; }
                public T Current => _owner._items[_index];
                object IEnumerator.Current => Current!;
                public bool MoveNext() { _owner.MoveNextCalls++; _index++; return _index < _owner._items.Length; }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class StreamingProbe<T> : IEnumerable<T>
        {
            private readonly int _count;
            private readonly Func<int, T> _factory;
            internal StreamingProbe(int count, Func<int, T> factory) { _count = count; _factory = factory; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly StreamingProbe<T> _owner;
                private int _index = -1;
                internal ProbeEnumerator(StreamingProbe<T> owner) { _owner = owner; }
                public T Current { get { _owner.CurrentReads++; return _owner._factory(_index); } }
                object IEnumerator.Current => Current!;
                public bool MoveNext() { _owner.MoveNextCalls++; _index++; return _index < _owner._count; }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}