using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public sealed class ProjectInterchangeProvenanceTargetMapResult
    {
        internal ProjectInterchangeProvenanceTargetMapResult(string sourceProjectId, int mappingsStored)
        {
            SourceProjectId = sourceProjectId ?? string.Empty;
            MappingsStored = mappingsStored;
        }

        public string SourceProjectId { get; }
        public int MappingsStored { get; }
    }

    /// <summary>
    /// Persists source semantic Element id -> imported target semantic Element id lineage.
    /// This map contains no raw CAD handles and grants no target-DWG ownership.
    /// </summary>
    public static class ProjectInterchangeProvenanceTargetMap
    {
        public const string MetadataPrefix = "Interchange.Provenance.TargetMap.";
        private const string ProjectRecordSuffix = ".Project";
        private const string ElementRecordSegment = ".Element.";
        private const string RecordVersion = "v1";
        private const int MaxMappings = 50000;
        private const int MaxEncodedChars = 1024 * 1024;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static ProjectInterchangeProvenanceTargetMapResult Store(
            ProjectState target,
            string sourceProjectId,
            string sourceDrawingFingerprint,
            IReadOnlyDictionary<string, string> sourceToTargetElementIds)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (sourceToTargetElementIds == null) throw new ArgumentNullException(nameof(sourceToTargetElementIds));
            var sourceId = Required(sourceProjectId, nameof(sourceProjectId));
            var sourceFingerprint = (sourceDrawingFingerprint ?? string.Empty).Trim();
            var mappingCount = sourceToTargetElementIds.Count;
            if (mappingCount < 0)
                throw new InvalidOperationException("Interchange provenance target map reported a negative mapping Count.");
            if (mappingCount > MaxMappings)
                throw new InvalidOperationException("Interchange provenance target map exceeds the supported " + MaxMappings.ToString(CultureInfo.InvariantCulture) + " mapping limit.");

            var normalized = sourceToTargetElementIds
                .Select(pair => new KeyValuePair<string, string>(Required(pair.Key, "sourceElementId"), Required(pair.Value, "targetElementId")))
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .ToList();
            if (normalized.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Count)
                throw new InvalidOperationException("Interchange provenance target map contains duplicate source Element ids.");
            if (normalized.Select(x => x.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Count)
                throw new InvalidOperationException("Interchange provenance target map must be one-to-one; duplicate target Element ids are not allowed.");
            foreach (var pair in normalized)
                if (target.FindElement(pair.Value) == null)
                    throw new InvalidOperationException("Interchange provenance target map references missing target Element " + pair.Value + ".");

            var sourcePrefix = MetadataPrefix + Token(sourceId);
            var records = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [sourcePrefix + ProjectRecordSuffix] = EncodeRecord(new[]
                {
                    sourceId,
                    sourceFingerprint,
                    normalized.Count.ToString(CultureInfo.InvariantCulture)
                })
            };
            foreach (var pair in normalized)
                records[sourcePrefix + ElementRecordSegment + Token(pair.Key)] = EncodeRecord(new[]
                {
                    sourceId,
                    sourceFingerprint,
                    pair.Key,
                    pair.Value
                });

            var encodedChars = records.Sum(x => checked(x.Key.Length + x.Value.Length));
            if (encodedChars > MaxEncodedChars)
                throw new InvalidOperationException("Interchange provenance target map exceeds the 1 MiB encoded metadata limit.");

            var rollback = ProjectStateSnapshot.Capture(target);
            try
            {
                foreach (var key in target.Metadata.Keys.Where(x => x.StartsWith(sourcePrefix + ".", StringComparison.OrdinalIgnoreCase)).ToArray())
                    target.Metadata.Remove(key);
                foreach (var pair in records) target.Metadata[pair.Key] = pair.Value;

                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeProvenanceTargetMap",
                    string.Empty,
                    "Stored source-to-target semantic Element lineage for project " + sourceId +
                    ": mappings=" + normalized.Count.ToString(CultureInfo.InvariantCulture) +
                    ". Mapping contains no CAD ownership handles.");
                target.Touch();
                return new ProjectInterchangeProvenanceTargetMapResult(sourceId, normalized.Count);
            }
            catch (Exception operationError)
            {
                try
                {
                    rollback.Restore(target);
                }
                catch (Exception rollbackError)
                {
                    throw new InvalidOperationException(
                        "Interchange provenance target-map storage failed and project rollback also failed.",
                        new AggregateException(operationError, rollbackError));
                }
                throw;
            }
        }

        public static string ReadTargetElementId(ProjectState target, string sourceProjectId, string sourceElementId)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var sourceId = Required(sourceProjectId, nameof(sourceProjectId));
            var elementId = Required(sourceElementId, nameof(sourceElementId));
            var key = MetadataPrefix + Token(sourceId) + ElementRecordSegment + Token(elementId);
            if (!target.Metadata.TryGetValue(key, out var encoded) || string.IsNullOrWhiteSpace(encoded)) return string.Empty;
            var fields = DecodeRecord(encoded);
            if (fields.Count != 4 ||
                !string.Equals(fields[0], sourceId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(fields[2], elementId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Interchange provenance target-map record does not match the requested source identity.");
            var targetId = Required(fields[3], "targetElementId");
            if (!string.Equals(fields[3], targetId, StringComparison.Ordinal))
                throw new InvalidOperationException("Interchange provenance target-map record contains a non-canonical padded target Element id.");
            if (target.FindElement(targetId) == null)
                throw new InvalidOperationException("Interchange provenance target-map record points to missing target Element " + targetId + ".");
            return targetId;
        }

        private static string Token(string value)
        {
            var canonical = Required(value, "identity").ToUpperInvariant();
            return Convert.ToBase64String(StrictUtf8.GetBytes(canonical))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string EncodeRecord(IEnumerable<string> fields)
        {
            var encoded = fields.Select(x => Convert.ToBase64String(StrictUtf8.GetBytes(x ?? string.Empty))).ToArray();
            return RecordVersion + "." + string.Join(".", encoded);
        }

        private static IReadOnlyList<string> DecodeRecord(string value)
        {
            var parts = (value ?? string.Empty).Split(new[] { '.' }, StringSplitOptions.None);
            if (parts.Length == 0 || !string.Equals(parts[0], RecordVersion, StringComparison.Ordinal))
                throw new InvalidOperationException("Unsupported interchange provenance target-map record version.");
            var fields = new List<string>();
            for (var i = 1; i < parts.Length; i++)
            {
                try { fields.Add(StrictUtf8.GetString(Convert.FromBase64String(parts[i]))); }
                catch (Exception ex) when (ex is FormatException || ex is DecoderFallbackException)
                {
                    throw new InvalidOperationException("Interchange provenance target-map record contains invalid base64 or UTF-8 data.", ex);
                }
            }
            return fields.AsReadOnly();
        }

        private static string Required(string value, string label)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Interchange provenance " + label + " is required.", label);
            if (normalized.Length > 256) throw new ArgumentException("Interchange provenance " + label + " exceeds the supported length.", label);
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Interchange provenance " + label + " contains control characters.", label);
            return normalized;
        }
    }
}
