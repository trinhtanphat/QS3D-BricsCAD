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
    public sealed class ProjectInterchangeUseSourceCatalogCommands
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        [CommandMethod("QS3DINTERCHANGEUSESOURCECATALOG", CommandFlags.Modal)]
        public void ImportSourceCatalogSemanticData()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Nạp QS3D Snapshot — dùng Zone/Floor/Family semantic từ source",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var json = ReadGuardedSnapshotText(dialog.FileName);
<<<<<<< HEAD
                var validation = ProjectInterchangeJsonValidator.Validate(json);
                if (!validation.IsValid)
                    throw new InvalidDataException("Snapshot is not valid QS3D semantic interchange JSON.");
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                    throw new InvalidOperationException("Interchange UseSource Catalog stopped because the active DWG changed after file selection.");
                var project = ProjectContextCoordinator.GetOrCreate(document);
=======
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    const string blocked = "Interchange UseSource Catalog: target drawing chưa có QS3D project để replace. Dùng QS3DINTERCHANGEIMPORT để import vào target mới/trống.";
                    try { PaletteCoordinator.SetStatus(blocked); } catch { }
                    document.Editor.WriteMessage("\nQS3D " + blocked);
                    return;
                }

>>>>>>> origin/main
                var previewChangeVersion = project.ChangeVersion;
                var plan = InterchangeUseSourceCatalogImportService.Plan(project, json);
                var replacements = plan.ZonesToReplace + plan.FloorsToReplace + plan.FamiliesToReplace;
                if (replacements <= 0)
                {
                    var none = "Interchange UseSource Catalog: snapshot không có Zone/Floor/Family ID collision để replace. Dùng QS3DINTERCHANGEIMPORT cho policy khác.";
                    try { PaletteCoordinator.SetStatus(none); } catch { }
                    document.Editor.WriteMessage("\nQS3D " + none);
                    return;
                }

                var confirm =
                    "Dùng semantic catalog từ snapshot cho Zone/Floor/Family trùng ID?\n\n" +
                    "Source project: " + plan.SourceProjectId + "\n" +
                    "Zone replace: " + plan.ZonesToReplace.ToString(CultureInfo.InvariantCulture) +
                    " • Floor replace: " + plan.FloorsToReplace.ToString(CultureInfo.InvariantCulture) +
                    " • Family replace: " + plan.FamiliesToReplace.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Catalog mới: " + (plan.ZonesToAdd + plan.FloorsToAdd + plan.FamiliesToAdd).ToString(CultureInfo.InvariantCulture) +
                    " • Element mới: " + plan.ElementsToAdd.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Element collision giữ target: " + plan.ElementCollisionsKept.ToString(CultureInfo.InvariantCulture) +
                    " • target element trực tiếp bị ảnh hưởng: " + plan.AffectedExistingElements.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Incoming source handles discard: " + plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) + "\n\n" +
                    "QUAN TRỌNG:\n" +
                    "• Zone: thay tên theo source.\n" +
                    "• Floor: thay tên + elevation theo source.\n" +
                    "• Family: cùng category, thay tên + portable properties theo source.\n" +
                    "• Element trùng ID KHÔNG bị replace trong policy này.\n" +
                    "• Element đang tham chiếu catalog bị đổi + transitive dependents/opening host sẽ invalidated generated ownership trong CAD transaction.\n" +
                    "• Không nhận incoming source CAD handles làm ownership; không tự QS3DBUILD3D/cut/rebar/curtain/save.\n\n" +
                    "Nếu native/semantic validation lỗi trước CAD commit, DWG transaction abort và project semantic rollback.";

                if (System.Windows.MessageBox.Show(
                        confirm,
                        "QS3D — Interchange UseSource Catalog",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;

                var confirmedProject = InterchangeConfirmationGuard.RequireFresh(
                    document,
                    project,
                    previewChangeVersion,
                    "Interchange UseSource Catalog");
                var result = InterchangeUseSourceCatalogImportService.Import(document, confirmedProject, json);
                try { PaletteCoordinator.RefreshProject(); } catch { }
                var status =
                    "Interchange UseSource Catalog: Zone " + result.ZonesReplaced.ToString(CultureInfo.InvariantCulture) +
                    " • Floor " + result.FloorsReplaced.ToString(CultureInfo.InvariantCulture) +
                    " • Family " + result.FamiliesReplaced.ToString(CultureInfo.InvariantCulture) +
                    " replaced • Element collision kept " + result.ElementCollisionsKept.ToString(CultureInfo.InvariantCulture) +
                    " • generated closure invalidated " + result.GeneratedElementsInvalidated.ToString(CultureInfo.InvariantCulture) +
                    ". Rebuild explicit; chưa tự lưu .qsdb.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
                document.Editor.WriteMessage("\nQS3D " + status);
                document.Editor.WriteMessage("\nQS3D chạy QS3DHEALTHALL và rebuild explicit các output cần dùng trước khi tin cậy generated geometry sau catalog replacement.");
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEUSESOURCECATALOG lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage("\nQS3DINTERCHANGEUSESOURCECATALOG error: " + ex.Message + " Không claim catalog import thành công nếu CAD/semantic apply chưa commit.");
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
