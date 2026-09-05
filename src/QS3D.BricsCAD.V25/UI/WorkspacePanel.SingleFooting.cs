using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Dedicated Workspace state integration for Móng đơn. The canonical BLT3D Add handler owns
    /// the synchronous Add route; this partial owns stable subtype identity/filtering and delegates
    /// the six-field editor to WorkspacePanel.SingleFooting.Properties.cs.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool SingleFootingWorkspaceBootstrapRegistered =
            RegisterSingleFootingWorkspaceBootstrap();

        private bool _applyingSingleFootingFilter;

        private static bool RegisterSingleFootingWorkspaceBootstrap()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnSingleFootingWorkspaceLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(TreeView),
                TreeView.SelectedItemChangedEvent,
                new RoutedPropertyChangedEventHandler<object>(OnSingleFootingTreeClassHandler),
                true);
            EventManager.RegisterClassHandler(
                typeof(ListBox),
                Selector.SelectionChangedEvent,
                new SelectionChangedEventHandler(OnSingleFootingFamilyClassHandler),
                true);
            EventManager.RegisterClassHandler(
                typeof(TextBox),
                TextBoxBase.TextChangedEvent,
                new TextChangedEventHandler(OnSingleFootingSearchClassHandler),
                true);
            return true;
        }

        private static void OnSingleFootingWorkspaceLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !SingleFootingWorkspaceBootstrapRegistered) return;
            panel.EnsureSingleFootingTreeNode();
        }

        private void EnsureSingleFootingTreeNode()
        {
            var foundation = ModelTree.Items
                .OfType<TreeViewItem>()
                .FirstOrDefault(item =>
                    string.Equals(item.Tag as string, ElementCategory.Foundation.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Header as string, "Móng", StringComparison.CurrentCultureIgnoreCase));
            if (foundation == null) return;

            var existing = foundation.Items
                .OfType<TreeViewItem>()
                .FirstOrDefault(item =>
                    string.Equals(item.Tag as string, SingleFootingContract.CategoryCode, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Header as string, SingleFootingContract.SubtypeName, StringComparison.CurrentCultureIgnoreCase));
            if (existing == null)
            {
                existing = new TreeViewItem
                {
                    Header = SingleFootingContract.SubtypeName,
                    ToolTip = "Móng đơn — Add để nhập L1/W1/L2/W2/H1/H2, Vẽ để pick tâm",
                    MinHeight = 22,
                    Padding = new Thickness(2, 1, 2, 1),
                    Margin = new Thickness(0)
                };
                foundation.Items.Insert(0, existing);
            }

            // Stable routing identity is never the localized display text or generic Foundation.
            existing.Tag = SingleFootingContract.CategoryCode;
            existing.Header = SingleFootingContract.SubtypeName;
            foundation.IsExpanded = true;
            TuneTreeItem(foundation, 0);
        }

        private static void OnSingleFootingTreeClassHandler(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(sender is TreeView tree) || !(e.NewValue is TreeViewItem item) || !IsSingleFootingTreeItem(item)) return;
            var panel = FindSingleFootingWorkspace(tree);
            if (panel == null) return;
            panel.HandleSingleFootingTreeSelection();
            e.Handled = true;
        }

        private static void OnSingleFootingFamilyClassHandler(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ListBox list)) return;
            var panel = FindSingleFootingWorkspace(list);
            if (panel == null || !ReferenceEquals(list, panel.FamilyList) || panel._loadingContext) return;
            if (!panel.IsSingleFootingSelected() || !(list.SelectedItem is ProjectFamily family) ||
                !SingleFootingContract.IsSingleFooting(family)) return;

            panel.HandleSingleFootingFamilySelection(family);
            e.Handled = true;
        }

        private static void OnSingleFootingSearchClassHandler(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox textBox)) return;
            var panel = FindSingleFootingWorkspace(textBox);
            if (panel == null || !ReferenceEquals(textBox, panel.FamilySearch) || !panel.IsSingleFootingSelected()) return;
            panel.ApplySingleFootingFamilyFilter();
            e.Handled = true;
        }

        private static WorkspacePanel? FindSingleFootingWorkspace(DependencyObject? source)
        {
            var current = source;
            while (current != null)
            {
                if (current is WorkspacePanel panel) return panel;
                try { current = ParentOf(current); }
                catch { return null; }
            }
            return null;
        }

        private void HandleSingleFootingTreeSelection()
        {
            HideBlt3dFamilyModeChooser();
            _categoryFilter = ElementCategory.Foundation;
            _familySubtypeFilter = SingleFootingContract.SubtypeName;
            ApplySingleFootingFamilyFilter();

            var first = FamilyList.Items.Cast<object>().OfType<ProjectFamily>().FirstOrDefault();
            _loadingContext = true;
            try
            {
                FamilyList.SelectedItem = first;
                if (first != null)
                {
                    _viewModel.SetActiveFamily(first);
                    _viewModel.ShowFamilyProperties();
                    SetStatus("Móng đơn • " + first.Name + " • bấm Vẽ rồi pick tâm móng.");
                }
                else
                {
                    _viewModel.SelectedFamilyName = string.Empty;
                    _viewModel.Properties.Clear();
                    SetStatus("Móng đơn • chưa có Family. Bấm + Add để nhập L1/W1/L2/W2/H1/H2.");
                }
            }
            finally { _loadingContext = false; }
            RefreshSelectedFamilyHighlight();
        }

        private void HandleSingleFootingFamilySelection(ProjectFamily family)
        {
            try
            {
                _categoryFilter = ElementCategory.Foundation;
                _familySubtypeFilter = SingleFootingContract.SubtypeName;
                _viewModel.SetActiveFamily(family);
                _viewModel.ShowFamilyProperties();
                RefreshSelectedFamilyHighlight();
                SetStatus("Móng đơn • " + family.Name + " • L1/W1/L2/W2/H1/H2 sẵn sàng chỉnh sửa.");
            }
            catch (Exception ex)
            {
                SetStatus("Chọn Family Móng đơn lỗi: " + ex.Message);
            }
        }

        private void ApplySingleFootingFamilyFilter()
        {
            if (_applyingSingleFootingFilter) return;
            _applyingSingleFootingFilter = true;
            try
            {
                var text = FamilySearch?.Text?.Trim() ?? string.Empty;
                var view = CollectionViewSource.GetDefaultView(FamilyList?.ItemsSource);
                if (view == null) return;
                view.Filter = item => item is ProjectFamily family &&
                    SingleFootingContract.IsSingleFooting(family) &&
                    (text.Length == 0 ||
                     family.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                     SingleFootingContract.CategoryCode.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
                view.Refresh();
            }
            finally { _applyingSingleFootingFilter = false; }
        }

        private void HandleSingleFootingAdd(RoutedEventArgs e)
        {
            e.Handled = true;
            HideBlt3dFamilyModeChooser();

            var dialog = new SingleFootingDimensionsDialog();
            var owner = Window.GetWindow(this);
            if (owner != null) dialog.Owner = owner;
            if (dialog.ShowDialog() != true || dialog.Dimensions == null)
            {
                SetStatus("Đã hủy Add Móng đơn; project không thay đổi.");
                return;
            }

            CreateSingleFootingFamily(dialog.Dimensions);
        }

        private bool IsSingleFootingSelected() =>
            ModelTree.SelectedItem is TreeViewItem item && IsSingleFootingTreeItem(item);

        private static bool IsSingleFootingTreeItem(TreeViewItem item)
        {
            if (item == null) return false;
            return item.Tag is string tag &&
                   string.Equals(tag.Trim(), SingleFootingContract.CategoryCode, StringComparison.OrdinalIgnoreCase);
        }

        private void CreateSingleFootingFamily(SingleFootingDimensions dimensions)
        {
            try
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                if (document == null) throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active.");

                var project = ProjectContextCoordinator.GetOrCreate(document);
                var existingNames = new HashSet<string>(
                    project.Families.Where(x => x.Category == ElementCategory.Foundation).Select(x => x.Name),
                    StringComparer.OrdinalIgnoreCase);
                var name = NextSubtypeFamilyName(SingleFootingContract.SubtypeName, existingNames);

                var created = ExecuteAtomic(project, () =>
                {
                    var family = ProjectFamilyService.Create(
                        project,
                        Guid.NewGuid().ToString("N"),
                        name,
                        ElementCategory.Foundation);
                    SingleFootingContract.Apply(family, dimensions);
                    ProjectFamilyActivationService.SetActive(project, family.Id);
                    AuditTrail.ForProject(project).Record(
                        "family.create.single-footing",
                        string.Empty,
                        family.Id + " • " + family.Name + " • " + SingleFootingContract.CategoryCode + " • L1/W1=" +
                        dimensions.L1M.ToString("0.###", CultureInfo.InvariantCulture) + "/" +
                        dimensions.W1M.ToString("0.###", CultureInfo.InvariantCulture) + " m");
                    return family;
                }, "Tạo Family Móng đơn");

                _categoryFilter = ElementCategory.Foundation;
                _familySubtypeFilter = SingleFootingContract.SubtypeName;
                RefreshAfterCommit(
                    () =>
                    {
                        RefreshProject();
                        _categoryFilter = ElementCategory.Foundation;
                        _familySubtypeFilter = SingleFootingContract.SubtypeName;
                        ApplySingleFootingFamilyFilter();
                        var live = _viewModel.Families.FirstOrDefault(x =>
                            string.Equals(x.Id, created.Id, StringComparison.OrdinalIgnoreCase));
                        FamilyList.SelectedItem = live;
                        if (live != null)
                        {
                            _viewModel.SetActiveFamily(live);
                            _viewModel.ShowFamilyProperties();
                        }
                        RefreshSelectedFamilyHighlight();
                    },
                    "Đã thêm " + created.Name + ". Bấm Vẽ rồi pick tâm móng; Esc/Enter để kết thúc.",
                    "Workspace Móng đơn");
            }
            catch (Exception ex)
            {
                SetStatus("Tạo Móng đơn lỗi: " + ex.Message);
            }
        }
    }
}
