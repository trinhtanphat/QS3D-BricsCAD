using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Export
{
    public sealed class ProjectInterchangeSourceHandleProvenanceRecord
    {
        internal ProjectInterchangeSourceHandleProvenanceRecord(
            string sourceProjectId,
            string sourceDrawingFingerprint,
            string sourceElementId,
            string targetElementId,
            IReadOnlyList<string> sourceHandles)
        {
            SourceProjectId = sourceProjectId ?? string.Empty;
            SourceDrawingFingerprint = sourceDrawingFingerprint ?? string.Empty;
            SourceElementId = sourceElementId ?? string.Empty;
            TargetElementId = targetElementId ?? string.Empty;
            SourceHandles = sourceHandles ?? throw new ArgumentNullException(nameof(sourceHandles));
        }

        public string SourceProjectId { get; }
        public string SourceDrawingFingerprint { get; }
        public string SourceElementId { get; }
        public string TargetElementId { get; }
        public IReadOnlyList<string> SourceHandles { get; }
    }

    public sealed class ProjectInterchangeSourceHandleProvenanceLedger
    {
        internal ProjectInterchangeSourceHandleProvenanceLedger(IReadOnlyList<ProjectInterchangeSourceHandleProvenanceRecord> records)
        {
            Records = records ?? throw new ArgumentNullException(nameof(records));
        }

        public IReadOnlyList<ProjectInterchangeSourceHandleProvenanceRecord> Records { get; }
        public int HandleCount => Records.Sum(x => x.SourceHandles.Count);
    }

    public static class ProjectInterchangeSourceHandleProvenanceStore
    {
        public const string MetadataKey = "Interchange.SourceHandleProvenance.v1";
        private const int FormatVersion = 1;
        private const int MaxPayloadChars = 1024 * 1024;
        private const int MaxRecords = 50000;
        private const int MaxTotalHandles = 100000;
        private const int MaxHandlesPerRecord = 256;
        private const int MaxIdentityLength = 160;
        private const int MaxFingerprintLength = 256;
        private const int MaxHandleLength = 128;

        public static IReadOnlyList<ProjectInterchangeSourceHandleProvenanceRecord> BuildRecords(
            ProjectInterchangeValidatedSnapshot source,
            IReadOnlyDictionary<string, string> sourceToTargetElementIds)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sourceToTargetElementIds == null) throw new ArgumentNullException(nameof(sourceToTargetElementIds));

            var records = new List<ProjectInterchangeSourceHandleProvenanceRecord>();
            foreach (var element in source.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                if (element.SourceHandles.Count == 0) continue;
                if (string.IsNullOrWhiteSpace(source.Project.DrawingFingerprint))
                    throw new InvalidOperationException("Source-handle provenance requires a non-empty source drawing fingerprint whenever source handles are present.");
                if (!sourceToTargetElementIds.TryGetValue(element.Id, out var targetElementId) || string.IsNullOrWhiteSpace(targetElementId))
                    throw new InvalidOperationException("Source-handle provenance has no explicit target element mapping for source element " + element.Id + ".");

                records.Add(CreateRecord(
                    source.Project.Id,
                    source.Project.DrawingFingerprint,
                    element.Id,
                    targetElementId,
                    element.SourceHandles));
                if (records.Count > MaxRecords)
                    throw new InvalidOperationException("Source-handle provenance exceeds the supported " + MaxRecords + " record limit.");
            }

            var totalHandles = records.Sum(x => x.SourceHandles.Count);
            if (totalHandles > MaxTotalHandles)
                throw new InvalidOperationException("Source-handle provenance exceeds the supported " + MaxTotalHandles + " handle limit.");
            return records.AsReadOnly();
        }

        public static ProjectInterchangeSourceHandleProvenanceLedger Load(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!project.Metadata.TryGetValue(MetadataKey, out var payload) || string.IsNullOrWhiteSpace(payload))
                return new ProjectInterchangeSourceHandleProvenanceLedger(Array.Empty<ProjectInterchangeSourceHandleProvenanceRecord>());
            if (payload.Length > MaxPayloadChars)
                throw new InvalidDataException("Source-handle provenance payload exceeds the 1 MiB metadata limit.");

            var root = ParseRoot(payload);
            if (!string.Equals(root.Name.LocalName, "sourceHandleProvenance", StringComparison.Ordinal))
                throw new InvalidDataException("Source-handle provenance root is invalid.");
            if (Integer(root.Attribute("version")?.Value, "source-handle provenance version") != FormatVersion)
                throw new InvalidDataException("Unsupported source-handle provenance version.");

            var records = new List<ProjectInterchangeSourceHandleProvenanceRecord>();
            var totalHandles = 0;
            foreach (var node in root.Elements("record"))
            {
                if (records.Count >= MaxRecords)
                    throw new InvalidDataException("Source-handle provenance exceeds the supported record limit.");
                var handles = node.Elements("handle")
                    .Select(x => Required(x.Attribute("value")?.Value, "source handle", MaxHandleLength))
                    .ToArray();
                if (handles.Length == 0)
                    throw new InvalidDataException("Source-handle provenance record must contain at least one source handle.");
                if (handles.Length > MaxHandlesPerRecord)
                    throw new InvalidDataException("Source-handle provenance record exceeds the supported per-element handle limit.");
                if (handles.Distinct(StringComparer.OrdinalIgnoreCase).Count() != handles.Length)
                    throw new InvalidDataException("Source-handle provenance record contains duplicate source handles.");

                totalHandles = checked(totalHandles + handles.Length);
                if (totalHandles > MaxTotalHandles)
                    throw new InvalidDataException("Source-handle provenance exceeds the supported total handle limit.");

                records.Add(CreateRecord(
                    Required(node.Attribute("sourceProjectId")?.Value, "source project id", MaxIdentityLength),
                    Required(node.Attribute("sourceDrawingFingerprint")?.Value, "source drawing fingerprint", MaxFingerprintLength),
                    Required(node.Attribute("sourceElementId")?.Value, "source element id", MaxIdentityLength),
                    Required(node.Attribute("targetElementId")?.Value, "target element id", MaxIdentityLength),
                    handles));
            }

            EnsureUniqueRecordKeys(records);
            return new ProjectInterchangeSourceHandleProvenanceLedger(records.AsReadOnly());
        }

        public static void Append(ProjectState project, IEnumerable<ProjectInterchangeSourceHandleProvenanceRecord> newRecords)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (newRecords == null) throw new ArgumentNullException(nameof(newRecords));

            var existing = Load(project).Records.ToList();
            var additions = newRecords.ToList();
            if (additions.Any(x => x == null))
                throw new ArgumentException("Source-handle provenance additions cannot contain null records.", nameof(newRecords));
            if (additions.Count == 0) return;

            foreach (var record in additions)
            {
                var normalized = CreateRecord(
                    record.SourceProjectId,
                    record.SourceDrawingFingerprint,
                    record.SourceElementId,
                    record.TargetElementId,
                    record.SourceHandles);
                if (project.FindElement(normalized.TargetElementId) == null)
                    throw new InvalidOperationException("Source-handle provenance target element does not exist: " + normalized.TargetElementId + ".");
                existing.Add(normalized);
            }

            if (existing.Count > MaxRecords)
                throw new InvalidOperationException("Source-handle provenance exceeds the supported " + MaxRecords + " record limit.");
            if (existing.Sum(x => x.SourceHandles.Count) > MaxTotalHandles)
                throw new InvalidOperationException("Source-handle provenance exceeds the supported " + MaxTotalHandles + " handle limit.");
            EnsureUniqueRecordKeys(existing);

            var payload = Serialize(existing);
            if (payload.Length > MaxPayloadChars)
                throw new InvalidOperationException("Source-handle provenance payload exceeds the 1 MiB metadata limit.");
            if (project.Metadata.TryGetValue(MetadataKey, out var current) && string.Equals(current, payload, StringComparison.Ordinal)) return;

            project.Touch();
            project.Metadata[MetadataKey] = payload;
        }

        private static ProjectInterchangeSourceHandleProvenanceRecord CreateRecord(
            string sourceProjectId,
            string sourceDrawingFingerprint,
            string sourceElementId,
            string targetElementId,
            IEnumerable<string> handles)
        {
            var projectId = Required(sourceProjectId, "source project id", MaxIdentityLength);
            var fingerprint = Required(sourceDrawingFingerprint, "source drawing fingerprint", MaxFingerprintLength);
            var sourceId = Required(sourceElementId, "source element id", MaxIdentityLength);
            var targetId = Required(targetElementId, "target element id", MaxIdentityLength);
            var normalizedHandles = (handles ?? throw new ArgumentNullException(nameof(handles)))
                .Select(x => Required(x, "source handle", MaxHandleLength))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToArray();
            if (normalizedHandles.Length == 0)
                throw new InvalidOperationException("Source-handle provenance record requires at least one source handle.");
            if (normalizedHandles.Length > MaxHandlesPerRecord)
                throw new InvalidOperationException("Source-handle provenance record exceeds the supported " + MaxHandlesPerRecord + " handle limit.");
            return new ProjectInterchangeSourceHandleProvenanceRecord(projectId, fingerprint, sourceId, targetId, normalizedHandles);
        }

        private static void EnsureUniqueRecordKeys(IEnumerable<ProjectInterchangeSourceHandleProvenanceRecord> records)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in records)
            {
                var key = record.SourceProjectId + "\u001f" + record.SourceDrawingFingerprint + "\u001f" + record.SourceElementId + "\u001f" + record.TargetElementId;
                if (!keys.Add(key))
                    throw new InvalidDataException("Source-handle provenance contains duplicate source/target mapping: " + record.SourceElementId + " -> " + record.TargetElementId + ".");
            }
        }

        private static string Serialize(IEnumerable<ProjectInterchangeSourceHandleProvenanceRecord> records)
        {
            var root = new XElement(
                "sourceHandleProvenance",
                new XAttribute("version", FormatVersion),
                records
                    .OrderBy(x => x.SourceProjectId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.SourceDrawingFingerprint, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.SourceElementId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.TargetElementId, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new XElement(
                        "record",
                        new XAttribute("sourceProjectId", x.SourceProjectId),
                        new XAttribute("sourceDrawingFingerprint", x.SourceDrawingFingerprint),
                        new XAttribute("sourceElementId", x.SourceElementId),
                        new XAttribute("targetElementId", x.TargetElementId),
                        x.SourceHandles.Select(handle => new XElement("handle", new XAttribute("value", handle))))));
            return root.ToString(SaveOptions.DisableFormatting);
        }

        private static XElement ParseRoot(string payload)
        {
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = MaxPayloadChars
                };
                using (var text = new StringReader(payload))
                using (var reader = XmlReader.Create(text, settings))
                {
                    return XDocument.Load(reader, LoadOptions.None).Root ?? throw new InvalidDataException("Source-handle provenance payload has no root element.");
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex) when (ex is XmlException || ex is ArgumentException)
            {
                throw new InvalidDataException("Source-handle provenance payload is malformed.", ex);
            }
        }

        private static int Integer(string? value, string label)
        {
            if (!int.TryParse(value, out var parsed)) throw new InvalidDataException("Invalid " + label + ".");
            return parsed;
        }

        private static string Required(string? value, string label, int maxLength)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new InvalidDataException("Source-handle provenance " + label + " is required.");
            if (normalized.Length > maxLength) throw new InvalidDataException("Source-handle provenance " + label + " exceeds the supported length.");
            if (normalized.Any(char.IsControl)) throw new InvalidDataException("Source-handle provenance " + label + " contains control characters.");
            return normalized;
        }
    }
}
