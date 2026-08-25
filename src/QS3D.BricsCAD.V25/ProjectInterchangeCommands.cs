using System;
using System.Globalization;
using System.IO;
using System.Text;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectInterchangeCommands
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

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

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    ReportUi(document, "Interchange JSON: chưa có QS3D project hiện hữu; chưa tạo project mới và chưa tạo file.");
                    return;
                }

                var snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
                ProjectInterchangeJsonExporter.Export(dialog.FileName, snapshot);

                var status = "Interchange JSON: " + snapshot.Elements.Count + " cấu kiện • " + snapshot.Families.Count + " Family • detached regenerate " + regenerated + " • " + dialog.FileName;
                FinalizeUi(
                    document,
                    status,
                    "QS3D snapshot là read-only semantic interchange v" + ProjectInterchangeJsonExporter.FormatVersion + ": ID/quantity/SI/provenance; regenerate chỉ chạy trên detached copy, không mutate project live; không phải QSDB backup và không chứa generated CAD ownership handles.");
            }
            catch (Exception ex)
            {
                ReportUi(document, "QS3DINTERCHANGEJSON lỗi: " + ex.Message);
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

                var json = ReadGuardedSnapshotText(dialog.FileName);
                var validation = ProjectInterchangeJsonValidator.Validate(json);
                if (!validation.IsValid)
                    throw new InvalidDataException("Snapshot không hợp lệ: " + validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " error(s). Chạy QS3DINTERCHANGEVALIDATE để xem chi tiết.");
                ProjectInterchangeValidatedSnapshotReader.Read(json);

                EnsureActive(document, "Interchange Append / preview");
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var previewChangeVersion = project.ChangeVersion;
                var preview = ProjectInterchangeImportPreview.Plan(project, json);
                if (!preview.Validation.IsValid)
                    throw new InvalidDataException("Snapshot không còn vượt qua validation khi lập import preview.");
                if (preview.CollisionCount > 0)
                {
                    ReportUi(document, "Interchange Append bị chặn: " + preview.CollisionCount.ToString(CultureInfo.InvariantCulture) + " semantic ID collision(s). Append-only không merge/replace/skip.");
                    return;
                }

                // The append plan performs the stricter all-new ID + name preflight. This is intentionally
                // read-only and runs before confirmation so a name collision never surprises the user after Yes.
                var appendPlan = ProjectInterchangeAppendOnlyImporter.Plan(project, json);
                var confirmText =
                    "Thêm snapshot semantic vào project hiện tại theo chế độ APPEND-ONLY?\n\n" +
                    "Source project: " + appendPlan.SourceProjectId + "\n" +
                    "Semantic identity mới: " + appendPlan.TotalSemanticIdentitiesToAdd.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Zone: " + appendPlan.ZonesToAdd.ToString(CultureInfo.InvariantCulture) +
                    " • Floor: " + appendPlan.FloorsToAdd.ToString(CultureInfo.InvariantCulture) +
                    " • Family: " + appendPlan.FamiliesToAdd.ToString(CultureInfo.InvariantCulture) +
                    " • Element: " + appendPlan.ElementsToAdd.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Source CAD handles sẽ bỏ: " + appendPlan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) +
                    " • validation warning: " + appendPlan.ValidationWarnings.ToString(CultureInfo.InvariantCulture) + "\n\n" +
                    "Chế độ này chỉ nhận ID/tên semantic mới; không merge/replace dữ liệu đang có.\n" +
                    "Drawing handles/fingerprint nguồn KHÔNG trở thành ownership của DWG đích; generated/native ownership không được tái tạo.\n" +
                    "Project ID, drawing identity và active context hiện tại được giữ nguyên. Imported elements được đánh dấu dirty để review/rebuild sau.\n\n" +
                    "File .qsdb sẽ chưa tự lưu.";
                if (System.Windows.MessageBox.Show(
                        confirmText,
                        "QS3D — Interchange Append",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;

                var currentProject = InterchangeConfirmationGuard.RequireFresh(
                    document,
                    project,
                    previewChangeVersion,
                    "Interchange Append");

                var result = ProjectInterchangeAppendOnlyImporter.Import(currentProject, json);
                InterchangePostMutationUi.RefreshProjectFailClosed(document);

                var status =
                    "Interchange Append: Zone +" + result.ZonesAdded.ToString(CultureInfo.InvariantCulture) +
                    " • Floor +" + result.FloorsAdded.ToString(CultureInfo.InvariantCulture) +
                    " • Family +" + result.FamiliesAdded.ToString(CultureInfo.InvariantCulture) +
                    " • Element +" + result.ElementsAdded.ToString(CultureInfo.InvariantCulture) +
                    " • source handles discarded " + result.SourceHandlesDiscarded.ToString(CultureInfo.InvariantCulture) +
                    " • warning " + result.ValidationWarnings.ToString(CultureInfo.InvariantCulture) +
                    ". Chưa tự lưu .qsdb.";
                FinalizeUi(
                    document,
                    status,
                    "QS3D append-only import chỉ thêm semantic portable state; không phải native CAD merge và không xác nhận source geometry trong DWG hiện tại.");
            }
            catch (Exception ex)
            {
                ReportUi(document, "QS3DINTERCHANGEAPPEND lỗi: " + ex.Message + " Importer rollback semantic mutation nếu apply thất bại.");
            }
        }

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " requires the DWG that started the operation to remain active.");
        }

        private static void FinalizeUi(Document document, string status, string detail)
        {
            try { PaletteCoordinator.SetStatus(status); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + status); } catch { }
            try { document.Editor.WriteMessage("\n" + detail); } catch { }
        }

        private static void ReportUi(Document document, string status)
        {
            try { PaletteCoordinator.SetStatus(status); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + status); } catch { }
        }

        private static string ReadGuardedSnapshotText(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Interchange snapshot path is required.", nameof(path));
            var fullPath = Path.GetFullPath(path);
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > ProjectInterchangeJsonValidator.MaxFileBytes)
                    throw new InvalidDataException("Semantic snapshot exceeds the guarded " + ProjectInterchangeJsonValidator.MaxFileBytes.ToString(CultureInfo.InvariantCulture) + " byte limit.");
                var length = checked((int)stream.Length);
                var bytes = new byte[length];
                var offset = 0;
                while (offset < bytes.Length)
                {
                    var read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read <= 0) throw new EndOfStreamException("Semantic snapshot changed or ended while it was being read.");
                    offset += read;
                }
                if (stream.ReadByte() != -1)
                    throw new InvalidDataException("Semantic snapshot changed while it was being read; reopen the file and retry.");
                try
                {
                    return StrictUtf8.GetString(bytes);
                }
                catch (DecoderFallbackException ex)
                {
                    throw new InvalidDataException("Semantic snapshot is not valid UTF-8.", ex);
                }
            }
        }
    }
}
