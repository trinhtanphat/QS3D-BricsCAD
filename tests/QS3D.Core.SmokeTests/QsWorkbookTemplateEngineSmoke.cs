using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QsWorkbookTemplateEngineSmoke
    {
        private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        internal static void Run()
        {
            RendersCanonicalRowsAndPreservesTemplateParts();
            PreservesDestinationOnInvalidMapping();
            RejectsMappedFormulaCells();
            RejectsUnsafeExpansionPastFooter();
        }

        private static void RendersCanonicalRowsAndPreservesTemplateParts()
        {
            var root = TempDirectory("qs-template-render");
            try
            {
                var template = Path.Combine(root, "company-template.xlsx");
                var output = Path.Combine(root, "company-output.xlsx");
                WriteTemplate(template, false, false);
                var definition = Definition();

                QsWorkbookTemplateExporter.Export(template, output, Rows(), definition);

                Require(File.Exists(output), "Template exporter did not create the destination workbook.");
                Require(ReadEntry(template, "xl/worksheets/sheet1.xml").Contains("SAMPLE"),
                    "Template exporter must not mutate the source template.");

                var worksheet = XDocument.Parse(ReadEntry(output, "xl/worksheets/sheet1.xml"));
                var merge = worksheet.Descendants(Ns + "mergeCell").Single();
                Require((string)merge.Attribute("ref") == "A1:D1", "Unrelated merged cells must be preserved.");
                var column = worksheet.Descendants(Ns + "col").Single();
                Require((string)column.Attribute("width") == "18", "Template column widths must be preserved.");
                var formula = worksheet.Descendants(Ns + "c")
                    .Single(cell => (string)cell.Attribute("r") == "J2")
                    .Element(Ns + "f");
                Require(formula != null && formula.Value == "SUM(1,1)", "Unmapped formulas outside the data row must be preserved.");

                RequireCellText(worksheet, "B3", "Wall A", "First mapped text value was not written.");
                RequireCellText(worksheet, "B4", "Beam B", "Expandable template row was not cloned for the second quantity row.");
                RequireCellNumber(worksheet, "C3", "2.4", "Mapped numeric evidence must remain numeric.");
                RequireCellNumber(worksheet, "C4", "1.5", "Second mapped numeric evidence must remain numeric.");
                RequireCellNumber(worksheet, "H3", "12.5", "Formwork evidence was not projected into the company template.");
                RequireStyle(worksheet, "B3", "5", "Mapped cells must preserve the template data-row style.");
                RequireStyle(worksheet, "B4", "5", "Cloned template rows must preserve the selected data-row style.");

                var trace = QsWorkbookTemplateTraceReader.Read(output, definition, 4);
                Require(trace.DrawingFingerprint == "DWG-TEMPLATE-01", "Generic template trace lost drawing fingerprint.");
                Require(trace.ElementIds.Count == 1 && trace.ElementIds[0] == "E2", "Generic template trace lost semantic Element ID.");
                Require(trace.Handles.Count == 1 && trace.Handles[0] == "8000000000000000", "Generic template trace lost unsigned CAD Handle.");
                Require(trace.TraceKey.StartsWith("QTPL1:", StringComparison.Ordinal), "Generic template trace key version is missing.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void PreservesDestinationOnInvalidMapping()
        {
            var root = TempDirectory("qs-template-atomic");
            try
            {
                var template = Path.Combine(root, "template.xlsx");
                var destination = Path.Combine(root, "existing.xlsx");
                WriteTemplate(template, false, false);
                File.WriteAllText(destination, "KEEP-ME", Encoding.UTF8);
                var original = File.ReadAllBytes(destination);

                ExpectThrows<ArgumentException>(() => new QsWorkbookTemplateDefinition(
                    "BOQ", 3,
                    new[]
                    {
                        new QsWorkbookTemplateMapping(QsWorkbookTemplateField.ElementName, "B"),
                        new QsWorkbookTemplateMapping(QsWorkbookTemplateField.NetConcreteM3, "B")
                    }), "Duplicate template columns must fail closed.");

                Require(original.SequenceEqual(File.ReadAllBytes(destination)),
                    "Invalid template mapping must not replace an existing destination workbook.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void RejectsMappedFormulaCells()
        {
            var root = TempDirectory("qs-template-formula-map");
            try
            {
                var template = Path.Combine(root, "formula-template.xlsx");
                var destination = Path.Combine(root, "output.xlsx");
                WriteTemplate(template, true, false);
                File.WriteAllText(destination, "UNCHANGED", Encoding.UTF8);
                var original = File.ReadAllBytes(destination);

                ExpectThrows<InvalidDataException>(() => QsWorkbookTemplateExporter.Export(template, destination, Rows(), Definition()),
                    "Mapped template formula cells must be rejected before destination replacement.");
                Require(original.SequenceEqual(File.ReadAllBytes(destination)),
                    "Mapped-formula rejection must preserve an existing destination workbook.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void RejectsUnsafeExpansionPastFooter()
        {
            var root = TempDirectory("qs-template-footer");
            try
            {
                var template = Path.Combine(root, "footer-template.xlsx");
                var destination = Path.Combine(root, "output.xlsx");
                WriteTemplate(template, false, true);
                ExpectThrows<InvalidDataException>(() => QsWorkbookTemplateExporter.Export(template, destination, Rows(), Definition()),
                    "Dynamic row expansion must fail closed when a footer exists below the one-row reserved data block.");
                Require(!File.Exists(destination), "Unsafe expansion must not create the destination workbook.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static QsWorkbookTemplateDefinition Definition()
        {
            return new QsWorkbookTemplateDefinition(
                "BOQ",
                3,
                new[]
                {
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.Index, "A"),
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.ElementName, "B"),
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.NetConcreteM3, "C"),
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.TraceKey, "D"),
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.ElementIds, "E"),
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.SourceHandles, "F"),
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.DrawingFingerprint, "G"),
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.FormworkM2, "H")
                });
        }

        private static IReadOnlyList<QuantityReportRow> Rows()
        {
            var first = new QuantityReportRow
            {
                Floor = "Tầng 1",
                Category = "StructuralWall",
                ElementName = "Wall A",
                Count = 1,
                DrawingFingerprint = "DWG-TEMPLATE-01",
                NetConcreteM3 = 2.4,
                HasNetConcreteM3Evidence = true,
                FormworkM2 = 12.5,
                HasFormworkM2Evidence = true,
                HasGrossConcreteM3Evidence = false,
                HasDeductionM3Evidence = false,
                HasLengthMEvidence = false,
                HasOuterPerimeterMEvidence = false,
                HasInnerPerimeterMEvidence = false,
                HasDoorAreaM2Evidence = false,
                HasSideAreaM2Evidence = false,
                HasBottomAreaM2Evidence = false,
                HasTopAreaM2Evidence = false,
                HasOtherAreaM2Evidence = false
            };
            first.ElementIds.Add("E1");
            first.SourceHandles.Add("A1");

            var second = new QuantityReportRow
            {
                Floor = "Tầng 2",
                Category = "Beam",
                ElementName = "Beam B",
                Count = 1,
                DrawingFingerprint = "DWG-TEMPLATE-01",
                NetConcreteM3 = 1.5,
                HasNetConcreteM3Evidence = true,
                FormworkM2 = 9.75,
                HasFormworkM2Evidence = true,
                HasGrossConcreteM3Evidence = false,
                HasDeductionM3Evidence = false,
                HasLengthMEvidence = false,
                HasOuterPerimeterMEvidence = false,
                HasInnerPerimeterMEvidence = false,
                HasDoorAreaM2Evidence = false,
                HasSideAreaM2Evidence = false,
                HasBottomAreaM2Evidence = false,
                HasTopAreaM2Evidence = false,
                HasOtherAreaM2Evidence = false
            };
            second.ElementIds.Add("E2");
            second.SourceHandles.Add("8000000000000000");
            return new[] { first, second };
        }

        private static void WriteTemplate(string path, bool mappedFormula, bool footer)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
            {
                WriteEntry(archive, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
                WriteEntry(archive, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                WriteEntry(archive, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"BOQ\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                WriteEntry(archive, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
                WriteEntry(archive, "xl/styles.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"6\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"4\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs></styleSheet>");

                var formulaCell = mappedFormula
                    ? "<c r=\"B3\" s=\"5\"><f>1+1</f><v>2</v></c>"
                    : "<c r=\"B3\" s=\"5\" t=\"inlineStr\"><is><t>SAMPLE</t></is></c>";
                var footerRow = footer ? "<row r=\"5\"><c r=\"A5\" t=\"inlineStr\"><is><t>FOOTER</t></is></c></row>" : string.Empty;
                WriteEntry(archive, "xl/worksheets/sheet1.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><dimension ref=\"A1:J" + (footer ? "5" : "3") + "\"/><cols><col min=\"2\" max=\"2\" width=\"18\" customWidth=\"1\"/></cols><sheetData>" +
                    "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>COMPANY BOQ</t></is></c></row>" +
                    "<row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>STT</t></is></c><c r=\"B2\" t=\"inlineStr\"><is><t>Cấu kiện</t></is></c><c r=\"J2\"><f>SUM(1,1)</f><v>2</v></c></row>" +
                    "<row r=\"3\"><c r=\"A3\" s=\"5\"><v>0</v></c>" + formulaCell + "<c r=\"C3\" s=\"5\"><v>0</v></c><c r=\"D3\" s=\"5\"/><c r=\"E3\" s=\"5\"/><c r=\"F3\" s=\"5\"/><c r=\"G3\" s=\"5\"/><c r=\"H3\" s=\"5\"/></row>" + footerRow + "</sheetData><mergeCells count=\"1\"><mergeCell ref=\"A1:D1\"/></mergeCells></worksheet>");
            }
        }

        private static void RequireCellText(XDocument document, string reference, string expected, string message)
        {
            var cell = document.Descendants(Ns + "c").Single(item => (string)item.Attribute("r") == reference);
            var actual = string.Concat(cell.Descendants(Ns + "t").Select(item => item.Value));
            Require(actual == expected, message + " Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void RequireCellNumber(XDocument document, string reference, string expected, string message)
        {
            var cell = document.Descendants(Ns + "c").Single(item => (string)item.Attribute("r") == reference);
            Require((string)cell.Attribute("t") == null, message + " Numeric cell must not use a string type.");
            var actual = (string)cell.Element(Ns + "v");
            Require(actual == expected, message + " Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void RequireStyle(XDocument document, string reference, string expected, string message)
        {
            var cell = document.Descendants(Ns + "c").Single(item => (string)item.Attribute("r") == reference);
            Require((string)cell.Attribute("s") == expected, message);
        }

        private static string ReadEntry(string path, string entryName)
        {
            using (var stream = File.OpenRead(path))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry(entryName) ?? throw new Exception("Missing XLSX entry: " + entryName);
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8)) return reader.ReadToEnd();
            }
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private static string TempDirectory(string name)
        {
            var path = Path.Combine(Path.GetTempPath(), name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void ExpectThrows<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception(message);
        }
    }
}
