using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCatalogEmptyPayloadSmoke
    {
        public static void Run()
        {
            MissingKeyRemainsEmptyCatalog();
            PresentEmptyPayloadFailsClosed();
        }

        private static void MissingKeyRemainsEmptyCatalog()
        {
            var project = new ProjectState("P-DOC-CATALOG-MISSING-PAYLOAD", "Documentation catalog missing payload");
            var beforeVersion = project.ChangeVersion;

            var catalog = new SemanticDocumentationCatalogStore().Load(project);

            Equal(0, catalog.Views.Count);
            Equal(0, catalog.Sheets.Count);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(false, project.Metadata.ContainsKey(SemanticDocumentationCatalogStore.MetadataKey));
        }

        private static void PresentEmptyPayloadFailsClosed()
        {
            var project = new ProjectState("P-DOC-CATALOG-EMPTY-PAYLOAD", "Documentation catalog empty payload");
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = string.Empty;
            var beforeVersion = project.ChangeVersion;

            try
            {
                new SemanticDocumentationCatalogStore().Load(project);
            }
            catch (InvalidDataException ex)
            {
                Equal("Semantic documentation catalog payload is empty.", ex.Message);
                Equal(beforeVersion, project.ChangeVersion);
                Equal(true, project.Metadata.ContainsKey(SemanticDocumentationCatalogStore.MetadataKey));
                Equal(string.Empty, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);
                return;
            }

            throw new Exception("Expected persisted empty semantic documentation catalog payload rejection.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Semantic documentation catalog empty-payload smoke expected " + expected + " but got " + actual + ".");
        }
    }

    internal static class SemanticDocumentationCatalogEmptyPayloadSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticDocumentationCatalogEmptyPayloadSmoke.Run();
    }
}
