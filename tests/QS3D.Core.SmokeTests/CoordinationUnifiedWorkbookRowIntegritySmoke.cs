using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Coordination;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationUnifiedWorkbookRowIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            StableClashAndDuplicateRemainAccepted();
            UnrelatedInvalidClashRowFailsClosed();
            UnrelatedInvalidDuplicateRowFailsClosed();
            UnrelatedInvalidTraceRowFailsClosed();
            DuplicateSelectedClashRowFailsClosed();
            DuplicateSelectedDuplicateRowFailsClosed();
            DuplicateTraceHeaderFailsClosed();
            DuplicateTraceKeyFailsClosed();
        }

        private static void StableClashAndDuplicateRemainAccepted()
        {
            WithWorkbook(path =>
            {
                var clash = CoordinationUnifiedWorkbookTraceReader.ReadClash(path, 2);
                var duplicate = CoordinationUnifiedWorkbookTraceReader.ReadDuplicate(path, 2);
                Equal("A", clash.LeftHandle, "stable clash left handle");
                Equal("B", clash.RightHandle, "stable clash right handle");
                Equal("C", duplicate.LeftHandle, "stable duplicate left handle");
                Equal("D", duplicate.RightHandle, "stable duplicate right handle");
            });
        }

        private static void UnrelatedInvalidClashRowFailsClosed()
        {
            WithWorkbook(path =>
            {
                AppendWorksheetRow(path, "xl/worksheets/sheet1.xml", 1048577, "UNRELATED");
                Throws<InvalidDataException>(() => CoordinationUnifiedWorkbookTraceReader.ReadClash(path, 2), "out-of-range unrelated CLASHES row");
            });
        }

        private static void UnrelatedInvalidDuplicateRowFailsClosed()
        {
            WithWorkbook(path =>
            {
                AppendWorksheetRow(path, "xl/worksheets/sheet2.xml", 1048577, "UNRELATED");
                Throws<InvalidDataException>(() => CoordinationUnifiedWorkbookTraceReader.ReadDuplicate(path, 2), "out-of-range unrelated DUPLICATES row");
            });
        }

        private static void UnrelatedInvalidTraceRowFailsClosed()
        {
            WithWorkbook(path =>
            {
                AppendWorksheetRow(path, "xl/worksheets/sheet3.xml", 1048577, "UNRELATED");
                Throws<InvalidDataException>(() => CoordinationUnifiedWorkbookTraceReader.ReadClash(path, 2), "out-of-range unrelated TRACE_MODEL row");
            });
        }

        private static void DuplicateSelectedClashRowFailsClosed()
        {
            WithWorkbook(path =>
            {
                AppendWorksheetRow(path, "xl/worksheets/sheet1.xml", 2, "DUPLICATE");
                Throws<InvalidDataException>(() => CoordinationUnifiedWorkbookTraceReader.ReadClash(path, 2), "duplicate selected CLASHES row");
            });
        }

        private static void DuplicateSelectedDuplicateRowFailsClosed()
        {
            WithWorkbook(path =>
            {
                AppendWorksheetRow(path, "xl/worksheets/sheet2.xml", 2, "DUPLICATE");
                Throws<InvalidDataException>(() => CoordinationUnifiedWorkbookTraceReader.ReadDuplicate(path, 2), "duplicate selected DUPLICATES row");
            });
        }

        private static void DuplicateTraceHeaderFailsClosed()
        {
            WithWorkbook(path =>
            {
                AppendWorksheetRow(path, "xl/worksheets/sheet3.xml", 1, "TRACE_KEY");
                Throws<InvalidDataException>(() => CoordinationUnifiedWorkbookTraceReader.ReadClash(path, 2), "duplicate TRACE_MODEL header");
            });
        }

        private static void DuplicateTraceKeyFailsClosed()
        {
            WithWorkbook(path =>
            {
                DuplicateFirstTraceDataRow(path);
                Throws<InvalidDataException>(() => CoordinationUnifiedWorkbookTraceReader.ReadClash(path, 2), "duplicate matching TRACE_KEY");
            });
        }

        private static void WithWorkbook(Action<string> assertion)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-coordination-unified-row-integrity-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var clash = CoordinationClashExportRow.CreateExactHard(
                    "coord-unified-row-integrity", "000A", "000B", "CL-A", "CL-B", "Pipe", "Beam", "L01");
                var duplicate = CoordinationDuplicateExportRow.Create(
                    "coord-unified-row-integrity", "EL-A", "000C", "EL-B", "000D",
                    DuplicateMatchKind.ExactGeometry, "Column", "Column", "L01");
                CoordinationUnifiedWorkbookExporter.Export(path, new[] { clash }, new[] { duplicate });
                assertion(path);
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void AppendWorksheetRow(string path, string entryName, int rowNumber, string text)
        {
            RewriteEntry(path, entryName, xml =>
            {
                const string marker = "</sheetData>";
                var index = xml.IndexOf(marker, StringComparison.Ordinal);
                if (index < 0) throw new InvalidOperationException("Worksheet sheetData terminator is missing.");
                var hostile = "<row r=\"" + rowNumber + "\"><c r=\"A" + rowNumber + "\" t=\"inlineStr\"><is><t>" + text + "</t></is></c></row>";
                return xml.Insert(index, hostile);
            });
        }

        private static void DuplicateFirstTraceDataRow(string path)
        {
            RewriteEntry(path, "xl/worksheets/sheet3.xml", xml =>
            {
                const string rowStart = "<row r=\"2\">";
                var start = xml.IndexOf(rowStart, StringComparison.Ordinal);
                if (start < 0) throw new InvalidOperationException("TRACE_MODEL row 2 is missing.");
                var end = xml.IndexOf("</row>", start, StringComparison.Ordinal);
                if (end < 0) throw new InvalidOperationException("TRACE_MODEL row 2 terminator is missing.");
                end += "</row>".Length;
                var duplicate = xml.Substring(start, end - start).Replace("r=\"2\"", "r=\"4\"");
                return xml.Insert(end, duplicate);
            });
        }

        private static void RewriteEntry(string path, string entryName, Func<string, string> rewrite)
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry(entryName) ?? throw new InvalidOperationException("Missing worksheet entry " + entryName + ".");
                string xml;
                using (var stream = entry.Open())
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
                    xml = reader.ReadToEnd();
                entry.Delete();
                var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using (var stream = replacement.Open())
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    writer.Write(rewrite(xml));
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
            throw new InvalidOperationException("CoordinationUnifiedWorkbookRowIntegritySmoke: expected " + typeof(T).Name + " for " + label + ".");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "CoordinationUnifiedWorkbookRowIntegritySmoke: " + label + " expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
