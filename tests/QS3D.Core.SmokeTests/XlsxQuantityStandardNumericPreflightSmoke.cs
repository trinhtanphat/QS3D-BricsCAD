using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxQuantityStandardNumericPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsNonFiniteBeforeFilesystemMutation();
            ExportsFiniteStandardRow();
        }

        private static void RejectsNonFiniteBeforeFilesystemMutation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-quantity-xlsx-numeric-" + Guid.NewGuid().ToString("N"));
            Delete(root);
            var row = ValidRow();
            row.FormworkM2 = double.PositiveInfinity;
            try
            {
                try
                {
                    XlsxQuantityExporter.Export(Path.Combine(root, "invalid.xlsx"), new[] { row });
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                        throw new InvalidOperationException("Quantity XLSX numeric preflight must identify the rows argument.", ex);
                    if (ex.Message.IndexOf("worksheet row 2", StringComparison.OrdinalIgnoreCase) < 0 ||
                        ex.Message.IndexOf("FormworkM2", StringComparison.Ordinal) < 0)
                        throw new InvalidOperationException("Quantity XLSX numeric preflight must identify worksheet row 2 and FormworkM2.", ex);
                    if (Directory.Exists(root))
                        throw new InvalidOperationException("Quantity XLSX numeric preflight touched the filesystem before rejecting Infinity.");
                    return;
                }

                throw new InvalidOperationException("Quantity XLSX standard export accepted a non-finite numeric cell.");
            }
            finally
            {
                Delete(root);
            }
        }

        private static void ExportsFiniteStandardRow()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-quantity-xlsx-numeric-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "valid.xlsx");
            try
            {
                XlsxQuantityExporter.Export(path, new[] { ValidRow() });
                if (!File.Exists(path))
                    throw new InvalidOperationException("Quantity XLSX finite standard export did not produce a workbook.");
            }
            finally
            {
                Delete(root);
            }
        }

        private static QuantityReportRow ValidRow()
        {
            return new QuantityReportRow
            {
                Floor = "L1",
                Zone = "Z1",
                Category = "ArchitecturalWall",
                FamilyName = "W1",
                Count = 1,
                GrossConcreteM3 = 1d,
                DeductionM3 = 0.1d,
                NetConcreteM3 = 0.9d,
                FormworkM2 = 2d,
                LengthM = 3d,
                OuterPerimeterM = 4d,
                InnerPerimeterM = 1d,
                DoorAreaM2 = 0.2d,
                SideAreaM2 = 5d,
                BottomAreaM2 = 0d,
                TopAreaM2 = 0d,
                OtherAreaM2 = 0d,
                DrawingFingerprint = "DRAWING-1"
            };
        }

        private static void Delete(string directory)
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