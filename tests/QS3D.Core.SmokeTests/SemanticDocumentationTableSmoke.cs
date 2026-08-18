using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationTableSmoke
    {
        public static void Run()
        {
            ExplicitOrderAndTemplatesArePreserved();
            EmptyRowsStillValidateTemplates();
            BlankOptionalCellsAreAllowedWithoutWeakeningTagLabels();
            DuplicateElementIdsFailClosed();
            AmbiguousProjectElementFailsClosed();
            DuplicateHeadersFailClosed();
            GeneratedOwnershipPropertiesRemainBlocked();
            OutputSnapshotsAreDefensivelyImmutable();
            UnusedReferenceIndexesStayLazy();
            KnownCountsFailClosedBeforeEnumeration();
            KnownCountsMustMatchCompletedTraversal();
            OversizedEnumerablesStopAtDeclaredBounds();
        }

        private static void ExplicitOrderAndTemplatesArePreserved()
        {
            var project = new ProjectState("table", "Table");
            var first = Element(project, "E-1", ElementCategory.Beam, "B1", 3.5);
            var second = Element(project, "E-2", ElementCategory.Column, "C2", 7.25);

            var table = SemanticDocumentationTableBuilder.Build(
                project,
                "  Semantic schedule  ",
                new[] { second.Id, first.Id },
                new[]
                {
                    new SemanticDocumentationColumn("Mark", "{P:Mark}"),
                    new SemanticDocumentationColumn("Category", "{Category}"),
                    new SemanticDocumentationColumn("Length", "{Q:LengthM}")
                });

            Equal("Semantic schedule", table.Title);
            Equal(3, table.Headers.Count);
            Equal("Mark", table.Headers[0]);
            Equal(2, table.Rows.Count);
            Equal("E-2", table.Rows[0].ElementId);
            Equal("C2", table.Rows[0].Cells[0]);
            Equal("Column", table.Rows[0].Cells[1]);
            Equal("7.25", table.Rows[0].Cells[2]);
            Equal("E-1", table.Rows[1].ElementId);
            Equal("B1", table.Rows[1].Cells[0]);
        }

        private static void EmptyRowsStillValidateTemplates()
        {
            var project = new ProjectState("table", "Table");
            var table = SemanticDocumentationTableBuilder.Build(
                project,
                "Empty",
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("Optional", "{P:MissingOptional}") },
                allowEmpty: true);
            Equal(1, table.Headers.Count);
            Equal(0, table.Rows.Count);

            Throws<FormatException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Empty",
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("Bad", "{Unsupported}") },
                allowEmpty: true));
            Throws<FormatException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Empty",
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("Bad", "{P:Missing") },
                allowEmpty: true));
            Throws<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Empty",
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("Native", "{P:GeneratedSolidHandle}") },
                allowEmpty: true));
        }

        private static void BlankOptionalCellsAreAllowedWithoutWeakeningTagLabels()
        {
            var project = new ProjectState("table", "Table");
            var element = Element(project, "E-1", ElementCategory.Beam, "B1", 1.0);
            var table = SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { element.Id },
                new[] { new SemanticDocumentationColumn("Optional", "{P:MissingOptional}") });

            Equal(string.Empty, table.Rows[0].Cells[0]);
            Throws<InvalidOperationException>(() => SemanticTagRenderer.Render(project, element, "{P:MissingOptional}"));
        }

        private static void DuplicateElementIdsFailClosed()
        {
            var project = new ProjectState("table", "Table");
            var element = Element(project, "E-1", ElementCategory.Beam, "B1", 1.0);
            Throws<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { element.Id, "e-1" },
                new[] { new SemanticDocumentationColumn("Id", "{Id}") }));
        }

        private static void AmbiguousProjectElementFailsClosed()
        {
            var project = new ProjectState("table", "Table");
            var element = Element(project, "E-1", ElementCategory.Beam, "B1", 1.0);
            Element(project, "e-1", ElementCategory.Column, "C1", 2.0);
            Throws<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { element.Id },
                new[] { new SemanticDocumentationColumn("Id", "{Id}") }));
        }

        private static void DuplicateHeadersFailClosed()
        {
            var project = new ProjectState("table", "Table");
            var element = Element(project, "E-1", ElementCategory.Beam, "B1", 1.0);
            Throws<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { element.Id },
                new[]
                {
                    new SemanticDocumentationColumn("Mark", "{P:Mark}"),
                    new SemanticDocumentationColumn("mark", "{Id}")
                }));
        }

        private static void GeneratedOwnershipPropertiesRemainBlocked()
        {
            var project = new ProjectState("table", "Table");
            var element = Element(project, "E-1", ElementCategory.Beam, "B1", 1.0);
            element.SetProperty("GeneratedSolidHandle", "ABC");
            Throws<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { element.Id },
                new[] { new SemanticDocumentationColumn("Native", "{P:GeneratedSolidHandle}") }));
        }

        private static void OutputSnapshotsAreDefensivelyImmutable()
        {
            var sourceCells = new List<string> { "A" };
            var row = new SemanticDocumentationRow("E-1", sourceCells);
            sourceCells[0] = "MUTATED";
            Equal("A", row.Cells[0]);
            Throws<NotSupportedException>(() => ((IList<string>)row.Cells)[0] = "MUTATED");

            var sourceHeaders = new List<string> { "Header" };
            var sourceRows = new List<SemanticDocumentationRow> { row };
            var table = new SemanticDocumentationTable("Schedule", sourceHeaders, sourceRows);
            sourceHeaders[0] = "MUTATED";
            sourceRows.Clear();
            Equal("Header", table.Headers[0]);
            Equal(1, table.Rows.Count);
            Equal("E-1", table.Rows[0].ElementId);
            Throws<NotSupportedException>(() => ((IList<string>)table.Headers)[0] = "MUTATED");
            Throws<NotSupportedException>(() => ((IList<SemanticDocumentationRow>)table.Rows).Clear());
        }

        private static void UnusedReferenceIndexesStayLazy()
        {
            var project = new ProjectState("table", "Table");
            project.Families.Add(new ProjectFamily("F-1", "Family A", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("f-1", "Family B", ElementCategory.Beam));
            var element = new ProjectElement("E-1", ElementCategory.Beam, "F-1", string.Empty, string.Empty);
            project.Elements.Add(element);

            var idOnly = SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { element.Id },
                new[] { new SemanticDocumentationColumn("Id", "{Id}") });
            Equal("E-1", idOnly.Rows[0].Cells[0]);

            Throws<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { element.Id },
                new[] { new SemanticDocumentationColumn("Family", "{Family}") }));
        }

        private static void KnownCountsFailClosedBeforeEnumeration()
        {
            var project = new ProjectState("table-count", "Table count");
            var element = Element(project, "E-1", ElementCategory.Beam, "B1", 1.0);
            var oneColumn = new[] { new SemanticDocumentationColumn("Id", "{Id}") };

            var oversizedRows = new NoEnumerationCollection<string>(5001);
            ThrowsMessage<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                oversizedRows,
                oneColumn), "at most 5000 rows");
            Equal(0, oversizedRows.EnumerationAttempts);

            var oversizedColumns = new NoEnumerationCollection<SemanticDocumentationColumn>(33);
            ThrowsMessage<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { element.Id },
                oversizedColumns), "at most 32 columns");
            Equal(0, oversizedColumns.EnumerationAttempts);

            var negativeRows = new ReadOnlyNoEnumerationCollection<string>(-1);
            ThrowsMessage<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                negativeRows,
                oneColumn), "invalid negative known count");
            Equal(0, negativeRows.EnumerationAttempts);

            var negativeColumns = new NonGenericNoEnumerationCollection<SemanticDocumentationColumn>(-1);
            ThrowsMessage<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { element.Id },
                negativeColumns), "invalid negative known count");
            Equal(0, negativeColumns.EnumerationAttempts);

            var conflictingRows = new ConflictingCountCollection<string>();
            ThrowsMessage<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                conflictingRows,
                oneColumn), "conflicting known counts");
            Equal(0, conflictingRows.EnumerationAttempts);
        }

        private static void KnownCountsMustMatchCompletedTraversal()
        {
            var project = new ProjectState("table-count-traversal", "Table count traversal");
            var first = Element(project, "E-1", ElementCategory.Beam, "B1", 1.0);
            var second = Element(project, "E-2", ElementCategory.Column, "C2", 2.0);
            var idColumn = new SemanticDocumentationColumn("Id", "{Id}");

            var underRows = new AdvertisedCountCollection<string>(2, first.Id);
            ThrowsMessage<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                underRows,
                new[] { idColumn }), "known count does not match completed traversal");

            var overRows = new AdvertisedCountCollection<string>(1, first.Id, second.Id);
            ThrowsMessage<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                overRows,
                new[] { idColumn }), "known count does not match completed traversal");

            var underColumns = new AdvertisedCountCollection<SemanticDocumentationColumn>(
                2,
                idColumn);
            ThrowsMessage<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { first.Id },
                underColumns), "known count does not match completed traversal");

            var overColumns = new AdvertisedCountCollection<SemanticDocumentationColumn>(
                1,
                idColumn,
                new SemanticDocumentationColumn("Mark", "{P:Mark}"));
            ThrowsMessage<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { first.Id },
                overColumns), "known count does not match completed traversal");

            var honest = SemanticDocumentationTableBuilder.Build(
                project,
                "Honest",
                new AdvertisedCountCollection<string>(2, first.Id, second.Id),
                new AdvertisedCountCollection<SemanticDocumentationColumn>(1, idColumn));
            Equal(2, honest.Rows.Count);
            Equal(1, honest.Headers.Count);

            var streamed = SemanticDocumentationTableBuilder.Build(
                project,
                "Streamed",
                Stream(first.Id, second.Id),
                Stream(idColumn));
            Equal(2, streamed.Rows.Count);
            Equal(1, streamed.Headers.Count);
        }

        private static void OversizedEnumerablesStopAtDeclaredBounds()
        {
            var project = new ProjectState("table", "Table");
            var element = Element(project, "E-1", ElementCategory.Beam, "B1", 1.0);

            ThrowsMessage<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                GuardedIds(5001),
                new[] { new SemanticDocumentationColumn("Id", "{Id}") }), "at most 5000 rows");

            ThrowsMessage<InvalidOperationException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Schedule",
                new[] { element.Id },
                GuardedColumns(33)), "at most 32 columns");
        }

        private static IEnumerable<T> Stream<T>(params T[] items)
        {
            foreach (var item in items) yield return item;
        }

        private static IEnumerable<string> GuardedIds(int allowedItems)
        {
            for (var i = 0; i < allowedItems; i++) yield return "ROW-" + i;
            throw new Exception("Documentation table enumerated row input beyond its declared hard limit.");
        }

        private static IEnumerable<SemanticDocumentationColumn> GuardedColumns(int allowedItems)
        {
            for (var i = 0; i < allowedItems; i++) yield return new SemanticDocumentationColumn("H-" + i, "{Id}");
            throw new Exception("Documentation table enumerated column input beyond its declared hard limit.");
        }

        private sealed class AdvertisedCountCollection<T> : ICollection<T>
        {
            private readonly T[] items;

            public AdvertisedCountCollection(int count, params T[] items)
            {
                Count = count;
                this.items = items ?? Array.Empty<T>();
            }

            public int Count { get; }
            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator()
            {
                foreach (var item in items) yield return item;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class NoEnumerationCollection<T> : ICollection<T>
        {
            public NoEnumerationCollection(int count) { Count = count; }
            public int EnumerationAttempts { get; private set; }
            public int Count { get; }
            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Known invalid Count must be rejected before enumeration.");
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyNoEnumerationCollection<T> : IReadOnlyCollection<T>
        {
            public ReadOnlyNoEnumerationCollection(int count) { Count = count; }
            public int EnumerationAttempts { get; private set; }
            public int Count { get; }
            public IEnumerator<T> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Known invalid read-only Count must be rejected before enumeration.");
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericNoEnumerationCollection<T> : IEnumerable<T>, ICollection
        {
            public NonGenericNoEnumerationCollection(int count) { Count = count; }
            public int EnumerationAttempts { get; private set; }
            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public IEnumerator<T> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Known invalid non-generic Count must be rejected before enumeration.");
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ConflictingCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            public int EnumerationAttempts { get; private set; }
            public int Count => 1;
            int IReadOnlyCollection<T>.Count => 2;
            int ICollection.Count => 2;
            public bool IsReadOnly => true;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public IEnumerator<T> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Conflicting known Counts must be rejected before enumeration.");
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private static ProjectElement Element(ProjectState project, string id, ElementCategory category, string mark, double length)
        {
            var element = new ProjectElement(id, category, string.Empty, string.Empty, string.Empty);
            element.SetProperty("Mark", mark);
            element.SetQuantity("LengthM", length);
            project.Elements.Add(element);
            return element;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void ThrowsMessage<T>(Action action, string expectedMessage) where T : Exception
        {
            try { action(); }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new Exception("Expected exception message containing '" + expectedMessage + "', got '" + ex.Message + "'.");
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
