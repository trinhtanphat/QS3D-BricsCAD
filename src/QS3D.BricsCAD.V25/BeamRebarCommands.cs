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
        [CommandMethod("QS3DBEAMREBAR3D", CommandFlags.UsePickSet)]
        public void BuildBeamRebar3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, "Cốt thép 3D Dầm: chọn LINE đã capture thành Beam và khai báo RebarNotation; top/bottom có thể đặt bằng RebarBeamTopCount/RebarBeamBottomCount.");
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
                    Report(document, "Cốt thép 3D Dầm: selection không có source handle hợp lệ.");
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, "Cốt thép 3D Dầm: BLOCKED • chưa có QS3D project hiện hữu; lệnh không tạo project mới từ selection.");
                    return;
                }

                var previewTargets = ResolveBeamTargets(previewProject, selectedHandles);
                if (previewTargets.Count == 0)
                {
                    Report(document, "Cốt thép 3D Dầm: chọn LINE đã capture thành Beam và khai báo RebarNotation; top/bottom có thể đặt bằng RebarBeamTopCount/RebarBeamBottomCount.");
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

                var count = BeamRebarSolidBuilder.BuildSelected(document, project);
                var message = count == 0
                    ? "Cốt thép 3D Dầm: chọn LINE đã capture thành Beam và khai báo RebarNotation; top/bottom có thể đặt bằng RebarBeamTopCount/RebarBeamBottomCount."
                    : "Cốt thép 3D Dầm: đã tạo/cập nhật " + count + " thanh dọc.";
                FinalizeUi(document, message);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DBEAMREBAR3D lỗi: " + ex.Message);
            }
        }

        private static List<ProjectElement> ResolveBeamTargets(ProjectState project, HashSet<string> selectedHandles) =>
            project.Elements
                .Where(x => x.Category == ElementCategory.Beam && x.SourceHandles.Any(selectedHandles.Contains))
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
