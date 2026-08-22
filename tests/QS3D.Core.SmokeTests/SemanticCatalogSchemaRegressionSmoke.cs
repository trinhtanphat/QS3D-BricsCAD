using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticCatalogSchemaRegressionSmoke
    {
        internal static void Run()
        {
            ScheduleCatalogRejectsUnsupportedSchema();
            DocumentationCatalogRejectsUnsupportedSchema();
        }

        private static void ScheduleCatalogRejectsUnsupportedSchema()
        {
            ScheduleLoadMustFail("<semanticSchedules xmlns='urn:qs3d:future' version='1'/>");
            ScheduleLoadMustFail("<semanticSchedules version='1' future='1'/>");
            ScheduleLoadMustFail("<semanticSchedules version='1'><future/></semanticSchedules>");
            ScheduleLoadMustFail("<semanticSchedules version='1'><schedule id='S1' name='Schedule' title='Schedule'><categories/><categories/><columns/></schedule></semanticSchedules>");
        }

        private static void DocumentationCatalogRejectsUnsupportedSchema()
        {
            DocumentationLoadMustFail("<documentation xmlns='urn:qs3d:future' version='1'><views/><sheets/></documentation>");
            DocumentationLoadMustFail("<documentation version='1' future='1'><views/><sheets/></documentation>");
            DocumentationLoadMustFail("<documentation version='1'><views/><sheets/><future/></documentation>");
            DocumentationLoadMustFail("<documentation version='1'><views/><views/><sheets/></documentation>");
        }

        private static void ScheduleLoadMustFail(string payload)
        {
            var project = new ProjectState("P-SCHEDULE-SCHEMA", "Schedule Schema");
            project.Metadata[SemanticScheduleCatalog.MetadataKey] = payload;
            ThrowsInvalidData(() => SemanticScheduleCatalog.Load(project), "Schedule catalog accepted unsupported schema.");
        }

        private static void DocumentationLoadMustFail(string payload)
        {
            var project = new ProjectState("P-DOCUMENTATION-SCHEMA", "Documentation Schema");
            project.Metadata[SemanticDocumentationCatalogStore.MetadataKey] = payload;
            ThrowsInvalidData(() => new SemanticDocumentationCatalogStore().Load(project), "Documentation catalog accepted unsupported schema.");
        }

        private static void ThrowsInvalidData(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidDataException)
            {
                return;
            }

            throw new Exception(message);
        }
    }

    internal static class SemanticCatalogSchemaRegressionSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticCatalogSchemaRegressionSmoke.Run();
    }
}
