using System;
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
            UpdateSelectedFloorText();
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
                    AuditTrail.ForProject(project).Record("floor.activate", floor.Id, floor.Name);
                    PaletteCoordinator.RefreshProject();
                }
                RefreshAll(floor.Id);
                SetStatus("Tầng hoạt động: " + floor.Name + ".");
            }
            catch (System.Exception ex) { SetStatus("Đặt tầng hoạt động lỗi: " + ex.Message); }
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
                    element.FloorId = floor.Id;
                    element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                    AuditTrail.ForProject(project).Record("floor.assign", element.Id, floor.Id + " • " + floor.Name);
                    changed++;
                }
                if (changed > 0)
                {
                    project.Touch();
                    PaletteCoordinator.RefreshProject();
                }
                SelectionCountText.Text = elements.Count.ToString();
                SetStatus("Đã gán tầng “" + floor.Name + "” cho " + changed + "/" + elements.Count + " semantic element.");
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
                SelectionCountText.Text = elements.Count.ToString();
                if (elements.Count == 0)
                {
                    SetStatus("Selection hiện tại chưa resolve được QS3D semantic element.");
                    return;
                }
                var floors = elements
                    .GroupBy(x => x.FloorId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .Select(group => FloorLabel(project, group.Key) + ": " + group.Count())
                    .ToList();
                SetStatus("Selection: " + elements.Count + " element • " + string.Join(" • ", floors));
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
                if (string.IsNullOrWhiteSpace(previous)) previous = project.ActiveFloorId;
                var floors = project.Floors
                    .OrderBy(x => x.ElevationM)
                    .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                _loading = true;
                try
                {
                    FloorList.ItemsSource = floors;
                    FloorList.SelectedItem = floors.FirstOrDefault(x => string.Equals(x.Id, previous, StringComparison.OrdinalIgnoreCase));
                }
                finally { _loading = false; }

                ActiveFloorText.Text = FloorLabel(project, project.ActiveFloorId);
                UpdateSelectedFloorText();
                var count = SemanticSelectionResolver.ResolveImplied(document, project).Count;
                SelectionCountText.Text = count.ToString();
            }
            catch (System.Exception ex) { SetStatus("Đọc danh sách tầng lỗi: " + ex.Message); }
        }

        private FloorDefinition RequireSelectedFloor(ProjectState project)
        {
            if (!(FloorList.SelectedItem is FloorDefinition selected))
                throw new InvalidOperationException("Chọn một tầng trước khi thực hiện thao tác.");
            return project.Floors.FirstOrDefault(x => string.Equals(x.Id, selected.Id, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Tầng đã chọn không còn tồn tại trong project hiện tại.");
        }

        private void UpdateSelectedFloorText()
        {
            SelectedFloorText.Text = FloorList.SelectedItem is FloorDefinition floor
                ? floor.Name + " (" + floor.ElevationM.ToString("0.###") + " m)"
                : "—";
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
