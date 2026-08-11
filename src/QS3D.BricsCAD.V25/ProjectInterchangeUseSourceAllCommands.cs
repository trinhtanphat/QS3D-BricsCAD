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
    public sealed class ProjectInterchangeUseSourceAllCommands
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        [CommandMethod("QS3DINTERCHANGEUSESOURCEALL", CommandFlags.Modal)]
        public void ImportAllSourceSemanticData()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Nạp QS3D Snapshot — Replace ALL semantic collisions từ source",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var json = ReadGuardedSnapshotText(dialog.FileName);
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var previewChangeVersion = project.ChangeVersion;
                var plan = InterchangeUseSourceAllImportService.Plan(project, json);
                var replacements = plan.ZonesToReplace + plan.FloorsToReplace + plan.FamiliesToReplace + plan.ElementsToReplace;
                if (replacements <= 0)
                {
                    var none = "Interchange UseSource ALL: snapshot không có semantic ID collision để replace. Dùng QS3DINTERCHANGEIMPORT cho append/policy khác.";
                    try { PaletteCoordinator.SetStatus(none); } catch { }
                    document.Editor.WriteMessage("\nQS3D " + none);
                    return;
                }

                var confirm =
                    "REPLACE ALL executable semantic collisions bằng dữ liệu source trong MỘT CAD TRANSACTION?\n\n" +
                    "Source project: " + plan.SourceProjectId + "\n" +
                    "Zone replace: " + plan.ZonesToReplace.ToString(CultureInfo.InvariantCulture) +
                    " • Floor: " + plan.FloorsToReplace.ToString(CultureInfo.InvariantCulture) +
                    " • Family: " + plan.FamiliesToReplace.ToString(CultureInfo.InvariantCulture) +
                    " • Element: " + plan.ElementsToReplace.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "New semantic identities: " + (plan.ZonesToAdd + plan.FloorsToAdd + plan.FamiliesToAdd + plan.ElementsToAdd).ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Existing target elements trực tiếp bị ảnh hưởng: " + plan.AffectedExistingElements.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Incoming source handles discard: " + plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) +
                    " • target source handles preserve: " + plan.TargetSourceHandlesToPreserve.ToString(CultureInfo.InvariantCulture) + "\n\n" +
                    "QUAN TRỌNG:\n" +
                    "• Zone/Floor/Family collisions dùng source semantic definitions.\n" +
                    "• Same-category Element collisions dùng source portable semantic state.\n" +
                    "• Existing target Element SourceHandles + drawing fingerprint được giữ nguyên; incoming handles không trở thành ownership của DWG này.\n" +
                    "• Union affected closure + transitive dependents + opening hosts được invalidated ownership-safely trước mutation.\n" +
                    "• Toàn bộ Catalog + Element replacement dùng MỘT ProjectStateSnapshot và MỘT native CAD transaction; không sequential partial-commit.\n" +
                    "• Không tự QS3DBUILD3D/cut/rebar/curtain/grid/save; rebuild explicit sau khi kiểm tra health.\n\n" +
                    "Nếu validation/mutation lỗi trước native commit, DWG transaction abort và semantic project rollback.";

                if (System.Windows.MessageBox.Show(
                        confirm,
                        "QS3D — Interchange UseSource ALL",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;

                InterchangeConfirmationGuard.RequireFresh(
                    document,
                    project,
                    previewChangeVersion,
                    "Interchange UseSource ALL");
                var result = InterchangeUseSourceAllImportService.Import(document, json);
                try { PaletteCoordinator.RefreshProject(); } catch { }
                var status =
                    "Interchange UseSource ALL: Zone " + result.ZonesReplaced.ToString(CultureInfo.InvariantCulture) +
                    " • Floor " + result.FloorsReplaced.ToString(CultureInfo.InvariantCulture) +
                    " • Family " + result.FamiliesReplaced.ToString(CultureInfo.InvariantCulture) +
                    " • Element " + result.ElementsReplaced.ToString(CultureInfo.InvariantCulture) +
                    " replaced • generated closure invalidated " + result.GeneratedElementsInvalidated.ToString(CultureInfo.InvariantCulture) +
                    " • target source handles preserved " + result.TargetSourceHandlesPreserved.ToString(CultureInfo.InvariantCulture) +
                    ". Rebuild explicit; chưa tự lưu .qsdb.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
                document.Editor.WriteMessage("\nQS3D " + status);
                document.Editor.WriteMessage("\nQS3D chạy QS3DHEALTHALL, inspect semantic/source ownership rồi rebuild explicit các output cần dùng.");
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEUSESOURCEALL lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage("\nQS3DINTERCHANGEUSESOURCEALL error: " + ex.Message + " Không claim all-scope import thành công nếu native/semantic apply chưa commit.");
            }
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
