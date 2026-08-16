using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

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
            return rows.AsReadOnly();
        }

        public IReadOnlyList<QuantityRevisionSummary> Summarize(IEnumerable<QuantityRevisionRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var summarizable = new List<QuantityRevisionRow>();
            var index = 0;
            foreach (var row in rows)
            {
                if (row == null)
                    throw new ArgumentException("Quantity revision summary contains a null row at index " + index + ".", nameof(rows));
                if (!string.IsNullOrWhiteSpace(row.QuantityName))
                {
                    ValidateCanonicalRequired(row.QuantityName, "summary row " + index + " quantity key");
                    summarizable.Add(row);
                }
                index++;
            }

            var result = new List<QuantityRevisionSummary>();
            foreach (var group in summarizable.GroupBy(x => x.QuantityName, StringComparer.OrdinalIgnoreCase).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var before = new CompensatedFiniteSum();
                var after = new CompensatedFiniteSum();
                foreach (var row in group)
                {
                    before.Add(row.Before, group.Key + "/Before");
                    after.Add(row.After, group.Key + "/After");
                }
                result.Add(new QuantityRevisionSummary
                {
                    QuantityName = group.Key,
                    Before = before.Value(group.Key + "/Before"),
                    After = after.Value(group.Key + "/After")
                });
            }
            return result.AsReadOnly();
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
                foreach (var quantity in element.Quantities)
                {
                    ValidateCanonicalRequired(quantity.Key, label + " element " + element.ElementId + " quantity key");
                    RevisionMath.Finite(quantity.Value, element.ElementId + "/" + quantity.Key + "/" + label);
                }
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

        private static void ValidateCanonicalRequired(string? value, string label)
        {
            if (value == null || string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Revision " + label + " must be non-empty and must not contain leading/trailing whitespace.");
        }

        private struct CompensatedFiniteSum
        {
            private double _sum;
            private double _compensation;

            internal void Add(double value, string label)
            {
                var next = RevisionMath.Add(_sum, value, label);
                var compensationLabel = label + " compensation";
                var correction = Math.Abs(_sum) >= Math.Abs(value)
                    ? RevisionMath.Add(RevisionMath.Subtract(_sum, next, compensationLabel), value, compensationLabel)
                    : RevisionMath.Add(RevisionMath.Subtract(value, next, compensationLabel), _sum, compensationLabel);

                _compensation = RevisionMath.Add(_compensation, correction, compensationLabel);
                _sum = next;
            }

            internal double Value(string label) => RevisionMath.Add(_sum, _compensation, label);
        }
    }
}
