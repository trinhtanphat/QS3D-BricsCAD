using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using Teigha.Runtime;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Review;
using QS3D.Core.Rules;
using QS3D.Core.Services;

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
                if (!TryGetReadOnlyProject(document, "Rule Preview", out var project)) return;
                var preview = new QuantityRulePreviewService().PreviewProject(project);
                var status = "Rule Preview: " + preview.ChangedElementCount + " cấu kiện • " +
                    preview.ChangeCount + " thay đổi • chỉ xem trước, chưa áp dụng.";
                Report(document, status);
            }
            catch (System.Exception ex)
            {
                Report(document, "QS3DRULEPREVIEW lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DREGENPREVIEW", CommandFlags.Modal)]
        public void PreviewRegeneration()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!TryGetReadOnlyProject(document, "Regen Preview", out var project)) return;
                var preview = new RegenerationPreviewService().Preview(project);
                ReportRegenerationPreview(document, preview, "Project");
            }
            catch (System.Exception ex)
            {
                Report(document, "QS3DREGENPREVIEW lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DREGENPREVIEWSEL", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void PreviewSelectedRegeneration()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!TryGetReadOnlyProject(document, "Regen Preview Selection", out var project)) return;
                var elementIds = ResolveSelectedSemanticIds(document, project);
                if (elementIds.Count == 0)
                {
                    Report(document, "Regen Preview Selection: chưa có semantic selection hợp lệ.");
                    return;
                }

                var preview = new RegenerationPreviewService().PreviewSubset(project, elementIds);
                ReportRegenerationPreview(document, preview, "Selection");
            }
            catch (System.Exception ex)
            {
                Report(document, "QS3DREGENPREVIEWSEL lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DIMPACTPREVIEW", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void PreviewDependencyImpact()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!TryGetReadOnlyProject(document, "Dependency Impact", out var project)) return;
                var elementIds = ResolveSelectedSemanticIds(document, project);
                if (elementIds.Count == 0)
                {
                    Report(document, "Dependency Impact: chưa có semantic selection hợp lệ.");
                    return;
                }

                var plan = new DependencyImpactPlanner().Plan(project, elementIds);
                Report(document,
                    "Dependency Impact: " + plan.RootElementIds.Count + " nguồn • " +
                    plan.DirectCount + " trực tiếp • " + plan.TotalCount + " tổng ảnh hưởng • depth " + plan.MaxDepth + ".");
                foreach (var entry in plan.Entries.Take(20))
                {
                    try
                    {
                        document.Editor.WriteMessage(
                            "\n  " + entry.ElementId + " • depth " + entry.Depth +
                            " • cause " + entry.CauseElementId + " • root " + entry.RootElementId);
                    }
                    catch { }
                }
                if (plan.Entries.Count > 20)
                {
                    try { document.Editor.WriteMessage("\n  … còn " + (plan.Entries.Count - 20) + " phần tử ảnh hưởng."); } catch { }
                }
            }
            catch (System.Exception ex)
            {
                Report(document, "QS3DIMPACTPREVIEW lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DRULEPREVIEWEXPORT", CommandFlags.Modal)]
        public void ExportQuantityRuleReview()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!TryGetReadOnlyProject(document, "Rule Preview Export", out var project)) return;

                var dialog = CreateReviewDialog(document, "rule-review");
                if (dialog.ShowDialog() != true) return;

                var preview = new QuantityRulePreviewService().PreviewProject(project);
                var snapshot = new PreviewReviewSnapshotService().Create(SnapshotName(dialog.FileName, "Rule Review"), preview);
                new PreviewReviewSnapshotStore().Save(snapshot, dialog.FileName);
                ReportReviewExport(document, snapshot, dialog.FileName);
            }
            catch (System.Exception ex)
            {
                Report(document, "QS3DRULEPREVIEWEXPORT lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DREGENPREVIEWEXPORT", CommandFlags.Modal)]
        public void ExportRegenerationReview()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!TryGetReadOnlyProject(document, "Regen Preview Export", out var project)) return;

                var dialog = CreateReviewDialog(document, "regen-review");
                if (dialog.ShowDialog() != true) return;

                var preview = new RegenerationPreviewService().Preview(project);
                var snapshot = new PreviewReviewSnapshotService().Create(SnapshotName(dialog.FileName, "Regen Review"), preview);
                new PreviewReviewSnapshotStore().Save(snapshot, dialog.FileName);
                ReportReviewExport(document, snapshot, dialog.FileName);
            }
            catch (System.Exception ex)
            {
                Report(document, "QS3DREGENPREVIEWEXPORT lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DREGENPREVIEWEXPORTSEL", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ExportSelectedRegenerationReview()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!TryGetReadOnlyProject(document, "Regen Selection Review Export", out var project)) return;
                var elementIds = ResolveSelectedSemanticIds(document, project);
                if (elementIds.Count == 0)
                {
                    Report(document, "Regen Review Selection: chưa có semantic selection hợp lệ; chưa tạo file.");
                    return;
                }

                var dialog = CreateReviewDialog(document, "regen-selection-review");
                if (dialog.ShowDialog() != true) return;

                var preview = new RegenerationPreviewService().PreviewSubset(project, elementIds);
                var snapshot = new PreviewReviewSnapshotService().Create(SnapshotName(dialog.FileName, "Regen Selection Review"), preview);
                new PreviewReviewSnapshotStore().Save(snapshot, dialog.FileName);
                ReportReviewExport(document, snapshot, dialog.FileName);
            }
            catch (System.Exception ex)
            {
                Report(document, "QS3DREGENPREVIEWEXPORTSEL lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DDIAGSUMMARY", CommandFlags.Modal)]
        public void ExportDiagnosticSummary()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!TryGetReadOnlyProject(document, "Diagnostic Summary", out var project)) return;

                var drawingName = string.IsNullOrWhiteSpace(document.Name)
                    ? "QS3D"
                    : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất QS3D Diagnostic Summary",
                    Filter = "QS3D Diagnostic Summary (*.json)|*.json",
                    DefaultExt = ".json",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-qs3d-diagnostic-summary.json"
                };
                if (dialog.ShowDialog() != true) return;

                var issues = new ComprehensiveModelHealthService().Inspect(project);
                ProjectDiagnosticSummaryExporter.Export(dialog.FileName, project, issues);
                FinalizeExportUi(document, "Diagnostic Summary: " + issues.Count + " health issue • " + Path.GetFileName(dialog.FileName));
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true }); } catch { }
            }
            catch (System.Exception ex)
            {
                Report(document, "QS3DDIAGSUMMARY lỗi: " + ex.Message);
            }
        }

        private static bool TryGetReadOnlyProject(Document document, string operation, out ProjectState project)
        {
            if (ProjectContextCoordinator.TryGetReadOnly(document, out project)) return true;
            project = null!;
            Report(document, operation + ": chưa có QS3D project hiện hữu; chưa tạo project mới.");
            return false;
        }

        private static IReadOnlyList<string> ResolveSelectedSemanticIds(Document document, ProjectState project)
        {
            return SemanticSelectionResolver.ResolveImplied(document, project)
                .Select(x => x.Id)
                .ToList()
                .AsReadOnly();
        }

        private static SaveFileDialog CreateReviewDialog(Document document, string suffix)
        {
            var drawingName = string.IsNullOrWhiteSpace(document.Name)
                ? "QS3D"
                : Path.GetFileNameWithoutExtension(document.Name);
            return new SaveFileDialog
            {
                Title = "Xuất QS3D Preview Review Snapshot",
                Filter = "QS3D Preview Review (*.qsreview)|*.qsreview",
                DefaultExt = ".qsreview",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = drawingName + "-" + suffix + ".qsreview"
            };
        }

        private static string SnapshotName(string path, string fallback)
        {
            var name = Path.GetFileNameWithoutExtension(path ?? string.Empty)?.Trim();
            if (name == null || name.Length == 0) return fallback;
            return name;
        }

        private static void ReportRegenerationPreview(Document document, RegenerationPreview preview, string scope)
        {
            var status = "Regen Preview " + scope + ": " + preview.ChangedElementCount + " cấu kiện • " +
                preview.ChangedFieldCount + " trường thay đổi • +" + preview.HealthDiff.NewIssues.Count +
                " / -" + preview.HealthDiff.ResolvedIssues.Count + " health • lỗi mới " +
                preview.HealthDiff.NewErrorCount + " • chỉ xem trước, chưa áp dụng.";
            Report(document, status);
        }

        private static void ReportReviewExport(Document document, PreviewReviewSnapshot snapshot, string path)
        {
            FinalizeExportUi(document,
                "Preview Review: " + snapshot.Kind + " • " + snapshot.Scope + " • " + snapshot.ChangedElementCount +
                " cấu kiện • fingerprint " + snapshot.Fingerprint.Substring(0, 12) + " • " + Path.GetFileName(path));
        }

        private static void FinalizeExportUi(Document document, string status)
        {
            try { PaletteCoordinator.SetStatus(status); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + status); } catch { }
        }

        private static void Report(Document document, string status)
        {
            try { PaletteCoordinator.SetStatus(status); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + status); } catch { }
        }
    }
}
