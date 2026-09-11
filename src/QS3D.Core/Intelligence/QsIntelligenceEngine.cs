using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using QS3D.Core.Cost;

namespace QS3D.Core.Intelligence
{
    public sealed class QsRevisionQuantityEngine
    {
        public IReadOnlyList<QsRevisionDelta> Compare(
            IEnumerable<QsQuantityRecord> previousRecords,
            IEnumerable<QsQuantityRecord> currentRecords,
            bool includeUnchanged = false)
        {
            if (previousRecords == null) throw new ArgumentNullException(nameof(previousRecords));
            if (currentRecords == null) throw new ArgumentNullException(nameof(currentRecords));

            var previous = Index(previousRecords, nameof(previousRecords));
            var current = Index(currentRecords, nameof(currentRecords));
            var ids = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in previous.Keys) ids.Add(id);
            foreach (var id in current.Keys) ids.Add(id);

            var result = new List<QsRevisionDelta>();
            foreach (var id in ids)
            {
                previous.TryGetValue(id, out var before);
                current.TryGetValue(id, out var after);

                if (before == null)
                {
                    result.Add(new QsRevisionDelta(id, QsRevisionChangeKind.Added, null, after, after!.Quantity));
                    continue;
                }

                if (after == null)
                {
                    result.Add(new QsRevisionDelta(id, QsRevisionChangeKind.Removed, before, null, -before.Quantity));
                    continue;
                }

                var equivalent = AreEquivalent(before, after);
                if (equivalent && !includeUnchanged) continue;
                var comparable = string.Equals(before.QuantityType, after.QuantityType, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(before.Unit, after.Unit, StringComparison.OrdinalIgnoreCase);
                var delta = comparable ? CheckedSubtract(after.Quantity, before.Quantity) : 0d;
                result.Add(new QsRevisionDelta(
                    id,
                    equivalent ? QsRevisionChangeKind.Unchanged : QsRevisionChangeKind.Modified,
                    before,
                    after,
                    delta));
            }

            return new ReadOnlyCollection<QsRevisionDelta>(result.ToArray());
        }

        private static Dictionary<string, QsQuantityRecord> Index(IEnumerable<QsQuantityRecord> records, string parameterName)
        {
            var result = new Dictionary<string, QsQuantityRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in records)
            {
                if (record == null) throw new ArgumentException("QS record must not be null.", parameterName);
                if (result.ContainsKey(record.ElementId))
                    throw new ArgumentException("Duplicate element id in revision snapshot: " + record.ElementId + ".", parameterName);
                result.Add(record.ElementId, record);
            }
            return result;
        }

        private static bool AreEquivalent(QsQuantityRecord left, QsQuantityRecord right)
        {
            return StringComparer.Ordinal.Equals(left.Source, right.Source) &&
                StringComparer.OrdinalIgnoreCase.Equals(left.Category, right.Category) &&
                StringComparer.Ordinal.Equals(left.Name, right.Name) &&
                StringComparer.OrdinalIgnoreCase.Equals(left.QuantityType, right.QuantityType) &&
                left.Quantity.Equals(right.Quantity) &&
                StringComparer.OrdinalIgnoreCase.Equals(left.Unit, right.Unit) &&
                StringComparer.OrdinalIgnoreCase.Equals(left.Discipline, right.Discipline) &&
                StringComparer.OrdinalIgnoreCase.Equals(left.ClassificationCode, right.ClassificationCode) &&
                StringComparer.OrdinalIgnoreCase.Equals(left.WbsCode, right.WbsCode) &&
                StringComparer.OrdinalIgnoreCase.Equals(left.CostCode, right.CostCode) &&
                StringComparer.Ordinal.Equals(left.GeometryFingerprint, right.GeometryFingerprint) &&
                PropertiesEqual(left.Properties, right.Properties);
        }

        private static bool PropertiesEqual(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
        {
            if (left.Count != right.Count) return false;
            foreach (var pair in left)
            {
                if (!right.TryGetValue(pair.Key, out var value)) return false;
                if (!string.Equals(pair.Value, value, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private static double CheckedSubtract(double right, double left)
        {
            var value = right - left;
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new OverflowException("Revision quantity delta overflowed finite Double range.");
            return value == 0d ? 0d : value;
        }
    }

    public sealed class QsMissingScopeDetector
    {
        public IReadOnlyList<QsMissingScope> Detect(
            IEnumerable<QsQuantityRecord> records,
            IEnumerable<string> requiredClassificationCodes)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (requiredClassificationCodes == null) throw new ArgumentNullException(nameof(requiredClassificationCodes));

            var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in records)
            {
                if (record == null) throw new ArgumentException("QS record must not be null.", nameof(records));
                if (!string.IsNullOrEmpty(record.ClassificationCode)) present.Add(record.ClassificationCode);
            }

            var required = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var code in requiredClassificationCodes)
                required.Add(QsQuantityRecord.RequireToken(code, nameof(requiredClassificationCodes)));

            var missing = new List<QsMissingScope>();
            foreach (var code in required)
            {
                if (present.Contains(code)) continue;
                missing.Add(new QsMissingScope(code, "Required QS scope is absent from the current quantity snapshot."));
            }
            return new ReadOnlyCollection<QsMissingScope>(missing.ToArray());
        }
    }

    public sealed class QsBoqSuggestionEngine
    {
        public IReadOnlyList<QsBoqSuggestion> Build(IEnumerable<QsQuantityRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            var groups = new Dictionary<GroupKey, Group>();

            foreach (var record in records)
            {
                if (record == null) throw new ArgumentException("QS record must not be null.", nameof(records));
                var classification = string.IsNullOrEmpty(record.ClassificationCode) ? "UNCLASSIFIED" : record.ClassificationCode;
                var key = new GroupKey(classification, record.WbsCode, record.CostCode, record.QuantityType, record.Unit);
                if (!groups.TryGetValue(key, out var group))
                {
                    group = new Group(classification, record.WbsCode, record.CostCode, record.QuantityType, record.Unit, record.Name);
                    groups.Add(key, group);
                }
                group.Add(record.Quantity);
            }

            var ordered = new List<Group>(groups.Values);
            ordered.Sort(Group.Compare);
            var result = new List<QsBoqSuggestion>();
            for (var i = 0; i < ordered.Count; i++)
            {
                var group = ordered[i];
                result.Add(new QsBoqSuggestion(
                    group.ClassificationCode,
                    group.WbsCode,
                    group.CostCode,
                    group.QuantityType,
                    group.Unit,
                    group.Description,
                    group.Total,
                    group.Count));
            }
            return new ReadOnlyCollection<QsBoqSuggestion>(result.ToArray());
        }

        private sealed class GroupKey : IEquatable<GroupKey>
        {
            internal GroupKey(string classificationCode, string wbsCode, string costCode, string quantityType, string unit)
            {
                ClassificationCode = classificationCode;
                WbsCode = wbsCode;
                CostCode = costCode;
                QuantityType = quantityType;
                Unit = unit;
            }

            private string ClassificationCode { get; }
            private string WbsCode { get; }
            private string CostCode { get; }
            private string QuantityType { get; }
            private string Unit { get; }

            public bool Equals(GroupKey? other)
            {
                return other != null &&
                    StringComparer.OrdinalIgnoreCase.Equals(ClassificationCode, other.ClassificationCode) &&
                    StringComparer.OrdinalIgnoreCase.Equals(WbsCode, other.WbsCode) &&
                    StringComparer.OrdinalIgnoreCase.Equals(CostCode, other.CostCode) &&
                    StringComparer.OrdinalIgnoreCase.Equals(QuantityType, other.QuantityType) &&
                    StringComparer.OrdinalIgnoreCase.Equals(Unit, other.Unit);
            }

            public override bool Equals(object? obj) => Equals(obj as GroupKey);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(ClassificationCode);
                    hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(WbsCode);
                    hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(CostCode);
                    hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(QuantityType);
                    return hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(Unit);
                }
            }
        }

        private sealed class Group
        {
            private double _sum;
            private double _compensation;

            internal Group(string classificationCode, string wbsCode, string costCode, string quantityType, string unit, string description)
            {
                ClassificationCode = classificationCode;
                WbsCode = wbsCode;
                CostCode = costCode;
                QuantityType = quantityType;
                Unit = unit;
                Description = description;
            }

            internal string ClassificationCode { get; }
            internal string WbsCode { get; }
            internal string CostCode { get; }
            internal string QuantityType { get; }
            internal string Unit { get; }
            internal string Description { get; }
            internal int Count { get; private set; }
            internal double Total => _sum;

            internal void Add(double value)
            {
                var adjusted = value - _compensation;
                var next = _sum + adjusted;
                _compensation = (next - _sum) - adjusted;
                if (double.IsNaN(next) || double.IsInfinity(next))
                    throw new OverflowException("BOQ quantity aggregation overflowed finite Double range.");
                _sum = next == 0d ? 0d : next;
                checked { Count++; }
            }

            internal static int Compare(Group left, Group right)
            {
                var classification = StringComparer.OrdinalIgnoreCase.Compare(left.ClassificationCode, right.ClassificationCode);
                if (classification != 0) return classification;
                var wbs = StringComparer.OrdinalIgnoreCase.Compare(left.WbsCode, right.WbsCode);
                if (wbs != 0) return wbs;
                var cost = StringComparer.OrdinalIgnoreCase.Compare(left.CostCode, right.CostCode);
                if (cost != 0) return cost;
                var quantity = StringComparer.OrdinalIgnoreCase.Compare(left.QuantityType, right.QuantityType);
                if (quantity != 0) return quantity;
                return StringComparer.OrdinalIgnoreCase.Compare(left.Unit, right.Unit);
            }
        }
    }

    public sealed class QsCostImpactEngine
    {
        public QsCostImpactSummary Evaluate(
            IEnumerable<QsRevisionDelta> deltas,
            RateBook rateBook,
            string currency,
            DateTime asOfUtc)
        {
            if (deltas == null) throw new ArgumentNullException(nameof(deltas));
            if (rateBook == null) throw new ArgumentNullException(nameof(rateBook));
            var canonicalCurrency = QsQuantityRecord.RequireToken(currency, nameof(currency)).ToUpperInvariant();
            if (asOfUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Cost impact valuation timestamp must be UTC.", nameof(asOfUtc));

            var lines = new List<QsCostImpactLine>();
            foreach (var delta in deltas)
            {
                if (delta == null) throw new ArgumentException("Revision delta must not be null.", nameof(deltas));
                if (delta.ChangeKind == QsRevisionChangeKind.Unchanged) continue;

                var previousRateMissing = false;
                var currentRateMissing = false;
                decimal? previousCost = delta.Previous == null
                    ? 0m
                    : ResolveCost(delta.Previous, rateBook, canonicalCurrency, asOfUtc, out previousRateMissing);
                decimal? currentCost = delta.Current == null
                    ? 0m
                    : ResolveCost(delta.Current, rateBook, canonicalCurrency, asOfUtc, out currentRateMissing);

                lines.Add(new QsCostImpactLine(
                    delta.ElementId,
                    delta.ChangeKind,
                    previousCost,
                    currentCost,
                    canonicalCurrency,
                    previousRateMissing,
                    currentRateMissing));
            }
            return new QsCostImpactSummary(lines);
        }

        private static decimal? ResolveCost(
            QsQuantityRecord record,
            RateBook rateBook,
            string currency,
            DateTime asOfUtc,
            out bool rateMissing)
        {
            rateMissing = false;
            if (string.IsNullOrEmpty(record.CostCode))
            {
                rateMissing = true;
                return null;
            }

            var resolution = rateBook.Resolve(new CostCode(record.CostCode), record.Unit, currency, asOfUtc);
            if (!resolution.IsMatched || resolution.Item == null)
            {
                rateMissing = true;
                return null;
            }

            decimal quantity;
            try
            {
                quantity = Convert.ToDecimal(record.Quantity);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException("QS quantity is outside Decimal range for 5D valuation: " + record.ElementId + ".", ex);
            }

            try
            {
                return checked(quantity * resolution.Item.UnitRate);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException("QS cost valuation overflowed Decimal range: " + record.ElementId + ".", ex);
            }
        }
    }

    /// <summary>
    /// Unified takeoff intelligence facade: classify -> QA -> revision -> missing scope -> BOQ -> 5D cost impact.
    /// It operates on normalized records so CAD, BIM, IFC and imported takeoff adapters can share the same workflow.
    /// </summary>
    public sealed class QsIntelligencePipeline
    {
        private readonly IQsClassificationProvider _classificationProvider;
        private readonly QsQualityAnalyzer _qualityAnalyzer;
        private readonly QsRevisionQuantityEngine _revisionEngine;
        private readonly QsMissingScopeDetector _scopeDetector;
        private readonly QsBoqSuggestionEngine _boqEngine;
        private readonly QsCostImpactEngine _costImpactEngine;

        public QsIntelligencePipeline()
            : this(new RuleBasedQsClassifier())
        {
        }

        public QsIntelligencePipeline(IQsClassificationProvider classificationProvider)
        {
            _classificationProvider = classificationProvider ?? throw new ArgumentNullException(nameof(classificationProvider));
            _qualityAnalyzer = new QsQualityAnalyzer();
            _revisionEngine = new QsRevisionQuantityEngine();
            _scopeDetector = new QsMissingScopeDetector();
            _boqEngine = new QsBoqSuggestionEngine();
            _costImpactEngine = new QsCostImpactEngine();
        }

        public QsIntelligenceReport Run(
            IEnumerable<QsQuantityRecord>? previousRecords,
            IEnumerable<QsQuantityRecord> currentRecords,
            IEnumerable<string> requiredClassificationCodes)
        {
            return Run(previousRecords, currentRecords, requiredClassificationCodes, null, string.Empty, null);
        }

        public QsIntelligenceReport Run(
            IEnumerable<QsQuantityRecord>? previousRecords,
            IEnumerable<QsQuantityRecord> currentRecords,
            IEnumerable<string> requiredClassificationCodes,
            RateBook? rateBook,
            string currency,
            DateTime? asOfUtc)
        {
            if (currentRecords == null) throw new ArgumentNullException(nameof(currentRecords));
            if (requiredClassificationCodes == null) throw new ArgumentNullException(nameof(requiredClassificationCodes));
            if (rateBook != null && !asOfUtc.HasValue)
                throw new ArgumentException("A UTC valuation timestamp is required when a rate book is supplied.", nameof(asOfUtc));
            if (rateBook != null && string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency is required when a rate book is supplied.", nameof(currency));

            var current = ClassifyMissing(currentRecords, nameof(currentRecords));
            var previous = previousRecords == null
                ? new ReadOnlyCollection<QsQuantityRecord>(new QsQuantityRecord[0])
                : ClassifyMissing(previousRecords, nameof(previousRecords));

            var quality = _qualityAnalyzer.Analyze(current);
            var deltas = _revisionEngine.Compare(previous, current);
            var missingScopes = _scopeDetector.Detect(current, requiredClassificationCodes);
            var boq = _boqEngine.Build(current);
            var cost = rateBook == null
                ? null
                : _costImpactEngine.Evaluate(deltas, rateBook, currency, asOfUtc!.Value);

            return new QsIntelligenceReport(current, quality, deltas, missingScopes, boq, cost);
        }

        private IReadOnlyList<QsQuantityRecord> ClassifyMissing(IEnumerable<QsQuantityRecord> records, string parameterName)
        {
            var result = new List<QsQuantityRecord>();
            foreach (var record in records)
            {
                if (record == null) throw new ArgumentException("QS record must not be null.", parameterName);
                if (!string.IsNullOrEmpty(record.ClassificationCode))
                {
                    result.Add(record);
                    continue;
                }

                var classification = _classificationProvider.Classify(record);
                result.Add(classification == null
                    ? record
                    : record.WithClassification(classification.Discipline, classification.ClassificationCode));
            }
            return new ReadOnlyCollection<QsQuantityRecord>(result.ToArray());
        }
    }
}
