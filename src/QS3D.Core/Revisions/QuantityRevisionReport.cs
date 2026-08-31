using System;
using System.Collections;
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
            var beforeSnapshot = RevisionSnapshotDetacher.Capture(before, "before");
            var afterSnapshot = RevisionSnapshotDetacher.Capture(after, "after");
            ValidateProjectIdentityCompatibility(beforeSnapshot, afterSnapshot);
            var left = Index(beforeSnapshot, "before");
            var right = Index(afterSnapshot, "after");
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
            var knownCount = SnapshotKnownSummaryCount(rows);
            var summarizable = new List<QuantityRevisionRow>();
            var index = 0;
            using (var enumerator = rows.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownSummaryCount(rows, knownCount);
                    var moved = enumerator.MoveNext();
                    RequireStableKnownSummaryCount(rows, knownCount);
                    if (!moved) break;

                    if (knownCount.HasValue && index >= knownCount.Value)
                        throw KnownSummaryCountTraversalMismatch(knownCount.Value, index + 1);

                    var row = enumerator.Current;
                    RequireStableKnownSummaryCount(rows, knownCount);
                    if (row == null)
                        throw new ArgumentException("Quantity revision summary contains a null row at index " + index + ".", nameof(rows));
                    if (!string.IsNullOrEmpty(row.QuantityName) && row.QuantityName.Any(char.IsControl))
                        ValidateCanonicalRequired(row.QuantityName, "summary row " + index + " quantity key");
                    if (!string.IsNullOrWhiteSpace(row.QuantityName))
                    {
                        ValidateCanonicalRequired(row.QuantityName, "summary row " + index + " quantity key");
                        summarizable.Add(row);
                    }
                    index++;
                }
            }

            if (knownCount.HasValue && index != knownCount.Value)
                throw KnownSummaryCountTraversalMismatch(knownCount.Value, index);

            RequireStableKnownSummaryCount(rows, knownCount);

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

        private static int? SnapshotKnownSummaryCount(IEnumerable<QuantityRevisionRow> rows)
        {
            int? knownCount = null;
            if (rows is ICollection<QuantityRevisionRow> genericCollection)
                ObserveKnownSummaryCount(genericCollection.Count, ref knownCount);
            if (rows is IReadOnlyCollection<QuantityRevisionRow> readOnlyCollection)
                ObserveKnownSummaryCount(readOnlyCollection.Count, ref knownCount);
            if (rows is ICollection nonGenericCollection)
                ObserveKnownSummaryCount(nonGenericCollection.Count, ref knownCount);
            return knownCount;
        }

        private static void RequireStableKnownSummaryCount(IEnumerable<QuantityRevisionRow> rows, int? knownCount)
        {
            var currentKnownCount = SnapshotKnownSummaryCount(rows);
            if (knownCount != currentKnownCount)
                throw new InvalidOperationException(
                    "Quantity revision summary input known Count changed during traversal from " +
                    FormatKnownCount(knownCount) + " to " + FormatKnownCount(currentKnownCount) + ".");
        }

        private static void ObserveKnownSummaryCount(int count, ref int? knownCount)
        {
            if (count < 0)
                throw new InvalidOperationException("Quantity revision summary input reported a negative known Count.");
            if (knownCount.HasValue && knownCount.Value != count)
                throw new InvalidOperationException(
                    "Quantity revision summary input exposes conflicting known Counts: " + knownCount.Value + " and " + count + ".");
            knownCount = count;
        }

        private static InvalidOperationException KnownSummaryCountTraversalMismatch(int knownCount, int observedCount)
        {
            return new InvalidOperationException(
                "Quantity revision summary input changed during enumeration; Count reported " + knownCount +
                " rows but traversal produced " + observedCount + ".");
        }

        private static string FormatKnownCount(int? knownCount) =>
            knownCount.HasValue ? knownCount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "<none>";

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

        private static Dictionary<string, RevisionElementSnapshot> Index(RevisionSnapshot snapshot, string label)
        {
            var result = new Dictionary<string, RevisionElementSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in snapshot.Elements)
            {
                if (element == null) throw new InvalidOperationException("Revision " + label + " contains a null element.");
                ValidateCanonicalRequired(element.ElementId, label + " element id");
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
            if (value == null ||
                string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
                value.Any(char.IsControl))
            {
                throw new InvalidOperationException(
                    "Revision " + label + " must be non-empty and must not contain leading/trailing whitespace or control characters.");
            }
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

            internal double Value(string label)
            {
                var result = RevisionMath.Add(_sum, _compensation, label);
                if (_compensation != 0d && result == _sum)
                    throw new OverflowException("Revision quantity total lost a non-zero compensation at floating-point precision: " + label);
                if (_sum != 0d && result == _compensation)
                    throw new OverflowException("Revision quantity total lost a non-zero primary sum at floating-point precision: " + label);
                return result;
            }
        }
    }
}
