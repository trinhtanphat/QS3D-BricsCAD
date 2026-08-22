using System;
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
            var path = Path.Combine(Path.GetTempPath(), "qs3d-curtain-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var row = new CurtainWallScheduleRow
                {
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
                CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow> { row });
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
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }
    }
}
