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
    public sealed class CurtainWallScheduleCommands
    {
        [CommandMethod("QS3DCURTAINXLSX", CommandFlags.Modal)]
        public void ExportCurtainSchedule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất bảng Vách Kính",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-Vach-Kinh.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Curtain XLSX: BLOCKED • cần một QS3D project hiện hữu; lệnh export không tạo project mới.");
                    return;
                }
                var snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
                var rows = CurtainWallScheduleBuilder.Build(snapshot);
                if (rows.Count == 0)
                {
                    Report(document, "Curtain XLSX: chưa có Vách Kính semantic để xuất.");
                    return;
                }

                var panels = 0;
                var glass = 0d;
                var frame = 0d;
                foreach (var row in rows)
                {
                    panels = QuantityReportMath.AddCount(panels, row.PanelCount);
                    glass = QuantityReportMath.Add(glass, row.NetGlassAreaM2, "Curtain export net glass area");
                    frame = QuantityReportMath.Add(frame, row.FrameLengthM, "Curtain export frame length");
                }

                CurtainWallXlsxExporter.Export(dialog.FileName, rows);

                var status = "Curtain XLSX: " + rows.Count + " nhóm • " + panels + " panel • " + glass.ToString("0.###") + " m² kính net • " + frame.ToString("0.###") + " m khung.";
                FinalizeUi(document, status, dialog.FileName);
            }
            catch (System.Exception)
            {
                Report(document, "QS3DCURTAINXLSX lỗi: không thể xuất bảng Vách Kính.");
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
