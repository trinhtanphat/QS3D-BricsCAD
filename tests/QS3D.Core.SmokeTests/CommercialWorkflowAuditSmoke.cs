using System;
using QS3D.Core.Audit;
using QS3D.Core.Commercial;
using QS3D.Core.Cost;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialWorkflowAuditSmoke
    {
        public static void Run()
        {
            RecordsCanonicalTenderAndCvrActions();
            RecordsReportExportWithoutInventingPersistence();
        }

        private static void RecordsCanonicalTenderAndCvrActions()
        {
            var project = new ProjectState("P-AUDIT", "Commercial audit smoke");
            var audit = AuditTrail.ForProject(project);
            var package = Package();
            var service = new TenderProcurementService();
            var evaluation = service.Evaluate(
                package,
                new[] { new TenderComplianceResponse("BID-A", "INSURANCE", true, "verified") });
            var award = service.Award(
                package,
                evaluation,
                "AWD-001",
                "BID-A",
                Revision("procurement-award", "AWD-001", "R1"));
            var cvr = new CommercialCostControlService().Evaluate(Period("R1", 450m));

            CommercialWorkflowAudit.RecordTenderEvaluation(audit, evaluation);
            CommercialWorkflowAudit.RecordTenderAward(audit, award);
            CommercialWorkflowAudit.RecordCvrEvaluation(audit, cvr);

            Equal(3, audit.Events.Count, "Commercial audit should append one event per successful Core action.");
            Equal("commercial.tender.evaluated", audit.Events[0].Action, "Tender evaluation action changed.");
            Equal("PKG-01", audit.Events[0].ElementId, "Tender evaluation must bind package identity.");
            Contains(audit.Events[0].Detail, "R3", "Tender evaluation detail must retain package revision provenance.");
            Equal("commercial:PKG-01:R3", audit.Events[0].CorrelationId, "Tender evaluation correlation id must bind package revision.");

            Equal("commercial.tender.awarded", audit.Events[1].Action, "Tender award action changed.");
            Equal("AWD-001", audit.Events[1].ElementId, "Tender award must bind award identity.");
            Contains(audit.Events[1].Detail, "BID-A", "Tender award detail must retain selected bid identity.");
            Contains(audit.Events[1].Detail, "R1", "Tender award detail must retain award revision provenance.");
            Equal("commercial:AWD-001:R1", audit.Events[1].CorrelationId, "Tender award correlation id must bind award revision.");

            Equal("commercial.cvr.evaluated", audit.Events[2].Action, "CVR evaluation action changed.");
            Equal("P-2026-09", audit.Events[2].ElementId, "CVR audit must bind period identity.");
            Contains(audit.Events[2].Detail, "forecastFinalCost=950", "CVR audit must use the Core-produced forecast final cost.");
            Equal("commercial:P-2026-09:R1", audit.Events[2].CorrelationId, "CVR correlation id must bind period revision.");
        }

        private static void RecordsReportExportWithoutInventingPersistence()
        {
            var project = new ProjectState("P-REPORT", "Commercial report audit smoke");
            var audit = AuditTrail.ForProject(project);
            var before = project.ChangeVersion;

            CommercialWorkflowAudit.RecordReportExport(
                audit,
                "QS3D_COMMERCIAL_QS_V1",
                "commercial-report.xlsx");

            Equal(1, audit.Events.Count, "Report export audit should append exactly one event.");
            Equal("commercial.report.exported", audit.Events[0].Action, "Report export action changed.");
            Equal("QS3D_COMMERCIAL_QS_V1", audit.Events[0].ElementId, "Report audit must bind schema identity.");
            Contains(audit.Events[0].Detail, "commercial-report.xlsx", "Report audit must retain safe output file name.");
            Require(project.ChangeVersion > before, "Audit recording must use existing ProjectState mutation tracking rather than a parallel store.");
        }

        private static TenderProcurementPackage Package()
        {
            return new TenderProcurementPackage(
                "PKG-01",
                "Structural works",
                "VND",
                ProcurementPackageStatus.Closed,
                new[] { new TenderRequirement("A", "Item A", "m", 10m) },
                new[] { new TenderComplianceRequirement("INSURANCE", "Valid insurance", true) },
                new[]
                {
                    new TenderBid("BID-A", "Alpha", "VND", new[]
                    {
                        new TenderQuoteLine("A", 10m)
                    })
                },
                Revision("procurement-package", "PKG-01", "R3"));
        }

        private static CommercialControlPeriod Period(string revision, decimal forecastToComplete)
        {
            return new CommercialControlPeriod(
                "P-2026-09",
                "VND",
                CommercialControlPeriodStatus.Open,
                1000m,
                100m,
                900m,
                400m,
                100m,
                650m,
                forecastToComplete,
                string.Empty,
                Revision("commercial-control-period", "P-2026-09", revision));
        }

        private static CommercialRevisionRef Revision(string kind, string id, string revision)
            => new CommercialRevisionRef(kind, id, revision);

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Contains(string actual, string expectedFragment, string message)
        {
            if (actual == null || actual.IndexOf(expectedFragment, StringComparison.Ordinal) < 0)
                throw new Exception(message + " Missing=" + expectedFragment + ", actual=" + actual + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
