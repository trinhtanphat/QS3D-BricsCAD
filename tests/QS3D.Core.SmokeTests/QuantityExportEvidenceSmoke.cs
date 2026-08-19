using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityExportEvidenceSmoke
    {
        internal static void Run()
        {
            BuilderKeepsMeasuredZeroDistinctFromAbsentEvidence();
            StandardWorkbookLeavesAbsentMetricsBlankAndKeepsProvenance();
            Ed2WorkbookKeepsDetailSummaryEvidenceParity();
            LegacyRowsRemainNumericByDefault();
        }

        private static void BuilderKeepsMeasuredZeroDistinctFromAbsentEvidence()
        {
            var project = CreateProject();
            var detail = ProjectQuantityReportBuilder.Detail(project);
            var summary = ProjectQuantityReportBuilder.Group(project);

            Require(detail.Count == 2, "Quantity evidence detail fixture must contain two rows.");
            Require(summary.Count == 1, "Quantity evidence summary fixture must contain one row.");

            var first = detail.Single(row => row.ElementIds.Count == 1 && row.ElementIds[0] == "E1");
            var second = detail.Single(row => row.ElementIds.Count == 1 && row.ElementIds[0] == "E2");
            var grouped = summary[0];

            Require(first.GrossConcreteM3 == 0d && first.HasGrossConcreteM3Evidence,
                "A measured zero gross volume must remain numeric evidence.");
            Require(!first.HasFormworkM2Evidence && first.FormworkM2 == 0d,
                "Absent formwork must retain numeric compatibility without claiming evidence.");
            Require(first.HasLengthMEvidence && first.LengthM == 5d,
                "Explicit detail length evidence was lost.");
            Require(!second.HasLengthMEvidence && second.LengthM == 0d,
                "Missing detail length was fabricated as evidence.");
            Require(grouped.HasGrossConcreteM3Evidence && grouped.GrossConcreteM3 == 2d,
                "Complete gross-volume evidence must survive grouping.");
            Require(!grouped.HasLengthMEvidence && grouped.LengthM == 5d,
                "Partial group length must retain its aggregate value without claiming complete evidence.");
            Require(!grouped.HasNetConcreteM3Evidence && grouped.NetConcreteM3 == 2d,
                "Gross-to-net compatibility fallback must not be exported as measured net evidence.");
            Require(!grouped.HasDeductionM3Evidence && grouped.DeductionM3 == 0d,
                "Compatibility deduction fallback must not be exported as measured deduction evidence.");
        }

        private static void StandardWorkbookLeavesAbsentMetricsBlankAndKeepsProvenance()
        {
            var root = TempDirectory("quantity-export-evidence-standard");
            try
            {
                var rows = ProjectQuantityReportBuilder.Detail(CreateProject());
                var path = Path.Combine(root, "evidence.xlsx");
                XlsxQuantityExporter.Export(path, rows);
                var sheet = ReadEntry(path, "xl/worksheets/sheet1.xml");

                RequireCellValue(sheet, "F2", "0", "Measured zero gross volume was not emitted as numeric zero.");
                RequireMissingCell(sheet, "G2", "Absent deduction evidence produced a numeric cell.");
                RequireMissingCell(sheet, "H2", "Absent net evidence produced a numeric cell.");
                RequireMissingCell(sheet, "I2", "Absent formwork evidence produced a numeric cell.");
                RequireCellValue(sheet, "J2", "5", "Explicit length evidence was not exported.");
                RequireMissingCell(sheet, "J3", "Missing length evidence produced a fabricated zero cell.");
                RequireCellText(sheet, "R2", "E1", "Semantic Element ID provenance was lost.");
                RequireCellText(sheet, "S2", "A1", "CAD Handle provenance was lost.");
                RequireCellText(sheet, "T2", "DWG-EXPORT-EVIDENCE", "Drawing fingerprint provenance was lost.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void Ed2WorkbookKeepsDetailSummaryEvidenceParity()
        {
            var root = TempDirectory("quantity-export-evidence-ed2");
            try
            {
                var project = CreateProject();
                var detail = ProjectQuantityReportBuilder.Detail(project);
                var summary = ProjectQuantityReportBuilder.Group(project);
                var path = Path.Combine(root, "evidence-ed2.xlsx");
                XlsxQuantityExporter.ExportEd2(path, detail, summary);

                var detailSheet = ReadEntry(path, "xl/worksheets/sheet1.xml");
                var summarySheet = ReadEntry(path, "xl/worksheets/sheet2.xml");

                RequireCellValue(detailSheet, "H2", "0", "ED2 detail lost measured zero gross evidence.");
                RequireCellValue(detailSheet, "L2", "5", "ED2 detail lost explicit length evidence.");
                RequireMissingCell(detailSheet, "L3", "ED2 detail fabricated missing length evidence.");
                RequireMissingCell(summarySheet, "L2", "ED2 summary claimed complete length evidence for a partial group.");
                RequireCellValue(summarySheet, "H2", "2", "ED2 summary lost complete gross-volume evidence.");
                RequireCellText(detailSheet, "W2", "E1", "ED2 detail lost Element ID provenance.");
                RequireCellText(detailSheet, "X2", "A1", "ED2 detail lost CAD Handle provenance.");
                RequireCellText(detailSheet, "Y2", "DWG-EXPORT-EVIDENCE", "ED2 detail lost drawing fingerprint provenance.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void LegacyRowsRemainNumericByDefault()
        {
            var root = TempDirectory("quantity-export-evidence-legacy");
            try
            {
                var row = new QuantityReportRow
                {
                    Category = "Legacy",
                    FamilyName = "Legacy row",
                    Count = 1,
                    FormworkM2 = 0d,
                    DrawingFingerprint = "DWG-LEGACY"
                };
                var path = Path.Combine(root, "legacy.xlsx");
                XlsxQuantityExporter.Export(path, new[] { row });
                var sheet = ReadEntry(path, "xl/worksheets/sheet1.xml");
                RequireCellValue(sheet, "I2", "0", "Legacy manually-created rows must retain numeric export defaults.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("export-evidence", "Export evidence")
            {
                DrawingFingerprint = "DWG-EXPORT-EVIDENCE"
            };
            project.Families.Add(new ProjectFamily("F1", "Beam family", ElementCategory.Beam));

            var first = new ProjectElement("E1", ElementCategory.Beam, "F1", string.Empty, string.Empty);
            first.SourceHandles.Add("A1");
            first.SetQuantity("GrossVolumeM3", 0d);
            first.SetQuantity("LengthM", 5d);

            var second = new ProjectElement("E2", ElementCategory.Beam, "F1", string.Empty, string.Empty);
            second.SourceHandles.Add("A2");
            second.SetQuantity("GrossVolumeM3", 2d);

            project.Elements.Add(first);
            project.Elements.Add(second);
            return project;
        }

        private static string ReadEntry(string path, string entryName)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                var entry = archive.GetEntry(entryName) ?? throw new Exception("Missing XLSX entry: " + entryName + ".");
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8)) return reader.ReadToEnd();
            }
        }

        private static void RequireCellValue(string sheet, string cellRef, string value, string message)
        {
            var cell = CellXml(sheet, cellRef);
            if (cell == null || cell.IndexOf("<v>" + value + "</v>", StringComparison.Ordinal) < 0)
                throw new Exception(message + " Cell=" + cellRef + ".");
        }

        private static void RequireCellText(string sheet, string cellRef, string value, string message)
        {
            var cell = CellXml(sheet, cellRef);
            if (cell == null || cell.IndexOf(">" + value + "<", StringComparison.Ordinal) < 0)
                throw new Exception(message + " Cell=" + cellRef + ".");
        }

        private static void RequireMissingCell(string sheet, string cellRef, string message)
        {
            if (CellXml(sheet, cellRef) != null) throw new Exception(message + " Cell=" + cellRef + ".");
        }

        private static string? CellXml(string sheet, string cellRef)
        {
            var marker = "<c r=\"" + cellRef + "\"";
            var start = sheet.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return null;
            var end = sheet.IndexOf("</c>", start, StringComparison.Ordinal);
            if (end < 0) throw new Exception("Malformed XLSX cell: " + cellRef + ".");
            return sheet.Substring(start, end + 4 - start);
        }

        private static string TempDirectory(string name)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-smoke-" + name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }

    internal static class QuantityExportEvidenceRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityExportEvidenceSmoke.Run();
    }
}
