using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Persistence
{
    public sealed class QsdbProjectStore
    {
        public void Save(ProjectState project, string path)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Project path is required.", nameof(path));

            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var tempPath = fullPath + ".tmp";
            var backupPath = fullPath + ".bak";

            project.SchemaVersion = ProjectState.CurrentSchemaVersion;
            project.Touch();
            var document = Serialize(project);

            try
            {
                document.Save(tempPath, SaveOptions.DisableFormatting);
                ValidateSerializedFile(tempPath);

                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Replace(tempPath, fullPath, backupPath, true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(fullPath, backupPath, true);
                        File.Delete(fullPath);
                        File.Move(tempPath, fullPath);
                    }
                }
                else
                {
                    File.Move(tempPath, fullPath);
                }
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        public ProjectState Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Project path is required.", nameof(path));
            var document = XDocument.Load(path, LoadOptions.None);
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
                    foreach (var handle in item.Element("handles")?.Elements("h") ?? Enumerable.Empty<XElement>()) if (!string.IsNullOrWhiteSpace(handle.Value)) element.SourceHandles.Add(handle.Value.Trim());
                    foreach (var dep in item.Element("dependencies")?.Elements("d") ?? Enumerable.Empty<XElement>()) if (!string.IsNullOrWhiteSpace(dep.Value)) element.DependsOn.Add(dep.Value.Trim());
                    ReadStringMap(item.Element("properties"), "p", element.Properties);
                    var quantities = item.Element("quantities");
                    if (quantities != null)
                        foreach (var q in quantities.Elements("q")) element.SetQuantity(Required(q, "name"), Double(q.Attribute("value")?.Value));
                    element.MarkClean(ElementDirtyFlags.All);
                    project.Elements.Add(element);
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
                new XElement("elements", project.Elements.Select(x => new XElement("element",
                    new XAttribute("id", x.Id), new XAttribute("category", x.Category), new XAttribute("familyId", x.FamilyId ?? string.Empty),
                    new XAttribute("floorId", x.FloorId ?? string.Empty), new XAttribute("zoneId", x.ZoneId ?? string.Empty), new XAttribute("drawingFingerprint", x.DrawingFingerprint ?? string.Empty),
                    new XElement("handles", x.SourceHandles.Select(h => new XElement("h", h))),
                    new XElement("dependencies", x.DependsOn.Select(d => new XElement("d", d))),
                    Map("properties", x.Properties),
                    new XElement("quantities", x.Quantities.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase).Select(q => new XElement("q", new XAttribute("name", q.Key), new XAttribute("value", F(q.Value))))))))));
        }

        private static void ValidateSerializedFile(string path)
        {
            var document = XDocument.Load(path, LoadOptions.None);
            var root = document.Root ?? throw new InvalidDataException("Serialized QSDB has no root element.");
            if (!string.Equals(root.Name.LocalName, "qs3d", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Serialized QSDB root is invalid.");
            var schema = Int(root.Attribute("schema")?.Value, 0);
            if (schema != ProjectState.CurrentSchemaVersion) throw new InvalidDataException("Serialized QSDB schema is invalid.");
            Required(root, "projectId");
            Required(root, "name");
        }

        private static void ValidateProject(ProjectState project)
        {
            var duplicateFamily = project.Families.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateFamily != null) throw new InvalidDataException("Duplicate family id in QSDB: " + duplicateFamily.Key);
            var duplicateElement = project.Elements.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateElement != null) throw new InvalidDataException("Duplicate element id in QSDB: " + duplicateElement.Key);
            var duplicateZone = project.Zones.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateZone != null) throw new InvalidDataException("Duplicate zone id in QSDB: " + duplicateZone.Key);
            var duplicateFloor = project.Floors.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateFloor != null) throw new InvalidDataException("Duplicate floor id in QSDB: " + duplicateFloor.Key);
        }

        private static bool IsRecoverableDataFailure(Exception exception) => exception is InvalidDataException || exception is XmlException || exception is FormatException;

        private static XElement Map(string name, System.Collections.Generic.IDictionary<string, string> values) =>
            new XElement(name, values.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => new XElement("p", new XAttribute("name", x.Key), new XAttribute("value", x.Value ?? string.Empty))));

        private static void ReadStringMap(XElement? container, string itemName, System.Collections.Generic.IDictionary<string, string> target)
        {
            if (container == null) return;
            foreach (var item in container.Elements(itemName)) target[Required(item, "name")] = Value(item, "value");
        }

        private static string Required(XElement element, string attribute) => element.Attribute(attribute)?.Value is string value && !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new InvalidDataException("Missing attribute: " + attribute);
        private static string Value(XElement element, string attribute) => element.Attribute(attribute)?.Value?.Trim() ?? string.Empty;
        private static double Double(string? value) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0d;
        private static int Int(string? value, int fallback) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : fallback;
        private static DateTime Date(string? value) => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result.ToUniversalTime() : new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
