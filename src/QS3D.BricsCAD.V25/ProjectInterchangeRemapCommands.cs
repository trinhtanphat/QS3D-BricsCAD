using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Export;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectInterchangeRemapCommands
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        [CommandMethod("QS3DINTERCHANGEREMAPPLAN", CommandFlags.Modal)]
        public void PreviewImportAsNewRemap()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "QS3D Semantic Snapshot — dry-run Import As New remap",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var json = ReadGuardedSnapshotText(dialog.FileName);
                EnsureActive(document, "Interchange remap dry-run");
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var appendPlan = ProjectInterchangeRemapAppendImporter.Plan(project, json);
                var plan = appendPlan.Remap;

                document.Editor.WriteMessage(
                    "\nQS3D Interchange Remap PLAN ONLY — source=" + plan.SourceProjectId +
                    " • identities=" + plan.IdentityCount.ToString(CultureInfo.InvariantCulture) +
                    " • ID remap=" + plan.IdRemapCount.ToString(CultureInfo.InvariantCulture) +
                    " • name remap=" + plan.NameRemapCount.ToString(CultureInfo.InvariantCulture) +
                    " • typed reference rewrites=" + plan.ReferenceRewrites.Count.ToString(CultureInfo.InvariantCulture) +
                    " • unresolved opaque refs=" + plan.OpaqueReferenceWarnings.Count.ToString(CultureInfo.InvariantCulture) +
                    " • runtime compatibility blockers=" + appendPlan.CompatibilityBlockers.Count.ToString(CultureInfo.InvariantCulture) +
                    " • append-as-new=" + (appendPlan.CanImport ? "READY" : "BLOCKED") + ".");

                foreach (var item in plan.Items.Where(x => x.IdChanged || x.NameChanged).Take(40))
                {
                    var namePart = item.NameChanged
                        ? " • name '" + item.SourceName + "' -> '" + item.TargetName + "'"
                        : string.Empty;
                    document.Editor.WriteMessage(
                        "\n  " + item.Kind + " " + item.SourceId + " -> " + item.TargetId + namePart + " • " + item.Reason);
                }
                if (plan.Items.Count(x => x.IdChanged || x.NameChanged) > 40)
                    document.Editor.WriteMessage("\n  ... remap list truncated in command line; planner remains deterministic for the full snapshot.");

                foreach (var rewrite in plan.ReferenceRewrites.Take(40))
                {
                    var key = string.IsNullOrWhiteSpace(rewrite.PropertyKey) ? rewrite.ReferenceKind : rewrite.PropertyKey;
                    document.Editor.WriteMessage(
                        "\n  REF " + rewrite.OwnerElementSourceId + " / " + key + ": " + rewrite.SourceReferenceId + " -> " + rewrite.TargetReferenceId);
                }
                if (plan.ReferenceRewrites.Count > 40)
                    document.Editor.WriteMessage("\n  ... typed reference rewrite list truncated in command line.");

                foreach (var warning in plan.OpaqueReferenceWarnings.Take(20))
                    document.Editor.WriteMessage("\n  BLOCK REF " + warning.OwnerElementSourceId + " / " + warning.PropertyKey + ": " + warning.Reason);
                if (plan.OpaqueReferenceWarnings.Count > 20)
                    document.Editor.WriteMessage("\n  ... unresolved property-reference warning list truncated in command line.");

                foreach (var blocker in appendPlan.CompatibilityBlockers.Take(20))
                    document.Editor.WriteMessage("\n  BLOCK RUNTIME " + blocker.OwnerKind + " " + blocker.OwnerSourceId + " / " + blocker.Field + ": " + blocker.Reason);
                if (appendPlan.CompatibilityBlockers.Count > 20)
                    document.Editor.WriteMessage("\n  ... runtime compatibility blocker list truncated in command line.");

                var status = appendPlan.CanImport
                    ? "Interchange remap dry-run READY: candidate IDs/names + typed references + target runtime compatibility đã resolve. Chưa mutate project; chưa import."
                    : "Interchange remap dry-run BLOCKED: còn property-reference hoặc target runtime compatibility blocker. Chưa mutate project; chưa import.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEREMAPPLAN lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage("\nQS3DINTERCHANGEREMAPPLAN error: " + ex.Message + " Dry-run không mutate project/DWG.");
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
