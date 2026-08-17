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
            RejectsDishonestKnownCountSourcesAtLimitPlusOne();
            RebuildStopsLazySourceAtLimitPlusOneAndPreservesCommittedGraph();
            DirtyOrderStopsLazySourceAtLimitPlusOne();
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
            if (known.Enumerated)
                throw new InvalidOperationException("DependencyGraphBoundedInputSmoke known oversized rebuild enumerated the source.");

            ThrowsLimit(() => graph.TopologicalDirtyOrder(known), "known oversized dirty order");
            if (known.Enumerated)
                throw new InvalidOperationException("DependencyGraphBoundedInputSmoke known oversized dirty order enumerated the source.");
        }

        private static void RejectsKnownReadOnlyOversizedSourcesBeforeEnumeration()
        {
            var known = new OversizedKnownReadOnlyCollection();
            var graph = new DependencyGraph();

            ThrowsLimit(() => graph.Rebuild(known), "known read-only oversized rebuild");
            if (known.Enumerated)
                throw new InvalidOperationException("DependencyGraphBoundedInputSmoke known read-only oversized rebuild enumerated the source.");

            ThrowsLimit(() => graph.TopologicalDirtyOrder(known), "known read-only oversized dirty order");
            if (known.Enumerated)
                throw new InvalidOperationException("DependencyGraphBoundedInputSmoke known read-only oversized dirty order enumerated the source.");
        }

        private static void RejectsDishonestKnownCountSourcesAtLimitPlusOne()
        {
            var graph = new DependencyGraph();
            var rebuild = new DishonestKnownCollection(Limit + 5);
            ThrowsLimit(() => graph.Rebuild(rebuild), "dishonest-count rebuild");
            Equal(Limit + 1, rebuild.Seen, "dishonest-count rebuild enumeration count");

            var dirtyOrder = new DishonestKnownCollection(Limit + 5);
            ThrowsLimit(() => graph.TopologicalDirtyOrder(dirtyOrder), "dishonest-count dirty order");
            Equal(Limit + 1, dirtyOrder.Seen, "dishonest-count dirty-order enumeration count");
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

        private sealed class DishonestKnownCollection : ICollection<ProjectElement>
        {
            private readonly int _actualCount;

            public DishonestKnownCollection(int actualCount)
            {
                _actualCount = actualCount;
            }

            public int Count => 1;
            public bool IsReadOnly => true;
            public int Seen { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                for (var index = 0; index < _actualCount; index++)
                {
                    Seen++;
                    yield return new ProjectElement("DISHONEST-" + index, ElementCategory.CustomQuantity);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => false;
            public void CopyTo(ProjectElement[] array, int arrayIndex) { }
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
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
    }
}
