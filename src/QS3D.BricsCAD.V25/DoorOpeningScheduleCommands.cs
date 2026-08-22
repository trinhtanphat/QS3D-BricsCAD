using System;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Export;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class DoorOpeningScheduleCommands
    {
        [CommandMethod("QS3DDOORXLSX", CommandFlags.Modal)]
        public void ExportDoorOpeningSchedule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất bảng Cửa / Lỗ mở",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-Cua-Lo-Mo.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                if (!ExistingProjectMutationContext.TryGet(document, out var project))
                    throw new InvalidOperationException("Door XLSX cần một QS3D project hiện hữu; lệnh export không tạo project mới.");
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                var rows = DoorOpeningScheduleBuilder.Build(project);
                if (rows.Count == 0)
                {
                    const string empty = "Door XLSX: project chưa có Cửa/Lỗ mở semantic để xuất.";
                    PaletteCoordinator.SetStatus(empty);
                    document.Editor.WriteMessage("\nQS3D " + empty);
                    return;
                }

                var count = 0;
                var area = 0d;
                foreach (var row in rows)
                {
                    count = QuantityReportMath.AddCount(count, row.Count);
                    area = QuantityReportMath.Add(area, row.OpeningAreaM2, "Door/Opening export area");
                }
                var hosts = rows.SelectMany(x => x.HostIds).Distinct(StringComparer.OrdinalIgnoreCase).Count();

                DoorOpeningXlsxExporter.Export(dialog.FileName, rows);

                var status = "Door XLSX: " + rows.Count + " nhóm • " + count + " Cửa/Lỗ • " + area.ToString("0.###") + " m² • " + hosts + " host.";
                FinalizeUi(document, status, dialog.FileName);
            }
            catch (System.Exception ex)
            {
                var status = "QS3DDOORXLSX lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\n" + status);
            }
        }

        private static void FinalizeUi(Document document, string status, string fileName)
        {
            try
            {
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + "\n" + fileName);
            }
            catch (System.Exception ex)
            {
                try
                {
                    document.Editor.WriteMessage("\n[QS3D] Cảnh báo UI sau export: " + ex.Message);
                }
                catch
                {
                    // Export has already committed; UI reporting is best effort only.
                }
            }
        }
    }
}
