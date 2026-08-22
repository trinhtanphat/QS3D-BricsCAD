using System;
using System.IO;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Services;
using QS3D.Core.Templates;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class TemplateCommands
    {
        [CommandMethod("QS3DTEMPLATEEXPORT", CommandFlags.Modal)]
        public void ExportTemplate()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DTEMPLATEEXPORT", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var drawingName = string.IsNullOrWhiteSpace(doc.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(doc.Name);
                var dialog = new SaveFileDialog { Title = "Xuất QS3D Template", Filter = "QS3D Template (*.qstemplate)|*.qstemplate", DefaultExt = ".qstemplate", AddExtension = true, OverwritePrompt = true, FileName = drawingName + ".qstemplate" };
                if (dialog.ShowDialog() != true) return;
                var store = new TemplateProfileStore();
                var profile = store.ExportProject(project, "template-" + Guid.NewGuid().ToString("N"), drawingName + " Template");
                store.Save(profile, dialog.FileName);
                PaletteCoordinator.SetStatus("Đã xuất template: " + dialog.FileName);
                doc.Editor.WriteMessage("\nQS3D template exported: " + dialog.FileName);
            });
        }

        [CommandMethod("QS3DTEMPLATEIMPORT", CommandFlags.Modal)]
        public void ImportTemplate()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DTEMPLATEIMPORT", () =>
            {
                var dialog = new OpenFileDialog { Title = "Nạp QS3D Template", Filter = "QS3D Template (*.qstemplate)|*.qstemplate", CheckFileExists = true, Multiselect = false };
                if (dialog.ShowDialog() != true) return;
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var store = new TemplateProfileStore();
                var profile = store.Load(dialog.FileName);
                var result = store.Apply(project, profile);
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                PaletteCoordinator.RefreshProject();
                var message = "Template " + profile.Name + ": family +" + result.FamiliesAdded + "/~" + result.FamiliesUpdated + " • rule +" + result.RulesAdded + "/~" + result.RulesUpdated + " • mapping " + result.LayerMappingsApplied + " • regen " + regenerated + ". Chưa tự lưu .qsdb.";
                PaletteCoordinator.SetStatus(message);
                doc.Editor.WriteMessage("\nQS3D " + message);
            });
        }

        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Guard(Document document, string operation, Action action) { try { action(); } catch (Exception ex) { document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message); PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); } }
    }
}
