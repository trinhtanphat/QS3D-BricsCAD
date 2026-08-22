using System;
using System.IO;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Export;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RoomFinishScheduleCommands
    {
        [CommandMethod("QS3DFINISHXLSX", CommandFlags.Modal)]
        public void ExportRoomFinishSchedule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                var rows = RoomFinishScheduleBuilder.Build(project);
                if (rows.Count == 0)
                {
                    const string empty = "HT_Phòng XLSX: project chưa có finish semantic để xuất.";
                    PaletteCoordinator.SetStatus(empty);
                    document.Editor.WriteMessage("\nQS3D " + empty);
                    return;
                }
                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất bảng hoàn thiện phòng",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-HT-Phong.xlsx"
                };
                if (dialog.ShowDialog() != true) return;
                RoomFinishXlsxExporter.Export(dialog.FileName, rows);

                var count = 0;
                var primary = 0d;
                foreach (var row in rows)
                {
                    count = QuantityReportMath.AddCount(count, row.Count);
                    primary = QuantityReportMath.Add(primary, row.PrimaryQuantity, "HT_Phòng export primary quantity");
                }
                var status = "HT_Phòng XLSX: " + rows.Count + " nhóm • " + count + " finish element • tổng KL chính " + primary.ToString("0.###") + ".";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + "\n" + dialog.FileName);
            }
            catch (System.Exception ex)
            {
                var status = "QS3DFINISHXLSX lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\n" + status);
            }
        }
    }
}
