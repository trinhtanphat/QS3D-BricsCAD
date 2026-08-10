using System;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Export;
using QS3D.Core.Rebar;
using QS3D.Core.Recognition;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class DomainExtensionsCommands
    {
        [CommandMethod("QS3DRECOGNIZE", CommandFlags.UsePickSet)] public void Recognize() => RecognizeInternal(false);
        [CommandMethod("QS3DRECOGNIZEAUTO", CommandFlags.UsePickSet)] public void RecognizeAuto() => RecognizeInternal(true);

        [CommandMethod("QS3DSTRUCTSOLID", CommandFlags.UsePickSet)]
        public void StructuralSolid()
        {
            var doc = Active();
            if (doc == null) return;
            Guard(doc, "QS3DSTRUCTSOLID", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var count = StructuralSolidBuilder.BuildSelected(doc, project);
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus("Structural 3D: " + count + " solid mới.");
                doc.Editor.WriteMessage("\nQS3D structural solids: " + count + " created.");
            });
        }

        [CommandMethod("QS3DBBSCSV", CommandFlags.Modal)]
        public void BbsCsv()
        {
            var doc = Active();
            if (doc == null) return;
            Guard(doc, "QS3DBBSCSV", () =>
            {
                var rows = ProjectRebarScheduleBuilder.Build(ProjectContextCoordinator.GetOrCreate(doc));
                var dialog = new SaveFileDialog { Title = "Xuất BBS CSV", Filter = "CSV UTF-8 (*.csv)|*.csv", FileName = "QS3D-BBS.csv", AddExtension = true, DefaultExt = ".csv" };
                if (dialog.ShowDialog() != true) return;
                RebarCsvExporter.Export(dialog.FileName, rows);
                PaletteCoordinator.SetStatus("Đã xuất " + rows.Count + " dòng BBS CSV.");
            });
        }

        [CommandMethod("QS3DREVBASE", CommandFlags.Modal)]
        public void RevisionBaseline()
        {
            var doc = Active();
            if (doc == null) return;
            Guard(doc, "QS3DREVBASE", () =>
            {
                var path = RevisionCoordinator.CaptureBaseline(doc);
                PaletteCoordinator.SetStatus("Đã chốt revision baseline: " + path);
                doc.Editor.WriteMessage("\nQS3D revision baseline: " + path);
            });
        }

        [CommandMethod("QS3DREVDIFF", CommandFlags.Modal)]
        public void RevisionDiff()
        {
            var doc = Active();
            if (doc == null) return;
            Guard(doc, "QS3DREVDIFF", () =>
            {
                var deltas = RevisionCoordinator.Compare(doc, out var before, out var after);
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                Action<string> locate = id =>
                {
                    var element = project.FindElement(id);
                    if (element != null) CadHandleService.Select(doc, element.SourceHandles);
                };
                Application.ShowModelessWindow(IntPtr.Zero, new RevisionWindow(before, after, deltas, locate), true);
                PaletteCoordinator.SetStatus("Revision diff: " + deltas.Count + " element thay đổi.");
            });
        }

        private static void RecognizeInternal(bool autoApply)
        {
            var doc = Active();
            if (doc == null) return;
            Guard(doc, autoApply ? "QS3DRECOGNIZEAUTO" : "QS3DRECOGNIZE", () =>
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(doc);
                var batch = new RecognitionEngine().SuggestBatch(snapshots);
                var applied = 0;
                Action<RecognitionResult> apply = result =>
                {
                    var candidate = result.TopCandidate;
                    if (candidate == null) return;
                    if (SemanticCaptureService.CaptureSnapshot(doc, result.Snapshot, candidate.Category))
                    {
                        applied++;
                        PaletteCoordinator.RefreshProject();
                        PaletteCoordinator.SetStatus("Nhận dạng → " + candidate.Category + " • " + result.Handle);
                    }
                };
                Action<RecognitionResult> locate = result => CadHandleService.Select(doc, new[] { result.Handle });
                if (autoApply)
                    foreach (var result in batch.AutoAccepted) apply(result);
                Application.ShowModelessWindow(IntPtr.Zero, new RecognitionWindow(batch.Results, apply, locate), true);
                doc.Editor.WriteMessage("\nQS3D Recognition: " + snapshots.Count + " object(s), auto=" + applied + ", review=" + batch.ReviewRequired.Count + ".");
            });
        }

        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Guard(Document document, string operation, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message);
                PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message);
            }
        }
    }
}
