using System;
using System.Collections;
using System.Collections.Generic;
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
            NonCanonicalDetailSelectionFailsClosed();
            NonCanonicalGroupSelectionFailsClosed();
            NonCanonicalDuplicateAliasFailsClosed();
            BlankAndNullSelectionIdsFailClosed();
            CanonicalCaseInsensitiveSelectionStillWorks();
            CanonicalCaseInsensitiveDuplicateStillFailsClosed();
            PureStreamRejectsItem10001();
            ExactKnownCountEntersEnumeration();
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
            var project = ProjectWithElements("under", "E1");
            var source = new ReadOnlyCountSequence(2, new[] { "E1" });
            ExpectInvalidOperation(() => ProjectQuantityReportBuilder.Detail(project, source));
        }

        private static void OverTraversalFailsClosed()
        {
            var project = ProjectWithElements("over", "E1", "E2");
            var source = new ReadOnlyCountSequence(1, new[] { "E1", "E2" });
            ExpectInvalidOperation(() => ProjectQuantityReportBuilder.Detail(project, source));
        }

        private static void NonCanonicalDetailSelectionFailsClosed()
        {
            var project = ProjectWithElements("noncanonical-detail", "E1");
            var source = new ReadOnlyCountSequence(1, new[] { " e1 " });
            ExpectArgumentException(() => ProjectQuantityReportBuilder.Detail(project, source));
        }

        private static void NonCanonicalGroupSelectionFailsClosed()
        {
            var project = ProjectWithElements("noncanonical-group", "E1");
            var source = new ReadOnlyCountSequence(1, new[] { "E1 " });
            ExpectArgumentException(() => ProjectQuantityReportBuilder.Group(project, source));
        }

        private static void NonCanonicalDuplicateAliasFailsClosed()
        {
            var project = ProjectWithElements("noncanonical-alias", "E1");
            var source = new ReadOnlyCountSequence(2, new[] { "E1", " E1" });
            ExpectArgumentException(() => ProjectQuantityReportBuilder.Detail(project, source));
        }

        private static void BlankAndNullSelectionIdsFailClosed()
        {
            var project = ProjectWithElements("blank-null", "E1");
            ExpectArgumentException(() => ProjectQuantityReportBuilder.Detail(
                project,
                new ReadOnlyCountSequence(1, new[] { "   " })));
            ExpectArgumentException(() => ProjectQuantityReportBuilder.Detail(
                project,
                new ReadOnlyCountSequence(1, new[] { (string)null! })));
        }

        private static void CanonicalCaseInsensitiveSelectionStillWorks()
        {
            var project = ProjectWithElements("canonical-case", "E1");
            var source = new ReadOnlyCountSequence(1, new[] { "e1" });
            var rows = ProjectQuantityReportBuilder.Detail(project, source);
            if (rows.Count != 1 || rows[0].ElementIds.Count != 1 || rows[0].ElementIds[0] != "E1")
                throw new Exception("Quantity report canonical selection must preserve case-insensitive semantic lookup without rewriting the input token.");
        }

        private static void CanonicalCaseInsensitiveDuplicateStillFailsClosed()
        {
            var project = ProjectWithElements("canonical-duplicate", "E1");
            var source = new ReadOnlyCountSequence(2, new[] { "E1", "e1" });
            ExpectArgumentException(() => ProjectQuantityReportBuilder.Detail(project, source));
        }

        private static void PureStreamRejectsItem10001()
        {
            var project = EmptyProject("stream-overflow");
            project.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("zone", "Zone"));
            var family = new ProjectFamily("family", "Family", ElementCategory.Slab);
            project.Families.Add(family);

            var ids = new string[SelectionBound];
            for (var i = 0; i < SelectionBound; i++)
            {
                var id = "S" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                ids[i] = id;
                project.Elements.Add(new ProjectElement(id, ElementCategory.Slab, family.Id, "floor", "zone"));
            }

            ExpectInvalidOperation(() => ProjectQuantityReportBuilder.Detail(project, StableIdsThenOverflow(ids)));
        }

        private static IEnumerable<string> StableIdsThenOverflow(IReadOnlyList<string> ids)
        {
            for (var i = 0; i < ids.Count; i++)
                yield return ids[i];
            yield return "S-overflow";
        }

        private static void ExactKnownCountEntersEnumeration()
        {
            var source = new ReadOnlyCountSequence(SelectionBound, Array.Empty<string>(), throwOnEnumeration: true);
            try
            {
                ProjectQuantityReportBuilder.Detail(EmptyProject("exact-bound"), source);
            }
            catch (EnumerationEnteredException)
            {
                if (!source.EnumeratorEntered)
                    throw new Exception("Quantity report exact-bound Count control did not enter enumeration.");
                return;
            }

            throw new Exception("Quantity report exact-bound known Count must pass preflight and enter enumeration.");
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

        private static void ExpectArgumentException(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new Exception("Expected ArgumentException.");
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