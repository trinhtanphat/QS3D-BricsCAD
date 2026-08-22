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
            RejectsNegativeCountAndPhysicalQuantitiesBeforeFilesystemMutation();
            ExportsZeroAndPositiveStandardRows();
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

        private static void RejectsNegativeCountAndPhysicalQuantitiesBeforeFilesystemMutation()
        {
            var sentinel = new byte[] { 81, 83, 51, 68, 45, 88, 76, 83, 88 };
            var mutations = new[]
            {
                new NegativeMutation("Count", row => row.Count = -1),
                new NegativeMutation("GrossConcreteM3", row => row.GrossConcreteM3 = -1d),
                new NegativeMutation("DeductionM3", row => row.DeductionM3 = -1d),
                new NegativeMutation("NetConcreteM3", row => row.NetConcreteM3 = -1d),
                new NegativeMutation("FormworkM2", row => row.FormworkM2 = -1d),
                new NegativeMutation("LengthM", row => row.LengthM = -1d),
                new NegativeMutation("OuterPerimeterM", row => row.OuterPerimeterM = -1d),
                new NegativeMutation("InnerPerimeterM", row => row.InnerPerimeterM = -1d),
                new NegativeMutation("DoorAreaM2", row => row.DoorAreaM2 = -1d),
                new NegativeMutation("SideAreaM2", row => row.SideAreaM2 = -1d),
                new NegativeMutation("BottomAreaM2", row => row.BottomAreaM2 = -1d),
                new NegativeMutation("TopAreaM2", row => row.TopAreaM2 = -1d),
                new NegativeMutation("OtherAreaM2", row => row.OtherAreaM2 = -1d),
            };

            for (var index = 0; index < mutations.Length; index++)
            {
                var mutation = mutations[index];
                var root = Path.Combine(
                    Path.GetTempPath(),
                    "qs3d-quantity-xlsx-negative-" + index + "-" + Guid.NewGuid().ToString("N"));
                Delete(root);
                var row = ValidRow();
                mutation.Apply(row);
                try
                {
                    try
                    {
                        XlsxQuantityExporter.Export(Path.Combine(root, "invalid.xlsx"), new[] { row });
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                            throw new InvalidOperationException("Quantity XLSX negative preflight must identify the rows argument.", ex);
                        if (ex.Message.IndexOf("worksheet row 2", StringComparison.OrdinalIgnoreCase) < 0 ||
                            ex.Message.IndexOf(mutation.FieldName, StringComparison.Ordinal) < 0 ||
                            ex.Message.IndexOf("non-negative", StringComparison.OrdinalIgnoreCase) < 0)
                            throw new InvalidOperationException(
                                "Quantity XLSX negative preflight must identify worksheet row 2, " + mutation.FieldName + " and the non-negative contract.",
                                ex);
                        if (Directory.Exists(root))
                            throw new InvalidOperationException(
                                "Quantity XLSX negative preflight touched the filesystem before rejecting " + mutation.FieldName + ".");

                        Directory.CreateDirectory(root);
                        var existingPath = Path.Combine(root, "existing.xlsx");
                        File.WriteAllBytes(existingPath, sentinel);
                        try
                        {
                            XlsxQuantityExporter.Export(existingPath, new[] { row });
                        }
                        catch (ArgumentOutOfRangeException existingEx)
                        {
                            if (!string.Equals(existingEx.ParamName, "rows", StringComparison.Ordinal) ||
                                existingEx.Message.IndexOf(mutation.FieldName, StringComparison.Ordinal) < 0 ||
                                existingEx.Message.IndexOf("non-negative", StringComparison.OrdinalIgnoreCase) < 0)
                                throw new InvalidOperationException(
                                    "Quantity XLSX existing-destination refusal lost field-level negative diagnostics for " + mutation.FieldName + ".",
                                    existingEx);
                            if (!ByteEqual(File.ReadAllBytes(existingPath), sentinel))
                                throw new InvalidOperationException(
                                    "Quantity XLSX negative preflight replaced the existing destination for " + mutation.FieldName + ".");
                            if (Directory.GetFiles(root, Path.GetFileName(existingPath) + ".*.tmp").Length != 0)
                                throw new InvalidOperationException(
                                    "Quantity XLSX negative preflight left a temporary package for " + mutation.FieldName + ".");
                            continue;
                        }
                        throw new InvalidOperationException(
                            "Quantity XLSX standard export accepted negative " + mutation.FieldName + " for an existing destination.");
                    }

                    throw new InvalidOperationException("Quantity XLSX standard export accepted negative " + mutation.FieldName + ".");
                }
                finally
                {
                    Delete(root);
                }
            }
        }

        private static bool ByteEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            for (var index = 0; index < left.Length; index++)
                if (left[index] != right[index]) return false;
            return true;
        }

        private static void ExportsZeroAndPositiveStandardRows()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-quantity-xlsx-numeric-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "valid.xlsx");
            try
            {
                XlsxQuantityExporter.Export(path, new[] { ValidRow(), ZeroRow() });
                if (!File.Exists(path))
                    throw new InvalidOperationException("Quantity XLSX zero/positive standard export did not produce a workbook.");
            }
            finally
            {
                Delete(root);
            }
        }

        private static QuantityReportRow ZeroRow()
        {
            return new QuantityReportRow
            {
                Floor = "L0",
                Zone = "Z0",
                Category = "Other",
                FamilyName = "Zero",
                Count = 0,
                DrawingFingerprint = "DRAWING-1"
            };
        }

        private sealed class NegativeMutation
        {
            public NegativeMutation(string fieldName, Action<QuantityReportRow> apply)
            {
                FieldName = fieldName;
                Apply = apply;
            }

            public string FieldName { get; }
            public Action<QuantityReportRow> Apply { get; }
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
