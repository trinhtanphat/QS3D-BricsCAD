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
        public double Delta => After - Before;
        public double? PercentChange => Math.Abs(Before) < 1e-12 ? (double?)null : Delta / Math.Abs(Before) * 100d;
    }

    public sealed class QuantityRevisionSummary
    {
        public string QuantityName { get; set; } = string.Empty;
        public double Before { get; set; }
        public double After { get; set; }
        public double Delta => After - Before;
    }

    public sealed class QuantityRevisionReport
    {
        public IReadOnlyList<QuantityRevisionRow> Build(RevisionSnapshot before, RevisionSnapshot after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            var left = before.Elements.ToDictionary(x => x.ElementId, StringComparer.OrdinalIgnoreCase);
            var right = after.Elements.ToDictionary(x => x.ElementId, StringComparer.OrdinalIgnoreCase);
<<<<<<< origin/main
            var rows = new List<QuantityRevisionRow>();
            foreach (var id in left.Keys.Union(right.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
=======
            var ids = left.Keys.Union(right.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
            var rows = new List<QuantityRevisionRow>();
            foreach (var id in ids)
>>>>>>> origin/agent/full-domain-20260810
            {
                left.TryGetValue(id, out var a); right.TryGetValue(id, out var b);
                var names = (a?.Quantities.Keys ?? Enumerable.Empty<string>()).Union(b?.Quantities.Keys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                if (names.Count == 0 && (a == null || b == null)) rows.Add(new QuantityRevisionRow { ElementId = id, Category = b?.Category ?? a?.Category ?? string.Empty, Change = a == null ? "Added" : "Removed" });
                foreach (var name in names)
                {
                    var beforeValue = a != null && a.Quantities.TryGetValue(name, out var av) ? av : 0d;
                    var afterValue = b != null && b.Quantities.TryGetValue(name, out var bv) ? bv : 0d;
                    if (a != null && b != null && Math.Abs(beforeValue - afterValue) <= 1e-9) continue;
<<<<<<< origin/main
                    rows.Add(new QuantityRevisionRow { ElementId = id, Category = b?.Category ?? a?.Category ?? string.Empty, QuantityName = name, Change = a == null ? "Added" : b == null ? "Removed" : "Changed", Before = beforeValue, After = afterValue });
=======
                    rows.Add(new QuantityRevisionRow
                    {
                        ElementId = id, Category = b?.Category ?? a?.Category ?? string.Empty, QuantityName = name,
                        Change = a == null ? "Added" : b == null ? "Removed" : "Changed", Before = beforeValue, After = afterValue
                    });
>>>>>>> origin/agent/full-domain-20260810
                }
            }
            return rows;
        }

        public IReadOnlyList<QuantityRevisionSummary> Summarize(IEnumerable<QuantityRevisionRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            return rows.Where(x => !string.IsNullOrWhiteSpace(x.QuantityName)).GroupBy(x => x.QuantityName, StringComparer.OrdinalIgnoreCase)
                .Select(x => new QuantityRevisionSummary { QuantityName = x.Key, Before = x.Sum(y => y.Before), After = x.Sum(y => y.After) })
                .OrderBy(x => x.QuantityName, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
