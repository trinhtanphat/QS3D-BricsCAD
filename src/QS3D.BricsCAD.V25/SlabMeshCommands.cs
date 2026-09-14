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
        private const string UiSyncWarning = "Native update đã hoàn tất; một phần UI không thể đồng bộ.";
        private const string CleanupWarning = "Native update đã commit; cleanup host phát sinh cảnh báo.";

        [CommandMethod("QS3DSLABREBAR3D", CommandFlags.UsePickSet)]
        public void BuildSlabMesh3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try
            {
                var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, nativeDatabaseIdentity, "Slab Mesh 3D: chọn Slab semantic có closed straight-segment plan-view POLYLINE + RebarSlabXNotation/RebarSlabYNotation. Rectangle giữ local-axis legacy; polygon dùng drawing X/Y.");
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
                    Report(document, nativeDatabaseIdentity, "Slab Mesh 3D: selection không có source handle hợp lệ.");
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, nativeDatabaseIdentity, "Slab Mesh 3D: BLOCKED • chưa có QS3D project hiện hữu; lệnh không tạo project mới từ selection.");
                    return;
                }

                var previewTargets = ResolveSlabTargets(previewProject, selectedHandles);
                if (previewTargets.Count == 0)
                {
                    Report(document, nativeDatabaseIdentity, "Slab Mesh 3D: chọn Slab semantic có closed straight-segment plan-view POLYLINE + RebarSlabXNotation/RebarSlabYNotation. Rectangle giữ local-axis legacy; polygon dùng drawing X/Y.");
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

                RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);
                var result = SlabMeshSolidBuilder.BuildSelected(document, project);
                var message = result.Bars == 0
                    ? "Slab Mesh 3D: chọn Slab semantic có closed straight-segment plan-view POLYLINE + RebarSlabXNotation/RebarSlabYNotation. Rectangle giữ local-axis legacy; polygon dùng drawing X/Y."
                    : "Slab Mesh 3D: đã tạo/cập nhật " + result.Bars + " thanh trên " + result.Elements + " sàn.";
                FinalizeUi(document, nativeDatabaseIdentity, message, result.PostCommitCleanupWarning);
            }
            catch (Exception)
            {
                Report(document, nativeDatabaseIdentity, "QS3DSLABREBAR3D không thể hoàn tất. Kiểm tra selection/project và thử lại.");
            }
        }

        [CommandMethod("QS3DSLABREBARHEALTH", CommandFlags.Modal)]
        public void SlabMeshHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, nativeDatabaseIdentity, "Slab Mesh Health: BLOCKED • chưa có QS3D project state/sidecar; lệnh kiểm tra không tạo project mới.");
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
                Report(document, nativeDatabaseIdentity, message);
                foreach (var issue in issues.Take(50))
                    TryWriteMessage(document, nativeDatabaseIdentity, "\n  [" + issue.Severity + "] " + issue.Code + " • " + issue.ElementId + " • " + issue.Message);
                if (issues.Count > 50) TryWriteMessage(document, nativeDatabaseIdentity, "\n  … health output truncated.");
            }
            catch (Exception)
            {
                Report(document, nativeDatabaseIdentity, "QS3DSLABREBARHEALTH không thể hoàn tất kiểm tra. Project/native geometry không bị thay đổi.");
            }
        }

        private static List<ProjectElement> ResolveSlabTargets(ProjectState project, HashSet<string> selectedHandles) =>
            project.Elements
                .Where(x => x.Category == ElementCategory.Slab && x.SourceHandles.Any(selectedHandles.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message, bool postCommitCleanupWarning)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            var visibleMessage = postCommitCleanupWarning ? message + " " + CleanupWarning : message;
            var uiSyncFailed = false;

            try
            {
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                PaletteCoordinator.RefreshProject();
            }
            catch { uiSyncFailed = true; }

            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try { document.Editor.Regen(); }
            catch { uiSyncFailed = true; }

            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try { PaletteCoordinator.SetStatus(visibleMessage); }
            catch { uiSyncFailed = true; }

            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            TryWriteMessage(document, nativeDatabaseIdentity, "\nQS3D " + visibleMessage);
            if (uiSyncFailed)
                TryWriteMessage(document, nativeDatabaseIdentity, "\nQS3D Slab Mesh 3D: " + UiSyncWarning);
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            try
            {
                return document.Database.UnmanagedObject;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (nativeDatabaseIdentity == IntPtr.Zero ||
                !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                return false;

            try
            {
                return document.Database.UnmanagedObject == nativeDatabaseIdentity;
            }
            catch
            {
                return false;
            }
        }

        private static void RequireActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                throw new InvalidOperationException("Slab Mesh 3D document generation changed before geometry mutation.");
        }

        private static void Report(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
            TryWriteMessage(document, nativeDatabaseIdentity, "\nQS3D " + message);
        }

        private static void TryWriteMessage(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try { document.Editor.WriteMessage(message); }
            catch { }
        }
    }
}
