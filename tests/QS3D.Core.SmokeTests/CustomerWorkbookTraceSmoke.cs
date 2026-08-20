using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CustomerWorkbookTraceSmoke
    {
        internal static void Run()
        {
            CustomerWorkbookRoundTripsDetailAndAggregateTrace();
            CustomerWorkbookPreservesEvidenceBlankVersusMeasuredZero();
            CustomerTraceReaderRejectsUnsupportedSheet();
        }

        private static void CustomerWorkbookRoundTripsDetailAndAggregateTrace()
        {
            var root = TempDirectory("customer-workbook-trace");
            try
            {
                var path = Path.Combine(root, "qs-customer.xlsx");
                QsCustomerWorkbookExporter.Export(path, Details(), Summary());

                var workbook = ReadEntry(path, "xl/workbook.xml");
                Require(workbook.Contains("name=\"DGKL\""), "Customer workbook is missing DGKL.");
                Require(workbook.Contains("name=\"COP_PHA\""), "Customer workbook is missing COP_PHA.");
                Require(workbook.Contains("name=\"CHI_TIET\""), "Customer workbook is missing CHI_TIET.");
                Require(workbook.Contains("name=\"TRACE_MODEL\""), "Customer workbook is missing TRACE_MODEL.");
                Require(Count(workbook, "<sheet ") == 4, "Customer workbook must expose exactly four worksheets.");

                var aggregate = QsCustomerWorkbookTraceReader.Read(path, "DGKL", 2);
                Require(aggregate.ElementIds.Count == 2, "DGKL trace must preserve both grouped Element IDs.");
                Require(aggregate.Handles.Count == 2, "DGKL trace must preserve both grouped CAD Handles.");
                Require(aggregate.DrawingFingerprint == "DWG-CUSTOMER-TRACE", "DGKL trace lost drawing fingerprint.");

                var formwork = QsCustomerWorkbookTraceReader.Read(path, "COP_PHA", 2);
                Require(formwork.ElementIds.Count == 2 && formwork.Handles.Count == 2,
                    "COP_PHA aggregate trace must preserve the complete source scope.");

                var detail = QsCustomerWorkbookTraceReader.Read(path, "CHI_TIET", 2);
                Require(detail.ElementIds.Count == 1 && detail.ElementIds[0] == "E1", "CHI_TIET trace must preserve one semantic element.");
                Require(detail.Handles.Count == 1 && detail.Handles[0] == "A1", "CHI_TIET trace must preserve one canonical CAD Handle.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void CustomerWorkbookPreservesEvidenceBlankVersusMeasuredZero()
        {
            var root = TempDirectory("customer-workbook-evidence");
            try
            {
                var path = Path.Combine(root, "qs-customer.xlsx");
                QsCustomerWorkbookExporter.Export(path, Details(), Summary());
                var detail = ReadEntry(path, "xl/worksheets/sheet3.xml");

                // CHI_TIET: H=Gross, I=Deduction. E1 has measured gross zero but no deduction evidence.
                RequireCellValue(detail, "H2", "0", "Measured zero gross quantity must remain numeric zero.");
                RequireMissingCell(detail, "I2", "Unsupported deduction must remain blank rather than fabricated zero.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void CustomerTraceReaderRejectsUnsupportedSheet()
        {
            var root = TempDirectory("customer-workbook-fail-closed");
            try
            {
                var path = Path.Combine(root, "qs-customer.xlsx");
                QsCustomerWorkbookExporter.Export(path, Details(), Summary());
                ExpectThrows<ArgumentException>(() => QsCustomerWorkbookTraceReader.Read(path, "TRACE_MODEL", 2),
                    "TRACE_MODEL must not be accepted as a user business-row locate source.");
                ExpectThrows<ArgumentOutOfRangeException>(() => QsCustomerWorkbookTraceReader.Read(path, "DGKL", 1),
                    "Header row must not be accepted as a business-row trace.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static QuantityReportRow[] Details()
        {
            var first = NewRow("E1", "A1", 0d, 12d);
            first.HasDeductionM3Evidence = false;
            first.DeductionM3 = 0d;
            var second = NewRow("E2", "A2", 2d, 18d);
            second.DeductionM3 = 0d;
            return new[] { first, second };
        }

        private static QuantityReportRow[] Summary()
        {
            var row = new QuantityReportRow
            {
                Floor = "L01",
                Zone = "A",
                Category = "Beam",
                FamilyId = "F-BEAM",
                FamilyName = "Beam 300x600",
                Material = "Concrete",
                DrawingFingerprint = "DWG-CUSTOMER-TRACE",
                Count = 2,
                GrossConcreteM3 = 2d,
                DeductionM3 = 0d,
                NetConcreteM3 = 2d,
                FormworkM2 = 30d,
                HasGrossConcreteM3Evidence = true,
                HasDeductionM3Evidence = false,
                HasNetConcreteM3Evidence = true,
                HasFormworkM2Evidence = true,
                HasLengthMEvidence = false,
                HasOuterPerimeterMEvidence = false,
                HasInnerPerimeterMEvidence = false,
                HasDoorAreaM2Evidence = false,
                HasSideAreaM2Evidence = false,
                HasBottomAreaM2Evidence = false,
                HasTopAreaM2Evidence = false,
                HasOtherAreaM2Evidence = false
            };
            row.ElementIds.Add("E1");
            row.ElementIds.Add("E2");
            row.SourceHandles.Add("A1");
            row.SourceHandles.Add("A2");
            return new[] { row };
        }

        private static QuantityReportRow NewRow(string elementId, string handle, double gross, double formwork)
        {
            var row = new QuantityReportRow
            {
                Floor = "L01",
                Zone = "A",
                Category = "Beam",
                FamilyId = "F-BEAM",
                FamilyName = "Beam 300x600",
                ElementName = "Beam " + elementId,
                Material = "Concrete",
                DrawingFingerprint = "DWG-CUSTOMER-TRACE",
                Count = 1,
                GrossConcreteM3 = gross,
                NetConcreteM3 = gross,
                FormworkM2 = formwork,
                HasGrossConcreteM3Evidence = true,
                HasNetConcreteM3Evidence = true,
                HasFormworkM2Evidence = true,
                HasLengthMEvidence = false,
                HasOuterPerimeterMEvidence = false,
                HasInnerPerimeterMEvidence = false,
                HasDoorAreaM2Evidence = false,
                HasSideAreaM2Evidence = false,
                HasBottomAreaM2Evidence = false,
                HasTopAreaM2Evidence = false,
                HasOtherAreaM2Evidence = false
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            return row;
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
            if (cell == null || cell.IndexOf("<v>" + value + "</v>", StringComparison.Ordinal) < 0) throw new Exception(message);
        }

        private static void RequireMissingCell(string sheet, string cellRef, string message)
        {
            if (CellXml(sheet, cellRef) != null) throw new Exception(message);
        }

        private static string CellXml(string sheet, string cellRef)
        {
            var marker = "<c r=\"" + cellRef + "\"";
            var start = sheet.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return null;
            var end = sheet.IndexOf("</c>", start, StringComparison.Ordinal);
            if (end < 0) throw new Exception("Malformed XLSX cell " + cellRef + ".");
            return sheet.Substring(start, end + 4 - start);
        }

        private static int Count(string text, string token)
        {
            var count = 0;
            var index = 0;
            while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }
            return count;
        }

        private static void ExpectThrows<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception(message);
        }

        private static string TempDirectory(string name)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-smoke-" + name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
