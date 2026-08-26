using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class LegacyQuantityReportIdentitySmoke
    {
        public static void Run()
        {
            var family = new FamilyDefinition("Legacy wall", ElementCategory.ArchitecturalWall, "Concrete");
            var first = new ElementInstance("Legacy-A", family, "Floor") { LengthM = 2d, GrossConcreteM3 = 1d };
            first.SourceHandles.Add("AA");
            var sameIdentityDifferentCase = new ElementInstance("legacy-a", family, "Floor") { LengthM = 3d, GrossConcreteM3 = 2d };
            sameIdentityDifferentCase.SourceHandles.Add("BB");

            var countSecond = new ElementInstance("Legacy-Count-B", family, "Floor") { LengthM = 3d, GrossConcreteM3 = 2d };
            var underTraversal = new ReadOnlyCountSequence(2, new[] { first });
            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(underTraversal));
            var overTraversal = new ReadOnlyCountSequence(1, new[] { first, countSecond });
            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(overTraversal));

            var honestKnownCount = QuantityReportBuilder.Group(new ReadOnlyCountSequence(2, new[] { first, countSecond })).Single();
            if (honestKnownCount.Count != 2 || Math.Abs(honestKnownCount.LengthM - 5d) > 1e-12)
                throw new Exception("Legacy quantity grouping must preserve valid known-Count enumeration semantics.");

            var pureStream = QuantityReportBuilder.Group(YieldElements(first, countSecond)).Single();
            if (pureStream.Count != 2 || Math.Abs(pureStream.GrossConcreteM3 - 3d) > 1e-12)
                throw new Exception("Legacy quantity grouping must preserve pure IEnumerable semantics without a known Count contract.");

            var negativeCount = new NonGenericCountSequence(-1);
            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(negativeCount));
            if (negativeCount.EnumeratorEntered)
                throw new Exception("Legacy quantity grouping must reject a negative known Count before enumeration.");

            var conflictingCounts = new ConflictingCountSequence();
            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(conflictingCounts));
            if (conflictingCounts.EnumeratorEntered)
                throw new Exception("Legacy quantity grouping must reject conflicting known Counts before enumeration.");

            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { first, first }));
            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { first, sameIdentityDifferentCase }));

            var second = new ElementInstance("Legacy-B", family, "Floor") { LengthM = 3d, GrossConcreteM3 = 2d };
            second.SourceHandles.Add(" aa ");
            second.SourceHandles.Add(" ");
            second.SourceHandles.Add("Bb");
            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { first, second }));

            second.SourceHandles.Clear();
            second.SourceHandles.Add("AA");
            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { first, second }));

            second.SourceHandles.Clear();
            second.SourceHandles.Add("Bb");
            var valid = QuantityReportBuilder.Group(new[] { first, second }).Single();
            if (valid.Count != 2 || Math.Abs(valid.LengthM - 5d) > 1e-12 || Math.Abs(valid.GrossConcreteM3 - 3d) > 1e-12)
                throw new Exception("Legacy quantity grouping must remain unchanged for distinct element identities.");
            if (valid.SourceHandles.Count != 2 || valid.SourceHandles[0] != "AA" || valid.SourceHandles[1] != "Bb")
                throw new Exception("Legacy quantity source handles must preserve canonical first-seen provenance.");
            if (valid.Material != "Concrete")
                throw new Exception("Legacy quantity rows must retain normalized material provenance.");

            var equivalentMaterialFamily = new FamilyDefinition("Legacy wall", ElementCategory.ArchitecturalWall, " concrete ");
            var equivalentMaterial = new ElementInstance("Legacy-C", equivalentMaterialFamily, "Floor") { LengthM = 1d };
            var differentMaterialFamily = new FamilyDefinition("Legacy wall", ElementCategory.ArchitecturalWall, "Steel");
            var differentMaterial = new ElementInstance("Legacy-D", differentMaterialFamily, "Floor") { LengthM = 4d };
            var materialGroups = QuantityReportBuilder.Group(new[] { first, equivalentMaterial, differentMaterial });
            if (materialGroups.Count != 2)
                throw new Exception("Legacy quantity grouping must separate different materials while merging case-equivalent material names.");
            var concreteGroup = materialGroups.Single(x => string.Equals(x.Material, "Concrete", StringComparison.OrdinalIgnoreCase));
            var steelGroup = materialGroups.Single(x => string.Equals(x.Material, "Steel", StringComparison.OrdinalIgnoreCase));
            if (concreteGroup.Count != 2 || Math.Abs(concreteGroup.LengthM - 3d) > 1e-12 || steelGroup.Count != 1 || Math.Abs(steelGroup.LengthM - 4d) > 1e-12)
                throw new Exception("Legacy material grouping totals/provenance are inconsistent.");

            ExpectArgumentException(
                () => QuantityReportBuilder.Group(new ElementInstance[] { first, null!, second }),
                "elements",
                "index: 1");

            var totals = QuantityReportTotals.FromRows(new[] { valid });
            if (totals.Count != 2 || Math.Abs(totals.LengthM - 5d) > 1e-12 || Math.Abs(totals.GrossConcreteM3 - 3d) > 1e-12)
                throw new Exception("Legacy quantity totals must remain unchanged for valid rows.");

            var countRowA = new QuantityReportRow { Count = 1, LengthM = 2d, GrossConcreteM3 = 1d };
            var countRowB = new QuantityReportRow { Count = 2, LengthM = 3d, GrossConcreteM3 = 2d };
            var honestRowCount = QuantityReportTotals.FromRows(new ReadOnlyRowCountSequence(2, new[] { countRowA, countRowB }));
            if (honestRowCount.Count != 3 || Math.Abs(honestRowCount.LengthM - 5d) > 1e-12 || Math.Abs(honestRowCount.GrossConcreteM3 - 3d) > 1e-12)
                throw new Exception("Legacy quantity totals must preserve valid known-Count row enumeration semantics.");

            ExpectThrows<InvalidOperationException>(() =>
                QuantityReportTotals.FromRows(new ReadOnlyRowCountSequence(2, new[] { countRowA })));
            ExpectThrows<InvalidOperationException>(() =>
                QuantityReportTotals.FromRows(new ReadOnlyRowCountSequence(1, new[] { countRowA, countRowB })));

            var negativeRowCount = new NonGenericRowCountSequence(-1);
            ExpectThrows<InvalidOperationException>(() => QuantityReportTotals.FromRows(negativeRowCount));
            if (negativeRowCount.EnumeratorEntered)
                throw new Exception("Legacy quantity totals must reject a negative known row Count before enumeration.");

            var conflictingRowCounts = new ConflictingRowCountSequence();
            ExpectThrows<InvalidOperationException>(() => QuantityReportTotals.FromRows(conflictingRowCounts));
            if (conflictingRowCounts.EnumeratorEntered)
                throw new Exception("Legacy quantity totals must reject conflicting known row Counts before enumeration.");

            var pureRowStream = QuantityReportTotals.FromRows(YieldRows(countRowA, countRowB));
            if (pureRowStream.Count != 3 || Math.Abs(pureRowStream.LengthM - 5d) > 1e-12 || Math.Abs(pureRowStream.GrossConcreteM3 - 3d) > 1e-12)
                throw new Exception("Legacy quantity totals must preserve pure IEnumerable row semantics without a known Count contract.");

            ExpectArgumentException(
                () => QuantityReportTotals.FromRows(new QuantityReportRow[] { valid, null! }),
                "rows",
                "index: 1");

            var negativeLength = new ElementInstance("Legacy-Negative-Length", family, "Floor");
            ExpectThrows<ArgumentOutOfRangeException>(() => negativeLength.LengthM = -1d);

            var negativeNet = new ElementInstance("Legacy-Negative-Net", family, "Floor")
            {
                GrossConcreteM3 = 1d,
                DeductionM3 = 2d
            };
            var negativeNetRow = QuantityReportBuilder.Group(new[] { negativeNet }).Single();
            if (Math.Abs(negativeNetRow.GrossConcreteM3 - 1d) > 1e-12 ||
                Math.Abs(negativeNetRow.DeductionM3 - 2d) > 1e-12 ||
                Math.Abs(negativeNetRow.NetConcreteM3) > 1e-12)
                throw new Exception("Legacy quantity report must preserve gross/deduction values while clamping derived net concrete to zero.");

            var negativeTotalRow = new QuantityReportRow { Count = 1, LengthM = -0.5d };
            ExpectThrows<InvalidOperationException>(() => QuantityReportTotals.FromRows(new[] { negativeTotalRow }));

            var project = new ProjectState("negative-report-project", "Negative report project");
            project.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("zone", "Zone"));
            project.Families.Add(new ProjectFamily("slab", "Slab", ElementCategory.Slab));
            var projectElement = new ProjectElement("P1", ElementCategory.Slab, "slab", "floor", "zone");
            projectElement.Quantities["LengthM"] = -1d;
            project.Elements.Add(projectElement);
            ExpectThrows<InvalidOperationException>(() => ProjectQuantityReportBuilder.Group(project));
        }

        private static IEnumerable<ElementInstance> YieldElements(params ElementInstance[] elements)
        {
            foreach (var element in elements)
                yield return element;
        }

        private static IEnumerable<QuantityReportRow> YieldRows(params QuantityReportRow[] rows)
        {
            foreach (var row in rows)
                yield return row;
        }

        private static void ExpectArgumentException(Action action, string paramName, string messagePart)
        {
            try { action(); }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, paramName, StringComparison.Ordinal) ||
                    ex.Message.IndexOf(messagePart, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Expected ArgumentException for '" + paramName + "' containing '" + messagePart + "', got: " + ex.Message);
                return;
            }
            throw new Exception("Expected ArgumentException.");
        }

        private static void ExpectThrows<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private sealed class NonGenericCountSequence : IEnumerable<ElementInstance>, ICollection
        {
            internal NonGenericCountSequence(int count)
            {
                Count = count;
            }

            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            internal bool EnumeratorEntered { get; private set; }

            public IEnumerator<ElementInstance> GetEnumerator()
            {
                EnumeratorEntered = true;
                throw new EnumerationEnteredException();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyCountSequence : IReadOnlyCollection<ElementInstance>
        {
            private readonly IReadOnlyList<ElementInstance> _items;

            internal ReadOnlyCountSequence(int count, IReadOnlyList<ElementInstance> items)
            {
                Count = count;
                _items = items;
            }

            public int Count { get; }
            public IEnumerator<ElementInstance> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ConflictingCountSequence : ICollection<ElementInstance>, IReadOnlyCollection<ElementInstance>
        {
            int ICollection<ElementInstance>.Count => 1;
            int IReadOnlyCollection<ElementInstance>.Count => 2;
            bool ICollection<ElementInstance>.IsReadOnly => true;
            internal bool EnumeratorEntered { get; private set; }

            public IEnumerator<ElementInstance> GetEnumerator()
            {
                EnumeratorEntered = true;
                throw new EnumerationEnteredException();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<ElementInstance>.Add(ElementInstance item) => throw new NotSupportedException();
            void ICollection<ElementInstance>.Clear() => throw new NotSupportedException();
            bool ICollection<ElementInstance>.Contains(ElementInstance item) => false;
            void ICollection<ElementInstance>.CopyTo(ElementInstance[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<ElementInstance>.Remove(ElementInstance item) => throw new NotSupportedException();
        }

        private sealed class NonGenericRowCountSequence : IEnumerable<QuantityReportRow>, ICollection
        {
            internal NonGenericRowCountSequence(int count)
            {
                Count = count;
            }

            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            internal bool EnumeratorEntered { get; private set; }

            public IEnumerator<QuantityReportRow> GetEnumerator()
            {
                EnumeratorEntered = true;
                throw new EnumerationEnteredException();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyRowCountSequence : IReadOnlyCollection<QuantityReportRow>
        {
            private readonly IReadOnlyList<QuantityReportRow> _items;

            internal ReadOnlyRowCountSequence(int count, IReadOnlyList<QuantityReportRow> items)
            {
                Count = count;
                _items = items;
            }

            public int Count { get; }
            public IEnumerator<QuantityReportRow> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ConflictingRowCountSequence : ICollection<QuantityReportRow>, IReadOnlyCollection<QuantityReportRow>
        {
            int ICollection<QuantityReportRow>.Count => 1;
            int IReadOnlyCollection<QuantityReportRow>.Count => 2;
            bool ICollection<QuantityReportRow>.IsReadOnly => true;
            internal bool EnumeratorEntered { get; private set; }

            public IEnumerator<QuantityReportRow> GetEnumerator()
            {
                EnumeratorEntered = true;
                throw new EnumerationEnteredException();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<QuantityReportRow>.Add(QuantityReportRow item) => throw new NotSupportedException();
            void ICollection<QuantityReportRow>.Clear() => throw new NotSupportedException();
            bool ICollection<QuantityReportRow>.Contains(QuantityReportRow item) => false;
            void ICollection<QuantityReportRow>.CopyTo(QuantityReportRow[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<QuantityReportRow>.Remove(QuantityReportRow item) => throw new NotSupportedException();
        }

        private sealed class EnumerationEnteredException : Exception
        {
        }
    }
}
