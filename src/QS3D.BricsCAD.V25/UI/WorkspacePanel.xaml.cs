using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using Application = Bricscad.ApplicationServices.Application;
using QS3D.BricsCAD.V25.UI.ViewModels;
using QS3D.Core.Domain;
using QS3D.Core.Model;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel : UserControl
    {
        private readonly WorkspaceViewModel _viewModel = new WorkspaceViewModel();
        private IReadOnlyList<EntitySnapshot> _inspection = Array.Empty<EntitySnapshot>();
        private bool _loadingContext;
        private ElementCategory? _categoryFilter;

        public WorkspacePanel()
        {
            InitializeComponent(); DataContext = _viewModel;
            var propertyView = CollectionViewSource.GetDefaultView(_viewModel.Properties);
            if (propertyView != null && propertyView.CanGroup) propertyView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PropertyRowViewModel.Group)));
            Loaded += (_, __) => RefreshProject();
        }

        public void RefreshProject()
        {
            var doc = Application.DocumentManager.MdiActiveDocument; if (doc == null) return;
            _loadingContext = true;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc); _viewModel.Load(project);
                ZoneCombo.SelectedIndex = _viewModel.ActiveZoneIndex(); FloorCombo.SelectedIndex = _viewModel.ActiveFloorIndex(); ApplyFamilyFilter();
                var active = project.Metadata.TryGetValue("ActiveFamilyId", out var id) ? project.FindFamily(id) : null;
                FamilyList.SelectedItem = active ?? FamilyList.Items.Cast<object>().OfType<ProjectFamily>().FirstOrDefault();
            }
            finally { _loadingContext = false; }
        }

        public void SetStatus(string status) => _viewModel.Status = status ?? string.Empty;
        public void SetInspection(IReadOnlyList<EntitySnapshot> snapshots) { _inspection = snapshots ?? Array.Empty<EntitySnapshot>(); InspectionList.ItemsSource = _inspection; SelectionCount.Text = _inspection.Count + " chọn"; SyncFamilyFromSelection(); }

        private void SyncFamilyFromSelection()
        {
            if (_inspection.Count == 0) return; var doc = Application.DocumentManager.MdiActiveDocument; if (doc == null) return;
            var handles = new HashSet<string>(_inspection.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase); var project = ProjectContextCoordinator.GetOrCreate(doc);
            var element = project.Elements.FirstOrDefault(x => SemanticReferenceHandles.MatchesSelection(x, handles)); if (element == null || string.IsNullOrWhiteSpace(element.FamilyId)) return;
            var family = project.FindFamily(element.FamilyId); if (family == null) return;
            _loadingContext = true;
            try { _categoryFilter = family.Category; ApplyFamilyFilter(); FamilyList.SelectedItem = family; FamilyList.ScrollIntoView(family); _viewModel.SetActiveFamily(family); }
            finally { _loadingContext = false; }
        }

        private void OnZoneChanged(object sender, SelectionChangedEventArgs e) { if (!_loadingContext) _viewModel.SetActiveZone(ZoneCombo.SelectedItem as string); }
        private void OnFloorChanged(object sender, SelectionChangedEventArgs e) { if (!_loadingContext) _viewModel.SetActiveFloor(FloorCombo.SelectedItem as string); }
        private void OnModelTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(e.NewValue is TreeViewItem item)) return;
            if (item.Tag is string tag && Enum.TryParse(tag, true, out ElementCategory category)) { _categoryFilter = category; ApplyFamilyFilter(); SetStatus("Nhóm mô hình: " + item.Header); }
            else { _categoryFilter = null; ApplyFamilyFilter(); }
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument; if (doc == null) return; var project = ProjectContextCoordinator.GetOrCreate(doc); var basis = FamilyList.SelectedItem as ProjectFamily;
            var category = basis?.Category ?? _categoryFilter ?? ElementCategory.Room; var baseName = basis?.Name ?? category.ToString(); var n = 2; var name = baseName + "-" + n;
            while (project.Families.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))) name = baseName + "-" + (++n);
            var family = new ProjectFamily(Guid.NewGuid().ToString("N"), name, category); if (basis != null) foreach (var property in basis.Properties) family.Properties[property.Key] = property.Value;
            project.Families.Add(family); project.Metadata["ActiveFamilyId"] = family.Id; project.Touch(); RefreshProject(); FamilyList.SelectedItem = family; SetStatus("Đã tạo Family “" + name + "”.");
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument; if (doc == null) return; var family = FamilyList.SelectedItem as ProjectFamily; if (family == null) return;
            var project = ProjectContextCoordinator.GetOrCreate(doc); var used = project.Elements.Count(x => string.Equals(x.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase));
            if (used > 0) { SetStatus("Không thể xóa: Family đang được " + used + " cấu kiện sử dụng."); return; }
            project.Families.Remove(family); if (project.Metadata.TryGetValue("ActiveFamilyId", out var id) && string.Equals(id, family.Id, StringComparison.OrdinalIgnoreCase)) project.Metadata.Remove("ActiveFamilyId"); project.Touch(); RefreshProject(); SetStatus("Đã xóa Family.");
        }

        private void OnCaptureSelectedClick(object sender, RoutedEventArgs e)
        {
            var family = FamilyList.SelectedItem as ProjectFamily;
            var category = _categoryFilter ?? family?.Category;
            if (!category.HasValue) { SetStatus("Chọn một nhóm mô hình hoặc Family trước khi bóc đối tượng CAD."); return; }
            if (family != null && family.Category == category.Value) _viewModel.SetActiveFamily(family);
            var command = CommandFor(category);
            SetStatus("Bóc từ chọn → " + category.Value);
            Send(command);
        }

        private void OnView3DClick(object sender, RoutedEventArgs e) { var family = FamilyList.SelectedItem as ProjectFamily; _viewModel.SetActiveFamily(family); SetStatus("Vẽ/Cập nhật 3D: " + (family?.Name ?? "chưa chọn Family")); Send("QS3DBUILD3D"); }
        private void OnWallJunctionsClick(object sender, RoutedEventArgs e) { SetStatus("Phân tích giao tim tường L / T / X trong selection."); Send("QS3DWALLJUNCTIONS"); }
        private void OnViewModel3DClick(object sender, RoutedEventArgs e) => Send("QS3DVIEW3D");
        private void OnOrbitClick(object sender, RoutedEventArgs e) => Send("QS3DORBIT");
        private void OnZoomSelectionClick(object sender, RoutedEventArgs e) => Send("QS3DZOOMSELECTED");
        private void OnTopViewClick(object sender, RoutedEventArgs e) => Send("QS3DVIEWTOP");
        private void OnAddFinishClick(object sender, RoutedEventArgs e) => Send("QS3DFINISH");
        private void OnRemoveFinishClick(object sender, RoutedEventArgs e) => Send("QS3DUNTRACKFINISH");
        private void OnPickRoomClick(object sender, RoutedEventArgs e) => Send("QS3DROOM");
        private void OnQuantityClick(object sender, RoutedEventArgs e) => Send("QS3DBQ");
        private void OnHealthClick(object sender, RoutedEventArgs e) => Send("QS3DHEALTH");
        private void OnSaveClick(object sender, RoutedEventArgs e) => Send("QS3DSAVE");
        private void OnRefreshClick(object sender, RoutedEventArgs e) { RefreshProject(); PaletteCoordinator.RefreshCad(); }
        private void OnLocateSelectedClick(object sender, RoutedEventArgs e) { var count = SelectInspection(); SetStatus("Đã chọn lại " + count + " đối tượng CAD."); if (count > 0) Send("QS3DZOOMSELECTED"); }
        private void OnFocusSelectedClick(object sender, RoutedEventArgs e) { var count = SelectInspection(); if (count <= 0) { SetStatus("Chưa có đối tượng để Focus."); return; } SetStatus("Focus " + count + " đối tượng."); Send("QS3DFOCUS"); }
        private void OnIsolateSelectedClick(object sender, RoutedEventArgs e) { var count = SelectInspection(); if (count <= 0) { SetStatus("Chưa có đối tượng để Cô lập."); return; } SetStatus("Cô lập " + count + " đối tượng."); Send("QS3DISOLATE"); }
        private void OnUnisolateClick(object sender, RoutedEventArgs e) { SetStatus("Khôi phục đối tượng đã cô lập."); Send("QS3DUNISOLATE"); }
        private void OnFamilySelectionChanged(object sender, SelectionChangedEventArgs e) { if (_loadingContext) return; _viewModel.SetActiveFamily(FamilyList.SelectedItem as ProjectFamily); }
        private void OnFamilySearchChanged(object sender, TextChangedEventArgs e) => ApplyFamilyFilter();

        private int SelectInspection()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || _inspection.Count == 0) return 0;
            return Cad.CadHandleService.Select(doc, _inspection.Select(x => x.Handle));
        }

        private void ApplyFamilyFilter()
        {
            var text = FamilySearch?.Text?.Trim() ?? string.Empty; var view = CollectionViewSource.GetDefaultView(FamilyList?.ItemsSource); if (view == null) return;
            view.Filter = item => item is ProjectFamily family && (!_categoryFilter.HasValue || family.Category == _categoryFilter.Value) && (text.Length == 0 || family.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 || family.Category.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0); view.Refresh();
        }

        private static string CommandFor(ElementCategory? category)
        {
            switch (category)
            {
                case ElementCategory.ArchitecturalWall: return "QS3DWALL";
                case ElementCategory.GlassWall: return "QS3DGLASSWALL";
                case ElementCategory.WallPier: return "QS3DWALLPIER";
                case ElementCategory.StructuralWall: return "QS3DSTRUCTWALL";
                case ElementCategory.Room: return "QS3DROOM";
                case ElementCategory.Door: return "QS3DDOOR";
                case ElementCategory.WallOpening: return "QS3DOPENING";
                case ElementCategory.Beam: return "QS3DBEAM";
                case ElementCategory.Slab: return "QS3DSLAB";
                case ElementCategory.Column: return "QS3DCOLUMN";
                case ElementCategory.Foundation: return "QS3DFOUNDATION";
                case ElementCategory.Stair: return "QS3DSTAIR";
                case ElementCategory.Railing: return "QS3DRAILING";
                case ElementCategory.Earthwork: return "QS3DEARTHWORK";
                case ElementCategory.FloorFinish:
                case ElementCategory.Waterproofing:
                case ElementCategory.Skirting:
                case ElementCategory.WallFinish:
                case ElementCategory.CeilingFinish:
                    return "QS3DFINISH";
                default: return "QS3DTAKEOFF";
            }
        }
        private static void Send(string command) => Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(command + " ", true, false, false);
    }
}
