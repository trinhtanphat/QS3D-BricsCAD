using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleCollectionNoOverreadSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownCountOverrunStopsBeforeUnexpectedCurrent();
            TerminalMoveNextCountDriftFailsClosed();
            CatalogCapacityStopsBeforeUnexpectedCurrent();
            StableCountedSnapshotStillMaterializesExactly();
        }

        private static void KnownCountOverrunStopsBeforeUnexpectedCurrent()
        {
            var values = new InstrumentedCountCollection<int>(new[] { 11, 22 }, 1, 1);
            var error = InvokeSnapshotExpectingInvalidOperation(values, 5000, "known-count overrun");

            Contains("known Count does not match completed traversal", error.Message,
                "Semantic schedule known-count N+1 must retain the existing cardinality diagnostic.");
            Equal(2, values.MoveNextCalls,
                "Semantic schedule known-count N+1 must discover the first disallowed item with MoveNext.");
            Equal(1, values.CurrentReads,
                "Semantic schedule known-count N+1 must reject before Current for the disallowed item.");
        }

        private static void TerminalMoveNextCountDriftFailsClosed()
        {
            var values = new InstrumentedCountCollection<int>(new[] { 3, 4 }, 2, 3);
            var error = InvokeSnapshotExpectingInvalidOperation(values, 5000, "terminal MoveNext Count drift");

            Contains("known Count changed or conflicted after MoveNext", error.Message,
                "Semantic schedule snapshot must rebind supported Count evidence immediately after terminal MoveNext=false.");
            Equal(3, values.MoveNextCalls,
                "Exact two-item semantic schedule traversal must observe terminal MoveNext=false.");
            Equal(2, values.CurrentReads,
                "Terminal MoveNext Count drift must be rejected after exactly the admitted values were read.");
        }

        private static void CatalogCapacityStopsBeforeUnexpectedCurrent()
        {
            var definitions = Enumerable.Range(1, 129).Select(Definition).ToArray();
            var values = new InstrumentedEnumerable<SemanticScheduleDefinition>(definitions);
            var project = new ProjectState("P-SCHEDULE-CURRENT-BOUND", "Semantic schedule Current bound");
            var beforeVersion = project.ChangeVersion;

            try
            {
                SemanticScheduleCatalog.Save(project, values);
            }
            catch (InvalidOperationException ex)
            {
                Equal("Semantic schedule catalog exceeds the supported 128 definitions.", ex.Message,
                    "Semantic schedule catalog capacity diagnostic must remain stable.");
                Equal(129, values.MoveNextCalls,
                    "Catalog capacity must detect item 129 through MoveNext.");
                Equal(128, values.CurrentReads,
                    "Catalog capacity must reject item 129 before Current is observed.");
                Equal(beforeVersion, project.ChangeVersion,
                    "Rejected catalog traversal must not mutate project version.");
                Equal(false, project.Metadata.ContainsKey(SemanticScheduleCatalog.MetadataKey),
                    "Rejected catalog traversal must not publish metadata.");
                return;
            }

            throw new InvalidOperationException("Expected semantic schedule catalog capacity rejection.");
        }

        private static void StableCountedSnapshotStillMaterializesExactly()
        {
            var values = new InstrumentedCountCollection<int>(new[] { 7, 8 }, 2, 2);
            var result = InvokeSnapshot(values, 5000, "stable control");

            Equal(2, result.Count, "Stable semantic schedule counted input must retain both values.");
            Equal(7, result[0], "Stable semantic schedule counted input must preserve first value order.");
            Equal(8, result[1], "Stable semantic schedule counted input must preserve second value order.");
            Equal(3, values.MoveNextCalls, "Stable semantic schedule counted input must terminate normally.");
            Equal(2, values.CurrentReads, "Stable semantic schedule counted input must read Current once per item.");
        }

        private static IReadOnlyList<int> InvokeSnapshot(IEnumerable<int> values, int maxCount, string capacityError)
        {
            var method = typeof(SemanticScheduleDefinition)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Single(x => x.Name == "SnapshotBounded" && x.IsGenericMethodDefinition)
                .MakeGenericMethod(typeof(int));

            try
            {
                return (IReadOnlyList<int>)(method.Invoke(null, new object[] { values, maxCount, capacityError })
                    ?? throw new InvalidOperationException("Semantic schedule SnapshotBounded returned null."));
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static InvalidOperationException InvokeSnapshotExpectingInvalidOperation(
            IEnumerable<int> values,
            int maxCount,
            string label)
        {
            try
            {
                InvokeSnapshot(values, maxCount, "capacity error");
            }
            catch (InvalidOperationException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected semantic schedule SnapshotBounded to fail: " + label);
        }

        private static SemanticScheduleDefinition Definition(int index)
        {
            return new SemanticScheduleDefinition(
                "S-" + index,
                "Schedule " + index,
                "Schedule " + index,
                Array.Empty<ElementCategory>(),
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("Id", "{Id}") });
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

        private sealed class InstrumentedEnumerable<T> : IEnumerable<T>
        {
            private readonly T[] _items;

            public InstrumentedEnumerable(T[] items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new CountingEnumerator(this, _items);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class CountingEnumerator : IEnumerator<T>
            {
                private readonly InstrumentedEnumerable<T> _owner;
                private readonly T[] _items;
                private int _index = -1;

                public CountingEnumerator(InstrumentedEnumerable<T> owner, T[] items)
                {
                    _owner = owner;
                    _items = items;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _items.Length)
                            throw new InvalidOperationException("Current read outside valid enumerator position.");
                        return _items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _items.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class InstrumentedCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private readonly int _postTraversalCount;
            private bool _enumerationCompleted;

            public InstrumentedCountCollection(T[] items, int initialCount, int postTraversalCount)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _initialCount = initialCount;
                _postTraversalCount = postTraversalCount;
            }

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            int ICollection<T>.Count => _enumerationCompleted ? _postTraversalCount : _initialCount;
            int IReadOnlyCollection<T>.Count => _enumerationCompleted ? _postTraversalCount : _initialCount;
            public bool IsReadOnly => true;

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