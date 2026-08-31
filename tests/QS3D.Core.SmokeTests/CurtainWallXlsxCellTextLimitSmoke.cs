using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallXlsxCellTextLimitSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AcceptsExactCellLimit();
            RejectsOversizedCellBeforeFilesystemMutation();
        }

        private static void AcceptsExactCellLimit()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-curtain-xlsx-cell-limit-ok-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "curtain.xlsx");
            try
            {
                CurtainWallXlsxExporter.Export(path, new[]
                {
                    new CurtainWallScheduleRow
                    {
                        FamilyName = new string('A', 32767),
                        WallCount = 1,
                        MinimumClearPanelWidthM = 0d,
                        MaximumClearPanelWidthM = 0d,
                        MinimumClearPanelHeightM = 0d,
                        MaximumClearPanelHeightM = 0d,
                        ElementIds = { "CW-CELL-LIMIT-1" },
                        SourceHandles = { "H-CELL-LIMIT-1" }
                    }
                });
                if (!File.Exists(path)) throw new Exception("Curtain XLSX must accept exactly 32,767 text characters.");
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
            var root = Path.Combine(Path.GetTempPath(), "qs3d-curtain-xlsx-cell-limit-reject-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "curtain.xlsx");
            try
            {
                try
                {
                    CurtainWallXlsxExporter.Export(path, new[]
                    {
                        new CurtainWallScheduleRow
                        {
                            FamilyName = new string('B', 32768),
                            WallCount = 1,
                            MinimumClearPanelWidthM = 0d,
                            MaximumClearPanelWidthM = 0d,
                            MinimumClearPanelHeightM = 0d,
                            MaximumClearPanelHeightM = 0d,
                            ElementIds = { "CW-CELL-LIMIT-2" },
                            SourceHandles = { "H-CELL-LIMIT-2" }
                        }
                    });
                }
                catch (ArgumentOutOfRangeException)
                {
                    if (Directory.Exists(root) || File.Exists(path))
                        throw new Exception("Oversized Curtain XLSX text must fail before destination filesystem mutation.");
                    return;
                }

                throw new Exception("Curtain XLSX must reject text cells longer than 32,767 characters.");
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
