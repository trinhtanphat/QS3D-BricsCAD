using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Xml;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxRebarXmlSanitizationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-rebar-xlsx-xml-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "rebar.xlsx");
            const string supplementary = "\uD83D\uDE00";
            try
            {
                XlsxRebarScheduleExporter.Export(path, new[]
                {
                    new RebarScheduleRow
                    {
                        ElementId = "E-1",
                        BarMark = "A\u0001B\uD800C<&" + supplementary,
                        ShapeCode = "00",
                        Notation = "1T10",
                        DiameterMm = 10d,
                        Quantity = 1,
                        FabricationStatus = "Ready " + supplementary
                    }
                });

                using (var archive = ZipFile.OpenRead(path))
                using (var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")?.Open() ?? throw new Exception("Rebar XLSX worksheet entry is missing.")))
                {
                    var xml = reader.ReadToEnd();
                    var document = new XmlDocument();
                    document.LoadXml(xml);

                    if (xml.IndexOf("A\uFFFDB\uFFFDC&lt;&amp;" + supplementary, StringComparison.Ordinal) < 0)
                        throw new Exception("Rebar XLSX must replace XML-invalid text, escape markup, and preserve valid supplementary Unicode characters.");
                    if (xml.IndexOf('\u0001') >= 0 || xml.IndexOf('\uD800') >= 0)
                        throw new Exception("Rebar XLSX worksheet must not retain XML-invalid control or unpaired surrogate characters.");
                    if (document.InnerText.IndexOf("A\uFFFDB\uFFFDC<&" + supplementary, StringComparison.Ordinal) < 0)
                        throw new Exception("Rebar XLSX sanitized inline text did not round-trip through XML parsing.");
                }
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
