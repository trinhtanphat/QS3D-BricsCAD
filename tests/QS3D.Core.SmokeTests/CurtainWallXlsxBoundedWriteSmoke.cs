using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallXlsxBoundedWriteSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => CumulativeWorksheetBudgetFailsBeforeDestinationReplacement();

        private static void CumulativeWorksheetBudgetFailsBeforeDestinationReplacement()
        {
            var path = Path.Combine(Path.GetTempPath(), "curtain-xlsx-bounded-" + Guid.NewGuid().ToString("N") + ".xlsx");
            const string sentinel = "KEEP-EXISTING-CURTAIN-WORKBOOK";
            File.WriteAllText(path, sentinel, Encoding.UTF8);
            try
            {
                ExistingCurtainWorkbookSurvivesBudgetFailure(path, sentinel);
            }
            finally
            {
                TryDelete(path);
                foreach (var temp in Directory.GetFiles(Path.GetDirectoryName(path)!, "." + Path.GetFileName(path) + ".*.tmp")) TryDelete(temp);
            }
        }
        private static void ExistingCurtainWorkbookSurvivesBudgetFailure(string path, string sentinel)
        {
            var rows = new List<CurtainWallScheduleRow>();
            var hostile = new string('界', 32767);
            for (var i = 0; i < 60; i++)
            {
                var row = new CurtainWallScheduleRow
                {
                    ProjectId = hostile,
                    DrawingFingerprint = hostile,
                    Floor = hostile,
                    FamilyName = hostile,
                    WallCount = 1,
                    TotalWallLengthM = 1d,
                    GrossWallAreaM2 = 1d,
                    OpeningAreaM2 = 0d,
                    NetGlassAreaM2 = 1d,
                    FrameFaceAreaM2 = 1d,
                    FrameLengthM = 1d,
                    PanelCount = 1,
                    VerticalFrameCount = 1,
                    HorizontalFrameCount = 1,
                    MinimumClearPanelWidthM = 1d,
                    MaximumClearPanelWidthM = 1d,
                    MinimumClearPanelHeightM = 1d,
                    MaximumClearPanelHeightM = 1d
                };
                row.ElementIds.Add(hostile);
                row.SourceHandles.Add(hostile);
                rows.Add(row);
            }
            try
            {
                CurtainWallXlsxExporter.Export(path, rows);
                throw new InvalidOperationException("Curtain XLSX worksheet budget unexpectedly published.");
            }
            catch (InvalidDataException error) when (error.Message.Contains("Curtain XLSX worksheet exceeds", StringComparison.Ordinal))
            {
            }

            var preserved = File.ReadAllText(path, Encoding.UTF8);
            if (!preserved.Contains(sentinel, StringComparison.Ordinal))
                throw new InvalidOperationException("Curtain XLSX bounded smoke: destination sentinel was replaced after failure.");
            var directory = Path.GetDirectoryName(path)!;
            var pattern = "." + Path.GetFileName(path) + ".*.tmp";
            if (Directory.GetFiles(directory, pattern).Length != 0)
                throw new InvalidOperationException("Curtain XLSX bounded smoke: owned temp artifact leaked after failure.");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
