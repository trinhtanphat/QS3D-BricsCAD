using System;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Export;
using QS3D.Core.Rebar;
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
                var project = ProjectContextCoordinator.GetOrCreate(document);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                var rows = ProjectRebarScheduleBuilder.Build(project);
                if (rows.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3D BBS CSV: chưa có cấu kiện khai báo RebarNotation.");
                    return;
                }
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
                var totalWeight = rows.Sum(x => x.TotalWeightKg);
                var status = "BBS CSV: " + rows.Count + " bar mark • " + totalWeight.ToString("0.###") + " kg • " + dialog.FileName;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DBBSCSV error: " + ex.Message);
                PaletteCoordinator.SetStatus("QS3DBBSCSV lỗi: " + ex.Message);
            }
        }
    }
}
