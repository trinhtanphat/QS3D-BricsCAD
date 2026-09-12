using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class TakeoffEvidenceAggregate2D
    {
        public TakeoffEvidenceAggregate2D(string classification, string zone, string unit, double quantity, IReadOnlyList<TakeoffQuantityEvidence2D> evidence)
        {
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            Zone = QsModelElementSnapshot.Optional(zone);
            Unit = QsModelElementSnapshot.Require(unit, "unit");
            Quantity = QsModelElementSnapshot.Finite(quantity, "quantity");
            Evidence = evidence ?? throw new ArgumentNullException("evidence");
            if (Evidence.Count == 0) throw new ArgumentException("Aggregate evidence cannot be empty.", "evidence");
        }

        public string Classification { get; private set; }
        public string Zone { get; private set; }
        public string Unit { get; private set; }
        public double Quantity { get; private set; }
        public IReadOnlyList<TakeoffQuantityEvidence2D> Evidence { get; private set; }
        public int EvidenceCount { get { return Evidence.Count; } }
        public int SheetCount { get { return Evidence.Select(x => x.SheetId).Distinct(StringComparer.OrdinalIgnoreCase).Count(); } }
    }

    public sealed class TakeoffQuantityAggregator2D
    {
        private sealed class AggregateKey : IEquatable<AggregateKey>
        {
            public AggregateKey(string classification, string zone, string unit)
            {
                Classification = classification;
                Zone = zone ?? string.Empty;
                Unit = unit;
            }

            public string Classification { get; private set; }
            public string Zone { get; private set; }
            public string Unit { get; private set; }

            public bool Equals(AggregateKey? other)
            {
                return other != null
                    && string.Equals(Classification, other.Classification, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Zone, other.Zone, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Unit, other.Unit, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object? obj) { return Equals(obj as AggregateKey); }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(Classification);
                    hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Zone);
                    hash = (hash * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Unit);
                    return hash;
                }
            }
        }

        public IReadOnlyList<TakeoffEvidenceAggregate2D> Aggregate(IEnumerable<TakeoffQuantityEvidence2D> evidence)
        {
            if (evidence == null) throw new ArgumentNullException("evidence");

            var materialized = new List<TakeoffQuantityEvidence2D>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in evidence)
            {
                if (item == null) throw new ArgumentException("Evidence collection contains null.", "evidence");
                var identity = item.SheetId + "\u001f" + item.Revision + "\u001f" + item.MarkupId;
                if (!identities.Add(identity))
                    throw new InvalidOperationException("Duplicate takeoff evidence identity for sheet/revision/markup: " + item.SheetId + "/" + item.Revision + "/" + item.MarkupId + ".");
                materialized.Add(item);
            }

            var result = materialized
                .GroupBy(x => new AggregateKey(x.Classification, x.Zone, x.Unit))
                .Select(group =>
                {
                    var orderedEvidence = group
                        .OrderBy(x => x.SheetId, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.Revision, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.MarkupId, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.SourceHandle, StringComparer.Ordinal)
                        .ToList();
                    var quantity = CompensatedSum(orderedEvidence.Select(x => x.Quantity));
                    return new TakeoffEvidenceAggregate2D(
                        group.Key.Classification,
                        group.Key.Zone,
                        group.Key.Unit,
                        quantity,
                        new ReadOnlyCollection<TakeoffQuantityEvidence2D>(orderedEvidence));
                })
                .OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Zone, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ReadOnlyCollection<TakeoffEvidenceAggregate2D>(result);
        }

        private static double CompensatedSum(IEnumerable<double> values)
        {
            var sum = 0d;
            var compensation = 0d;
            foreach (var value in values)
            {
                QsModelElementSnapshot.Finite(value, "quantity");
                var next = sum + value;
                if (Math.Abs(sum) >= Math.Abs(value)) compensation += (sum - next) + value;
                else compensation += (value - next) + sum;
                sum = next;
            }
            return QsModelElementSnapshot.Finite(sum + compensation, "quantity");
        }
    }
}
