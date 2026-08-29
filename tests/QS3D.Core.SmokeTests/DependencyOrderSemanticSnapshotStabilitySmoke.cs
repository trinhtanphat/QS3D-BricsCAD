using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyOrderSemanticSnapshotStabilitySmoke
    {
        internal static void Run()
        {
            DirtyMutationFromLaterMoveNextFailsClosed();
            DependencyMutationFromLaterMoveNextFailsClosed();
            StableSemanticStatePreservesTopologicalOrder();
        }

        private static void DirtyMutationFromLaterMoveNextFailsClosed()
        {
            var first = Element("A", dirty: false);
            var second = Element("B", dirty: true);
            var source = new MutatingEnumerable(new[] { first, second }, () => first.MarkDirty(ElementDirtyFlags.Quantity));
            var error = Throws<InvalidOperationException>(() => new DependencyGraph().TopologicalDirtyOrder(source));
            Contains(error.Message, "changed after semantic element A was admitted");
            Equal(2, source.CurrentReads);
            Equal(3, source.MoveNextCalls);
        }

        private static void DependencyMutationFromLaterMoveNextFailsClosed()
        {
            var first = Element("A", dirty: true);
            var second = Element("B", dirty: true);
            var source = new MutatingEnumerable(new[] { first, second }, () => first.DependsOn.Add("B"));
            var error = Throws<InvalidOperationException>(() => new DependencyGraph().TopologicalDirtyOrder(source));
            Contains(error.Message, "changed after semantic element A was admitted");
            Equal(2, source.CurrentReads);
            Equal(3, source.MoveNextCalls);
        }

        private static void StableSemanticStatePreservesTopologicalOrder()
        {
            var first = Element("A", dirty: true);
            var second = Element("B", dirty: true);
            second.DependsOn.Add("A");
            var source = new MutatingEnumerable(new[] { second, first }, null);
            var ordered = new DependencyGraph().TopologicalDirtyOrder(source);
            Equal(2, ordered.Count);
            Equal("A", ordered[0].Id);
            Equal("B", ordered[1].Id);
        }

        private static ProjectElement Element(string id, bool dirty)
        {
            var element = new ProjectElement(id, ElementCategory.Grid, string.Empty, string.Empty, string.Empty);
            element.MarkClean(ElementDirtyFlags.All);
            if (dirty) element.MarkDirty(ElementDirtyFlags.Quantity);
            return element;
        }

        private sealed class MutatingEnumerable : IReadOnlyCollection<ProjectElement>
        {
            private readonly ProjectElement[] _values;
            private readonly Action? _mutation;

            internal MutatingEnumerable(ProjectElement[] values, Action? mutation)
            {
                _values = values;
                _mutation = mutation;
            }

            public int Count => _values.Length;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<ProjectElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly MutatingEnumerable _owner;
                private int _index = -1;
                internal Enumerator(MutatingEnumerable owner) { _owner = owner; }
                public ProjectElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._values[_index];
                    }
                }
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index == 1) _owner._mutation?.Invoke();
                    return _index < _owner._values.Length;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static T Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T error) { return error; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Contains(string value, string expected)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected text containing '" + expected + "', got '" + (value ?? string.Empty) + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class DependencyOrderSemanticSnapshotStabilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DependencyOrderSemanticSnapshotStabilitySmoke.Run();
    }
}
