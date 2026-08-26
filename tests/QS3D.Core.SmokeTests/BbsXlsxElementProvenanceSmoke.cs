using System;
using System.IO;
using System.IO.Compression;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BbsXlsxElementProvenanceSmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidElementIdBeforePublication();
            AcceptsValidElementId();
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

                var row = ValidRow("E\u0001BAD");
                try
                {
                    XlsxRebarScheduleExporter.Export(destination, new[] { row });
                }
                catch (ArgumentException ex)
                {
                    if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                        throw new InvalidOperationException("BBS XLSX invalid ElementId validation must identify the rows argument.", ex);
                    if (ex.Message.IndexOf("worksheet row 2", StringComparison.OrdinalIgnoreCase) < 0)
                        throw new InvalidOperationException("BBS XLSX invalid ElementId validation must identify worksheet row 2.", ex);
                    if (ex.Message.IndexOf("Element", StringComparison.Ordinal) < 0)
                        throw new InvalidOperationException("BBS XLSX invalid ElementId validation must identify the Element field.", ex);

                    if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                        throw new InvalidOperationException("BBS XLSX invalid ElementId validation replaced an existing destination file.");
                    return;
                }

                throw new InvalidOperationException("BBS XLSX exporter accepted an XML-invalid ElementId and silently rewrote provenance.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AcceptsValidElementId()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-bbs-xlsx-provenance-valid-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "bbs.xlsx");
                XlsxRebarScheduleExporter.Export(destination, new[] { ValidRow("E-VALID") });

                if (!File.Exists(destination) || new FileInfo(destination).Length == 0)
                    throw new InvalidOperationException("BBS XLSX exporter rejected a valid ElementId control row.");

                using (var stream = File.OpenRead(destination))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false))
                {
                    if (archive.GetEntry("xl/worksheets/sheet1.xml") == null)
                        throw new InvalidOperationException("BBS XLSX valid control did not publish the worksheet payload.");
                }
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
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
