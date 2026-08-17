using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphBoundedInputSmoke
    {
        private const int Limit = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            AcceptsExactBoundAndPreservesOrdinaryGraphBehavior();
            RejectsKnownOversizedSourcesBeforeEnumeration();
            RejectsKnownReadOnlyOversizedSourcesBeforeEnumeration();
            RejectsNonGenericOversizedSourcesBeforeEnumeration();
            RejectsConflictingKnownCountsBeforeEnumeration();
            CapacityFailurePrecedesCountConflict();
            RejectsNegativeKnownCountBeforeEnumeration();
            AcceptsConsistentMultipleCountContracts();
            RebuildStopsLazySourceAtLimitPlusOneAndPreservesCommittedGraph();
            DirtyOrderStopsLazySourceAtLimitPlusOne();
            DishonestLowCountsStillStopAtLimitPlusOne();
        }

        private static void AcceptsExactBoundAndPreservesOrdinaryGraphBehavior()
        {
            var exact = new List<ProjectElement>(Limit);
            for (var index = 0; index < Limit; index++)
                exact.Add(new ProjectElement("E" + index, ElementCategory.CustomQuantity));

            exact[1].DependsOn.Add("E0");
            var graph = new DependencyGraph();
            graph.Rebuild(exact);

            var direct = graph.GetDirectDependents("E0");
            Equal(1, direct.Count, "exact-bound direct dependent count");
            Equal("E1", direct[0], "exact-bound direct dependent id");

            var dirtyOrder = graph.TopologicalDirtyOrder(exact);
            Equal(Limit, dirtyOrder.Count, "exact-bound dirty order count");
            Equal("E0", dirtyOrder[0].Id, "dependency must precede dependent in dirty order");
            Equal("E1", dirtyOrder[1].Id, "dependent must follow dependency in dirty order");
        }

        private static void RejectsKnownOversizedSourcesBeforeEnumeration()
        {
            var known = new OversizedKnownCollection();
            var graph = new DependencyGraph();

            ThrowsLimit(() => graph.Rebuild(known), "known oversized rebuild");
            Equal(false, known.Enumerated, "known oversized rebuild enumeration");

            ThrowsLimit(() => graph.TopologicalDirtyOrder(known), "known oversized dirty order");
            Equal(false, known.Enumerated, "known oversized dirty-order enumeration");
        }

        private static void RejectsKnownReadOnlyOversizedSourcesBeforeEnumeration()
        {
            var known = new OversizedKnownReadOnlyCollection();
            var graph = new DependencyGraph();

            ThrowsLimit(() => graph.Rebuild(known), "known read-only oversized rebuild");
            Equal(false, known.Enumerated, "known read-only oversized rebuild enumeration");

            ThrowsLimit(() => graph.TopologicalDirtyOrder(known), "known read-only oversized dirty order");
            Equal(false, known.Enumerated, "known read-only oversized dirty-order enumeration");
        }

        private static void RejectsNonGenericOversizedSourcesBeforeEnumeration()
        {
            var known = new NonGenericCountCollection(Limit + 1, 0);
            var graph = new DependencyGraph();

            ThrowsLimit(() => graph.Rebuild(known), "non-generic oversized rebuild");
            Equal(0, known.EnumerationRequests, "non-generic oversized rebuild enumeration requests");

            ThrowsLimit(() => graph.TopologicalDirtyOrder(known), "non-generic oversized dirty order");
            Equal(0, known.EnumerationRequests, "non-generic oversized dirty-order enumeration requests");
        }

        private static void RejectsConflictingKnownCountsBeforeEnumeration()
        {
            var source = new MultiContractCollection(1, 2, 1, 1);
            var graph = new DependencyGraph();

            ThrowsConflict(() => graph.Rebuild(source), "conflicting-count rebuild");
            Equal(0, source.EnumerationRequests, "conflicting-count rebuild enumeration requests");
            Equal(1, source.GenericCountReads, "conflicting-count generic count reads");
            Equal(1, source.ReadOnlyCountReads, "conflicting-count read-only count reads");
            Equal(1, source.NonGenericCountReads, "conflicting-count non-generic count reads");

            ThrowsConflict(() => graph.TopologicalDirtyOrder(source), "conflicting-count dirty order");
            Equal(0, source.EnumerationRequests, "conflicting-count dirty-order enumeration requests");
        }

        private static void CapacityFailurePrecedesCountConflict()
        {
            var source = new MultiContractCollection(1, 2, Limit + 1, 1);
            var graph = new DependencyGraph();

            ThrowsLimit(() => graph.Rebuild(source), "capacity-over-conflict rebuild");
            Equal(0, source.EnumerationRequests, "capacity-over-conflict enumeration requests");
            Equal(1, source.GenericCountReads, "capacity-over-conflict generic count reads");
            Equal(1, source.ReadOnlyCountReads, "capacity-over-conflict read-only count reads");
            Equal(1, source.NonGenericCountReads, "capacity-over-conflict non-generic count reads");
        }

        private static void RejectsNegativeKnownCountBeforeEnumeration()
        {
            var source = new MultiContractCollection(1, -1, 1, 1);
            var graph = new DependencyGraph();

            ThrowsNegative(() => graph.Rebuild(source), "negative-count rebuild");
            Equal(0, source.EnumerationRequests, "negative-count enumeration requests");
        }

        private static void AcceptsConsistentMultipleCountContracts()
        {
            var source = new MultiContractCollection(2, 2, 2, 2, index =>
            {
                var element = new ProjectElement(index == 0 ? "ROOT" : "CHILD", ElementCategory.CustomQuantity);
                if (index == 1) element.DependsOn.Add("ROOT");
                return element;
            });
            var graph = new DependencyGraph();
            graph.Rebuild(source);

            Equal(1, source.EnumerationRequests, "consistent-count rebuild enumeration requests");
            Equal(2, source.Seen, "consistent-count rebuild seen count");
            var direct = graph.GetDirectDependents("ROOT");
            Equal(1, direct.Count, "consistent-count dependent count");
            Equal("CHILD", direct[0], "consistent-count dependent id");

            var orderSource = new MultiContractCollection(2, 2, 2, 2, index =>
            {
                var element = new ProjectElement(index == 0 ? "ORDER-ROOT" : "ORDER-CHILD", ElementCategory.CustomQuantity);
                if (index == 1) element.DependsOn.Add("ORDER-ROOT");
                return element;
            });
            var order = graph.TopologicalDirtyOrder(orderSource);
            Equal(2, order.Count, "consistent-count dirty order count");
            Equal("ORDER-ROOT", order[0].Id, "consistent-count dependency order root");
            Equal("ORDER-CHILD", order[1].Id, "consistent-count dependency order child");
        }

        private static void RebuildStopsLazySourceAtLimitPlusOneAndPreservesCommittedGraph()
        {
            var root = new ProjectElement("ROOT", ElementCategory.CustomQuantity);
            var dependent = new ProjectElement("DEPENDENT", ElementCategory.CustomQuantity);
            dependent.DependsOn.Add("ROOT");
            var graph = new DependencyGraph();
            graph.Rebuild(new[] { root, dependent });

            var source = new CountingElements(Limit + 5);
            ThrowsLimit(() => graph.Rebuild(source), "lazy rebuild");
            Equal(Limit + 1, source.Seen, "lazy rebuild enumeration count");

            var direct = graph.GetDirectDependents("ROOT");
            Equal(1, direct.Count, "rebuild rollback direct dependent count");
            Equal("DEPENDENT", direct[0], "rebuild rollback direct dependent id");
            if (!graph.TryGetElement("DEPENDENT", out var retained) || !ReferenceEquals(dependent, retained))
                throw new InvalidOperationException("DependencyGraphBoundedInputSmoke oversized rebuild replaced the previously committed graph.");
        }

        private static void DirtyOrderStopsLazySourceAtLimitPlusOne()
        {
            var source = new CountingElements(Limit + 5);
            var graph = new DependencyGraph();
            ThrowsLimit(() => graph.TopologicalDirtyOrder(source), "lazy dirty order");
            Equal(Limit + 1, source.Seen, "lazy dirty-order enumeration count");
        }

        private static void DishonestLowCountsStillStopAtLimitPlusOne()
        {
            var rebuildSource = new MultiContractCollection(1, 1, 1, Limit + 5);
            var graph = new DependencyGraph();
            ThrowsLimit(() => graph.Rebuild(rebuildSource), "dishonest-count rebuild");
            Equal(Limit + 1, rebuildSource.Seen, "dishonest-count rebuild seen count");

            var orderSource = new MultiContractCollection(1, 1, 1, Limit + 5);
            ThrowsLimit(() => graph.TopologicalDirtyOrder(orderSource), "dishonest-count dirty order");
            Equal(Limit + 1, orderSource.Seen, "dishonest-count dirty-order seen count");
        }

        private static void ThrowsLimit(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("10000", StringComparison.Ordinal) >= 0 &&
                    ex.Message.IndexOf("limit", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("DependencyGraphBoundedInputSmoke " + label + " returned the wrong diagnostic: " + ex.Message, ex);
            }

            throw new InvalidOperationException("DependencyGraphBoundedInputSmoke " + label + " did not fail closed.");
        }

        private static void ThrowsConflict(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("conflicting known element counts", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("DependencyGraphBoundedInputSmoke " + label + " returned the wrong diagnostic: " + ex.Message, ex);
            }

            throw new InvalidOperationException("DependencyGraphBoundedInputSmoke " + label + " did not reject conflicting known counts.");
        }

        private static void ThrowsNegative(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("negative known element count", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("DependencyGraphBoundedInputSmoke " + label + " returned the wrong diagnostic: " + ex.Message, ex);
            }

            throw new InvalidOperationException("DependencyGraphBoundedInputSmoke " + label + " did not reject negative known count evidence.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("DependencyGraphBoundedInputSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountingElements : IEnumerable<ProjectElement>
        {
            private readonly int _count;

            public CountingElements(int count)
            {
                _count = count;
            }

            public int Seen { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                for (var index = 0; index < _count; index++)
                {
                    Seen++;
                    yield return new ProjectElement("LAZY-" + index, ElementCategory.CustomQuantity);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class OversizedKnownCollection : ICollection<ProjectElement>
        {
            public int Count => Limit + 1;
            public bool IsReadOnly => true;
            public bool Enumerated { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                Enumerated = true;
                return ((IEnumerable<ProjectElement>)Array.Empty<ProjectElement>()).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => false;
            public void CopyTo(ProjectElement[] array, int arrayIndex) { }
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
        }

        private sealed class OversizedKnownReadOnlyCollection : IReadOnlyCollection<ProjectElement>
        {
            public int Count => Limit + 1;
            public bool Enumerated { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                Enumerated = true;
                return ((IEnumerable<ProjectElement>)Array.Empty<ProjectElement>()).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericCountCollection : IEnumerable<ProjectElement>, ICollection
        {
            private readonly int _count;
            private readonly int _yieldCount;

            public NonGenericCountCollection(int count, int yieldCount)
            {
                _count = count;
                _yieldCount = yieldCount;
            }

            int ICollection.Count => _count;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            public int EnumerationRequests { get; private set; }

            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationRequests++;
                for (var index = 0; index < _yieldCount; index++)
                    yield return new ProjectElement("NON-GENERIC-" + index, ElementCategory.CustomQuantity);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class MultiContractCollection : ICollection<ProjectElement>, IReadOnlyCollection<ProjectElement>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly int _yieldCount;
            private readonly Func<int, ProjectElement> _factory;

            public MultiContractCollection(
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                int yieldCount,
                Func<int, ProjectElement>? factory = null)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _yieldCount = yieldCount;
                _factory = factory ?? (index => new ProjectElement("MULTI-" + index, ElementCategory.CustomQuantity));
            }

            int ICollection<ProjectElement>.Count
            {
                get
                {
                    GenericCountReads++;
                    return _genericCount;
                }
            }

            int IReadOnlyCollection<ProjectElement>.Count
            {
                get
                {
                    ReadOnlyCountReads++;
                    return _readOnlyCount;
                }
            }

            int ICollection.Count
            {
                get
                {
                    NonGenericCountReads++;
                    return _nonGenericCount;
                }
            }

            bool ICollection<ProjectElement>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public int GenericCountReads { get; private set; }
            public int ReadOnlyCountReads { get; private set; }
            public int NonGenericCountReads { get; private set; }
            public int EnumerationRequests { get; private set; }
            public int Seen { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationRequests++;
                for (var index = 0; index < _yieldCount; index++)
                {
                    Seen++;
                    yield return _factory(index);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            bool ICollection<ProjectElement>.Contains(ProjectElement item) => false;
            void ICollection<ProjectElement>.CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            void ICollection<ProjectElement>.Add(ProjectElement item) => throw new NotSupportedException();
            void ICollection<ProjectElement>.Clear() => throw new NotSupportedException();
            bool ICollection<ProjectElement>.Remove(ProjectElement item) => throw new NotSupportedException();
        }
    }
}