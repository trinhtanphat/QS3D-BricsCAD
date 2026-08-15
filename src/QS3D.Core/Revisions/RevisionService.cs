using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using QS3D.Core.Domain;

namespace QS3D.Core.Revisions
{
    public sealed class RevisionElementSnapshot
    {
        public string ElementId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string FamilyId { get; set; } = string.Empty;
        public string FloorId { get; set; } = string.Empty;
        public string ZoneId { get; set; } = string.Empty;
        public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public IDictionary<string, double> Quantities { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        public IList<string> SourceHandles { get; } = new List<string>();
        public IList<string> Dependencies { get; } = new List<string>();
    }

    public sealed class RevisionSnapshot
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public string ProjectId { get; set; } = string.Empty;
        public IList<RevisionElementSnapshot> Elements { get; } = new List<RevisionElementSnapshot>();
    }

    public sealed class RevisionFieldDelta
    {
        public string Field { get; set; } = string.Empty;
        public string Before { get; set; } = string.Empty;
        public string After { get; set; } = string.Empty;
    }

    public sealed class RevisionDelta
    {
        public string ElementId { get; set; } = string.Empty;
        public string Change { get; set; } = string.Empty;
        public IList<RevisionFieldDelta> Fields { get; } = new List<RevisionFieldDelta>();
    }

    public sealed class RevisionService
    {
        private const double QuantityTolerance = 1e-9;

        public RevisionSnapshot Capture(ProjectState project, string revisionId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(revisionId) || !string.Equals(revisionId, revisionId.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Revision id is required and must not contain leading/trailing whitespace.", nameof(revisionId));
            ValidateXmlArgument(revisionId, nameof(revisionId), "Revision id");
            ValidateCanonicalRequired(project.ProjectId, "project id");
            ValidateXmlState(project.ProjectId, "project id");
            var snapshot = new RevisionSnapshot
            {
                Id = revisionId,
                CreatedUtc = DateTime.UtcNow,
                ProjectId = project.ProjectId
            };
            var elementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null || string.IsNullOrWhiteSpace(element.Id)) throw new InvalidOperationException("Revision capture encountered an element without id.");
                if (!elementIds.Add(element.Id)) throw new InvalidOperationException("Revision capture encountered duplicate element id: " + element.Id + ".");
                ValidateOptionalCanonicalIdentity(element.FamilyId, "element " + element.Id + " family id");
                ValidateOptionalCanonicalIdentity(element.FloorId, "element " + element.Id + " floor id");
                ValidateOptionalCanonicalIdentity(element.ZoneId, "element " + element.Id + " zone id");
                var item = new RevisionElementSnapshot
                {
                    ElementId = element.Id,
                    Category = element.Category.ToString(),
                    FamilyId = element.FamilyId,
                    FloorId = element.FloorId,
                    ZoneId = element.ZoneId
                };
                foreach (var property in element.Properties)
                {
                    ValidateCanonicalRequired(property.Key, "element " + element.Id + " property key");
                    item.Properties[property.Key] = property.Value ?? string.Empty;
                }
                foreach (var quantity in element.Quantities)
                {
                    ValidateCanonicalRequired(quantity.Key, "element " + element.Id + " quantity key");
                    item.Quantities[quantity.Key] = RevisionMath.Finite(quantity.Value, element.Id + "/" + quantity.Key);
                }
                foreach (var handle in CanonicalSourceHandles(element)) item.SourceHandles.Add(handle);
                foreach (var dependency in CanonicalDependencies(element.DependsOn, "element " + element.Id)) item.Dependencies.Add(dependency);
                ValidateCaptureXmlPayload(item);
                snapshot.Elements.Add(item);
            }
            return snapshot;
        }

        private static void ValidateCaptureXmlPayload(RevisionElementSnapshot item)
        {
            var label = "element " + item.ElementId;
            ValidateXmlState(item.ElementId, label + " id");
            ValidateXmlState(item.Category, label + " category");
            ValidateXmlState(item.FamilyId, label + " family id");
            ValidateXmlState(item.FloorId, label + " floor id");
            ValidateXmlState(item.ZoneId, label + " zone id");
            foreach (var property in item.Properties)
            {
                ValidateXmlState(property.Key, label + " property key");
                ValidateXmlState(property.Value, label + " property value");
            }
            foreach (var quantity in item.Quantities)
                ValidateXmlState(quantity.Key, label + " quantity key");
            foreach (var handle in item.SourceHandles)
                ValidateXmlState(handle, label + " source handle");
            foreach (var dependency in item.Dependencies)
                ValidateXmlState(dependency, label + " dependency");
        }

        private static void ValidateXmlArgument(string value, string parameterName, string label)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException(label + " contains characters that are invalid in XML.", parameterName, ex);
            }
        }

        private static void ValidateXmlState(string? value, string label)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value ?? string.Empty);
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException("Revision capture " + label + " contains characters that are invalid in XML.", ex);
            }
        }

        public IReadOnlyList<RevisionDelta> Compare(RevisionSnapshot before, RevisionSnapshot after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            ValidateProjectIdentityCompatibility(before, after);
            var result = new List<RevisionDelta>();
            var left = Index(before, "before");
            var right = Index(after, "after");

            foreach (var id in left.Keys.Except(right.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                result.Add(new RevisionDelta { ElementId = id, Change = "Removed" });
            foreach (var id in right.Keys.Except(left.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                result.Add(new RevisionDelta { ElementId = id, Change = "Added" });

            foreach (var id in left.Keys.Intersect(right.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var delta = new RevisionDelta { ElementId = id, Change = "Changed" };
                var a = left[id];
                var b = right[id];
                Add(delta, "Category", a.Category, b.Category);
                AddIdentity(delta, "FamilyId", a.FamilyId, b.FamilyId);
                AddIdentity(delta, "FloorId", a.FloorId, b.FloorId);
                AddIdentity(delta, "ZoneId", a.ZoneId, b.ZoneId);
                CompareSourceHandles(delta, a.SourceHandles, b.SourceHandles, id);
                CompareDependencies(delta, a.Dependencies, b.Dependencies);
                CompareProperties(delta, a.Properties, b.Properties);
                CompareQuantities(delta, a.Quantities, b.Quantities, id);
                if (delta.Fields.Count > 0) result.Add(delta);
            }
            return result.AsReadOnly();
        }

        private static void ValidateProjectIdentityCompatibility(RevisionSnapshot before, RevisionSnapshot after)
        {
            var beforeProjectId = before.ProjectId ?? string.Empty;
            var afterProjectId = after.ProjectId ?? string.Empty;

            if (beforeProjectId.Length == 0 && afterProjectId.Length == 0) return;

            if (beforeProjectId.Length == 0)
                throw new InvalidOperationException("Revision baseline has no project identity; capture a new baseline before comparing.");
            if (afterProjectId.Length == 0)
                throw new InvalidOperationException("Current revision has no project identity; capture a new revision before comparing.");
            ValidateCanonicalRequired(beforeProjectId, "before project id");
            ValidateCanonicalRequired(afterProjectId, "after project id");

            if (!string.Equals(beforeProjectId, afterProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Revision baseline belongs to a different project; capture a new baseline before comparing.");
        }

        private static IReadOnlyList<string> CanonicalSourceHandles(ProjectElement element) =>
            CanonicalSourceHandles(element.SourceHandles, "element " + element.Id);

        private static IReadOnlyList<string> CanonicalSourceHandles(IEnumerable<string> sourceHandles, string label)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var rawValue in sourceHandles ?? Enumerable.Empty<string>())
            {
                var raw = rawValue ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("Revision " + label + " contains a blank source handle at index " + index.ToString(CultureInfo.InvariantCulture) + ".");
                if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Revision " + label + " contains a non-canonical padded source handle: " + raw + ".");
                if (!seen.Add(raw))
                    throw new InvalidOperationException("Revision " + label + " contains a duplicate source handle: " + raw + ".");
                result.Add(raw);
                index++;
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> CanonicalDependencies(IEnumerable<string> dependencies, string label)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var rawValue in dependencies ?? Enumerable.Empty<string>())
            {
                var raw = rawValue ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("Revision " + label + " contains a blank dependency at index " + index.ToString(CultureInfo.InvariantCulture) + ".");
                if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Revision " + label + " contains a non-canonical padded dependency: " + raw + ".");
                if (!seen.Add(raw))
                    throw new InvalidOperationException("Revision " + label + " contains a duplicate dependency: " + raw + ".");
                result.Add(raw);
                index++;
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result.AsReadOnly();
        }

        private static void CompareSourceHandles(RevisionDelta delta, IEnumerable<string> before, IEnumerable<string> after, string elementId)
        {
            var left = CanonicalSourceHandles(before, "before element " + elementId);
            var right = CanonicalSourceHandles(after, "after element " + elementId);
            if (left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase)) return;
            delta.Fields.Add(new RevisionFieldDelta
            {
                Field = "SourceHandles",
                Before = FormatList(left),
                After = FormatList(right)
            });
        }

        private static void CompareDependencies(RevisionDelta delta, IEnumerable<string> before, IEnumerable<string> after)
        {
            var left = CanonicalDependencies(before, "before dependency list");
            var right = CanonicalDependencies(after, "after dependency list");
            if (left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase)) return;
            delta.Fields.Add(new RevisionFieldDelta
            {
                Field = "Dependencies",
                Before = FormatList(left),
                After = FormatList(right)
            });
        }

        private static Dictionary<string, RevisionElementSnapshot> Index(RevisionSnapshot snapshot, string label)
        {
            var result = new Dictionary<string, RevisionElementSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in snapshot.Elements)
            {
                if (element == null || string.IsNullOrWhiteSpace(element.ElementId)) throw new InvalidOperationException("Revision " + label + " contains an element without id.");
                if (!string.Equals(element.ElementId, element.ElementId.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Revision " + label + " contains a non-canonical padded element id: " + element.ElementId + ".");
                ValidateCanonicalCategory(element.Category, label + " element " + element.ElementId + " category");
                ValidateOptionalCanonicalIdentity(element.FamilyId, label + " element " + element.ElementId + " family id");
                ValidateOptionalCanonicalIdentity(element.FloorId, label + " element " + element.ElementId + " floor id");
                ValidateOptionalCanonicalIdentity(element.ZoneId, label + " element " + element.ElementId + " zone id");
                ValidateCanonicalMapKeys(element.Properties, label + " element " + element.ElementId + " property");
                ValidateCanonicalMapKeys(element.Quantities, label + " element " + element.ElementId + " quantity");
                foreach (var quantity in element.Quantities)
                    RevisionMath.Finite(quantity.Value, element.ElementId + "/" + quantity.Key + "/" + label);
                CanonicalSourceHandles(element.SourceHandles, label + " element " + element.ElementId);
                ValidateCanonicalStringList(element.Dependencies, label + " element " + element.ElementId + " dependencies");
                if (result.ContainsKey(element.ElementId)) throw new InvalidOperationException("Revision " + label + " contains duplicate element id: " + element.ElementId);
                result.Add(element.ElementId, element);
            }
            return result;
        }

        private static void ValidateCanonicalCategory(string? value, string label)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !Enum.TryParse(value, true, out ElementCategory category) ||
                !Enum.IsDefined(typeof(ElementCategory), category) ||
                !string.Equals(value, category.ToString(), StringComparison.Ordinal))
                throw new InvalidOperationException("Revision " + label + " must use a canonical element category name.");
        }

        private static void ValidateOptionalCanonicalIdentity(string? value, string label)
        {
            if (value == null || value.Length == 0) return;
            ValidateCanonicalRequired(value, label);
        }

        private static void ValidateCanonicalRequired(string? value, string label)
        {
            if (value == null || string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Revision " + label + " must be non-empty and must not contain leading/trailing whitespace.");
        }

        private static void ValidateCanonicalMapKeys<T>(IDictionary<string, T> values, string label)
        {
            foreach (var key in values.Keys) ValidateCanonicalRequired(key, label + " key");
        }

        private static void ValidateCanonicalStringList(IEnumerable<string> values, string label)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var value in values)
            {
                ValidateCanonicalRequired(value, label + " value at index " + index.ToString(CultureInfo.InvariantCulture));
                if (!seen.Add(value)) throw new InvalidOperationException("Revision " + label + " contains duplicate value: " + value + ".");
                index++;
            }
        }

        private static void CompareProperties(RevisionDelta delta, IDictionary<string, string> before, IDictionary<string, string> after)
        {
            var keys = new HashSet<string>(before.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(after.Keys);
            foreach (var key in keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var hasA = before.TryGetValue(key, out var a);
                var hasB = after.TryGetValue(key, out var b);
                if (hasA != hasB)
                {
                    delta.Fields.Add(new RevisionFieldDelta
                    {
                        Field = "Property:" + key,
                        Before = hasA ? a ?? string.Empty : string.Empty,
                        After = hasB ? b ?? string.Empty : string.Empty
                    });
                    continue;
                }
                Add(delta, "Property:" + key, a ?? string.Empty, b ?? string.Empty);
            }
        }

        private static void CompareQuantities(RevisionDelta delta, IDictionary<string, double> before, IDictionary<string, double> after, string elementId)
        {
            var keys = new HashSet<string>(before.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(after.Keys);
            foreach (var key in keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var hasA = before.TryGetValue(key, out var a);
                var hasB = after.TryGetValue(key, out var b);
                if (hasA) a = RevisionMath.Finite(a, elementId + "/" + key + "/before");
                if (hasB) b = RevisionMath.Finite(b, elementId + "/" + key + "/after");
                if (hasA && hasB && Math.Abs(RevisionMath.Subtract(a, b, elementId + "/" + key)) <= QuantityTolerance) continue;
                Add(delta, "Quantity:" + key, hasA ? F(a, elementId + "/" + key + "/before") : string.Empty, hasB ? F(b, elementId + "/" + key + "/after") : string.Empty);
            }
        }

        private static void AddIdentity(RevisionDelta delta, string field, string before, string after)
        {
            if (string.Equals(before ?? string.Empty, after ?? string.Empty, StringComparison.OrdinalIgnoreCase)) return;
            delta.Fields.Add(new RevisionFieldDelta { Field = field, Before = before ?? string.Empty, After = after ?? string.Empty });
        }

        private static void Add(RevisionDelta delta, string field, string before, string after)
        {
            if (string.Equals(before ?? string.Empty, after ?? string.Empty, StringComparison.Ordinal)) return;
            delta.Fields.Add(new RevisionFieldDelta { Field = field, Before = before ?? string.Empty, After = after ?? string.Empty });
        }

        private static string FormatList(IEnumerable<string> values) =>
            string.Join(",", values.Select(EscapeListToken));

        private static string EscapeListToken(string value) =>
            (value ?? string.Empty).Replace("\\", "\\\\").Replace(",", "\\,");

        private static string F(double value, string label) => RevisionMath.Finite(value, label).ToString("R", CultureInfo.InvariantCulture);
    }
}