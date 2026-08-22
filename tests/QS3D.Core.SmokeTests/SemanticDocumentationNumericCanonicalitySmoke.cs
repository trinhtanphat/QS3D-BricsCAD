using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationNumericCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var store = new SemanticDocumentationCatalogStore();
            var project = new ProjectState("DOC-NUMERIC", "Documentation Numeric Canonicality");
            var views = new[]
            {
                new SemanticViewDefinition("VIEW-1", "View 1")
            };
            var sheets = new[]
            {
                new SemanticSheetDefinition(
                    "SHEET-1",
                    "A101",
                    "Sheet 1",
                    1000d,
                    500d,
                    new[] { new SemanticSheetPlacementDefinition("VIEW-1", 10d, 20d, 100d, 80d) })
            };

            store.Save(project, views, sheets);
            var canonical = project.Metadata[SemanticDocumentationCatalogStore.MetadataKey];
            var loaded = store.Load(project);
            if (loaded.Sheets.Count != 1 || loaded.Views.Count != 1)
                throw new Exception("Canonical semantic documentation catalog must remain loadable.");

            Rejects(store, canonical, document =>
            {
                var sheet = document.Root?.Element("sheets")?.Element("sheet")
                    ?? throw new Exception("Numeric canonicality smoke is missing its sheet fixture.");
                sheet.SetAttributeValue("widthMm", "1000.0");
            }, "noncanonical sheet width");

            Rejects(store, canonical, document =>
            {
                var placement = document.Root?.Element("sheets")?.Element("sheet")?.Element("placements")?.Element("placement")
                    ?? throw new Exception("Numeric canonicality smoke is missing its placement fixture.");
                placement.SetAttributeValue("xMm", "10.0");
            }, "noncanonical placement x");

            Rejects(store, canonical, document =>
            {
                var sheet = document.Root?.Element("sheets")?.Element("sheet")
                    ?? throw new Exception("Numeric canonicality smoke is missing its sheet fixture.");
                sheet.SetAttributeValue("heightMm", " 500 ");
            }, "whitespace-padded sheet height");
        }

        private static void Rejects(
            SemanticDocumentationCatalogStore store,
            string canonical,
            Action<XDocument> mutate,
            string label)
        {
            var document = XDocument.Parse(canonical, LoadOptions.None);
            mutate(document);
            var project = new ProjectState("DOC-NUMERIC-LOAD", "Documentation Numeric Load");
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = document.Root?.ToString(SaveOptions.DisableFormatting)
                ?? throw new Exception("Numeric canonicality smoke produced an empty XML payload.");

            try
            {
                store.Load(project);
            }
            catch (InvalidDataException)
            {
                return;
            }

            throw new Exception("Semantic documentation catalog accepted " + label + ".");
        }
    }
}
