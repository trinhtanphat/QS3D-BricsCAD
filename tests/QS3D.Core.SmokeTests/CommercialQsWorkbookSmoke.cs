using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using QS3D.Core.Commercial;
using QS3D.Core.Cost;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialQsWorkbookSmoke
    {
        public static void Run()
        {
            ExportIsByteStableAndCarriesAllCommercialSections();
            SnapshotRejectsStaleTenderEvaluationRevision();
        }

        private static void ExportIsByteStableAndCarriesAllCommercialSections()
        {
            var register = Register();
            var progress = new ProgressClaimService().Evaluate(
                new[] { new ProgressContractItem("BASE", "m", 100m, 10m) },
                new[] { new ProgressClaimLine("BASE", 20m, 30m) },
                retentionPercent: 10m);
            var ipc = new InterimPaymentCertificateService().Create(
                "IPC-007",
                "VND",
                progress,
                register,
                new[] { new VariationCertificationLine("VO-ADD", 20m, 50m) },
                variationRetentionThisPeriod: 3m,
                retentionRelease: 5m,
                advanceRecovery: 10m,
                otherDeductions: 2m,
                previousNetCertified: 500m);
            var finalAccount = new FinalAccountService().Reconcile(
                "FA-001",
                "VND",
                1000m,
                register,
                -25m,
                1000m,
                50m,
                50m,
                5m);

            var package = Package();
            var tenderService = new TenderProcurementService();
            var evaluation = tenderService.Evaluate(
                package,
                new[]
                {
                    new TenderComplianceResponse("BID-A", "INSURANCE", true, "verified"),
                    new TenderComplianceResponse("BID-B", "INSURANCE", false, "failed")
                });
            var award = tenderService.Award(
                package,
                evaluation,
                "AWD-001",
                "BID-A",
                Revision("procurement-award", "AWD-001", "R1"));
            var cvr = new CommercialCostControlService().Evaluate(
                new CommercialControlPeriod(
                    "P-2026-09",
                    "VND",
                    CommercialControlPeriodStatus.Open,
                    1000m,
                    100m,
                    900m,
                    400m,
                    100m,
                    650m,
                    450m,
                    string.Empty,
                    Revision("commercial-control-period", "P-2026-09", "R4")));

            var snapshot = new CommercialQsWorkbookSnapshot(
                register,
                ipc,
                finalAccount,
                package,
                evaluation,
                award,
                cvr);

            var root = Path.Combine(Path.GetTempPath(), "qs3d-commercial-workbook-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var first = Path.Combine(root, "first.xlsx");
                var second = Path.Combine(root, "second.xlsx");
                CommercialQsWorkbook.Export(first, snapshot);
                CommercialQsWorkbook.Export(second, snapshot);

                var firstBytes = File.ReadAllBytes(first);
                var secondBytes = File.ReadAllBytes(second);
                Require(firstBytes.SequenceEqual(secondBytes), "Commercial workbook export must be byte-stable for identical validated inputs.");

                using (var archive = ZipFile.OpenRead(first))
                {
                    var workbook = ReadEntry(archive, "xl/workbook.xml");
                    foreach (var sheet in new[] { "META", "VARIATIONS", "IPC", "FINAL_ACCOUNT", "TENDER", "CVR" })
                        Contains(workbook, "name=\"" + sheet + "\"", "Commercial workbook is missing fixed worksheet " + sheet + ".");

                    var allXml = string.Join("\n", archive.Entries
                        .Where(x => x.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal))
                        .OrderBy(x => x.FullName, StringComparer.Ordinal)
                        .Select(x => ReadEntry(archive, x.FullName)));
                    Contains(allXml, "QS3D_COMMERCIAL_QS_V1", "Workbook must expose canonical commercial schema provenance.");
                    Contains(allXml, "VO-ADD", "Workbook must retain variation identity.");
                    Contains(allXml, "IPC-007", "Workbook must retain IPC identity.");
                    Contains(allXml, "FA-001", "Workbook must retain Final Account identity.");
                    Contains(allXml, "PKG-01", "Workbook must retain tender package identity.");
                    Contains(allXml, "AWD-001", "Workbook must retain tender award identity.");
                    Contains(allXml, "P-2026-09", "Workbook must retain CVR period identity.");
                    Contains(allXml, "R3", "Workbook must retain tender package revision provenance.");
                    Contains(allXml, "R4", "Workbook must retain CVR revision provenance.");
                    Contains(allXml, "950", "Workbook must project Core forecast final cost without UI recalculation.");

                    var tenderXml = ReadEntry(archive, "xl/worksheets/sheet5.xml");
                    Contains(tenderXml, ">BID_RESULT</t>", "Tender worksheet must include one explicit evaluation row per bid.");
                    Contains(tenderXml, ">Beta</t>", "Tender worksheet must retain evaluated bidder identity.");
                    Contains(tenderXml, ">120</t>", "Tender worksheet must project the Core evaluated total for BID-B.");
                    Contains(tenderXml, ">2</t>", "Tender worksheet must project the Core commercial rank for BID-B.");
                    Contains(tenderXml, ">FAIL</t>", "Tender worksheet must project mandatory compliance outcome for BID-B.");
                }
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void SnapshotRejectsStaleTenderEvaluationRevision()
        {
            var stalePackage = Package("R3");
            var staleEvaluation = new TenderProcurementService().Evaluate(
                stalePackage,
                new[]
                {
                    new TenderComplianceResponse("BID-A", "INSURANCE", true, "verified"),
                    new TenderComplianceResponse("BID-B", "INSURANCE", false, "failed")
                });
            var currentPackage = Package("R5");

            var rejected = false;
            try
            {
                _ = new CommercialQsWorkbookSnapshot(
                    null,
                    null,
                    null,
                    currentPackage,
                    staleEvaluation,
                    null,
                    null);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            Require(rejected, "Commercial workbook snapshot must reject a tender evaluation from a stale package revision.");
        }

        private static CommercialVariationRegister Register()
        {
            return new CommercialVariationRegister("VND", new[]
            {
                new CommercialVariation(
                    "VO-ADD",
                    "Approved additional scope",
                    "VND",
                    120m,
                    120m,
                    CommercialVariationStatus.Approved,
                    Revision("variation", "VO-ADD", "R2"))
            });
        }

        private static TenderProcurementPackage Package(string revision = "R3")
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
                    }),
                    new TenderBid("BID-B", "Beta", "VND", new[]
                    {
                        new TenderQuoteLine("A", 12m)
                    })
                },
                Revision("procurement-package", "PKG-01", revision));
        }

        private static CommercialRevisionRef Revision(string kind, string id, string revision)
            => new CommercialRevisionRef(kind, id, revision);

        private static string ReadEntry(ZipArchive archive, string name)
        {
            var entry = archive.GetEntry(name) ?? throw new Exception("Workbook entry missing: " + name + ".");
            using (var stream = entry.Open())
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        private static void Contains(string actual, string expectedFragment, string message)
        {
            if (actual.IndexOf(expectedFragment, StringComparison.Ordinal) < 0)
                throw new Exception(message + " Missing=" + expectedFragment + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
