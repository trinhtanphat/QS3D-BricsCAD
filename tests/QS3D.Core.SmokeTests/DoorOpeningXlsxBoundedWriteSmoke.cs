using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningXlsxBoundedWriteSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => CumulativeWorksheetBudgetFailsBeforeDestinationReplacement();

        private static void CumulativeWorksheetBudgetFailsBeforeDestinationReplacement()
        {
            var path = Path.Combine(Path.GetTempPath(), "door-opening-xlsx-bounded-" + Guid.NewGuid().ToString("N") + ".xlsx");
            const string sentinel = "KEEP-EXISTING-DOOR-OPENING-WORKBOOK";
            File.WriteAllText(path, sentinel, Encoding.UTF8);
            try
            {
                ExistingDoorOpeningWorkbookSurvivesBudgetFailure(path, sentinel);
            }
            finally
            {
                TryDelete(path);
                var directory = Path.GetDirectoryName(path)!;
                foreach (var temp in Directory.GetFiles(directory, "." + Path.GetFileName(path) + ".*.tmp")) TryDelete(temp);
            }
        }

        private static void ExistingDoorOpeningWorkbookSurvivesBudgetFailure(string path, string sentinel)
        {
            var rows = new List<DoorOpeningScheduleRow>();
            var hostile = new string('界', 32767);
            for (var i = 0; i < 40; i++)
            {
                var row = new DoorOpeningScheduleRow
                {
                    ProjectId = hostile,
                    DrawingFingerprint = hostile,
                    Floor = hostile,
                    Category = hostile,
                    FamilyName = hostile,
                    Material = hostile,
                    WidthM = 1d,
                    HeightM = 1d,
                    SillHeightM = 0d,
                    ThicknessM = 0.1d,
                    Count = 1,
                    OpeningAreaM2 = 1d,
                    HostCount = 1
                };
                row.ElementIds.Add(hostile);
                row.HostIds.Add(hostile);
                row.SourceHandles.Add(hostile);
                rows.Add(row);
            }

            try
            {
                DoorOpeningXlsxExporter.Export(path, rows);
                throw new InvalidOperationException("Door/opening XLSX worksheet budget unexpectedly published.");
            }
            catch (InvalidDataException error) when (error.Message.Contains("Door/opening XLSX worksheet exceeds", StringComparison.Ordinal))
            {
            }

            var preserved = File.ReadAllText(path, Encoding.UTF8);
            if (!preserved.Contains(sentinel, StringComparison.Ordinal))
                throw new InvalidOperationException("Door/opening XLSX bounded smoke: destination sentinel was replaced after failure.");
            var directory = Path.GetDirectoryName(path)!;
            var pattern = "." + Path.GetFileName(path) + ".*.tmp";
            if (Directory.GetFiles(directory, pattern).Length != 0)
                throw new InvalidOperationException("Door/opening XLSX bounded smoke: owned temp artifact leaked after failure.");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}