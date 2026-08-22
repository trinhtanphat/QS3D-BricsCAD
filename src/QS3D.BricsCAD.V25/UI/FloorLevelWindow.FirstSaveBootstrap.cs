using System;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow
    {
        private ProjectState RequireProjectForFirstSave(bool creatingNewFloor, out bool bootstrappedProject)
        {
            bootstrappedProject = false;
            if (!creatingNewFloor || _boundProject != null)
                return RequireBoundProjectForMutation("lưu tầng", "Lưu Floor/Level");

            EnsureBoundDrawingIsActive("lưu tầng");
            if (ProjectContextCoordinator.TryGetReadOnly(_document, out _))
                throw new InvalidOperationException("QS3D project đã xuất hiện hoặc thay đổi từ lần Refresh gần nhất. Hãy Refresh Level Picker trước khi lưu tầng.");

            var project = ProjectContextCoordinator.GetOrCreate(_document);
            bootstrappedProject = true;
            return project;
        }

        private static void ValidateFirstSaveFloorDraft(string name)
        {
            if (name.Length == 0 || name.Length > 120)
                throw new InvalidOperationException("Tên tầng phải có từ 1 đến 120 ký tự.");
        }
    }
}
