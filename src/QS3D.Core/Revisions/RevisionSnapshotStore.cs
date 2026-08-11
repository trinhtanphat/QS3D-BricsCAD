using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Revisions
{
    public sealed class RevisionSnapshotStore
    {
        private const long MaxRevisionFileBytes = 64L * 1024L * 1024L;

        public void Save(RevisionSnapshot snapshot, string path)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Revision path is required.", nameof(path));
            ValidateSnapshot(snapshot);
            var full = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var temp = AtomicFileCommit.CreateTempPath(full);
            var backup = full + ".bak";
            try
            {
                Serialize(snapshot).Save(temp, SaveOptions.DisableFormatting);
                ValidateSerializedFile(temp);
                AtomicFileCommit.ReplaceWithBackup(temp, full, backup);
            }
            finally
            {
                AtomicFileCommit.TryDelete(temp);
            }
        }

        public RevisionSnapshot LoadWithBackupFallback(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Revision path is required.", nameof(path));
            var full = Path.GetFullPath(path);
            try
            {
                return Load(full);
            }
            catch (Exception primary) when (IsRecoverableDataFailure(primary))
            {
                var backup = full + ".bak";
                if (!File.Exists(backup)) throw;
                try
                {
                    return Load(backup);
                }
                catch (Exception secondary) when (IsRecoverableDataFailure(secondary))
                {
                    throw new InvalidDataException("Both the QS3D revision baseline and its backup are invalid.", new AggregateException(primary, secondary));
                }
            }
        }

        public RevisionSnapshot Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Revision path is required.", nameof(path));
            var root = LoadDocument(path).Root ?? throw new InvalidDataException("Revision file has no root.");
            RevisionSnapshotXmlSchemaValidator.Validate(root);
            if (!string.Equals(root.Name.LocalName, "qs3dRevision", StringComparison.Ordinal)) throw new InvalidDataException("Invalid QS3D revision root.");
            var snapshot = new RevisionSnapshot
            {
                Id = CanonicalRequired(root, "id", "revision id"),
                CreatedUtc = Date(root.Attribute("createdUtc")?.Value)
            };
            foreach (var node in root.Element("elements")?.Elements("element") ?? Enumerable.Empty<XElement>())
            {
                var item = new RevisionElementSnapshot
                {
                    ElementId = CanonicalRequired(node, "id", "revision element id"),
                    Category = Category(node.Attribute("category")?.Value),
                    FamilyId = CanonicalOptionalValue(node, "familyId", "revision element family id"),
                    FloorId = CanonicalOptionalValue(node, "floorId", "revision element floor id"),
                    ZoneId = CanonicalOptionalValue(node, "zoneId", "revision element zone id")
                };
                foreach (var property in node.Element("properties")?.Elements("p") ?? Enumerable.Empty<XElement>())
                {
                    var name = CanonicalRequired(property, "name", "revision property name");
                    if (item.Properties.ContainsKey(name)) throw new InvalidDataException("Duplicate revision property: " + name);
                    item.Properties[name] = property.Attribute("value")?.Value ?? string.Empty;
                }
                foreach (var quantity in node.Element("quantities")?.Elements("q") ?? Enumerable.Empty<XElement>())
                {
                    var name = CanonicalRequired(quantity, "name", "revision quantity name");
                    if (item.Quantities.ContainsKey(name)) throw new InvalidDataException("Duplicate revision quantity: " + name);
                    item.Quantities[name] = Number(quantity.Attribute("value")?.Value);
                }
                foreach (var handle in node.Element("sourceHandles")?.Elements("h") ?? Enumerable.Empty<XElement>())
                {
                    var value = CanonicalRequired(handle, "value", "revision source handle");
                    if (item.SourceHandles.Contains(value, StringComparer.OrdinalIgnoreCase))
                        throw new InvalidDataException("Duplicate revision source handle: " + value);
                    item.SourceHandles.Add(value);
                }
                foreach (var dependency in node.Element("dependencies")?.Elements("d") ?? Enumerable.Empty<XElement>())
                {
                    var value = CanonicalRequired(dependency, "value", "revision dependency");
                    if (item.Dependencies.Contains(value, StringComparer.OrdinalIgnoreCase))
                        throw new InvalidDataException("Duplicate revision dependency: " + value);
                    item.Dependencies.Add(value);
                }
                snapshot.Elements.Add(item);
            }
            if (snapshot.Elements.GroupBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1)) throw new InvalidDataException("Revision contains duplicate element ids.");
            return snapshot;
        }

        private static XDocument LoadDocument(string path)
        {
            var full = Path.GetFullPath(path);
            var info = new FileInfo(full);
            if (info.Length > MaxRevisionFileBytes) throw new InvalidDataException("QS3D revision exceeds the maximum supported file size of 64 MiB.");
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxRevisionFileBytes
            };
            using (var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = XmlReader.Create(stream, settings))
            {
                return XDocument.Load(reader, LoadOptions.None);
            }
        }

        private void ValidateSerializedFile(string path)
        {
            Load(path);
        }

        private static XDocument Serialize(RevisionSnapshot snapshot) => new XDocument(
            new XElement("qs3dRevision",
                new XAttribute("id", snapshot.Id ?? string.Empty),
                new XAttribute("createdUtc", snapshot.CreatedUtc.ToString("O", CultureInfo.InvariantCulture)),
                new XElement("elements", snapshot.Elements.OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).Select(x =>
                    new XElement("element",
                        new XAttribute("id", x.ElementId), new XAttribute("category", x.Category ?? string.Empty), new XAttribute("familyId", x.FamilyId ?? string.Empty),
                        new XAttribute("floorId", x.FloorId ?? string.Empty), new XAttribute("zoneId", x.ZoneId ?? string.Empty),
                        new XElement("properties", x.Properties.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Select(p => new XElement("p", new XAttribute("name", p.Key), new XAttribute("value", p.Value ?? string.Empty)))),
                        new XElement("quantities", x.Quantities.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase).Select(q => new XElement("q", new XAttribute("name", q.Key), new XAttribute("value", Finite(q.Value).ToString("R", CultureInfo.InvariantCulture))))),
                        new XElement("sourceHandles", x.SourceHandles.OrderBy(h => h, StringComparer.OrdinalIgnoreCase).Select(h => new XElement("h", new XAttribute("value", h)))),
                        new XElement("dependencies", x.Dependencies.OrderBy(d => d, StringComparer.OrdinalIgnoreCase).Select(d => new XElement("d", new XAttribute("value", d)))))))));

        private static void ValidateSnapshot(RevisionSnapshot snapshot)
        {
            ValidateCanonicalRequired(snapshot.Id, "revision id");
            ValidateUtcTimestamp(snapshot.CreatedUtc, "revision CreatedUtc");

            var elementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in snapshot.Elements)
            {
                if (element == null) throw new InvalidDataException("Revision snapshot contains a null element.");
                ValidateCanonicalRequired(element.ElementId, "revision element id");
                if (!elementIds.Add(element.ElementId)) throw new InvalidDataException("Revision contains duplicate element id: " + element.ElementId);
                ValidateCanonicalCategory(element.Category);
                ValidateOptionalCanonicalValue(element.FamilyId, "revision element family id");
                ValidateOptionalCanonicalValue(element.FloorId, "revision element floor id");
                ValidateOptionalCanonicalValue(element.ZoneId, "revision element zone id");
                ValidateStringMap(element.Properties, "revision element " + element.ElementId + " properties");
                ValidateNumberMap(element.Quantities, "revision element " + element.ElementId + " quantities");
                ValidateCanonicalStringList(element.SourceHandles, "revision element " + element.ElementId + " source handles");
                ValidateCanonicalStringList(element.Dependencies, "revision element " + element.ElementId + " dependencies");
            }
        }

        private static void ValidateStringMap(IDictionary<string, string> values, string label)
        {
            foreach (var key in values.Keys) ValidateCanonicalRequired(key, label + " key");
        }

        private static void ValidateNumberMap(IDictionary<string, double> values, string label)
        {
            foreach (var item in values)
            {
                ValidateCanonicalRequired(item.Key, label + " key");
                Finite(item.Value);
            }
        }

        private static void ValidateCanonicalStringList(IEnumerable<string> values, string label)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var value in values)
            {
                ValidateCanonicalRequired(value, label + " value at index " + index.ToString(CultureInfo.InvariantCulture));
                if (!seen.Add(value)) throw new InvalidDataException("Duplicate " + label + " value: " + value);
                index++;
            }
        }

        private static void ValidateCanonicalCategory(string value)
        {
            var category = ParseCategory(value);
            if (!string.Equals(value, category.ToString(), StringComparison.Ordinal))
                throw new InvalidDataException("Revision element category must use its canonical enum name: " + value + ".");
        }

        private static ElementCategory ParseCategory(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !Enum.TryParse(value, true, out ElementCategory category) ||
                !Enum.IsDefined(typeof(ElementCategory), category))
                throw new InvalidDataException("Invalid revision element category: " + (value ?? string.Empty) + ".");
            return category;
        }

        private static string Category(string? value)
        {
            var category = ParseCategory(value);
            var canonical = category.ToString();
            if (!string.Equals(value, canonical, StringComparison.Ordinal))
                throw new InvalidDataException("Revision element category must use its canonical enum name: " + (value ?? string.Empty) + ".");
            return canonical;
        }

        private static void ValidateCanonicalRequired(string? value, string label)
        {
            if (value == null) throw new InvalidDataException("Revision " + label + " is required.");
            var trimmed = value.Trim();
            if (trimmed.Length == 0) throw new InvalidDataException("Revision " + label + " is required.");
            if (!string.Equals(value, trimmed, StringComparison.Ordinal))
                throw new InvalidDataException("Revision " + label + " must not contain leading/trailing whitespace.");
        }

        private static void ValidateOptionalCanonicalValue(string? value, string label)
        {
            if (value == null || value.Length == 0) return;
            ValidateCanonicalRequired(value, label);
        }

        private static void ValidateUtcTimestamp(DateTime value, string label)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new InvalidDataException("Revision " + label + " must have DateTimeKind.Utc for deterministic persistence.");
        }

        private static bool IsRecoverableDataFailure(Exception exception) => exception is InvalidDataException || exception is XmlException || exception is FormatException || exception is FileNotFoundException;
        private static string CanonicalRequired(XElement element, string name, string label)
        {
            var value = element.Attribute(name)?.Value;
            ValidateCanonicalRequired(value, label);
            return value ?? string.Empty;
        }
        private static string CanonicalOptionalValue(XElement element, string name, string label)
        {
            var value = element.Attribute(name)?.Value;
            ValidateOptionalCanonicalValue(value, label);
            return value ?? string.Empty;
        }
        private static double Number(string? value) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && !double.IsNaN(result) && !double.IsInfinity(result) ? result : throw new InvalidDataException("Invalid revision quantity.");
        private static double Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value) ? value : throw new InvalidDataException("Revision quantity must be finite.");
        private static DateTime Date(string? value)
        {
            if (value == null) throw new InvalidDataException("Invalid revision timestamp.");
            var raw = value.Trim();
            if (raw.Length == 0) throw new InvalidDataException("Invalid revision timestamp.");
            if (!HasExplicitUtcOffset(raw) || !DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                throw new InvalidDataException("Invalid revision timestamp.");
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
    }
}
