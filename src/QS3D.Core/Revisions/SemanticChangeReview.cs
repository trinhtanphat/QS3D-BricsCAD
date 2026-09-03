using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using QS3D.Core.Export;

namespace QS3D.Core.Revisions
{
    public enum SemanticChangeFieldKind
    {
        Identity = 0,
        Property = 1,
        Quantity = 2,
        Other = 3
    }

    public sealed class SemanticChangeReviewField
    {
        internal SemanticChangeReviewField(SemanticChangeFieldKind kind, string field, string before, string after)
        {
            Kind = kind;
            Field = field ?? string.Empty;
            Before = before ?? string.Empty;
            After = after ?? string.Empty;
        }

        public SemanticChangeFieldKind Kind { get; }
        public string Field { get; }
        public string Before { get; }
        public string After { get; }
    }

    public sealed class SemanticChangeReviewElement
    {
        internal SemanticChangeReviewElement(
            string elementId,
            string category,
            string change,
            IEnumerable<SemanticChangeReviewField> fields,
            int omittedSourceReferenceChangeCount)
        {
            ElementId = elementId ?? string.Empty;
            Category = category ?? string.Empty;
            Change = change ?? string.Empty;
            Fields = (fields ?? Enumerable.Empty<SemanticChangeReviewField>()).ToList().AsReadOnly();
            OmittedSourceReferenceChangeCount = omittedSourceReferenceChangeCount;
        }

        public string ElementId { get; }
        public string Category { get; }
        public string Change { get; }
        public IReadOnlyList<SemanticChangeReviewField> Fields { get; }
        public int OmittedSourceReferenceChangeCount { get; }
        public int IdentityChangeCount => Fields.Count(x => x.Kind == SemanticChangeFieldKind.Identity);
        public int PropertyChangeCount => Fields.Count(x => x.Kind == SemanticChangeFieldKind.Property);
        public int QuantityChangeCount => Fields.Count(x => x.Kind == SemanticChangeFieldKind.Quantity);
    }

    public sealed class SemanticChangeReviewSummary
    {
        internal SemanticChangeReviewSummary(
            int addedElementCount,
            int removedElementCount,
            int changedElementCount,
            int identityChangeCount,
            int propertyChangeCount,
            int quantityChangeCount,
            int otherChangeCount,
            int omittedSourceReferenceChangeCount)
        {
            AddedElementCount = addedElementCount;
            RemovedElementCount = removedElementCount;
            ChangedElementCount = changedElementCount;
            IdentityChangeCount = identityChangeCount;
            PropertyChangeCount = propertyChangeCount;
            QuantityChangeCount = quantityChangeCount;
            OtherChangeCount = otherChangeCount;
            OmittedSourceReferenceChangeCount = omittedSourceReferenceChangeCount;
        }

        public int AddedElementCount { get; }
        public int RemovedElementCount { get; }
        public int ChangedElementCount { get; }
        public int IdentityChangeCount { get; }
        public int PropertyChangeCount { get; }
        public int QuantityChangeCount { get; }
        public int OtherChangeCount { get; }
        public int OmittedSourceReferenceChangeCount { get; }
        public int TotalElementCount => checked(AddedElementCount + RemovedElementCount + ChangedElementCount);
        public int VisibleFieldChangeCount => checked(checked(IdentityChangeCount + PropertyChangeCount) + checked(QuantityChangeCount + OtherChangeCount));
    }

    public sealed class SemanticChangeReview
    {
        internal SemanticChangeReview(
            string beforeRevisionId,
            string afterRevisionId,
            IEnumerable<SemanticChangeReviewElement> elements,
            SemanticChangeReviewSummary summary)
        {
            BeforeRevisionId = beforeRevisionId ?? string.Empty;
            AfterRevisionId = afterRevisionId ?? string.Empty;
            Elements = (elements ?? Enumerable.Empty<SemanticChangeReviewElement>()).ToList().AsReadOnly();
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        }

        public string BeforeRevisionId { get; }
        public string AfterRevisionId { get; }
        public IReadOnlyList<SemanticChangeReviewElement> Elements { get; }
        public SemanticChangeReviewSummary Summary { get; }
        public bool HasChanges => Elements.Count > 0;
    }

    public sealed class SemanticChangeReviewBuilder
    {
        private const string SourceHandlesField = "SourceHandles";
        private const string PropertyFieldPrefix = "Property:";

        public SemanticChangeReview Build(RevisionSnapshot before, RevisionSnapshot after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));

            var beforeSnapshot = RevisionSnapshotDetacher.Capture(before, "semantic review before");
            var afterSnapshot = RevisionSnapshotDetacher.Capture(after, "semantic review after");
            var beforeRevisionId = CanonicalRevisionId(beforeSnapshot.Id, "before revision id");
            var afterRevisionId = CanonicalRevisionId(afterSnapshot.Id, "after revision id");

            var beforeIndex = Index(beforeSnapshot, "before");
            var afterIndex = Index(afterSnapshot, "after");
            var deltas = new RevisionService().Compare(beforeSnapshot, afterSnapshot);
            var elements = new List<SemanticChangeReviewElement>(deltas.Count);

            foreach (var delta in deltas)
            {
                if (delta == null || string.IsNullOrWhiteSpace(delta.ElementId))
                    throw new InvalidOperationException("Revision comparison returned an invalid semantic delta.");

                beforeIndex.TryGetValue(delta.ElementId, out var left);
                afterIndex.TryGetValue(delta.ElementId, out var right);
                var category = right?.Category ?? left?.Category ?? string.Empty;
                var fields = new List<SemanticChangeReviewField>();
                var omittedSourceReferences = 0;

                foreach (var field in delta.Fields)
                {
                    if (field == null || string.IsNullOrWhiteSpace(field.Field))
                        throw new InvalidOperationException("Revision comparison returned an invalid field delta for " + delta.ElementId + ".");
                    if (!IsPortableReviewField(field.Field))
                    {
                        omittedSourceReferences++;
                        continue;
                    }

                    fields.Add(new SemanticChangeReviewField(
                        Classify(field.Field),
                        field.Field,
                        field.Before,
                        field.After));
                }

                var orderedFields = fields
                    .OrderBy(x => x.Kind)
                    .ThenBy(x => x.Field, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                elements.Add(new SemanticChangeReviewElement(
                    delta.ElementId,
                    category,
                    delta.Change,
                    orderedFields,
                    omittedSourceReferences));
            }

            var orderedElements = elements
                .OrderBy(x => ChangeRank(x.Change))
                .ThenBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var summary = new SemanticChangeReviewSummary(
                orderedElements.Count(x => string.Equals(x.Change, "Added", StringComparison.Ordinal)),
                orderedElements.Count(x => string.Equals(x.Change, "Removed", StringComparison.Ordinal)),
                orderedElements.Count(x => string.Equals(x.Change, "Changed", StringComparison.Ordinal)),
                orderedElements.Sum(x => x.Fields.Count(f => f.Kind == SemanticChangeFieldKind.Identity)),
                orderedElements.Sum(x => x.Fields.Count(f => f.Kind == SemanticChangeFieldKind.Property)),
                orderedElements.Sum(x => x.Fields.Count(f => f.Kind == SemanticChangeFieldKind.Quantity)),
                orderedElements.Sum(x => x.Fields.Count(f => f.Kind == SemanticChangeFieldKind.Other)),
                orderedElements.Sum(x => x.OmittedSourceReferenceChangeCount));

            if (summary.TotalElementCount != orderedElements.Count)
                throw new InvalidOperationException("Semantic change review summary is inconsistent with its grouped elements.");

            return new SemanticChangeReview(beforeRevisionId, afterRevisionId, orderedElements, summary);
        }

        private static string CanonicalRevisionId(string? value, string label)
        {
            var raw = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw) ||
                !string.Equals(raw, raw.Trim(), StringComparison.Ordinal) ||
                raw.Any(char.IsControl))
            {
                throw new InvalidOperationException(
                    "Revision " + label + " is required and must not contain leading/trailing whitespace or control characters.");
            }

            try
            {
                XmlConvert.VerifyXmlChars(raw);
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException(
                    "Revision " + label + " contains characters that are invalid in XML.", ex);
            }
            return raw;
        }

        private static bool IsPortableReviewField(string field)
        {
            if (string.Equals(field, SourceHandlesField, StringComparison.OrdinalIgnoreCase)) return false;
            if (!field.StartsWith(PropertyFieldPrefix, StringComparison.OrdinalIgnoreCase)) return true;
            var propertyKey = field.Substring(PropertyFieldPrefix.Length);
            return ProjectInterchangeElementPropertyPolicy.IsPortable(propertyKey);
        }

        private static Dictionary<string, RevisionElementSnapshot> Index(RevisionSnapshot snapshot, string label)
        {
            var result = new Dictionary<string, RevisionElementSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in snapshot.Elements)
            {
                if (element == null || string.IsNullOrWhiteSpace(element.ElementId))
                    throw new InvalidOperationException("Revision " + label + " contains an element without id.");
                if (!string.Equals(element.ElementId, element.ElementId.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Revision " + label + " contains a non-canonical padded element id: " + element.ElementId + ".");
                if (result.ContainsKey(element.ElementId))
                    throw new InvalidOperationException("Revision " + label + " contains duplicate element id: " + element.ElementId + ".");
                result.Add(element.ElementId, element);
            }
            return result;
        }

        private static SemanticChangeFieldKind Classify(string field)
        {
            if (string.Equals(field, "Category", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(field, "FamilyId", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(field, "FloorId", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(field, "ZoneId", StringComparison.OrdinalIgnoreCase))
                return SemanticChangeFieldKind.Identity;
            if (field.StartsWith(PropertyFieldPrefix, StringComparison.OrdinalIgnoreCase))
                return SemanticChangeFieldKind.Property;
            if (field.StartsWith("Quantity:", StringComparison.OrdinalIgnoreCase))
                return SemanticChangeFieldKind.Quantity;
            return SemanticChangeFieldKind.Other;
        }

        private static int ChangeRank(string change)
        {
            if (string.Equals(change, "Added", StringComparison.Ordinal)) return 0;
            if (string.Equals(change, "Removed", StringComparison.Ordinal)) return 1;
            if (string.Equals(change, "Changed", StringComparison.Ordinal)) return 2;
            return 3;
        }
    }
}
