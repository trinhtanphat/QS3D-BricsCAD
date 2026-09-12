using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class BeamRebarCommands
    {
        private const string SelectionGuidance = "Cốt thép 3D Dầm: chọn LINE đã capture thành Beam và khai báo RebarNotation; top/bottom có thể đặt bằng RebarBeamTopCount/RebarBeamBottomCount.";
        private const string OperationFailure = "QS3DBEAMREBAR3D lỗi: không thể tạo/cập nhật thép dọc dầm. Kiểm tra selection, project semantic và dữ liệu rebar rồi thử lại.";
        private const string UiSyncWarning = "UI sync warning: đã cập nhật thép dọc dầm nhưng đồng bộ giao diện chưa hoàn tất. Dữ liệu CAD/project đã được giữ nguyên; hãy refresh giao diện.";

        [CommandMethod("QS3DBEAMREBAR3D", CommandFlags.UsePickSet)]
        public void BuildBeamRebar3D()
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
                    Report(document, nativeDatabaseIdentity, "Cốt thép 3D Dầm: selection không có source handle hợp lệ.");
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, nativeDatabaseIdentity, "Cốt thép 3D Dầm: BLOCKED • chưa có QS3D project hiện hữu; lệnh không tạo project mới từ selection.");
                    return;
                }

                var previewTargets = ResolveBeamTargets(previewProject, selectedHandles);
                if (previewTargets.Count == 0)
                {
                    Report(document, nativeDatabaseIdentity, SelectionGuidance);
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var expectedTargetIds = new HashSet<string>(previewTargets.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);

                var project = ExistingProjectMutationContext.Require(document, "Beam Rebar 3D");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("Beam Rebar 3D: QS3D project đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                var targets = ResolveBeamTargets(project, selectedHandles);
                if (!expectedTargetIds.SetEquals(targets.Select(x => x.Id)))
                    throw new InvalidOperationException("Beam Rebar 3D: semantic Beam target set đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);
                var count = BeamRebarSolidBuilder.BuildSelected(document, project, selectedIds);
                var message = count == 0
                    ? SelectionGuidance
                    : "Cốt thép 3D Dầm: đã tạo/cập nhật " + count + " thanh dọc.";
                FinalizeUi(document, nativeDatabaseIdentity, message);
            }
            catch (Exception)
            {
                Report(document, nativeDatabaseIdentity, OperationFailure);
            }
        }

        private static List<ProjectElement> ResolveBeamTargets(ProjectState project, HashSet<string> selectedHandles) =>
            project.Elements
                .Where(x => x.Category == ElementCategory.Beam && x.SourceHandles.Any(selectedHandles.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try
            {
                RefreshModelTree(document, nativeDatabaseIdentity);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                document.Editor.Regen();
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                TrySetPaletteStatus(document, nativeDatabaseIdentity, message);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                TryWriteMessage(document, nativeDatabaseIdentity, "\nQS3D " + message + " " + UiSyncWarning + " (" + ex.GetType().Name + ").");
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
                throw new InvalidOperationException("Beam Rebar 3D document generation changed before geometry mutation.");
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
