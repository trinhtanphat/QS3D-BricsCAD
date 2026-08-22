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
    public partial class FamilyManagerWindow : Window
    {
        private readonly Document _document;
        private bool _loading;
        private bool _creatingNew;

        private sealed class CategoryChoice
        {
            public string Label { get; set; } = string.Empty;
            public ElementCategory? Category { get; set; }
            public override string ToString() => Label;
        }

        private sealed class PropertyRow
        {
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        public FamilyManagerWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            Loaded += (_, __) => InitializeAndRefresh();
        }

        private void InitializeAndRefresh()
        {
            var categories = Enum.GetValues(typeof(ElementCategory)).Cast<ElementCategory>().OrderBy(x => x.ToString()).ToList();
            var filters = new List<CategoryChoice> { new CategoryChoice { Label = "Tất cả", Category = null } };
            filters.AddRange(categories.Select(x => new CategoryChoice { Label = x.ToString(), Category = x }));
            _loading = true;
            try
            {
                CategoryFilter.ItemsSource = filters;
                CategoryFilter.SelectedIndex = 0;
                NewCategoryCombo.ItemsSource = filters.Skip(1).ToList();
                NewCategoryCombo.SelectedIndex = 0;
            }
            finally { _loading = false; }
            RefreshAll();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshAll();
        private void OnCategoryFilterChanged(object sender, SelectionChangedEventArgs e) { if (!_loading) RefreshAll(); }

        private void OnFamilySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            _creatingNew = false;
            LoadFamily();
        }

        private void OnPropertySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PropertyList.SelectedItem is PropertyRow row) { PropertyKeyBox.Text = row.Key; PropertyValueBox.Text = row.Value; }
        }

        private void OnNewClick(object sender, RoutedEventArgs e)
        {
            _creatingNew = true;
            FamilyList.SelectedItem = null;
            FamilyNameBox.Text = string.Empty;
            PropertyList.ItemsSource = null;
            PropertyKeyBox.Text = string.Empty;
            PropertyValueBox.Text = string.Empty;
            ReferenceCountText.Text = "0";
            FamilyNameBox.Focus();
            SetStatus("Tạo Family mới: chọn Category, nhập tên rồi bấm Lưu tên.");
        }

        private void OnDuplicateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(FamilyList.SelectedItem is ProjectFamily source)) throw new InvalidOperationException("Chọn Family trước khi duplicate.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var name = NextCopyName(project, source);
                var clone = ProjectFamilyService.Duplicate(project, source.Id, "family-" + Guid.NewGuid().ToString("N"), name);
                AuditTrail.ForProject(project).Record("family.duplicate", string.Empty, source.Id + " -> " + clone.Id + " • " + clone.Name);
                PaletteCoordinator.RefreshProject();
                RefreshAll(clone.Id);
                SetStatus("Đã duplicate “" + source.Name + "” → “" + clone.Name + "”.");
            }
            catch (Exception ex) { SetStatus("Duplicate Family lỗi: " + ex.Message); }
        }

        private void OnRenameClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                ProjectFamily family;
                if (_creatingNew)
                {
                    var choice = NewCategoryCombo.SelectedItem as CategoryChoice;
                    if (choice?.Category == null) throw new InvalidOperationException("Chọn Category cho Family mới.");
                    family = ProjectFamilyService.Create(project, "family-" + Guid.NewGuid().ToString("N"), FamilyNameBox.Text, choice.Category.Value);
                    AuditTrail.ForProject(project).Record("family.create", string.Empty, family.Id + " • " + family.Category + " • " + family.Name);
                    _creatingNew = false;
                }
                else
                {
                    family = FamilyList.SelectedItem as ProjectFamily ?? throw new InvalidOperationException("Chọn Family trước khi đổi tên.");
                    var before = family.Name;
                    ProjectFamilyService.Rename(project, family.Id, FamilyNameBox.Text);
                    if (!string.Equals(before, family.Name, StringComparison.Ordinal)) AuditTrail.ForProject(project).Record("family.rename", string.Empty, family.Id + " • " + before + " -> " + family.Name);
                }
                PaletteCoordinator.RefreshProject();
                RefreshAll(family.Id);
                SetStatus("Đã lưu Family “" + family.Name + "”.");
            }
            catch (Exception ex) { SetStatus("Lưu Family lỗi: " + ex.Message); }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(FamilyList.SelectedItem is ProjectFamily family)) throw new InvalidOperationException("Chọn Family trước khi xóa.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                if (!ProjectFamilyService.Delete(project, family.Id)) return;
                AuditTrail.ForProject(project).Record("family.delete", string.Empty, family.Id + " • " + family.Name);
                PaletteCoordinator.RefreshProject();
                RefreshAll();
                SetStatus("Đã xóa Family “" + family.Name + "”.");
            }
            catch (Exception ex) { SetStatus("Xóa Family lỗi: " + ex.Message); }
        }

        private void OnSavePropertyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var family = FamilyList.SelectedItem as ProjectFamily ?? throw new InvalidOperationException("Chọn Family trước khi lưu property.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var result = ProjectFamilyService.SetProperty(project, family.Id, PropertyKeyBox.Text, PropertyValueBox.Text);
                AuditTrail.ForProject(project).Record("family.property.set", string.Empty, family.Id + " • " + PropertyKeyBox.Text + "=" + PropertyValueBox.Text + " • inherited=" + result.InheritedInstancesUpdated + " • overrides=" + result.OverridesPreserved);
                PaletteCoordinator.RefreshProject();
                RefreshAll(family.Id);
                SetStatus("Đã lưu property • cập nhật " + result.InheritedInstancesUpdated + " instance kế thừa • giữ " + result.OverridesPreserved + " override.");
            }
            catch (Exception ex) { SetStatus("Lưu Family property lỗi: " + ex.Message); }
        }

        private void OnRemovePropertyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var family = FamilyList.SelectedItem as ProjectFamily ?? throw new InvalidOperationException("Chọn Family trước khi xóa property.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var key = PropertyKeyBox.Text;
                var result = ProjectFamilyService.RemoveProperty(project, family.Id, key);
                AuditTrail.ForProject(project).Record("family.property.remove", string.Empty, family.Id + " • " + key + " • inherited=" + result.InheritedInstancesUpdated + " • overrides=" + result.OverridesPreserved);
                PaletteCoordinator.RefreshProject();
                RefreshAll(family.Id);
                SetStatus("Đã xóa Family property • bỏ " + result.InheritedInstancesUpdated + " inherited-copy • giữ " + result.OverridesPreserved + " override.");
            }
            catch (Exception ex) { SetStatus("Xóa Family property lỗi: " + ex.Message); }
        }

        private void OnAssignClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("gán Family cho selection");
                var family = FamilyList.SelectedItem as ProjectFamily ?? throw new InvalidOperationException("Chọn Family trước khi gán.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project).ToList();
                if (elements.Count == 0) throw new InvalidOperationException("Selection hiện tại không resolve được QS3D semantic element.");
                var previous = elements.ToDictionary(x => x.Id, x => x.FamilyId, StringComparer.OrdinalIgnoreCase);
                var changed = ProjectFamilyService.Assign(project, family.Id, elements);
                foreach (var element in elements)
                    if (previous.TryGetValue(element.Id, out var before) && !string.Equals(before, element.FamilyId, StringComparison.OrdinalIgnoreCase)) AuditTrail.ForProject(project).Record("family.assign", element.Id, before + " -> " + family.Id);
                PaletteCoordinator.RefreshProject();
                RefreshAll(family.Id);
                SetStatus("Đã gán Family “" + family.Name + "” cho " + changed + "/" + elements.Count + " semantic element.");
            }
            catch (Exception ex) { SetStatus("Gán Family lỗi: " + ex.Message); }
        }

        private void RefreshAll(string preferredId = "")
        {
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var previous = string.IsNullOrWhiteSpace(preferredId) ? (FamilyList.SelectedItem as ProjectFamily)?.Id : preferredId;
                var filter = (CategoryFilter.SelectedItem as CategoryChoice)?.Category;
                var families = project.Families.Where(x => !filter.HasValue || x.Category == filter.Value).OrderBy(x => x.Category).ThenBy(x => x.Name).ToList();
                _loading = true;
                try { FamilyList.ItemsSource = families; FamilyList.SelectedItem = families.FirstOrDefault(x => string.Equals(x.Id, previous, StringComparison.OrdinalIgnoreCase)) ?? families.FirstOrDefault(); }
                finally { _loading = false; }
                _creatingNew = false;
                LoadFamily();
                Title = "QS3D • Family Manager • " + DrawingLabel(_document);
            }
            catch (Exception ex) { SetStatus("Đọc Family Catalog lỗi: " + ex.Message); }
        }

        private void LoadFamily()
        {
            var project = ProjectContextCoordinator.GetOrCreate(_document);
            if (!(FamilyList.SelectedItem is ProjectFamily family)) { FamilyNameBox.Text = string.Empty; PropertyList.ItemsSource = null; ReferenceCountText.Text = "0"; return; }
            FamilyNameBox.Text = family.Name;
            NewCategoryCombo.SelectedItem = (NewCategoryCombo.ItemsSource as IEnumerable<CategoryChoice>)?.FirstOrDefault(x => x.Category == family.Category);
            PropertyList.ItemsSource = family.Properties.OrderBy(x => x.Key).Select(x => new PropertyRow { Key = x.Key, Value = x.Value }).ToList();
            ReferenceCountText.Text = ProjectFamilyService.ReferenceCount(project, family.Id).ToString(CultureInfo.InvariantCulture);
            PropertyKeyBox.Text = string.Empty; PropertyValueBox.Text = string.Empty;
        }

        private static string NextCopyName(ProjectState project, ProjectFamily source)
        {
            for (var index = 1; index <= 10000; index++)
            {
                var candidate = source.Name + (index == 1 ? " Copy" : " Copy " + index.ToString(CultureInfo.InvariantCulture));
                if (!project.Families.Any(x => x.Category == source.Category && string.Equals(x.Name, candidate, StringComparison.OrdinalIgnoreCase))) return candidate;
            }
            throw new InvalidOperationException("Không tìm được tên Family duplicate an toàn.");
        }

        private void EnsureActive(string operation) { if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)) throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Family Manager trước khi " + operation + "."); }
        private static string DrawingLabel(Document document) { var name = document.Name ?? string.Empty; if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu"; try { return System.IO.Path.GetFileName(name); } catch { return name; } }
        private void SetStatus(string text) { StatusText.Text = text ?? string.Empty; PaletteCoordinator.SetStatus(StatusText.Text); }
    }
}
