using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.Persistence
{
    public sealed class QsdbProjectStore
    {
        private const long MaxProjectFileBytes = 64L * 1024L * 1024L;

        public void Save(ProjectState project, string path)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Project path is required.", nameof(path));

            ValidateProject(project);
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            var backupPath = fullPath + ".bak";
            var previousSchemaVersion = project.SchemaVersion;
            var previousUpdatedUtc = project.UpdatedUtc;
            var committed = false;

            try
            {
                project.SchemaVersion = ProjectState.CurrentSchemaVersion;
                project.Touch();
                var document = Serialize(project);
                document.Save(tempPath, SaveOptions.DisableFormatting);
                ValidateSerializedFile(tempPath);
                AtomicFileCommit.ReplaceWithBackup(tempPath, fullPath, backupPath);
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    project.SchemaVersion = previousSchemaVersion;
                    project.UpdatedUtc = previousUpdatedUtc;
                }
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        public ProjectState Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Project path is required.", nameof(path));
            var document = LoadDocument(path);
            ProjectSchemaMigrator.MigrateToCurrent(document);
            var root = document.Root ?? throw new InvalidDataException("QSDB has no root element.");

            var project = new ProjectState(Required(root, "projectId"), Required(root, "name"))
            {
                SchemaVersion = ProjectState.CurrentSchemaVersion,
                DrawingPath = Value(root, "drawingPath"),
                DrawingFingerprint = Value(root, "drawingFingerprint"),
                ActiveZoneId = Value(root, "activeZoneId"),
                ActiveFloorId = Value(root, "activeFloorId"),
                UpdatedUtc = Date(root.Attribute("updatedUtc")?.Value)
            };

            var zones = root.Element("zones");
            if (zones != null)
                foreach (var item in zones.Elements("zone")) project.Zones.Add(new ZoneDefinition(Required(item, "id"), Required(item, "name")));

            var floors = root.Element("floors");
            if (floors != null)
                foreach (var item in floors.Elements("floor")) project.Floors.Add(new FloorDefinition(Required(item, "id"), Required(item, "name"), Double(item.Attribute("elevationM")?.Value)));

            var families = root.Element("families");
            if (families != null)
            {
                foreach (var item in families.Elements("family"))
                {
                    if (!Enum.TryParse(Required(item, "category"), true, out ElementCategory category)) throw new InvalidDataException("Invalid family category.");
                    var family = new ProjectFamily(Required(item, "id"), Required(item, "name"), category);
                    ReadStringMap(item.Element("properties"), "p", family.Properties);
                    project.Families.Add(family);
                }
            }

            var rules = root.Element("rules");
            if (rules != null)
            {
                foreach (var item in rules.Elements("rule"))
                {
                    if (!Enum.TryParse(Required(item, "category"), true, out ElementCategory category)) throw new InvalidDataException("Invalid quantity rule category.");
                    project.QuantityRules.Add(new QuantityRule(
                        Required(item, "id"), category, Required(item, "output"), Required(item, "expression"), Required(item, "version")));
                }
            }

            var elements = root.Element("elements");
            if (elements != null)
            {
                foreach (var item in elements.Elements("element"))
                {
                    if (!Enum.TryParse(Required(item, "category"), true, out ElementCategory category)) throw new InvalidDataException("Invalid element category.");
                    var element = new ProjectElement(Required(item, "id"), category, Value(item, "familyId"), Value(item, "floorId"), Value(item, "zoneId"))
                    {
                        DrawingFingerprint = Value(item, "drawingFingerprint")
                    };
                    foreach (var handle in item.Element("handles")?.Elements("h") ?? Enumerable.Empty<XElement>())
                        if (!string.IsNullOrWhiteSpace(handle.Value)) element.SourceHandles.Add(handle.Value.Trim());
                    foreach (var dep in item.Element("dependencies")?.Elements("d") ?? Enumerable.Empty<XElement>())
                        if (!string.IsNullOrWhiteSpace(dep.Value)) element.DependsOn.Add(dep.Value.Trim());
                    ReadStringMap(item.Element("properties"), "p", element.Properties);
                    var quantities = item.Element("quantities");
                    if (quantities != null)
                        foreach (var q in quantities.Elements("q")) element.SetQuantity(Required(q, "name"), Double(q.Attribute("value")?.Value));
                    element.RestorePersistenceState(Dirty(item.Attribute("dirty")?.Value), Date(item.Attribute("updatedUtc")?.Value));
                    project.Elements.Add(element);
                }
            }

            var audit = root.Element("audit");
            if (audit != null)
            {
                foreach (var item in audit.Elements("event"))
                {
                    project.AuditEvents.Add(new AuditEvent
                    {
                        Utc = Date(item.Attribute("utc")?.Value),
                        Action = Value(item, "action"),
                        ElementId = Value(item, "elementId"),
                        Detail = Value(item, "detail"),
                        Actor = Value(item, "actor"),
                        CorrelationId = Value(item, "correlationId")
                    });
                }
            }

            ReadStringMap(root.Element("metadata"), "p", project.Metadata);
            ValidateProject(project);
            return project;
        }

        public ProjectLoadResult LoadWithBackupFallback(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Project path is required.", nameof(path));
            var fullPath = Path.GetFullPath(path);
            try
            {
                return new ProjectLoadResult(Load(fullPath), fullPath, false, string.Empty);
            }
            catch (Exception primary) when (IsRecoverableDataFailure(primary))
            {
                var backupPath = fullPath + ".bak";
                if (!File.Exists(backupPath)) throw;
                try
                {
                    var project = Load(backupPath);
                    return new ProjectLoadResult(project, backupPath, true, primary.Message);
                }
                catch (Exception backup) when (IsRecoverableDataFailure(backup))
                {
                    throw new InvalidDataException("Both the QSDB project and its backup are invalid.", new AggregateException(primary, backup));
                }
            }
        }

        private static XDocument Serialize(ProjectState project)
        {
            return new XDocument(new XElement("qs3d",
                new XAttribute("schema", ProjectState.CurrentSchemaVersion),
                new XAttribute("projectId", project.ProjectId),
                new XAttribute("name", project.Name),
                new XAttribute("updatedUtc", project.UpdatedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
                new XAttribute("drawingPath", project.DrawingPath ?? string.Empty),
                new XAttribute("drawingFingerprint", project.DrawingFingerprint ?? string.Empty),
                new XAttribute("activeZoneId", project.ActiveZoneId ?? string.Empty),
                new XAttribute("activeFloorId", project.ActiveFloorId ?? string.Empty),
                Map("metadata", project.Metadata),
                new XElement("zones", project.Zones.Select(x => new XElement("zone", new XAttribute("id", x.Id), new XAttribute("name", x.Name)))),
                new XElement("floors", project.Floors.Select(x => new XElement("floor", new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("elevationM", F(x.ElevationM))))),
                new XElement("families", project.Families.Select(x => new XElement("family", new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("category", x.Category), Map("properties", x.Properties)))),
                new XElement("rules", project.QuantityRules.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(x => new XElement("rule",
                    new XAttribute("id", x.Id), new XAttribute("category", x.Category), new XAttribute("output", x.OutputName), new XAttribute("expression", x.Expression), new XAttribute("version", x.Version)))),
                new XElement("elements", project.Elements.Select(x => new XElement("element",
                    new XAttribute("id", x.Id), new XAttribute("category", x.Category), new XAttribute("familyId", x.FamilyId ?? string.Empty),
                    new XAttribute("floorId", x.FloorId ?? string.Empty), new XAttribute("zoneId", x.ZoneId ?? string.Empty), new XAttribute("drawingFingerprint", x.DrawingFingerprint ?? string.Empty),
                    new XAttribute("dirty", ((int)x.Dirty).ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("updatedUtc", x.UpdatedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
                    new XElement("handles", x.SourceHandles.Select(h => new XElement("h", h))),
                    new XElement("dependencies", x.DependsOn.Select(d => new XElement("d", d))),
                    Map("properties", x.Properties),
                    new XElement("quantities", x.Quantities.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase).Select(q => new XElement("q", new XAttribute("name", q.Key), new XAttribute("value", F(q.Value)))))))),
                new XElement("audit", project.AuditEvents.OrderBy(x => x.Utc).Select(x => new XElement("event",
                    new XAttribute("utc", x.Utc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
                    new XAttribute("action", x.Action ?? string.Empty),
                    new XAttribute("elementId", x.ElementId ?? string.Empty),
                    new XAttribute("detail", x.Detail ?? string.Empty),
                    new XAttribute("actor", x.Actor ?? string.Empty),
                    new XAttribute("correlationId", x.CorrelationId ?? string.Empty))))));
        }

        private static XDocument LoadDocument(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > MaxProjectFileBytes)
                throw new InvalidDataException("QSDB project exceeds the maximum supported file size of 64 MiB.");

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxProjectFileBytes
            };

            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = XmlReader.Create(stream, settings))
            {
                return XDocument.Load(reader, LoadOptions.None);
            }
        }

        private static void ValidateSerializedFile(string path)
        {
            var document = LoadDocument(path);
            var root = document.Root ?? throw new InvalidDataException("Serialized QSDB has no root element.");
            if (!string.Equals(root.Name.LocalName, "qs3d", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Serialized QSDB root is invalid.");
            var schema = Int(root.Attribute("schema")?.Value, 0);
            if (schema != ProjectState.CurrentSchemaVersion) throw new InvalidDataException("Serialized QSDB schema is invalid.");
            Required(root, "projectId");
            Required(root, "name");
        }

        private static void ValidateProject(ProjectState project)
        {
            if (string.IsNullOrWhiteSpace(project.Name)) throw new InvalidDataException("QSDB project name is required.");
            if (project.Zones.Any(x => x == null || string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Name))) throw new InvalidDataException("QSDB zones require non-empty ids and names.");
            if (project.Floors.Any(x => x == null || string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Name))) throw new InvalidDataException("QSDB floors require non-empty ids and names.");
            if (project.Families.Any(x => x == null || string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Name))) throw new InvalidDataException("QSDB families require non-empty ids and names.");
            if (project.Elements.Any(x => x == null || string.IsNullOrWhiteSpace(x.Id))) throw new InvalidDataException("QSDB elements require non-empty ids.");
            if (project.QuantityRules.Any(x => x == null || string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.OutputName))) throw new InvalidDataException("QSDB quantity rules require non-empty ids and outputs.");
            var duplicateFamily = project.Families.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateFamily != null) throw new InvalidDataException("Duplicate family id in QSDB: " + duplicateFamily.Key);
            var duplicateElement = project.Elements.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateElement != null) throw new InvalidDataException("Duplicate element id in QSDB: " + duplicateElement.Key);
            var duplicateZone = project.Zones.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateZone != null) throw new InvalidDataException("Duplicate zone id in QSDB: " + duplicateZone.Key);
            var duplicateFloor = project.Floors.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateFloor != null) throw new InvalidDataException("Duplicate floor id in QSDB: " + duplicateFloor.Key);
            var duplicateRule = project.QuantityRules.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateRule != null) throw new InvalidDataException("Duplicate quantity rule id in QSDB: " + duplicateRule.Key);
            var duplicateOutput = project.QuantityRules.GroupBy(x => x.Category + "\u001f" + x.OutputName, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateOutput != null) throw new InvalidDataException("Multiple quantity rules target the same category/output: " + duplicateOutput.Key.Replace("\u001f", "/"));

            foreach (var floor in project.Floors)
                if (double.IsNaN(floor.ElevationM) || double.IsInfinity(floor.ElevationM)) throw new InvalidDataException("Floor elevation must be finite: " + floor.Id);
            ValidateStringMap(project.Metadata, "project metadata");
            foreach (var family in project.Families) ValidateStringMap(family.Properties, "family " + family.Id + " properties");
            foreach (var element in project.Elements)
            {
                ValidateStringMap(element.Properties, "element " + element.Id + " properties");
                foreach (var quantity in element.Quantities)
                {
                    if (string.IsNullOrWhiteSpace(quantity.Key)) throw new InvalidDataException("Element quantity names must not be empty: " + element.Id);
                    if (double.IsNaN(quantity.Value) || double.IsInfinity(quantity.Value)) throw new InvalidDataException("Element quantity must be finite: " + element.Id + "/" + quantity.Key);
                }
            }
        }

        private static void ValidateStringMap(System.Collections.Generic.IDictionary<string, string> values, string label)
        {
            if (values.Keys.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("QSDB " + label + " contains an empty key.");
        }

        private static bool IsRecoverableDataFailure(Exception exception) => exception is InvalidDataException || exception is XmlException || exception is FormatException || exception is FileNotFoundException;

        private static XElement Map(string name, System.Collections.Generic.IDictionary<string, string> values) =>
            new XElement(name, values.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => new XElement("p", new XAttribute("name", x.Key), new XAttribute("value", x.Value ?? string.Empty))));

        private static void ReadStringMap(XElement? container, string itemName, System.Collections.Generic.IDictionary<string, string> target)
        {
            if (container == null) return;
            foreach (var item in container.Elements(itemName))
            {
                var key = Required(item, "name");
                if (target.ContainsKey(key)) throw new InvalidDataException("Duplicate QSDB map key: " + key);
                target[key] = Value(item, "value");
            }
        }

        private static string Required(XElement element, string attribute) => element.Attribute(attribute)?.Value is string value && !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new InvalidDataException("Missing attribute: " + attribute);
        private static string Value(XElement element, string attribute) => element.Attribute(attribute)?.Value?.Trim() ?? string.Empty;

        private static double Double(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0d;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || double.IsNaN(result) || double.IsInfinity(result))
                throw new InvalidDataException("Invalid QSDB numeric value: " + value);
            return result;
        }

        private static int Int(string? value, int fallback) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : fallback;

        private static DateTime Date(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
                throw new InvalidDataException("Invalid QSDB UTC timestamp: " + value);
            return result.ToUniversalTime();
        }

        private static ElementDirtyFlags Dirty(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return ElementDirtyFlags.None;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var raw) || raw < 0 || (raw & ~(int)ElementDirtyFlags.All) != 0)
                throw new InvalidDataException("Invalid QSDB dirty flags: " + value);
            return (ElementDirtyFlags)raw;
        }

        private static string F(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidDataException("QSDB numeric values must be finite.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}