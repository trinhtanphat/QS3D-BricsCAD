using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Keeps the floor-opening workspace route distinct from the generic Slab category.
    /// slabOpen intentionally uses WallOpening for persistence, so category-only filtering
    /// cannot safely activate it from the Sàn branch.
    /// </summary>
    public partial class WorkspacePanel
    {
        private const string SlabOpeningWorkspaceTag = "QS3D.SlabOpen";
        private bool _slabOpeningWorkspaceRouteAttached;

        private void EnsureSlabOpeningWorkspaceRoute()
        {
            if (_slabOpeningWorkspaceRouteAttached || ModelTree == null) return;
            _slabOpeningWorkspaceRouteAttached = true;

            var openingNode = FindTreeItem(ModelTree.Items, item =>
                string.Equals((item.Header as string)?.Trim(), "Lỗ Mở Sàn", StringComparison.CurrentCultureIgnoreCase));

            if (openingNode == null)
            {
                var slabLeaf = ModelTree.Items
                    .OfType<TreeViewItem>()
                    .FirstOrDefault(item =>
                        string.Equals(item.Tag as string, ElementCategory.Slab.ToString(), StringComparison.OrdinalIgnoreCase));

                if (slabLeaf != null)
                {
                    var index = ModelTree.Items.IndexOf(slabLeaf);
                    ModelTree.Items.Remove(slabLeaf);

                    slabLeaf.Header = "Sàn Đặc";
                    var slabGroup = new TreeViewItem
                    {
                        Header = "Sàn",
                        IsExpanded = true
                    };
                    openingNode = new TreeViewItem
                    {
                        Header = "Lỗ Mở Sàn",
                        Tag = SlabOpeningWorkspaceTag
                    };
                    slabGroup.Items.Add(slabLeaf);
                    slabGroup.Items.Add(openingNode);
                    ModelTree.Items.Insert(index, slabGroup);
                }
            }
            else
            {
                openingNode.Tag = SlabOpeningWorkspaceTag;
            }

            ModelTree.SelectedItemChanged += OnSlabOpeningWorkspaceTreeSelectionChanged;
            FamilySearch.TextChanged += OnSlabOpeningWorkspaceFamilySearchChanged;
        }

        private void OnSlabOpeningWorkspaceTreeSelectionChanged(
            object sender,
            System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(e.NewValue is TreeViewItem item) ||
                !string.Equals(item.Tag as string, SlabOpeningWorkspaceTag, StringComparison.Ordinal))
                return;

            // The generic tree handler runs first and intentionally clears its category filter for
            // this special tag. Re-apply the semantic exact-family filter instead of pretending that
            // slabOpen is an ordinary Slab (it is persisted as WallOpening by contract).
            _categoryFilter = null;
            ApplySlabOpeningWorkspaceFamilyFilter();

            ProjectFamily? family;
            try
            {
                family = FindOrCreateExactSlabOpeningFamily();
            }
            catch (Exception ex)
            {
                SetStatus("Khởi tạo/kích hoạt exact slabOpen lỗi: " + ex.Message);
                return;
            }

            if (family == null)
            {
                _loadingContext = true;
                try { FamilyList.SelectedItem = null; }
                finally { _loadingContext = false; }

                SetStatus("Lỗ Mở Sàn yêu cầu Family exact slabOpen; không dùng Family Sàn thay thế.");
                return;
            }

            _loadingContext = true;
            try
            {
                FamilyList.SelectedItem = family;
                FamilyList.ScrollIntoView(family);
            }
            finally { _loadingContext = false; }

            try
            {
                _viewModel.SetActiveFamily(family);
                _viewModel.ShowFamilyProperties();
                SetStatus("Nhóm mô hình: Lỗ Mở Sàn • Active Family: slabOpen • Direct Draw -Z + Auto BoolSubtract.");
            }
            catch (Exception ex)
            {
                SetStatus("Kích hoạt exact slabOpen lỗi: " + ex.Message);
            }
        }

        private void OnSlabOpeningWorkspaceFamilySearchChanged(object sender, TextChangedEventArgs e)
        {
            if (IsSlabOpeningWorkspaceRouteSelected())
                ApplySlabOpeningWorkspaceFamilyFilter();
        }

        private bool IsSlabOpeningWorkspaceRouteSelected()
        {
            return ModelTree?.SelectedItem is TreeViewItem item &&
                   string.Equals(item.Tag as string, SlabOpeningWorkspaceTag, StringComparison.Ordinal);
        }

        private ProjectFamily? ResolveWorkspaceDrawFamily()
        {
            if (!IsSlabOpeningWorkspaceRouteSelected())
                return FamilyList.SelectedItem as ProjectFamily;

            ApplySlabOpeningWorkspaceFamilyFilter();
            var family = FindOrCreateExactSlabOpeningFamily();
            if (family == null)
            {
                SetStatus("Không thể vẽ Lỗ Mở Sàn: cần exact Family slabOpen; không fallback sang Slab/WallOpening khác.");
                return null;
            }

            _loadingContext = true;
            try
            {
                FamilyList.SelectedItem = family;
                FamilyList.ScrollIntoView(family);
            }
            finally { _loadingContext = false; }

            return family;
        }

        private ProjectFamily? FindExactSlabOpeningFamily()
        {
            return FindUniqueExactSlabOpeningFamily(_viewModel.Families);
        }

        private static ProjectFamily? FindUniqueExactSlabOpeningFamily(
            System.Collections.Generic.IEnumerable<ProjectFamily> families)
        {
            if (families == null) throw new ArgumentNullException(nameof(families));
            var matches = families
                .Where(SlabOpeningContract.IsSlabOpenFamily)
                .Take(2)
                .ToList();
            if (matches.Count > 1)
                throw new InvalidOperationException(
                    "Project có nhiều Family cùng thỏa exact slabOpen. Hãy giữ đúng một Family slabOpen trước khi vẽ Lỗ Mở Sàn.");
            return matches.Count == 1 ? matches[0] : null;
        }

        private ProjectFamily? FindOrCreateExactSlabOpeningFamily()
        {
            var family = FindExactSlabOpeningFamily();
            if (family != null) return family;

            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
                throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active để khởi tạo slabOpen.");

            var project = ExistingProjectMutationContext.Require(document, "Khởi tạo Family slabOpen từ Lỗ Mở Sàn");
            family = FindUniqueExactSlabOpeningFamily(project.Families);
            if (family == null)
            {
                family = ProjectFamilyService.Create(
                    project,
                    SlabOpeningContract.FamilyKey,
                    SlabOpeningContract.FamilyKey,
                    ElementCategory.WallOpening);
            }

            if (!_viewModel.Families.Contains(family))
                _viewModel.Families.Add(family);
            return family;
        }

        private void ApplySlabOpeningWorkspaceFamilyFilter()
        {
            var view = CollectionViewSource.GetDefaultView(FamilyList?.ItemsSource);
            if (view == null) return;

            var text = FamilySearch?.Text?.Trim() ?? string.Empty;
            view.Filter = item =>
                item is ProjectFamily family &&
                SlabOpeningContract.IsSlabOpenFamily(family) &&
                (text.Length == 0 ||
                 family.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 family.Category.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
            view.Refresh();
        }

        private static TreeViewItem? FindTreeItem(
            ItemCollection items,
            Func<TreeViewItem, bool> predicate)
        {
            foreach (var value in items)
            {
                if (!(value is TreeViewItem item)) continue;
                if (predicate(item)) return item;

                var nested = FindTreeItem(item.Items, predicate);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
