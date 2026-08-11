using System;
using System.Globalization;
using System.IO;
using System.Text;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Export;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectInterchangeRemapAppendCommands
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        [CommandMethod("QS3DINTERCHANGEREMAPAPPEND", CommandFlags.Modal)]
        public void ImportSnapshotAsNewSemanticIdentities()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "QS3D Semantic Snapshot — Import As New với deterministic remap",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var json = ReadGuardedSnapshotText(dialog.FileName);
                EnsureActive(document, "Interchange Import As New / preview");
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var previewChangeVersion = project.ChangeVersion;
                var plan = ProjectInterchangeRemapAppendImporter.Plan(project, json);
                if (!plan.CanImport)
                {
                    var blocked =
                        "Interchange Import As New BLOCKED: " + plan.BlockerCount.ToString(CultureInfo.InvariantCulture) +
                        " blocker(s) — opaque ID/ref " + plan.Remap.OpaqueReferenceWarnings.Count.ToString(CultureInfo.InvariantCulture) +
                        ", runtime compatibility " + plan.CompatibilityBlockers.Count.ToString(CultureInfo.InvariantCulture) +
                        ". Chạy QS3DINTERCHANGEREMAPPLAN để xem chi tiết; chưa mutate project/DWG.";
                    try { PaletteCoordinator.SetStatus(blocked); } catch { }
                    document.Editor.WriteMessage("\nQS3D " + blocked);
                    return;
                }

                if (plan.IdRemapCount == 0 && plan.NameRemapCount == 0)
                {
                    var appendInstead =
                        "Snapshot không cần ID/name remap. Dùng QS3DINTERCHANGEAPPEND cho append-only chuẩn; QS3DINTERCHANGEREMAPAPPEND không mutate trong trường hợp này.";
                    try { PaletteCoordinator.SetStatus(appendInstead); } catch { }
                    document.Editor.WriteMessage("\nQS3D " + appendInstead);
                    return;
                }

                var confirm =
                    "IMPORT AS NEW toàn bộ snapshot bằng deterministic remap?\n\n" +
                    "Source project: " + plan.Remap.SourceProjectId + "\n" +
                    "Semantic identities: " + plan.Remap.IdentityCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "ID remap: " + plan.IdRemapCount.ToString(CultureInfo.InvariantCulture) +
                    " • name remap: " + plan.NameRemapCount.ToString(CultureInfo.InvariantCulture) +
                    " • typed reference rewrites: " + plan.ReferenceRewriteCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Incoming source handles discard: " + plan.SourceHandleCount.ToString(CultureInfo.InvariantCulture) +
                    " • native/generated owner properties discard: " + plan.OwnershipPropertiesToDiscard.ToString(CultureInfo.InvariantCulture) + "\n\n" +
                    "QUAN TRỌNG:\n" +
                    "• Existing target Zone/Floor/Family/Element KHÔNG bị replace hoặc rename.\n" +
                    "• Incoming identities được append dưới candidate ID/name deterministic; typed FamilyId/FloorId/ZoneId/DependsOn/HostWallId được rewrite theo plan.\n" +
                    "• Property ID/ref chưa có rewrite policy hoặc dữ liệu vượt runtime capacity/property limits sẽ BLOCK; command không đoán relation và không truncate semantic data.\n" +
                    "• SourceHandles, drawing fingerprint và Generated*/PhysicalOpeningCut*/handle owner metadata không trở thành CAD ownership của DWG target.\n" +
                    "• Đây là semantic-only import: không tạo native geometry, không QS3DBUILD3D/cut/rebar/curtain/grid và không tự lưu .qsdb.\n" +
                    "• Importer re-plan ngay trước mutation và dùng ProjectStateSnapshot rollback nếu semantic apply/validation lỗi.\n\n" +
                    "Tiếp tục Import As New?";

                if (System.Windows.MessageBox.Show(
                        confirm,
                        "QS3D — Interchange Import As New",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;

                EnsureActive(document, "Interchange Import As New / mutation");
                var currentProject = ProjectContextCoordinator.GetOrCreate(document);
                if (!ReferenceEquals(currentProject, project) || currentProject.ChangeVersion != previewChangeVersion)
                    throw new InvalidOperationException(
                        "Interchange Import As New target semantic project changed after preview. Run the command again to review a fresh remap plan.");

                var result = ProjectInterchangeRemapAppendImporter.Import(currentProject, json);
                try { PaletteCoordinator.RefreshProject(); } catch { }

                var status =
                    "Interchange Import As New: Zone +" + result.ZonesAdded.ToString(CultureInfo.InvariantCulture) +
                    " • Floor +" + result.FloorsAdded.ToString(CultureInfo.InvariantCulture) +
                    " • Family +" + result.FamiliesAdded.ToString(CultureInfo.InvariantCulture) +
                    " • Element +" + result.ElementsAdded.ToString(CultureInfo.InvariantCulture) +
                    " • ID remap " + result.IdsRemapped.ToString(CultureInfo.InvariantCulture) +
                    " • name remap " + result.NamesRemapped.ToString(CultureInfo.InvariantCulture) +
                    " • refs rewritten " + result.ReferencesRewritten.ToString(CultureInfo.InvariantCulture) +
                    " • source handles discarded " + result.SourceHandlesDiscarded.ToString(CultureInfo.InvariantCulture) +
                    ". Semantic-only; rebuild/save explicit.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEREMAPAPPEND lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage("\nQS3DINTERCHANGEREMAPAPPEND error: " + ex.Message + " Không claim Import As New thành công nếu semantic apply chưa hoàn tất.");
            }
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
