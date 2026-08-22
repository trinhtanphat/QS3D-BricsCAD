using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationNamedEnumTokenSmoke
    {
        internal static void Run()
        {
            RejectsNumericViewKindAlias();
            RejectsNumericCategoryAlias();
            AcceptsCaseInsensitiveNamedTokens();
        }

        private static void RejectsNumericViewKindAlias()
        {
            var project = Fixture();
            var store = new SemanticDocumentationCatalogStore();
            SaveFixture(store, project);
            Rewrite(project, document =>
            {
                var view = document.Root?.Element("views")?.Element("view")
                    ?? throw new Exception("SemanticDocumentationNamedEnumTokenSmoke: missing view fixture.");
                view.SetAttributeValue("kind", ((int)SemanticViewKind.Model).ToString(CultureInfo.InvariantCulture));
            });

            Throws<InvalidDataException>(() => store.Load(project), "numeric view kind");
        }

        private static void RejectsNumericCategoryAlias()
        {
            var project = Fixture();
            var store = new SemanticDocumentationCatalogStore();
            SaveFixture(store, project);
            Rewrite(project, document =>
            {
                var category = document.Root?.Element("views")?.Element("view")?.Element("categories")?.Element("category")
                    ?? throw new Exception("SemanticDocumentationNamedEnumTokenSmoke: missing category fixture.");
                category.SetAttributeValue("value", ((int)ElementCategory.ArchitecturalWall).ToString(CultureInfo.InvariantCulture));
            });

            Throws<InvalidDataException>(() => store.Load(project), "numeric category");
        }

        private static void AcceptsCaseInsensitiveNamedTokens()
        {
            var project = Fixture();
            var store = new SemanticDocumentationCatalogStore();
            SaveFixture(store, project);
            Rewrite(project, document =>
            {
                var view = document.Root?.Element("views")?.Element("view")
                    ?? throw new Exception("SemanticDocumentationNamedEnumTokenSmoke: missing view fixture.");
                view.SetAttributeValue("kind", SemanticViewKind.Model.ToString().ToLowerInvariant());
                var category = view.Element("categories")?.Element("category")
                    ?? throw new Exception("SemanticDocumentationNamedEnumTokenSmoke: missing category fixture.");
                category.SetAttributeValue("value", ElementCategory.ArchitecturalWall.ToString().ToLowerInvariant());
            });

            var catalog = store.Load(project);
            Equal(1, catalog.Views.Count, "view count");
            Equal(SemanticViewKind.Model, catalog.Views[0].Kind, "named view kind");
            Equal(1, catalog.Views[0].Categories.Count, "category count");
            Equal(ElementCategory.ArchitecturalWall, catalog.Views[0].Categories[0], "named category");
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-DOC-ENUM", "Documentation enum smoke");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.ArchitecturalWall));
            return project;
        }

        private static void SaveFixture(SemanticDocumentationCatalogStore store, ProjectState project)
        {
            store.Save(
                project,
                new[]
                {
                    new SemanticViewDefinition(
                        "V1",
                        "View 1",
                        SemanticViewKind.Model,
                        categories: new[] { ElementCategory.ArchitecturalWall })
                },
                Array.Empty<SemanticSheetDefinition>());
        }

        private static void Rewrite(ProjectState project, Action<XDocument> rewrite)
        {
            var payload = project.Metadata[SemanticDocumentationCatalogStore.MetadataKey];
            var document = XDocument.Parse(payload, LoadOptions.None);
            rewrite(document);
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = document.Root!.ToString(SaveOptions.DisableFormatting);
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("SemanticDocumentationNamedEnumTokenSmoke " + label + ": expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("SemanticDocumentationNamedEnumTokenSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class SemanticDocumentationNamedEnumTokenSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticDocumentationNamedEnumTokenSmoke.Run();
    }
}
