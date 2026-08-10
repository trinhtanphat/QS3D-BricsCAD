using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallXlsxSmoke
    {
        public static void Run()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-curtain-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow>
                {
                    new CurtainWallScheduleRow
                    {
                        Floor = "Tầng 1",
                        FamilyName = "Vách kính 12mm",
                        WallCount = 2,
                        TotalWallLengthM = 9d,
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
                    }
                });
                if (!File.Exists(path)) throw new Exception("Curtain XLSX was not created.");
                using (var archive = ZipFile.OpenRead(path))
                {
                    if (archive.GetEntry("xl/worksheets/sheet1.xml") == null) throw new Exception("Curtain XLSX worksheet is missing.");
                    if (archive.GetEntry("xl/workbook.xml") == null) throw new Exception("Curtain XLSX workbook is missing.");
                    using (var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        if (xml.IndexOf("DT kính net", StringComparison.Ordinal) < 0) throw new Exception("Curtain XLSX header is missing.");
                        if (xml.IndexOf("22.5", StringComparison.Ordinal) < 0) throw new Exception("Curtain XLSX numeric payload is missing.");
                    }
                }
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }
    }
}
