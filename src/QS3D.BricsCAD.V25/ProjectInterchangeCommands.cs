using System;
using System.IO;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Export;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectInterchangeCommands
    {
        [CommandMethod("QS3DINTERCHANGEJSON", CommandFlags.Modal)]
        public void ExportSemanticSnapshot()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất QS3D Semantic Snapshot JSON",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json",
                    DefaultExt = ".qs3d.json",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + ".qs3d.json"
                };
                if (dialog.ShowDialog() != true) return;

                ProjectInterchangeJsonExporter.Export(dialog.FileName, project);
                var status = "Interchange JSON: " + project.Elements.Count + " cấu kiện • " + project.Families.Count + " Family • regenerate " + regenerated + " • " + dialog.FileName;
                try { PaletteCoordinator.SetStatus(status); } catch { }
                document.Editor.WriteMessage("\nQS3D " + status);
                document.Editor.WriteMessage("\nQS3D snapshot là read-only semantic interchange v" + ProjectInterchangeJsonExporter.FormatVersion + ": ID/quantity/SI/provenance; không phải QSDB backup và không chứa generated CAD ownership handles.");
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEJSON lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage("\nQS3DINTERCHANGEJSON error: " + ex.Message);
            }
        }
    }
}
