using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCatalogLoadCountBoundSmoke
    {
        private const int MaxCatalogItems = 10000;

        public static void Run()
        {
            ExactViewBoundLoads();
            ViewBoundPlusOneFailsClosed();
            ExactSheetBoundLoads();
            SheetBoundPlusOneFailsClosed();
        }

        private static void ExactViewBoundLoads()
        {
            var project = NewProject("VIEWS-BOUND");
            var payload = BuildPayload(MaxCatalogItems, 0);
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = payload;
            var beforeVersion = project.ChangeVersion;

            var catalog = new SemanticDocumentationCatalogStore().Load(project);

            Equal(MaxCatalogItems, catalog.Views.Count);
            Equal(0, catalog.Sheets.Count);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(payload, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);
        }

        private static void ViewBoundPlusOneFailsClosed()
        {
            var project = NewProject("VIEWS-OVER-BOUND");
            var payload = BuildPayload(MaxCatalogItems + 1, 0);
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = payload;
            var beforeVersion = project.ChangeVersion;

            ExpectInvalidData(
                project,
                beforeVersion,
                payload,
                "Semantic documentation catalog supports at most 10000 persisted views.");
        }

        private static void ExactSheetBoundLoads()
        {
            var project = NewProject("SHEETS-BOUND");
            var payload = BuildPayload(0, MaxCatalogItems);
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = payload;
            var beforeVersion = project.ChangeVersion;

            var catalog = new SemanticDocumentationCatalogStore().Load(project);

            Equal(0, catalog.Views.Count);
            Equal(MaxCatalogItems, catalog.Sheets.Count);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(payload, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);
        }

        private static void SheetBoundPlusOneFailsClosed()
        {
            var project = NewProject("SHEETS-OVER-BOUND");
            var payload = BuildPayload(0, MaxCatalogItems + 1);
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = payload;
            var beforeVersion = project.ChangeVersion;

            ExpectInvalidData(
                project,
                beforeVersion,
                payload,
                "Semantic documentation catalog supports at most 10000 persisted sheets.");
        }

        private static ProjectState NewProject(string suffix) =>
            new ProjectState("P-DOC-CATALOG-" + suffix, "Documentation catalog load count bound");

        private static void ExpectInvalidData(ProjectState project, long beforeVersion, string payload, string expectedMessage)
        {
            try
            {
                new SemanticDocumentationCatalogStore().Load(project);
            }
            catch (InvalidDataException ex)
            {
                Equal(expectedMessage, ex.Message);
                Equal(beforeVersion, project.ChangeVersion);
                Equal(payload, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);
                return;
            }

            throw new Exception("Expected persisted semantic documentation catalog count-bound rejection.");
        }

        private static string BuildPayload(int viewCount, int sheetCount)
        {
            var builder = new StringBuilder(128 + (viewCount * 64) + (sheetCount * 88));
            builder.Append("<documentation version=\"1\"><views>");
            for (var i = 0; i < viewCount; i++)
            {
                builder.Append("<view id=\"V");
                builder.Append(i);
                builder.Append("\" name=\"View");
                builder.Append(i);
                builder.Append("\" kind=\"Model\" floorId=\"\" zoneId=\"\"/>");
            }

            builder.Append("</views><sheets>");
            for (var i = 0; i < sheetCount; i++)
            {
                builder.Append("<sheet id=\"S");
                builder.Append(i);
                builder.Append("\" number=\"N");
                builder.Append(i);
                builder.Append("\" name=\"Sheet");
                builder.Append(i);
                builder.Append("\" widthMm=\"1\" heightMm=\"1\" titleBlockName=\"\"/>");
            }

            builder.Append("</sheets></documentation>");
            var payload = builder.ToString();
            if (payload.Length >= 1024 * 1024)
                throw new Exception("Count-bound smoke payload must remain below the catalog 1 MiB limit.");
            return payload;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Semantic documentation catalog load count-bound smoke expected " + expected + " but got " + actual + ".");
        }
    }

    internal static class SemanticDocumentationCatalogLoadCountBoundSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticDocumentationCatalogLoadCountBoundSmoke.Run();
    }
}
