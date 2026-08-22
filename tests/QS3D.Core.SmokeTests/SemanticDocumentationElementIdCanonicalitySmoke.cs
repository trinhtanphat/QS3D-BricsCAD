using System;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationElementIdCanonicalitySmoke
    {
        internal static void Run()
        {
            PreservesCanonicalIdentityAndDisplayNormalization();
            RejectsPaddedElementIdentity();
        }

        private static void PreservesCanonicalIdentityAndDisplayNormalization()
        {
            var project = CreateProject();
            var table = SemanticDocumentationTableBuilder.Build(
                project,
                "  Semantic table  ",
                new[] { "ELEMENT-1" },
                new[] { new SemanticDocumentationColumn("  Element  ", "  {Id}  ") });

            Require(table.Title == "Semantic table", "Documentation title trim behavior changed.");
            Require(table.Headers.Count == 1 && table.Headers[0] == "Element", "Documentation header trim behavior changed.");
            Require(table.Rows.Count == 1, "Canonical case-insensitive element id did not resolve exactly one row.");
            Require(table.Rows[0].ElementId == "element-1", "Documentation row did not preserve canonical project element identity.");
            Require(table.Rows[0].Cells.Count == 1 && table.Rows[0].Cells[0] == "element-1", "Canonical element id template output changed.");
        }

        private static void RejectsPaddedElementIdentity()
        {
            var project = CreateProject();
            Throws<ArgumentException>(() => SemanticDocumentationTableBuilder.Build(
                project,
                "Semantic table",
                new[] { " element-1 " },
                new[] { new SemanticDocumentationColumn("Element", "{Id}") }));
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("documentation-canonical-id", "Documentation canonical id");
            project.Elements.Add(new ProjectElement("element-1", ElementCategory.CustomQuantity));
            return project;
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
