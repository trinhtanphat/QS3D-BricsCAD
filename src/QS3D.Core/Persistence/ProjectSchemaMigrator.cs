using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Persistence
{
    internal static class ProjectSchemaMigrator
    {
        private const string LegacyUpdatedUtc = "1970-01-01T00:00:00.0000000Z";

        public static XDocument MigrateToCurrent(XDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var callerRoot = document.Root ?? throw new InvalidDataException("QSDB has no root element.");
            if (!string.Equals(callerRoot.Name.LocalName, "qs3d", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("QSDB root element must be qs3d.");

            var schema = ReadSchema(callerRoot);
            if (schema <= 0) throw new InvalidDataException("Unsupported QSDB schema version: " + schema.ToString(CultureInfo.InvariantCulture));
            if (schema > ProjectState.CurrentSchemaVersion) throw new InvalidDataException("QSDB schema is newer than this QS3D build: " + schema.ToString(CultureInfo.InvariantCulture));

            if (schema == ProjectState.CurrentSchemaVersion)
            {
                ValidateCurrentPersistenceState(callerRoot);
                QsdbProjectXmlSchemaValidator.ValidateCurrent(callerRoot);
                return document;
            }

            var workingDocument = new XDocument(document);
            var root = workingDocument.Root ?? throw new InvalidDataException("QSDB has no root element.");

            while (schema < ProjectState.CurrentSchemaVersion)
            {
                switch (schema)
                {
                    case 1:
                        MigrateV1ToV2(root);
                        schema = 2;
                        break;
                    case 2:
                        MigrateV2ToV3(root);
                        schema = 3;
                        break;
                    case 3:
                        MigrateV3ToV4(root);
                        schema = 4;
                        break;
                    default:
                        throw new InvalidDataException("No migration path exists from QSDB schema " + schema.ToString(CultureInfo.InvariantCulture));
                }
                root.SetAttributeValue("schema", schema.ToString(CultureInfo.InvariantCulture));
            }

            ValidateCurrentPersistenceState(root);
            QsdbProjectXmlSchemaValidator.ValidateCurrent(root);

            callerRoot.ReplaceWith(new XElement(root));
            return document;
        }

        private static int ReadSchema(XElement root)
        {
            var raw = root.Attribute("schema")?.Value;
            if (string.IsNullOrEmpty(raw) || !int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var schema)) return 0;
            return string.Equals(raw, schema.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) ? schema : 0;
        }

        private static void MigrateV1ToV2(XElement root)
        {
            if (root.Attribute("updatedUtc") == null) root.SetAttributeValue("updatedUtc", LegacyUpdatedUtc);
            var elements = root.Element("elements");
            if (elements != null)
            {
                foreach (var element in elements.Elements("element"))
                {
                    if (element.Attribute("dirty") == null) element.SetAttributeValue("dirty", ((int)ElementDirtyFlags.All).ToString(CultureInfo.InvariantCulture));
                    if (element.Attribute("updatedUtc") == null) element.SetAttributeValue("updatedUtc", LegacyUpdatedUtc);
                }
            }
            SetMigrationOrigin(root, "1");
        }

        private static void MigrateV2ToV3(XElement root)
        {
            if (root.Attribute("changeVersion") == null) root.SetAttributeValue("changeVersion", "0");
            if (root.Element("rules") == null) root.Add(new XElement("rules"));
            if (root.Element("audit") == null) root.Add(new XElement("audit"));
            SetMigrationOrigin(root, "2");
        }

        private static void MigrateV3ToV4(XElement root)
        {
            var metadata = root.Element("metadata");
            if (metadata != null && metadata.Elements("p").Any(x =>
                (x.Attribute("name")?.Value ?? string.Empty).StartsWith(ProjectMeasurementWorkItemMappingCodec.Prefix, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("QSDB v3 metadata uses the reserved measurement/work-item mapping namespace and cannot be migrated automatically.");
            SetMigrationOrigin(root, "3");
        }

        private static void ValidateCurrentPersistenceState(XElement root)
        {
            RequirePersistenceValue(root, "updatedUtc", "Project root");
            RequirePersistenceValue(root, "changeVersion", "Project root");
            RequireSingleContainer(root, "metadata");
            RequireSingleContainer(root, "zones");
            var floors = RequireSingleContainer(root, "floors");
            RequireSingleContainer(root, "families");
            RequireSingleContainer(root, "rules");
            var elements = RequireSingleContainer(root, "elements");
            var audit = RequireSingleContainer(root, "audit");
            foreach (var floor in floors.Elements("floor")) RequirePersistenceValue(floor, "elevationM", "Project floor");
            foreach (var element in elements.Elements("element"))
            {
                RequirePersistenceValue(element, "updatedUtc", "Project element");
                RequirePersistenceValue(element, "dirty", "Project element");
                var quantities = element.Element("quantities");
                if (quantities != null) foreach (var quantity in quantities.Elements("q")) RequirePersistenceValue(quantity, "value", "Project quantity");
            }
            foreach (var auditEvent in audit.Elements("event")) RequirePersistenceValue(auditEvent, "utc", "Audit event");
        }

        private static XElement RequireSingleContainer(XElement root, string name)
        {
            var matches = root.Elements(name).Take(2).ToArray();
            if (matches.Length != 1) throw new InvalidDataException("QSDB requires exactly one " + name + " section.");
            return matches[0];
        }

        private static void RequirePersistenceValue(XElement element, string attributeName, string owner)
        {
            if (string.IsNullOrWhiteSpace(element.Attribute(attributeName)?.Value)) throw new InvalidDataException(owner + " is missing required " + attributeName + ".");
        }

        private static void SetMigrationOrigin(XElement root, string version)
        {
            var metadata = root.Element("metadata");
            if (metadata == null) { metadata = new XElement("metadata"); root.AddFirst(metadata); }
            var exists = metadata.Elements("p").Any(x => string.Equals(x.Attribute("name")?.Value, "QS3D.SchemaMigratedFrom", StringComparison.OrdinalIgnoreCase));
            if (!exists) metadata.Add(new XElement("p", new XAttribute("name", "QS3D.SchemaMigratedFrom"), new XAttribute("value", version)));
        }
    }
}
