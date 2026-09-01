using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationWorkbookRowIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            StableLookupRemainsAccepted();
            UnrelatedInvalidClashRowFailsClosed();
            UnrelatedInvalidTraceRowFailsClosed();
        }

        private static void StableLookupRemainsAccepted()
        {
            WithWorkbook(path =>
            {
                var trace = CoordinationWorkbookTraceReader.Read(path, 2);
                Equal("A", trace.LeftHandle, "stable left handle");
                Equal("B", trace.RightHandle, "stable right handle");
                Equal("coord-row-integrity", trace.DrawingFingerprint, "stable drawing fingerprint");
            });
        }

        private static void UnrelatedInvalidClashRowFailsClosed()
        {
            WithWorkbook(path =>
            {
                AppendWorksheetRow(path, "xl/worksheets/sheet1.xml", 1048577);
                Throws<InvalidDataException>(() => CoordinationWorkbookTraceReader.Read(path, 2), "out-of-range unrelated CLASHES row");
            });
        }

        private static void UnrelatedInvalidTraceRowFailsClosed()
        {
            WithWorkbook(path =>
            {
                AppendWorksheetRow(path, "xl/worksheets/sheet2.xml", 1048577);
                Throws<InvalidDataException>(() => CoordinationWorkbookTraceReader.Read(path, 2), "out-of-range unrelated TRACE_MODEL row");
            });
        }

        private static void WithWorkbook(Action<string> assertion)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-coordination-row-integrity-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var row = CoordinationClashExportRow.CreateExactHard(
                    "coord-row-integrity",
                    "000A",
                    "000B",
                    "ELEMENT-A",
                    "ELEMENT-B",
                    "Pipe",
                    "Beam",
                    "L01");
                CoordinationWorkbookExporter.Export(path, new[] { row });
                assertion(path);
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void AppendWorksheetRow(string path, string entryName, int rowNumber)
        {
            string xml;
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry(entryName) ?? throw new InvalidOperationException("Missing worksheet entry " + entryName + ".");
                using (var stream = entry.Open())
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
                    xml = reader.ReadToEnd();

                const string marker = "</sheetData>";
                var index = xml.IndexOf(marker, StringComparison.Ordinal);
                if (index < 0) throw new InvalidOperationException("Worksheet sheetData terminator is missing.");
                var hostile = "<row r=\"" + rowNumber + "\"><c r=\"A" + rowNumber + "\" t=\"inlineStr\"><is><t>UNRELATED</t></is></c></row>";
                xml = xml.Insert(index, hostile);

                entry.Delete();
                var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using (var stream = replacement.Open())
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    writer.Write(xml);
            }
        }

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("CoordinationWorkbookRowIntegritySmoke: expected " + typeof(T).Name + " for " + label + ".");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "CoordinationWorkbookRowIntegritySmoke: " + label + " expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
