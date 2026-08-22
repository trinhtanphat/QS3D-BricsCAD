using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CustomerWorkbookTraceTamperSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsTraceIdentityTamperWithoutRekey();
        }

        private static void RejectsTraceIdentityTamperWithoutRekey()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-smoke-customer-trace-tamper-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var path = Path.Combine(root, "qs-customer-tampered.xlsx");
                QsCustomerWorkbookExporter.Export(path, new[] { Row() }, new[] { Row() });

                RewriteEntry(path, "xl/worksheets/sheet4.xml", xml =>
                {
                    const string original = ">E1<";
                    const string tampered = ">E2<";
                    var index = xml.IndexOf(original, StringComparison.Ordinal);
                    if (index < 0) throw new Exception("TRACE_MODEL regression fixture is missing the expected E1 identity.");
                    return xml.Substring(0, index) + tampered + xml.Substring(index + original.Length);
                });

                ExpectThrows<InvalidDataException>(
                    () => QsCustomerWorkbookTraceReader.Read(path, "DGKL", 2),
                    "Customer workbook must reject TRACE_MODEL identity tampering when TRACE_KEY was not recomputed.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static QuantityReportRow Row()
        {
            var row = new QuantityReportRow
            {
                Floor = "L01",
                Zone = "A",
                Category = "Beam",
                FamilyId = "F-BEAM",
                FamilyName = "Beam 300x600",
                ElementName = "Beam E1",
                Material = "Concrete",
                DrawingFingerprint = "DWG-CUSTOMER-TRACE-TAMPER",
                Count = 1,
                GrossConcreteM3 = 1d,
                NetConcreteM3 = 1d,
                HasGrossConcreteM3Evidence = true,
                HasNetConcreteM3Evidence = true,
                HasDeductionM3Evidence = false,
                HasFormworkM2Evidence = false,
                HasLengthMEvidence = false,
                HasOuterPerimeterMEvidence = false,
                HasInnerPerimeterMEvidence = false,
                HasDoorAreaM2Evidence = false,
                HasSideAreaM2Evidence = false,
                HasBottomAreaM2Evidence = false,
                HasTopAreaM2Evidence = false,
                HasOtherAreaM2Evidence = false
            };
            row.ElementIds.Add("E1");
            row.SourceHandles.Add("A1");
            return row;
        }

        private static void RewriteEntry(string path, string entryName, Func<string, string> transform)
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry(entryName) ?? throw new Exception("Missing XLSX entry: " + entryName + ".");
                string source;
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8)) source = reader.ReadToEnd();
                var replacement = transform(source);
                entry.Delete();
                var rewritten = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using (var writer = new StreamWriter(rewritten.Open(), new UTF8Encoding(false))) writer.Write(replacement);
            }
        }

        private static void ExpectThrows<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception(message);
        }
    }
}
