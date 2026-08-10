using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;
using QS3D.Core.Recognition;
using QS3D.Core.Revisions;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ReviewCommands
    {
        [CommandMethod("QS3DBBSVIEW", CommandFlags.Modal)]
        public void ShowBbs()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DBBSVIEW", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc); Regenerate(project); var rows = ProjectRebarScheduleBuilder.Build(project);
                if (rows.Count == 0) { doc.Editor.WriteMessage("\nQS3D BBS: chưa có cấu kiện khai báo RebarNotation."); return; }
                Action<RebarScheduleRow> locate = row => { var element = project.FindElement(row.ElementId); if (element == null) return; var count = CadHandleService.Select(doc, SourceHandleResolver.Resolve(project, new[] { element.Id })); PaletteCoordinator.SetStatus("BBS Locate " + row.BarMark + " • " + count + " CAD object"); if (count > 0) doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false); };
                var fileName = (string.IsNullOrWhiteSpace(doc.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(doc.Name)) + "-BBS.xlsx";
                Application.ShowModelessWindow(IntPtr.Zero, new RebarScheduleWindow(doc, rows, locate, fileName), true);
            });
        }

        [CommandMethod("QS3DRECOGNIZE", CommandFlags.UsePickSet)] public void Recognize() => RecognizeInternal(false, false);
        [CommandMethod("QS3DRECOGNIZEAUTO", CommandFlags.UsePickSet)] public void RecognizeAuto() => RecognizeInternal(true, false);
        [CommandMethod("QS3DB4D", CommandFlags.Modal)] public void ScanB4DWorkflow() => RecognizeInternal(true, true);
        private static void RecognizeInternal(bool autoApply, bool scanCurrentSpace)
        {
            var doc = Active(); if (doc == null) return;
            var operation = scanCurrentSpace ? "QS3DB4D" : autoApply ? "QS3DRECOGNIZEAUTO" : "QS3DRECOGNIZE";
            Guard(doc, operation, () =>
            {
                if (!DrawingUnitWorkflow.EnsureResolved(doc, operation)) return;
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var snapshots = scanCurrentSpace ? EntitySnapshotReader.ReadCurrentSpace(doc) : EntitySnapshotReader.ReadCurrentSelection(doc);
                if (scanCurrentSpace)
                {
                    var generatedHandles = CollectGeneratedHandles(project);
                    snapshots = snapshots.Where(x => !generatedHandles.Contains(x.Handle)).ToList();
                }
                if (snapshots.Count == 0) { doc.Editor.WriteMessage("\nQS3D: Current Space không có đối tượng CAD nguồn để quét."); return; }
                var batch = new ProjectRecognitionService().SuggestBatch(project, snapshots); var applied = 0; var skipped = 0;
                Action<RecognitionResult> apply = result =>
                {
                    var candidate = result.TopCandidate; if (candidate == null) return;
                    var collision = SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, result.Handle);
                    if (collision != null && collision.Category == candidate.Category) collision = null;
                    if (collision != null) throw new InvalidOperationException("CAD handle " + result.Handle + " đã thuộc " + collision.Category + ".");
                    if (!SemanticCaptureService.CaptureSnapshot(doc, result.Snapshot, candidate.Category)) return;
                    var captured = SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, result.Handle);
                    if (captured == null || captured.Category != candidate.Category)
                        throw new InvalidOperationException("Recognition capture did not produce one matching semantic owner for CAD handle " + result.Handle + ".");
                    applied++;
                    AuditTrail.ForProject(project).Record("recognition.apply", captured.Id, candidate.RuleId + " • confidence " + candidate.Confidence.ToString("0.000") + " • " + candidate.EvidenceText);
                    PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus("Nhận dạng → " + candidate.Category + " • " + result.Handle);
                };
                Action<RecognitionResult> locate = result => { var count = CadHandleService.Select(doc, new[] { result.Handle }); if (count > 0) doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false); };
                if (autoApply)
                {
                    foreach (var result in batch.AutoAccepted)
                    {
                        try { apply(result); }
                        catch (System.Exception ex)
                        {
                            skipped++;
                            AuditTrail.ForProject(project).Record("recognition.skip", result.Handle, ex.Message);
                            doc.Editor.WriteMessage("\nQS3D Recognition skip " + result.Handle + ": " + ex.Message);
                        }
                    }
                }
                Application.ShowModelessWindow(IntPtr.Zero, new RecognitionWindow(doc, batch.Results, apply, locate), true);
                doc.Editor.WriteMessage("\nQS3D " + (scanCurrentSpace ? "B4D" : "Recognition") + ": scanned=" + snapshots.Count + ", auto=" + applied + ", review=" + batch.ReviewRequired.Count + ", skipped=" + skipped + ".");
            });
        }

        [CommandMethod("QS3DREVBASE", CommandFlags.Modal)]
        public void RevisionBaseline()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DREVBASE", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc); Regenerate(project); var path = RevisionCoordinator.CaptureBaseline(doc);
                AuditTrail.ForProject(project).Record("revision.baseline", string.Empty, path);
                PaletteCoordinator.SetStatus("Đã lưu baseline revision: " + path); doc.Editor.WriteMessage("\nQS3D revision baseline saved: " + path);
            });
        }

        [CommandMethod("QS3DREVDIFF", CommandFlags.Modal)]
        public void RevisionDiff()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DREVDIFF", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc); Regenerate(project); var before = RevisionCoordinator.LoadBaseline(doc); var after = RevisionCoordinator.CaptureCurrent(doc); var rows = new QuantityRevisionReport().Build(before, after);
                Action<QuantityRevisionRow> locate = row => { var element = project.FindElement(row.ElementId); if (element == null) return; var count = CadHandleService.Select(doc, SourceHandleResolver.Resolve(project, new[] { element.Id })); if (count > 0) doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false); };
                Application.ShowModelessWindow(IntPtr.Zero, new RevisionWindow(doc, before, after, rows, locate), true);
                AuditTrail.ForProject(project).Record("revision.compare", string.Empty, before.Id + " → " + after.Id + " • " + rows.Count + " quantity changes");
                PaletteCoordinator.SetStatus("Revision diff: " + rows.Count + " thay đổi quantity.");
            });
        }

        private static HashSet<string> CollectGeneratedHandles(ProjectState project) =>
            new HashSet<string>(GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project), StringComparer.OrdinalIgnoreCase);

        private static int Regenerate(ProjectState project) => new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Guard(Document document, string operation, Action action) { try { action(); } catch (System.Exception ex) { document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message); PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); } }
    }
}
