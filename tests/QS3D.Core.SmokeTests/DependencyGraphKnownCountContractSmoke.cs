using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphKnownCountContractSmoke
    {
        private const int MaxElementInputCount = 10000;

        public static void Run()
        {
            ConflictingKnownCountsFailBeforeEnumerationAndPreserveGraph();
            CapacityViolationPrecedesCountConflict();
            NegativeKnownCountFailsBeforeEnumeration();
            NonGenericOversizedCountFailsBeforeEnumeration();
            ConsistentKnownCountsRemainAccepted();
            KnownCountTraversalMismatchFailsClosedAndPreservesGraph();
            KnownCountTraversalMismatchFailsDirtyOrdering();
            KnownCountOverrunPrecedesUnexpectedNullAndPreservesGraph();
            KnownCountOverrunPrecedesDuplicateValidation();
            DirtyOrderingKnownCountOverrunPrecedesUnexpectedNullValidation();
            DirtyAndCleanDuplicateSemanticIdsFailDirtyOrdering();
            CleanDuplicateSemanticIdsFailDirtyOrdering();
            ExactBoundRemainsAccepted();
            DishonestKnownCountStopsAtFirstUnexpectedElement();
            PureStreamingStillStopsAtIndependentBoundary();
        }

        private static void ConflictingKnownCountsFailBeforeEnumerationAndPreserveGraph()
        {
            var graph = new DependencyGraph();
            var keep = Element("KEEP");
            graph.Rebuild(new[] { keep });

            var rebuildSource = new MultiCountCollection(new[] { Element("NEW") }, 1, 2, 1, throwOnEnumeration: true);
            ExpectInvalidOperation(() => graph.Rebuild(rebuildSource), "conflicting known element counts", "Rebuild must reject conflicting known counts before enumeration.");
            if (rebuildSource.EnumerationRequested)
                throw new Exception("Rebuild inspected the enumerator after conflicting known-count evidence was already available.");
            if (!graph.TryGetElement("KEEP", out var retained) || !ReferenceEquals(keep, retained) || graph.TryGetElement("NEW", out _))
                throw new Exception("A conflicting-count rebuild must preserve the previously committed dependency graph atomically.");

            var orderingSource = new MultiCountCollection(new[] { Element("ORDER") }, 1, 1, 2, throwOnEnumeration: true);
            ExpectInvalidOperation(() => graph.TopologicalDirtyOrder(orderingSource), "conflicting known element counts", "Topological ordering must reject conflicting known counts before enumeration.");
            if (orderingSource.EnumerationRequested)
                throw new Exception("Topological ordering inspected the enumerator after conflicting known-count evidence was already available.");
        }

        private static void CapacityViolationPrecedesCountConflict()
        {
            var graph = new DependencyGraph();
            var source = new MultiCountCollection(new[] { Element("CAPACITY") }, 1, MaxElementInputCount + 1, 2, throwOnEnumeration: true);
            ExpectInvalidOperation(() => graph.Rebuild(source), "exceeds the supported", "Known capacity violations must retain precedence over count-conflict diagnostics.");
            if (source.EnumerationRequested)
                throw new Exception("Capacity violation must be rejected before caller enumeration.");
        }

        private static void NegativeKnownCountFailsBeforeEnumeration()
        {
            var graph = new DependencyGraph();
            var source = new MultiCountCollection(new[] { Element("NEG") }, -1, -1, -1, throwOnEnumeration: true);
            ExpectInvalidOperation(() => graph.Rebuild(source), "invalid negative element count", "Negative known dependency count must fail closed before enumeration.");
            if (source.EnumerationRequested)
                throw new Exception("Negative known dependency count must be rejected before enumeration.");
        }

        private static void NonGenericOversizedCountFailsBeforeEnumeration()
        {
            var graph = new DependencyGraph();
            var rebuildSource = new NonGenericCountEnumerable(MaxElementInputCount + 1);
            ExpectInvalidOperation(() => graph.Rebuild(rebuildSource), "exceeds the supported", "Non-generic oversized rebuild count must fail before enumeration.");
            if (rebuildSource.EnumerationRequested)
                throw new Exception("Rebuild ignored the non-generic ICollection count contract.");

            var orderingSource = new NonGenericCountEnumerable(MaxElementInputCount + 1);
            ExpectInvalidOperation(() => graph.TopologicalDirtyOrder(orderingSource), "exceeds the supported", "Non-generic oversized ordering count must fail before enumeration.");
            if (orderingSource.EnumerationRequested)
                throw new Exception("Topological ordering ignored the non-generic ICollection count contract.");
        }

        private static void ConsistentKnownCountsRemainAccepted()
        {
            var element = Element("CONSISTENT");
            var source = new MultiCountCollection(new[] { element }, 1, 1, 1, throwOnEnumeration: false);
            var graph = new DependencyGraph();
            graph.Rebuild(source);
            if (!graph.TryGetElement("consistent", out var resolved) || !ReferenceEquals(element, resolved))
                throw new Exception("Consistent multi-interface counts must remain valid dependency graph input.");

            var ordered = graph.TopologicalDirtyOrder(source);
            if (source.EnumerationRequestCount != 2)
                throw new Exception("Consistent known-count input should be enumerated normally by both dependency operations.");
            if (ordered.Count > 1)
                throw new Exception("Single-element dependency ordering produced an impossible result count.");
        }

        private static void KnownCountTraversalMismatchFailsClosedAndPreservesGraph()
        {
            var graph = new DependencyGraph();
            var keep = Element("KEEP-TRAVERSAL");
            graph.Rebuild(new[] { keep });

            var under = new MultiCountCollection(new[] { Element("UNDER") }, 2, 2, 2, throwOnEnumeration: false);
            ExpectInvalidOperation(() => graph.Rebuild(under), "count changed during enumeration", "Rebuild must reject known Count under-enumeration.");
            if (under.EnumerationRequestCount != 1)
                throw new Exception("Known Count under-enumeration must inspect the rebuild source exactly once.");
            if (!graph.TryGetElement("KEEP-TRAVERSAL", out var retainedAfterUnder) || !ReferenceEquals(keep, retainedAfterUnder) || graph.TryGetElement("UNDER", out _))
                throw new Exception("Known Count under-enumeration must preserve the previously committed graph atomically.");

            var over = new MultiCountCollection(new[] { Element("OVER-1"), Element("OVER-2") }, 1, 1, 1, throwOnEnumeration: false);
            ExpectInvalidOperation(() => graph.Rebuild(over), "count changed during enumeration", "Rebuild must reject known Count over-enumeration.");
            if (over.EnumerationRequestCount != 1)
                throw new Exception("Known Count over-enumeration must inspect the rebuild source exactly once.");
            if (!graph.TryGetElement("KEEP-TRAVERSAL", out var retainedAfterOver) || !ReferenceEquals(keep, retainedAfterOver) || graph.TryGetElement("OVER-1", out _) || graph.TryGetElement("OVER-2", out _))
                throw new Exception("Known Count over-enumeration must preserve the previously committed graph atomically.");
        }

        private static void KnownCountTraversalMismatchFailsDirtyOrdering()
        {
            var graph = new DependencyGraph();

            var under = new MultiCountCollection(new[] { Element("ORDER-UNDER") }, 2, 2, 2, throwOnEnumeration: false);
            ExpectInvalidOperation(() => graph.TopologicalDirtyOrder(under), "count changed during enumeration", "Topological ordering must reject known Count under-enumeration.");
            if (under.EnumerationRequestCount != 1)
                throw new Exception("Known Count under-enumeration must inspect the ordering source exactly once.");

            var over = new MultiCountCollection(new[] { Element("ORDER-OVER-1"), Element("ORDER-OVER-2") }, 1, 1, 1, throwOnEnumeration: false);
            ExpectInvalidOperation(() => graph.TopologicalDirtyOrder(over), "count changed during enumeration", "Topological ordering must reject known Count over-enumeration.");
            if (over.EnumerationRequestCount != 1)
                throw new Exception("Known Count over-enumeration must inspect the ordering source exactly once.");
        }

        private static void KnownCountOverrunPrecedesUnexpectedNullAndPreservesGraph()
        {
            var graph = new DependencyGraph();
            var keep = Element("KEEP-OVERRUN");
            graph.Rebuild(new[] { keep });

            var source = new MultiCountCollection(
                new ProjectElement[] { Element("VALID-FIRST"), null! },
                1,
                1,
                1,
                throwOnEnumeration: false);

            ExpectInvalidOperation(
                () => graph.Rebuild(source),
                "count changed during enumeration",
                "Known-count overrun must win before unexpected null-element validation.");
            if (!graph.TryGetElement("KEEP-OVERRUN", out var retained) || !ReferenceEquals(keep, retained) || graph.TryGetElement("VALID-FIRST", out _))
                throw new Exception("Known-count overrun must preserve the previously committed graph atomically.");
        }

        private static void KnownCountOverrunPrecedesDuplicateValidation()
        {
            var duplicate = Element("DUP-OVERRUN");
            var source = new MultiCountCollection(
                new[] { duplicate, Element("dup-overrun") },
                1,
                1,
                1,
                throwOnEnumeration: false);

            ExpectInvalidOperation(
                () => new DependencyGraph().Rebuild(source),
                "count changed during enumeration",
                "Known-count overrun must win before duplicate semantic-id validation on the first unexpected element.");
        }

        private static void DirtyOrderingKnownCountOverrunPrecedesUnexpectedNullValidation()
        {
            var source = new MultiCountCollection(
                new ProjectElement[] { Element("ORDER-FIRST"), null! },
                1,
                1,
                1,
                throwOnEnumeration: false);

            ExpectInvalidOperation(
                () => new DependencyGraph().TopologicalDirtyOrder(source),
                "count changed during enumeration",
                "Dependency ordering known-count overrun must win before unexpected null-element validation.");
        }

        private static void DirtyAndCleanDuplicateSemanticIdsFailDirtyOrdering()
        {
            var dirty = Element("ORDER-DUP-DIRTY-CLEAN");
            var clean = Element("ORDER-DUP-DIRTY-CLEAN");
            clean.MarkClean(ElementDirtyFlags.All);

            ExpectInvalidOperation(
                () => new DependencyGraph().TopologicalDirtyOrder(new[] { dirty, clean }),
                "duplicate semantic element id",
                "Topological ordering must reject duplicate semantic IDs across dirty and clean elements.");
        }

        private static void CleanDuplicateSemanticIdsFailDirtyOrdering()
        {
            var first = Element("ORDER-DUP-CLEAN-CASE");
            var second = Element("order-dup-clean-case");
            first.MarkClean(ElementDirtyFlags.All);
            second.MarkClean(ElementDirtyFlags.All);

            ExpectInvalidOperation(
                () => new DependencyGraph().TopologicalDirtyOrder(new[] { first, second }),
                "duplicate semantic element id",
                "Topological ordering must reject case-insensitive duplicate semantic IDs when all matching elements are clean.");
        }

        private static void ExactBoundRemainsAccepted()
        {
            var elements = new List<ProjectElement>(MaxElementInputCount);
            for (var index = 0; index < MaxElementInputCount; index++)
                elements.Add(Element("BOUND-" + index));

            var graph = new DependencyGraph();
            graph.Rebuild(elements);
            var ordered = graph.TopologicalDirtyOrder(elements);
            if (!graph.TryGetElement("BOUND-9999", out _))
                throw new Exception("The exact 10,000-element dependency graph boundary must remain accepted.");
            if (ordered.Count != MaxElementInputCount)
                throw new Exception("The exact-bound dirty ordering must retain all default-dirty semantic elements.");
        }

        private static void DishonestKnownCountStopsAtFirstUnexpectedElement()
        {
            var rebuildSource = new DishonestReadOnlyCollection(MaxElementInputCount + 1, reportedCount: 1);
            ExpectInvalidOperation(() => new DependencyGraph().Rebuild(rebuildSource), "count changed during enumeration", "Dishonest rebuild Count must stop on the first unexpected element.");
            if (rebuildSource.MoveNextCalls != 2)
                throw new Exception("Rebuild must stop immediately after observing the first element beyond its trustworthy known Count.");

            var orderingSource = new DishonestReadOnlyCollection(MaxElementInputCount + 1, reportedCount: 1);
            ExpectInvalidOperation(() => new DependencyGraph().TopologicalDirtyOrder(orderingSource), "count changed during enumeration", "Dishonest ordering Count must stop on the first unexpected element.");
            if (orderingSource.MoveNextCalls != 2)
                throw new Exception("Topological ordering must stop immediately after observing the first element beyond its trustworthy known Count.");
        }

        private static void PureStreamingStillStopsAtIndependentBoundary()
        {
            var rebuildSource = new StreamingElements(MaxElementInputCount + 2);
            ExpectInvalidOperation(() => new DependencyGraph().Rebuild(rebuildSource), "exceeds the supported", "Pure-streaming rebuild input must retain the independent element limit.");
            if (rebuildSource.MoveNextCalls != MaxElementInputCount + 1)
                throw new Exception("Pure-streaming rebuild must stop immediately after observing raw element 10,001.");

            var orderingSource = new StreamingElements(MaxElementInputCount + 2);
            ExpectInvalidOperation(() => new DependencyGraph().TopologicalDirtyOrder(orderingSource), "exceeds the supported", "Pure-streaming ordering input must retain the independent element limit.");
            if (orderingSource.MoveNextCalls != MaxElementInputCount + 1)
                throw new Exception("Pure-streaming ordering must stop immediately after observing raw element 10,001.");
        }

        private static ProjectElement Element(string id)
        {
            return new ProjectElement(id, ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception(message + " Actual diagnostic: " + ex.Message);
                return;
            }
            throw new Exception(message);
        }

        private sealed class MultiCountCollection : ICollection<ProjectElement>, IReadOnlyCollection<ProjectElement>, ICollection
        {
            private readonly ProjectElement[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            public MultiCountCollection(ProjectElement[] items, int genericCount, int readOnlyCount, int nonGenericCount, bool throwOnEnumeration)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            public bool EnumerationRequested { get; private set; }
            public int EnumerationRequestCount { get; private set; }
            int ICollection<ProjectElement>.Count => _genericCount;
            int IReadOnlyCollection<ProjectElement>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<ProjectElement>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationRequested = true;
                EnumerationRequestCount++;
                if (_throwOnEnumeration) throw new Exception("Enumerator must not be requested.");
                return ((IEnumerable<ProjectElement>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<ProjectElement>.Add(ProjectElement item) => throw new NotSupportedException();
            void ICollection<ProjectElement>.Clear() => throw new NotSupportedException();
            bool ICollection<ProjectElement>.Contains(ProjectElement item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<ProjectElement>.CopyTo(ProjectElement[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<ProjectElement>.Remove(ProjectElement item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }

        private sealed class NonGenericCountEnumerable : IEnumerable<ProjectElement>, ICollection
        {
            private readonly int _count;

            public NonGenericCountEnumerable(int count) { _count = count; }
            public bool EnumerationRequested { get; private set; }
            public int Count => _count;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationRequested = true;
                throw new Exception("Enumerator must not be requested for oversized known-count input.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class DishonestReadOnlyCollection : IReadOnlyCollection<ProjectElement>
        {
            private readonly int _actualCount;
            private readonly int _reportedCount;

            public DishonestReadOnlyCollection(int actualCount, int reportedCount)
            {
                _actualCount = actualCount;
                _reportedCount = reportedCount;
            }

            public int Count => _reportedCount;
            public int MoveNextCalls { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator() => new Enumerator(this, _actualCount, "DISHONEST");
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly DishonestReadOnlyCollection _owner;
                private readonly int _actualCount;
                private readonly string _prefix;
                private int _index = -1;

                public Enumerator(DishonestReadOnlyCollection owner, int actualCount, string prefix)
                {
                    _owner = owner;
                    _actualCount = actualCount;
                    _prefix = prefix;
                }

                public ProjectElement Current { get; private set; } = null!;
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index >= _actualCount) return false;
                    Current = Element(_prefix + "-" + _index);
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StreamingElements : IEnumerable<ProjectElement>
        {
            private readonly int _actualCount;

            public StreamingElements(int actualCount)
            {
                _actualCount = actualCount;
            }

            public int MoveNextCalls { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly StreamingElements _owner;
                private int _index = -1;

                public Enumerator(StreamingElements owner)
                {
                    _owner = owner;
                }

                public ProjectElement Current { get; private set; } = null!;
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index >= _owner._actualCount) return false;
                    Current = Element("STREAM-" + _index);
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
