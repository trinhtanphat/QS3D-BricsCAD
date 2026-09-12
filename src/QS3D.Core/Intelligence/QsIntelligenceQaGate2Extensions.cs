using System;
using System.Collections.Generic;
using QS3D.Core.BenchmarkParity;
using QS3D.Core.Cost;

namespace QS3D.Core.Intelligence
{
    /// <summary>
    /// Auditable result from the QA Gate 2.0 protected QS Intelligence path.
    /// The QA decision is returned with the normal intelligence report so hosts can
    /// surface active/waived findings without re-evaluating the authoritative snapshot.
    /// </summary>
    public sealed class QsQaGuardedIntelligenceReport
    {
        public QsQaGuardedIntelligenceReport(QsQaGate2Decision qaDecision, QsIntelligenceReport intelligenceReport)
        {
            QaDecision = qaDecision ?? throw new ArgumentNullException(nameof(qaDecision));
            IntelligenceReport = intelligenceReport ?? throw new ArgumentNullException(nameof(intelligenceReport));
        }

        public QsQaGate2Decision QaDecision { get; }
        public QsIntelligenceReport IntelligenceReport { get; }
    }

    /// <summary>
    /// Production integration boundary between Solibri-parity QA Gate 2.0 and the
    /// normalized QS Intelligence pipeline. Existing Run(...) overloads remain intact
    /// for compatibility; new production hosts that own authoritative model/IFC QA
    /// snapshots should use RunWithQaGate2(...).
    /// </summary>
    public static class QsIntelligenceQaGate2Extensions
    {
        public static QsQaGuardedIntelligenceReport RunWithQaGate2(
            this QsIntelligencePipeline pipeline,
            IEnumerable<QsModelElementSnapshot> qaElements,
            QsQaRuleProfile qaProfile,
            IEnumerable<QsQaWaiver>? waivers,
            DateTime qaEvaluationUtc,
            IEnumerable<QsQuantityRecord>? previousRecords,
            IEnumerable<QsQuantityRecord> currentRecords,
            IEnumerable<string> requiredClassificationCodes)
        {
            return RunWithQaGate2(
                pipeline,
                qaElements,
                qaProfile,
                waivers,
                qaEvaluationUtc,
                previousRecords,
                currentRecords,
                requiredClassificationCodes,
                null,
                string.Empty,
                null);
        }

        public static QsQaGuardedIntelligenceReport RunWithQaGate2(
            this QsIntelligencePipeline pipeline,
            IEnumerable<QsModelElementSnapshot> qaElements,
            QsQaRuleProfile qaProfile,
            IEnumerable<QsQaWaiver>? waivers,
            DateTime qaEvaluationUtc,
            IEnumerable<QsQuantityRecord>? previousRecords,
            IEnumerable<QsQuantityRecord> currentRecords,
            IEnumerable<string> requiredClassificationCodes,
            RateBook? rateBook,
            string currency,
            DateTime? asOfUtc)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            if (qaElements == null) throw new ArgumentNullException(nameof(qaElements));
            if (qaProfile == null) throw new ArgumentNullException(nameof(qaProfile));
            if (currentRecords == null) throw new ArgumentNullException(nameof(currentRecords));
            if (requiredClassificationCodes == null) throw new ArgumentNullException(nameof(requiredClassificationCodes));
            if (qaEvaluationUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("QA Gate 2.0 evaluation timestamp must be UTC.", nameof(qaEvaluationUtc));

            var decision = new QsQaGate2().Evaluate(qaElements, qaProfile, waivers, qaEvaluationUtc);
            var executor = new QsQaGuardedExecutor();

            // Treat the unified pipeline as the protected downstream boundary. Nest all
            // three named workflow demands so the contract remains fail-closed if the
            // pipeline expands its takeoff/BOQ/estimate responsibilities later.
            var report = executor.Execute(decision, QsQaGuardedWorkflow.Takeoff, () =>
                executor.Execute(decision, QsQaGuardedWorkflow.Boq, () =>
                    executor.Execute(decision, QsQaGuardedWorkflow.Estimate, () =>
                        pipeline.Run(
                            previousRecords,
                            currentRecords,
                            requiredClassificationCodes,
                            rateBook,
                            currency,
                            asOfUtc))));

            return new QsQaGuardedIntelligenceReport(decision, report);
        }
    }
}
