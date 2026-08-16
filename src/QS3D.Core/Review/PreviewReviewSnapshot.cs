using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;
using QS3D.Core.Services;

namespace QS3D.Core.Review
{
    public enum PreviewReviewKind
    {
        QuantityRule = 0,
        Regeneration = 1
    }

    public sealed class PreviewReviewEntry
    {
        internal PreviewReviewEntry(string elementId, string category, string change, string field, string before, string after, string beforeProvenance, string afterProvenance)
        {
            ElementId = elementId ?? string.Empty;
            Category = category ?? string.Empty;
            Change = change ?? string.Empty;
            Field = field ?? string.Empty;
            Before = before ?? string.Empty;
            After = after ?? string.Empty;
            BeforeProvenance = beforeProvenance ?? string.Empty;
            AfterProvenance = afterProvenance ?? string.Empty;
        }

        public string ElementId { get; }
        public string Category { get; }
        public string Change { get; }
        public string Field { get; }
        public string Before { get; }
        public string After { get; }
        public string BeforeProvenance { get; }
        public string AfterProvenance { get; }
    }

    public sealed class PreviewReviewSnapshot
    {
        internal PreviewReviewSnapshot(
            string name,
            string projectId,
            PreviewReviewKind kind,
            long sourceChangeVersion,
            string scope,
            IEnumerable<string> targetElementIds,
            IEnumerable<PreviewReviewEntry> entries,
            int changedElementCount,
            int regeneratedElementCount,
            int newHealthIssueCount,
            int newHealthErrorCount,
            int resolvedHealthIssueCount,
            int omittedHandleFieldCount,
            string fingerprint)
        {
            Name = name ?? string.Empty;
            ProjectId = projectId ?? string.Empty;
            Kind = kind;
            SourceChangeVersion = sourceChangeVersion;
            Scope = scope ?? string.Empty;
            TargetElementIds = (targetElementIds ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
            Entries = (entries ?? Enumerable.Empty<PreviewReviewEntry>()).ToList().AsReadOnly();
            ChangedElementCount = changedElementCount;
            RegeneratedElementCount = regeneratedElementCount;
            NewHealthIssueCount = newHealthIssueCount;
            NewHealthErrorCount = newHealthErrorCount;
            ResolvedHealthIssueCount = resolvedHealthIssueCount;
            OmittedHandleFieldCount = omittedHandleFieldCount;
            Fingerprint = fingerprint ?? string.Empty;
        }

        public string Name { get; }
        public string ProjectId { get; }
        public PreviewReviewKind Kind { get; }
        public long SourceChangeVersion { get; }
        public string Scope { get; }
        public IReadOnlyList<string> TargetElementIds { get; }
        public IReadOnlyList<PreviewReviewEntry> Entries { get; }
        public int ChangedElementCount { get; }
        public int RegeneratedElementCount { get; }
        public int NewHealthIssueCount { get; }
        public int NewHealthErrorCount { get; }
        public int ResolvedHealthIssueCount { get; }
        public int OmittedHandleFieldCount { get; }
        public string Fingerprint { get; }
        public bool IsSubset => string.Equals(Scope, "Subset", StringComparison.Ordinal);
    }

    public sealed class PreviewReviewSnapshotService
    {
        private const string PropertyFieldPrefix = "Property:";
        private const string QuantityFieldPrefix = "Quantity:";

        public const string FormatName = "QS3D.PreviewReviewSnapshot";
        public const int FormatVersion = 1;

        public PreviewReviewSnapshot Create(string name, QuantityRuleProjectPreview preview)
        {
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            var safeName = CanonicalRequired(name, nameof(name));
            var projectId = CanonicalRequired(preview.ProjectId, nameof(preview.ProjectId));
            var entries = new List<PreviewReviewEntry>();

            foreach (var element in preview.Elements.OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase))
            {
                if (element == null) throw new InvalidOperationException("Quantity-rule preview contains a null element preview.");
                var elementId = CanonicalRequired(element.ElementId, "quantity preview element id");
                foreach (var change in element.Changes.OrderBy(x => x.OutputName, StringComparer.OrdinalIgnoreCase))
                {
                    if (change == null) throw new InvalidOperationException("Quantity-rule preview contains a null change.");
                    var output = CanonicalRequired(change.OutputName, "quantity preview output name");
                    entries.Add(new PreviewReviewEntry(
                        elementId,
                        element.Category.ToString(),
                        change.Kind.ToString(),
                        QuantityFieldPrefix + output,
                        NullableNumber(change.BeforeValue, elementId + "/" + output + "/before"),
                        NullableNumber(change.AfterValue, elementId + "/" + output + "/after"),
                        change.BeforeProvenance,
                        change.AfterProvenance));
                }
            }

            return Build(
                safeName,
                projectId,
                PreviewReviewKind.QuantityRule,
                preview.SourceChangeVersion,
                "Project",
                Array.Empty<string>(),
                entries,
                preview.ChangedElementCount,
                0,
                0,
                0,
                0,
                0);
        }

        public PreviewReviewSnapshot Create(string name, RegenerationPreview preview)
        {
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            var safeName = CanonicalRequired(name, nameof(name));
            var projectId = CanonicalRequired(preview.ProjectId, nameof(preview.ProjectId));
            var targets = CanonicalIds(preview.TargetElementIds, "regeneration preview target", allowEmpty: !preview.IsSubset);
            if (preview.IsSubset && targets.Count == 0)
                throw new InvalidOperationException("Subset regeneration preview has no targets.");
            if (!preview.IsSubset && targets.Count != 0)
                throw new InvalidOperationException("Whole-project regeneration preview unexpectedly contains targets.");

            var entries = new List<PreviewReviewEntry>();
            var omittedHandles = 0;
            foreach (var delta in preview.Deltas.OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase))
            {
                if (delta == null) throw new InvalidOperationException("Regeneration preview contains a null revision delta.");
                var elementId = CanonicalRequired(delta.ElementId, "regeneration preview element id");
                var change = CanonicalRequired(delta.Change, "regeneration preview change");
                var safeFieldCount = 0;
                foreach (var field in delta.Fields.OrderBy(x => x.Field, StringComparer.OrdinalIgnoreCase))
                {
                    if (field == null) throw new InvalidOperationException("Regeneration preview contains a null field delta.");
                    var fieldName = CanonicalRequired(field.Field, "regeneration preview field");
                    if (!IsPortableReviewField(fieldName))
                    {
                        omittedHandles++;
                        continue;
                    }
                    entries.Add(new PreviewReviewEntry(elementId, string.Empty, change, fieldName, field.Before, field.After, string.Empty, string.Empty));
                    safeFieldCount++;
                }
                if (safeFieldCount == 0)
                    entries.Add(new PreviewReviewEntry(elementId, string.Empty, change, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));
            }

            return Build(
                safeName,
                projectId,
                PreviewReviewKind.Regeneration,
                preview.SourceChangeVersion,
                preview.IsSubset ? "Subset" : "Project",
                targets,
                entries,
                preview.ChangedElementCount,
                preview.RegeneratedElementCount,
                preview.HealthDiff.NewIssues.Count,
                preview.HealthDiff.NewErrorCount,
                preview.HealthDiff.ResolvedIssues.Count,
                omittedHandles);
        }

        public bool Verify(PreviewReviewSnapshot snapshot)
        {
            if (snapshot == null) return false;
            try
            {
                ValidateSnapshot(snapshot);
                return string.Equals(snapshot.Fingerprint, ComputeFingerprint(snapshot), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return false;
            }
        }

        internal static bool IsHandleField(string field)
        {
            return !string.IsNullOrWhiteSpace(field) && field.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsPortableReviewField(string field)
        {
            if (string.IsNullOrWhiteSpace(field)) return true;
            if (IsHandleField(field)) return false;
            if (!field.StartsWith(PropertyFieldPrefix, StringComparison.OrdinalIgnoreCase)) return true;
            return ProjectInterchangeElementPropertyPolicy.IsPortable(field.Substring(PropertyFieldPrefix.Length));
        }

        internal static bool IsCanonicalOptionalReviewField(string field)
        {
            var raw = field ?? string.Empty;
            return IsCanonicalOptionalReviewToken(raw) &&
                   HasCanonicalStructuredPayload(raw, PropertyFieldPrefix) &&
                   HasCanonicalStructuredPayload(raw, QuantityFieldPrefix);
        }

        internal static bool IsCanonicalOptionalReviewCategory(string category)
        {
            return IsCanonicalOptionalReviewToken(category ?? string.Empty);
        }

        internal static bool IsCanonicalReviewChange(string change)
        {
            return string.Equals(change, "Added", StringComparison.Ordinal) ||
                   string.Equals(change, "Changed", StringComparison.Ordinal) ||
                   string.Equals(change, "Removed", StringComparison.Ordinal);
        }

        private static bool IsCanonicalOptionalReviewToken(string raw)
        {
            return raw.Length == 0 ||
                   (!string.IsNullOrWhiteSpace(raw) && string.Equals(raw, raw.Trim(), StringComparison.Ordinal));
        }

        private static bool HasCanonicalStructuredPayload(string field, string prefix)
        {
            if (!field.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            var payload = field.Substring(prefix.Length);
            return payload.Length > 0 &&
                   !string.IsNullOrWhiteSpace(payload) &&
                   string.Equals(payload, payload.Trim(), StringComparison.Ordinal);
        }

        internal static string ComputeFingerprint(PreviewReviewSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var sb = new StringBuilder(4096);
            Part(sb, FormatName);
            Part(sb, FormatVersion.ToString(CultureInfo.InvariantCulture));
            Part(sb, snapshot.Name);
            Part(sb, snapshot.ProjectId);
            Part(sb, snapshot.Kind.ToString());
            Part(sb, snapshot.SourceChangeVersion.ToString(CultureInfo.InvariantCulture));
            Part(sb, snapshot.Scope);
            Part(sb, snapshot.ChangedElementCount.ToString(CultureInfo.InvariantCulture));
            Part(sb, snapshot.RegeneratedElementCount.ToString(CultureInfo.InvariantCulture));
            Part(sb, snapshot.NewHealthIssueCount.ToString(CultureInfo.InvariantCulture));
            Part(sb, snapshot.NewHealthErrorCount.ToString(CultureInfo.InvariantCulture));
            Part(sb, snapshot.ResolvedHealthIssueCount.ToString(CultureInfo.InvariantCulture));
            Part(sb, snapshot.OmittedHandleFieldCount.ToString(CultureInfo.InvariantCulture));
            foreach (var target in snapshot.TargetElementIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) Part(sb, target);
            foreach (var entry in OrderedEntries(snapshot.Entries))
            {
                Part(sb, entry.ElementId);
                Part(sb, entry.Category);
                Part(sb, entry.Change);
                Part(sb, entry.Field);
                Part(sb, entry.Before);
                Part(sb, entry.After);
                Part(sb, entry.BeforeProvenance);
                Part(sb, entry.AfterProvenance);
            }
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        internal static void ValidateSnapshot(PreviewReviewSnapshot snapshot)
        {
            CanonicalRequired(snapshot.Name, nameof(snapshot.Name));
            CanonicalRequired(snapshot.ProjectId, nameof(snapshot.ProjectId));
            if (!Enum.IsDefined(typeof(PreviewReviewKind), snapshot.Kind))
                throw new InvalidOperationException("Preview review kind is not supported: " + snapshot.Kind + ".");
            if (snapshot.SourceChangeVersion < 0) throw new InvalidOperationException("Preview review source change version cannot be negative.");
            if (!string.Equals(snapshot.Scope, "Project", StringComparison.Ordinal) && !string.Equals(snapshot.Scope, "Subset", StringComparison.Ordinal))
                throw new InvalidOperationException("Preview review scope must be Project or Subset.");
            if (snapshot.ChangedElementCount < 0 || snapshot.RegeneratedElementCount < 0 || snapshot.NewHealthIssueCount < 0 || snapshot.NewHealthErrorCount < 0 || snapshot.ResolvedHealthIssueCount < 0 || snapshot.OmittedHandleFieldCount < 0)
                throw new InvalidOperationException("Preview review summary counts cannot be negative.");
            if (snapshot.NewHealthErrorCount > snapshot.NewHealthIssueCount)
                throw new InvalidOperationException("Preview review new health error count exceeds total new health issues.");

            var targets = CanonicalIds(snapshot.TargetElementIds, "preview review target", allowEmpty: true);
            if (snapshot.IsSubset && targets.Count == 0) throw new InvalidOperationException("Subset preview review requires targets.");
            if (!snapshot.IsSubset && targets.Count != 0) throw new InvalidOperationException("Project preview review cannot contain subset targets.");
            if (snapshot.Kind == PreviewReviewKind.QuantityRule && snapshot.IsSubset)
                throw new InvalidOperationException("Quantity-rule review snapshot does not support subset scope.");

            var seenRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var changedElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in snapshot.Entries)
            {
                if (entry == null) throw new InvalidOperationException("Preview review contains a null entry.");
                CanonicalRequired(entry.ElementId, "preview review entry element id");
                var change = CanonicalRequired(entry.Change, "preview review entry change");
                if (!IsCanonicalReviewChange(change)) throw new InvalidOperationException("Preview review entry change is not supported: " + change + ".");
                if (!IsCanonicalOptionalReviewCategory(entry.Category)) throw new InvalidOperationException("Preview review entry category must be exact-empty or canonical without surrounding whitespace.");
                if (!IsCanonicalOptionalReviewField(entry.Field)) throw new InvalidOperationException("Preview review entry field must be exact-empty or canonical without surrounding whitespace and use canonical structured payloads.");
                if (!IsPortableReviewField(entry.Field)) throw new InvalidOperationException("Preview review artifacts cannot contain drawing-local/native fields: " + entry.Field + ".");
                var rowKey = entry.ElementId + "\u001f" + entry.Field;
                if (!seenRows.Add(rowKey)) throw new InvalidOperationException("Preview review contains a duplicate element/field row: " + entry.ElementId + "/" + entry.Field + ".");
                changedElements.Add(entry.ElementId);
            }
            if (changedElements.Count != snapshot.ChangedElementCount)
                throw new InvalidOperationException("Preview review changed-element summary does not match its entries.");

            ValidateKindSpecificInvariants(snapshot);

            if (string.IsNullOrWhiteSpace(snapshot.Fingerprint) || snapshot.Fingerprint.Length != 64 || snapshot.Fingerprint.Any(ch => !Uri.IsHexDigit(ch)))
                throw new InvalidOperationException("Preview review fingerprint must be a 64-character SHA-256 hex value.");
        }

        private static void ValidateKindSpecificInvariants(PreviewReviewSnapshot snapshot)
        {
            if (snapshot.Kind == PreviewReviewKind.QuantityRule)
            {
                if (!string.Equals(snapshot.Scope, "Project", StringComparison.Ordinal) || snapshot.TargetElementIds.Count != 0)
                    throw new InvalidOperationException("Quantity-rule review snapshot must use whole-project scope without targets.");
                if (snapshot.RegeneratedElementCount != 0 || snapshot.NewHealthIssueCount != 0 || snapshot.NewHealthErrorCount != 0 || snapshot.ResolvedHealthIssueCount != 0 || snapshot.OmittedHandleFieldCount != 0)
                    throw new InvalidOperationException("Quantity-rule review snapshot cannot contain regeneration or health summary counts.");
                foreach (var entry in snapshot.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Category))
                        throw new InvalidOperationException("Quantity-rule review entries require a category.");
                    if (!entry.Field.StartsWith(QuantityFieldPrefix, StringComparison.Ordinal))
                        throw new InvalidOperationException("Quantity-rule review entries must use canonical Quantity: fields.");
                }
                return;
            }

            if (snapshot.Kind == PreviewReviewKind.Regeneration)
            {
                foreach (var entry in snapshot.Entries)
                {
                    if (entry.Category.Length != 0)
                        throw new InvalidOperationException("Regeneration review entries must not contain quantity-rule categories.");
                    if (entry.BeforeProvenance.Length != 0 || entry.AfterProvenance.Length != 0)
                        throw new InvalidOperationException("Regeneration review entries must not contain quantity-rule provenance.");
                }
            }
        }

        private static PreviewReviewSnapshot Build(
            string name,
            string projectId,
            PreviewReviewKind kind,
            long sourceChangeVersion,
            string scope,
            IEnumerable<string> targets,
            IEnumerable<PreviewReviewEntry> entries,
            int changedElementCount,
            int regeneratedElementCount,
            int newHealthIssueCount,
            int newHealthErrorCount,
            int resolvedHealthIssueCount,
            int omittedHandleFieldCount)
        {
            var orderedTargets = CanonicalIds(targets, "preview review target", allowEmpty: true);
            var orderedEntries = OrderedEntries(entries).ToList().AsReadOnly();
            var draft = new PreviewReviewSnapshot(name, projectId, kind, sourceChangeVersion, scope, orderedTargets, orderedEntries, changedElementCount, regeneratedElementCount, newHealthIssueCount, newHealthErrorCount, resolvedHealthIssueCount, omittedHandleFieldCount, new string('0', 64));
            var fingerprint = ComputeFingerprint(draft);
            var result = new PreviewReviewSnapshot(name, projectId, kind, sourceChangeVersion, scope, orderedTargets, orderedEntries, changedElementCount, regeneratedElementCount, newHealthIssueCount, newHealthErrorCount, resolvedHealthIssueCount, omittedHandleFieldCount, fingerprint);
            ValidateSnapshot(result);
            return result;
        }

        private static IEnumerable<PreviewReviewEntry> OrderedEntries(IEnumerable<PreviewReviewEntry> entries)
        {
            return (entries ?? Enumerable.Empty<PreviewReviewEntry>())
                .OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Field, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Change, StringComparer.Ordinal)
                .ThenBy(x => x.Before, StringComparer.Ordinal)
                .ThenBy(x => x.After, StringComparer.Ordinal);
        }

        private static IReadOnlyList<string> CanonicalIds(IEnumerable<string> values, string label, bool allowEmpty)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                var safe = CanonicalRequired(value, label);
                if (!seen.Add(safe)) throw new InvalidOperationException("Duplicate " + label + ": " + safe + ".");
                result.Add(safe);
            }
            if (!allowEmpty && result.Count == 0) throw new InvalidOperationException(label + " list cannot be empty.");
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result.AsReadOnly();
        }

        private static string CanonicalRequired(string value, string label)
        {
            var raw = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException(label + " is required.");
            if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal)) throw new InvalidOperationException(label + " must not contain surrounding whitespace: " + raw + ".");
            return raw;
        }

        private static string NullableNumber(double? value, string label)
        {
            if (!value.HasValue) return string.Empty;
            if (double.IsNaN(value.Value) || double.IsInfinity(value.Value)) throw new InvalidOperationException("Preview review quantity must be finite: " + label + ".");
            return value.Value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void Part(StringBuilder sb, string value)
        {
            var safe = value ?? string.Empty;
            try
            {
                XmlConvert.VerifyXmlChars(safe);
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException("Preview review persisted text contains characters that are invalid in XML.", ex);
            }
            sb.Append(safe.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(safe).Append(';');
        }
    }

    public sealed class PreviewReviewSnapshotStore
    {
        private const long MaxFileBytes = 16L * 1024L * 1024L;

        public void Save(PreviewReviewSnapshot snapshot, string path)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Preview review path is required.", nameof(path));
            var service = new PreviewReviewSnapshotService();
            if (!service.Verify(snapshot)) throw new InvalidOperationException("Preview review snapshot fingerprint or invariants are invalid.");

            var full = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var temp = AtomicFileCommit.CreateTempPath(full);
            try
            {
                Serialize(snapshot).Save(temp, SaveOptions.DisableFormatting);
                Load(temp);
                AtomicFileCommit.ReplaceWithBackup(temp, full, full + ".bak");
            }
            finally
            {
                AtomicFileCommit.TryDelete(temp);
            }
        }

        public PreviewReviewSnapshot Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Preview review path is required.", nameof(path));
            var document = LoadDocument(path);
            ValidateXmlShape(document);
            var root = document.Root ?? throw new InvalidDataException("Preview review file has no root.");
            if (!string.Equals(root.Name.LocalName, "qs3dPreviewReview", StringComparison.Ordinal)) throw new InvalidDataException("Invalid preview review root.");
            if (!string.Equals(Required(root, "format"), PreviewReviewSnapshotService.FormatName, StringComparison.Ordinal)) throw new InvalidDataException("Unsupported preview review format.");
            if (NonNegativeInt(root, "formatVersion") != PreviewReviewSnapshotService.FormatVersion) throw new InvalidDataException("Unsupported preview review format version.");

            var name = CanonicalRequired(root, "name");
            var projectId = CanonicalRequired(root, "projectId");
            var kindText = Required(root, "kind");
            if (!Enum.TryParse(kindText, false, out PreviewReviewKind kind) ||
                !Enum.IsDefined(typeof(PreviewReviewKind), kind) ||
                !string.Equals(kindText, kind.ToString(), StringComparison.Ordinal))
                throw new InvalidDataException("Invalid preview review kind.");
            var sourceChangeVersion = NonNegativeLong(root, "sourceChangeVersion");
            var scope = Required(root, "scope");
            var changedElementCount = NonNegativeInt(root, "changedElementCount");
            var regeneratedElementCount = NonNegativeInt(root, "regeneratedElementCount");
            var newHealthIssueCount = NonNegativeInt(root, "newHealthIssueCount");
            var newHealthErrorCount = NonNegativeInt(root, "newHealthErrorCount");
            var resolvedHealthIssueCount = NonNegativeInt(root, "resolvedHealthIssueCount");
            var omittedHandleFieldCount = NonNegativeInt(root, "omittedHandleFieldCount");
            var fingerprint = Required(root, "fingerprint");

            var targets = new List<string>();
            var seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in root.Element("targets")?.Elements("target") ?? Enumerable.Empty<XElement>())
            {
                var id = CanonicalRequired(node, "id");
                if (!seenTargets.Add(id)) throw new InvalidDataException("Duplicate preview review target: " + id + ".");
                targets.Add(id);
            }
            targets.Sort(StringComparer.OrdinalIgnoreCase);

            var entries = new List<PreviewReviewEntry>();
            var seenRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in root.Element("entries")?.Elements("entry") ?? Enumerable.Empty<XElement>())
            {
                var elementId = CanonicalRequired(node, "elementId");
                var category = Value(node, "category");
                if (!PreviewReviewSnapshotService.IsCanonicalOptionalReviewCategory(category)) throw new InvalidDataException("Preview review category is not canonical: category.");
                var change = CanonicalRequired(node, "change");
                if (!PreviewReviewSnapshotService.IsCanonicalReviewChange(change)) throw new InvalidDataException("Preview review change is not supported: " + change + ".");
                var field = Value(node, "field");
                if (!PreviewReviewSnapshotService.IsCanonicalOptionalReviewField(field)) throw new InvalidDataException("Preview review field is not canonical: field.");
                if (!PreviewReviewSnapshotService.IsPortableReviewField(field)) throw new InvalidDataException("Preview review file contains a forbidden drawing-local/native field: " + field + ".");
                var key = elementId + "\u001f" + field;
                if (!seenRows.Add(key)) throw new InvalidDataException("Duplicate preview review element/field row: " + elementId + "/" + field + ".");
                entries.Add(new PreviewReviewEntry(elementId, category, change, field, Value(node, "before"), Value(node, "after"), Value(node, "beforeProvenance"), Value(node, "afterProvenance")));
            }
            entries = entries.OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Field, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Change, StringComparer.Ordinal).ThenBy(x => x.Before, StringComparer.Ordinal).ThenBy(x => x.After, StringComparer.Ordinal).ToList();

            var snapshot = new PreviewReviewSnapshot(name, projectId, kind, sourceChangeVersion, scope, targets, entries, changedElementCount, regeneratedElementCount, newHealthIssueCount, newHealthErrorCount, resolvedHealthIssueCount, omittedHandleFieldCount, fingerprint);
            if (!new PreviewReviewSnapshotService().Verify(snapshot)) throw new InvalidDataException("Preview review snapshot fingerprint or invariants are invalid.");
            return snapshot;
        }

        private static XDocument Serialize(PreviewReviewSnapshot snapshot)
        {
            return new XDocument(
                new XElement("qs3dPreviewReview",
                    new XAttribute("format", PreviewReviewSnapshotService.FormatName),
                    new XAttribute("formatVersion", PreviewReviewSnapshotService.FormatVersion),
                    new XAttribute("name", snapshot.Name),
                    new XAttribute("projectId", snapshot.ProjectId),
                    new XAttribute("kind", snapshot.Kind.ToString()),
                    new XAttribute("sourceChangeVersion", snapshot.SourceChangeVersion.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("scope", snapshot.Scope),
                    new XAttribute("changedElementCount", snapshot.ChangedElementCount),
                    new XAttribute("regeneratedElementCount", snapshot.RegeneratedElementCount),
                    new XAttribute("newHealthIssueCount", snapshot.NewHealthIssueCount),
                    new XAttribute("newHealthErrorCount", snapshot.NewHealthErrorCount),
                    new XAttribute("resolvedHealthIssueCount", snapshot.ResolvedHealthIssueCount),
                    new XAttribute("omittedHandleFieldCount", snapshot.OmittedHandleFieldCount),
                    new XAttribute("fingerprint", snapshot.Fingerprint),
                    new XElement("targets", snapshot.TargetElementIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(x => new XElement("target", new XAttribute("id", x)))),
                    new XElement("entries", snapshot.Entries.Select(x => new XElement("entry",
                        new XAttribute("elementId", x.ElementId),
                        new XAttribute("category", x.Category),
                        new XAttribute("change", x.Change),
                        new XAttribute("field", x.Field),
                        new XAttribute("before", x.Before),
                        new XAttribute("after", x.After),
                        new XAttribute("beforeProvenance", x.BeforeProvenance),
                        new XAttribute("afterProvenance", x.AfterProvenance))))));
        }

        private static void ValidateXmlShape(XDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var root = document.Root ?? throw new InvalidDataException("Preview review file has no root.");
            foreach (var node in document.Nodes())
            {
                if (ReferenceEquals(node, root)) continue;
                if (node is XText text && string.IsNullOrWhiteSpace(text.Value)) continue;
                throw new InvalidDataException("Preview review XML contains unsupported document-level node content.");
            }
            ValidateElementShape(
                root,
                "qs3dPreviewReview",
                new[]
                {
                    "format", "formatVersion", "name", "projectId", "kind", "sourceChangeVersion", "scope",
                    "changedElementCount", "regeneratedElementCount", "newHealthIssueCount", "newHealthErrorCount",
                    "resolvedHealthIssueCount", "omittedHandleFieldCount", "fingerprint"
                },
                new[] { "targets", "entries" });
            EnsureSingleChild(root, "targets");
            EnsureSingleChild(root, "entries");

            var targets = root.Element("targets")!;
            ValidateElementShape(targets, "targets", Array.Empty<string>(), new[] { "target" });
            foreach (var target in targets.Elements("target"))
                ValidateElementShape(target, "target", new[] { "id" }, Array.Empty<string>());

            var entries = root.Element("entries")!;
            ValidateElementShape(entries, "entries", Array.Empty<string>(), new[] { "entry" });
            foreach (var entry in entries.Elements("entry"))
                ValidateElementShape(
                    entry,
                    "entry",
                    new[] { "elementId", "category", "change", "field", "before", "after", "beforeProvenance", "afterProvenance" },
                    Array.Empty<string>());
        }

        private static void ValidateElementShape(XElement element, string expectedName, IReadOnlyCollection<string> requiredAttributes, IReadOnlyCollection<string> allowedChildren)
        {
            if (element.Name != XName.Get(expectedName))
                throw new InvalidDataException("Preview review XML contains an unsupported element or namespace: " + element.Name + ".");

            var expectedAttributes = new HashSet<XName>(requiredAttributes.Select(XName.Get));
            foreach (var attribute in element.Attributes())
                if (!expectedAttributes.Contains(attribute.Name))
                    throw new InvalidDataException("Preview review XML contains an unsupported attribute on " + expectedName + ": " + attribute.Name + ".");
            foreach (var attributeName in expectedAttributes)
                if (element.Attribute(attributeName) == null)
                    throw new InvalidDataException("Preview review XML is missing required attribute on " + expectedName + ": " + attributeName.LocalName + ".");

            var expectedChildren = new HashSet<XName>(allowedChildren.Select(XName.Get));
            foreach (var node in element.Nodes())
            {
                if (node is XElement child)
                {
                    if (!expectedChildren.Contains(child.Name))
                        throw new InvalidDataException("Preview review XML contains an unsupported child of " + expectedName + ": " + child.Name + ".");
                    continue;
                }

                if (node is XText text && string.IsNullOrWhiteSpace(text.Value)) continue;
                throw new InvalidDataException("Preview review XML contains unsupported node content in " + expectedName + ".");
            }
        }

        private static void EnsureSingleChild(XElement parent, string childName)
        {
            if (parent.Elements(XName.Get(childName)).Count() != 1)
                throw new InvalidDataException("Preview review XML requires exactly one " + childName + " element.");
        }

        private static XDocument LoadDocument(string path)
        {
            var full = Path.GetFullPath(path);
            var info = new FileInfo(full);
            if (info.Length > MaxFileBytes) throw new InvalidDataException("Preview review file exceeds the maximum supported size of 16 MiB.");
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxFileBytes };
            using (var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = XmlReader.Create(stream, settings))
                return XDocument.Load(reader, LoadOptions.None);
        }

        private static string Required(XElement element, string name)
        {
            var value = element.Attribute(name)?.Value;
            if (value == null || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("Missing preview review attribute: " + name + ".");
            return value;
        }

        private static string CanonicalRequired(XElement element, string name)
        {
            var value = Required(element, name);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal)) throw new InvalidDataException("Preview review attribute is not canonical: " + name + ".");
            return value;
        }

        private static string Value(XElement element, string name) => element.Attribute(name)?.Value ?? string.Empty;

        private static int NonNegativeInt(XElement element, string name)
        {
            if (!int.TryParse(Required(element, name), NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 0)
                throw new InvalidDataException("Invalid non-negative preview review integer: " + name + ".");
            return value;
        }

        private static long NonNegativeLong(XElement element, string name)
        {
            if (!long.TryParse(Required(element, name), NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 0)
                throw new InvalidDataException("Invalid non-negative preview review integer: " + name + ".");
            return value;
        }
    }
}
