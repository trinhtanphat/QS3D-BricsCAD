using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxXmlSanitizationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-room-finish-xlsx-xml-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "room-finish.xlsx");
            try
            {
                RoomFinishXlsxExporter.Export(path, new[]
                {
                    new RoomFinishScheduleRow
                    {
                        Material = "A\u0001B\uD800C<&",
                        Count = 1
                    }
                });

                using (var archive = ZipFile.OpenRead(path))
                using (var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")?.Open() ?? throw new Exception("Room Finish XLSX worksheet entry is missing.")))
                {
                    var xml = reader.ReadToEnd();
                    if (xml.IndexOf("A\uFFFDB\uFFFDC&lt;&amp;", StringComparison.Ordinal) < 0)
                        throw new Exception("Room Finish XLSX must replace XML-invalid text while preserving ordinary characters and escaping markup.");
                    if (xml.IndexOf('\u0001') >= 0 || xml.IndexOf('\uD800') >= 0)
                        throw new Exception("Room Finish XLSX worksheet must not retain XML-invalid control or unpaired surrogate characters.");
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
