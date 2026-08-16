using System;
using System.IO;
using System.Text;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class DocumentationCatalogLoadBoundSmoke
    {
        private const int MaxCatalogItems = 10000;
        private const int MaxCatalogChars = 1024 * 1024;

        public static void Run()
        {
            ExactViewBoundLoads();
            ExcessViewCountFailsBelowDocumentLimit();
            ExactSheetBoundLoads();
            ExcessSheetCountFailsBelowDocumentLimit();
        }

        private static void ExactViewBoundLoads()
        {
            var catalog = Load(BuildViewPayload(MaxCatalogItems));
            Require(catalog.Views.Count == MaxCatalogItems, "Persisted documentation catalog should accept exactly 10,000 views.");
            Require(catalog.Sheets.Count == 0, "View-bound control unexpectedly loaded sheets.");
        }

        private static void ExcessViewCountFailsBelowDocumentLimit()
        {
            var payload = BuildViewPayload(MaxCatalogItems + 1);
            Require(payload.Length < MaxCatalogChars, "View over-bound payload must stay below the 1 MiB document limit.");
            Throws<InvalidDataException>(() => Load(payload));
        }

        private static void ExactSheetBoundLoads()
        {
            var catalog = Load(BuildSheetPayload(MaxCatalogItems));
            Require(catalog.Views.Count == 0, "Sheet-bound control unexpectedly loaded views.");
            Require(catalog.Sheets.Count == MaxCatalogItems, "Persisted documentation catalog should accept exactly 10,000 sheets.");
        }

        private static void ExcessSheetCountFailsBelowDocumentLimit()
        {
            var payload = BuildSheetPayload(MaxCatalogItems + 1);
            Require(payload.Length < MaxCatalogChars, "Sheet over-bound payload must stay below the 1 MiB document limit.");
            Throws<InvalidDataException>(() => Load(payload));
        }

        private static SemanticDocumentationCatalog Load(string payload)
        {
            var project = new ProjectState("DOC-BOUND", "Documentation Bound Smoke");
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = payload;
            return new SemanticDocumentationCatalogStore().Load(project);
        }

        private static string BuildViewPayload(int count)
        {
            var builder = new StringBuilder(64 + count * 48);
            builder.Append("<documentation version=\"1\"><views>");
            for (var i = 0; i < count; i++)
            {
                builder.Append("<view id=\"v");
                builder.Append(i);
                builder.Append("\" name=\"v");
                builder.Append(i);
                builder.Append("\" kind=\"Model\"/>");
            }
            builder.Append("</views><sheets/></documentation>");
            return builder.ToString();
        }

        private static string BuildSheetPayload(int count)
        {
            var builder = new StringBuilder(64 + count * 72);
            builder.Append("<documentation version=\"1\"><views/><sheets>");
            for (var i = 0; i < count; i++)
            {
                builder.Append("<sheet id=\"s");
                builder.Append(i);
                builder.Append("\" number=\"");
                builder.Append(i);
                builder.Append("\" name=\"s");
                builder.Append(i);
                builder.Append("\" widthMm=\"1\" heightMm=\"1\"/>");
            }
            builder.Append("</sheets></documentation>");
            return builder.ToString();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
