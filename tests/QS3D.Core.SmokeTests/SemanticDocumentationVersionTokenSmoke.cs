using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationVersionTokenSmoke
    {
        internal static void Run()
        {
            AcceptsCanonicalVersion();
            RejectsAlias("01", "leading zero");
            RejectsAlias("+1", "leading sign");
            RejectsAlias(" 1 ", "surrounding whitespace");
        }

        private static void AcceptsCanonicalVersion()
        {
            var project = Fixture();
            var store = new SemanticDocumentationCatalogStore();
            SaveFixture(store, project);
            var catalog = store.Load(project);
            if (catalog.Views.Count != 1 || catalog.Views[0].Id != "V1")
                throw new Exception("SemanticDocumentationVersionTokenSmoke canonical version: catalog did not round-trip.");
        }

        private static void RejectsAlias(string token, string label)
        {
            var project = Fixture();
            var store = new SemanticDocumentationCatalogStore();
            SaveFixture(store, project);
            var document = XDocument.Parse(project.Metadata[SemanticDocumentationCatalogStore.MetadataKey], LoadOptions.None);
            var root = document.Root ?? throw new Exception("SemanticDocumentationVersionTokenSmoke: missing root fixture.");
            root.SetAttributeValue("version", token);
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = root.ToString(SaveOptions.DisableFormatting);
            Throws<InvalidDataException>(() => store.Load(project), label);
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-DOC-VERSION", "Documentation version smoke");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.ArchitecturalWall));
            return project;
        }

        private static void SaveFixture(SemanticDocumentationCatalogStore store, ProjectState project)
        {
            store.Save(
                project,
                new[] { new SemanticViewDefinition("V1", "View 1") },
                Array.Empty<SemanticSheetDefinition>());
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("SemanticDocumentationVersionTokenSmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }

    internal static class SemanticDocumentationVersionTokenSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticDocumentationVersionTokenSmoke.Run();
    }
}
