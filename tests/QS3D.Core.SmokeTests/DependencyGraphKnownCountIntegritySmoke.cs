using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphKnownCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RebuildRejectsOverrunBeforeCurrentAndPreservesGraph();
            OrderingRejectsOverrunBeforeCurrent();
            RebuildRejectsPostTraversalCountDriftAndPreservesGraph();
            OrderingRejectsPostTraversalNegativeCount();
            OrderingRejectsPostTraversalCountConflict();
            StableMultiInterfaceCountsRemainAccepted();
            PureStreamingInputsRemainAccepted();
        }

        private static void RebuildRejectsOverrunBeforeCurrentAndPreservesGraph()
        {
            var graph = new DependencyGraph();
            var keep = Element("KEEP");
            graph.Rebuild(new[] { keep });

            var source = new CurrentCountingReadOnlySource(2, 1, index => Element("NEW-" + index), true);
            ExpectInvalid(() => graph.Rebuild(source), "count changed during enumeration", "Rebuild Count=N must reject N+1 before reading Current.");
            if (source.MoveNextCalls != 2 || source.CurrentReads != 1)
                throw new InvalidOperationException("Dependency rebuild must stop at N+1 MoveNext without N+1 Current.");
            if (!graph.TryGetElement("KEEP", out var retained) || !ReferenceEquals(keep, retained) || graph.TryGetElement("NEW-0", out _))
                throw new InvalidOperationException("Rejected dependency rebuild must preserve the previously published graph atomically.");
        }

        private static void OrderingRejectsOverrunBeforeCurrent()
        {
            var source = new CurrentCountingReadOnlySource(2, 1, index => Element("ORDER-" + index), true);
            ExpectInvalid(() => new DependencyGraph().TopologicalDirtyOrder(source), "count changed during enumeration", "Ordering Count=N must reject N+1 before reading Current.");
            if (source.MoveNextCalls != 2 || source.CurrentReads != 1)
                throw new InvalidOperationException("Dependency ordering must stop at N+1 MoveNext without N+1 Current.");
        }

        private static void RebuildRejectsPostTraversalCountDriftAndPreservesGraph()
        {
            var graph = new DependencyGraph();
            var keep = Element("KEEP-DRIFT");
            graph.Rebuild(new[] { keep });

            var source = new MultiCountSource(new[] { Element("DRIFT") }, 1, 1, 1, 2, 2, 2);
            ExpectInvalid(() => graph.Rebuild(source), "count changed during enumeration", "Rebuild must reject Count drift after exact traversal.");
            if (!source.TraversalCompleted)
                throw new InvalidOperationException("Post-traversal dependency Count drift fixture did not reach exact exhaustion.");
            if (!graph.TryGetElement("KEEP-DRIFT", out var retained) || !ReferenceEquals(keep, retained) || graph.TryGetElement("DRIFT", out _))
                throw new InvalidOperationException("Post-traversal Count drift must not publish staged dependency graph state.");
        }

        private static void OrderingRejectsPostTraversalNegativeCount()
        {
            var source = new MultiCountSource(new[] { Element("NEGATIVE") }, 1, 1, 1, -1, -1, -1);
            ExpectInvalid(() => new DependencyGraph().TopologicalDirtyOrder(source), "invalid negative element count", "Ordering must reject rebound negative Count evidence.");
            if (!source.TraversalCompleted)
                throw new InvalidOperationException("Negative rebound Count fixture did not complete exact traversal.");
        }

        private static void OrderingRejectsPostTraversalCountConflict()
        {
            var source = new MultiCountSource(new[] { Element("CONFLICT") }, 1, 1, 1, 1, 2, 1);
            ExpectInvalid(() => new DependencyGraph().TopologicalDirtyOrder(source), "conflicting known element counts", "Ordering must reject rebound conflicting Count evidence.");
        }

        private static void StableMultiInterfaceCountsRemainAccepted()
        {
            var element = Element("STABLE");
            var source = new MultiCountSource(new[] { element }, 1, 1, 1, 1, 1, 1);
            var graph = new DependencyGraph();
            graph.Rebuild(source);
            if (!graph.TryGetElement("stable", out var resolved) || !ReferenceEquals(element, resolved))
                throw new InvalidOperationException("Stable multi-interface Count evidence must remain accepted by dependency rebuild.");

            var ordered = graph.TopologicalDirtyOrder(source);
            if (ordered.Count != 1 || !ReferenceEquals(element, ordered[0]))
                throw new InvalidOperationException("Stable multi-interface Count evidence must remain accepted by dependency ordering.");
        }

        private static void PureStreamingInputsRemainAccepted()
        {
            var graph = new DependencyGraph();
            graph.Rebuild(Stream("STREAM"));
            var ordered = graph.TopologicalDirtyOrder(Stream("STREAM"));
            if (ordered.Count != 1 || !string.Equals(ordered[0].Id, "STREAM", StringComparison.Ordinal))
                throw new InvalidOperationException("Pure streaming dependency inputs must remain supported.");
        }

        private static IEnumerable<ProjectElement> Stream(string id)
        {
            yield return Element(id);
        }

        private static ProjectElement Element(string id)
        {
            return new ProjectElement(id, ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
        }

        private static void ExpectInvalid(Action action, string fragment, string failure)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(failure + " Actual diagnostic: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException(failure);
        }

        private sealed class CurrentCountingReadOnlySource : IReadOnlyCollection<ProjectElement>
        {
            private readonly int _actualCount;
            private readonly Func<int, ProjectElement> _factory;
            private readonly bool _throwOnUnexpectedCurrent;

            internal CurrentCountingReadOnlySource(int actualCount, int reportedCount, Func<int, ProjectElement> factory, bool throwOnUnexpectedCurrent)
            {
                _actualCount = actualCount;
                Count = reportedCount;
                _factory = factory;
                _throwOnUnexpectedCurrent = throwOnUnexpectedCurrent;
            }

            public int Count { get; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<ProjectElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly CurrentCountingReadOnlySource _owner;
                private int _index = -1;
                internal Enumerator(CurrentCountingReadOnlySource owner) { _owner = owner; }

                public ProjectElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._throwOnUnexpectedCurrent && _owner.CurrentReads > _owner.Count)
                            throw new InvalidOperationException("Unexpected dependency Current read beyond admitted Count.");
                        return _owner._factory(_index);
                    }
                }

                object IEnumerator.Current => Current!;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._actualCount;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class MultiCountSource : ICollection<ProjectElement>, IReadOnlyCollection<ProjectElement>, ICollection
        {
            private readonly ProjectElement[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly int _finalGenericCount;
            private readonly int _finalReadOnlyCount;
            private readonly int _finalNonGenericCount;

            internal MultiCountSource(
                ProjectElement[] items,
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                int finalGenericCount,
                int finalReadOnlyCount,
                int finalNonGenericCount)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _finalGenericCount = finalGenericCount;
                _finalReadOnlyCount = finalReadOnlyCount;
                _finalNonGenericCount = finalNonGenericCount;
            }

            internal bool TraversalCompleted { get; private set; }
            int ICollection<ProjectElement>.Count => TraversalCompleted ? _finalGenericCount : _genericCount;
            int IReadOnlyCollection<ProjectElement>.Count => TraversalCompleted ? _finalReadOnlyCount : _readOnlyCount;
            int ICollection.Count => TraversalCompleted ? _finalNonGenericCount : _nonGenericCount;
            bool ICollection<ProjectElement>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            public IEnumerator<ProjectElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<ProjectElement>.Add(ProjectElement item) => throw new NotSupportedException();
            void ICollection<ProjectElement>.Clear() => throw new NotSupportedException();
            bool ICollection<ProjectElement>.Contains(ProjectElement item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<ProjectElement>.CopyTo(ProjectElement[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<ProjectElement>.Remove(ProjectElement item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly MultiCountSource _owner;
                private int _index = -1;
                internal Enumerator(MultiCountSource owner) { _owner = owner; }
                public ProjectElement Current => _owner._items[_index];
                object IEnumerator.Current => Current!;
                public bool MoveNext()
                {
                    _index++;
                    if (_index < _owner._items.Length) return true;
                    _owner.TraversalCompleted = true;
                    return false;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
