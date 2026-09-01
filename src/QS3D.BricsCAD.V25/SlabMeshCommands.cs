using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SlabMeshCommands
    {
        [CommandMethod("QS3DSLABREBAR3D", CommandFlags.UsePickSet)]
        public void BuildSlabMesh3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, "Slab Mesh 3D: chọn Slab semantic có closed straight-segment plan-view POLYLINE + RebarSlabXNotation/RebarSlabYNotation. Rectangle giữ local-axis legacy; polygon dùng drawing X/Y.");
                    return;
                }

                var selectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var id in selectedIds)
                {
                    try { selectedHandles.Add(id.Handle.ToString()); }
                    catch { }
                }
                if (selectedHandles.Count == 0)
                {
                    Report(document, "Slab Mesh 3D: selection không có source handle hợp lệ.");
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, "Slab Mesh 3D: BLOCKED • chưa có QS3D project hiện hữu; lệnh không tạo project mới từ selection.");
                    return;
                }

                var previewTargets = ResolveSlabTargets(previewProject, selectedHandles);
                if (previewTargets.Count == 0)
                {
                    Report(document, "Slab Mesh 3D: chọn Slab semantic có closed straight-segment plan-view POLYLINE + RebarSlabXNotation/RebarSlabYNotation. Rectangle giữ local-axis legacy; polygon dùng drawing X/Y.");
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var expectedTargetIds = new HashSet<string>(previewTargets.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);

                var project = ExistingProjectMutationContext.Require(document, "Slab Mesh 3D");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("Slab Mesh 3D: QS3D project đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                var targets = ResolveSlabTargets(project, selectedHandles);
                if (!expectedTargetIds.SetEquals(targets.Select(x => x.Id)))
                    throw new InvalidOperationException("Slab Mesh 3D: semantic target set đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                var result = SlabMeshSolidBuilder.BuildSelected(document, project);
                var message = result.Bars == 0
                    ? "Slab Mesh 3D: chọn Slab semantic có closed straight-segment plan-view POLYLINE + RebarSlabXNotation/RebarSlabYNotation. Rectangle giữ local-axis legacy; polygon dùng drawing X/Y."
                    : "Slab Mesh 3D: đã tạo/cập nhật " + result.Bars + " thanh trên " + result.Elements + " sàn.";
                FinalizeUi(document, message);
            }
            catch (Exception)
            {
                Report(document, "QS3DSLABREBAR3D không thể hoàn tất. Kiểm tra selection/project và thử lại.");
            }
        }

        [CommandMethod("QS3DSLABREBARHEALTH", CommandFlags.Modal)]
        public void SlabMeshHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Slab Mesh Health: BLOCKED • chưa có QS3D project state/sidecar; lệnh kiểm tra không tạo project mới.");
                    return;
                }

                var handles = new List<string>();
                foreach (var element in project.Elements)
                {
                    if (!element.Properties.TryGetValue("GeneratedSlabMeshHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                    handles.AddRange(raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0));
                }
                var live = CadHandleService.GetLiveSolidHandles(document, handles.Distinct(StringComparer.OrdinalIgnoreCase));
                var issues = new GeneratedSlabMeshHealthService().Inspect(project, live);
                var summary = new HealthSummary(issues);
                var message = "Slab Mesh Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                Report(document, message);
                foreach (var issue in issues.Take(50))
                    TryWriteMessage(document, "\n  [" + issue.Severity + "] " + issue.Code + " • " + issue.ElementId + " • " + issue.Message);
                if (issues.Count > 50) TryWriteMessage(document, "\n  … health output truncated.");
            }
            catch (Exception)
            {
                Report(document, "QS3DSLABREBARHEALTH không thể hoàn tất kiểm tra. Project/native geometry không bị thay đổi.");
            }
        }

        private static List<ProjectElement> ResolveSlabTargets(ProjectState project, HashSet<string> selectedHandles) =>
            project.Elements
                .Where(x => x.Category == ElementCategory.Slab && x.SourceHandles.Any(selectedHandles.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static void FinalizeUi(Document document, string message)
        {
            var uiSyncFailed = false;
            try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }
            try { document.Editor.Regen(); } catch { uiSyncFailed = true; }
            try { PaletteCoordinator.SetStatus(message); } catch { uiSyncFailed = true; }
            TryWriteMessage(document, "\nQS3D " + message);
            if (uiSyncFailed)
                TryWriteMessage(document, "\nQS3D Slab Mesh 3D: native update đã hoàn tất; một phần UI không thể đồng bộ.");
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
            TryWriteMessage(document, "\nQS3D " + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); }
            catch { }
        }
    }
}
