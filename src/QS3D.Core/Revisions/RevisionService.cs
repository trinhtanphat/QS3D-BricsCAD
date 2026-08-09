using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Revisions
{
    public sealed class RevisionElementSnapshot
    {
        public string ElementId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string FamilyId { get; set; } = string.Empty;
        public IDictionary<string, double> Quantities { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RevisionSnapshot
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public IList<RevisionElementSnapshot> Elements { get; } = new List<RevisionElementSnapshot>();
    }

    public sealed class RevisionDelta
    {
        public string ElementId { get; set; } = string.Empty;
        public string Change { get; set; } = string.Empty;
    }

    public sealed class RevisionService
    {
        public RevisionSnapshot Capture(ProjectState project, string revisionId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var snapshot = new RevisionSnapshot { Id = revisionId ?? string.Empty, CreatedUtc = DateTime.UtcNow };
            foreach (var element in project.Elements)
            {
                var item = new RevisionElementSnapshot { ElementId = element.Id, Category = element.Category.ToString(), FamilyId = element.FamilyId };
                foreach (var quantity in element.Quantities) item.Quantities[quantity.Key] = quantity.Value;
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
            foreach (var id in left.Keys.Except(right.Keys, StringComparer.OrdinalIgnoreCase)) result.Add(new RevisionDelta { ElementId = id, Change = "Removed" });
            foreach (var id in right.Keys.Except(left.Keys, StringComparer.OrdinalIgnoreCase)) result.Add(new RevisionDelta { ElementId = id, Change = "Added" });
            foreach (var id in left.Keys.Intersect(right.Keys, StringComparer.OrdinalIgnoreCase))
            {
                var a = left[id]; var b = right[id];
                if (!string.Equals(a.Category, b.Category, StringComparison.Ordinal) || !string.Equals(a.FamilyId, b.FamilyId, StringComparison.Ordinal) || !QuantityEqual(a.Quantities, b.Quantities))
                    result.Add(new RevisionDelta { ElementId = id, Change = "Changed" });
            }
            return result;
        }

        private static bool QuantityEqual(IDictionary<string, double> a, IDictionary<string, double> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var pair in a)
                if (!b.TryGetValue(pair.Key, out var value) || Math.Abs(value - pair.Value) > 1e-9) return false;
            return true;
        }
    }
}
