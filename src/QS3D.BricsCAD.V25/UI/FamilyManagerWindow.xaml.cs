using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FamilyManagerWindow : Window
    {
        private readonly Document _document;
        private ProjectState? _boundProject;
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
            DocumentBoundWindowLifetime.Attach(this, _document);
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
            if (_creatingNew && FamilyList.SelectedItem == null)
            {
                RefreshQuickWorkflow();
                return;
            }
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
            RefreshQuickWorkflow();
            SetStatus("Tạo Family mới: chọn Category, nhập tên rồi bấm Lưu tên. Custom property là tùy chọn.");
        }

        private void OnDuplicateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("duplicate Family");
                var project = ExistingProjectMutationContext.Require(_document, "Duplicate Family");
                var source = RequireSelectedFamily(project);
                var name = NextCopyName(project, source);
                var clone = ExecuteAtomic(project, () =>
                {
                    var created = ProjectFamilyService.Duplicate(project, source.Id, "family-" + Guid.NewGuid().ToString("N"), name);
                    AuditTrail.ForProject(project).Record("family.duplicate", string.Empty, source.Id + " -> " + created.Id + " • " + created.Name);
                    return created;
                }, "Duplicate Family");

                RefreshAfterCommit(
                    () => RefreshAll(clone.Id),
                    "Đã duplicate “" + source.Name + "” → “" + clone.Name + "”.",
                    "Family duplicate");
            }
            catch (Exception ex) { SetStatus("Duplicate Family lỗi: " + ex.Message); }
        }

        private void OnRenameClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("lưu Family");
                var project = ExistingProjectMutationContext.Require(_document, "Lưu Family");
                var creatingNew = _creatingNew;
                var family = ExecuteAtomic(project, () =>
                {
                    if (creatingNew)
                    {
                        var choice = NewCategoryCombo.SelectedItem as CategoryChoice;
                        if (choice?.Category == null) throw new InvalidOperationException("Chọn Category cho Family mới.");
                        var created = ProjectFamilyService.Create(project, "family-" + Guid.NewGuid().ToString("N"), FamilyNameBox.Text, choice.Category.Value);
                        AuditTrail.ForProject(project).Record("family.create", string.Empty, created.Id + " • " + created.Category + " • " + created.Name);
                        return created;
                    }

                    var current = RequireSelectedFamily(project);
                    var before = current.Name;
                    ProjectFamilyService.Rename(project, current.Id, FamilyNameBox.Text);
                    if (!string.Equals(before, current.Name, StringComparison.Ordinal))
                        AuditTrail.ForProject(project).Record("family.rename", string.Empty, current.Id + " • " + before + " -> " + current.Name);
                    return current;
                }, "Lưu Family");

                _creatingNew = false;
                RefreshAfterCommit(
                    () => RefreshAll(family.Id),
                    "Đã lưu Family “" + family.Name + "”.",
                    "Family save");
            }
            catch (Exception ex) { SetStatus("Lưu Family lỗi: " + ex.Message); }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("xóa Family");
                var project = ExistingProjectMutationContext.Require(_document, "Xóa Family");
                var family = RequireSelectedFamily(project);
                var deleted = ExecuteAtomic(project, () =>
                {
                    var removed = ProjectFamilyService.Delete(project, family.Id);
                    if (removed)
                        AuditTrail.ForProject(project).Record("family.delete", string.Empty, family.Id + " • " + family.Name);
                    return removed;
                }, "Xóa Family");
                if (!deleted) return;

                RefreshAfterCommit(
                    () => RefreshAll(),
                    "Đã xóa Family “" + family.Name + "”.",
                    "Family delete");
            }
            catch (Exception ex) { SetStatus("Xóa Family lỗi: " + ex.Message); }
        }

        private void OnSavePropertyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var key = (PropertyKeyBox.Text ?? string.Empty).Trim();
                if (key.Length == 0)
                {
                    SetStatus("Custom property là tùy chọn. Family và các property chuẩn vẫn được giữ; nhập Key chỉ khi cần thêm hoặc sửa custom property.");
                    return;
                }

                EnsureActive("lưu Family property");
                var project = ExistingProjectMutationContext.Require(_document, "Lưu Family property");
                var family = RequireSelectedFamily(project);
                var value = PropertyValueBox.Text;
                var result = ExecuteAtomic(project, () =>
                {
                    var beforeVersion = project.ChangeVersion;
                    var update = ProjectFamilyService.SetProperty(project, family.Id, key, value);
                    if (project.ChangeVersion != beforeVersion)
                        AuditTrail.ForProject(project).Record("family.property.set", string.Empty, family.Id + " • " + key + "=" + value + " • inherited=" + update.InheritedInstancesUpdated + " • overrides=" + update.OverridesPreserved);
                    return update;
                }, "Lưu Family property");

                RefreshAfterCommit(
                    () => RefreshAll(family.Id),
                    "Đã lưu property • cập nhật " + result.InheritedInstancesUpdated + " instance kế thừa • giữ " + result.OverridesPreserved + " override.",
                    "Family property save");
            }
            catch (Exception ex) { SetStatus("Lưu Family property lỗi: " + ex.Message); }
        }

        private void OnRemovePropertyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var key = (PropertyKeyBox.Text ?? string.Empty).Trim();
                if (key.Length == 0)
                {
                    SetStatus("Custom property là tùy chọn. Chọn một property hoặc nhập Key trước khi xóa.");
                    return;
                }

                EnsureActive("xóa Family property");
                var project = ExistingProjectMutationContext.Require(_document, "Xóa Family property");
                var family = RequireSelectedFamily(project);
                var result = ExecuteAtomic(project, () =>
                {
                    var beforeVersion = project.ChangeVersion;
                    var update = ProjectFamilyService.RemoveProperty(project, family.Id, key);
                    if (project.ChangeVersion != beforeVersion)
                        AuditTrail.ForProject(project).Record("family.property.remove", string.Empty, family.Id + " • " + key + " • inherited=" + update.InheritedInstancesUpdated + " • overrides=" + update.OverridesPreserved);
                    return update;
                }, "Xóa Family property");

                RefreshAfterCommit(
                    () => RefreshAll(family.Id),
                    "Đã xóa Family property • bỏ " + result.InheritedInstancesUpdated + " inherited-copy • giữ " + result.OverridesPreserved + " override.",
                    "Family property remove");
            }
            catch (Exception ex) { SetStatus("Xóa Family property lỗi: " + ex.Message); }
        }

        private void OnAssignClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("gán Family cho selection");
                if (!(FamilyList.SelectedItem is ProjectFamily selectedFamily))
                    throw new InvalidOperationException("Chọn Family trước khi thực hiện thao tác.");
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var previewProject))
                    throw new InvalidOperationException("Gán Family cho selection cần một QS3D project hiện hữu; thao tác này không tạo project mới.");
                var previewFamily = previewProject.FindFamily(selectedFamily.Id)
                    ?? throw new InvalidOperationException("Family đã chọn không còn tồn tại trong project hiện tại. Hãy Refresh và chọn lại.");
                var expectedProjectId = previewProject.ProjectId;
                var previewIds = SemanticSelectionResolver.ResolveImplied(_document, previewProject)
                    .Select(x => x.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (previewIds.Count == 0) throw new InvalidOperationException("Selection hiện tại không resolve được QS3D semantic element.");

                var project = ExistingProjectMutationContext.Require(_document, "Gán Family cho selection");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("QS3D project đã thay đổi sau khi đọc selection. Không có Family assignment nào được áp dụng; hãy Refresh và thử lại.");
                var family = project.FindFamily(previewFamily.Id)
                    ?? throw new InvalidOperationException("Family đã thay đổi hoặc bị xóa khỏi project hiện tại. Hãy Refresh và chọn lại.");
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project)
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();
                var currentIds = elements.Select(x => x.Id)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (!previewIds.SequenceEqual(currentIds, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Selection hoặc semantic ownership đã thay đổi trước khi gán Family. Không có mutation nào được áp dụng; hãy chọn lại và thử lại.");

                var previous = elements.ToDictionary(x => x.Id, x => x.FamilyId, StringComparer.OrdinalIgnoreCase);
                var changed = ExecuteAtomic(project, () =>
                {
                    var count = ProjectFamilyService.Assign(project, family.Id, elements);
                    foreach (var element in elements)
                        if (previous.TryGetValue(element.Id, out var before) && !string.Equals(before, element.FamilyId, StringComparison.OrdinalIgnoreCase))
                            AuditTrail.ForProject(project).Record("family.assign", element.Id, before + " -> " + family.Id);
                    return count;
                }, "Gán Family cho selection");

                RefreshAfterCommit(
                    () => RefreshAll(family.Id),
                    "Đã gán Family “" + family.Name + "” cho " + changed + "/" + elements.Count + " semantic element.",
                    "Family assign");
            }
            catch (Exception ex) { SetStatus("Gán Family lỗi: " + ex.Message); }
        }

        private void RefreshAll(string preferredId = "")
        {
            try
            {
                var previous = string.IsNullOrWhiteSpace(preferredId) ? (FamilyList.SelectedItem as ProjectFamily)?.Id : preferredId;
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                {
                    _boundProject = null;
                    _loading = true;
                    try { FamilyList.ItemsSource = null; FamilyList.SelectedItem = null; }
                    finally { _loading = false; }
                    _creatingNew = false;
                    ClearFamilyEditor();
                    ActiveFamilyText.Text = "—";
                    Title = "QS3D • Family Manager • " + DrawingLabel(_document);
                    SetStatus("Chưa có QS3D project hiện hữu cho bản vẽ này. Family Manager không tạo replacement project khi chỉ đọc.");
                    return;
                }

                var filter = (CategoryFilter.SelectedItem as CategoryChoice)?.Category;
                var families = project.Families.Where(x => !filter.HasValue || x.Category == filter.Value).OrderBy(x => x.Category).ThenBy(x => x.Name).ToList();
                _loading = true;
                try { FamilyList.ItemsSource = families; FamilyList.SelectedItem = families.FirstOrDefault(x => string.Equals(x.Id, previous, StringComparison.OrdinalIgnoreCase)) ?? families.FirstOrDefault(); }
                finally { _loading = false; }
                _creatingNew = false;
                LoadFamily();
                _boundProject = project;
                Title = "QS3D • Family Manager • " + DrawingLabel(_document);
            }
            catch (Exception ex) { SetStatus("Đọc Family Catalog lỗi: " + ex.Message); }
        }

        private void LoadFamily()
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
            {
                ActiveFamilyText.Text = "—";
                ClearFamilyEditor();
                return;
            }

            var active = ProjectFamilyActivationService.GetActive(project);
            ActiveFamilyText.Text = active == null ? "—" : active.Name + " • " + active.Category;
            if (!(FamilyList.SelectedItem is ProjectFamily selected))
            {
                ClearFamilyEditor();
                return;
            }

            var family = project.FindFamily(selected.Id);
            if (family == null)
            {
                ClearFamilyEditor();
                SetStatus("Family đã chọn không còn tồn tại trong project hiện tại. Hãy Refresh và chọn lại.");
                return;
            }

            FamilyNameBox.Text = family.Name;
            NewCategoryCombo.SelectedItem = (NewCategoryCombo.ItemsSource as IEnumerable<CategoryChoice>)?.FirstOrDefault(x => x.Category == family.Category);
            PropertyList.ItemsSource = family.Properties.OrderBy(x => x.Key).Select(x => new PropertyRow { Key = x.Key, Value = x.Value }).ToList();
            ReferenceCountText.Text = ProjectFamilyService.ReferenceCount(project, family.Id).ToString(CultureInfo.InvariantCulture);
            PropertyKeyBox.Text = string.Empty;
            PropertyValueBox.Text = string.Empty;
        }

        private void ClearFamilyEditor()
        {
            FamilyNameBox.Text = string.Empty;
            PropertyList.ItemsSource = null;
            PropertyKeyBox.Text = string.Empty;
            PropertyValueBox.Text = string.Empty;
            ReferenceCountText.Text = "0";
        }

        private ProjectFamily RequireSelectedFamily(ProjectState project)
        {
            if (!(FamilyList.SelectedItem is ProjectFamily selected))
                throw new InvalidOperationException("Chọn Family trước khi thực hiện thao tác.");
            return project.FindFamily(selected.Id)
                ?? throw new InvalidOperationException("Family đã chọn không còn tồn tại trong project hiện tại.");
        }

        private static T ExecuteAtomic<T>(ProjectState project, Func<T> operation, string operationName)
        {
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                return operation();
            }
            catch (Exception operationError)
            {
                try
                {
                    rollback.Restore(project);
                }
                catch (Exception restoreError)
                {
                    throw new InvalidOperationException(
                        operationName + " thất bại và rollback project cũng không hoàn tất.",
                        new AggregateException(operationError, restoreError));
                }
                throw;
            }
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

        private static string NextCopyName(ProjectState project, ProjectFamily source)
        {
            for (var index = 1; index <= 10000; index++)
            {
                var candidate = source.Name + (index == 1 ? " Copy" : " Copy " + index.ToString(CultureInfo.InvariantCulture));
                if (!project.Families.Any(x => x.Category == source.Category && string.Equals(x.Name, candidate, StringComparison.OrdinalIgnoreCase))) return candidate;
            }
            throw new InvalidOperationException("Không tìm được tên Family duplicate an toàn.");
        }

        private void EnsureActive(string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Family Manager trước khi " + operation + ".");
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var currentProject) ||
                _boundProject == null ||
                !ReferenceEquals(currentProject, _boundProject))
                throw new InvalidOperationException("QS3D project đã thay đổi từ lần Refresh gần nhất. Hãy Refresh Family Manager trước khi " + operation + ".");
        }

        private static string DrawingLabel(Document document) { var name = document.Name ?? string.Empty; if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu"; try { return System.IO.Path.GetFileName(name); } catch { return name; } }
        private void SetStatus(string text) { StatusText.Text = text ?? string.Empty; try { PaletteCoordinator.SetStatus(StatusText.Text); } catch { } }
    }
}
