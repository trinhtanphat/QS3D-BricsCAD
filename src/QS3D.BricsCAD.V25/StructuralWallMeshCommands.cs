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
    public sealed class StructuralWallMeshCommands
    {
        [CommandMethod("QS3DWALLREBAR3D", CommandFlags.UsePickSet)]
        public void BuildStructuralWallMesh3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, "Wall Mesh 3D: chọn StructuralWall semantic LINE có RebarWallHorizontalNotation/RebarWallVerticalNotation.");
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
                    Report(document, "Wall Mesh 3D: selection không có source handle hợp lệ.");
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, "Wall Mesh 3D: BLOCKED • chưa có QS3D project hiện hữu; lệnh không tạo project mới từ selection.");
                    return;
                }

                var previewTargets = ResolveWallTargets(previewProject, selectedHandles);
                if (previewTargets.Count == 0)
                {
                    Report(document, "Wall Mesh 3D: chọn StructuralWall semantic LINE có RebarWallHorizontalNotation/RebarWallVerticalNotation.");
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var expectedTargetIds = new HashSet<string>(previewTargets.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);

                var project = ExistingProjectMutationContext.Require(document, "Wall Mesh 3D");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("Wall Mesh 3D: QS3D project đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                var targets = ResolveWallTargets(project, selectedHandles);
                if (!expectedTargetIds.SetEquals(targets.Select(x => x.Id)))
                    throw new InvalidOperationException("Wall Mesh 3D: semantic target set đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                var result = StructuralWallMeshSolidBuilder.BuildSelected(document, project);
                var message = result.Bars == 0
                    ? "Wall Mesh 3D: chọn StructuralWall semantic LINE có RebarWallHorizontalNotation/RebarWallVerticalNotation."
                    : "Wall Mesh 3D: đã tạo/cập nhật " + result.Bars + " thanh trên " + result.Elements + " vách.";
                FinalizeUi(document, message);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DWALLREBAR3D lỗi: " + ex.Message);
            }
        }

        private static List<ProjectElement> ResolveWallTargets(ProjectState project, HashSet<string> selectedHandles) =>
            project.Elements
                .Where(x => x.Category == ElementCategory.StructuralWall && x.SourceHandles.Any(selectedHandles.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static void FinalizeUi(Document document, string message)
        {
            try
            {
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                TryWriteMessage(document, "\nQS3D " + message + " UI sync warning: " + ex.Message);
            }
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
