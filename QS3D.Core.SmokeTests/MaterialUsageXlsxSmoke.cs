using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageXlsxSmoke
    {
        public static void Run()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-material-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                MaterialUsageXlsxExporter.Export(path, new List<MaterialUsageRow>
                {
                    new MaterialUsageRow
                    {
                        Floor = "Tầng 1",
                        MaterialName = "Kính",
                        UnitHint = "m²",
                        Component = "Material",
                        Category = "GlassWall",
                        FamilyName = "Vách kính 12mm",
                        ElementCount = 2,
                        LengthM = 9d,
                        AreaM2 = 22.5d,
                        VolumeM3 = 0.27d,
                        MassKg = 0d
                    }
                });
                if (!File.Exists(path)) throw new Exception("Material XLSX was not created.");
                using (var archive = ZipFile.OpenRead(path))
                {
                    var worksheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new Exception("Material XLSX worksheet is missing.");
                    if (archive.GetEntry("xl/workbook.xml") == null) throw new Exception("Material XLSX workbook is missing.");
                    using (var reader = new StreamReader(worksheet.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        if (xml.IndexOf("KL chính", StringComparison.Ordinal) < 0) throw new Exception("Material XLSX primary quantity header is missing.");
                        if (xml.IndexOf("22.5", StringComparison.Ordinal) < 0) throw new Exception("Material XLSX numeric payload is missing.");
                        if (xml.IndexOf("Kính", StringComparison.Ordinal) < 0) throw new Exception("Material XLSX material name is missing.");
                    }
                }
                File.WriteAllText(path, "ORIGINAL");
                var invalidRow = new MaterialUsageRow { FamilyName = "Invalid\u0001Family" };
                try { MaterialUsageXlsxExporter.Export(path, new List<MaterialUsageRow> { invalidRow }); throw new Exception("Invalid XML text must reject material XLSX export."); }
                catch (XmlException) { }
                if (File.ReadAllText(path) != "ORIGINAL") throw new Exception("Rejected material XLSX export replaced the existing destination.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }
    }
}
