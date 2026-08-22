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
            var root = document.Root ?? throw new InvalidDataException("QSDB has no root element.");
            if (!string.Equals(root.Name.LocalName, "qs3d", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("QSDB root element must be qs3d.");

            var schema = ReadSchema(root);
            if (schema <= 0) throw new InvalidDataException("Unsupported QSDB schema version: " + schema.ToString(CultureInfo.InvariantCulture));
            if (schema > ProjectState.CurrentSchemaVersion) throw new InvalidDataException("QSDB schema is newer than this QS3D build: " + schema.ToString(CultureInfo.InvariantCulture));

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
                    default:
                        throw new InvalidDataException("No migration path exists from QSDB schema " + schema.ToString(CultureInfo.InvariantCulture));
                }
                root.SetAttributeValue("schema", schema.ToString(CultureInfo.InvariantCulture));
            }
            return document;
        }

        private static int ReadSchema(XElement root)
        {
            var raw = root.Attribute("schema")?.Value;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var schema) ? schema : 0;
        }

        private static void MigrateV1ToV2(XElement root)
        {
            if (root.Attribute("updatedUtc") == null) root.SetAttributeValue("updatedUtc", LegacyUpdatedUtc);

            var elements = root.Element("elements");
            if (elements != null)
            {
                foreach (var element in elements.Elements("element"))
                {
                    if (element.Attribute("dirty") == null)
                        element.SetAttributeValue("dirty", ((int)ElementDirtyFlags.All).ToString(CultureInfo.InvariantCulture));
                    if (element.Attribute("updatedUtc") == null)
                        element.SetAttributeValue("updatedUtc", LegacyUpdatedUtc);
                }
            }

            SetMigrationOrigin(root, "1");
        }

        private static void MigrateV2ToV3(XElement root)
        {
            if (root.Element("rules") == null) root.Add(new XElement("rules"));
            if (root.Element("audit") == null) root.Add(new XElement("audit"));
            SetMigrationOrigin(root, "2");
        }

        private static void SetMigrationOrigin(XElement root, string version)
        {
            var metadata = root.Element("metadata");
            if (metadata == null)
            {
                metadata = new XElement("metadata");
                root.AddFirst(metadata);
            }
            var exists = metadata.Elements("p").Any(x => string.Equals(x.Attribute("name")?.Value, "QS3D.SchemaMigratedFrom", StringComparison.OrdinalIgnoreCase));
            if (!exists) metadata.Add(new XElement("p", new XAttribute("name", "QS3D.SchemaMigratedFrom"), new XAttribute("value", version)));
        }
    }
}
