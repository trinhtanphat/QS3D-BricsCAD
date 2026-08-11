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
    public sealed class ProjectInterchangeSourceHandleProvenancePlan
    {
        internal ProjectInterchangeSourceHandleProvenancePlan(
            string sourceProjectId,
            string sourceDrawingFingerprint,
            int elementsWithHandles,
            int sourceHandleCount,
            int validationWarnings)
        {
            SourceProjectId = sourceProjectId ?? string.Empty;
            SourceDrawingFingerprint = sourceDrawingFingerprint ?? string.Empty;
            ElementsWithHandles = elementsWithHandles;
            SourceHandleCount = sourceHandleCount;
            ValidationWarnings = validationWarnings;
        }

        public string SourceProjectId { get; }
        public string SourceDrawingFingerprint { get; }
        public int ElementsWithHandles { get; }
        public int SourceHandleCount { get; }
        public int ValidationWarnings { get; }
    }

    public sealed class ProjectInterchangeSourceHandleProvenanceResult
    {
        internal ProjectInterchangeSourceHandleProvenanceResult(ProjectInterchangeSourceHandleProvenancePlan plan)
        {
            SourceProjectId = plan.SourceProjectId;
            ElementsStored = plan.ElementsWithHandles;
            SourceHandlesStored = plan.SourceHandleCount;
        }

        public string SourceProjectId { get; }
        public int ElementsStored { get; }
        public int SourceHandlesStored { get; }
    }

    /// <summary>
    /// Stores imported drawing-local source handles as project provenance only.
    /// These records are deliberately outside ProjectElement.SourceHandles and Generated* owner slots,
    /// so they can never claim native objects in the target DWG.
    /// </summary>
    public static class ProjectInterchangeSourceHandleProvenance
    {
        public const string MetadataPrefix = "Interchange.Provenance.Source.";
        public const string LastSourceProjectIdKey = "Interchange.Provenance.LastSourceProjectId";
        public const string LastElementsStoredKey = "Interchange.Provenance.LastElementsStored";
        public const string LastSourceHandlesStoredKey = "Interchange.Provenance.LastSourceHandlesStored";
        public const string LastStoredUtcKey = "Interchange.Provenance.LastStoredUtc";
        public const string PolicyName = "PreserveAsProvenanceOnly";

        private const string ProjectRecordSuffix = ".Project";
        private const string ElementRecordSegment = ".Element.";
        private const string RecordVersion = "v1";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static ProjectInterchangeSourceHandleProvenancePlan Plan(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var elementsWithHandles = source.Elements.Count(x => x.SourceHandles.Count > 0);
            var sourceHandleCount = source.Elements.Sum(x => x.SourceHandles.Count);
            return new ProjectInterchangeSourceHandleProvenancePlan(
                source.Project.Id,
                source.Project.DrawingFingerprint,
                elementsWithHandles,
                sourceHandleCount,
                source.Validation.WarningCount);
        }

        public static ProjectInterchangeSourceHandleProvenanceResult Store(ProjectState target, string json)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var plan = PlanFromValidated(source);
            var rollback = ProjectStateSnapshot.Capture(target);

            try
            {
                var sourceToken = Token(source.Project.Id);
                var sourcePrefix = MetadataPrefix + sourceToken;
                RemoveExistingSourceRecords(target.Metadata, sourcePrefix);

                target.Metadata[sourcePrefix + ProjectRecordSuffix] = EncodeRecord(new[]
                {
                    source.Project.Id,
                    source.Project.DrawingFingerprint,
                    source.Project.UpdatedUtcRaw,
                    InterchangeSourceHandlePolicy.PreserveAsProvenanceOnly.ToString()
                });

                foreach (var element in source.Elements
                    .Where(x => x.SourceHandles.Count > 0)
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    var handles = element.SourceHandles
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    var fields = new List<string>(4 + handles.Length)
                    {
                        element.Id,
                        element.DrawingFingerprint,
                        element.SourceRefScope,
                        handles.Length.ToString(CultureInfo.InvariantCulture)
                    };
                    fields.AddRange(handles);
                    target.Metadata[sourcePrefix + ElementRecordSegment + Token(element.Id)] = EncodeRecord(fields);
                }

                target.Metadata[LastSourceProjectIdKey] = source.Project.Id;
                target.Metadata[LastElementsStoredKey] = plan.ElementsWithHandles.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastSourceHandlesStoredKey] = plan.SourceHandleCount.ToString(CultureInfo.InvariantCulture);
                target.Metadata[LastStoredUtcKey] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

                AuditTrail.ForProject(target).Record(
                    "ImportInterchangeSourceHandleProvenance",
                    string.Empty,
                    "Stored drawing-local source-handle provenance from project " + source.Project.Id +
                    ": elements=" + plan.ElementsWithHandles.ToString(CultureInfo.InvariantCulture) +
                    ", handles=" + plan.SourceHandleCount.ToString(CultureInfo.InvariantCulture) +
                    ". No imported handle was assigned to target DWG ownership.");
                target.Touch();
                return new ProjectInterchangeSourceHandleProvenanceResult(plan);
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
                        "Interchange source-handle provenance storage failed and project rollback also failed.",
                        new AggregateException(operationError, rollbackError));
                }
                throw;
            }
        }

        public static IReadOnlyList<string> ReadSourceHandles(ProjectState target, string sourceProjectId, string sourceElementId)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrWhiteSpace(sourceProjectId)) throw new ArgumentException("Source project id is required.", nameof(sourceProjectId));
            if (string.IsNullOrWhiteSpace(sourceElementId)) throw new ArgumentException("Source element id is required.", nameof(sourceElementId));

            var key = MetadataPrefix + Token(sourceProjectId.Trim()) + ElementRecordSegment + Token(sourceElementId.Trim());
            if (!target.Metadata.TryGetValue(key, out var encoded) || string.IsNullOrWhiteSpace(encoded))
                return Array.Empty<string>();

            var fields = DecodeRecord(encoded);
            if (fields.Count < 4 || !string.Equals(fields[0], sourceElementId.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Interchange provenance record does not match the requested source element identity.");
            if (!int.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out var handleCount) || handleCount < 0)
                throw new InvalidOperationException("Interchange provenance record contains an invalid source-handle count.");
            if (fields.Count != 4 + handleCount)
                throw new InvalidOperationException("Interchange provenance record source-handle count does not match its encoded payload.");

            return fields.Skip(4).ToList().AsReadOnly();
        }

        private static ProjectInterchangeSourceHandleProvenancePlan PlanFromValidated(ProjectInterchangeValidatedSnapshot source)
        {
            return new ProjectInterchangeSourceHandleProvenancePlan(
                source.Project.Id,
                source.Project.DrawingFingerprint,
                source.Elements.Count(x => x.SourceHandles.Count > 0),
                source.Elements.Sum(x => x.SourceHandles.Count),
                source.Validation.WarningCount);
        }

        private static void RemoveExistingSourceRecords(IDictionary<string, string> metadata, string sourcePrefix)
        {
            var keys = metadata.Keys
                .Where(x => x.StartsWith(sourcePrefix + ".", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (var key in keys) metadata.Remove(key);
        }

        private static string Token(string value)
        {
            // Project/element semantic IDs are resolved OrdinalIgnoreCase everywhere else in the
            // model. Canonicalizing before encoding keeps provenance lookup stable when caller casing
            // differs from the snapshot while leaving the original identity preserved inside records.
            var canonicalIdentity = (value ?? string.Empty).Trim().ToUpperInvariant();
            var bytes = Encoding.UTF8.GetBytes(canonicalIdentity);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string EncodeRecord(IEnumerable<string> fields)
        {
            var encoded = (fields ?? Enumerable.Empty<string>())
                .Select(x => Convert.ToBase64String(Encoding.UTF8.GetBytes(x ?? string.Empty)))
                .ToArray();
            return RecordVersion + "." + string.Join(".", encoded);
        }

        private static IReadOnlyList<string> DecodeRecord(string value)
        {
            var parts = (value ?? string.Empty).Split(new[] { '.' }, StringSplitOptions.None);
            if (parts.Length == 0 || !string.Equals(parts[0], RecordVersion, StringComparison.Ordinal))
                throw new InvalidOperationException("Unsupported interchange provenance record version.");

            var fields = new List<string>();
            for (var i = 1; i < parts.Length; i++)
            {
                try
                {
                    fields.Add(StrictUtf8.GetString(Convert.FromBase64String(parts[i])));
                }
                catch (Exception ex) when (ex is FormatException || ex is DecoderFallbackException)
                {
                    throw new InvalidOperationException("Interchange provenance record contains invalid base64 or UTF-8 data.", ex);
                }
            }
            return fields.AsReadOnly();
        }
    }
}
