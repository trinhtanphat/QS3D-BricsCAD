using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallXlsxProvenanceVectorIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            RejectsShortSourceHandleVectorBeforePublication();
            RejectsLongSourceHandleVectorBeforePublication();
            MatchedProvenanceVectorExportsInPositionalOrder();
        }

        private static void RejectsShortSourceHandleVectorBeforePublication()
        {
            var path = TempPath("short");
            try
            {
                const string sentinel = "existing-short-workbook";
                File.WriteAllText(path, sentinel);
                var row = ValidRow();
                row.SourceHandles.RemoveAt(row.SourceHandles.Count - 1);

                ThrowsCardinalityMismatch(
                    () => CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow> { row }),
                    "short source-handle vector");
                Equal(sentinel, File.ReadAllText(path), "short source-handle vector must preserve destination bytes");
            }
            finally { SafeDelete(path); }
        }

        private static void RejectsLongSourceHandleVectorBeforePublication()
        {
            var path = TempPath("long");
            try
            {
                const string sentinel = "existing-long-workbook";
                File.WriteAllText(path, sentinel);
                var row = ValidRow();
                row.SourceHandles.Add("CC");

                ThrowsCardinalityMismatch(
                    () => CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow> { row }),
                    "long source-handle vector");
                Equal(sentinel, File.ReadAllText(path), "long source-handle vector must preserve destination bytes");
            }
            finally { SafeDelete(path); }
        }

        private static void MatchedProvenanceVectorExportsInPositionalOrder()
        {
            var path = TempPath("matched");
            try
            {
                var row = ValidRow();
                CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow> { row });
                if (!File.Exists(path))
                    throw new Exception("matched provenance vector must publish the workbook");

                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                    if (entry == null) throw new Exception("matched provenance vector worksheet is missing");
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        Contains(xml, "CW-1;CW-2", "matched provenance vector element order");
                        Contains(xml, "AA;BB", "matched provenance vector source-handle order");
                    }
                }
            }
            finally { SafeDelete(path); }
        }

        private static CurtainWallScheduleRow ValidRow()
        {
            var row = new CurtainWallScheduleRow
            {
                ProjectId = "PROJECT-PROVENANCE",
                DrawingFingerprint = "DRAWING-PROVENANCE",
                Floor = "Tầng 1",
                FamilyName = "Curtain provenance",
                WallCount = 2,
                TotalWallLengthM = 10d,
                GrossWallAreaM2 = 20d,
                OpeningAreaM2 = 1d,
                NetGlassAreaM2 = 17d,
                FrameFaceAreaM2 = 2d,
                FrameLengthM = 30d,
                PanelCount = 4,
                VerticalFrameCount = 3,
                HorizontalFrameCount = 2,
                MinimumClearPanelWidthM = 1d,
                MaximumClearPanelWidthM = 1.2d,
                MinimumClearPanelHeightM = 2d,
                MaximumClearPanelHeightM = 2.2d
            };
            row.ElementIds.Add("CW-1");
            row.ElementIds.Add("CW-2");
            row.SourceHandles.Add("AA");
            row.SourceHandles.Add("BB");
            return row;
        }

        private static void ThrowsCardinalityMismatch(Action action, string scenario)
        {
            try { action(); }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("Source Handles count must match Element IDs count", StringComparison.Ordinal) < 0)
                    throw new Exception("Curtain XLSX " + scenario + " produced the wrong diagnostic: " + ex.Message);
                return;
            }
            throw new Exception("Curtain XLSX " + scenario + " must fail closed before publication");
        }

        private static string TempPath(string label) =>
            Path.Combine(Path.GetTempPath(), "qs3d-curtain-provenance-vector-" + label + "-" + Guid.NewGuid().ToString("N") + ".xlsx");

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

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
