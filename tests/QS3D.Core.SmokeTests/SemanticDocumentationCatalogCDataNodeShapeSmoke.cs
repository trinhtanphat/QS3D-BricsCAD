using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCatalogCDataNodeShapeSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AcceptsOrdinaryWhitespaceFormatting();
            RejectsRootWhitespaceCData();
            RejectsNestedWhitespaceCData();
        }

        private static void AcceptsOrdinaryWhitespaceFormatting()
        {
            var project = ProjectWith(
                "<documentation version='1'>\n  <views>\n  </views>\n  <sheets>\n  </sheets>\n</documentation>");
            var catalog = new SemanticDocumentationCatalogStore().Load(project);
            if (catalog.Views.Count != 0 || catalog.Sheets.Count != 0)
                throw new InvalidOperationException("SemanticDocumentationCatalogCDataNodeShapeSmoke ordinary whitespace changed the empty catalog.");
        }

        private static void RejectsRootWhitespaceCData() =>
            Reject(
                "<documentation version='1'><![CDATA[   ]]><views/><sheets/></documentation>",
                "root whitespace CDATA");

        private static void RejectsNestedWhitespaceCData() =>
            Reject(
                "<documentation version='1'><views><![CDATA[\t ]]></views><sheets/></documentation>",
                "nested whitespace CDATA");

        private static void Reject(string payload, string label)
        {
            try
            {
                new SemanticDocumentationCatalogStore().Load(ProjectWith(payload));
            }
            catch (InvalidDataException)
            {
                return;
            }

            throw new InvalidOperationException(
                "SemanticDocumentationCatalogCDataNodeShapeSmoke expected InvalidDataException for " + label + ".");
        }

        private static ProjectState ProjectWith(string payload)
        {
            var project = new ProjectState("P-DOC-CDATA", "Documentation CDATA");
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = payload;
            return project;
        }
    }
}
