using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxBoundedWriteSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            VietnameseHeaderRoundTripIsPreserved();
            CumulativeWorksheetBudgetFailsBeforeDestinationReplacement();
        }

        private static void VietnameseHeaderRoundTripIsPreserved()
        {
            var path = Path.Combine(Path.GetTempPath(), "room-finish-xlsx-header-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                RoomFinishXlsxExporter.Export(path, new[] { new RoomFinishScheduleRow { Floor = "Tầng 1", Room = "Phòng 101", Category = "Sơn", FamilyName = "Tường", Material = "Sơn nước", UnitHint = "m²", Count = 1, PrimaryQuantity = 1d, LengthM = 1d, AreaM2 = 1d, ProjectId = "P", DrawingFingerprint = "F" } });
                using var archive = ZipFile.OpenRead(path);
                var entry = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidOperationException("Room-finish XLSX header smoke: worksheet entry is missing.");
                using var reader = new StreamReader(entry.Open(), new UTF8Encoding(false, true), true);
                var xml = reader.ReadToEnd();
                var headers = new[] { "Tầng", "Phòng", "Loại hoàn thiện", "Family / Loại", "Vật liệu", "Đơn vị", "SL", "KL chính", "Dài (m)", "Diện tích (m²)", "Element IDs", "Room IDs", "Project ID", "Drawing fingerprint", "Source Handles" };
                foreach (var header in headers)
                    if (!xml.Contains(">" + header + "</t>", StringComparison.Ordinal))
                        throw new InvalidOperationException("Room-finish XLSX header smoke: UTF-8 header fidelity drifted: " + header);

                var workbookEntry = archive.GetEntry("xl/workbook.xml") ?? throw new InvalidOperationException("Room-finish XLSX header smoke: workbook entry is missing.");
                using var workbookReader = new StreamReader(workbookEntry.Open(), new UTF8Encoding(false, true), true);
                var workbookXml = workbookReader.ReadToEnd();
                if (!workbookXml.Contains("name=\"HT Phòng\"", StringComparison.Ordinal))
                    throw new InvalidOperationException("Room-finish XLSX header smoke: workbook sheet-name UTF-8 fidelity drifted.");
            }
            finally { TryDelete(path); }
        }

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
