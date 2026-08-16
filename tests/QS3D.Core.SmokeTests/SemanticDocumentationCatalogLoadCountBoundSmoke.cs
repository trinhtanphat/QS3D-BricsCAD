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
        private const int Limit = 10000;
        private const int MaxCatalogChars = 1024 * 1024;

        internal static void Run()
        {
            ExactViewBoundLoads();
            OverBoundViewsFailClosed();
            ExactSheetBoundLoads();
            OverBoundSheetsFailClosed();
        }

        private static void ExactViewBoundLoads()
        {
            var project = ProjectWithPayload(BuildPayload(Limit, 0));
            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal(Limit, catalog.Views.Count, "Exactly 10,000 persisted documentation views must remain accepted.");
            Equal(0, catalog.Sheets.Count, "View-bound fixture unexpectedly materialized sheets.");
        }

        private static void OverBoundViewsFailClosed()
        {
            var payload = BuildPayload(Limit + 1, 0);
            Require(payload.Length < MaxCatalogChars, "Over-bound view fixture must stay below the 1 MiB XML guard.");
            MustFailLoad(
                ProjectWithPayload(payload),
                "Persisted documentation view 10,001 must fail closed at the load cardinality boundary.");
        }

        private static void ExactSheetBoundLoads()
        {
            var project = ProjectWithPayload(BuildPayload(0, Limit));
            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            Equal(0, catalog.Views.Count, "Sheet-bound fixture unexpectedly materialized views.");
            Equal(Limit, catalog.Sheets.Count, "Exactly 10,000 persisted documentation sheets must remain accepted.");
        }

        private static void OverBoundSheetsFailClosed()
        {
            var payload = BuildPayload(0, Limit + 1);
            Require(payload.Length < MaxCatalogChars, "Over-bound sheet fixture must stay below the 1 MiB XML guard.");
            MustFailLoad(
                ProjectWithPayload(payload),
                "Persisted documentation sheet 10,001 must fail closed at the load cardinality boundary.");
        }

        private static ProjectState ProjectWithPayload(string payload)
        {
            var project = new ProjectState("P-DOC-LOAD-BOUND", "Documentation Load Bound");
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = payload;
            return project;
        }

        private static string BuildPayload(int viewCount, int sheetCount)
        {
            var builder = new StringBuilder(Math.Min(MaxCatalogChars - 1, 128 + (viewCount * 64) + (sheetCount * 88)));
            builder.Append("<documentation version=\"1\"><views>");
            for (var index = 0; index < viewCount; index++)
            {
                builder.Append("<view id=\"v").Append(index)
                    .Append("\" name=\"n").Append(index)
                    .Append("\" kind=\"Model\" floorId=\"\" zoneId=\"\"/>");
            }
            builder.Append("</views><sheets>");
            for (var index = 0; index < sheetCount; index++)
            {
                builder.Append("<sheet id=\"s").Append(index)
                    .Append("\" number=\"").Append(index)
                    .Append("\" name=\"n").Append(index)
                    .Append("\" widthMm=\"1\" heightMm=\"1\" titleBlockName=\"\"/>");
            }
            builder.Append("</sheets></documentation>");
            return builder.ToString();
        }

        private static void MustFailLoad(ProjectState project, string message)
        {
            try
            {
                _ = new SemanticDocumentationCatalogStore().Load(project);
            }
            catch (InvalidDataException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }

    internal static class SemanticDocumentationCatalogLoadCountBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticDocumentationCatalogLoadCountBoundSmoke.Run();
    }
}
