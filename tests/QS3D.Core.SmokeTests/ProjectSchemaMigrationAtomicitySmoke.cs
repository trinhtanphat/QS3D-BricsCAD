using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSchemaMigrationAtomicitySmoke
    {
        public static void Run()
        {
            FailedLegacyMigrationDoesNotMutateInput();
            IntermediateMigrationFailureDoesNotMutateInput();
            SuccessfulLegacyMigrationStillPublishesInPlace();
        }

        private static void FailedLegacyMigrationDoesNotMutateInput()
        {
            var document = XDocument.Parse(
                "<qs3d schema=\"1\" projectId=\"legacy\" name=\"Legacy\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                "<metadata/><zones/><families/><elements/></qs3d>",
                LoadOptions.PreserveWhitespace);
            var before = document.ToString(SaveOptions.DisableFormatting);

            var rejected = false;
            try
            {
                Migrate(document);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Require(rejected, "Malformed legacy QSDB unexpectedly migrated successfully.");
            Require(string.Equals(before, document.ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal),
                "Rejected legacy migration mutated the caller-owned XDocument.");
            Require(document.Root?.Attribute("schema")?.Value == "1", "Rejected migration advanced the caller schema version.");
            Require(document.Root?.Attribute("updatedUtc") == null, "Rejected migration leaked v1 persistence defaults into the caller document.");
            Require(document.Root?.Element("rules") == null && document.Root?.Element("audit") == null,
                "Rejected migration leaked later-version containers into the caller document.");
        }

        private static void IntermediateMigrationFailureDoesNotMutateInput()
        {
            var document = XDocument.Parse(
                "<qs3d schema=\"3\" projectId=\"legacy\" name=\"Legacy\" updatedUtc=\"2026-01-01T00:00:00Z\" changeVersion=\"0\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                "<metadata><p name=\"QS3D.MeasurementWorkItemMapping.bad\" value=\"x\"/></metadata><zones/><floors/><families/><rules/><elements/><audit/></qs3d>",
                LoadOptions.PreserveWhitespace);
            var before = document.ToString(SaveOptions.DisableFormatting);

            var rejected = false;
            try
            {
                Migrate(document);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Require(rejected, "Reserved v3 measurement/work-item metadata unexpectedly migrated successfully.");
            Require(string.Equals(before, document.ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal),
                "Intermediate migration failure mutated the caller-owned XDocument.");
            Require(document.Root?.Attribute("schema")?.Value == "3", "Intermediate migration failure advanced the caller schema version.");
            Require(document.Root?.Element("metadata")?.Element("p")?.Attribute("name")?.Value == "QS3D.MeasurementWorkItemMapping.bad",
                "Intermediate migration failure rewrote the caller metadata.");
        }

        private static void SuccessfulLegacyMigrationStillPublishesInPlace()
        {
            var document = XDocument.Parse(
                "<qs3d schema=\"1\" projectId=\"legacy\" name=\"Legacy\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                "<metadata/><zones/><floors/><families/><elements/></qs3d>",
                LoadOptions.PreserveWhitespace);

            var returned = Migrate(document);

            Require(ReferenceEquals(document, returned), "Successful migration stopped returning the caller-owned XDocument.");
            Require(document.Root?.Attribute("schema")?.Value == ProjectState.CurrentSchemaVersion.ToString(),
                "Successful legacy migration did not publish the current schema to the caller document.");
            Require(document.Root?.Element("rules") != null && document.Root?.Element("audit") != null,
                "Successful migration did not publish required current-schema containers.");
            Require(document.Root?.Element("metadata")?.Elements("p") != null,
                "Successful migration lost metadata while publishing the validated root.");
        }

        private static XDocument Migrate(XDocument document)
        {
            var type = typeof(QsdbProjectStore).Assembly.GetType("QS3D.Core.Persistence.ProjectSchemaMigrator", throwOnError: true)
                ?? throw new Exception("ProjectSchemaMigrator type was not found.");
            var method = type.GetMethod("MigrateToCurrent", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new Exception("ProjectSchemaMigrator.MigrateToCurrent was not found.");
            try
            {
                return (XDocument)(method.Invoke(null, new object[] { document })
                    ?? throw new Exception("ProjectSchemaMigrator returned null."));
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
