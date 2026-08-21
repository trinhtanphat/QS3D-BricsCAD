using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallXlsxProvenanceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AssertProvenanceIsExported();
            AssertOversizedProvenanceFailsBeforeFilesystemCreation();
        }

        private static void AssertProvenanceIsExported()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-curtain-xlsx-provenance-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "curtain.xlsx");
                var row = ValidRow();
                row.ProjectId = "project-curtain";
                row.DrawingFingerprint = "drawing-fingerprint-curtain";
                row.ElementIds.Add("CW-01");
                row.ElementIds.Add("CW-02");
                row.SourceHandles.Add("AB12");
                row.SourceHandles.Add("CD34");

                CurtainWallXlsxExporter.Export(destination, new[] { row });

                using (var archive = ZipFile.OpenRead(destination))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml")
                        ?? throw new InvalidOperationException("Curtain XLSX is missing sheet1.xml.");
                    string xml;
                    using (var reader = new StreamReader(entry.Open())) xml = reader.ReadToEnd();
                    AssertContains(xml, "<dimension ref=\"A1:T2\"/>", "expanded provenance worksheet range");
                    AssertContains(xml, "Project ID", "Project ID header");
                    AssertContains(xml, "Drawing Fingerprint", "Drawing Fingerprint header");
                    AssertContains(xml, "Element IDs", "Element IDs header");
                    AssertContains(xml, "Source Handles", "Source Handles header");
                    AssertContains(xml, "project-curtain", "Project ID value");
                    AssertContains(xml, "drawing-fingerprint-curtain", "Drawing Fingerprint value");
                    AssertContains(xml, "CW-01;CW-02", "Element IDs value");
                    AssertContains(xml, "AB12;CD34", "Source Handles value");
                }
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertOversizedProvenanceFailsBeforeFilesystemCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-curtain-xlsx-provenance-bound-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                var row = ValidRow();
                row.SourceHandles.Add(new string('A', 32768));
                try
                {
                    CurtainWallXlsxExporter.Export(Path.Combine(untouchedDirectory, "curtain.xlsx"), new[] { row });
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    if (ex.Message.IndexOf("Source Handles", StringComparison.OrdinalIgnoreCase) < 0)
                        throw new InvalidOperationException("Curtain XLSX oversized source-handle failure must identify the provenance field.", ex);
                    if (Directory.Exists(untouchedDirectory))
                        throw new InvalidOperationException("Curtain XLSX oversized provenance touched the filesystem before failing.");
                    return;
                }

                throw new InvalidOperationException("Curtain XLSX exporter accepted source handles exceeding Excel's cell text limit.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static CurtainWallScheduleRow ValidRow()
        {
            return new CurtainWallScheduleRow
            {
                Floor = "L1",
                FamilyName = "CW1",
                WallCount = 1,
                TotalWallLengthM = 4d,
                GrossWallAreaM2 = 12d,
                OpeningAreaM2 = 1d,
                NetGlassAreaM2 = 8d,
                FrameFaceAreaM2 = 3d,
                FrameLengthM = 10d,
                PanelCount = 2,
                VerticalFrameCount = 3,
                HorizontalFrameCount = 2,
                MinimumClearPanelWidthM = 1d,
                MaximumClearPanelWidthM = 2d,
                MinimumClearPanelHeightM = 2d,
                MaximumClearPanelHeightM = 3d
            };
        }

        private static void AssertContains(string text, string expected, string label)
        {
            if (text.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Curtain XLSX did not preserve expected " + label + ".");
        }
    }
}
