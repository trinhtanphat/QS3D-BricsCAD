using System;
using System.IO;
using System.Linq;
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
    public sealed class MaterialUsageScheduleCommands
    {
        [CommandMethod("QS3DMATERIALXLSX", CommandFlags.Modal)]
        public void ExportMaterialUsage()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất bảng vật liệu",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-Vat-Lieu.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Material XLSX: BLOCKED • cần một QS3D project hiện hữu; lệnh export không tạo project mới.");
                    return;
                }
                var snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
                var rows = MaterialUsageScheduleBuilder.Build(snapshot);
                if (rows.Count == 0)
                {
                    const string empty = "Material XLSX: project chưa có material usage để xuất.";
                    Report(document, empty);
                    return;
                }

                var materials = rows.Select(x => x.MaterialName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var elements = 0;
                foreach (var row in rows) elements = QuantityReportMath.AddCount(elements, row.ElementCount);

                MaterialUsageXlsxExporter.Export(dialog.FileName, rows);

                var status = "Material XLSX: " + rows.Count + " nhóm • " + materials + " vật liệu • " + elements + " lượt cấu kiện/component.";
                FinalizeUi(document, status, dialog.FileName);
            }
            catch (System.Exception)
            {
                Report(document, "QS3DMATERIALXLSX lỗi: không thể xuất bảng vật liệu.");
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
