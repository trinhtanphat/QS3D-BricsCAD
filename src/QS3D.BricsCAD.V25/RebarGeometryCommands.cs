using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RebarGeometryCommands
    {
        private const string SelectionGuidance = "Rebar 3D: chọn Column semantic có closed rectangle POLYLINE + RebarNotation.";
        private const string OperationFailure = "QS3DREBAR3D lỗi: không thể tạo/cập nhật thép dọc cột. Kiểm tra selection, project semantic và dữ liệu rebar rồi thử lại.";
        private const string UiSyncWarning = "UI sync warning: đã cập nhật thép dọc cột nhưng đồng bộ giao diện chưa hoàn tất. Dữ liệu CAD/project đã được giữ nguyên; hãy refresh giao diện.";

        [CommandMethod("QS3DREBAR3D", CommandFlags.UsePickSet)]
        public void BuildRebar3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try
            {
                // Capture PICKFIRST once before canonical project binding. The same
                // ObjectId snapshot is passed through semantic admission and native build.
                var selectedIds = CadSelectionGuard.ReadImpliedSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, nativeDatabaseIdentity, SelectionGuidance);
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
                    Report(document, nativeDatabaseIdentity, "Rebar 3D: selection không có source handle hợp lệ.");
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, nativeDatabaseIdentity, "Rebar 3D: BLOCKED • chưa có QS3D project hiện hữu; lệnh không tạo project mới từ selection.");
                    return;
                }

                var previewTargets = ResolveColumnTargets(previewProject, selectedHandles);
                if (previewTargets.Count == 0)
                {
                    Report(document, nativeDatabaseIdentity, SelectionGuidance);
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var expectedTargetIds = new HashSet<string>(previewTargets.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);

                var project = ExistingProjectMutationContext.Require(document, "Rebar 3D");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("Rebar 3D: QS3D project đã thay đổi sau khi đọc PICKFIRST selection; hãy chọn lại target.");

                var targets = ResolveColumnTargets(project, selectedHandles);
                if (!expectedTargetIds.SetEquals(targets.Select(x => x.Id)))
                    throw new InvalidOperationException("Rebar 3D: semantic Column target set đã thay đổi sau khi đọc PICKFIRST selection; hãy chọn lại target.");

                RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);
                var outcome = ColumnRebarSolidBuilder.BuildSelected(document, project, selectedIds);
                var count = outcome.Count;
                var message = count == 0
                    ? SelectionGuidance
                    : "Rebar 3D: đã tạo/cập nhật " + count + " thanh đứng cho cột được chọn.";
                FinalizeUi(document, nativeDatabaseIdentity, message, outcome.PostCommitCleanupWarning);
            }
            catch (Exception)
            {
                Report(document, nativeDatabaseIdentity, OperationFailure);
            }
        }

        private static List<ProjectElement> ResolveColumnTargets(ProjectState project, HashSet<string> selectedHandles) =>
            project.Elements
                .Where(x => x.Category == ElementCategory.Column && x.SourceHandles.Any(selectedHandles.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message, bool postCommitCleanupWarning)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            var visibleMessage = postCommitCleanupWarning ? message + " " + UiSyncWarning : message;
            try
            {
                RefreshModelTree(document, nativeDatabaseIdentity);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                document.Editor.Regen();
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                TrySetPaletteStatus(document, nativeDatabaseIdentity, visibleMessage);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                document.Editor.WriteMessage("\nQS3D " + visibleMessage);
            }
            catch (Exception)
            {
                TryWriteMessage(document, nativeDatabaseIdentity, "\nQS3D " + message + " " + UiSyncWarning);
            }
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
                throw new InvalidOperationException("Column Rebar 3D document generation changed before geometry mutation.");
        }

        private static void RefreshModelTree(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            PaletteCoordinator.RefreshProject();
        }

        private static void TrySetPaletteStatus(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            PaletteCoordinator.SetStatus(message);
        }

        private static void Report(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            TrySetPaletteStatus(document, nativeDatabaseIdentity, message);
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
