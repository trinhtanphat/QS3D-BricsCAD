using System;
using System.IO;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxQuantityNullRowSmoke
    {
        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-quantity-null-row-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "quantity.xlsx");
                const string sentinel = "preserve-existing-quantity-xlsx";
                File.WriteAllText(destination, sentinel);

                AssertNullRow(destination);
                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException("Quantity XLSX null-row validation replaced an existing destination file.");

                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                AssertNullRow(Path.Combine(untouchedDirectory, "invalid.xlsx"));
                if (Directory.Exists(untouchedDirectory))
                    throw new InvalidOperationException("Quantity XLSX null-row validation touched the filesystem before failing.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AssertNullRow(string path)
        {
            try
            {
                XlsxQuantityExporter.Export(path, new QuantityReportRow[] { null! });
            }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                    throw new InvalidOperationException("Quantity XLSX null-row validation must identify the rows argument.", ex);
                if (ex.Message.IndexOf("row index: 0", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Quantity XLSX null-row validation must identify the zero-based row index.", ex);
                return;
            }

            throw new InvalidOperationException("Quantity XLSX exporter accepted a null report row.");
        }
    }
}
