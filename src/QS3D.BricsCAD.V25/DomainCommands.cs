using System;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;
using QS3D.Core.Recognition;
using QS3D.Core.Revisions;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class DomainCommands
    {
        [CommandMethod("QS3DBEAM", CommandFlags.UsePickSet)] public void Beam() => Capture(ElementCategory.Beam, "Dầm", true);
        [CommandMethod("QS3DSLAB", CommandFlags.UsePickSet)] public void Slab() => Capture(ElementCategory.Slab, "Sàn kết cấu", true);
        [CommandMethod("QS3DCOLUMN", CommandFlags.UsePickSet)] public void Column() => Capture(ElementCategory.Column, "Cột", true);
        [CommandMethod("QS3DSTRUCTWALL", CommandFlags.UsePickSet)] public void StructuralWall() => Capture(ElementCategory.StructuralWall, "Vách kết cấu", true);
        [CommandMethod("QS3DFOUNDATION", CommandFlags.UsePickSet)] public void Foundation() => Capture(ElementCategory.Foundation, "Móng", true);
        [CommandMethod("QS3DEARTHWORK", CommandFlags.UsePickSet)] public void Earthwork() => Capture(ElementCategory.Earthwork, "Đào đắp", false);
        [CommandMethod("QS3DREBAR", CommandFlags.UsePickSet)] public void Rebar() => Capture(ElementCategory.Rebar, "Cốt thép", false);

        [CommandMethod("QS3DBBS", CommandFlags.Modal)] public void Bbs() { var doc = Active(); if (doc == null) return; Guard(doc, "QS3DBBS", () => { var project = ProjectContextCoordinator.GetOrCreate(doc); var rows = new RebarScheduleBuilder().Build(project); Action<RebarScheduleRow> locate = row => { var handles = row.ElementIds.SelectMany(id => project.FindElement(id)?.SourceHandles ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase); var count = CadHandleService.Select(doc, handles); PaletteCoordinator.SetStatus("BBS Locate: " + count + " CAD object"); }; Application.ShowModelessWindow(IntPtr.Zero, new RebarScheduleWindow(rows, locate), true); PaletteCoordinator.SetStatus("BBS: " + rows.Count + " dòng."); }); }
        [CommandMethod("QS3DRECOGNIZE", CommandFlags.UsePickSet)] public void Recognize() => RecognizeInternal(false);
        [CommandMethod("QS3DRECOGNIZEAUTO", CommandFlags.UsePickSet)] public void RecognizeAuto() => RecognizeInternal(true);
        private static void RecognizeInternal(bool autoApply)
        {
            var doc = Active(); if (doc == null) return; Guard(doc, autoApply ? "QS3DRECOGNIZEAUTO" : "QS3DRECOGNIZE", () => { var snapshots = EntitySnapshotReader.ReadCurrentSelection(doc); var batch = new RecognitionEngine().SuggestBatch(snapshots); var applied = 0; Action<RecognitionResult> apply = result => { var candidate = result.TopCandidate; if (candidate == null) return; if (SemanticCaptureService.CaptureSnapshot(doc, result.Snapshot, candidate.Category)) { applied++; PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus("Nhận dạng → " + candidate.Category + " • " + result.Handle); } }; Action<RecognitionResult> locate = result => CadHandleService.Select(doc, new[] { result.Handle }); if (autoApply) foreach (var result in batch.AutoAccepted) apply(result); Application.ShowModelessWindow(IntPtr.Zero, new RecognitionWindow(batch.Results, apply, locate), true); doc.Editor.WriteMessage("\nQS3D Recognition: " + snapshots.Count + " object(s), auto=" + applied + ", review=" + batch.ReviewRequired.Count + "."); });
        }
        [CommandMethod("QS3DREGEN", CommandFlags.Modal)] public void Regenerate() { var doc = Active(); if (doc == null) return; Guard(doc, "QS3DREGEN", () => { var project = ProjectContextCoordinator.GetOrCreate(doc); var ok = 0; var failed = 0; foreach (var element in project.Elements) { try { SemanticCaptureService.RegenerateElement(project, element); ok++; } catch { failed++; } } project.Touch(); PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus("Regenerate: " + ok + " OK • " + failed + " cần kiểm tra"); doc.Editor.WriteMessage("\nQS3D regenerate: " + ok + " OK, " + failed + " failed."); }); }
        [CommandMethod("QS3DREVBASE", CommandFlags.Modal)] public void RevisionBaseline() { var doc = Active(); if (doc == null) return; Guard(doc, "QS3DREVBASE", () => { var path = RevisionCoordinator.CaptureBaseline(doc); PaletteCoordinator.SetStatus("Đã lưu baseline revision: " + path); doc.Editor.WriteMessage("\nQS3D revision baseline saved: " + path); }); }
        [CommandMethod("QS3DREVDIFF", CommandFlags.Modal)] public void RevisionDiff() { var doc = Active(); if (doc == null) return; Guard(doc, "QS3DREVDIFF", () => { var before = RevisionCoordinator.LoadBaseline(doc); var after = RevisionCoordinator.CaptureCurrent(doc); var rows = new QuantityRevisionReport().Build(before, after); var project = ProjectContextCoordinator.GetOrCreate(doc); Action<QuantityRevisionRow> locate = row => { var element = project.FindElement(row.ElementId); if (element != null) CadHandleService.Select(doc, element.SourceHandles); }; Application.ShowModelessWindow(IntPtr.Zero, new RevisionWindow(before, after, rows, locate), true); PaletteCoordinator.SetStatus("Revision diff: " + rows.Count + " thay đổi quantity."); }); }

        private static void Capture(ElementCategory category, string label, bool build3d)
        {
            var doc = Active(); if (doc == null) return; Guard(doc, "QS3D " + label, () => { var count = SemanticCaptureService.Capture(doc, category); var solids = build3d ? StructuralSolidBuilder.BuildSelected(doc, ProjectContextCoordinator.GetOrCreate(doc), category) : 0; PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus(label + ": " + count + " semantic" + (build3d ? " • " + solids + " solid 3D" : string.Empty)); doc.Editor.WriteMessage("\nQS3D " + label + ": " + count + " element(s)" + (build3d ? ", " + solids + " solid(s)." : ".")); });
        }
        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Guard(Document document, string operation, Action action) { try { action(); } catch (Exception ex) { document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message); PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); } }
    }
}
