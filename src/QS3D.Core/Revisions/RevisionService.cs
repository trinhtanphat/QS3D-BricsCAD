using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            var snapshot = new RevisionSnapshot { Id = revisionId ?? string.Empty, CreatedUtc = DateTime.UtcNow };
            foreach (var element in project.Elements)
            {
                if (element == null || string.IsNullOrWhiteSpace(element.Id)) throw new InvalidOperationException("Revision capture encountered an element without id.");
                var item = new RevisionElementSnapshot
                {
                    ElementId = element.Id,
                    Category = element.Category.ToString(),
                    FamilyId = element.FamilyId,
                    FloorId = element.FloorId,
                    ZoneId = element.ZoneId
                };
                foreach (var property in element.Properties) item.Properties[property.Key] = property.Value ?? string.Empty;
                foreach (var quantity in element.Quantities)
                    item.Quantities[quantity.Key] = RevisionMath.Finite(quantity.Value, element.Id + "/" + quantity.Key);
                foreach (var handle in CanonicalSourceHandles(element)) item.SourceHandles.Add(handle);
                foreach (var dependency in CanonicalDependencies(element.DependsOn)) item.Dependencies.Add(dependency);
                snapshot.Elements.Add(item);
            }
            return snapshot;
        }

        public IReadOnlyList<RevisionDelta> Compare(RevisionSnapshot before, RevisionSnapshot after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
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
            return result;
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

        private static IReadOnlyList<string> CanonicalDependencies(IEnumerable<string> dependencies)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in dependencies ?? Enumerable.Empty<string>())
            {
                var value = (raw ?? string.Empty).Trim();
                if (value.Length == 0 || !seen.Add(value)) continue;
                result.Add(value);
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
                Before = string.Join(",", left),
                After = string.Join(",", right)
            });
        }

        private static void CompareDependencies(RevisionDelta delta, IEnumerable<string> before, IEnumerable<string> after)
        {
            var left = CanonicalDependencies(before);
            var right = CanonicalDependencies(after);
            if (left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase)) return;
            delta.Fields.Add(new RevisionFieldDelta
            {
                Field = "Dependencies",
                Before = string.Join(",", left),
                After = string.Join(",", right)
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
                if (result.ContainsKey(element.ElementId)) throw new InvalidOperationException("Revision " + label + " contains duplicate element id: " + element.ElementId);
                result.Add(element.ElementId, element);
            }
            return result;
        }

        private static void CompareProperties(RevisionDelta delta, IDictionary<string, string> before, IDictionary<string, string> after)
        {
            var keys = new HashSet<string>(before.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(after.Keys);
            foreach (var key in keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                before.TryGetValue(key, out var a);
                after.TryGetValue(key, out var b);
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

        private static string F(double value, string label) => RevisionMath.Finite(value, label).ToString("R", CultureInfo.InvariantCulture);
    }
}
