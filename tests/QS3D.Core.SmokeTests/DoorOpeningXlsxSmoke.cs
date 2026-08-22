using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningXlsxSmoke
    {
        public static void Run()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var row = new DoorOpeningScheduleRow
                {
                    Floor = "Tầng 1",
                    Category = "Door",
                    FamilyName = "Cửa D1",
                    Material = "Gỗ",
                    WidthM = 0.9d,
                    HeightM = 2.2d,
                    SillHeightM = 0d,
                    ThicknessM = 0.1d,
                    Count = 2,
                    OpeningAreaM2 = 3.9d,
                    HostCount = 2
                };
                row.ElementIds.Add("d1"); row.ElementIds.Add("d2");
                row.HostIds.Add("wall-a"); row.HostIds.Add("wall-b");
                DoorOpeningXlsxExporter.Export(path, new List<DoorOpeningScheduleRow> { row });
                if (!File.Exists(path)) throw new Exception("Door/opening XLSX was not created.");
                using (var archive = ZipFile.OpenRead(path))
                {
                    var worksheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new Exception("Door/opening XLSX worksheet is missing.");
                    if (archive.GetEntry("xl/workbook.xml") == null) throw new Exception("Door/opening XLSX workbook is missing.");
                    using (var reader = new StreamReader(worksheet.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        if (xml.IndexOf("DT mở", StringComparison.Ordinal) < 0) throw new Exception("Door/opening XLSX area header is missing.");
                        if (xml.IndexOf("3.9", StringComparison.Ordinal) < 0) throw new Exception("Door/opening XLSX numeric payload is missing.");
                        if (xml.IndexOf("wall-a;wall-b", StringComparison.Ordinal) < 0) throw new Exception("Door/opening XLSX host provenance is missing.");
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
