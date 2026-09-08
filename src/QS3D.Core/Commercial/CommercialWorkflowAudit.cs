using System;
using System.Globalization;
using System.IO;
using QS3D.Core.Audit;

namespace QS3D.Core.Commercial
{
    public static class CommercialWorkflowAudit
    {
        public static void RecordTenderEvaluation(AuditTrail audit, TenderProcurementEvaluation evaluation)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            if (evaluation == null) throw new ArgumentNullException(nameof(evaluation));
            var revision = evaluation.PackageRevision;
            audit.Record(
                "commercial.tender.evaluated",
                evaluation.PackageId,
                "packageRevision=" + revision.RevisionId + ";recommendedBid=" + evaluation.RecommendedBidId,
                correlationId: Correlation(evaluation.PackageId, revision.RevisionId));
        }

        public static void RecordTenderAward(AuditTrail audit, TenderAwardDecision award)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            if (award == null) throw new ArgumentNullException(nameof(award));
            audit.Record(
                "commercial.tender.awarded",
                award.AwardId,
                "package=" + award.PackageId + ";bid=" + award.BidId + ";bidder=" + award.Bidder +
                ";evaluatedTotal=" + Invariant(award.EvaluatedTotal) + ";currency=" + award.Currency +
                ";awardRevision=" + award.AwardRevision.RevisionId,
                correlationId: Correlation(award.AwardId, award.AwardRevision.RevisionId));
        }

        public static void RecordCvrEvaluation(AuditTrail audit, CommercialCostControlResult result)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            if (result == null) throw new ArgumentNullException(nameof(result));
            audit.Record(
                "commercial.cvr.evaluated",
                result.PeriodId,
                CvrDetail(result),
                correlationId: Correlation(result.PeriodId, result.Revision.RevisionId));
        }

        public static void RecordCvrFreeze(AuditTrail audit, CommercialControlPeriod period)
        {
            RecordCvrTransition(audit, "commercial.cvr.frozen", period);
        }

        public static void RecordCvrReopen(AuditTrail audit, CommercialControlPeriod period)
        {
            RecordCvrTransition(audit, "commercial.cvr.reopened", period);
        }

        public static void RecordCvrForecastRevision(AuditTrail audit, CommercialControlPeriod period, CommercialCostControlResult result)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            if (period == null) throw new ArgumentNullException(nameof(period));
            if (result == null) throw new ArgumentNullException(nameof(result));
            audit.Record(
                "commercial.cvr.forecast-revised",
                period.PeriodId,
                "revision=" + period.Revision.RevisionId + ";forecastCostToComplete=" + Invariant(period.ForecastCostToComplete) +
                ";forecastFinalCost=" + Invariant(result.ForecastFinalCost) + ";reason=" + period.TransitionReason,
                correlationId: Correlation(period.PeriodId, period.Revision.RevisionId));
        }

        public static void RecordReportExport(AuditTrail audit, string schemaVersion, string outputPath)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            schemaVersion = RequireToken(schemaVersion, nameof(schemaVersion));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.", nameof(outputPath));
            var fileName = Path.GetFileName(outputPath);
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Output file name is required.", nameof(outputPath));
            audit.Record(
                "commercial.report.exported",
                schemaVersion,
                "file=" + fileName,
                correlationId: "commercial-report:" + schemaVersion);
        }

        private static void RecordCvrTransition(AuditTrail audit, string action, CommercialControlPeriod period)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            if (period == null) throw new ArgumentNullException(nameof(period));
            audit.Record(
                action,
                period.PeriodId,
                "revision=" + period.Revision.RevisionId + ";status=" + period.Status + ";reason=" + period.TransitionReason,
                correlationId: Correlation(period.PeriodId, period.Revision.RevisionId));
        }

        private static string CvrDetail(CommercialCostControlResult result)
        {
            return "revision=" + result.Revision.RevisionId +
                ";revisedBudget=" + Invariant(result.RevisedBudget) +
                ";costToDate=" + Invariant(result.CostToDate) +
                ";committedExposure=" + Invariant(result.CommittedExposure) +
                ";forecastFinalCost=" + Invariant(result.ForecastFinalCost) +
                ";forecastVariance=" + Invariant(result.ForecastVariance) +
                ";cvrMargin=" + Invariant(result.CvrMargin) +
                ";currency=" + result.Currency;
        }

        private static string Correlation(string id, string revision)
        {
            return "commercial:" + RequireToken(id, nameof(id)) + ":" + RequireToken(revision, nameof(revision));
        }

        private static string RequireToken(string value, string parameterName)
        {
            if (value == null || value.Length == 0 || string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("A canonical non-empty token is required.", parameterName);
            return value;
        }

        private static string Invariant(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
