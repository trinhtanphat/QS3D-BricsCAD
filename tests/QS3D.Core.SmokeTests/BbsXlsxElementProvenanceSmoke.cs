using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BbsXlsxElementProvenanceSmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidElementIdBeforePublication();
            RejectsMalformedSurrogateBeforeFilesystemMutation();
            AcceptsValidUnicodeElementId();
        }

        private static void RejectsXmlInvalidElementIdBeforePublication()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-bbs-xlsx-provenance-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "bbs.xlsx");
                const string sentinel = "preserve-existing-bbs-destination";
                File.WriteAllText(destination, sentinel);

                AssertInvalidElementId(destination, "E\u0001BAD");

                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException("BBS XLSX invalid ElementId validation replaced an existing destination file.");
                if (Directory.GetFiles(root).Length != 1)
                    throw new InvalidOperationException("BBS XLSX invalid ElementId validation left temporary package residue.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void RejectsMalformedSurrogateBeforeFilesystemMutation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-bbs-xlsx-provenance-surrogate-" + Guid.NewGuid().ToString("N"));
            var destination = Path.Combine(root, "nested", "bbs.xlsx");
            try
            {
                AssertInvalidElementId(destination, "E-" + new string(new[] { '\uD800' }));
                if (Directory.Exists(root))
                    throw new InvalidOperationException("BBS XLSX malformed ElementId validation mutated the filesystem before failing.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AcceptsValidUnicodeElementId()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-bbs-xlsx-provenance-valid-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "bbs.xlsx");
                const string elementId = "CỘT-α-梁-01";
                XlsxRebarScheduleExporter.Export(destination, new[] { ValidRow(elementId) });

                if (!File.Exists(destination) || new FileInfo(destination).Length == 0)
                    throw new InvalidOperationException("BBS XLSX exporter rejected a valid Unicode ElementId control row.");

                using (var stream = File.OpenRead(destination))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false))
                using (var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open(), Encoding.UTF8))
                {
                    var sheet = reader.ReadToEnd();
                    if (sheet.IndexOf(elementId, StringComparison.Ordinal) < 0)
                        throw new InvalidOperationException("BBS XLSX exporter did not preserve the valid Unicode ElementId exactly.");
                    if (sheet.IndexOf("Fabrication Status", StringComparison.Ordinal) < 0 ||
                        sheet.IndexOf("Standard Code", StringComparison.Ordinal) < 0 ||
                        sheet.IndexOf("Detailing Revision", StringComparison.Ordinal) < 0)
                        throw new InvalidOperationException("BBS XLSX provenance hardening changed fabrication metadata columns.");
                }
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertInvalidElementId(string destination, string elementId)
        {
            try
            {
                XlsxRebarScheduleExporter.Export(destination, new[] { ValidRow(elementId) });
            }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                    throw new InvalidOperationException("BBS XLSX invalid ElementId validation must identify the rows argument.", ex);
                if (ex.Message.IndexOf("worksheet row 2", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("BBS XLSX invalid ElementId validation must identify worksheet row 2.", ex);
                if (ex.Message.IndexOf("Element", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("BBS XLSX invalid ElementId validation must identify the Element field.", ex);
                return;
            }

            throw new InvalidOperationException("BBS XLSX exporter accepted an XML-invalid ElementId and silently rewrote provenance.");
        }

        private static RebarScheduleRow ValidRow(string elementId)
        {
            return new RebarScheduleRow
            {
                ElementId = elementId,
                BarMark = "B1",
                ShapeCode = "00",
                Notation = "1D16",
                DiameterMm = 16d,
                Quantity = 1,
                CuttingLengthM = 1d,
                TotalLengthM = 1d,
                UnitWeightKgM = RebarWeight.KilogramsPerMeter(16d),
                NetWeightKg = RebarWeight.KilogramsPerMeter(16d),
                WastePercent = 0d,
                TotalWeightKg = RebarWeight.KilogramsPerMeter(16d),
                FabricationStatus = string.Empty,
                FabricationStandardCode = string.Empty,
                FabricationDetailingRevision = string.Empty
            };
        }
    }
}
