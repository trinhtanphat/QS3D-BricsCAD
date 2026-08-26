using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageXlsxCellTextLimitSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AcceptsExactCellLimit();
            RejectsOversizedCellBeforeFilesystemMutation();
        }

        private static void AcceptsExactCellLimit()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-material-xlsx-cell-limit-ok-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "material.xlsx");
            try
            {
                MaterialUsageXlsxExporter.Export(path, new[]
                {
                    new MaterialUsageRow
                    {
                        MaterialName = new string('A', 32767),
                        UnitHint = "m3",
                        ElementCount = 1,
                        ElementIds = { "E-1" },
                        VolumeM3 = 1d
                    }
                });
                if (!File.Exists(path)) throw new Exception("Material XLSX must accept exactly 32,767 text characters.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void RejectsOversizedCellBeforeFilesystemMutation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-material-xlsx-cell-limit-reject-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "material.xlsx");
            try
            {
                try
                {
                    MaterialUsageXlsxExporter.Export(path, new[]
                    {
                        new MaterialUsageRow
                        {
                            MaterialName = new string('B', 32768),
                            UnitHint = "m3",
                            ElementCount = 1,
                            ElementIds = { "E-1" },
                            VolumeM3 = 1d
                        }
                    });
                }
                catch (ArgumentOutOfRangeException)
                {
                    if (Directory.Exists(root) || File.Exists(path))
                        throw new Exception("Oversized Material XLSX text must fail before destination filesystem mutation.");
                    return;
                }

                throw new Exception("Material XLSX must reject text cells longer than 32,767 characters.");
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
