using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.BricsCAD.V25.UI.ViewModels;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel : UserControl
    {
        private readonly WorkspaceViewModel _viewModel = new WorkspaceViewModel();
        private IReadOnlyList<EntitySnapshot> _inspection = Array.Empty<EntitySnapshot>();

        public WorkspacePanel()
        {
            InitializeComponent();
            DataContext = _viewModel;
            Loaded += (_, __) => RefreshProject();
        }

        public void RefreshProject()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            _viewModel.Load(ProjectContextCoordinator.GetOrCreate(doc));
            if (FamilyList.Items.Count > 0 && FamilyList.SelectedIndex < 0) FamilyList.SelectedIndex = 0;
        }

        public void SetStatus(string status) => _viewModel.Status = status ?? string.Empty;

        public void SetInspection(IReadOnlyList<EntitySnapshot> snapshots)
        {
            _inspection = snapshots ?? Array.Empty<EntitySnapshot>();
            InspectionList.ItemsSource = _inspection;
            SelectionCount.Text = _inspection.Count + " chọn";
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument; if (doc == null) return;
            var project = ProjectContextCoordinator.GetOrCreate(doc);
            var basis = FamilyList.SelectedItem as ProjectFamily;
            var category = basis?.Category ?? ElementCategory.Room;
            var baseName = basis?.Name ?? category.ToString();
            var n = 2;
            var name = baseName + "-" + n;
            while (project.Families.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))) name = baseName + "-" + (++n);
            var family = new ProjectFamily(Guid.NewGuid().ToString("N"), name, category);
            if (basis != null) foreach (var property in basis.Properties) family.Properties[property.Key] = property.Value;
            project.Families.Add(family);
            project.Touch();
            RefreshProject();
            FamilyList.SelectedItem = family;
            SetStatus("Đã tạo Family “" + name + "”.");
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument; if (doc == null) return;
            var family = FamilyList.SelectedItem as ProjectFamily; if (family == null) return;
            var project = ProjectContextCoordinator.GetOrCreate(doc);
            var used = project.Elements.Count(x => string.Equals(x.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase));
            if (used > 0) { SetStatus("Không thể xóa: Family đang được " + used + " cấu kiện sử dụng."); return; }
            project.Families.Remove(family); project.Touch(); RefreshProject(); SetStatus("Đã xóa Family.");
        }

        private void OnView3DClick(object sender, RoutedEventArgs e)
        {
            var family = FamilyList.SelectedItem as ProjectFamily;
            var command = family?.Category == ElementCategory.ArchitecturalWall ? "QS3DWALL" : family?.Category == ElementCategory.Room ? "QS3DROOM" : family?.Category == ElementCategory.Door ? "QS3DDOOR" : family?.Category == ElementCategory.WallOpening ? "QS3DOPENING" : "QS3DTAKEOFF";
            Send(command);
        }

        private void OnCreateFinishesClick(object sender, RoutedEventArgs e) => Send("QS3DFINISH");
        private void OnQuantityClick(object sender, RoutedEventArgs e) => Send("QS3DBQ");
        private void OnHealthClick(object sender, RoutedEventArgs e) => Send("QS3DHEALTH");
        private void OnSaveClick(object sender, RoutedEventArgs e) => Send("QS3DSAVE");
        private void OnRefreshClick(object sender, RoutedEventArgs e) { RefreshProject(); PaletteCoordinator.RefreshCad(); }

        private void OnLocateSelectedClick(object sender, RoutedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument; if (doc == null || _inspection.Count == 0) return;
            var count = Cad.CadHandleService.Select(doc, _inspection.Select(x => x.Handle));
            SetStatus("Đã chọn lại " + count + " đối tượng CAD.");
        }

        private void OnFamilySelectionChanged(object sender, SelectionChangedEventArgs e) => _viewModel.LoadProperties(FamilyList.SelectedItem as ProjectFamily);
        private void OnFamilySearchChanged(object sender, TextChangedEventArgs e)
        {
            var text = FamilySearch.Text?.Trim() ?? string.Empty;
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(FamilyList.ItemsSource);
            if (view == null) return;
            view.Filter = item => item is ProjectFamily f && (text.Length == 0 || f.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 || f.Category.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void Send(string command)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.SendStringToExecute(command + " ", true, false, false);
        }
    }
}
