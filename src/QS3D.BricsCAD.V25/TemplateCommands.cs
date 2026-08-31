using System;
using System.IO;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Persistence;
using QS3D.Core.Services;
using QS3D.Core.Templates;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class TemplateCommands
    {
        private const string ExportUiWarning = "[QS3D] Cảnh báo UI sau export template; dữ liệu export đã hoàn tất nhưng UI chưa cập nhật đầy đủ.";
        private const string ImportUiWarning = "[QS3D] Cảnh báo UI sau import template; dữ liệu import đã áp dụng nhưng UI chưa cập nhật đầy đủ.";
        private const string RollbackUiWarning = "[QS3D] Cảnh báo UI sau rollback Template Import; project đã rollback nhưng UI chưa cập nhật đầy đủ.";

        [CommandMethod("QS3DTEMPLATEEXPORT", CommandFlags.Modal)]
        public void ExportTemplate()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DTEMPLATEEXPORT", () =>
            {
                var drawingName = string.IsNullOrWhiteSpace(doc.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(doc.Name);
                var dialog = new SaveFileDialog { Title = "Xuất QS3D Template", Filter = "QS3D Template (*.qstemplate)|*.qstemplate", DefaultExt = ".qstemplate", AddExtension = true, OverwritePrompt = true, FileName = drawingName + ".qstemplate" };
                if (dialog.ShowDialog() != true) return;

                if (!ProjectContextCoordinator.TryGetReadOnly(doc, out var project))
                {
                    const string blocked = "Template export: chưa có QS3D project hiện hữu; export không tạo project mới.";
                    try { PaletteCoordinator.SetStatus(blocked); } catch { }
                    try { doc.Editor.WriteMessage("\nQS3D " + blocked); } catch { }
                    return;
                }
                var store = new TemplateProfileStore();
                var profile = store.ExportProject(project, "template-" + Guid.NewGuid().ToString("N"), drawingName + " Template");
                store.Save(profile, dialog.FileName);
                FinalizeExportUi(doc, "Đã xuất template: " + dialog.FileName, "QS3D template exported: " + dialog.FileName);
            });
        }

        [CommandMethod("QS3DTEMPLATEIMPORT", CommandFlags.Modal)]
        public void ImportTemplate()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DTEMPLATEIMPORT", () =>
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(doc, out var previewProject))
                {
                    const string blocked = "Template import: chưa có QS3D project hiện hữu; import không tạo project mới.";
                    try { PaletteCoordinator.SetStatus(blocked); } catch { }
                    try { doc.Editor.WriteMessage("\nQS3D " + blocked); } catch { }
                    return;
                }
                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;

                var dialog = new OpenFileDialog { Title = "Nạp QS3D Template", Filter = "QS3D Template (*.qstemplate)|*.qstemplate", CheckFileExists = true, Multiselect = false };
                if (dialog.ShowDialog() != true) return;

                var store = new TemplateProfileStore();
                var profile = store.Load(dialog.FileName);
                var confirmText = "Áp dụng template “" + profile.Name + "” vào project hiện tại?\n\n" +
                                  "Family: " + profile.Families.Count + "\n" +
                                  "Rule khối lượng: " + profile.QuantityRules.Count + "\n" +
                                  "Layer mapping: " + profile.LayerMappings.Count + "\n\n" +
                                  "QS3D sẽ regenerate thử trước khi chấp nhận thay đổi. File .qsdb sẽ chưa tự lưu.";
                if (System.Windows.MessageBox.Show(confirmText, "QS3D — Nạp Template", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;

                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, doc))
                    throw new InvalidOperationException("Template Import: DWG active đã thay đổi trong lúc chọn/xác nhận template. Hãy chạy lại trên bản vẽ hiện hành.");

                var project = ExistingProjectMutationContext.Require(doc, "Template Import");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("Template Import: QS3D project đã thay đổi trong lúc chọn/xác nhận template. Không áp dụng dữ liệu stale; hãy chạy lại.");

                var rollback = ProjectStateSnapshot.Capture(project);
                TemplateApplyResult result;
                int regenerated;
                try
                {
                    result = store.Apply(project, profile);
                    regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                }
                catch (System.Exception importError)
                {
                    try
                    {
                        rollback.Restore(project);
                    }
                    catch (System.Exception restoreError)
                    {
                        throw new InvalidOperationException("Template import failed and project rollback also failed.", new AggregateException(importError, restoreError));
                    }

                    RefreshProjectBestEffort(doc);
                    throw;
                }

                var message = "Template " + profile.Name + ": family +" + result.FamiliesAdded + "/~" + result.FamiliesUpdated + " • rule +" + result.RulesAdded + "/~" + result.RulesUpdated + " • mapping " + result.LayerMappingsApplied + " • regen " + regenerated + ". Chưa tự lưu .qsdb.";
                FinalizeImportUi(doc, message);
            });
        }

        private static void FinalizeExportUi(Document document, string status, string message)
        {
            try
            {
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\n" + message);
            }
            catch (System.Exception)
            {
                TryWrite(document, "\n" + ExportUiWarning);
            }
        }

        private static void FinalizeImportUi(Document document, string message)
        {
            var warning = false;
            try { PaletteCoordinator.RefreshProject(); }
            catch (System.Exception) { warning = true; }
            try { PaletteCoordinator.SetStatus(message); }
            catch (System.Exception) { warning = true; }
            try { document.Editor.WriteMessage("\nQS3D " + message); }
            catch (System.Exception) { warning = true; }
            if (!warning) return;
            TryWrite(document, "\n" + ImportUiWarning);
        }

        private static void RefreshProjectBestEffort(Document document)
        {
            try { PaletteCoordinator.RefreshProject(); }
            catch (System.Exception)
            {
                TryWrite(document, "\n" + RollbackUiWarning);
            }
        }

        private static void TryWrite(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); }
            catch { }
        }

        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Guard(Document document, string operation, Action action)
        {
            try { action(); }
            catch (System.Exception)
            {
                var message = operation + " lỗi; thao tác không hoàn tất. Xem log chẩn đoán nếu cần chi tiết kỹ thuật.";
                TryWrite(document, "\n" + message);
                try { PaletteCoordinator.SetStatus(message); } catch { }
            }
        }
    }
}
