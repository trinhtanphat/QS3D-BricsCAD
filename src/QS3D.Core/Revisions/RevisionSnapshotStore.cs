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
            Save(snapshot, path, MaxRevisionFileBytes);
        }

        private void Save(RevisionSnapshot snapshot, string path, long maximumBytes)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Revision path is required.", nameof(path));
            if (maximumBytes <= 0L) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
            ValidateSnapshot(snapshot);
            var document = Serialize(snapshot);
            ValidateSerializedSize(document, maximumBytes);
            var full = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var temp = AtomicFileCommit.CreateTempPath(full);
            var backup = full + ".bak";
            try
            {
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    document.Save(stream, SaveOptions.DisableFormatting);
                }
                ValidateSerializedFile(temp);
                if (ShouldPreserveValidatedBackup(full, backup))
                {
                    AtomicFileCommit.ReplaceWithoutBackup(temp, full);
                    Load(full);
                    Load(backup);
                }
                else
                {
                    AtomicFileCommit.ReplaceWithBackup(temp, full, backup);
                }
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
            var schemaVersion = ReadSchemaVersion(root);
            var snapshot = new RevisionSnapshot
            {
                Id = CanonicalRequired(root, "id", "revision id"),
                CreatedUtc = Date(root.Attribute("createdUtc")?.Value),
                ProjectId = schemaVersion == 2
                    ? CanonicalRequired(root, "projectId", "revision project id")
                    : string.Empty
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

        private static int ReadSchemaVersion(XElement root)
        {
            var versionAttribute = root.Attribute("schemaVersion");
            var projectIdAttribute = root.Attribute("projectId");
            if (versionAttribute == null)
            {
                if (projectIdAttribute != null)
                    throw new InvalidDataException("QS3D revision project identity requires schemaVersion=2.");
                return 1;
            }

            var raw = versionAttribute.Value;
            if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal) ||
                !int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var version))
                throw new InvalidDataException("QS3D revision schemaVersion must be a canonical integer.");

            if (version == 1)
            {
                if (projectIdAttribute != null)
                    throw new InvalidDataException("QS3D revision schemaVersion=1 cannot contain projectId.");
                return 1;
            }

            if (version == 2)
            {
                if (projectIdAttribute == null)
                    throw new InvalidDataException("QS3D revision schemaVersion=2 requires projectId.");
                return 2;
            }

            throw new InvalidDataException("Unsupported QS3D revision schemaVersion: " + raw + ".");
        }

        private static XDocument LoadDocument(string path)
        {
            return LoadDocument(path, MaxRevisionFileBytes);
        }

        private static XDocument LoadDocument(string path, long maximumBytes)
        {
            if (maximumBytes <= 0L) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
            var full = Path.GetFullPath(path);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = maximumBytes
            };
            using (var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > maximumBytes)
                    throw new InvalidDataException("QS3D revision exceeds the maximum supported file size of 64 MiB.");
                using (var reader = XmlReader.Create(stream, settings))
                {
                    return XDocument.Load(reader, LoadOptions.None);
                }
            }
        }

        private static void ValidateSerializedSize(XDocument document, long maximumBytes)
        {
            using (var stream = new BoundedCountingStream(maximumBytes))
            {
                document.Save(stream, SaveOptions.DisableFormatting);
            }
        }

        private void ValidateSerializedFile(string path)
        {
            Load(path);
        }

        private bool ShouldPreserveValidatedBackup(string primaryPath, string backupPath)
        {
            if (!File.Exists(backupPath)) return false;
            try
            {
                Load(primaryPath);
                return false;
            }
            catch (Exception primaryError) when (IsRecoverableDataFailure(primaryError))
            {
                try
                {
                    Load(backupPath);
                    return true;
                }
                catch (Exception backupError) when (IsRecoverableDataFailure(backupError))
                {
                    return false;
                }
            }
        }

        private static XDocument Serialize(RevisionSnapshot snapshot)
        {
            var root = new XElement("qs3dRevision");
            if (!string.IsNullOrEmpty(snapshot.ProjectId))
            {
                root.Add(
                    new XAttribute("schemaVersion", "2"),
                    new XAttribute("projectId", snapshot.ProjectId));
            }
            root.Add(
                new XAttribute("id", snapshot.Id ?? string.Empty),
                new XAttribute("createdUtc", snapshot.CreatedUtc.ToString("O", CultureInfo.InvariantCulture)),
                new XElement("elements", snapshot.Elements.OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).Select(x =>
                    new XElement("element",
                        new XAttribute("id", x.ElementId), new XAttribute("category", x.Category ?? string.Empty), new XAttribute("familyId", x.FamilyId ?? string.Empty),
                        new XAttribute("floorId", x.FloorId ?? string.Empty), new XAttribute("zoneId", x.ZoneId ?? string.Empty),
                        new XElement("properties", x.Properties.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Select(p => new XElement("p", new XAttribute("name", p.Key), new XAttribute("value", p.Value ?? string.Empty)))),
                        new XElement("quantities", x.Quantities.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase).Select(q => new XElement("q", new XAttribute("name", q.Key), new XAttribute("value", Finite(q.Value).ToString("R", CultureInfo.InvariantCulture))))),
                        new XElement("sourceHandles", x.SourceHandles.OrderBy(h => h, StringComparer.OrdinalIgnoreCase).Select(h => new XElement("h", new XAttribute("value", h)))),
                        new XElement("dependencies", x.Dependencies.OrderBy(d => d, StringComparer.OrdinalIgnoreCase).Select(d => new XElement("d", new XAttribute("value", d))))))));
            return new XDocument(root);
        }

        private static void ValidateSnapshot(RevisionSnapshot snapshot)
        {
            ValidateCanonicalRequired(snapshot.Id, "revision id");
            ValidateUtcTimestamp(snapshot.CreatedUtc, "revision CreatedUtc");
            ValidateOptionalCanonicalValue(snapshot.ProjectId, "revision project id");

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
            foreach (var item in values)
            {
                ValidateCanonicalRequired(item.Key, label + " key");
                ValidateXmlText(item.Value, label + " value for " + item.Key);
            }
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
            ValidateXmlText(value, label);
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

        private static void ValidateXmlText(string? value, string label)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value ?? string.Empty);
            }
            catch (XmlException exception)
            {
                throw new InvalidDataException("Revision " + label + " contains characters that are invalid in XML.", exception);
            }
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
        private static double Number(string? value)
        {
            if (value == null || !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || double.IsNaN(result) || double.IsInfinity(result))
                throw new InvalidDataException("Invalid revision quantity.");
            var canonical = result.ToString("R", CultureInfo.InvariantCulture);
            if (!string.Equals(value, canonical, StringComparison.Ordinal))
                throw new InvalidDataException("Non-canonical revision quantity: " + value + ".");
            return result;
        }
        private static double Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value) ? value : throw new InvalidDataException("Revision quantity must be finite.");
        private static DateTime Date(string? value)
        {
            if (value == null || value.Length == 0 || !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
                !DateTime.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ||
                result.Kind != DateTimeKind.Utc ||
                !string.Equals(value, result.ToString("O", CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new InvalidDataException("Invalid or non-canonical revision timestamp.");
            return result;
        }

        private sealed class BoundedCountingStream : Stream
        {
            private readonly long _maximumBytes;
            private long _length;

            public BoundedCountingStream(long maximumBytes)
            {
                if (maximumBytes <= 0L) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
                _maximumBytes = maximumBytes;
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => _length;
            public override long Position
            {
                get => _length;
                set => throw new NotSupportedException();
            }

            public override void Flush() { }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
                if (buffer.Length - offset < count) throw new ArgumentException("Invalid buffer range.");
                if (_length > _maximumBytes - count)
                    throw new InvalidDataException("QS3D revision exceeds the maximum supported file size of 64 MiB.");
                _length += count;
            }
        }
    }
}