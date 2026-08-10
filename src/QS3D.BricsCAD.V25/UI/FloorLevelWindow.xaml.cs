using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow : Window
    {
        private readonly Document _document;
        private bool _loading;
        private string _editingFloorId = string.Empty;

        public FloorLevelWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
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
            FloorList.SelectedItem = null;
            FloorNameBox.Text = string.Empty;
            var nextElevation = project.Floors.Count == 0 ? 0d : project.Floors.Max(x => x.ElevationM) + 3.6d;
            FloorElevationBox.Text = nextElevation.ToString("0.###", CultureInfo.InvariantCulture);
            FloorNameBox.Focus();
            SetStatus("Tạo tầng mới. Nhập tên và cao độ rồi bấm Lưu.");
        }

        private void OnSaveFloorClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var name = (FloorNameBox.Text ?? string.Empty).Trim();
                var elevation = ParseElevation(FloorElevationBox.Text);
                FloorDefinition floor;
                if (string.IsNullOrWhiteSpace(_editingFloorId))
                {
                    var id = "floor-" + Guid.NewGuid().ToString("N");
                    floor = ProjectFloorService.Create(project, id, name, elevation);
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
                PaletteCoordinator.RefreshProject();
                RefreshAll(floor.Id);
                SetStatus("Đã lưu tầng “" + floor.Name + "” • " + floor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m.");
            }
            catch (Exception ex) { SetStatus("Lưu tầng lỗi: " + ex.Message); }
        }

        private void OnDeleteFloorClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(FloorList.SelectedItem is FloorDefinition floor)) throw new InvalidOperationException("Chọn một tầng trước khi xóa.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var label = floor.Name;
                if (!ProjectFloorService.Delete(project, floor.Id)) return;
                AuditTrail.ForProject(project).Record("floor.delete", string.Empty, floor.Id + " • " + label);
                _editingFloorId = string.Empty;
                PaletteCoordinator.RefreshProject();
                RefreshAll();
                SetStatus("Đã xóa tầng “" + label + "”.");
            }
            catch (Exception ex) { SetStatus("Xóa tầng lỗi: " + ex.Message); }
        }

        private void OnActivateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(FloorList.SelectedItem is FloorDefinition floor)) throw new InvalidOperationException("Chọn một tầng trước khi đặt active.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var previous = project.ActiveFloorId;
                ProjectFloorService.SetActive(project, floor.Id);
                if (!string.Equals(previous, floor.Id, StringComparison.OrdinalIgnoreCase))
                    AuditTrail.ForProject(project).Record("floor.activate", string.Empty, previous + " -> " + floor.Id + " • " + floor.Name + " • " + floor.ElevationM.ToString("R", CultureInfo.InvariantCulture) + "m");
                RefreshLabels();
                PaletteCoordinator.RefreshProject();
                SetStatus("Tầng hoạt động: " + floor.Name + " • " + floor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m.");
            }
            catch (Exception ex) { SetStatus("Đặt tầng active lỗi: " + ex.Message); }
        }

        private void OnAssignClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureBoundDrawingIsActive("gán tầng cho selection");
                if (!(FloorList.SelectedItem is FloorDefinition floor)) throw new InvalidOperationException("Chọn một tầng trước khi gán.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project).ToList();
                if (elements.Count == 0) throw new InvalidOperationException("Selection hiện tại không resolve được QS3D semantic element.");
                var previousFloors = elements
                    .Where(x => !string.Equals(x.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(x => x.Id, x => x.FloorId, StringComparer.OrdinalIgnoreCase);
                var changed = ProjectFloorService.Assign(project, floor.Id, elements);
                foreach (var element in elements)
                    if (previousFloors.TryGetValue(element.Id, out var previous))
                        AuditTrail.ForProject(project).Record("floor.assign", element.Id, previous + " -> " + floor.Id + " • semantic only; CAD source position unchanged");
                PaletteCoordinator.RefreshProject();
                RefreshLabels();
                SetStatus("Đã gán “" + floor.Name + "” cho " + changed + "/" + elements.Count + " semantic element. Generated output liên quan đã stale; CAD source không bị Move.");
            }
            catch (Exception ex) { SetStatus("Gán tầng lỗi: " + ex.Message); }
        }

        private void OnInspectSelectionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureBoundDrawingIsActive("kiểm tra selection");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project);
                SelectionCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
                var floors = elements.GroupBy(x => x.FloorId, StringComparer.OrdinalIgnoreCase)
                    .Select(x => (project.Floors.FirstOrDefault(f => string.Equals(f.Id, x.Key, StringComparison.OrdinalIgnoreCase))?.Name ?? x.Key) + ": " + x.Count())
                    .ToList();
                SetStatus(elements.Count == 0 ? "Selection chưa resolve được semantic element." : "Selection: " + string.Join(" • ", floors));
            }
            catch (Exception ex) { SetStatus("Kiểm tra selection lỗi: " + ex.Message); }
        }

        private void RefreshAll(string preferredFloorId = "")
        {
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var previous = string.IsNullOrWhiteSpace(preferredFloorId) ? (FloorList.SelectedItem as FloorDefinition)?.Id : preferredFloorId;
                var floors = project.Floors.OrderBy(x => x.ElevationM).ThenBy(x => x.Name).ToList();
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
            catch (Exception ex) { SetStatus("Đọc Floor/Level lỗi: " + ex.Message); }
        }

        private void LoadEditorFromSelection()
        {
            var selected = FloorList.SelectedItem as FloorDefinition;
            if (selected == null) return;
            _editingFloorId = selected.Id;
            FloorNameBox.Text = selected.Name;
            FloorElevationBox.Text = selected.ElevationM.ToString("R", CultureInfo.InvariantCulture);
        }

        private void RefreshLabels()
        {
            var project = ProjectContextCoordinator.GetOrCreate(_document);
            var active = project.Floors.FirstOrDefault(x => string.Equals(x.Id, project.ActiveFloorId, StringComparison.OrdinalIgnoreCase));
            var selected = FloorList.SelectedItem as FloorDefinition;
            ActiveFloorText.Text = active == null ? "—" : active.Name + " • " + active.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            SelectedFloorText.Text = selected == null ? "—" : selected.Name + " • " + selected.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            ReferenceCountText.Text = selected == null ? "0" : ProjectFloorService.ReferenceCount(project, selected.Id).ToString(CultureInfo.InvariantCulture);
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
            {
                SelectionCountText.Text = "—";
                return;
            }
            try { SelectionCountText.Text = SemanticSelectionResolver.ResolveImplied(_document, project).Count.ToString(CultureInfo.InvariantCulture); }
            catch { SelectionCountText.Text = "!"; }
        }

        private void EnsureBoundDrawingIsActive(string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Level Picker trước khi " + operation + ".");
        }

        private static double ParseElevation(string raw)
        {
            var text = (raw ?? string.Empty).Trim();
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                throw new InvalidOperationException("Cao độ không phải số hợp lệ.");
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Cao độ phải hữu hạn.");
            return value;
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
            PaletteCoordinator.SetStatus(StatusText.Text);
        }
    }
}
