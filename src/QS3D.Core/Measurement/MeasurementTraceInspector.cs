using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Measurement
{
    public sealed class MeasurementTraceInspectorFact
    {
        internal MeasurementTraceInspectorFact(MeasurementTraceFact fact)
        {
            Name = fact.Name;
            Value = fact.Value;
            Unit = fact.Unit;
            SourceIdentity = fact.SourceIdentity;
        }

        public string Name { get; }
        public double Value { get; }
        public string Unit { get; }
        public string? SourceIdentity { get; }
    }

    public sealed class MeasurementTraceInspectorAdjustment
    {
        internal MeasurementTraceInspectorAdjustment(MeasurementTraceAdjustment adjustment)
        {
            Kind = adjustment.Kind;
            Amount = adjustment.Amount;
            Unit = adjustment.Unit;
            Reason = adjustment.Reason;
            SourceIdentity = adjustment.SourceIdentity;
            RuleId = adjustment.RuleId;
            RuleVersion = adjustment.RuleVersion;
        }

        public MeasurementTraceAdjustmentKind Kind { get; }
        public double Amount { get; }
        public string Unit { get; }
        public string Reason { get; }
        public string SourceIdentity { get; }
        public string? RuleId { get; }
        public string? RuleVersion { get; }
    }

    /// <summary>
    /// Read-only presentation projection for a canonical <see cref="MeasurementTrace"/>.
    /// This type copies trace evidence exactly; it never recalculates quantities,
    /// adjustments, conversions, or rule outcomes.
    /// </summary>
    public sealed class MeasurementTraceInspector
    {
        private MeasurementTraceInspector(MeasurementTrace trace)
        {
            SemanticIdentity = trace.SemanticIdentity;
            SourceIdentity = trace.SourceIdentity;
            QuantityKey = trace.QuantityKey;
            GrossValue = trace.GrossValue;
            NetValue = trace.NetValue;
            Unit = trace.Unit;
            RoundingPolicy = trace.RoundingPolicy;
            RuleId = trace.RuleId;
            RuleVersion = trace.RuleVersion;

            var facts = new List<MeasurementTraceInspectorFact>(trace.InputFacts.Count);
            for (var i = 0; i < trace.InputFacts.Count; i++)
                facts.Add(new MeasurementTraceInspectorFact(trace.InputFacts[i]));
            InputFacts = new ReadOnlyCollection<MeasurementTraceInspectorFact>(facts.ToArray());

            var adjustments = new List<MeasurementTraceInspectorAdjustment>(trace.Adjustments.Count);
            for (var i = 0; i < trace.Adjustments.Count; i++)
                adjustments.Add(new MeasurementTraceInspectorAdjustment(trace.Adjustments[i]));
            Adjustments = new ReadOnlyCollection<MeasurementTraceInspectorAdjustment>(adjustments.ToArray());

            Warnings = CopyMessages(trace.Warnings);
            Assumptions = CopyMessages(trace.Assumptions);
        }

        public string SemanticIdentity { get; }
        public string SourceIdentity { get; }
        public string QuantityKey { get; }
        public double GrossValue { get; }
        public double NetValue { get; }
        public string Unit { get; }
        public string RoundingPolicy { get; }
        public string? RuleId { get; }
        public string? RuleVersion { get; }
        public IReadOnlyList<MeasurementTraceInspectorFact> InputFacts { get; }
        public IReadOnlyList<MeasurementTraceInspectorAdjustment> Adjustments { get; }
        public IReadOnlyList<string> Warnings { get; }
        public IReadOnlyList<string> Assumptions { get; }

        public static MeasurementTraceInspector FromTrace(MeasurementTrace trace)
        {
            if (trace == null) throw new ArgumentNullException(nameof(trace));
            return new MeasurementTraceInspector(trace);
        }

        private static IReadOnlyList<string> CopyMessages(IReadOnlyList<string> source)
        {
            var messages = new string[source.Count];
            for (var i = 0; i < source.Count; i++)
                messages[i] = source[i];
            return new ReadOnlyCollection<string>(messages);
        }
    }
}
