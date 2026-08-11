using System;
using System.IO;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class BbsCsvCommands
    {
        [CommandMethod("QS3DBBSCSV", CommandFlags.Modal)]
        public void ExportCsv()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "BBS CSV: BLOCKED • chưa có QS3D project state/sidecar; export không tạo project mới.");
                    return;
                }

                var snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
                var rows = ProjectRebarScheduleBuilder.Build(snapshot);
                if (rows.Count == 0)
                {
                    Report(document, "BBS CSV: chưa có cấu kiện khai báo RebarNotation.");
                    return;
                }

                var totalWeight = 0d;
                foreach (var row in rows) totalWeight = QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS CSV total weight");

                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất BBS CSV UTF-8",
                    Filter = "CSV UTF-8 (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-BBS.csv"
                };
                if (dialog.ShowDialog() != true) return;

                RebarCsvExporter.Export(dialog.FileName, rows);

                var status = "BBS CSV: " + rows.Count + " bar mark • " + totalWeight.ToString("0.###") + " kg • " + dialog.FileName;
                FinalizeUi(document, status);
            }
            catch (System.Exception ex)
            {
                Report(document, "QS3DBBSCSV lỗi: " + ex.Message);
            }
        }

        private static void FinalizeUi(Document document, string status)
        {
            try
            {
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
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

        private static void Report(Document document, string status)
        {
            TrySetStatus(status);
            try { document.Editor.WriteMessage("\nQS3D " + status); }
            catch { }
        }

        private static void TrySetStatus(string status)
        {
            try { PaletteCoordinator.SetStatus(status); }
            catch { }
        }
    }
}
