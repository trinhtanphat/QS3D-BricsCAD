using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxXmlCharacterIntegritySmoke
    {
        private const string HostileText = "A<&\u0001B\uD800😀";

        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-xml-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                VerifyMaterialExporter(Path.Combine(root, "material.xlsx"));
                VerifyCurtainExporter(Path.Combine(root, "curtain.xlsx"));
                VerifyDoorExporter(Path.Combine(root, "door.xlsx"));
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void VerifyMaterialExporter(string path)
        {
            MaterialUsageXlsxExporter.Export(path, new List<MaterialUsageRow>
            {
                new MaterialUsageRow
                {
                    Floor = HostileText,
                    MaterialName = HostileText,
                    UnitHint = "m",
                    Component = HostileText,
                    Category = HostileText,
                    FamilyName = HostileText,
                    ElementCount = 1,
                    ElementIds = { "E-XML-1" },
                    LengthM = 1d
                }
            });
            VerifyWorksheet(path, "material");
        }

        private static void VerifyCurtainExporter(string path)
        {
            var row = new CurtainWallScheduleRow
            {
                Floor = HostileText,
                FamilyName = HostileText,
                WallCount = 1,
                MinimumClearPanelWidthM = 0d,
                MinimumClearPanelHeightM = 0d
            };
            row.ElementIds.Add("CW-XML-1");
            row.SourceHandles.Add("H-XML-1");
            CurtainWallXlsxExporter.Export(path, new List<CurtainWallScheduleRow> { row });
            VerifyWorksheet(path, "curtain");
        }

        private static void VerifyDoorExporter(string path)
        {
            var row = new DoorOpeningScheduleRow
            {
                Floor = HostileText,
                Category = HostileText,
                FamilyName = HostileText,
                Material = HostileText,
                WidthM = 0.9d,
                HeightM = 2.1d,
                SillHeightM = 0d,
                ThicknessM = 0.1d,
                OpeningAreaM2 = 1.89d,
                Count = 1,
                HostCount = 1
            };
            row.ElementIds.Add("E-1");
            row.HostIds.Add("H-1");
            DoorOpeningXlsxExporter.Export(path, new List<DoorOpeningScheduleRow> { row });
            VerifyWorksheet(path, "door/opening");
        }

        private static void VerifyWorksheet(string path, string label)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                var entry = archive.GetEntry("xl/worksheets/sheet1.xml")
                    ?? throw new InvalidOperationException(label + " XLSX is missing sheet1.xml.");
                string xml;
                using (var reader = new StreamReader(entry.Open())) xml = reader.ReadToEnd();

                Expect(xml.IndexOf('\u0001') < 0, label + " XLSX must not contain XML 1.0-forbidden control characters.");
                Expect(xml.Contains("�"), label + " XLSX must replace forbidden characters instead of dropping the surrounding text.");
                Expect(xml.Contains("😀"), label + " XLSX must preserve valid supplementary Unicode.");
                Expect(xml.Contains("&lt;&amp;"), label + " XLSX must continue escaping XML markup characters.");

                using (var stringReader = new StringReader(xml))
                using (var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
                    while (xmlReader.Read()) { }
            }
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
