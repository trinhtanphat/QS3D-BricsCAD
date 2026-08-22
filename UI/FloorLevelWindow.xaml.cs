using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow : Window
    {
        private bool _loading;

        public FloorLevelWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => RefreshAll();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshAll();

        private void OnFloorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            RefreshLabels();
        }

        private void OnActivateClick(object sender, RoutedEventArgs e)
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var floor = RequireSelectedFloor(project);
                if (!string.Equals(project.ActiveFloorId, floor.Id, StringComparison.OrdinalIgnoreCase))
                {
                    project.ActiveFloorId = floor.Id;
                    project.Touch();
                    AuditTrail.ForProject(project).Record("floor.activate", string.Empty, floor.Id + " • " + floor.Name + " • " + floor.ElevationM.ToString("R", CultureInfo.InvariantCulture) + "m");
                    PaletteCoordinator.RefreshProject();
                }
                RefreshAll(floor.Id);
                SetStatus("Tầng hoạt động: " + floor.Name + " • " + floor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m.");
            }
            catch (System.Exception ex) { SetStatus("Đặt tầng active lỗi: " + ex.Message); }
        }

        private void OnAssignClick(object sender, RoutedEventArgs e)
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var floor = RequireSelectedFloor(project);
                var elements = SemanticSelectionResolver.ResolveImplied(document, project).ToList();
                if (elements.Count == 0) throw new InvalidOperationException("Selection hiện tại không resolve được QS3D semantic element.");

                var changed = 0;
                foreach (var element in elements)
                {
                    if (string.Equals(element.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase)) continue;
                    var previous = element.FloorId;
                    element.FloorId = floor.Id;
                    element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                    AuditTrail.ForProject(project).Record("floor.assign", element.Id, previous + " -> " + floor.Id + " • semantic only; CAD source position unchanged");
                    changed++;
                }
                if (changed > 0)
                {
                    project.Touch();
                    PaletteCoordinator.RefreshProject();
                }
                SelectionCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
                RefreshLabels();
                SetStatus("Đã gán “" + floor.Name + "” cho " + changed + "/" + elements.Count + " semantic element. Generated output liên quan đã stale; CAD source không bị Move.");
            }
            catch (System.Exception ex) { SetStatus("Gán tầng lỗi: " + ex.Message); }
        }

        private void OnInspectSelectionClick(object sender, RoutedEventArgs e)
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var elements = SemanticSelectionResolver.ResolveImplied(document, project).ToList();
                SelectionCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
                var floors = elements
                    .GroupBy(x => x.FloorId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .Select(group => FloorLabel(project, group.Key) + ": " + group.Count())
                    .ToList();
                SetStatus(elements.Count == 0 ? "Selection chưa resolve được semantic element." : "Selection: " + string.Join(" • ", floors));
            }
            catch (System.Exception ex) { SetStatus("Kiểm tra selection lỗi: " + ex.Message); }
        }

        private void RefreshAll(string selectedFloorId = "")
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var previous = string.IsNullOrWhiteSpace(selectedFloorId)
                    ? (FloorList.SelectedItem as FloorDefinition)?.Id
                    : selectedFloorId;
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
                RefreshLabels();
                if (floors.Count == 0) SetStatus("Project chưa có tầng. Tạo tầng trong Project Setup trước khi dùng Level Picker.");
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

        private void RefreshLabels()
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var active = project.Floors.FirstOrDefault(x => string.Equals(x.Id, project.ActiveFloorId, StringComparison.OrdinalIgnoreCase));
            var selected = FloorList.SelectedItem as FloorDefinition;
            ActiveFloorText.Text = active == null ? "—" : active.Name + " • " + active.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            SelectedFloorText.Text = selected == null ? "—" : selected.Name + " • " + selected.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            try
            {
                SelectionCountText.Text = SemanticSelectionResolver.ResolveImplied(document, project).Count.ToString(CultureInfo.InvariantCulture);
            }
            catch { SelectionCountText.Text = "!"; }
        }

        private static string FloorLabel(ProjectState project, string floorId)
        {
            var floor = project.Floors.FirstOrDefault(x => string.Equals(x.Id, floorId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            return floor == null ? (string.IsNullOrWhiteSpace(floorId) ? "Chưa gán" : floorId) : floor.Name;
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
            PaletteCoordinator.SetStatus(StatusText.Text);
        }
    }
}
