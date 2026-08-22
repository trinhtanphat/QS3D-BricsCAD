using System;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationTableSmoke
    {
        public static void Run()
        {
            ExplicitOrderAndTemplatesArePreserved();
            BlankOptionalCellsAreAllowedWithoutWeakeningTagLabels();
            DuplicateElementIdsFailClosed();
            DuplicateHeadersFailClosed();
            GeneratedOwnershipPropertiesRemainBlocked();
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
    }
}
