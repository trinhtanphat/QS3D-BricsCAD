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
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var plan = ProjectInterchangeSourceHandleProvenance.Plan(project, json);
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
                var result = ProjectInterchangeSourceHandleProvenance.Store(project, json);
                try { PaletteCoordinator.RefreshProject(); } catch { }

                var status =
                    "Interchange provenance-only: source " + result.SourceProjectId +
                    " • elements " + result.ElementsStored.ToString(CultureInfo.InvariantCulture) +
                    " • handles " + result.SourceHandlesStored.ToString(CultureInfo.InvariantCulture) +
                    ". Không thay semantic/native ownership; chưa tự lưu .qsdb.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEPROVENANCE lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage("\nQS3DINTERCHANGEPROVENANCE error: " + ex.Message + " Không claim provenance đã lưu nếu operation chưa hoàn tất.");
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
