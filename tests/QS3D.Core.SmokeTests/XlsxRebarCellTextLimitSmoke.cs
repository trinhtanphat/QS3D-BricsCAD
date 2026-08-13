using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxRebarCellTextLimitSmoke
    {
        private const int MaxCellTextLength = 32767;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AcceptsExactLimit();
            RejectsOversizedCellBeforeFilesystemMutation();
        }

        private static void AcceptsExactLimit()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-rebar-xlsx-cell-limit-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "exact-limit.xlsx");
            try
            {
                var row = ValidRow();
                row.BarMark = new string('B', MaxCellTextLength);
                XlsxRebarScheduleExporter.Export(path, new[] { row });
                if (!File.Exists(path))
                    throw new InvalidOperationException("Rebar XLSX rejected text at Excel's exact cell-content limit.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void RejectsOversizedCellBeforeFilesystemMutation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-rebar-xlsx-cell-limit-" + Guid.NewGuid().ToString("N"));
            DeleteDirectory(root);
            var oversized = ValidRow();
            oversized.BarMark = new string('B', MaxCellTextLength + 1);
            try
            {
                try
                {
                    XlsxRebarScheduleExporter.Export(
                        Path.Combine(root, "oversized.xlsx"),
                        new[] { ValidRow(), oversized });
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                        throw new InvalidOperationException("Rebar XLSX cell-limit validation must identify the rows argument.", ex);
                    if (ex.Message.IndexOf("worksheet row 3", StringComparison.OrdinalIgnoreCase) < 0 ||
                        ex.Message.IndexOf("Bar Mark", StringComparison.Ordinal) < 0)
                        throw new InvalidOperationException("Rebar XLSX cell-limit validation must identify the worksheet row and field.", ex);
                    if (Directory.Exists(root))
                        throw new InvalidOperationException("Rebar XLSX cell-limit validation touched the filesystem before failing.");
                    return;
                }

                throw new InvalidOperationException("Rebar XLSX exporter accepted a 32,768-character cell value.");
            }
            finally
            {
                DeleteDirectory(root);
            }
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
                TotalWeightKg = 1d,
                FabricationStatus = "Ready",
                FabricationStandardCode = "STD",
                FabricationDetailingRevision = "R1"
            };
        }

        private static void DeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
