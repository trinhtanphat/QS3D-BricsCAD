using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxRebarNullRowPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-rebar-xlsx-null-row-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var destination = Path.Combine(root, "rebar.xlsx");
                const string sentinel = "preserve-existing-rebar-xlsx";
                File.WriteAllText(destination, sentinel);

                AssertNullRow(destination);
                if (!string.Equals(File.ReadAllText(destination), sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException("Rebar XLSX null-row validation replaced an existing destination file.");

                var untouchedDirectory = Path.Combine(root, "must-not-be-created");
                AssertNullRow(Path.Combine(untouchedDirectory, "invalid.xlsx"));
                if (Directory.Exists(untouchedDirectory))
                    throw new InvalidOperationException("Rebar XLSX null-row validation touched the filesystem before failing.");

                var validPath = Path.Combine(root, "valid.xlsx");
                XlsxRebarScheduleExporter.Export(validPath, new[] { ValidRow() });
                if (!File.Exists(validPath))
                    throw new InvalidOperationException("Rebar XLSX ordinary non-null export behavior regressed.");
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void AssertNullRow(string path)
        {
            try
            {
                XlsxRebarScheduleExporter.Export(path, new RebarScheduleRow[] { ValidRow(), null! });
            }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                    throw new InvalidOperationException("Rebar XLSX null-row validation must identify the rows argument.", ex);
                if (ex.Message.IndexOf("row index: 1", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Rebar XLSX null-row validation must identify the zero-based invalid row index.", ex);
                return;
            }

            throw new InvalidOperationException("Rebar XLSX exporter accepted a null schedule row.");
        }

        private static RebarScheduleRow ValidRow()
        {
            return new RebarScheduleRow
            {
                ElementId = "E-1",
                BarMark = "B1",
                ShapeCode = "00",
                Notation = "1T10",
                DiameterMm = 10d,
                Quantity = 1,
                CuttingLengthM = 1d,
                TotalLengthM = 1d,
                UnitWeightKgM = 1d,
                NetWeightKg = 1d,
                TotalWeightKg = 1d
            };
        }
    }
}
