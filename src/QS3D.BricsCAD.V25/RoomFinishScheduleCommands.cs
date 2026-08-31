using System;
using System.IO;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
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

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "HT_Phòng XLSX: BLOCKED • cần một QS3D project hiện hữu; lệnh export không tạo project mới.");
                    return;
                }
                var snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
                var rows = RoomFinishScheduleBuilder.Build(snapshot);
                if (rows.Count == 0)
                {
                    Report(document, "HT_Phòng XLSX: project chưa có finish semantic để xuất.");
                    return;
                }

                var count = 0;
                var primaryAccumulator = new QuantityReportMath.FiniteAccumulator();
                foreach (var row in rows)
                {
                    count = QuantityReportMath.AddCount(count, row.Count);
                    var primaryQuantity = QuantityReportMath.NonNegative(row.PrimaryQuantity, "HT_Phòng export primary quantity");
                    primaryAccumulator.Add(primaryQuantity, "HT_Phòng export primary quantity");
                }
                var primary = primaryAccumulator.Value("HT_Phòng export primary quantity");

                RoomFinishXlsxExporter.Export(dialog.FileName, rows);

                var status = "HT_Phòng XLSX: " + rows.Count + " nhóm • " + count + " finish element • tổng KL chính " + primary.ToString("0.###") + ".";
                FinalizeUi(document, status, dialog.FileName);
            }
            catch (System.Exception)
            {
                Report(document, "QS3DFINISHXLSX lỗi: không thể xuất bảng hoàn thiện phòng.");
            }
        }

        private static void FinalizeUi(Document document, string status, string fileName)
        {
            try
            {
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + "\n" + fileName);
            }
            catch (System.Exception)
            {
                try
                {
                    document.Editor.WriteMessage("\n[QS3D] Cảnh báo UI sau export: không thể cập nhật giao diện sau khi file đã được xuất.");
                }
                catch
                {
                    // Export has already committed; UI reporting is best effort only.
                }
            }
        }

        private static void Report(Document document, string status)
        {
            try { PaletteCoordinator.SetStatus(status); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + status); } catch { }
        }
    }
}
