using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphCurrentCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RebuildCurrentCountDriftPreemptsMalformedDependencyAndPreservesGraph();
            OrderingCurrentCountDriftPreemptsMalformedDependency();
        }

        private static void RebuildCurrentCountDriftPreemptsMalformedDependencyAndPreservesGraph()
        {
            var graph = new DependencyGraph();
            var seed = new ProjectElement("SEED", ElementCategory.Beam);
            graph.Rebuild(new[] { seed });

            var hostile = new CurrentDriftCollection(MalformedElement(), admittedCount: 1, driftedCount: 2);
            ThrowsContaining(
                () => graph.Rebuild(hostile),
                "element count changed during enumeration");

            Equal(1, hostile.MoveNextCalls, "Rebuild MoveNext calls");
            Equal(1, hostile.CurrentReads, "Rebuild Current reads");
            if (!graph.TryGetElement("SEED", out var actual) || !ReferenceEquals(seed, actual))
                throw new InvalidOperationException("DependencyGraph rebuild Count drift replaced previously committed graph state.");
            if (graph.TryGetElement("BROKEN", out _))
                throw new InvalidOperationException("DependencyGraph rebuild Count drift leaked staged hostile input.");
        }

        private static void OrderingCurrentCountDriftPreemptsMalformedDependency()
        {
            var graph = new DependencyGraph();
            var hostile = new CurrentDriftCollection(MalformedElement(), admittedCount: 1, driftedCount: 2);

            ThrowsContaining(
                () => graph.TopologicalDirtyOrder(hostile),
                "element count changed during enumeration");

            Equal(1, hostile.MoveNextCalls, "Ordering MoveNext calls");
            Equal(1, hostile.CurrentReads, "Ordering Current reads");
        }

        private static ProjectElement MalformedElement()
        {
            var element = new ProjectElement("BROKEN", ElementCategory.Beam);
            element.DependsOn.Add(" ");
            return element;
        }

        private static void ThrowsContaining(Action action, string token)
        {
            try
            {
                action();
            }
            catch (Exception ex) when (ex.Message.Contains(token, StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException("Expected exception containing: " + token);
        }

        private static void Equal(int expected, int actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + " expected " + expected + " but got " + actual + ".");
        }

        private sealed class CurrentDriftCollection : ICollection<ProjectElement>
        {
            private readonly ProjectElement _item;
            private readonly int _driftedCount;
            private int _count;

            internal CurrentDriftCollection(ProjectElement item, int admittedCount, int driftedCount)
            {
                _item = item;
                _count = admittedCount;
                _driftedCount = driftedCount;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public int Count => _count;
            public bool IsReadOnly => true;

            public IEnumerator<ProjectElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => ReferenceEquals(_item, item);
            public void CopyTo(ProjectElement[] array, int arrayIndex) => array[arrayIndex] = _item;
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly CurrentDriftCollection _owner;
                private bool _moved;

                internal Enumerator(CurrentDriftCollection owner) => _owner = owner;

                public ProjectElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._count = _owner._driftedCount;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
