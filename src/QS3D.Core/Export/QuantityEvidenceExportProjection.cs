using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Reporting;

namespace QS3D.Core.Export
{
    /// <summary>
    /// Flat, deterministic export record projected directly from a
    /// <see cref="QuantityExplanation"/>. Numeric values are copied from the
    /// evidence graph; exporters must not recalculate takeoff quantities.
    /// </summary>
    public sealed class QuantityEvidenceExportRecord
    {
        public string EvidenceId { get; set; } = string.Empty;
        public string ParentEvidenceId { get; set; } = string.Empty;
        public string RecordKind { get; set; } = string.Empty;
        public string SubjectKey { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal GrossValue { get; set; }
        public decimal NetValue { get; set; }
        public decimal Value { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string SemanticKey { get; set; } = string.Empty;
        public string FormulaOrReason { get; set; } = string.Empty;
        public string SelectorKind { get; set; } = string.Empty;
        public string SelectorKey { get; set; } = string.Empty;
        public string SourceReference { get; set; } = string.Empty;
        public string TargetReference { get; set; } = string.Empty;
        public string Operands { get; set; } = string.Empty;
    }

    public static class QuantityEvidenceExportProjection
    {
        public static IReadOnlyList<QuantityEvidenceExportRecord> Create(QuantityExplanation explanation)
        {
            if (explanation == null) throw new ArgumentNullException(nameof(explanation));

            var rows = new List<QuantityEvidenceExportRecord>(
                1 + explanation.Contributions.Count + explanation.Adjustments.Count);

            rows.Add(new QuantityEvidenceExportRecord
            {
                EvidenceId = explanation.EvidenceId,
                RecordKind = "Summary",
                SubjectKey = explanation.SubjectKey,
                Category = explanation.Category,
                Metric = explanation.Metric,
                Unit = explanation.Unit,
                GrossValue = explanation.GrossValue,
                NetValue = explanation.NetValue,
                Value = explanation.NetValue
            });

            foreach (var contribution in explanation.Contributions)
            {
                var selector = contribution.Selector;
                rows.Add(new QuantityEvidenceExportRecord
                {
                    EvidenceId = contribution.EvidenceId,
                    ParentEvidenceId = explanation.EvidenceId,
                    RecordKind = "Contribution",
                    SubjectKey = explanation.SubjectKey,
                    Category = explanation.Category,
                    Metric = explanation.Metric,
                    Unit = explanation.Unit,
                    GrossValue = explanation.GrossValue,
                    NetValue = explanation.NetValue,
                    Value = contribution.Value,
                    Operation = contribution.Operation.ToString(),
                    SemanticKey = contribution.SemanticKey,
                    FormulaOrReason = contribution.Formula,
                    SelectorKind = selector.Kind.ToString(),
                    SelectorKey = selector.CanonicalKey,
                    SourceReference = selector.Kind == QuantityEvidenceSelectorKind.Intersection
                        ? selector.SourceEntityKey ?? string.Empty
                        : string.Empty,
                    TargetReference = selector.Kind == QuantityEvidenceSelectorKind.Intersection
                        ? selector.TargetEntityKey ?? string.Empty
                        : string.Empty,
                    Operands = FormatOperands(contribution.Operands)
                });
            }

            foreach (var adjustment in explanation.Adjustments)
            {
                rows.Add(new QuantityEvidenceExportRecord
                {
                    EvidenceId = adjustment.EvidenceId,
                    ParentEvidenceId = explanation.EvidenceId,
                    RecordKind = "Adjustment",
                    SubjectKey = explanation.SubjectKey,
                    Category = explanation.Category,
                    Metric = explanation.Metric,
                    Unit = explanation.Unit,
                    GrossValue = explanation.GrossValue,
                    NetValue = explanation.NetValue,
                    Value = adjustment.Delta,
                    Operation = adjustment.Operation.ToString(),
                    SemanticKey = adjustment.SemanticKey,
                    FormulaOrReason = adjustment.RuleKey + ": " + adjustment.Reason,
                    SelectorKind = adjustment.Selector.Kind.ToString(),
                    SelectorKey = adjustment.Selector.CanonicalKey,
                    SourceReference = adjustment.SourceReference,
                    TargetReference = adjustment.TargetReference
                });
            }

            return rows;
        }

        public static IReadOnlyList<QuantityEvidenceExportRecord> CreateMany(
            IReadOnlyList<QuantityExplanation> explanations)
        {
            if (explanations == null) throw new ArgumentNullException(nameof(explanations));

            var ordered = new List<QuantityExplanation>(explanations.Count);
            for (var index = 0; index < explanations.Count; index++)
            {
                var explanation = explanations[index];
                if (explanation == null)
                    throw new ArgumentException("Quantity explanations cannot contain null entries.", nameof(explanations));
                ordered.Add(explanation);
            }

            ordered.Sort((left, right) => string.CompareOrdinal(left.EvidenceId, right.EvidenceId));

            var rows = new List<QuantityEvidenceExportRecord>();
            foreach (var explanation in ordered)
                rows.AddRange(Create(explanation));
            return rows;
        }

        private static string FormatOperands(IReadOnlyList<QuantityEvidenceOperand> operands)
        {
            if (operands == null || operands.Count == 0) return string.Empty;
            return string.Join("; ", operands.Select(operand =>
                operand.Key + "=" + operand.Value.ToString("G29", CultureInfo.InvariantCulture) +
                (string.IsNullOrWhiteSpace(operand.Unit) ? string.Empty : " " + operand.Unit)));
        }
    }
}
