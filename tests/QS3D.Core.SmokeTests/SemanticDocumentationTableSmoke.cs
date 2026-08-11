using System;
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
