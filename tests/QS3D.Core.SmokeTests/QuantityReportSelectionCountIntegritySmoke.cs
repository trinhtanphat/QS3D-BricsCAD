using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportSelectionCountIntegritySmoke
    {
        private const int SelectionBound = 10000;

        internal static void Run()
        {
            NegativeNonGenericCountFailsBeforeEnumeration();
            OversizedReadOnlyCountFailsBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            UnderTraversalFailsClosed();
            OverTraversalFailsClosed();
            HonestKnownCountPreservesSelectionSemantics();
            PureStreamRejectsItem10001();
            PureStreamExactBoundReachesIdentityValidation();
        }

        private static void NegativeNonGenericCountFailsBeforeEnumeration()
        {
            var source = new NonGenericCountSequence(-1);
            ExpectInvalidOperation(() => ProjectQuantityReportBuilder.Detail(EmptyProject("negative"), source));
            if (source.EnumeratorEntered)
                throw new Exception("Quantity report selection must reject negative non-generic Count before enumeration.");
        }

        private static void OversizedReadOnlyCountFailsBeforeEnumeration()
        {
            var source = new ReadOnlyCountSequence(SelectionBound + 1, Array.Empty<string>(), throwOnEnumeration: true);
            ExpectInvalidOperation(() => ProjectQuantityReportBuilder.Detail(EmptyProject("oversize"), source));
            if (source.EnumeratorEntered)
                throw new Exception("Quantity report selection must reject oversized IReadOnlyCollection Count before enumeration.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new ConflictingCountSequence();
            ExpectInvalidOperation(() => ProjectQuantityReportBuilder.Detail(EmptyProject("conflict"), source));
            if (source.EnumeratorEntered)
                throw new Exception("Quantity report selection must reject conflicting generic/read-only Counts before enumeration.");
        }

        private static void UnderTraversalFailsClosed()
        {
            var source = new ReadOnlyCountSequence(2, new[] { "E1" });
            ExpectInvalidOperation(() => ProjectQuantityReportBuilder.Detail(EmptyProject("under"), source));
        }

        private static void OverTraversalFailsClosed()
        {
            var source = new ReadOnlyCountSequence(1, new[] { "E1", "E2" });
            ExpectInvalidOperation(() => ProjectQuantityReportBuilder.Detail(EmptyProject("over"), source));
        }

        private static void HonestKnownCountPreservesSelectionSemantics()
        {
            var project = ProjectWithElements("honest", "E1");
            var source = new ReadOnlyCountSequence(1, new[] { " e1 " });
            var rows = ProjectQuantityReportBuilder.Detail(project, source);
            if (rows.Count != 1 || rows[0].ElementIds.Count != 1 || rows[0].ElementIds[0] != "E1")
                throw new Exception("Quantity report honest Count control must preserve case-insensitive, trimmed selected-element lookup.");
        }

        private static void PureStreamRejectsItem10001()
        {
            ExpectInvalidOperation(() => ProjectQuantityReportBuilder.Detail(
                EmptyProject("stream-overflow"),
                PureIds(SelectionBound + 1)));
        }

        private static void PureStreamExactBoundReachesIdentityValidation()
        {
            try
            {
                ProjectQuantityReportBuilder.Detail(EmptyProject("stream-boundary"), PureIds(SelectionBound));
            }
            catch (KeyNotFoundException)
            {
                return;
            }

            throw new Exception("Quantity report pure-stream exact bound must be accepted by the traversal guard and proceed to normal id validation.");
        }

        private static IEnumerable<string> PureIds(int count)
        {
            for (var i = 0; i < count; i++)
                yield return "S" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static ProjectState EmptyProject(string id)
        {
            return new ProjectState("quantity-selection-" + id, "Quantity selection " + id);
        }

        private static ProjectState ProjectWithElements(string id, params string[] elementIds)
        {
            var project = EmptyProject(id);
            project.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("zone", "Zone"));
            var family = new ProjectFamily("family", "Family", ElementCategory.Slab);
            project.Families.Add(family);
            foreach (var elementId in elementIds)
                project.Elements.Add(new ProjectElement(elementId, ElementCategory.Slab, family.Id, "floor", "zone"));
            return project;
        }

        private static void ExpectInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception("Expected InvalidOperationException.");
        }

        private sealed class EnumerationEnteredException : Exception
        {
        }

        private sealed class NonGenericCountSequence : IEnumerable<string>, ICollection
        {
            internal NonGenericCountSequence(int count)
            {
                Count = count;
            }

            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            internal bool EnumeratorEntered { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorEntered = true;
                throw new EnumerationEnteredException();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyCountSequence : IReadOnlyCollection<string>
        {
            private readonly IReadOnlyList<string> _items;
            private readonly bool _throwOnEnumeration;

            internal ReadOnlyCountSequence(int count, IReadOnlyList<string> items, bool throwOnEnumeration = false)
            {
                Count = count;
                _items = items;
                _throwOnEnumeration = throwOnEnumeration;
            }

            public int Count { get; }
            internal bool EnumeratorEntered { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorEntered = true;
                if (_throwOnEnumeration)
                    throw new EnumerationEnteredException();
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ConflictingCountSequence : ICollection<string>, IReadOnlyCollection<string>
        {
            int ICollection<string>.Count => 1;
            int IReadOnlyCollection<string>.Count => 2;
            bool ICollection<string>.IsReadOnly => true;
            internal bool EnumeratorEntered { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorEntered = true;
                throw new EnumerationEnteredException();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Contains(string item) => false;
            void ICollection<string>.CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
        }
    }
}