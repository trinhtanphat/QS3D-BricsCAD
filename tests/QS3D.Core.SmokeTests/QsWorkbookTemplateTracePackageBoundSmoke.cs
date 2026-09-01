using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class QsWorkbookTemplateTracePackageBoundSmoke
    {
        internal static void Run()
        {
            OversizedWorkbookFailsAtPackageAdmission();
            CanonicalWorkbookStillReadsTrace();
        }

        private static void OversizedWorkbookFailsAtPackageAdmission()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-template-trace-bound-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var path = Path.Combine(root, "oversized.xlsx");
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.WriteByte(0x50);
                    stream.WriteByte(0x4B);
                    stream.SetLength(128L * 1024L * 1024L + 1L);
                }

                var definition = CreateDefinition();
                var rejectedAtAdmission = false;
                try
                {
                    QsWorkbookTemplateTraceReader.Read(path, definition, 2);
                }
                catch (InvalidDataException ex)
                {
                    rejectedAtAdmission = string.Equals(
                        ex.Message,
                        "XLSX template workbook is too large for bounded processing.",
                        StringComparison.Ordinal);
                }

                if (!rejectedAtAdmission)
                    throw new Exception("Oversized template trace workbook must fail at the shared package-size admission before ZIP parsing.");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void CanonicalWorkbookStillReadsTrace()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-template-trace-control-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                const string fingerprint = "DRAW-001";
                const string elementId = "E-001";
                const string handle = "1A";
                var traceKey = ComputeTraceKey(fingerprint, elementId, handle);
                var path = Path.Combine(root, "canonical.xlsx");
                CreateCanonicalWorkbook(path, fingerprint, elementId, handle, traceKey);

                var trace = QsWorkbookTemplateTraceReader.Read(path, CreateDefinition(), 2);
                if (!string.Equals(trace.DrawingFingerprint, fingerprint, StringComparison.Ordinal))
                    throw new Exception("Canonical template trace control changed drawing fingerprint.");
                if (trace.ElementIds.Count != 1 || !string.Equals(trace.ElementIds[0], elementId, StringComparison.Ordinal))
                    throw new Exception("Canonical template trace control changed element provenance.");
                if (trace.Handles.Count != 1 || !string.Equals(trace.Handles[0], handle, StringComparison.Ordinal))
                    throw new Exception("Canonical template trace control changed CAD handle provenance.");
                if (!string.Equals(trace.TraceKey, traceKey, StringComparison.Ordinal))
                    throw new Exception("Canonical template trace control changed the trace key.");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static QsWorkbookTemplateDefinition CreateDefinition()
        {
            return new QsWorkbookTemplateDefinition(
                "QTO",
                2,
                new List<QsWorkbookTemplateMapping>
                {
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.DrawingFingerprint, "A"),
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.ElementIds, "B"),
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.SourceHandles, "C"),
                    new QsWorkbookTemplateMapping(QsWorkbookTemplateField.TraceKey, "D")
                });
        }

        private static void CreateCanonicalWorkbook(string path, string fingerprint, string elementId, string handle, string traceKey)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
            {
                WriteEntry(
                    archive,
                    "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"QTO\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                WriteEntry(
                    archive,
                    "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
                WriteEntry(
                    archive,
                    "xl/worksheets/sheet1.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"2\">" +
                    InlineCell("A2", fingerprint) + InlineCell("B2", elementId) + InlineCell("C2", handle) + InlineCell("D2", traceKey) +
                    "</row></sheetData></worksheet>");
            }
        }

        private static string InlineCell(string reference, string value)
        {
            return "<c r=\"" + reference + "\" t=\"inlineStr\"><is><t>" + value + "</t></is></c>";
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                writer.Write(content);
        }

        private static string ComputeTraceKey(string fingerprint, string elementId, string handle)
        {
            var payload = "QTPL1\n" + fingerprint + "\n" + elementId + "\n" + handle;
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var builder = new StringBuilder("QTPL1:");
                foreach (var value in bytes) builder.Append(value.ToString("X2"));
                return builder.ToString();
            }
        }
    }
}