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
    public sealed class CurtainWallScheduleCommands
    {
        [CommandMethod("QS3DCURTAINXLSX", CommandFlags.Modal)]
        public void ExportCurtainSchedule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                var rows = CurtainWallScheduleBuilder.Build(project);
                if (rows.Count == 0)
                {
                    const string empty = "Curtain XLSX: chưa có Vách Kính semantic để xuất.";
                    PaletteCoordinator.SetStatus(empty);
                    document.Editor.WriteMessage("\nQS3D " + empty);
                    return;
                }

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
                CurtainWallXlsxExporter.Export(dialog.FileName, rows);

                var panels = 0;
                var glass = 0d;
                var frame = 0d;
                foreach (var row in rows)
                {
                    panels = QuantityReportMath.AddCount(panels, row.PanelCount);
                    glass = QuantityReportMath.Add(glass, row.NetGlassAreaM2, "Curtain export net glass area");
                    frame = QuantityReportMath.Add(frame, row.FrameLengthM, "Curtain export frame length");
                }
                var status = "Curtain XLSX: " + rows.Count + " nhóm • " + panels + " panel • " + glass.ToString("0.###") + " m² kính net • " + frame.ToString("0.###") + " m khung.";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + "\n" + dialog.FileName);
            }
            catch (System.Exception ex)
            {
                var status = "QS3DCURTAINXLSX lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\n" + status);
            }
        }
    }
}
