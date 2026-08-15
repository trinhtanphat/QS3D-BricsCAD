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
using QS3D.Core.Persistence;
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
                if (!ProjectContextCoordinator.TryGetReadOnly(doc, out var project))
                {
                    doc.Editor.WriteMessage("\nQS3D BBS: chưa có QS3D project hiện hữu; viewer không tạo project mới.");
                    return;
                }
                var previewProject = ProjectStateSnapshot.CreateDetachedCopy(project);
                Regenerate(previewProject);
                var rows = ProjectRebarScheduleBuilder.Build(previewProject);
                if (rows.Count == 0) { doc.Editor.WriteMessage("\nQS3D BBS: chưa có cấu kiện khai báo RebarNotation."); return; }
                Action<RebarScheduleRow> locate = row =>
                {
                    var count = LocateCurrentElement(doc, row.ElementId, "BBS Locate");
                    PaletteCoordinator.SetStatus("BBS Locate " + row.BarMark + " • " + count + " CAD object");
                };
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
                var snapshots = scanCurrentSpace ? EntitySnapshotReader.ReadCurrentSpace(doc) : EntitySnapshotReader.ReadCurrentSelection(doc);
                snapshots = snapshots.Where(x => !x.HasQs3dGeneratedOwnershipMarker).ToList();
                string? expectedProjectId = null;
                if (scanCurrentSpace && ProjectContextCoordinator.TryGetReadOnly(doc, out var previewProject))
                {
                    expectedProjectId = previewProject.ProjectId;
                    var generatedHandles = CollectGeneratedHandles(previewProject);
                    snapshots = snapshots.Where(x => !generatedHandles.Contains(x.Handle)).ToList();
                }
                if (snapshots.Count == 0) { doc.Editor.WriteMessage("\nQS3D: Current Space không có đối tượng CAD nguồn để quét."); return; }
                if (!DrawingUnitWorkflow.EnsureResolved(doc, operation)) return;

                ProjectState project;
                if (expectedProjectId != null)
                {
                    project = ExistingProjectMutationContext.Require(doc, operation + " recognition");
                    if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(operation + ": QS3D project đã thay đổi trong lúc quét CAD source. Hãy chạy lại lệnh.");
                }
                else
                {
                    project = ProjectContextCoordinator.GetOrCreate(doc);
                }

                var reviewProjectId = project.ProjectId;
                var batch = new ProjectRecognitionService().SuggestBatch(project, snapshots);
                var applied = 0;
                var skipped = 0;

                Func<IReadOnlyList<RecognitionResult>, bool, int> apply = (results, requireLiveConfidence) =>
                {
                    var plan = RecognitionApplyBatchService.PrepareStrict(
                        doc,
                        reviewProjectId,
                        results,
                        requireAutoAcceptance: requireLiveConfidence);
                    var committed = RecognitionApplyBatchService.Commit(doc, reviewProjectId, plan);
                    applied += committed;
                    if (committed > 0)
                    {
                        try
                        {
                            PaletteCoordinator.RefreshProject();
                            PaletteCoordinator.SetStatus(
                                requireLiveConfidence
                                    ? "Nhận dạng chắc chắn: đã áp dụng atomically " + committed + " đối tượng sau live confidence recheck."
                                    : "Nhận dạng: đã áp dụng atomically " + committed + " đối tượng.");
                        }
                        catch (System.Exception uiError)
                        {
                            doc.Editor.WriteMessage("\nQS3D Recognition batch đã commit; UI refresh warning: " + uiError.Message);
                        }
                    }
                    return committed;
                };

                Action<RecognitionResult> locate = result =>
                {
                    var count = CadHandleService.Select(doc, new[] { result.Handle });
                    if (count == 0) throw new InvalidOperationException("Recognition Locate: CAD handle " + result.Handle + " không còn tồn tại. Hãy chạy lại Recognition.");
                    doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                };

                if (autoApply)
                {
                    var autoPlan = RecognitionApplyBatchService.PrepareBestEffort(doc, reviewProjectId, batch.AutoAccepted);
                    skipped = autoPlan.SkippedCount;
                    foreach (var skip in autoPlan.Skips)
                        doc.Editor.WriteMessage("\nQS3D Recognition skip " + skip.Handle + ": " + skip.Reason);

                    try
                    {
                        var autoCommitted = RecognitionApplyBatchService.Commit(doc, reviewProjectId, autoPlan);
                        applied += autoCommitted;
                        if (autoCommitted > 0)
                        {
                            try
                            {
                                PaletteCoordinator.RefreshProject();
                                PaletteCoordinator.SetStatus("Nhận dạng auto: đã áp dụng atomically " + autoCommitted + " đối tượng; preflight skip " + skipped + ".");
                            }
                            catch (System.Exception uiError)
                            {
                                doc.Editor.WriteMessage("\nQS3D Recognition auto batch đã commit; UI refresh warning: " + uiError.Message);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nQS3D Recognition auto batch rolled back: " + ex.Message);
                        PaletteCoordinator.SetStatus("Nhận dạng auto: batch đã rollback, không giữ partial semantic capture. " + ex.Message);
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
                var project = ExistingProjectMutationContext.Require(doc, "Revision baseline");
                Regenerate(project);
                var path = RevisionCoordinator.CaptureBaseline(doc);
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
                if (!ProjectContextCoordinator.TryGetReadOnly(doc, out _))
                    throw new InvalidOperationException("Revision diff cần một QS3D project hiện hữu; review không tạo project mới.");
                var before = RevisionCoordinator.LoadBaseline(doc);
                var after = RevisionCoordinator.CaptureCurrent(doc);
                var rows = new QuantityRevisionReport().Build(before, after);
                Action<QuantityRevisionRow> locate = row => LocateCurrentElement(doc, row.ElementId, "Revision Locate");
                Application.ShowModelessWindow(IntPtr.Zero, new RevisionWindow(doc, before, after, rows, locate), true);
                PaletteCoordinator.SetStatus("Revision diff: " + rows.Count + " thay đổi quantity.");
            });
        }

        private static int LocateCurrentElement(Document document, string elementId, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + ": hãy kích hoạt lại đúng bản vẽ nguồn trước khi định vị.");
            if (string.IsNullOrWhiteSpace(elementId))
                throw new InvalidOperationException(operation + ": dòng review không có ElementId hợp lệ.");

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject))
                throw new InvalidOperationException(operation + ": QS3D project hiện hành không còn khả dụng. Hãy làm mới bảng review.");
            var element = currentProject.FindElement(elementId)
                ?? throw new InvalidOperationException(operation + ": cấu kiện " + elementId + " không còn tồn tại trong project hiện tại. Hãy làm mới bảng review.");
            var handles = SourceHandleResolver.Resolve(currentProject, new[] { element.Id });
            if (handles.Count == 0)
                throw new InvalidOperationException(operation + ": cấu kiện " + element.Id + " không còn CAD source handle hợp lệ trong project hiện tại.");

            var count = CadHandleService.Select(document, handles);
            if (count == 0)
                throw new InvalidOperationException(operation + ": CAD source của cấu kiện " + element.Id + " không còn tồn tại trong bản vẽ hiện tại.");
            document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
            return count;
        }

        private static HashSet<string> CollectGeneratedHandles(ProjectState project) =>
            new HashSet<string>(GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project), StringComparer.OrdinalIgnoreCase);

        private static int Regenerate(ProjectState project) => new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Guard(Document document, string operation, Action action) { try { action(); } catch (System.Exception ex) { document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message); PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); } }
    }
}
