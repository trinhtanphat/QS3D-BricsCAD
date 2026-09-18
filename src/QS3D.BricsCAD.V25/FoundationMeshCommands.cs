using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class FoundationMeshCommands
    {
        private const string OperationFailure = "QS3DFOUNDATIONREBAR3D lỗi: không thể tạo/cập nhật thép móng. Kiểm tra selection, project semantic và dữ liệu rebar rồi thử lại.";
        private const string UiSyncWarning = "UI sync warning: đã cập nhật thép móng nhưng đồng bộ giao diện chưa hoàn tất. Dữ liệu CAD/project đã được giữ nguyên; hãy refresh giao diện.";
        private const string CleanupWarning = "Cleanup warning: thép móng đã được commit nhưng giải phóng tài nguyên native chưa hoàn tất; không chạy lại lệnh để tránh tạo trùng.";

        [CommandMethod("QS3DFOUNDATIONREBAR3D", CommandFlags.UsePickSet)]
        public void BuildFoundationMesh3D()
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
                    Report(document, nativeDatabaseIdentity, "Foundation Rebar 3D: chọn Foundation semantic có closed straight plan-view POLYLINE + RebarFoundationXNotation/RebarFoundationYNotation. Rectangle giữ local X/Y; polygon dùng drawing X/Y.");
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
                    Report(document, nativeDatabaseIdentity, "Foundation Rebar 3D: selection không có source handle hợp lệ.");
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, nativeDatabaseIdentity, "Foundation Rebar 3D: BLOCKED • chưa có QS3D project hiện hữu; lệnh không tạo project mới từ selection.");
                    return;
                }

                var previewTargets = ResolveFoundationTargets(previewProject, selectedHandles);
                if (previewTargets.Count == 0)
                {
                    Report(document, nativeDatabaseIdentity, "Foundation Rebar 3D: chọn Foundation semantic có closed straight plan-view POLYLINE + RebarFoundationXNotation/RebarFoundationYNotation. Rectangle giữ local X/Y; polygon dùng drawing X/Y.");
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var expectedTargetIds = new HashSet<string>(previewTargets.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);

                var project = ExistingProjectMutationContext.Require(document, "Foundation Rebar 3D");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("Foundation Rebar 3D: QS3D project đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                var targets = ResolveFoundationTargets(project, selectedHandles);
                if (!expectedTargetIds.SetEquals(targets.Select(x => x.Id)))
                    throw new InvalidOperationException("Foundation Rebar 3D: semantic target set đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);
                var result = FoundationMeshSolidBuilder.BuildSelected(document, project, selectedIds);
                var message = result.Bars == 0
                    ? "Foundation Rebar 3D: chọn Foundation semantic có closed straight plan-view POLYLINE + RebarFoundationXNotation/RebarFoundationYNotation. Rectangle giữ local X/Y; polygon dùng drawing X/Y."
                    : "Foundation Rebar 3D: đã tạo/cập nhật " + result.Bars + " thanh cho " + result.Elements + " móng.";
                FinalizeUi(document, nativeDatabaseIdentity, message, result.PostCommitCleanupWarning);
            }
            catch (Exception)
            {
                Report(document, nativeDatabaseIdentity, OperationFailure);
            }
        }

        private static List<ProjectElement> ResolveFoundationTargets(ProjectState project, HashSet<string> selectedHandles) =>
            project.Elements
                .Where(x => x.Category == ElementCategory.Foundation && x.SourceHandles.Any(selectedHandles.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message, bool postCommitCleanupWarning)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            var publishedMessage = postCommitCleanupWarning ? message + " " + CleanupWarning : message;
            try
            {
                RefreshModelTree(document, nativeDatabaseIdentity);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                document.Editor.Regen();
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                SetPaletteStatusForDocument(document, nativeDatabaseIdentity, publishedMessage);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                document.Editor.WriteMessage("\nQS3D " + publishedMessage);
            }
            catch (Exception)
            {
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                TryWriteMessage(document, nativeDatabaseIdentity, "\nQS3D " + publishedMessage + " " + UiSyncWarning);
            }
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            try { return document.Database.UnmanagedObject; }
            catch { return IntPtr.Zero; }
        }

        private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (nativeDatabaseIdentity == IntPtr.Zero ||
                !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                return false;
            try { return document.Database.UnmanagedObject == nativeDatabaseIdentity; }
            catch { return false; }
        }

        private static void RequireActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                throw new InvalidOperationException("Foundation Rebar 3D document generation changed before geometry mutation.");
        }

        private static void RefreshModelTree(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            PaletteCoordinator.RefreshProject();
        }

        private static void SetPaletteStatusForDocument(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            PaletteCoordinator.SetStatus(message);
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
