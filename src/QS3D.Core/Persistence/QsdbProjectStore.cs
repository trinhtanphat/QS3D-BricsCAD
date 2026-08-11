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
            SaveCore(project, path, SaveMode.ReplaceWithBackup);
        }

        public void SaveNew(ProjectState project, string path)
        {
            SaveCore(project, path, SaveMode.PublishNew);
        }

        public void SavePreservingValidatedBackup(ProjectState project, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Project path is required.", nameof(path));
            var fullPath = Path.GetFullPath(path);
            var backupPath = fullPath + ".bak";
            if (!File.Exists(backupPath))
                throw new FileNotFoundException("A validated QSDB backup is required for recovery-safe publication.", backupPath);
            Load(backupPath);
            SaveCore(project, fullPath, SaveMode.ReplacePrimaryOnly);
            Load(fullPath);
            Load(backupPath);
        }

        private void SaveCore(ProjectState project, string path, SaveMode mode)
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
            var previousChangeVersion = project.ChangeVersion;
            var committed = false;

            try
            {
                project.SchemaVersion = ProjectState.CurrentSchemaVersion;
                project.Touch();
                var document = Serialize(project);
                document.Save(tempPath, SaveOptions.DisableFormatting);
                ValidateSerializedFile(tempPath);
                if (mode == SaveMode.PublishNew)
                    AtomicFileCommit.PublishNew(tempPath, fullPath, backupPath);
                else if (mode == SaveMode.ReplacePrimaryOnly)
                    AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
                else
                    AtomicFileCommit.ReplaceWithBackup(tempPath, fullPath, backupPath);
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    project.SchemaVersion = previousSchemaVersion;
                    project.RestorePersistenceState(previousUpdatedUtc, previousChangeVersion);
                }
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        private enum SaveMode
        {
            ReplaceWithBackup,
            PublishNew,
            ReplacePrimaryOnly
        }

        public ProjectState Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Project path is required.", nameof(path));
            var document = LoadDocument(path);
            ProjectSchemaMigrator.MigrateToCurrent(document);
            var root = document.Root ?? throw new InvalidDataException("QSDB has no root element.");
            var updatedUtc = Date(root.Attribute("updatedUtc")?.Value);
            var changeVersion = ChangeVersion(root.Attribute("changeVersion")?.Value);

            var project = new ProjectState(Required(root, "projectId"), Required(root, "name"))
            {
                SchemaVersion = ProjectState.CurrentSchemaVersion,
                DrawingPath = Value(root, "drawingPath"),
                DrawingFingerprint = Value(root, "drawingFingerprint"),
                ActiveZoneId = Value(root, "activeZoneId"),
                ActiveFloorId = Value(root, "activeFloorId")
            };
            project.RestorePersistenceState(updatedUtc, changeVersion);

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
                    var category = Category(item, "family");
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
                    var category = Category(item, "quantity rule");
                    project.QuantityRules.Add(new QuantityRule(
                        Required(item, "id"), category, Required(item, "output"), Required(item, "expression"), Required(item, "version")));
                }
            }

            var elements = root.Element("elements");
            if (elements != null)
            {
                foreach (var item in elements.Elements("element"))
                {
                    var category = Category(item, "element");
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
                    {
                        foreach (var q in quantities.Elements("q"))
                        {
                            var quantityName = Required(q, "name");
                            var quantityValue = Double(q.Attribute("value")?.Value);
                            if (element.Quantities.ContainsKey(quantityName))
                                throw new InvalidDataException("Duplicate QSDB element quantity name: " + element.Id + "/" + quantityName);
                            element.SetQuantity(quantityName, quantityValue);
                        }
                    }
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
                        Action = RawValue(item, "action"),
                        ElementId = RawValue(item, "elementId"),
                        Detail = RawValue(item, "detail"),
                        Actor = RawValue(item, "actor"),
                        CorrelationId = RawValue(item, "correlationId")
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
                new XAttribute("changeVersion", project.ChangeVersion.ToString(CultureInfo.InvariantCulture)),
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
            if (project.Families.Any(x => !Enum.IsDefined(typeof(ElementCategory), x.Category))) throw new InvalidDataException("QSDB family category is undefined.");
            if (project.Elements.Any(x => !Enum.IsDefined(typeof(ElementCategory), x.Category))) throw new InvalidDataException("QSDB element category is undefined.");
            if (project.QuantityRules.Any(x => !Enum.IsDefined(typeof(ElementCategory), x.Category))) throw new InvalidDataException("QSDB quantity rule category is undefined.");
            if (project.AuditEvents.Any(x => x == null)) throw new InvalidDataException("QSDB audit trail cannot contain null events.");
            ValidateUtcTimestamp(project.UpdatedUtc, "project UpdatedUtc");
            ValidateOptionalCanonicalValue(project.ActiveZoneId, "active zone id");
            ValidateOptionalCanonicalValue(project.ActiveFloorId, "active floor id");
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
                ValidateUtcTimestamp(element.UpdatedUtc, "element " + element.Id + " UpdatedUtc");
                ValidateOptionalCanonicalValue(element.FamilyId, "element " + element.Id + " family id");
                ValidateOptionalCanonicalValue(element.FloorId, "element " + element.Id + " floor id");
                ValidateOptionalCanonicalValue(element.ZoneId, "element " + element.Id + " zone id");
                ValidateCanonicalStringList(element.SourceHandles, "element " + element.Id + " source handles");
                ValidateCanonicalStringList(element.DependsOn, "element " + element.Id + " dependencies");
                ValidateStringMap(element.Properties, "element " + element.Id + " properties");
                foreach (var quantity in element.Quantities)
                {
                    ValidateCanonicalKey(quantity.Key, "element " + element.Id + " quantity name");
                    if (double.IsNaN(quantity.Value) || double.IsInfinity(quantity.Value)) throw new InvalidDataException("Element quantity must be finite: " + element.Id + "/" + quantity.Key);
                }
            }
            foreach (var audit in project.AuditEvents)
                ValidateUtcTimestamp(audit.Utc, "audit event UTC timestamp");
        }

        private static void ValidateStringMap(System.Collections.Generic.IDictionary<string, string> values, string label)
        {
            foreach (var key in values.Keys) ValidateCanonicalKey(key, label + " key");
        }

        private static void ValidateCanonicalStringList(System.Collections.Generic.IEnumerable<string> values, string label)
        {
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("QSDB " + label + " contains an empty value at index " + index + ".");
                if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                    throw new InvalidDataException("QSDB " + label + " contains a non-canonical padded value at index " + index + ".");
                if (!seen.Add(value))
                    throw new InvalidDataException("QSDB " + label + " contains a duplicate value at index " + index + ": " + value + ".");
                index++;
            }
        }

        private static void ValidateOptionalCanonicalValue(string? value, string label)
        {
            if (value == null || value.Length == 0) return;
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("QSDB " + label + " must not be whitespace.");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("QSDB " + label + " must not contain leading/trailing whitespace.");
        }

        private static void ValidateCanonicalKey(string key, string label)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new InvalidDataException("QSDB " + label + " must not be empty.");
            if (!string.Equals(key, key.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("QSDB " + label + " must not contain leading/trailing whitespace.");
        }

        private static void ValidateUtcTimestamp(DateTime value, string label)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new InvalidDataException("QSDB " + label + " must have DateTimeKind.Utc for deterministic persistence.");
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
                target[key] = RawValue(item, "value");
            }
        }

        private static string Required(XElement element, string attribute) => element.Attribute(attribute)?.Value is string value && !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new InvalidDataException("Missing attribute: " + attribute);
        private static string Value(XElement element, string attribute) => element.Attribute(attribute)?.Value?.Trim() ?? string.Empty;
        private static string RawValue(XElement element, string attribute) => element.Attribute(attribute)?.Value ?? string.Empty;

        private static ElementCategory Category(XElement element, string label)
        {
            var raw = Required(element, "category");
            if (!Enum.TryParse(raw, true, out ElementCategory category) || !Enum.IsDefined(typeof(ElementCategory), category))
                throw new InvalidDataException("Invalid " + label + " category: " + raw + ".");
            return category;
        }

        private static double Double(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0d;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || double.IsNaN(result) || double.IsInfinity(result))
                throw new InvalidDataException("Invalid QSDB numeric value: " + value);
            return result;
        }

        private static int Int(string? value, int fallback) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : fallback;

        private static long ChangeVersion(string? value)
        {
            if (value == null) return 0L;
            if (value.Length == 0 || !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result) || result < 0L)
                throw new InvalidDataException("Invalid QSDB change version: " + value);
            return result;
        }

        private static DateTime Date(string? value)
        {
            if (value == null) return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            if (string.IsNullOrWhiteSpace(value)) return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var raw = value.Trim();
            if (!HasExplicitUtcOffset(raw) || !DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                throw new InvalidDataException("Invalid QSDB UTC timestamp: " + value);
            return result.UtcDateTime;
        }

        private static bool HasExplicitUtcOffset(string value)
        {
            if (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase)) return true;
            var timeSeparator = value.IndexOf('T');
            if (timeSeparator < 0) return false;
            var offsetSeparator = Math.Max(value.LastIndexOf('+'), value.LastIndexOf('-'));
            return offsetSeparator > timeSeparator;
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
