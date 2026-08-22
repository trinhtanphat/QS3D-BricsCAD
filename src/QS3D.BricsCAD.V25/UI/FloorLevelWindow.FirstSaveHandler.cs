using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow
    {
        private void OnSaveFloorFirstBootstrapClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var creatingNewFloor = string.IsNullOrWhiteSpace(_editingFloorId);
                var name = (FloorNameBox.Text ?? string.Empty).Trim();
                ValidateFirstSaveFloorDraft(name);
                var elevation = ParseElevation(FloorElevationBox.Text);
                var project = RequireProjectForFirstSave(creatingNewFloor, out var bootstrappedProject);
                var rollback = ProjectStateSnapshot.Capture(project);
                FloorDefinition floor;
                try
                {
                    if (creatingNewFloor)
                    {
                        floor = ProjectFloorService.Create(project, "floor-" + Guid.NewGuid().ToString("N"), name, elevation);
                        AuditTrail.ForProject(project).Record("floor.create", string.Empty, floor.Id + " • " + floor.Name + " • " + floor.ElevationM.ToString("R", CultureInfo.InvariantCulture) + "m");
                    }
                    else
                    {
                        var existing = project.Floors.FirstOrDefault(x => string.Equals(x.Id, _editingFloorId, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException("Tầng đang chỉnh không còn tồn tại trong project hiện tại. Hãy Refresh rồi chọn lại tầng.");
                        var before = existing.Name + "@" + existing.ElevationM.ToString("R", CultureInfo.InvariantCulture);
                        floor = ProjectFloorService.Update(project, existing.Id, name, elevation);
                        var after = floor.Name + "@" + floor.ElevationM.ToString("R", CultureInfo.InvariantCulture);
                        if (!string.Equals(before, after, StringComparison.Ordinal))
                            AuditTrail.ForProject(project).Record("floor.update", string.Empty, floor.Id + " • " + before + " -> " + after);
                    }
                }
                catch (Exception operationError)
                {
                    try
                    {
                        RestoreOrThrow(project, rollback, operationError, "Lưu Floor/Level");
                    }
                    finally
                    {
                        if (bootstrappedProject)
                            ProjectContextCoordinator.Forget(_document);
                    }
                    throw;
                }

                _editingFloorId = floor.Id;
                RefreshAfterCommit(
                    () => RefreshAll(floor.Id),
                    "Đã lưu tầng “" + floor.Name + "” • " + floor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m.",
                    "Floor/Level save");
            }
            catch (Exception ex)
            {
                SetStatus("Lưu tầng lỗi: " + ex.Message);
            }
        }
    }
}
