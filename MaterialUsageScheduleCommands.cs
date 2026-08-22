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
    public sealed class MaterialUsageScheduleCommands
    {
        [CommandMethod("QS3DMATERIALXLSX", CommandFlags.Modal)]
        public void ExportMaterialUsage()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                var rows = MaterialUsageScheduleBuilder.Build(project);
                if (rows.Count == 0)
                {
                    const string empty = "Material XLSX: project chưa có material usage để xuất.";
                    PaletteCoordinator.SetStatus(empty);
                    document.Editor.WriteMessage("\nQS3D " + empty);
                    return;
                }

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
                MaterialUsageXlsxExporter.Export(dialog.FileName, rows);
                var materials = rows.Select(x => x.MaterialName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var elements = rows.Sum(x => x.ElementCount);
                var status = "Material XLSX: " + rows.Count + " nhóm • " + materials + " vật liệu • " + elements + " lượt cấu kiện/component.";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + "\n" + dialog.FileName);
            }
            catch (Exception ex)
            {
                var status = "QS3DMATERIALXLSX lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\n" + status);
            }
        }
    }
}
