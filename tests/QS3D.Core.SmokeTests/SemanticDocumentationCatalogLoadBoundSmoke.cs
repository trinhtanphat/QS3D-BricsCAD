using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCatalogLoadBoundSmoke
    {
        private const int CatalogLimit = 10000;
        private const int MetadataCharacterLimit = 1024 * 1024;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ExactViewBoundLoads();
            ViewBoundPlusOneFailsClosed();
            ExactSheetBoundLoads();
            SheetBoundPlusOneFailsClosed();
        }

        private static void ExactViewBoundLoads()
        {
            var project = BuildProject(BuildViewPayload(CatalogLimit));
            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal(CatalogLimit, catalog.Views.Count, "Exact persisted view bound changed.");
            Equal(0, catalog.Sheets.Count, "View-bound payload unexpectedly materialized sheets.");
        }

        private static void ViewBoundPlusOneFailsClosed()
        {
            var project = BuildProject(BuildViewPayload(CatalogLimit + 1));
            MustFailLoad(
                () => new SemanticDocumentationCatalogStore().Load(project),
                "Persisted documentation catalog must reject view element 10,001.");
        }

        private static void ExactSheetBoundLoads()
        {
            var project = BuildProject(BuildSheetPayload(CatalogLimit));
            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal(0, catalog.Views.Count, "Sheet-bound payload unexpectedly materialized views.");
            Equal(CatalogLimit, catalog.Sheets.Count, "Exact persisted sheet bound changed.");
        }

        private static void SheetBoundPlusOneFailsClosed()
        {
            var project = BuildProject(BuildSheetPayload(CatalogLimit + 1));
            MustFailLoad(
                () => new SemanticDocumentationCatalogStore().Load(project),
                "Persisted documentation catalog must reject sheet element 10,001.");
        }

        private static ProjectState BuildProject(string payload)
        {
            if (payload.Length >= MetadataCharacterLimit)
                throw new InvalidOperationException(
                    "Count-bound regression payload must stay below the 1 MiB metadata cap; actual length: " +
                    payload.Length.ToString(CultureInfo.InvariantCulture) + ".");

            var project = new ProjectState("P-DOC-LOAD-BOUND", "Documentation Load Bound");
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = payload;
            return project;
        }

        private static string BuildViewPayload(int count)
        {
            var builder = new StringBuilder(Math.Min(MetadataCharacterLimit - 1, 80 + count * 44));
            builder.Append("<documentation version=\"1\"><views>");
            for (var i = 0; i < count; i++)
            {
                var token = i.ToString(CultureInfo.InvariantCulture);
                builder.Append("<view id=\"v").Append(token)
                    .Append("\" name=\"n").Append(token)
                    .Append("\" kind=\"Plan\"/>");
            }
            builder.Append("</views><sheets/></documentation>");
            return builder.ToString();
        }

        private static string BuildSheetPayload(int count)
        {
            var builder = new StringBuilder(Math.Min(MetadataCharacterLimit - 1, 80 + count * 72));
            builder.Append("<documentation version=\"1\"><views/><sheets>");
            for (var i = 0; i < count; i++)
            {
                var token = i.ToString(CultureInfo.InvariantCulture);
                builder.Append("<sheet id=\"s").Append(token)
                    .Append("\" number=\"").Append(token)
                    .Append("\" name=\"n").Append(token)
                    .Append("\" widthMm=\"1\" heightMm=\"1\"/>");
            }
            builder.Append("</sheets></documentation>");
            return builder.ToString();
        }

        private static void MustFailLoad(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidDataException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
