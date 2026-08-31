using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyImpactPlannerKnownCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            InvalidKnownCountsFailBeforeEnumeration();
            TraversalMustMatchKnownCount();
            PostTraversalKnownCountDriftFailsClosed();
            HonestCountedAndStreamingSourcesRemainSupported();
        }

        private static void InvalidKnownCountsFailBeforeEnumeration()
        {
            var project = Fixture();
            var max = project.Elements.Count;

            var oversized = new CountContractRoots(new[] { "ROOT" }, max + 1, max + 1, max + 1, throwOnEnumeration: true);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(project, oversized));
            Equal(false, oversized.EnumerationStarted);

            var negative = new CountContractRoots(new[] { "ROOT" }, -1, -1, -1, throwOnEnumeration: true);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(project, negative));
            Equal(false, negative.EnumerationStarted);

            var conflicting = new CountContractRoots(new[] { "ROOT" }, 1, 2, 1, throwOnEnumeration: true);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(project, conflicting));
            Equal(false, conflicting.EnumerationStarted);
        }

        private static void TraversalMustMatchKnownCount()
        {
            var project = Fixture();

            var underYield = new CountContractRoots(new[] { "ROOT" }, 2, 2, 2, throwOnEnumeration: false);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(project, underYield));
            Equal(true, underYield.EnumerationStarted);

            var overYield = new CountContractRoots(new[] { "ROOT", "B" }, 1, 1, 1, throwOnEnumeration: false);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(project, overYield));
            Equal(true, overYield.EnumerationStarted);
        }

        private static void PostTraversalKnownCountDriftFailsClosed()
        {
            var project = Fixture();

            var changed = new DriftingCountContractRoots(new[] { "ROOT" }, 1, 1, 1, 2, 2, 2);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(project, changed));
            Equal(true, changed.EnumerationCompleted);

            var conflicting = new DriftingCountContractRoots(new[] { "ROOT" }, 1, 1, 1, 1, 2, 1);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(project, conflicting));
            Equal(true, conflicting.EnumerationCompleted);

            var negative = new DriftingCountContractRoots(new[] { "ROOT" }, 1, 1, 1, -1, -1, -1);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(project, negative));
            Equal(true, negative.EnumerationCompleted);
        }

        private static void HonestCountedAndStreamingSourcesRemainSupported()
        {
            var project = Fixture();
            var counted = new CountContractRoots(new[] { "ROOT" }, 1, 1, 1, throwOnEnumeration: false);
            var countedPlan = new DependencyImpactPlanner().Plan(project, counted);
            Equal(new[] { "ROOT" }, countedPlan.RootElementIds.ToArray());

            IEnumerable<string> StreamingRoots()
            {
                yield return "ROOT";
            }

            var streamingPlan = new DependencyImpactPlanner().Plan(project, StreamingRoots());
            Equal(new[] { "ROOT" }, streamingPlan.RootElementIds.ToArray());
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-IMPACT-COUNT", "Dependency impact count contracts");
            project.Elements.Add(Element("ROOT"));
            project.Elements.Add(Element("B", "ROOT"));
            project.Elements.Add(Element("A", "ROOT"));
            return project;
        }

        private static ProjectElement Element(string id, params string[] dependencies)
        {
            var element = new ProjectElement(id, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            foreach (var dependency in dependencies) element.DependsOn.Add(dependency);
            return element;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (expected is Array expectedArray && actual is Array actualArray)
            {
                if (expectedArray.Length != actualArray.Length) throw new Exception("Array lengths differ.");
                for (var i = 0; i < expectedArray.Length; i++)
                    if (!Equals(expectedArray.GetValue(i), actualArray.GetValue(i))) throw new Exception("Array values differ at index " + i + ".");
                return;
            }
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private sealed class CountContractRoots : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly IReadOnlyList<string> _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            public CountContractRoots(
                IEnumerable<string> items,
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                bool throwOnEnumeration)
            {
                _items = (items ?? Array.Empty<string>()).ToArray();
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            public bool EnumerationStarted { get; private set; }
            public int Count => _genericCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationStarted = true;
                if (_throwOnEnumeration)
                    throw new Exception("Source should have been rejected before enumeration.");
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Contains(string item) => _items.Contains(item);
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class DriftingCountContractRoots : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly IReadOnlyList<string> _items;
            private readonly int _postGenericCount;
            private readonly int _postReadOnlyCount;
            private readonly int _postNonGenericCount;
            private int _genericCount;
            private int _readOnlyCount;
            private int _nonGenericCount;

            public DriftingCountContractRoots(
                IEnumerable<string> items,
                int initialGenericCount,
                int initialReadOnlyCount,
                int initialNonGenericCount,
                int postGenericCount,
                int postReadOnlyCount,
                int postNonGenericCount)
            {
                _items = (items ?? Array.Empty<string>()).ToArray();
                _genericCount = initialGenericCount;
                _readOnlyCount = initialReadOnlyCount;
                _nonGenericCount = initialNonGenericCount;
                _postGenericCount = postGenericCount;
                _postReadOnlyCount = postReadOnlyCount;
                _postNonGenericCount = postNonGenericCount;
            }

            public bool EnumerationCompleted { get; private set; }
            public int Count => _genericCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<string> GetEnumerator() => Enumerate().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<string> Enumerate()
            {
                foreach (var item in _items) yield return item;
                _genericCount = _postGenericCount;
                _readOnlyCount = _postReadOnlyCount;
                _nonGenericCount = _postNonGenericCount;
                EnumerationCompleted = true;
            }

            public bool Contains(string item) => _items.Contains(item);
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }
    }
}
