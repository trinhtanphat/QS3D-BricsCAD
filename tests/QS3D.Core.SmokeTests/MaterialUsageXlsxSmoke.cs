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
    internal static class MaterialUsageXlsxSmoke
    {
        public static void Run()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-material-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var row = new MaterialUsageRow
                {
                    Floor = "Tầng 1",
                    MaterialName = "Kính",
                    UnitHint = "m²",
                    Component = "Material",
                    Category = "GlassWall",
                    FamilyName = "Vách kính 12mm",
                    ElementCount = 2,
                    LengthM = 1e-9d,
                    AreaM2 = 22.5d,
                    VolumeM3 = 0.27d,
                    MassKg = 0d
                };
                MaterialUsageXlsxExporter.Export(path, new List<MaterialUsageRow> { row });
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

                        var document = new XmlDocument();
                        document.LoadXml(xml);
                        var namespaces = new XmlNamespaceManager(document.NameTable);
                        namespaces.AddNamespace("s", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                        var lengthNode = document.SelectSingleNode("/s:worksheet/s:sheetData/s:row[@r='2']/s:c[@r='I2']/s:v", namespaces);
                        if (lengthNode == null) throw new Exception("Material XLSX length cell is missing.");
                        var storedLength = double.Parse(lengthNode.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture);
                        if (storedLength != row.LengthM)
                            throw new Exception("Material XLSX numeric payload did not round-trip the source double.");
                    }
                }
                File.WriteAllText(path, "ORIGINAL");
                var invalidRow = new MaterialUsageRow { FamilyName = "Invalid\u0001Family" };
                MaterialUsageXlsxExporter.Export(path, new List<MaterialUsageRow> { invalidRow });
                using (var archive = ZipFile.OpenRead(path))
                {
                    var worksheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new Exception("Sanitized material XLSX worksheet is missing.");
                    using (var reader = new StreamReader(worksheet.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        if (xml.IndexOf('\u0001') >= 0) throw new Exception("Material XLSX retained an XML-invalid control character.");
                        if (xml.IndexOf('\uFFFD') < 0) throw new Exception("Material XLSX did not preserve the sanitized replacement marker.");
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
