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
                var item = new RevisionElementSnapshot
                {
                    ElementId = element.Id,
                    Category = element.Category.ToString(),
                    FamilyId = element.FamilyId,
                    FloorId = element.FloorId,
                    ZoneId = element.ZoneId
                };
                foreach (var property in element.Properties) item.Properties[property.Key] = property.Value ?? string.Empty;
                foreach (var quantity in element.Quantities) item.Quantities[quantity.Key] = quantity.Value;
                foreach (var handle in element.SourceHandles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) item.SourceHandles.Add(handle);
                snapshot.Elements.Add(item);
            }
            return snapshot;
        }

        public IReadOnlyList<RevisionDelta> Compare(RevisionSnapshot before, RevisionSnapshot after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            var result = new List<RevisionDelta>();
            var left = before.Elements.ToDictionary(x => x.ElementId, StringComparer.OrdinalIgnoreCase);
            var right = after.Elements.ToDictionary(x => x.ElementId, StringComparer.OrdinalIgnoreCase);

            foreach (var id in left.Keys.Except(right.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                result.Add(new RevisionDelta { ElementId = id, Change = "Removed" });
            foreach (var id in right.Keys.Except(left.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                result.Add(new RevisionDelta { ElementId = id, Change = "Added" });

            foreach (var id in left.Keys.Intersect(right.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var delta = new RevisionDelta { ElementId = id, Change = "Changed" };
                var a = left[id]; var b = right[id];
                Add(delta, "Category", a.Category, b.Category);
                Add(delta, "FamilyId", a.FamilyId, b.FamilyId);
                Add(delta, "FloorId", a.FloorId, b.FloorId);
                Add(delta, "ZoneId", a.ZoneId, b.ZoneId);
                Add(delta, "SourceHandles", string.Join(",", a.SourceHandles), string.Join(",", b.SourceHandles));
                CompareProperties(delta, a.Properties, b.Properties);
                CompareQuantities(delta, a.Quantities, b.Quantities);
                if (delta.Fields.Count > 0) result.Add(delta);
            }
            return result;
        }

        private static void CompareProperties(RevisionDelta delta, IDictionary<string, string> before, IDictionary<string, string> after)
        {
            var keys = new HashSet<string>(before.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(after.Keys);
            foreach (var key in keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                before.TryGetValue(key, out var a); after.TryGetValue(key, out var b);
                Add(delta, "Property:" + key, a ?? string.Empty, b ?? string.Empty);
            }
        }

        private static void CompareQuantities(RevisionDelta delta, IDictionary<string, double> before, IDictionary<string, double> after)
        {
            var keys = new HashSet<string>(before.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(after.Keys);
            foreach (var key in keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var hasA = before.TryGetValue(key, out var a); var hasB = after.TryGetValue(key, out var b);
                if (hasA && hasB && Math.Abs(a - b) <= QuantityTolerance) continue;
                Add(delta, "Quantity:" + key, hasA ? F(a) : string.Empty, hasB ? F(b) : string.Empty);
            }
        }

        private static void Add(RevisionDelta delta, string field, string before, string after)
        {
            if (string.Equals(before ?? string.Empty, after ?? string.Empty, StringComparison.Ordinal)) return;
            delta.Fields.Add(new RevisionFieldDelta { Field = field, Before = before ?? string.Empty, After = after ?? string.Empty });
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
