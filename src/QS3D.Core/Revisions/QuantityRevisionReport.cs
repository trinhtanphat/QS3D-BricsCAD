using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Revisions
{
    public sealed class QuantityRevisionRow
    {
        public string ElementId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string QuantityName { get; set; } = string.Empty;
        public string Change { get; set; } = string.Empty;
        public double Before { get; set; }
        public double After { get; set; }
        public double Delta => RevisionMath.Subtract(After, Before, ElementId + "/" + QuantityName);
        public double? PercentChange => Math.Abs(RevisionMath.Finite(Before, ElementId + "/" + QuantityName + "/Before")) < 1e-12 ? (double?)null : RevisionMath.Percent(Delta, Before, ElementId + "/" + QuantityName);
    }

    public sealed class QuantityRevisionSummary
    {
        public string QuantityName { get; set; } = string.Empty;
        public double Before { get; set; }
        public double After { get; set; }
        public double Delta => RevisionMath.Subtract(After, Before, QuantityName);
    }

    public sealed class QuantityRevisionReport
    {
        public IReadOnlyList<QuantityRevisionRow> Build(RevisionSnapshot before, RevisionSnapshot after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            var left = Index(before, "before");
            var right = Index(after, "after");
            var rows = new List<QuantityRevisionRow>();
            foreach (var id in left.Keys.Union(right.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                left.TryGetValue(id, out var a); right.TryGetValue(id, out var b);
                var names = (a?.Quantities.Keys ?? Enumerable.Empty<string>()).Union(b?.Quantities.Keys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                if (names.Count == 0 && (a == null || b == null)) rows.Add(new QuantityRevisionRow { ElementId = id, Category = b?.Category ?? a?.Category ?? string.Empty, Change = a == null ? "Added" : "Removed" });
                foreach (var name in names)
                {
                    var av = 0d; var bv = 0d;
                    var hasBefore = a != null && a.Quantities.TryGetValue(name, out av);
                    var hasAfter = b != null && b.Quantities.TryGetValue(name, out bv);
                    var beforeValue = hasBefore ? RevisionMath.Finite(av, id + "/" + name + "/before") : 0d;
                    var afterValue = hasAfter ? RevisionMath.Finite(bv, id + "/" + name + "/after") : 0d;
                    var delta = RevisionMath.Subtract(afterValue, beforeValue, id + "/" + name);
                    if (a != null && b != null && hasBefore && hasAfter && Math.Abs(delta) <= 1e-9) continue;
                    rows.Add(new QuantityRevisionRow { ElementId = id, Category = b?.Category ?? a?.Category ?? string.Empty, QuantityName = name, Change = !hasBefore ? "Added" : !hasAfter ? "Removed" : "Changed", Before = beforeValue, After = afterValue });
                }
            }
            return rows;
        }

        public IReadOnlyList<QuantityRevisionSummary> Summarize(IEnumerable<QuantityRevisionRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var result = new List<QuantityRevisionSummary>();
            foreach (var group in rows.Where(x => x != null && !string.IsNullOrWhiteSpace(x.QuantityName)).GroupBy(x => x.QuantityName, StringComparer.OrdinalIgnoreCase).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var before = 0d;
                var after = 0d;
                foreach (var row in group)
                {
                    before = RevisionMath.Add(before, row.Before, group.Key + "/Before");
                    after = RevisionMath.Add(after, row.After, group.Key + "/After");
                }
                result.Add(new QuantityRevisionSummary { QuantityName = group.Key, Before = before, After = after });
            }
            return result;
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
    }
}
