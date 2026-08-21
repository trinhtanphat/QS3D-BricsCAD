using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
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
            CustomerTraceReaderSupportsSharedStringResave();
            CustomerTraceReaderSupportsRichSharedStrings();
            CustomerTraceReaderRejectsMissingSharedStringsPart();
            CustomerTraceReaderRejectsInvalidSharedStringIndex();
            CustomerTraceReaderRejectsDuplicateSharedStringsPart();
            CustomerTraceReaderRejectsUnsupportedSheet();
            CustomerTraceReaderRejectsTamperedTraceIdentity();
            CustomerWorkbookRejectsMalformedProvenance();
            CustomerWorkbookRejectsOversizedTraceCell();
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

                var highBit = QsCustomerWorkbookTraceReader.Read(path, "CHI_TIET", 3);
                Require(highBit.ElementIds.Count == 1 && highBit.ElementIds[0] == "E2", "CHI_TIET high-bit trace lost its semantic element.");
                Require(highBit.Handles.Count == 1 && highBit.Handles[0] == "8000000000000000",
                    "Customer workbook must preserve unsigned 64-bit CAD Handles.");
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

                RequireCellValue(detail, "H2", "0", "Measured zero gross quantity must remain numeric zero.");
                RequireMissingCell(detail, "I2", "Unsupported deduction must remain blank rather than fabricated zero.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void CustomerTraceReaderSupportsSharedStringResave()
        {
            var root = TempDirectory("customer-workbook-shared-strings");
            try
            {
                var path = Path.Combine(root, "qs-customer.xlsx");
                QsCustomerWorkbookExporter.Export(path, Details(), Summary());
                ConvertInlineStringsToSharedStrings(path);

                var aggregate = QsCustomerWorkbookTraceReader.Read(path, "DGKL", 2);
                Require(aggregate.ElementIds.Count == 2 && aggregate.Handles.Count == 2,
                    "Shared-string DGKL resave must preserve aggregate trace identity.");
                var detail = QsCustomerWorkbookTraceReader.Read(path, "CHI_TIET", 3);
                Require(detail.ElementIds.Count == 1 && detail.ElementIds[0] == "E2",
                    "Shared-string CHI_TIET resave must preserve semantic element identity.");
                Require(detail.Handles.Count == 1 && detail.Handles[0] == "8000000000000000",
                    "Shared-string CHI_TIET resave must preserve unsigned CAD Handle identity.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void CustomerTraceReaderSupportsRichSharedStrings()
        {
            var root = TempDirectory("customer-workbook-rich-shared-strings");
            try
            {
                var path = Path.Combine(root, "qs-customer.xlsx");
                QsCustomerWorkbookExporter.Export(path, Details(), Summary());
                var indexes = ConvertInlineStringsToSharedStrings(path);
                int traceHeaderIndex;
                Require(indexes.TryGetValue(QsCustomerWorkbookExporter.TraceHeader, out traceHeaderIndex),
                    "Shared-string fixture did not include TRACE_KEY.");

                MutateSharedStrings(path, document =>
                {
                    XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                    var items = document.Root!.Elements(ns + "si").ToList();
                    var item = items[traceHeaderIndex];
                    item.RemoveNodes();
                    item.Add(new XElement(ns + "r", new XElement(ns + "t", "TRACE_")));
                    item.Add(new XElement(ns + "r", new XElement(ns + "t", "KEY")));
                });

                var trace = QsCustomerWorkbookTraceReader.Read(path, "DGKL", 2);
                Require(trace.ElementIds.Count == 2,
                    "Rich-text shared-string runs must concatenate to the original TRACE_KEY header.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void CustomerTraceReaderRejectsMissingSharedStringsPart()
        {
            var root = TempDirectory("customer-workbook-missing-shared-strings");
            try
            {
                var path = Path.Combine(root, "qs-customer.xlsx");
                QsCustomerWorkbookExporter.Export(path, Details(), Summary());
                ConvertInlineStringsToSharedStrings(path);
                using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
                {
                    var entry = archive.GetEntry("xl/sharedStrings.xml") ?? throw new Exception("Missing shared-string fixture part.");
                    entry.Delete();
                }
                ExpectThrows<InvalidDataException>(() => QsCustomerWorkbookTraceReader.Read(path, "DGKL", 2),
                    "Reader must reject shared-string cells when sharedStrings.xml is missing.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void CustomerTraceReaderRejectsInvalidSharedStringIndex()
        {
            var root = TempDirectory("customer-workbook-invalid-shared-index");
            try
            {
                var negativePath = Path.Combine(root, "negative.xlsx");
                QsCustomerWorkbookExporter.Export(negativePath, Details(), Summary());
                ConvertInlineStringsToSharedStrings(negativePath);
                MutateWorksheet(negativePath, "xl/worksheets/sheet1.xml", document =>
                {
                    XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                    var cell = document.Descendants(ns + "c")
                        .First(item => string.Equals(item.Attribute("t")?.Value, "s", StringComparison.Ordinal));
                    cell.Element(ns + "v")!.Value = "-1";
                });
                ExpectThrows<InvalidDataException>(() => QsCustomerWorkbookTraceReader.Read(negativePath, "DGKL", 2),
                    "Reader must reject negative shared-string indices.");

                var outOfRangePath = Path.Combine(root, "out-of-range.xlsx");
                QsCustomerWorkbookExporter.Export(outOfRangePath, Details(), Summary());
                ConvertInlineStringsToSharedStrings(outOfRangePath);
                MutateWorksheet(outOfRangePath, "xl/worksheets/sheet1.xml", document =>
                {
                    XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                    var cell = document.Descendants(ns + "c")
                        .First(item => string.Equals(item.Attribute("t")?.Value, "s", StringComparison.Ordinal));
                    cell.Element(ns + "v")!.Value = "2147483647";
                });
                ExpectThrows<InvalidDataException>(() => QsCustomerWorkbookTraceReader.Read(outOfRangePath, "DGKL", 2),
                    "Reader must reject out-of-range shared-string indices.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void CustomerTraceReaderRejectsDuplicateSharedStringsPart()
        {
            var root = TempDirectory("customer-workbook-duplicate-shared-strings");
            try
            {
                var path = Path.Combine(root, "qs-customer.xlsx");
                QsCustomerWorkbookExporter.Export(path, Details(), Summary());
                ConvertInlineStringsToSharedStrings(path);
                using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
                {
                    var duplicate = archive.CreateEntry("xl/sharedStrings.xml", CompressionLevel.Optimal);
                    using (var writer = new StreamWriter(duplicate.Open(), new UTF8Encoding(false)))
                        writer.Write("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><si><t>duplicate</t></si></sst>");
                }
                ExpectThrows<InvalidDataException>(() => QsCustomerWorkbookTraceReader.Read(path, "DGKL", 2),
                    "Reader must reject duplicate sharedStrings.xml package parts.");
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

        private static void CustomerTraceReaderRejectsTamperedTraceIdentity()
        {
            var root = TempDirectory("customer-workbook-trace-tamper");
            try
            {
                var path = Path.Combine(root, "qs-customer.xlsx");
                QsCustomerWorkbookExporter.Export(path, Details(), Summary());
                const string traceEntry = "xl/worksheets/sheet4.xml";
                var original = ReadEntry(path, traceEntry);
                var tampered = original.Replace("DWG-CUSTOMER-TRACE", "DWG-CUSTOMER-TAMPERED");
                Require(!string.Equals(original, tampered, StringComparison.Ordinal), "TRACE_MODEL tamper fixture did not modify provenance.");
                ReplaceEntry(path, traceEntry, tampered);

                ExpectThrows<InvalidDataException>(
                    () => QsCustomerWorkbookTraceReader.Read(path, "DGKL", 2),
                    "Customer trace reader must recompute TRACE_KEY and reject tampered TRACE_MODEL provenance.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void CustomerWorkbookRejectsMalformedProvenance()
        {
            var root = TempDirectory("customer-workbook-provenance");
            try
            {
                var paddedDetails = Details();
                paddedDetails[0].ElementIds.Clear();
                paddedDetails[0].ElementIds.Add(" E1 ");
                ExpectThrows<InvalidDataException>(
                    () => QsCustomerWorkbookExporter.Export(Path.Combine(root, "padded.xlsx"), paddedDetails, Summary()),
                    "Customer workbook must reject padded semantic identities instead of silently normalizing them.");

                var badSummary = Summary();
                badSummary[0].Count = 1;
                ExpectThrows<InvalidDataException>(
                    () => QsCustomerWorkbookExporter.Export(Path.Combine(root, "count-mismatch.xlsx"), Details(), badSummary),
                    "Customer workbook must bind grouped Count to Element ID provenance cardinality.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void CustomerWorkbookRejectsOversizedTraceCell()
        {
            var root = TempDirectory("customer-workbook-cell-limit");
            try
            {
                var firstId = new string('A', 20000);
                var secondId = new string('B', 20000);
                var details = new[]
                {
                    NewRow(firstId, "A1", 1d, 1d),
                    NewRow(secondId, "A2", 1d, 1d)
                };
                var summary = Summary()[0];
                summary.ElementIds.Clear();
                summary.ElementIds.Add(firstId);
                summary.ElementIds.Add(secondId);
                summary.SourceHandles.Clear();
                summary.SourceHandles.Add("A1");
                summary.SourceHandles.Add("A2");
                var path = Path.Combine(root, "oversized-trace.xlsx");

                ExpectThrows<InvalidDataException>(
                    () => QsCustomerWorkbookExporter.Export(path, details, new[] { summary }),
                    "Customer workbook must reject TRACE_MODEL text cells beyond Excel's 32,767-character limit.");
                Require(!File.Exists(path), "Oversized customer workbook must fail before output file commit.");
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
            var second = NewRow("E2", "8000000000000000", 2d, 18d);
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
            row.SourceHandles.Add("8000000000000000");
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

        private static Dictionary<string, int> ConvertInlineStringsToSharedStrings(string path)
        {
            var strings = new List<string>();
            var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var worksheetNames = archive.Entries
                    .Where(entry => entry.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal) && entry.FullName.EndsWith(".xml", StringComparison.Ordinal))
                    .Select(entry => entry.FullName)
                    .ToList();
                foreach (var worksheetName in worksheetNames)
                {
                    var entry = archive.GetEntry(worksheetName) ?? throw new Exception("Missing worksheet fixture part: " + worksheetName + ".");
                    XDocument document;
                    using (var stream = entry.Open()) document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
                    foreach (var cell in document.Descendants(ns + "c").Where(item => string.Equals(item.Attribute("t")?.Value, "inlineStr", StringComparison.Ordinal)).ToList())
                    {
                        var value = string.Concat(cell.Descendants(ns + "t").Select(text => text.Value));
                        int index;
                        if (!indexes.TryGetValue(value, out index))
                        {
                            index = strings.Count;
                            strings.Add(value);
                            indexes.Add(value, index);
                        }
                        cell.Elements(ns + "is").Remove();
                        cell.SetAttributeValue("t", "s");
                        cell.Add(new XElement(ns + "v", index));
                    }
                    entry.Delete();
                    var replacement = archive.CreateEntry(worksheetName, CompressionLevel.Optimal);
                    using (var writer = new StreamWriter(replacement.Open(), new UTF8Encoding(false)))
                        document.Save(writer, SaveOptions.DisableFormatting);
                }

                var shared = archive.CreateEntry("xl/sharedStrings.xml", CompressionLevel.Optimal);
                var sst = new XDocument(new XElement(ns + "sst",
                    new XAttribute("count", strings.Count),
                    new XAttribute("uniqueCount", strings.Count),
                    strings.Select(value => new XElement(ns + "si", new XElement(ns + "t", value)))));
                using (var writer = new StreamWriter(shared.Open(), new UTF8Encoding(false)))
                    sst.Save(writer, SaveOptions.DisableFormatting);
            }
            return indexes;
        }

        private static void MutateSharedStrings(string path, Action<XDocument> mutation)
        {
            MutateXmlEntry(path, "xl/sharedStrings.xml", mutation);
        }

        private static void MutateWorksheet(string path, string entryName, Action<XDocument> mutation)
        {
            MutateXmlEntry(path, entryName, mutation);
        }

        private static void MutateXmlEntry(string path, string entryName, Action<XDocument> mutation)
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry(entryName) ?? throw new Exception("Missing XLSX entry: " + entryName + ".");
                XDocument document;
                using (var stream = entry.Open()) document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
                mutation(document);
                entry.Delete();
                var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using (var writer = new StreamWriter(replacement.Open(), new UTF8Encoding(false)))
                    document.Save(writer, SaveOptions.DisableFormatting);
            }
        }

        private static string ReadEntry(string path, string entryName)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                var entry = archive.GetEntry(entryName) ?? throw new Exception("Missing XLSX entry: " + entryName + ".");
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8)) return reader.ReadToEnd();
            }
        }

        private static void ReplaceEntry(string path, string entryName, string content)
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry(entryName) ?? throw new Exception("Missing XLSX entry: " + entryName + ".");
                entry.Delete();
                var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using (var writer = new StreamWriter(replacement.Open(), new UTF8Encoding(false))) writer.Write(content);
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

        private static string? CellXml(string sheet, string cellRef)
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
