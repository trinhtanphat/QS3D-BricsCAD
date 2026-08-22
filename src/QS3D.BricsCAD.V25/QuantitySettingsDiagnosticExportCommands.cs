using System;
using System.IO;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Reporting;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class QuantitySettingsDiagnosticExportCommands
    {
        [CommandMethod("QS3DQSETTINGSHEALTHEXPORT", CommandFlags.Modal)]
        public void ExportQuantitySettingsHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var settings = new QuantitySettingsStore().Load();
                var snapshot = QuantityCalculationMatrixDiagnosticSnapshot.Create(settings);

                var dialog = new SaveFileDialog
                {
                    Title = "Xuất QS3D Quantity Settings Health",
                    Filter = "QS3D Quantity Settings Health (*.json)|*.json",
                    DefaultExt = ".json",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = "QS3D_quantity_settings_health.json"
                };
                if (dialog.ShowDialog() != true) return;

                QuantityCalculationMatrixDiagnosticSnapshotExporter.Save(dialog.FileName, snapshot);
                WriteLine(document,
                    "Quantity Settings Health Export: " +
                    snapshot.ExistingDirectedRuleCount + "/" + snapshot.ExpectedDirectedRuleCount +
                    " luật có hướng • " + snapshot.MissingDirectedPairs.Count +
                    " cặp thiếu • " + Path.GetFileName(dialog.FileName));
            }
            catch (System.Exception)
            {
                WriteLine(document, "QS3DQSETTINGSHEALTHEXPORT lỗi: không thể tạo hoặc ghi báo cáo Quantity Settings Health. Kiểm tra cấu hình và vị trí lưu.");
            }
        }

        private static void WriteLine(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); }
            catch { }
        }
    }
}