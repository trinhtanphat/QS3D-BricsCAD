using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxBoundedWriteSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => CumulativeWorksheetBudgetFailsBeforeDestinationReplacement();

        private static void CumulativeWorksheetBudgetFailsBeforeDestinationReplacement()
        {
            var path = Path.Combine(Path.GetTempPath(), "room-finish-xlsx-bounded-" + Guid.NewGuid().ToString("N") + ".xlsx");
            const string sentinel = "KEEP-EXISTING-ROOM-FINISH-WORKBOOK";
            File.WriteAllText(path, sentinel, Encoding.UTF8);
            try { ExistingRoomFinishWorkbookSurvivesBudgetFailure(path, sentinel); }
            finally
            {
                TryDelete(path);
                var directory = Path.GetDirectoryName(path)!;
                foreach (var temp in Directory.GetFiles(directory, "." + Path.GetFileName(path) + ".*.tmp")) TryDelete(temp);
            }
        }
        private static void ExistingRoomFinishWorkbookSurvivesBudgetFailure(string path, string sentinel)
        {
            var rows = new List<RoomFinishScheduleRow>();
            var hostile = new string('&', 32767);
            for (var i = 0; i < 24; i++)
            {
                var row = new RoomFinishScheduleRow
                {
                    Floor = hostile,
                    Room = hostile,
                    Category = hostile,
                    FamilyName = hostile,
                    Material = hostile,
                    UnitHint = hostile,
                    Count = 1,
                    PrimaryQuantity = 1d,
                    LengthM = 1d,
                    AreaM2 = 1d,
                    ProjectId = hostile,
                    DrawingFingerprint = hostile
                };
                row.ElementIds.Add(hostile);
                row.RoomIds.Add(hostile);
                row.SourceHandles.Add(hostile);
                rows.Add(row);
            }

            try
            {
                RoomFinishXlsxExporter.Export(path, rows);
                throw new InvalidOperationException("Room-finish XLSX worksheet budget unexpectedly published.");
            }
            catch (InvalidDataException error) when (error.Message.Contains("Room-finish XLSX worksheet exceeds", StringComparison.Ordinal)) { }

            var preserved = File.ReadAllText(path, Encoding.UTF8);
            if (!preserved.Contains(sentinel, StringComparison.Ordinal))
                throw new InvalidOperationException("Room-finish XLSX bounded smoke: destination sentinel was replaced after failure.");
            var directory = Path.GetDirectoryName(path)!;
            var pattern = "." + Path.GetFileName(path) + ".*.tmp";
            if (Directory.GetFiles(directory, pattern).Length != 0)
                throw new InvalidOperationException("Room-finish XLSX bounded smoke: owned temp artifact leaked after failure.");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
