using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Intelligence
{
    public enum QsQualitySeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public enum QsRevisionChangeKind
    {
        Unchanged = 0,
        Added = 1,
        Removed = 2,
        Modified = 3
    }

    public sealed class QsQuantityRecord
    {
        public QsQuantityRecord(
            string elementId,
            string source,
            string category,
            string name,
            string quantityType,
            double quantity,
            string unit,
            string discipline = "",
            string classificationCode = "",
            string wbsCode = "",
            string costCode = "",
            string geometryFingerprint = "",
            IDictionary<string, string>? properties = null)
        {
            ElementId = RequireToken(elementId, nameof(elementId));
            Source = RequireText(source, nameof(source));
            Category = RequireText(category, nameof(category));
            Name = RequireText(name, nameof(name));
            QuantityType = RequireToken(quantityType, nameof(quantityType));
            if (double.IsNaN(quantity) || double.IsInfinity(quantity) || quantity < 0d)
                throw new ArgumentOutOfRangeException(nameof(quantity), "QS quantity must be finite and non-negative.");
            Quantity = quantity == 0d ? 0d : quantity;
            Unit = RequireLowerToken(unit, nameof(unit));
            Discipline = OptionalText(discipline, nameof(discipline));
            ClassificationCode = OptionalToken(classificationCode, nameof(classificationCode));
            WbsCode = OptionalToken(wbsCode, nameof(wbsCode));
            CostCode = OptionalToken(costCode, nameof(costCode));
            GeometryFingerprint = OptionalToken(geometryFingerprint, nameof(geometryFingerprint));

            var snapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (properties != null)
            {
                foreach (var pair in properties)
                {
                    var key = RequireToken(pair.Key, nameof(properties));
                    var value = pair.Value ?? string.Empty;
                    EnsureValidUnicodeScalarText(value, nameof(properties));
                    if (snapshot.ContainsKey(key))
                        throw new ArgumentException("Duplicate QS property key: " + key + ".", nameof(properties));
                    snapshot.Add(key, value);
                }
            }
            Properties = new ReadOnlyDictionary<string, string>(snapshot);
        }

        public string ElementId { get; }
        public string Source { get; }
        public string Category { get; }
        public string Name { get; }
        public string QuantityType { get; }
        public double Quantity { get; }
        public string Unit { get; }
        public string Discipline { get; }
        public string ClassificationCode { get; }
        public string WbsCode { get; }
        public string CostCode { get; }
        public string GeometryFingerprint { get; }
        public IReadOnlyDictionary<string, string> Properties { get; }

        public QsQuantityRecord WithClassification(string discipline, string classificationCode)
        {
            return new QsQuantityRecord(
                ElementId,
                Source,
                Category,
                Name,
                QuantityType,
                Quantity,
                Unit,
                discipline,
                classificationCode,
                WbsCode,
                CostCode,
                GeometryFingerprint,
                CopyProperties());
        }

        private IDictionary<string, string> CopyProperties()
        {
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in Properties)
                copy.Add(pair.Key, pair.Value);
            return copy;
        }

        internal static string RequireToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value is required.", parameterName);
            var canonical = value.Trim();
            if (!string.Equals(value, canonical, StringComparison.Ordinal))
                throw new ArgumentException("Value must not contain surrounding whitespace.", parameterName);
            EnsureValidUnicodeScalarText(canonical, parameterName);
            for (var i = 0; i < canonical.Length; i++)
            {
                if (char.IsWhiteSpace(canonical[i]) || char.IsControl(canonical[i]))
                    throw new ArgumentException("Token must not contain whitespace or control characters.", parameterName);
            }
            return canonical;
        }

        internal static string RequireLowerToken(string value, string parameterName)
        {
            var token = RequireToken(value, parameterName);
            var lower = token.ToLowerInvariant();
            if (!string.Equals(token, lower, StringComparison.Ordinal))
                throw new ArgumentException("Token must use canonical lower-case text.", parameterName);
            return token;
        }

        internal static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value is required.", parameterName);
            var canonical = value.Trim();
            if (!string.Equals(value, canonical, StringComparison.Ordinal))
                throw new ArgumentException("Value must not contain surrounding whitespace.", parameterName);
            EnsureValidUnicodeScalarText(canonical, parameterName);
            return canonical;
        }

        internal static string OptionalToken(string? value, string parameterName)
        {
            if (value == null || value.Length == 0) return string.Empty;
            return RequireToken(value, parameterName);
        }

        internal static string OptionalText(string? value, string parameterName)
        {
            if (value == null || value.Length == 0) return string.Empty;
            var canonical = value.Trim();
            if (!string.Equals(value, canonical, StringComparison.Ordinal))
                throw new ArgumentException("Value must not contain surrounding whitespace.", parameterName);
            EnsureValidUnicodeScalarText(canonical, parameterName);
            return canonical;
        }

        private static void EnsureValidUnicodeScalarText(string value, string parameterName)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];
                if (!char.IsSurrogate(current)) continue;
                if (char.IsHighSurrogate(current) &&
                    index + 1 < value.Length &&
                    char.IsLowSurrogate(value[index + 1]))
                {
                    index++;
                    continue;
                }
                throw new ArgumentException("Value must contain valid Unicode scalar text.", parameterName);
            }
        }
    }

    public sealed class QsClassificationResult
    {
        public QsClassificationResult(string discipline, string classificationCode, double confidence, IEnumerable<string> matchedTerms)
        {
            Discipline = QsQuantityRecord.RequireText(discipline, nameof(discipline));
            ClassificationCode = QsQuantityRecord.RequireToken(classificationCode, nameof(classificationCode));
            if (double.IsNaN(confidence) || double.IsInfinity(confidence) || confidence < 0d || confidence > 1d)
                throw new ArgumentOutOfRangeException(nameof(confidence));
            Confidence = confidence;
            if (matchedTerms == null) throw new ArgumentNullException(nameof(matchedTerms));
            var terms = new List<string>();
            foreach (var term in matchedTerms)
                terms.Add(QsQuantityRecord.RequireText(term, nameof(matchedTerms)));
            MatchedTerms = new ReadOnlyCollection<string>(terms.ToArray());
        }

        public string Discipline { get; }
        public string ClassificationCode { get; }
        public double Confidence { get; }
        public IReadOnlyList<string> MatchedTerms { get; }
    }

    public interface IQsClassificationProvider
    {
        QsClassificationResult? Classify(QsQuantityRecord record);
    }

    public sealed class QsQualityFinding
    {
        public QsQualityFinding(string ruleId, QsQualitySeverity severity, string elementId, string message)
        {
            RuleId = QsQuantityRecord.RequireToken(ruleId, nameof(ruleId));
            Severity = severity;
            ElementId = elementId == "*" ? "*" : QsQuantityRecord.RequireToken(elementId, nameof(elementId));
            Message = QsQuantityRecord.RequireText(message, nameof(message));
        }

        public string RuleId { get; }
        public QsQualitySeverity Severity { get; }
        public string ElementId { get; }
        public string Message { get; }
    }

    public sealed class QsRevisionDelta
    {
        public QsRevisionDelta(
            string elementId,
            QsRevisionChangeKind changeKind,
            QsQuantityRecord? previous,
            QsQuantityRecord? current,
            double quantityDelta)
        {
            ElementId = QsQuantityRecord.RequireToken(elementId, nameof(elementId));
            ChangeKind = changeKind;
            Previous = previous;
            Current = current;
            if (double.IsNaN(quantityDelta) || double.IsInfinity(quantityDelta))
                throw new ArgumentOutOfRangeException(nameof(quantityDelta));
            QuantityDelta = quantityDelta == 0d ? 0d : quantityDelta;
        }

        public string ElementId { get; }
        public QsRevisionChangeKind ChangeKind { get; }
        public QsQuantityRecord? Previous { get; }
        public QsQuantityRecord? Current { get; }
        public double QuantityDelta { get; }
    }

    public sealed class QsMissingScope
    {
        public QsMissingScope(string classificationCode, string message)
        {
            ClassificationCode = QsQuantityRecord.RequireToken(classificationCode, nameof(classificationCode));
            Message = QsQuantityRecord.RequireText(message, nameof(message));
        }

        public string ClassificationCode { get; }
        public string Message { get; }
    }

    public sealed class QsBoqSuggestion
    {
        public QsBoqSuggestion(
            string classificationCode,
            string wbsCode,
            string costCode,
            string quantityType,
            string unit,
            string description,
            double quantity,
            int elementCount)
        {
            ClassificationCode = QsQuantityRecord.RequireToken(classificationCode, nameof(classificationCode));
            WbsCode = QsQuantityRecord.OptionalToken(wbsCode, nameof(wbsCode));
            CostCode = QsQuantityRecord.OptionalToken(costCode, nameof(costCode));
            QuantityType = QsQuantityRecord.RequireToken(quantityType, nameof(quantityType));
            Unit = QsQuantityRecord.RequireLowerToken(unit, nameof(unit));
            Description = QsQuantityRecord.RequireText(description, nameof(description));
            if (double.IsNaN(quantity) || double.IsInfinity(quantity) || quantity < 0d)
                throw new ArgumentOutOfRangeException(nameof(quantity));
            if (elementCount <= 0) throw new ArgumentOutOfRangeException(nameof(elementCount));
            Quantity = quantity == 0d ? 0d : quantity;
            ElementCount = elementCount;
        }

        public string ClassificationCode { get; }
        public string WbsCode { get; }
        public string CostCode { get; }
        public string QuantityType { get; }
        public string Unit { get; }
        public string Description { get; }
        public double Quantity { get; }
        public int ElementCount { get; }
    }

    public sealed class QsCostImpactLine
    {
        public QsCostImpactLine(
            string elementId,
            QsRevisionChangeKind changeKind,
            decimal? previousCost,
            decimal? currentCost,
            string currency,
            bool previousRateMissing,
            bool currentRateMissing)
        {
            ElementId = QsQuantityRecord.RequireToken(elementId, nameof(elementId));
            ChangeKind = changeKind;
            PreviousCost = previousCost;
            CurrentCost = currentCost;
            Currency = QsQuantityRecord.RequireToken(currency, nameof(currency));
            PreviousRateMissing = previousRateMissing;
            CurrentRateMissing = currentRateMissing;
        }

        public string ElementId { get; }
        public QsRevisionChangeKind ChangeKind { get; }
        public decimal? PreviousCost { get; }
        public decimal? CurrentCost { get; }
        public decimal? DeltaCost => PreviousCost.HasValue && CurrentCost.HasValue
            ? CurrentCost.Value - PreviousCost.Value
            : (decimal?)null;
        public string Currency { get; }
        public bool PreviousRateMissing { get; }
        public bool CurrentRateMissing { get; }
    }

    public sealed class QsCostImpactSummary
    {
        public QsCostImpactSummary(IEnumerable<QsCostImpactLine> lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            var snapshot = new List<QsCostImpactLine>();
            decimal knownDelta = 0m;
            var unresolved = 0;
            foreach (var line in lines)
            {
                if (line == null) throw new ArgumentException("Cost impact line must not be null.", nameof(lines));
                snapshot.Add(line);
                if (line.DeltaCost.HasValue) knownDelta += line.DeltaCost.Value;
                else unresolved++;
            }
            Lines = new ReadOnlyCollection<QsCostImpactLine>(snapshot.ToArray());
            KnownDeltaCost = knownDelta;
            UnresolvedLineCount = unresolved;
        }

        public IReadOnlyList<QsCostImpactLine> Lines { get; }
        public decimal KnownDeltaCost { get; }
        public int UnresolvedLineCount { get; }
    }

    public sealed class QsIntelligenceReport
    {
        public QsIntelligenceReport(
            IEnumerable<QsQuantityRecord> classifiedCurrentRecords,
            IEnumerable<QsQualityFinding> qualityFindings,
            IEnumerable<QsRevisionDelta> revisionDeltas,
            IEnumerable<QsMissingScope> missingScopes,
            IEnumerable<QsBoqSuggestion> boqSuggestions,
            QsCostImpactSummary? costImpact)
        {
            ClassifiedCurrentRecords = Snapshot(classifiedCurrentRecords, nameof(classifiedCurrentRecords));
            QualityFindings = Snapshot(qualityFindings, nameof(qualityFindings));
            RevisionDeltas = Snapshot(revisionDeltas, nameof(revisionDeltas));
            MissingScopes = Snapshot(missingScopes, nameof(missingScopes));
            BoqSuggestions = Snapshot(boqSuggestions, nameof(boqSuggestions));
            CostImpact = costImpact;
        }

        public IReadOnlyList<QsQuantityRecord> ClassifiedCurrentRecords { get; }
        public IReadOnlyList<QsQualityFinding> QualityFindings { get; }
        public IReadOnlyList<QsRevisionDelta> RevisionDeltas { get; }
        public IReadOnlyList<QsMissingScope> MissingScopes { get; }
        public IReadOnlyList<QsBoqSuggestion> BoqSuggestions { get; }
        public QsCostImpactSummary? CostImpact { get; }

        private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> source, string parameterName) where T : class
        {
            if (source == null) throw new ArgumentNullException(parameterName);
            var result = new List<T>();
            foreach (var item in source)
            {
                if (item == null) throw new ArgumentException("Collection must not contain null values.", parameterName);
                result.Add(item);
            }
            return new ReadOnlyCollection<T>(result.ToArray());
        }
    }
}
