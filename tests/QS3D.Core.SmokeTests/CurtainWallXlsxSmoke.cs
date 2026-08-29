using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallXlsxSmoke
    {
        public static void Run()
        {
            ValidWorkbookPreservesTraceAndNumericPayload();
            ZeroCountRowRemainsValid();
            CardinalityMismatchFailsBeforePublication();
            InvalidProvenanceFailsBeforeDirectoryCreation();
            InvalidSourceHandlePreservesExistingDestination();
            PresentationTextStillUsesOpenXmlSanitization();
            CountStableCrossRowProvenanceMutationFailsBeforePublication();
        }

        private static void ValidWorkbookPreservesTraceAndNumericPayload()
        {
            var path = TempPath("valid");
            try
            {
                var row = ValidRow();
                CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow> { row });
                if (!File.Exists(path)) throw new Exception("Curtain XLSX was not created.");
                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                    if (entry == null) throw new Exception("Curtain XLSX worksheet is missing.");
                    if (archive.GetEntry("xl/workbook.xml") == null) throw new Exception("Curtain XLSX workbook is missing.");
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        Contains(xml, "DT kính net", "header");
                        Contains(xml, "PROJECT-1", "Project ID provenance");
                        Contains(xml, "DRAWING-1", "drawing provenance");
                        Contains(xml, "CW-1;CW-2", "element provenance order");
                        Contains(xml, "AA;BB", "source-handle provenance order");

                        var document = new XmlDocument();
                        document.LoadXml(xml);
                        var namespaces = new XmlNamespaceManager(document.NameTable);
                        namespaces.AddNamespace("s", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                        var lengthNode = document.SelectSingleNode("/s:worksheet/s:sheetData/s:row[@r='2']/s:c[@r='D2']/s:v", namespaces);
                        if (lengthNode == null) throw new Exception("Curtain XLSX wall length cell is missing.");
                        var storedLength = double.Parse(lengthNode.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture);
                        if (storedLength != row.TotalWallLengthM)
                            throw new Exception("Curtain XLSX numeric payload did not round-trip the source double.");
                    }
                }
            }
            finally { SafeDelete(path); }
        }

        private static void ZeroCountRowRemainsValid()
        {
            var path = TempPath("zero");
            try
            {
                var row = ValidRow();
                row.WallCount = 0;
                row.ElementIds.Clear();
                row.SourceHandles.Clear();
                CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow> { row });
                if (!File.Exists(path)) throw new Exception("Zero-count Curtain XLSX row should remain valid.");
            }
            finally { SafeDelete(path); }
        }

        private static void CardinalityMismatchFailsBeforePublication()
        {
            var path = TempPath("count-mismatch");
            try
            {
                File.WriteAllText(path, "existing-workbook");
                var row = ValidRow();
                row.WallCount = 1;
                Throws<ArgumentException>(() => CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow> { row }));
                Equal("existing-workbook", File.ReadAllText(path), "count mismatch must preserve destination");
            }
            finally { SafeDelete(path); }
        }

        private static void InvalidProvenanceFailsBeforeDirectoryCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-curtain-invalid-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "nested", "curtain.xlsx");
            try
            {
                var row = ValidRow();
                row.ProjectId = "PROJECT" + new string(new[] { (char)1 });
                Throws<ArgumentException>(() => CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow> { row }));
                if (Directory.Exists(root)) throw new Exception("Invalid Curtain XLSX provenance must fail before destination directory creation.");
            }
            finally { SafeDeleteDirectory(root); }
        }

        private static void InvalidSourceHandlePreservesExistingDestination()
        {
            var path = TempPath("source-handle");
            try
            {
                File.WriteAllText(path, "existing-workbook");
                var row = ValidRow();
                row.SourceHandles[0] = "AA" + new string(new[] { (char)1 });
                Throws<ArgumentException>(() => CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow> { row }));
                Equal("existing-workbook", File.ReadAllText(path), "invalid source handle must preserve destination");
            }
            finally { SafeDelete(path); }
        }

        private static void PresentationTextStillUsesOpenXmlSanitization()
        {
            var path = TempPath("display-text");
            try
            {
                var row = ValidRow();
                row.FamilyName = "Curtain" + new string(new[] { (char)1 }) + "Family";
                CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow> { row });
                if (!File.Exists(path)) throw new Exception("Presentation-text sanitization compatibility regressed.");
            }
            finally { SafeDelete(path); }
        }

        private static void CountStableCrossRowProvenanceMutationFailsBeforePublication()
        {
            var path = TempPath("cross-row-provenance-mutation");
            try
            {
                File.WriteAllText(path, "existing-workbook");
                var first = ValidRow();
                var second = ValidRow();
                second.ProjectId = "PROJECT-2";
                var rows = new TraversalMutatingRows(first, second);
                Throws<InvalidOperationException>(() => CurtainWallXlsxExporter.Export(path, rows));
                Equal("existing-workbook", File.ReadAllText(path), "count-stable cross-row provenance mutation must preserve destination");
                if (rows.FirstReadCount != 1 || rows.SecondReadCount != 1)
                    throw new Exception("Curtain XLSX mutation regression must preserve the single-read outer-row contract.");
            }
            finally { SafeDelete(path); }
        }

        private static CurtainWallScheduleRow ValidRow()
        {
            var row = new CurtainWallScheduleRow
            {
                ProjectId = "PROJECT-1",
                DrawingFingerprint = "DRAWING-1",
                Floor = "Tầng 1",
                FamilyName = "Vách kính 12mm",
                WallCount = 2,
                TotalWallLengthM = 1e-9d,
                GrossWallAreaM2 = 27d,
                OpeningAreaM2 = 2d,
                NetGlassAreaM2 = 22.5d,
                FrameFaceAreaM2 = 2.5d,
                FrameLengthM = 51d,
                PanelCount = 12,
                VerticalFrameCount = 8,
                HorizontalFrameCount = 6,
                MinimumClearPanelWidthM = 1.3d,
                MaximumClearPanelWidthM = 1.45d,
                MinimumClearPanelHeightM = 1.35d,
                MaximumClearPanelHeightM = 1.45d
            };
            row.ElementIds.Add("CW-1");
            row.ElementIds.Add("CW-2");
            row.SourceHandles.Add("AA");
            row.SourceHandles.Add("BB");
            return row;
        }

        private static string TempPath(string label)
        {
            return Path.Combine(Path.GetTempPath(), "qs3d-curtain-" + label + "-" + Guid.NewGuid().ToString("N") + ".xlsx");
        }

        private static void Contains(string text, string expected, string label)
        {
            if (text.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Curtain XLSX missing " + label + ": " + expected + ".");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Curtain XLSX " + label + ". Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static void SafeDeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
        }

        private sealed class TraversalMutatingRows : IReadOnlyList<CurtainWallScheduleRow>
        {
            private readonly CurtainWallScheduleRow _first;
            private readonly CurtainWallScheduleRow _second;

            internal TraversalMutatingRows(CurtainWallScheduleRow first, CurtainWallScheduleRow second)
            {
                _first = first;
                _second = second;
            }

            public int Count => 2;
            public int FirstReadCount { get; private set; }
            public int SecondReadCount { get; private set; }

            public CurtainWallScheduleRow this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        FirstReadCount++;
                        if (FirstReadCount > 1) throw new InvalidOperationException("First caller-owned row index was read more than once.");
                        return _first;
                    }
                    if (index == 1)
                    {
                        SecondReadCount++;
                        if (SecondReadCount > 1) throw new InvalidOperationException("Second caller-owned row index was read more than once.");
                        _first.ElementIds[0] = "CW-MUTATED";
                        return _second;
                    }
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public IEnumerator<CurtainWallScheduleRow> GetEnumerator() =>
                throw new InvalidOperationException("Curtain XLSX exporter must not enumerate caller-owned rows during snapshot validation.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
