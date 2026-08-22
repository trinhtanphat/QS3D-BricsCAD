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
    public sealed class ProjectInterchangeProvenanceCommands
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        [CommandMethod("QS3DINTERCHANGEPROVENANCE", CommandFlags.Modal)]
        public void StoreSourceHandleProvenance()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Nạp provenance source CAD từ QS3D Semantic Snapshot",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var json = ReadGuardedSnapshotText(dialog.FileName);
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var reviewProject))
                    throw new InvalidOperationException("Interchange provenance cần một QS3D project hiện hữu; bước review không tạo project mới.");
                var reviewProjectId = reviewProject.ProjectId;
                var reviewUpdatedUtc = reviewProject.UpdatedUtc;
                var reviewChangeVersion = reviewProject.ChangeVersion;
                var reviewDrawingFingerprint = reviewProject.DrawingFingerprint ?? string.Empty;
                var plan = ProjectInterchangeSourceHandleProvenance.Plan(reviewProject, json);
                var confirm =
                    "Lưu source CAD handles của snapshot dưới dạng PROVENANCE-ONLY?\n\n" +
                    "Source project: " + plan.SourceProjectId + "\n" +
                    "Element có source handles: " + plan.ElementsWithHandles.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Source handles: " + plan.SourceHandleCount.ToString(CultureInfo.InvariantCulture) + "\n\n" +
                    "Lệnh này KHÔNG import/replace Zone, Floor, Family hoặc Element semantic.\n" +
                    "Lệnh này KHÔNG ghi handle vào ProjectElement.SourceHandles, không tạo Generated* owner slot và không nhận native CAD ownership trong DWG hiện tại.\n" +
                    "Provenance được lưu trong project metadata để truy vết nguồn; QS3D Semantic Snapshot export không re-export metadata này như source ownership.";

                if (System.Windows.MessageBox.Show(
                        confirm,
                        "QS3D — Interchange Provenance Only",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Information) != System.Windows.MessageBoxResult.Yes) return;

                EnsureActive(document, "Interchange provenance import");
                var project = ExistingProjectMutationContext.Require(document, "Interchange provenance import");
                if (!string.Equals(project.ProjectId, reviewProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.UpdatedUtc != reviewUpdatedUtc ||
                    project.ChangeVersion != reviewChangeVersion ||
                    !string.Equals(project.DrawingFingerprint ?? string.Empty, reviewDrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Interchange provenance: project đã thay đổi sau bước review. Hãy mở lại snapshot và xác nhận lại trước khi lưu provenance.");

                var result = ProjectInterchangeSourceHandleProvenance.Store(project, json);
                var status =
                    "Interchange provenance-only: source " + result.SourceProjectId +
                    " • elements " + result.ElementsStored.ToString(CultureInfo.InvariantCulture) +
                    " • handles " + result.SourceHandlesStored.ToString(CultureInfo.InvariantCulture) +
                    ". Không thay semantic/native ownership; chưa tự lưu .qsdb.";
                FinalizeUi(document, status);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DINTERCHANGEPROVENANCE lỗi: " + ex.Message + " Không claim provenance đã lưu nếu operation chưa hoàn tất.");
            }
        }

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " requires the DWG that started the operation to remain active.");
        }

        private static void FinalizeUi(Document document, string status)
        {
            try
            {
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception uiError)
            {
                try { document.Editor.WriteMessage("\nQS3D Interchange provenance đã commit; UI sync warning: " + uiError.Message); } catch { }
            }
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + message); } catch { }
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
