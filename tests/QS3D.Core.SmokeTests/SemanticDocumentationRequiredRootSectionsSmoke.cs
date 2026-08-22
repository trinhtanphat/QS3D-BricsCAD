using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationRequiredRootSectionsSmoke
    {
        internal static void Run()
        {
            AcceptsCanonicalRootSections();
            RejectsMissingRootSection("views");
            RejectsMissingRootSection("sheets");
        }

        private static void AcceptsCanonicalRootSections()
        {
            var project = Fixture();
            var store = new SemanticDocumentationCatalogStore();
            SaveFixture(store, project);
            var catalog = store.Load(project);
            if (catalog.Views.Count != 1 || catalog.Views[0].Id != "V1" || catalog.Sheets.Count != 0)
                throw new Exception("SemanticDocumentationRequiredRootSectionsSmoke canonical payload did not round-trip.");
        }

        private static void RejectsMissingRootSection(string sectionName)
        {
            var project = Fixture();
            var store = new SemanticDocumentationCatalogStore();
            SaveFixture(store, project);
            var document = XDocument.Parse(project.Metadata[SemanticDocumentationCatalogStore.MetadataKey], LoadOptions.None);
            var root = document.Root ?? throw new Exception("SemanticDocumentationRequiredRootSectionsSmoke: missing root fixture.");
            var section = root.Element(sectionName)
                ?? throw new Exception("SemanticDocumentationRequiredRootSectionsSmoke: missing " + sectionName + " fixture.");
            section.Remove();
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = root.ToString(SaveOptions.DisableFormatting);
            Throws<InvalidDataException>(() => store.Load(project), "missing " + sectionName);
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-DOC-ROOTS", "Documentation roots smoke");
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
            throw new Exception("SemanticDocumentationRequiredRootSectionsSmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }

    internal static class SemanticDocumentationRequiredRootSectionsSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticDocumentationRequiredRootSectionsSmoke.Run();
    }
}
