using System;
using System.Globalization;
using System.IO;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
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

                var project = ProjectContextCoordinator.GetOrCreate(document);
                var snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
                ProjectInterchangeJsonExporter.Export(dialog.FileName, snapshot);

                var status = "Interchange JSON: " + snapshot.Elements.Count + " cấu kiện • " + snapshot.Families.Count + " Family • detached regenerate " + regenerated + " • " + dialog.FileName;
                try { PaletteCoordinator.SetStatus(status); } catch { }
                document.Editor.WriteMessage("\nQS3D " + status);
                document.Editor.WriteMessage("\nQS3D snapshot là read-only semantic interchange v" + ProjectInterchangeJsonExporter.FormatVersion + ": ID/quantity/SI/provenance; regenerate chỉ chạy trên detached copy, không mutate project live; không phải QSDB backup và không chứa generated CAD ownership handles.");
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEJSON lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage("\nQS3DINTERCHANGEJSON error: " + ex.Message);
            }
        }

        [CommandMethod("QS3DINTERCHANGEAPPEND", CommandFlags.Modal)]
        public void AppendSemanticSnapshot()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Thêm QS3D Semantic Snapshot vào project hiện tại",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var validation = ProjectInterchangeJsonValidator.ValidateFile(dialog.FileName);
                if (!validation.IsValid)
                    throw new InvalidDataException("Snapshot không hợp lệ: " + validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " error(s). Chạy QS3DINTERCHANGEVALIDATE để xem chi tiết.");

                var json = File.ReadAllText(dialog.FileName);
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var preview = ProjectInterchangeImportPreview.Plan(project, json);
                if (!preview.Validation.IsValid)
                    throw new InvalidDataException("Snapshot không còn vượt qua validation khi lập import preview.");
                if (preview.CollisionCount > 0)
                {
                    var collisionStatus = "Interchange Append bị chặn: " + preview.CollisionCount.ToString(CultureInfo.InvariantCulture) + " semantic ID collision(s). Append-only không merge/replace/skip.";
                    try { PaletteCoordinator.SetStatus(collisionStatus); } catch { }
                    document.Editor.WriteMessage("\nQS3D " + collisionStatus);
                    return;
                }

                var confirmText =
                    "Thêm snapshot semantic vào project hiện tại theo chế độ APPEND-ONLY?\n\n" +
                    "Source project: " + preview.SourceProjectId + "\n" +
                    "Semantic identity mới: " + preview.NewIdentityCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Zone: " + validation.ZoneCount.ToString(CultureInfo.InvariantCulture) +
                    " • Floor: " + validation.FloorCount.ToString(CultureInfo.InvariantCulture) +
                    " • Family: " + validation.FamilyCount.ToString(CultureInfo.InvariantCulture) +
                    " • Element: " + validation.ElementCount.ToString(CultureInfo.InvariantCulture) + "\n\n" +
                    "Chế độ này chỉ nhận ID/tên semantic mới; không merge/replace dữ liệu đang có.\n" +
                    "Drawing handles/fingerprint nguồn KHÔNG trở thành ownership của DWG đích; generated/native ownership không được tái tạo.\n" +
                    "Project ID, drawing identity và active context hiện tại được giữ nguyên. Imported elements được đánh dấu dirty để review/rebuild sau.\n\n" +
                    "File .qsdb sẽ chưa tự lưu.";
                if (System.Windows.MessageBox.Show(
                        confirmText,
                        "QS3D — Interchange Append",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;

                var result = ProjectInterchangeAppendOnlyImporter.Import(project, json);
                try { PaletteCoordinator.RefreshProject(); } catch { }

                var status =
                    "Interchange Append: Zone +" + result.ZonesAdded.ToString(CultureInfo.InvariantCulture) +
                    " • Floor +" + result.FloorsAdded.ToString(CultureInfo.InvariantCulture) +
                    " • Family +" + result.FamiliesAdded.ToString(CultureInfo.InvariantCulture) +
                    " • Element +" + result.ElementsAdded.ToString(CultureInfo.InvariantCulture) +
                    " • source handles discarded " + result.SourceHandlesDiscarded.ToString(CultureInfo.InvariantCulture) +
                    " • warning " + result.ValidationWarnings.ToString(CultureInfo.InvariantCulture) +
                    ". Chưa tự lưu .qsdb.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
                document.Editor.WriteMessage("\nQS3D " + status);
                document.Editor.WriteMessage("\nQS3D append-only import chỉ thêm semantic portable state; không phải native CAD merge và không xác nhận source geometry trong DWG hiện tại.");
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEAPPEND lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage("\nQS3DINTERCHANGEAPPEND error: " + ex.Message + " Importer rollback semantic mutation nếu apply thất bại.");
            }
        }
    }
}
