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
            FailedFinalValidationDoesNotMutateInput();
            FailedIntermediateMigrationDoesNotMutateInput();
            SuccessfulLegacyMigrationPublishesInPlace();
            AlreadyCurrentDocumentKeepsCallerIdentity();
        }

        private static void FailedFinalValidationDoesNotMutateInput()
        {
            // A v1 document missing the required floors container reaches final
            // current-schema validation only after v1->v2->v3->v4 mutations.
            var document = XDocument.Parse(
                "<qs3d schema=\"1\" projectId=\"legacy\" name=\"Legacy\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                "<metadata/><zones/><families/><elements/></qs3d>",
                LoadOptions.PreserveWhitespace);
            var root = document.Root ?? throw new Exception("Final-validation fixture has no root.");
            var before = document.ToString(SaveOptions.DisableFormatting);

            RequireInvalidData(document, "Malformed legacy QSDB unexpectedly migrated successfully.");

            Require(ReferenceEquals(root, document.Root), "Rejected final validation replaced the caller root.");
            Require(string.Equals(before, document.ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal),
                "Rejected final validation mutated the caller-owned XDocument.");
            Require(document.Root?.Attribute("schema")?.Value == "1", "Rejected final validation advanced the caller schema version.");
            Require(document.Root?.Attribute("updatedUtc") == null, "Rejected final validation leaked v1 persistence defaults.");
            Require(document.Root?.Attribute("changeVersion") == null, "Rejected final validation leaked v2 persistence defaults.");
            Require(document.Root?.Element("rules") == null && document.Root?.Element("audit") == null,
                "Rejected final validation leaked later-version containers.");
            Require(document.Root?.Element("metadata")?.Element("p") == null,
                "Rejected final validation leaked migration provenance metadata.");
        }

        private static void FailedIntermediateMigrationDoesNotMutateInput()
        {
            var prefix = MeasurementWorkItemMappingPrefix();
            var reservedName = prefix + "atomicity-probe";
            var document = XDocument.Parse(
                "<qs3d schema=\"3\" projectId=\"legacy\" name=\"Legacy\" updatedUtc=\"2026-01-01T00:00:00Z\" changeVersion=\"0\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                "<metadata/><zones/><floors/><families/><rules/><elements/><audit/></qs3d>",
                LoadOptions.PreserveWhitespace);
            var root = document.Root ?? throw new Exception("Intermediate-failure fixture has no root.");
            root.Element("metadata")!.Add(new XElement("p", new XAttribute("name", reservedName), new XAttribute("value", "x")));
            var before = document.ToString(SaveOptions.DisableFormatting);

            RequireInvalidData(document, "Reserved v3 measurement/work-item metadata unexpectedly migrated successfully.");

            Require(ReferenceEquals(root, document.Root), "Intermediate migration failure replaced the caller root.");
            Require(string.Equals(before, document.ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal),
                "Intermediate migration failure mutated the caller-owned XDocument.");
            Require(document.Root?.Attribute("schema")?.Value == "3", "Intermediate migration failure advanced the caller schema version.");
            Require(string.Equals(document.Root?.Element("metadata")?.Element("p")?.Attribute("name")?.Value, reservedName, StringComparison.Ordinal),
                "Intermediate migration failure rewrote the caller metadata.");
            Require(document.Root?.Element("metadata")?.Elements("p") != null,
                "Intermediate migration failure removed caller metadata.");
        }

        private static void SuccessfulLegacyMigrationPublishesInPlace()
        {
            var document = XDocument.Parse(
                "<qs3d schema=\"1\" projectId=\"legacy\" name=\"Legacy\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                "<metadata/><zones/><floors/><families/><elements/></qs3d>",
                LoadOptions.PreserveWhitespace);
            var originalRoot = document.Root ?? throw new Exception("Successful-migration fixture has no root.");

            var returned = Migrate(document);

            Require(ReferenceEquals(document, returned), "Successful migration stopped returning the caller-owned XDocument.");
            Require(!ReferenceEquals(originalRoot, document.Root), "Successful migration did not publish the validated detached root.");
            Require(document.Root?.Attribute("schema")?.Value == ProjectState.CurrentSchemaVersion.ToString(),
                "Successful legacy migration did not publish the current schema.");
            Require(document.Root?.Attribute("updatedUtc")?.Value == "1970-01-01T00:00:00.0000000Z",
                "Successful v1 migration did not publish the legacy updatedUtc default.");
            Require(document.Root?.Attribute("changeVersion")?.Value == "0",
                "Successful v2 migration did not publish the changeVersion default.");
            Require(document.Root?.Element("rules") != null && document.Root?.Element("audit") != null,
                "Successful migration did not publish required current-schema containers.");
            Require(document.Root?.Element("metadata")?.Element("p")?.Attribute("name")?.Value == "QS3D.SchemaMigratedFrom",
                "Successful migration did not publish migration provenance.");
        }

        private static void AlreadyCurrentDocumentKeepsCallerIdentity()
        {
            var document = XDocument.Parse(
                "<qs3d schema=\"4\" projectId=\"current\" name=\"Current\" updatedUtc=\"2026-01-01T00:00:00.0000000Z\" changeVersion=\"0\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                "<metadata/><zones/><floors/><families/><rules/><elements/><audit/></qs3d>",
                LoadOptions.PreserveWhitespace);
            var root = document.Root ?? throw new Exception("Current-schema fixture has no root.");
            var before = document.ToString(SaveOptions.DisableFormatting);

            var returned = Migrate(document);

            Require(ReferenceEquals(document, returned), "Current-schema migration stopped returning the caller-owned XDocument.");
            Require(ReferenceEquals(root, document.Root), "Current-schema validation replaced an already-current caller root.");
            Require(string.Equals(before, document.ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal),
                "Current-schema validation mutated a valid caller document.");
        }

        private static string MeasurementWorkItemMappingPrefix()
        {
            var assembly = typeof(QsdbProjectStore).Assembly;
            var type = assembly.GetType("QS3D.Core.Persistence.ProjectMeasurementWorkItemMappingCodec", throwOnError: true)
                ?? throw new Exception("ProjectMeasurementWorkItemMappingCodec type was not found.");
            var field = type.GetField("Prefix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new Exception("ProjectMeasurementWorkItemMappingCodec.Prefix was not found.");
            var value = field.GetValue(null) as string;
            if (string.IsNullOrEmpty(value)) throw new Exception("ProjectMeasurementWorkItemMappingCodec.Prefix is empty.");
            return value;
        }

        private static void RequireInvalidData(XDocument document, string message)
        {
            try
            {
                Migrate(document);
            }
            catch (InvalidDataException)
            {
                return;
            }
            throw new Exception(message);
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
                throw ex.InnerException!;
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
