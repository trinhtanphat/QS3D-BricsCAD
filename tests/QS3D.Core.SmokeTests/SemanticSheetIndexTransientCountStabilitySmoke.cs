using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetIndexTransientCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectGrowthAfterMoveNextBeforeCurrent();
            RejectNegativeAfterMoveNextBeforeCurrent();
            RejectShrinkBeforeNextMoveNext();
        }

        private static void RejectGrowthAfterMoveNextBeforeCurrent()
        {
            var source = new TransientCountCollection<SemanticSheetPlan>(
                owner => owner.MoveNextCalls >= 1 ? 2 : 1,
                Plan("S-A", "A-001"));
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Contains("known count changed during traversal", error.Message,
                "Transient semantic sheet Count growth must fail at the post-MoveNext boundary.");
            Equal(1, source.MoveNextCalls, "Transient growth must observe exactly one MoveNext.");
            Equal(0, source.CurrentReads, "Transient growth must fail before Current.");
        }

        private static void RejectNegativeAfterMoveNextBeforeCurrent()
        {
            var source = new TransientCountCollection<SemanticSheetPlan>(
                owner => owner.MoveNextCalls >= 1 ? -1 : 1,
                Plan("S-A", "A-001"));
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Contains("invalid negative known count", error.Message,
                "Transient negative semantic sheet Count must fail at the post-MoveNext boundary.");
            Equal(1, source.MoveNextCalls, "Transient negative Count must observe exactly one MoveNext.");
            Equal(0, source.CurrentReads, "Transient negative Count must fail before Current.");
        }

        private static void RejectShrinkBeforeNextMoveNext()
        {
            var source = new TransientCountCollection<SemanticSheetPlan>(
                owner => owner.CurrentReads >= 1 ? 1 : 2,
                Plan("S-A", "A-001"),
                Plan("S-B", "A-002"));
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Contains("known count changed during traversal", error.Message,
                "Transient semantic sheet Count shrink must fail before the next MoveNext.");
            Equal(1, source.MoveNextCalls, "Pre-next-iteration Count shrink must win before the second MoveNext.");
            Equal(1, source.CurrentReads, "Pre-next-iteration Count shrink must retain only the admitted first Current read.");
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
            try { action(); }
            catch (TException ex) { return ex; }
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

        private sealed class TransientCountCollection<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly Func<TransientCountCollection<T>, int> _count;

            internal TransientCountCollection(Func<TransientCountCollection<T>, int> count, params T[] items)
            {
                _count = count ?? throw new ArgumentNullException(nameof(count));
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count => _count(this);
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly TransientCountCollection<T> _owner;
                private int _index = -1;
                internal ProbeEnumerator(TransientCountCollection<T> owner) { _owner = owner; }

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
                    return _index < _owner._items.Length;
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
    }
}
