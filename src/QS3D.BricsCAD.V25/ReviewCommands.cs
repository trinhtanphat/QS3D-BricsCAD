using System;
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
        [CommandMethod("QS3DBUILD3D", CommandFlags.UsePickSet)]
        public void Build3D()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DBUILD3D", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc); var snapshots = EntitySnapshotReader.ReadImpliedSelection(doc);
                var handles = snapshots.Select(x => x.Handle).ToArray();
                var tracked = project.Elements.FirstOrDefault(x => x.SourceHandles.Any(h => handles.Contains(h, StringComparer.OrdinalIgnoreCase)));
                ElementCategory? category = tracked?.Category;
                if (!category.HasValue && project.Metadata.TryGetValue("ActiveFamilyId", out var familyId)) category = project.FindFamily(familyId)?.Category;
                if (!category.HasValue) { doc.Editor.WriteMessage("\nQS3D: chọn source CAD hoặc Family trước khi Vẽ 3D."); return; }
                if (snapshots.Count == 0) { doc.Editor.WriteMessage("\nQS3D: chọn source CAD cần tạo/cập nhật 3D."); return; }
                if (tracked == null) SemanticCaptureService.Capture(doc, category.Value);
                int solids;
                if (category == ElementCategory.ArchitecturalWall) solids = WallSolidBuilder.BuildSelectedLineWalls(doc, project);
                else if (StructuralSolidBuilder.Supports(category.Value)) solids = StructuralSolidBuilder.BuildSelected(doc, project, category.Value);
                else { PaletteCoordinator.SetStatus("Vẽ 3D native hiện hỗ trợ Tường KT, Dầm, Sàn, Cột, Vách BTCT và Móng."); return; }
                var regenerated = Regenerate(project); PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus("3D " + category + ": " + solids + " solid • regenerate " + regenerated + " lượt.");
                doc.Editor.WriteMessage("\nQS3D 3D " + category + ": " + solids + " solid(s)."); doc.SendStringToExecute("QS3DVIEW3D ", true, false, false);
            });
        }

        [CommandMethod("QS3DBBSVIEW", CommandFlags.Modal)]
        public void ShowBbs()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DBBSVIEW", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc); Regenerate(project); var rows = ProjectRebarScheduleBuilder.Build(project);
                if (rows.Count == 0) { doc.Editor.WriteMessage("\nQS3D BBS: chưa có cấu kiện khai báo RebarNotation."); return; }
                Action<RebarScheduleRow> locate = row => { var element = project.FindElement(row.ElementId); if (element == null) return; var count = CadHandleService.Select(doc, element.SourceHandles); PaletteCoordinator.SetStatus("BBS Locate " + row.BarMark + " • " + count + " CAD object"); if (count > 0) doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false); };
                var fileName = (string.IsNullOrWhiteSpace(doc.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(doc.Name)) + "-BBS.xlsx";
                Application.ShowModelessWindow(IntPtr.Zero, new RebarScheduleWindow(rows, locate, fileName), true);
            });
        }

        [CommandMethod("QS3DRECOGNIZE", CommandFlags.UsePickSet)] public void Recognize() => RecognizeInternal(false);
        [CommandMethod("QS3DRECOGNIZEAUTO", CommandFlags.UsePickSet)] public void RecognizeAuto() => RecognizeInternal(true);
        [CommandMethod("QS3DB4D", CommandFlags.UsePickSet)] public void ScanB4DWorkflow() => RecognizeInternal(true);
        private static void RecognizeInternal(bool autoApply)
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, autoApply ? "QS3DRECOGNIZEAUTO" : "QS3DRECOGNIZE", () =>
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(doc); if (snapshots.Count == 0) return;
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var batch = new ProjectRecognitionService().SuggestBatch(project, snapshots); var applied = 0; var skipped = 0;
                Action<RecognitionResult> apply = result =>
                {
                    var candidate = result.TopCandidate; if (candidate == null) return;
                    var collision = project.Elements.FirstOrDefault(x => x.Category != candidate.Category && x.SourceHandles.Any(h => string.Equals(h, result.Handle, StringComparison.OrdinalIgnoreCase)));
                    if (collision != null) throw new InvalidOperationException("CAD handle " + result.Handle + " đã thuộc " + collision.Category + ".");
                    if (!SemanticCaptureService.CaptureSnapshot(doc, result.Snapshot, candidate.Category)) return;
                    applied++;
                    AuditTrail.ForProject(project).Record("recognition.apply", candidate.Category.ToString().ToUpperInvariant() + "-" + result.Handle, candidate.RuleId + " • confidence " + candidate.Confidence.ToString("0.000") + " • " + candidate.EvidenceText);
                    PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus("Nhận dạng → " + candidate.Category + " • " + result.Handle);
                };
                Action<RecognitionResult> locate = result => { var count = CadHandleService.Select(doc, new[] { result.Handle }); if (count > 0) doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false); };
                if (autoApply) foreach (var result in batch.AutoAccepted) try { apply(result); } catch { skipped++; }
                Application.ShowModelessWindow(IntPtr.Zero, new RecognitionWindow(batch.Results, apply, locate), true);
                doc.Editor.WriteMessage("\nQS3D Recognition: " + snapshots.Count + " object(s), auto=" + applied + ", review=" + batch.ReviewRequired.Count + ", skipped=" + skipped + ".");
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
                Action<QuantityRevisionRow> locate = row => { var element = project.FindElement(row.ElementId); if (element == null) return; var count = CadHandleService.Select(doc, element.SourceHandles); if (count > 0) doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false); };
                Application.ShowModelessWindow(IntPtr.Zero, new RevisionWindow(before, after, rows, locate), true);
                AuditTrail.ForProject(project).Record("revision.compare", string.Empty, before.Id + " → " + after.Id + " • " + rows.Count + " quantity changes");
                PaletteCoordinator.SetStatus("Revision diff: " + rows.Count + " thay đổi quantity.");
            });
        }

        private static int Regenerate(ProjectState project) => new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Guard(Document document, string operation, Action action) { try { action(); } catch (System.Exception ex) { document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message); PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); } }
    }
}
