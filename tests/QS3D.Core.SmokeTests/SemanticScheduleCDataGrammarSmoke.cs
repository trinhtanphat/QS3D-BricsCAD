using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleCDataGrammarSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsWhitespaceOnlyCDataButPreservesWhitespaceText();
        }

        private static void RejectsWhitespaceOnlyCDataButPreservesWhitespaceText()
        {
            var project = new ProjectState("P-schedule-cdata", "Schedule CDATA");
            SemanticScheduleCatalog.Save(project, new[] { CreateDefinition() });

            var canonical = project.Metadata[SemanticScheduleCatalog.MetadataKey];
            const string rootOpen = "<semanticSchedules version=\"1\">";
            if (canonical.IndexOf(rootOpen, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("SemanticScheduleCDataGrammarSmoke: canonical payload root was not found.");

            project.Metadata[SemanticScheduleCatalog.MetadataKey] =
                canonical.Replace(rootOpen, rootOpen + "\n  ");
            var whitespaceLoaded = SemanticScheduleCatalog.Load(project);
            if (whitespaceLoaded.Count != 1)
                throw new InvalidOperationException("SemanticScheduleCDataGrammarSmoke: ordinary XML whitespace was not preserved as valid.");

            project.Metadata[SemanticScheduleCatalog.MetadataKey] =
                canonical.Replace(rootOpen, rootOpen + "<![CDATA[ \n ]]>" );

            try
            {
                SemanticScheduleCatalog.Load(project);
            }
            catch (InvalidDataException ex)
            {
                if (ex.Message.IndexOf("unsupported CDATA content", StringComparison.Ordinal) >= 0)
                    return;
                throw new InvalidOperationException("SemanticScheduleCDataGrammarSmoke: CDATA failed with the wrong contract.", ex);
            }

            throw new InvalidOperationException("SemanticScheduleCDataGrammarSmoke: whitespace-only CDATA was accepted.");
        }

        private static SemanticScheduleDefinition CreateDefinition()
        {
            return new SemanticScheduleDefinition(
                "schedule-cdata",
                "Schedule CDATA",
                "Schedule CDATA",
                Array.Empty<ElementCategory>(),
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("Element", "{Id}") });
        }
    }
}
