using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow : Window
    {
        private readonly Document _document;
        private string _editingFloorId = string.Empty;
        private bool _loading;

        public FloorLevelWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Loaded += (_, __) => RefreshAll();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshAll();

        private void OnFloorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            LoadEditorFromSelection();
            RefreshLabels();
        }

        private void OnNewFloorClick(object sender, RoutedEventArgs e)
        {
            var project = ProjectContextCoordinator.GetOrCreate(_document);
            _editingFloorId = string.Empty;
            _loading = true;
            try { FloorList.SelectedItem = null; }
            finally { _loading = false; }
            FloorNameBox.Text = string.Empty;
            var nextElevation = project.Floors.Count == 0 ? 0d : project.Floors.Max(x => x.ElevationM) + 3.6d;
            FloorElevationBox.Text = nextElevation.ToString("0.###", CultureInfo.InvariantCulture);
            ReferenceCountText.Text = "0";
            FloorNameBox.Focus();
            SetStatus("Tạo tầng mới. Nhập tên và cao độ rồi bấm Lưu.");
        }

        private void OnSaveFloorClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureBoundDrawingIsActive("lưu tầng");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var name = (FloorNameBox.Text ?? string.Empty).Trim();
                var elevation = ParseElevation(FloorElevationBox.Text);
                var rollback = ProjectStateSnapshot.Capture(project);
                FloorDefinition floor;
                try
                {
                    if (string.IsNullOrWhiteSpace(_editingFloorId))
                    {
                        floor = ProjectFloorService.Create(project, "floor-" + Guid.NewGuid().ToString("N"), name, elevation);
                        AuditTrail.ForProject(project).Record("floor.create", string.Empty, floor.Id + " • " + floor.Name + " • " + floor.ElevationM.ToString("R", CultureInfo.InvariantCulture) + "m");
                    }
                    else
                    {
                        var existing = project.Floors.FirstOrDefault(x => string.Equals(x.Id, _editingFloorId, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException("Tầng đang chỉnh không còn tồn tại trong project.");
                        var before = existing.Name + "@" + existing.ElevationM.ToString("R", CultureInfo.InvariantCulture);
                        floor = ProjectFloorService.Update(project, existing.Id, name, elevation);
                        var after = floor.Name + "@" + floor.ElevationM.ToString("R", CultureInfo.InvariantCulture);
                        if (!string.Equals(before, after, StringComparison.Ordinal))
                            AuditTrail.ForProject(project).Record("floor.update", string.Empty, floor.Id + " • " + before + " -> " + after);
                    }
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Lưu Floor/Level");
                    throw;
                }

                _editingFloorId = floor.Id;
                RefreshAfterCommit(
                    () => RefreshAll(floor.Id),
                    "Đã lưu tầng “" + floor.Name + "” • " + floor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m.",
                    "Floor/Level save");
            }
            catch (System.Exception ex) { SetStatus("Lưu tầng lỗi: " + ex.Message); }
        }

        private void OnDeleteFloorClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureBoundDrawingIsActive("xóa tầng");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var floor = RequireSelectedFloor(project);
                var rollback = ProjectStateSnapshot.Capture(project);
                var deleted = false;
                try
                {
                    deleted = ProjectFloorService.Delete(project, floor.Id);
                    if (deleted)
                        AuditTrail.ForProject(project).Record("floor.delete", string.Empty, floor.Id + " • " + floor.Name);
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Xóa Floor/Level");
                    throw;
                }

                if (!deleted) return;
                _editingFloorId = string.Empty;
                RefreshAfterCommit(
                    () => RefreshAll(),
                    "Đã xóa tầng “" + floor.Name + "”.",
                    "Floor/Level delete");
            }
            catch (System.Exception ex) { SetStatus("Xóa tầng lỗi: " + ex.Message); }
        }

        private void OnActivateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureBoundDrawingIsActive("đặt tầng hoạt động");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var floor = RequireSelectedFloor(project);
                var previous = project.ActiveFloorId;
                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    ProjectFloorService.SetActive(project, floor.Id);
                    if (!string.Equals(previous, floor.Id, StringComparison.OrdinalIgnoreCase))
                        AuditTrail.ForProject(project).Record("floor.activate", string.Empty, previous + " -> " + floor.Id + " • " + floor.Name + " • " + floor.ElevationM.ToString("R", CultureInfo.InvariantCulture) + "m");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Đặt Floor/Level active");
                    throw;
                }

                RefreshAfterCommit(
                    () => RefreshAll(floor.Id),
                    "Tầng hoạt động: " + floor.Name + " • " + floor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m.",
                    "Floor/Level activate");
            }
            catch (System.Exception ex) { SetStatus("Đặt tầng active lỗi: " + ex.Message); }
        }

        private void OnAssignClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureBoundDrawingIsActive("gán tầng cho selection");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var floor = RequireSelectedFloor(project);
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project)
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();
                if (elements.Count == 0) throw new InvalidOperationException("Selection hiện tại không resolve được QS3D semantic element.");

                var changedElements = elements
                    .Where(element => !string.Equals(element.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(element => new { Element = element, Previous = element.FloorId })
                    .ToList();
                var rollback = ProjectStateSnapshot.Capture(project);
                int changed;
                try
                {
                    changed = ProjectFloorService.Assign(project, floor.Id, elements);
                    foreach (var item in changedElements)
                        AuditTrail.ForProject(project).Record("floor.assign", item.Element.Id, item.Previous + " -> " + floor.Id + " • semantic only; CAD source position unchanged");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Gán Floor/Level cho selection");
                    throw;
                }

                RefreshAfterCommit(
                    () =>
                    {
                        SelectionCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
                        RefreshLabels();
                    },
                    "Đã gán “" + floor.Name + "” cho " + changed + "/" + elements.Count + " semantic element. Generated output liên quan đã stale; CAD source không bị Move.",
                    "Floor/Level assign");
            }
            catch (System.Exception ex) { SetStatus("Gán tầng lỗi: " + ex.Message); }
        }

        private void OnInspectSelectionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureBoundDrawingIsActive("kiểm tra selection");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project).ToList();
                SelectionCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
                var floors = elements
                    .GroupBy(x => x.FloorId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .Select(group => FloorLabel(project, group.Key) + ": " + group.Count())
                    .ToList();
                SetStatus(elements.Count == 0 ? "Selection chưa resolve được semantic element." : "Selection: " + string.Join(" • ", floors));
            }
            catch (System.Exception ex) { SetStatus("Kiểm tra selection lỗi: " + ex.Message); }
        }

        private void RefreshAll(string preferredFloorId = "")
        {
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var previous = string.IsNullOrWhiteSpace(preferredFloorId)
                    ? (FloorList.SelectedItem as FloorDefinition)?.Id
                    : preferredFloorId;
                var floors = project.Floors
                    .OrderBy(x => x.ElevationM)
                    .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                _loading = true;
                try
                {
                    FloorList.ItemsSource = floors;
                    FloorList.SelectedItem = floors.FirstOrDefault(x => string.Equals(x.Id, previous, StringComparison.OrdinalIgnoreCase))
                        ?? floors.FirstOrDefault(x => string.Equals(x.Id, project.ActiveFloorId, StringComparison.OrdinalIgnoreCase))
                        ?? floors.FirstOrDefault();
                }
                finally { _loading = false; }
                LoadEditorFromSelection();
                RefreshLabels();
                Title = "QS3D • Level Picker • " + DrawingLabel(_document);
                if (floors.Count == 0)
                {
                    _editingFloorId = string.Empty;
                    FloorNameBox.Text = string.Empty;
                    FloorElevationBox.Text = "0";
                    SetStatus("Project chưa có tầng. Dùng Mới → nhập tên/cao độ → Lưu.");
                }
            }
            catch (System.Exception ex) { SetStatus("Đọc Floor/Level lỗi: " + ex.Message); }
        }

        private FloorDefinition RequireSelectedFloor(ProjectState project)
        {
            if (!(FloorList.SelectedItem is FloorDefinition selected))
                throw new InvalidOperationException("Chọn một tầng trước khi thực hiện thao tác.");
            return project.Floors.FirstOrDefault(x => string.Equals(x.Id, selected.Id, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Tầng đã chọn không còn tồn tại trong project hiện tại.");
        }

        private void LoadEditorFromSelection()
        {
            if (!(FloorList.SelectedItem is FloorDefinition floor))
            {
                _editingFloorId = string.Empty;
                FloorNameBox.Text = string.Empty;
                FloorElevationBox.Text = string.Empty;
                return;
            }
            _editingFloorId = floor.Id;
            FloorNameBox.Text = floor.Name;
            FloorElevationBox.Text = floor.ElevationM.ToString("R", CultureInfo.InvariantCulture);
        }

        private void RefreshLabels()
        {
            var project = ProjectContextCoordinator.GetOrCreate(_document);
            var active = project.Floors.FirstOrDefault(x => string.Equals(x.Id, project.ActiveFloorId, StringComparison.OrdinalIgnoreCase));
            var selected = FloorList.SelectedItem as FloorDefinition;
            ActiveFloorText.Text = active == null ? "—" : active.Name + " • " + active.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            SelectedFloorText.Text = selected == null ? "—" : selected.Name + " • " + selected.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            ReferenceCountText.Text = selected == null
                ? "0"
                : ProjectFloorService.ReferenceCount(project, selected.Id).ToString(CultureInfo.InvariantCulture);
            if (!ReferenceEquals(Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument, _document))
            {
                SelectionCountText.Text = "—";
                return;
            }
            try { SelectionCountText.Text = SemanticSelectionResolver.ResolveImplied(_document, project).Count.ToString(CultureInfo.InvariantCulture); }
            catch { SelectionCountText.Text = "!"; }
        }

        private void RefreshAfterCommit(Action refresh, string successMessage, string context)
        {
            SetStatus(successMessage);
            try
            {
                refresh();
                PaletteCoordinator.RefreshProject();
            }
            catch (Exception refreshError)
            {
                var warning = successMessage + " UI sync warning: " + refreshError.Message;
                try { StatusText.Text = warning; } catch { }
                try { PaletteCoordinator.SetStatus(warning); } catch { }
                try { _document.Editor.WriteMessage("\nQS3D " + context + " đã commit; UI sync warning: " + refreshError.Message); } catch { }
            }
        }

        private static void RestoreOrThrow(ProjectState project, ProjectStateSnapshot rollback, Exception operationError, string operation)
        {
            try
            {
                rollback.Restore(project);
            }
            catch (Exception restoreError)
            {
                throw new InvalidOperationException(
                    operation + " thất bại và rollback project cũng không hoàn tất.",
                    new AggregateException(operationError, restoreError));
            }
        }

        private void EnsureBoundDrawingIsActive(string operation)
        {
            if (!ReferenceEquals(Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Level Picker trước khi " + operation + ".");
        }

        private static double ParseElevation(string raw)
        {
            var text = (raw ?? string.Empty).Trim();
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                throw new InvalidOperationException("Cao độ không phải số hợp lệ.");
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Cao độ phải hữu hạn.");
            return value;
        }

        private static string FloorLabel(ProjectState project, string floorId)
        {
            var floor = project.Floors.FirstOrDefault(x => string.Equals(x.Id, floorId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            return floor == null ? (string.IsNullOrWhiteSpace(floorId) ? "Chưa gán" : floorId) : floor.Name;
        }

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu";
            try { return System.IO.Path.GetFileName(name); }
            catch { return name; }
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
            try { PaletteCoordinator.SetStatus(StatusText.Text); } catch { }
        }
    }
}
