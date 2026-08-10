using System;
using System.Globalization;
using System.IO;
using System.Text;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Export;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectInterchangeImportCommands
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        [CommandMethod("QS3DINTERCHANGEIMPORT", CommandFlags.Modal)]
        public void ImportSemanticSnapshotWithPolicy()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Nạp QS3D Semantic Snapshot — chọn policy theo collision",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var json = ReadGuardedSnapshotText(dialog.FileName);
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var preview = ProjectInterchangeImportPreview.Plan(project, json);
                if (!preview.Validation.IsValid)
                    throw new InvalidDataException("Snapshot không vượt qua strict validation/import preview.");

                var keepPlan = ProjectInterchangeKeepTargetImporter.Plan(project, json);
                if (preview.CollisionCount == 0)
                {
                    var appendPlan = ProjectInterchangeAppendOnlyImporter.Plan(project, json);
                    var appendConfirm =
                        "Snapshot không có semantic ID collision. Chạy APPEND-ONLY?\n\n" +
                        "Source project: " + appendPlan.SourceProjectId + "\n" +
                        "Semantic identity mới: " + appendPlan.TotalSemanticIdentitiesToAdd.ToString(CultureInfo.InvariantCulture) + "\n" +
                        "Incoming source handles discard: " + appendPlan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) + "\n\n" +
                        "Không merge/replace, không nhận CAD ownership từ source, không tự lưu .qsdb.";
                    if (System.Windows.MessageBox.Show(
                            appendConfirm,
                            "QS3D — Interchange Import / Append-only",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes) return;

                    EnsureActive(document, "Interchange append-only import");
                    var result = ProjectInterchangeAppendOnlyImporter.Import(project, json);
                    FinishSemanticOnlyImport(
                        document,
                        "Interchange Import / Append-only: semantic +" +
                        (result.ZonesAdded + result.FloorsAdded + result.FamiliesAdded + result.ElementsAdded).ToString(CultureInfo.InvariantCulture) +
                        " • source handles discarded " + result.SourceHandlesDiscarded.ToString(CultureInfo.InvariantCulture) +
                        ". Chưa tự lưu .qsdb.");
                    return;
                }

                InterchangeUseSourceElementImportPlan? useSourcePlan = null;
                string useSourceBlock = string.Empty;
                try
                {
                    var candidate = InterchangeUseSourceElementImportService.Plan(project, json);
                    if (candidate.ElementsToReplace > 0) useSourcePlan = candidate;
                }
                catch (Exception ex)
                {
                    useSourceBlock = ex.Message;
                }

                if (useSourcePlan == null)
                {
                    var keepOnly =
                        "Snapshot có " + preview.CollisionCount.ToString(CultureInfo.InvariantCulture) + " semantic ID collision(s), nhưng không có executable same-category Element replacement.\n\n" +
                        "Policy khả dụng hiện tại: KEEP TARGET.\n" +
                        "Target identity trùng ID giữ nguyên; chỉ semantic identity mới được thêm. Incoming source handles bị discard.\n" +
                        (string.IsNullOrWhiteSpace(useSourceBlock) ? string.Empty : "\nUseSource bị chặn: " + useSourceBlock + "\n") +
                        "\nTiếp tục KeepTarget?";
                    if (System.Windows.MessageBox.Show(
                            keepOnly,
                            "QS3D — Interchange Import / KeepTarget",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;

                    EnsureActive(document, "Interchange KeepTarget import");
                    var keepResult = ProjectInterchangeKeepTargetImporter.Import(project, json);
                    FinishSemanticOnlyImport(
                        document,
                        "Interchange Import / KeepTarget: semantic +" +
                        (keepResult.ZonesAdded + keepResult.FloorsAdded + keepResult.FamiliesAdded + keepResult.ElementsAdded).ToString(CultureInfo.InvariantCulture) +
                        " • target identities kept " + keepResult.TargetIdentitiesKept.ToString(CultureInfo.InvariantCulture) +
                        " • source handles discarded " + keepResult.SourceHandlesDiscarded.ToString(CultureInfo.InvariantCulture) +
                        ". Chưa tự lưu .qsdb.");
                    return;
                }

                var chooseText =
                    "Snapshot có " + preview.CollisionCount.ToString(CultureInfo.InvariantCulture) + " semantic ID collision(s). Chọn policy:\n\n" +
                    "YES — REPLACE ELEMENT SEMANTIC\n" +
                    "• replace same-category Element collisions: " + useSourcePlan.ElementsToReplace.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "• giữ Zone/Floor/Family collision của target\n" +
                    "• giữ target SourceHandles/drawing fingerprint\n" +
                    "• xóa ownership-safe generated outputs của affected closure; rebuild explicit\n\n" +
                    "NO — KEEP TARGET\n" +
                    "• giữ toàn bộ target identities trùng ID: " + keepPlan.TotalSemanticIdentitiesToKeep.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "• chỉ append identities mới\n" +
                    "• không replace target semantic collision\n\n" +
                    "CANCEL — không import.\n\n" +
                    "Cả hai policy đều discard incoming source CAD handles và không tự lưu .qsdb.";

                var choice = System.Windows.MessageBox.Show(
                    chooseText,
                    "QS3D — Interchange Import Policy",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Warning);
                if (choice == System.Windows.MessageBoxResult.Cancel) return;

                if (choice == System.Windows.MessageBoxResult.No)
                {
                    EnsureActive(document, "Interchange KeepTarget import");
                    var keepResult = ProjectInterchangeKeepTargetImporter.Import(project, json);
                    FinishSemanticOnlyImport(
                        document,
                        "Interchange Import / KeepTarget: semantic +" +
                        (keepResult.ZonesAdded + keepResult.FloorsAdded + keepResult.FamiliesAdded + keepResult.ElementsAdded).ToString(CultureInfo.InvariantCulture) +
                        " • target identities kept " + keepResult.TargetIdentitiesKept.ToString(CultureInfo.InvariantCulture) +
                        " • source handles discarded " + keepResult.SourceHandlesDiscarded.ToString(CultureInfo.InvariantCulture) +
                        ". Chưa tự lưu .qsdb.");
                    return;
                }

                EnsureActive(document, "Interchange UseSource element import");
                var sourceResult = InterchangeUseSourceElementImportService.Import(document, json);
                try { PaletteCoordinator.RefreshProject(); } catch { }
                var status =
                    "Interchange Import / UseSource: Element replace " + sourceResult.ElementsReplaced.ToString(CultureInfo.InvariantCulture) +
                    " • Element +" + sourceResult.ElementsAdded.ToString(CultureInfo.InvariantCulture) +
                    " • catalog +" + (sourceResult.ZonesAdded + sourceResult.FloorsAdded + sourceResult.FamiliesAdded).ToString(CultureInfo.InvariantCulture) +
                    " • generated closure invalidated " + sourceResult.GeneratedElementsInvalidated.ToString(CultureInfo.InvariantCulture) +
                    ". Rebuild explicit; chưa tự lưu .qsdb.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEIMPORT lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage("\nQS3DINTERCHANGEIMPORT error: " + ex.Message + " Import policy không được claim thành công nếu apply chưa hoàn tất.");
            }
        }

        private static void FinishSemanticOnlyImport(Document document, string status)
        {
            try { PaletteCoordinator.RefreshProject(); } catch { }
            try { PaletteCoordinator.SetStatus(status); } catch { }
            document.Editor.WriteMessage("\nQS3D " + status);
        }

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " requires the DWG that started the operation to remain active.");
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
                try { return StrictUtf8.GetString(bytes); }
                catch (DecoderFallbackException ex) { throw new InvalidDataException("Semantic snapshot is not valid UTF-8.", ex); }
            }
        }
    }
}
