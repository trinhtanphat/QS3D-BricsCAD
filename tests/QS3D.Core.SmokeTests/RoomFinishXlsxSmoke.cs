using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxSmoke
    {
        public static void Run()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var row = new RoomFinishScheduleRow
                {
                    Floor = "Tầng 1",
                    Room = "Phòng 101",
                    Category = "WallFinish",
                    FamilyName = "Sơn nước",
                    Material = "Sơn",
                    UnitHint = "m²",
                    Count = 2,
                    AreaM2 = 30d,
                    PrimaryQuantity = 30d
                };
                row.ElementIds.Add("wf1"); row.ElementIds.Add("wf2"); row.RoomIds.Add("room-1");
                RoomFinishXlsxExporter.Export(path, new List<RoomFinishScheduleRow> { row });
                if (!File.Exists(path)) throw new Exception("Room-finish XLSX was not created.");
                using (var archive = ZipFile.OpenRead(path))
                {
                    var worksheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new Exception("Room-finish XLSX worksheet is missing.");
                    if (archive.GetEntry("xl/workbook.xml") == null) throw new Exception("Room-finish XLSX workbook is missing.");
                    using (var reader = new StreamReader(worksheet.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        if (xml.IndexOf("Loại hoàn thiện", StringComparison.Ordinal) < 0) throw new Exception("Room-finish XLSX header is missing.");
                        if (xml.IndexOf("Phòng 101", StringComparison.Ordinal) < 0) throw new Exception("Room-finish XLSX room label is missing.");
                        if (xml.IndexOf(">30<", StringComparison.Ordinal) < 0) throw new Exception("Room-finish XLSX quantity is missing.");
                    }
                }
                File.WriteAllText(path, "ORIGINAL");
                var invalidRow = new RoomFinishScheduleRow { FamilyName = "Invalid\u0001Family" };
                try { RoomFinishXlsxExporter.Export(path, new List<RoomFinishScheduleRow> { invalidRow }); throw new Exception("Invalid XML text must reject room-finish XLSX export."); }
                catch (XmlException) { }
                if (File.ReadAllText(path) != "ORIGINAL") throw new Exception("Rejected room-finish XLSX export replaced the existing destination.");
            }
            finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        }
    }
}
