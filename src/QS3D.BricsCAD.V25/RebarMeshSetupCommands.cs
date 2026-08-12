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
    public sealed class RebarMeshSetupCommands
    {
        [CommandMethod("QS3DREBARMESHSETUP", CommandFlags.UsePickSet)]
        public void RebarMeshSetup()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0) return;
                var selectedHandles = new HashSet<string>(
                    snapshots.Select(x => x.Handle).Where(x => !string.IsNullOrWhiteSpace(x)),
                    StringComparer.OrdinalIgnoreCase);
                if (selectedHandles.Count == 0) return;

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    document.Editor.WriteMessage("\nQS3D Rebar Mesh Setup: chưa có QS3D project hiện hữu để resolve semantic source.");
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var previewMatches = ResolveMeshTargets(previewProject, selectedHandles);
                if (previewMatches.Count != 1)
                {
                    document.Editor.WriteMessage("\nQS3D Rebar Mesh Setup: chọn đúng một Slab, StructuralWall hoặc Foundation semantic source.");
                    return;
                }

                var expectedElementId = previewMatches[0].Id;
                var expectedCategory = previewMatches[0].Category;
                var project = ExistingProjectMutationContext.Require(document, "Rebar Mesh Setup");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("Rebar Mesh Setup: QS3D project đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                var matches = ResolveMeshTargets(project, selectedHandles);
                if (matches.Count != 1 ||
                    !string.Equals(matches[0].Id, expectedElementId, StringComparison.OrdinalIgnoreCase) ||
                    matches[0].Category != expectedCategory)
                    throw new InvalidOperationException("Rebar Mesh Setup: semantic target đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                var element = matches[0];
                var elementId = element.Id;
                var window = new RebarMeshSetupWindow(document, project, element, () =>
                {
                    PaletteCoordinator.RefreshProject();
                    PaletteCoordinator.SetStatus("Đã lưu mesh input cho " + elementId + ". Rebuild 3D để cập nhật generated bars.");
                });
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DREBARMESHSETUP lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static List<ProjectElement> ResolveMeshTargets(ProjectState project, HashSet<string> selectedHandles) =>
            project.Elements
                .Where(x => IsMeshTarget(x) && x.SourceHandles.Any(selectedHandles.Contains))
                .Take(3)
                .ToList();

        private static bool IsMeshTarget(ProjectElement element) =>
            element.Category == ElementCategory.Slab ||
            element.Category == ElementCategory.StructuralWall ||
            element.Category == ElementCategory.Foundation;
    }
}