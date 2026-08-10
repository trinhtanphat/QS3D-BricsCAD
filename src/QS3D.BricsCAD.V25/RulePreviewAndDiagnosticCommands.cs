using System;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Rules;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RulePreviewAndDiagnosticCommands
    {
        [CommandMethod("QS3DRULEPREVIEW", CommandFlags.Modal)]
        public void PreviewQuantityRules()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var preview = new QuantityRulePreviewService().PreviewProject(project);
                var changed = preview.Elements.Where(x => x.HasChanges).ToList();
                document.Editor.WriteMessage(
                    "\nQS3D Rule Preview: " + changed.Count + " element • " + preview.ChangeCount + " thay đổi quantity/provenance. Không mutate project.");

                foreach (var element in changed.Take(20))
                {
                    var details = string.Join(", ", element.Changes.Take(8).Select(x => x.OutputName + "=" + x.Kind));
                    if (element.Changes.Count > 8) details += ", ...";
                    document.Editor.WriteMessage("\n  " + element.ElementId + " [" + element.Category + "]: " + details);
                }
                if (changed.Count > 20)
                    document.Editor.WriteMessage("\n  ... còn " + (changed.Count - 20) + " element có thay đổi.");

                TrySetStatus("Rule Preview: " + changed.Count + " element • " + preview.ChangeCount + " thay đổi • read-only.");
            }
            catch (System.Exception ex)
            {
                try { document.Editor.WriteMessage("\nQS3DRULEPREVIEW error: " + ex.Message); } catch { }
                TrySetStatus("QS3DRULEPREVIEW lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DREGENPREVIEW", CommandFlags.Modal)]
        public void PreviewRegeneration()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var preview = new RegenerationPreviewService().Preview(project);
                document.Editor.WriteMessage(
                    "\nQS3D Regen Preview: " + preview.RegeneratedElementCount + " lượt regen • " +
                    preview.ChangedElementCount + " element đổi • " + preview.ChangedFieldCount + " field đổi • " +
                    preview.HealthDiff.NewErrorCount + " Health error mới. Không mutate project.");

                foreach (var delta in preview.Deltas.Take(20))
                {
                    var fields = string.Join(", ", delta.Fields.Take(8).Select(x => x.Field));
                    if (delta.Fields.Count > 8) fields += ", ...";
                    document.Editor.WriteMessage("\n  " + delta.ElementId + " [" + delta.Change + "]: " + fields);
                }
                if (preview.Deltas.Count > 20)
                    document.Editor.WriteMessage("\n  ... còn " + (preview.Deltas.Count - 20) + " element có delta.");

                TrySetStatus(
                    "Regen Preview: " + preview.ChangedElementCount + " element • " + preview.ChangedFieldCount +
                    " field • " + preview.HealthDiff.NewErrorCount + " error mới • read-only.");
            }
            catch (System.Exception ex)
            {
                try { document.Editor.WriteMessage("\nQS3DREGENPREVIEW error: " + ex.Message); } catch { }
                TrySetStatus("QS3DREGENPREVIEW lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DDIAGSUMMARY", CommandFlags.Modal)]
        public void ExportDiagnosticSummary()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất QS3D Diagnostic Summary (privacy-safe)",
                    Filter = "QS3D Diagnostic JSON (*.qs3d-diagnostic.json)|*.qs3d-diagnostic.json|JSON (*.json)|*.json",
                    DefaultExt = ".qs3d-diagnostic.json",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-diagnostic.qs3d-diagnostic.json"
                };
                if (dialog.ShowDialog() != true) return;

                var project = ProjectContextCoordinator.GetOrCreate(document);
                var issues = new ComprehensiveModelHealthService().Inspect(project);
                ProjectDiagnosticSummaryExporter.Export(dialog.FileName, project, issues);
                var errors = issues.Count(x => x.Severity == HealthSeverity.Error);
                var warnings = issues.Count(x => x.Severity == HealthSeverity.Warning);
                var status = "Diagnostic Summary: " + errors + " error • " + warnings + " warning • privacy-safe • " + dialog.FileName;
                FinalizeExportUi(document, status);
            }
            catch (System.Exception ex)
            {
                try { document.Editor.WriteMessage("\nQS3DDIAGSUMMARY error: " + ex.Message); } catch { }
                TrySetStatus("QS3DDIAGSUMMARY lỗi: " + ex.Message);
            }
        }

        private static void FinalizeExportUi(Document document, string status)
        {
            try
            {
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (System.Exception ex)
            {
                try { document.Editor.WriteMessage("\n[QS3D] Cảnh báo UI sau diagnostic export: " + ex.Message); }
                catch { }
            }
        }

        private static void TrySetStatus(string status)
        {
            try { PaletteCoordinator.SetStatus(status); }
            catch { }
        }
    }
}
