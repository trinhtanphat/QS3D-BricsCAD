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
        [CommandMethod("QS3DBUILD3D", CommandFlags.UsePickSet)]
        public void Build3D()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DBUILD3D", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var snapshots = EntitySnapshotReader.ReadImpliedSelection(doc);
                if (snapshots.Count == 0) { doc.Editor.WriteMessage("\nQS3D: chọn source CAD cần tạo/cập nhật 3D."); return; }

                var handles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
                var tracked = project.Elements
                    .Where(x => x.SourceHandles.Any(handles.Contains))
                    .ToList();
                var trackedCategories = tracked
                    .Select(x => x.Category)
                    .Distinct()
                    .ToList();
                if (trackedCategories.Count > 1)
                {
                    var categories = string.Join(", ", trackedCategories.OrderBy(x => x.ToString()).Select(x => x.ToString()));
                    throw new InvalidOperationException("Selection đang trộn nhiều semantic category (" + categories + "). Chọn một category mỗi lần trước khi Vẽ 3D.");
                }

                ElementCategory? category = trackedCategories.Count == 1 ? trackedCategories[0] : null;
                if (!category.HasValue && project.Metadata.TryGetValue("ActiveFamilyId", out var familyId)) category = project.FindFamily(familyId)?.Category;
                if (!category.HasValue) { doc.Editor.WriteMessage("\nQS3D: chọn source CAD hoặc Family trước khi Vẽ 3D."); return; }

                if (tracked.Count > 0)
                {
                    var trackedHandles = new HashSet<string>(tracked.SelectMany(x => x.SourceHandles), StringComparer.OrdinalIgnoreCase);
                    var untracked = snapshots.Where(x => !trackedHandles.Contains(x.Handle)).Select(x => x.Handle).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    if (untracked.Count > 0)
                        throw new InvalidOperationException("Selection đang trộn source đã capture với " + untracked.Count + " source chưa capture. Capture/chọn cùng một semantic category trước khi Vẽ 3D để tránh xử lý một phần.");
                }
                else
                {
                    SemanticCaptureService.Capture(doc, category.Value);
                }

                int solids;
                var detailSolids = 0;
                if (IsTktWall(category.Value))
                {
                    solids = category.Value == ElementCategory.WallPier
                        ? WallPierProfileSolidBuilder.BuildSelectedLinePiers(doc, project)
                        : WallSolidBuilder.BuildSelectedLineWalls(doc, project, category.Value);
                    solids += PolylineWallSolidBuilder.BuildSelected(doc, project, category.Value);
                    if (category.Value == ElementCategory.GlassWall)
                    {
                        var curtain = CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(doc, project);
                        detailSolids = curtain.Frames;
                    }
                }
                else if (StructuralSolidBuilder.Supports(category.Value)) solids = StructuralSolidBuilder.BuildSelected(doc, project, category.Value);
                else { PaletteCoordinator.SetStatus("Vẽ 3D native hiện hỗ trợ Tường KT (Gạch/Kính/Trụ), Dầm, Sàn, Cột, Vách BTCT, Móng, Cầu thang, Lan can và Đào đất."); return; }
                if (solids == 0 && detailSolids == 0)
                {
                    var hint = GeometryHint(category.Value);
                    PaletteCoordinator.SetStatus("Không tạo được solid cho " + category + ". " + hint);
                    doc.Editor.WriteMessage("\nQS3D 3D: không có source tương thích. " + hint);
                    return;
                }
                var regenerated = Regenerate(project); PaletteCoordinator.RefreshProject();
                var detailText = detailSolids > 0 ? " • curtain frame " + detailSolids : string.Empty;
                PaletteCoordinator.SetStatus("3D " + category + ": " + solids + " host/solid" + detailText + " • regenerate " + regenerated + " lượt.");
                doc.Editor.WriteMessage("\nQS3D 3D " + category + ": " + solids + " host/solid(s)" + detailText + "."); doc.SendStringToExecute("QS3DVIEW3D ", true, false, false);
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
                Action<RebarScheduleRow> locate = row => { var element = project.FindElement(row.ElementId); if (element == null) return; var count = CadHandleService.Select(doc, SourceHandleResolver.Resolve(project, new[] { element.Id })); PaletteCoordinator.SetStatus("BBS Locate " + row.BarMark + " • " + count + " CAD object"); if (count > 0) doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false); };
                var fileName = (string.IsNullOrWhiteSpace(doc.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(doc.Name)) + "-BBS.xlsx";
                Application.ShowModelessWindow(IntPtr.Zero, new RebarScheduleWindow(rows, locate, fileName), true);
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
                    var collision = project.Elements.FirstOrDefault(x => x.Category != candidate.Category && x.SourceHandles.Any(h => string.Equals(h, result.Handle, StringComparison.OrdinalIgnoreCase)));
                    if (collision != null) throw new InvalidOperationException("CAD handle " + result.Handle + " đã thuộc " + collision.Category + ".");
                    if (!SemanticCaptureService.CaptureSnapshot(doc, result.Snapshot, candidate.Category)) return;
                    applied++;
                    AuditTrail.ForProject(project).Record("recognition.apply", candidate.Category.ToString().ToUpperInvariant() + "-" + result.Handle, candidate.RuleId + " • confidence " + candidate.Confidence.ToString("0.000") + " • " + candidate.EvidenceText);
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
                Application.ShowModelessWindow(IntPtr.Zero, new RecognitionWindow(batch.Results, apply, locate), true);
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
                Application.ShowModelessWindow(IntPtr.Zero, new RevisionWindow(before, after, rows, locate), true);
                AuditTrail.ForProject(project).Record("revision.compare", string.Empty, before.Id + " → " + after.Id + " • " + rows.Count + " quantity changes");
                PaletteCoordinator.SetStatus("Revision diff: " + rows.Count + " thay đổi quantity.");
            });
        }

        private static HashSet<string> CollectGeneratedHandles(ProjectState project) =>
            new HashSet<string>(GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project), StringComparer.OrdinalIgnoreCase);

        private static string GeometryHint(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Beam:
                case ElementCategory.StructuralWall:
                case ElementCategory.Railing:
                    return "Dùng LINE làm tim cấu kiện.";
                case ElementCategory.Slab:
                case ElementCategory.Column:
                case ElementCategory.Foundation:
                case ElementCategory.Stair:
                case ElementCategory.Earthwork:
                    return "Dùng closed POLYLINE làm footprint.";
                case ElementCategory.WallPier:
                    return "Dùng LINE hoặc open POLYLINE plan-view cho profile Rectangular/Chamfered của Trụ Tường; bulge được tessellate và bend nội bộ giữ miter/bevel guard.";
                case ElementCategory.GlassWall:
                    return "Dùng LINE để dựng host kính + mullion/transom frame; open POLYLINE vẫn dựng generic host GlassWall.";
                case ElementCategory.ArchitecturalWall:
                    return "Dùng LINE hoặc open plan-view POLYLINE làm tim Tường KT; bulge được tessellate trước khi tạo footprint.";
                default:
                    return "Source CAD hiện chưa có native solid adapter.";
            }
        }

        private static bool IsTktWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall || category == ElementCategory.GlassWall || category == ElementCategory.WallPier;

        private static int Regenerate(ProjectState project) => new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Guard(Document document, string operation, Action action) { try { action(); } catch (System.Exception ex) { document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message); PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); } }
    }
}
