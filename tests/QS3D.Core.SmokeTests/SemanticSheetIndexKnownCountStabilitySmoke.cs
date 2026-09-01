using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetIndexKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            KnownCountOverrunRejectsBeforeSecondCurrent();
            KnownCountUnderYieldStillFailsClosed();
            PostTraversalCountDriftFailsClosed();
            ConflictingCountSurfacesFailBeforeTraversal();
            NegativeCountFailsBeforeTraversal();
            StreamingCeilingRejectsBeforeOverflowCurrent();
            NullSheetStillFailsClosed();
            HonestCountedInputRemainsSortedAndAccepted();
            DuplicateNumbersRemainRejected();
        }

        private static void KnownCountOverrunRejectsBeforeSecondCurrent()
        {
            var source = new CountProbeCollection<SemanticSheetPlan>(1, 1, Plan("S-A", "A-001"), Plan("S-B", "A-002"));
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Contains("traversal count does not match its known count", error.Message,
                "Known-count overrun must fail closed.");
            Equal(2, source.MoveNextCalls, "Overrun must observe the boundary MoveNext.");
            Equal(1, source.CurrentReads, "Overrun must reject before reading Current beyond admitted Count.");
        }

        private static void KnownCountUnderYieldStillFailsClosed()
        {
            var source = new CountProbeCollection<SemanticSheetPlan>(2, 2, Plan("S-A", "A-001"));
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Contains("traversal count does not match its known count", error.Message,
                "Known-count under-yield must remain rejected.");
            Equal(1, source.CurrentReads, "Under-yield must read only the sheet actually produced.");
        }

        private static void PostTraversalCountDriftFailsClosed()
        {
            var source = new CountProbeCollection<SemanticSheetPlan>(1, 2, Plan("S-A", "A-001"));
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Contains("known count changed during traversal", error.Message,
                "Post-traversal known Count drift must fail closed.");
            Equal(5, source.CountReads, "Count evidence must be rebound at traversal boundaries and after traversal.");
            Equal(1, source.CurrentReads, "Count rebind must not cause an extra Current read.");
        }

        private static void ConflictingCountSurfacesFailBeforeTraversal()
        {
            var source = new ConflictingCountCollection<SemanticSheetPlan>(1, 2, Plan("S-A", "A-001"));
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Contains("conflicting known counts", error.Message,
                "Conflicting Count surfaces must fail before traversal.");
            Equal(0, source.GetEnumeratorCalls, "Conflicting Count surfaces must not start traversal.");
        }

        private static void NegativeCountFailsBeforeTraversal()
        {
            var source = new CountProbeCollection<SemanticSheetPlan>(-1, -1, Plan("S-A", "A-001"));
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Contains("invalid negative known count", error.Message,
                "Negative Count evidence must fail before traversal.");
            Equal(0, source.GetEnumeratorCalls, "Negative Count evidence must not start traversal.");
        }

        private static void StreamingCeilingRejectsBeforeOverflowCurrent()
        {
            var source = new RepeatingStreamingProbe<SemanticSheetPlan>(Plan("S-A", "A-001"), 10001);
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Contains("supports at most 10000 sheets", error.Message,
                "Pure streaming input must retain the semantic sheet ceiling.");
            Equal(10001, source.MoveNextCalls, "Streaming ceiling must observe the overflow MoveNext.");
            Equal(10000, source.CurrentReads, "Streaming ceiling must reject before overflow Current is read.");
        }

        private static void NullSheetStillFailsClosed()
        {
            var source = new CountProbeCollection<SemanticSheetPlan>(1, 1, (SemanticSheetPlan)null!);
            var error = Capture<ArgumentException>(() => SemanticSheetIndexBuilder.Build(source));
            Contains("cannot contain a null sheet", error.Message,
                "Null sheet entries must remain rejected.");
            Equal(1, source.CurrentReads, "Null validation must inspect exactly the produced entry.");
        }

        private static void HonestCountedInputRemainsSortedAndAccepted()
        {
            var source = new CountProbeCollection<SemanticSheetPlan>(
                2,
                2,
                Plan("S-B", "A-020"),
                Plan("S-A", "A-010"));

            var index = SemanticSheetIndexBuilder.Build(source);
            Equal(2, index.Rows.Count, "Honest counted input must remain accepted.");
            Equal("S-A", index.Rows[0].SheetId, "Sheet index must remain deterministically sorted by number.");
            Equal("S-B", index.Rows[1].SheetId, "Sheet index must preserve deterministic trailing order.");
            Equal(7, source.CountReads, "Honest Count evidence must be rebound around each traversal boundary and after traversal.");
            Equal(2, source.CurrentReads, "Honest traversal must read each Current exactly once.");
        }

        private static void DuplicateNumbersRemainRejected()
        {
            var source = new[] { Plan("S-A", "A-001"), Plan("S-B", "A-001") };
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Contains("duplicate sheet number", error.Message,
                "Duplicate semantic sheet numbers must remain rejected after materialization hardening.");
        }

        private static SemanticSheetPlan Plan(string id, string number)
        {
            var definition = new SemanticSheetDefinition(
                id,
                number,
                "Sheet " + id,
                841.0,
                594.0,
                Array.Empty<SemanticSheetPlacementDefinition>());
            return SemanticSheetPlanner.Build(definition, Array.Empty<SemanticViewPlan>());
        }

        private static TException Capture<TException>(Action action) where TException : Exception
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
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountProbeCollection<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private readonly int _postTraversalCount;
            private bool _completed;

            internal CountProbeCollection(int initialCount, int postTraversalCount, params T[] items)
            {
                _initialCount = initialCount;
                _postTraversalCount = postTraversalCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _completed ? _postTraversalCount : _initialCount;
                }
            }

            public bool IsReadOnly => true;
            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return new ProbeEnumerator(this);
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly CountProbeCollection<T> _owner;
                private int _index = -1;
                internal ProbeEnumerator(CountProbeCollection<T> owner) { _owner = owner; }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }
                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index < _owner._items.Length) return true;
                    _owner._completed = true;
                    return false;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class ConflictingCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _collectionCount;
            private readonly int _readOnlyCount;

            internal ConflictingCountCollection(int collectionCount, int readOnlyCount, params T[] items)
            {
                _collectionCount = collectionCount;
                _readOnlyCount = readOnlyCount;
                _items = items;
            }

            int ICollection<T>.Count => _collectionCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            bool ICollection<T>.IsReadOnly => true;
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return ((IEnumerable<T>)_items).GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => throw new NotSupportedException();
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        }

        private sealed class RepeatingStreamingProbe<T> : IEnumerable<T>
        {
            private readonly T _value;
            private readonly int _count;

            internal RepeatingStreamingProbe(T value, int count)
            {
                _value = value;
                _count = count;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly RepeatingStreamingProbe<T> _owner;
                private int _index = -1;
                internal ProbeEnumerator(RepeatingStreamingProbe<T> owner) { _owner = owner; }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._value;
                    }
                }
                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._count;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class SemanticSheetIndexKnownCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SemanticSheetIndexKnownCountStabilitySmoke.Run();
        }
    }
}
