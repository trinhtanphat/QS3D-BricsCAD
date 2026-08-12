using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleWhitespacePayloadSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MissingAndEmptyRemainEmptyButWhitespaceFailsClosed();
        }

        private static void MissingAndEmptyRemainEmptyButWhitespaceFailsClosed()
        {
            var project = new ProjectState("P-schedule-whitespace", "Schedule Whitespace");

            if (SemanticScheduleCatalog.Load(project).Count != 0)
                throw new InvalidOperationException("SemanticScheduleWhitespacePayloadSmoke: missing metadata did not load as an empty catalog.");

            project.Metadata[SemanticScheduleCatalog.MetadataKey] = string.Empty;
            if (SemanticScheduleCatalog.Load(project).Count != 0)
                throw new InvalidOperationException("SemanticScheduleWhitespacePayloadSmoke: exact empty metadata did not load as an empty catalog.");

            project.Metadata[SemanticScheduleCatalog.MetadataKey] = " \r\n\t ";
            try
            {
                SemanticScheduleCatalog.Load(project);
            }
            catch (InvalidDataException)
            {
                return;
            }

            throw new InvalidOperationException("SemanticScheduleWhitespacePayloadSmoke: whitespace-only persisted metadata was silently treated as an empty catalog.");
        }
    }
}
