using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Dedicated Workspace integration for Móng đơn. This intentionally layers on top of the
    /// established BLT3D Family workspace instead of changing the generic Foundation workflows.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool SingleFootingWorkspaceBootstrapRegistered =
            RegisterSingleFootingWorkspaceBootstrap();

        private bool _singleFootingWorkspaceIntegrated;

        private static bool RegisterSingleFootingWorkspaceBootstrap()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnSingleFootingWorkspaceLoaded),
                true);
            return true;
        }

        private static void OnSingleFootingWorkspaceLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel)) return;
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(panel.EnsureSingleFootingWorkspaceIntegration));
        }

        private void EnsureSingleFootingWorkspaceIntegration()
        {
            if (!SingleFootingWorkspaceBootstrapRegistered || _singleFootingWorkspaceIntegrated) return;

            EnsureSingleFootingTreeNode();
            ModelTree.SelectedItemChanged -= OnSingleFootingTreeSelectionChanged;
            ModelTree.SelectedItemChanged += OnSingleFootingTreeSelectionChanged;

            // The BLT3D surface rewires Add after the base Workspace initializes. Rewire once more
            // at ContextIdle so Móng đơn gets the six-parameter dialog while all other categories
            // retain the existing Tham số / Solid3D chooser.
            foreach (var button in FindVisualChildren<Button>(this).Where(IsBlt3dFamilyAddButton))
            {
                button.Click -= OnAddClick;
                button.Click -= OnFamilyAddModeClick;
                button.Click -= OnBlt3dFamilyAddClick;
                button.Click -= OnSingleFootingAwareAddClick;
                button.Click += OnSingleFootingAwareAddClick;
                button.ToolTip = "Add Family — Móng đơn nhập L1/W1/L2/W2/H1/H2; các nhóm khác giữ workflow hiện tại";
            }

            var menu = FamilyList.ContextMenu;
            if (menu != null)
            {
                foreach (var item in menu.Items.OfType<MenuItem>().Where(item =>
                             string.Equals(item.Header as string, "Nhân bản Family", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(item.Header as string, "Thêm Family…", StringComparison.OrdinalIgnoreCase)))
                {
                    item.Click -= OnAddClick;
                    item.Click -= OnFamilyAddModeClick;
                    item.Click -= OnBlt3dFamilyAddClick;
                    item.Click -= OnSingleFootingAwareAddClick;
                    item.Click += OnSingleFootingAwareAddClick;
                    item.Header = "Thêm Family…";
                }
            }

            _singleFootingWorkspaceIntegrated = true;
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
                .FirstOrDefault(item => string.Equals(item.Header as string, SingleFootingContract.SubtypeName, StringComparison.CurrentCultureIgnoreCase));
            if (existing == null)
            {
                existing = new TreeViewItem
                {
                    Header = SingleFootingContract.SubtypeName,
                    Tag = ElementCategory.Foundation.ToString(),
                    ToolTip = "Móng đơn — Add để nhập L1/W1/L2/W2/H1/H2, Vẽ để pick tâm",
                    MinHeight = 22,
                    Padding = new Thickness(2, 1, 2, 1),
                    Margin = new Thickness(0)
                };
                foundation.Items.Insert(0, existing);
            }

            foundation.IsExpanded = true;
            TuneTreeItem(foundation, 0);
        }

        private void OnSingleFootingTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(e.NewValue is TreeViewItem item) || !IsSingleFootingTreeItem(item)) return;

            _categoryFilter = ElementCategory.Foundation;
            _familySubtypeFilter = SingleFootingContract.SubtypeName;
            ApplyFamilySubtypeFilter();

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

        private void OnSingleFootingAwareAddClick(object sender, RoutedEventArgs e)
        {
            if (!IsSingleFootingSelected())
            {
                OnBlt3dFamilyAddClick(sender, e);
                return;
            }

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
            if (!(item.Tag is string tag) ||
                !Enum.TryParse(tag, true, out ElementCategory category) ||
                category != ElementCategory.Foundation) return false;
            return string.Equals(
                (item.Header as string ?? string.Empty).Trim(),
                SingleFootingContract.SubtypeName,
                StringComparison.CurrentCultureIgnoreCase);
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
                        family.Id + " • " + family.Name + " • L1/W1=" +
                        dimensions.L1M.ToString("0.###") + "/" + dimensions.W1M.ToString("0.###") + " m");
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
                        ApplyFamilySubtypeFilter();
                        var live = _viewModel.Families.FirstOrDefault(x =>
                            string.Equals(x.Id, created.Id, StringComparison.OrdinalIgnoreCase));
                        FamilyList.SelectedItem = live;
                        if (live != null) _viewModel.SetActiveFamily(live);
                        RefreshSelectedFamilyHighlight();
                    },
                    "Đã tạo " + created.Name + ". Bấm Vẽ rồi pick tâm móng; Esc/Enter để kết thúc.",
                    "Workspace Móng đơn");
            }
            catch (Exception ex)
            {
                SetStatus("Tạo Móng đơn lỗi: " + ex.Message);
            }
        }
    }
}
