using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampKnownCountNoOverreadSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownCountOverrunStopsBeforeUnexpectedCurrent();
            MaximumCeilingStopsBeforeUnexpectedCurrent();
            PostTraversalCountDriftFailsClosed();
            ConflictingCountSurfacesFailBeforeEnumeration();
            StableCountedInputStillMaterializesExactly();
        }

        private static void KnownCountOverrunStopsBeforeUnexpectedCurrent()
        {
            var values = new InstrumentedCountCollection<int>(new[] { 11, 22 }, 1, 1);
            var error = InvokeExpectingInvalidOperation(values, 1, "known-count overrun");

            Contains("known count does not match", error.Message,
                "Persistence known-count N+1 must report the existing cardinality contract.");
            Equal(2, values.MoveNextCalls,
                "Persistence known-count N+1 must observe the first disallowed item with MoveNext.");
            Equal(1, values.CurrentReads,
                "Persistence known-count N+1 must reject before reading Current for the disallowed item.");
        }

        private static void MaximumCeilingStopsBeforeUnexpectedCurrent()
        {
            var values = new InstrumentedCountCollection<int>(Enumerable.Range(0, 10_001).ToArray(), 10_000, 10_000);
            var error = InvokeExpectingInvalidOperation(values, 10_000, "maximum ceiling");

            Contains("supports at most 10000 entries", error.Message,
                "The independent persistence snapshot ceiling must retain precedence at item 10001.");
            Equal(10_001, values.MoveNextCalls,
                "Persistence ceiling must detect item 10001 through MoveNext.");
            Equal(10_000, values.CurrentReads,
                "Persistence ceiling must reject item 10001 before Current is observed.");
        }

        private static void PostTraversalCountDriftFailsClosed()
        {
            var values = new InstrumentedCountCollection<int>(new[] { 3, 4 }, 2, 3);
            var error = InvokeExpectingInvalidOperation(values, 2, "post-traversal Count drift");

            Contains("count changed or conflicted after traversal", error.Message,
                "Persistence snapshot must rebind deterministic Count evidence after exact traversal.");
            Equal(3, values.MoveNextCalls,
                "Exact two-item traversal must include the terminal MoveNext=false observation.");
            Equal(2, values.CurrentReads,
                "Post-traversal Count drift must be rejected only after the exact admitted values were read.");
        }

        private static void ConflictingCountSurfacesFailBeforeEnumeration()
        {
            var values = new InstrumentedCountCollection<int>(new[] { 5, 6 }, 2, 2, readOnlyCountOverride: 3);
            var error = InvokeExpectingInvalidOperation(values, 2, "conflicting Count surfaces");

            Contains("known count does not match enumerated entry count", error.Message,
                "Conflicting supported Count surfaces must preserve the deterministic Count mismatch diagnostic.");
            Equal(0, values.MoveNextCalls,
                "Conflicting pre-traversal Count evidence must fail before MoveNext.");
            Equal(0, values.CurrentReads,
                "Conflicting pre-traversal Count evidence must fail before Current.");
        }

        private static void StableCountedInputStillMaterializesExactly()
        {
            var values = new InstrumentedCountCollection<int>(new[] { 7, 8 }, 2, 2);
            var result = Invoke(values, 2, "stable control");

            Equal(2, result.Count, "Stable counted persistence input must retain both values.");
            Equal(7, result[0], "Stable counted persistence input must preserve first value order.");
            Equal(8, result[1], "Stable counted persistence input must preserve second value order.");
            Equal(3, values.MoveNextCalls, "Stable counted persistence input must terminate normally.");
            Equal(2, values.CurrentReads, "Stable counted persistence input must read Current exactly once per item.");
        }

        private static List<int> Invoke(IEnumerable<int> values, int knownCount, string label)
        {
            var method = typeof(ProjectPersistenceStamp)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Single(x => x.Name == "SnapshotBounded" && x.IsGenericMethodDefinition)
                .MakeGenericMethod(typeof(int));

            try
            {
                return (List<int>)(method.Invoke(null, new object[] { values, knownCount, label, 10_000 })
                    ?? throw new InvalidOperationException("Persistence SnapshotBounded returned null."));
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static InvalidOperationException InvokeExpectingInvalidOperation(
            IEnumerable<int> values,
            int knownCount,
            string label)
        {
            try
            {
                Invoke(values, knownCount, label);
            }
            catch (InvalidOperationException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected persistence SnapshotBounded to fail: " + label);
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private sealed class InstrumentedCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private readonly int _postTraversalCount;
            private readonly int? _readOnlyCountOverride;
            private bool _enumerationCompleted;

            public InstrumentedCountCollection(
                T[] items,
                int initialCount,
                int postTraversalCount,
                int? readOnlyCountOverride = null)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _initialCount = initialCount;
                _postTraversalCount = postTraversalCount;
                _readOnlyCountOverride = readOnlyCountOverride;
            }

            int ICollection<T>.Count => CurrentCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCountOverride ?? CurrentCount;
            public bool IsReadOnly => true;
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }

            private int CurrentCount => _enumerationCompleted ? _postTraversalCount : _initialCount;

            public IEnumerator<T> GetEnumerator() => new CountingEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Contains(T item) => ((ICollection<T>)_items).Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class CountingEnumerator : IEnumerator<T>
            {
                private readonly InstrumentedCountCollection<T> _owner;
                private int _index = -1;

                public CountingEnumerator(InstrumentedCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _owner._items.Length)
                            throw new InvalidOperationException("Current read outside valid enumerator position.");
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index < _owner._items.Length) return true;
                    _owner._enumerationCompleted = true;
                    return false;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
