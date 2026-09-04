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
            QsdbProjectStructuralCardinality.Validate(callerRoot);

            var schema = ReadSchema(callerRoot);
            if (schema <= 0) throw new InvalidDataException("Unsupported QSDB schema version: " + schema.ToString(CultureInfo.InvariantCulture));
            if (schema > ProjectState.CurrentSchemaVersion) throw new InvalidDataException("QSDB schema is newer than this QS3D build: " + schema.ToString(CultureInfo.InvariantCulture));

            if (schema == ProjectState.CurrentSchemaVersion)
            {
                ValidatePrimaryIdentityCanonicality(callerRoot);
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

            QsdbProjectStructuralCardinality.Validate(root);
            ValidatePrimaryIdentityCanonicality(root);
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

        private static void ValidatePrimaryIdentityCanonicality(XElement root)
        {
            foreach (var item in root.Element("metadata")?.Elements("p") ?? Enumerable.Empty<XElement>())
                RequireCanonicalAttribute(item, "name", "Project metadata key");
            foreach (var zone in root.Element("zones")?.Elements("zone") ?? Enumerable.Empty<XElement>())
                RequireCanonicalAttribute(zone, "id", "Project zone id");
            foreach (var floor in root.Element("floors")?.Elements("floor") ?? Enumerable.Empty<XElement>())
                RequireCanonicalAttribute(floor, "id", "Project floor id");
            foreach (var family in root.Element("families")?.Elements("family") ?? Enumerable.Empty<XElement>())
                RequireCanonicalAttribute(family, "id", "Project family id");
            foreach (var rule in root.Element("rules")?.Elements("rule") ?? Enumerable.Empty<XElement>())
            {
                RequireCanonicalAttribute(rule, "id", "Quantity rule id");
                RequireCanonicalAttribute(rule, "output", "Quantity rule output");
            }
            foreach (var element in root.Element("elements")?.Elements("element") ?? Enumerable.Empty<XElement>())
            {
                RequireCanonicalAttribute(element, "id", "Project element id");
                foreach (var quantity in element.Element("quantities")?.Elements("q") ?? Enumerable.Empty<XElement>())
                    RequireCanonicalAttribute(quantity, "name", "Project element quantity name");
            }
        }

        private static void RequireCanonicalAttribute(XElement element, string attributeName, string owner)
        {
            var value = element.Attribute(attributeName)?.Value
                ?? throw new InvalidDataException(owner + " is missing required " + attributeName + ".");
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException(owner + " is missing required " + attributeName + ".");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException(owner + " must not contain leading/trailing whitespace.");
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
