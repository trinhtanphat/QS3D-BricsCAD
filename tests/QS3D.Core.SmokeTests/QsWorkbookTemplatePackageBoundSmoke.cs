using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QsWorkbookTemplatePackageBoundSmoke
    {
        private const int OversizedMetadataPadding = 4 * 1024 * 1024;

        internal static void Run()
        {
            RejectsCompressionAmplifiedWorkbookMetadata();
            CanonicalTemplateStillExports();
        }

        private static void RejectsCompressionAmplifiedWorkbookMetadata()
        {
            var root = TempDirectory("qs-template-package-bound");
            try
            {
                var template = Path.Combine(root, "compressed-metadata-bomb.xlsx");
                var destination = Path.Combine(root, "existing.xlsx");
                WriteTemplate(template, OversizedMetadataPadding);
                File.WriteAllText(destination, "KEEP", Encoding.UTF8);
                var before = File.ReadAllBytes(destination);

                Require(new FileInfo(template).Length < 256 * 1024,
                    "Hostile template must remain small on disk so the regression exercises compressed XML amplification.");

                ExpectThrows<InvalidDataException>(
                    () => QsWorkbookTemplateExporter.Export(template, destination, Rows(), Definition()),
                    "Compressed oversized workbook XML must fail before template DOM materialization/export.");

                Require(BytesEqual(before, File.ReadAllBytes(destination)),
                    "Oversized template rejection must preserve the existing destination atomically.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void CanonicalTemplateStillExports()
        {
            var root = TempDirectory("qs-template-package-control");
            try
            {
                var template = Path.Combine(root, "canonical.xlsx");
                var destination = Path.Combine(root, "output.xlsx");
                WriteTemplate(template, 0);

                QsWorkbookTemplateExporter.Export(template, destination, Rows(), Definition());

                Require(File.Exists(destination) && new FileInfo(destination).Length > 0,
                    "Canonical bounded template must still export successfully.");
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
                2,
                new[] { new QsWorkbookTemplateMapping(QsWorkbookTemplateField.ElementName, "B") });
        }

        private static IReadOnlyList<QuantityReportRow> Rows()
        {
            var row = new QuantityReportRow
            {
                ElementName = "Wall A",
                Count = 1,
                DrawingFingerprint = "DWG-TEMPLATE-BOUND",
                HasGrossConcreteM3Evidence = false,
                HasDeductionM3Evidence = false,
                HasNetConcreteM3Evidence = false,
                HasFormworkM2Evidence = false,
                HasLengthMEvidence = false,
                HasOuterPerimeterMEvidence = false,
                HasInnerPerimeterMEvidence = false,
                HasDoorAreaM2Evidence = false,
                HasSideAreaM2Evidence = false,
                HasBottomAreaM2Evidence = false,
                HasTopAreaM2Evidence = false,
                HasOtherAreaM2Evidence = false
            };
            row.ElementIds.Add("E-TEMPLATE-BOUND");
            row.SourceHandles.Add("A1");
            return new[] { row };
        }

        private static void WriteTemplate(string path, int workbookPadding)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
            {
                WriteEntry(archive, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
                WriteEntry(archive, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                WriteWorkbookEntry(archive, workbookPadding);
                WriteEntry(archive, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
                WriteEntry(archive, "xl/worksheets/sheet1.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><dimension ref=\"A1:B2\"/><sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>BOQ</t></is></c></row><row r=\"2\"><c r=\"B2\" t=\"inlineStr\"><is><t>SAMPLE</t></is></c></row></sheetData></worksheet>");
            }
        }

        private static void WriteWorkbookEntry(ZipArchive archive, int padding)
        {
            var entry = archive.CreateEntry("xl/workbook.xml", CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"BOQ\" sheetId=\"1\" r:id=\"rId1\"/></sheets>");
                var block = new string(' ', 8192);
                var remaining = padding;
                while (remaining > 0)
                {
                    var count = Math.Min(block.Length, remaining);
                    writer.Write(block, 0, count);
                    remaining -= count;
                }
                writer.Write("</workbook>");
            }
        }

        private static void WriteEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                writer.Write(content);
        }

        private static string TempDirectory(string prefix)
        {
            var path = Path.Combine(Path.GetTempPath(), prefix + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch { }
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            for (var i = 0; i < left.Length; i++)
                if (left[i] != right[i]) return false;
            return true;
        }

        private static void ExpectThrows<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException(message);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
