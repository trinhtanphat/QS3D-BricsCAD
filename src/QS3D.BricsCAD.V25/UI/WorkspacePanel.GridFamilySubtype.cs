using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Grid-specific Workspace subtype overlay. Grid stays a semantic/reference category;
    /// the leaf selection only chooses the straight/curved authoring contract.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly string[] GridFamilySubtypes =
        {
            "Lưới Thẳng", "Lưới Cong"
        };

        private static readonly bool GridFamilySubtypeInteractionsRegistered =
            RegisterGridFamilySubtypeInteractions();

        private bool _gridFamilySubtypeInteractionsAttached;

        private static bool RegisterGridFamilySubtypeInteractions()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnWorkspaceLoadedForGridFamilySubtype),
                true);
            return true;
        }

        private static void OnWorkspaceLoadedForGridFamilySubtype(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel)
                panel.AttachGridFamilySubtypeInteractions();
        }

        private void AttachGridFamilySubtypeInteractions()
        {
            if (!GridFamilySubtypeInteractionsRegistered || _gridFamilySubtypeInteractionsAttached) return;
            _gridFamilySubtypeInteractionsAttached = true;

            ModelTree.SelectedItemChanged += OnGridFamilySubtypeTreeSelectionChanged;
            FamilySearch.TextChanged += OnGridFamilySubtypeSearchChanged;
            RewireGridAwareFamilyAddActions();

            if (ModelTree.SelectedItem is TreeViewItem selected)
                ApplyGridTreeSelection(selected);
        }

        private void RewireGridAwareFamilyAddActions()
        {
            foreach (var button in FindVisualChildren<Button>(this).Where(IsWorkspaceAddFamilyButton))
            {
                button.Click -= OnFamilyAddModeClick;
                button.Click -= OnGridAwareFamilyAddModeClick;
                button.Click += OnGridAwareFamilyAddModeClick;
            }

            var menu = FamilyList.ContextMenu;
            if (menu == null) return;
            foreach (var item in menu.Items.OfType<MenuItem>().Where(x =>
                         string.Equals(x.Header as string, "Nhân bản Family", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(x.Header as string, "Thêm Family…", StringComparison.OrdinalIgnoreCase)))
            {
                item.Click -= OnFamilyAddModeClick;
                item.Click -= OnGridAwareFamilyAddModeClick;
                item.Click += OnGridAwareFamilyAddModeClick;
            }
        }

        private void OnGridAwareFamilyAddModeClick(object sender, RoutedEventArgs e)
        {
            if (!IsGridSubtype(_familySubtypeFilter))
            {
                OnFamilyAddModeClick(sender, e);
                return;
            }

            e.Handled = true;
            var menu = CreateContextMenu();
            menu.Items.Add(CreateMenuItem("Tham số", (s, args) => CreateGridFamilyFromWorkspaceSubtype(false)));
            menu.Items.Add(CreateMenuItem("Solid3D", (s, args) => CreateGridFamilyFromWorkspaceSubtype(true)));
            menu.PlacementTarget = sender as UIElement ?? FamilyList;
            menu.Placement = sender is Button ? System.Windows.Controls.Primitives.PlacementMode.Bottom : System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        private void OnGridFamilySubtypeTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item)
                ApplyGridTreeSelection(item);
        }

        private void ApplyGridTreeSelection(TreeViewItem item)
        {
            var subtype = ResolveGridSubtype(item);
            if (subtype.Length == 0) return;

            _familySubtypeFilter = subtype;
            _categoryFilter = ElementCategory.Grid;
            ApplyGridFamilySubtypeFilter();

            var first = FamilyList.Items.Cast<object>().OfType<ProjectFamily>().FirstOrDefault();
            _loadingContext = true;
            try
            {
                FamilyList.SelectedItem = first;
                if (first != null)
                {
                    _viewModel.SetActiveFamily(first);
                    _viewModel.ShowFamilyProperties();
                    SetStatus("Nhóm mô hình: " + subtype + " • " + first.Name);
                }
                else
                {
                    _viewModel.SelectedFamilyName = string.Empty;
                    _viewModel.Properties.Clear();
                    SetStatus("Nhóm mô hình: " + subtype + " • chưa có Family; bấm Add để tạo Family lưới.");
                }
            }
            finally { _loadingContext = false; }
            RefreshSelectedFamilyHighlight();
        }

        private void OnGridFamilySubtypeSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (IsGridSubtype(_familySubtypeFilter))
                ApplyGridFamilySubtypeFilter();
        }

        private void ApplyGridFamilySubtypeFilter()
        {
            if (_applyingFamilySubtypeFilter || !IsGridSubtype(_familySubtypeFilter)) return;
            _applyingFamilySubtypeFilter = true;
            try
            {
                var text = FamilySearch?.Text?.Trim() ?? string.Empty;
                var subtype = _familySubtypeFilter;
                var view = CollectionViewSource.GetDefaultView(FamilyList?.ItemsSource);
                if (view == null) return;
                view.Filter = item => item is ProjectFamily family &&
                    family.Category == ElementCategory.Grid &&
                    FamilyNameHasSubtype(family.Name, subtype) &&
                    (text.Length == 0 ||
                     family.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                     family.Category.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
                view.Refresh();
            }
            finally { _applyingFamilySubtypeFilter = false; }
        }

        private void CreateGridFamilyFromWorkspaceSubtype(bool launchSolid3D)
        {
            try
            {
                var subtype = _familySubtypeFilter;
                if (!IsGridSubtype(subtype))
                    throw new InvalidOperationException("Hãy chọn Lưới Thẳng hoặc Lưới Cong trước khi tạo Family.");

                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active.");
                if (launchSolid3D && !Cad.NativeBuildCapability.Supports(ElementCategory.Grid))
                {
                    SetStatus(Cad.NativeBuildCapability.UnsupportedMessage(ElementCategory.Grid));
                    return;
                }

                var selected = FamilyList.SelectedItem as ProjectFamily;
                if (selected != null &&
                    (selected.Category != ElementCategory.Grid || !FamilyNameHasSubtype(selected.Name, subtype)))
                    selected = null;

                var project = selected == null
                    ? ProjectContextCoordinator.GetOrCreate(doc)
                    : ExistingProjectMutationContext.Require(doc, "Thêm Family lưới từ Workspace");
                var basis = selected == null ? null : project.FindFamily(selected.Id);
                if (selected != null && basis == null)
                    throw new InvalidOperationException("Family lưới đang chọn không còn tồn tại trong project hiện tại. Hãy Refresh Workspace.");

                var existingNames = new HashSet<string>(
                    project.Families.Where(x => x.Category == ElementCategory.Grid).Select(x => x.Name),
                    StringComparer.OrdinalIgnoreCase);
                var name = NextSubtypeFamilyName(subtype, existingNames);

                var created = ExecuteAtomic(project, () =>
                {
                    ProjectFamily family;
                    if (basis != null)
                    {
                        family = ProjectFamilyService.Duplicate(project, basis.Id, Guid.NewGuid().ToString("N"), name);
                        AuditTrail.ForProject(project).Record(
                            "family.duplicate", string.Empty,
                            basis.Id + " -> " + family.Id + " • " + family.Name + " • Workspace Grid " + subtype);
                    }
                    else
                    {
                        family = ProjectFamilyService.Create(project, Guid.NewGuid().ToString("N"), name, ElementCategory.Grid);
                        SeedQuickSchemaDefaults(family);
                        AuditTrail.ForProject(project).Record(
                            "family.create", string.Empty,
                            family.Id + " • Grid • " + family.Name + " • Workspace Grid " + subtype);
                    }
                    SeedGridFamilyDefaults(family, subtype);
                    ProjectFamilyActivationService.SetActive(project, family.Id);
                    return family;
                }, "Tạo Family " + subtype + " từ Workspace");

                RefreshAfterCommit(
                    () =>
                    {
                        RefreshProject();
                        _categoryFilter = ElementCategory.Grid;
                        _familySubtypeFilter = subtype;
                        ApplyGridFamilySubtypeFilter();
                        var live = _viewModel.Families.FirstOrDefault(x =>
                            string.Equals(x.Id, created.Id, StringComparison.OrdinalIgnoreCase));
                        FamilyList.SelectedItem = live;
                        if (live != null) _viewModel.ShowFamilyProperties();
                        RefreshSelectedFamilyHighlight();
                        if (launchSolid3D) OnView3DClick(this, new RoutedEventArgs());
                    },
                    "Đã tạo Family “" + created.Name + "” • " + subtype + ".",
                    "Workspace Grid Family");
            }
            catch (Exception ex)
            {
                SetStatus("Tạo Family lưới lỗi: " + ex.Message);
            }
        }

        private static void SeedGridFamilyDefaults(ProjectFamily family, string subtype)
        {
            if (family.Category != ElementCategory.Grid) return;
            SeedGridDefault(family, "GridAxisName", "1");
            SeedGridDefault(family, "GridLocked", "false");
            SeedGridDefault(family, "GridStartBubbleVisible", "true");
            SeedGridDefault(family, "GridEndBubbleVisible", "true");
            SeedGridDefault(family, "DisplayColor", "Theo loại (mặc định)");
            SeedGridDefault(family, "DisplayTransparencyPercent", "0");
            SeedGridDefault(family, "Mark", string.Empty);
            SeedGridDefault(family, "Comment", string.Empty);
            SeedGridDefault(family, "WBS", string.Empty);
            SeedGridDefault(family, "Material", "Khác");
            if (string.Equals(subtype, "Lưới Cong", StringComparison.OrdinalIgnoreCase))
                SeedGridDefault(family, "GridRadiusM", "0.5");
        }

        private static void SeedGridDefault(ProjectFamily family, string key, string value)
        {
            if (!family.Properties.ContainsKey(key)) family.Properties[key] = value;
        }

        private static string ResolveGridSubtype(TreeViewItem item)
        {
            if (!(item.Tag is string tag) ||
                !Enum.TryParse(tag, true, out ElementCategory category) ||
                category != ElementCategory.Grid) return string.Empty;
            var header = (item.Header as string ?? string.Empty).Trim();
            return GridFamilySubtypes.FirstOrDefault(x =>
                string.Equals(x, header, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        }

        private static string InferGridSubtype(string familyName) =>
            GridFamilySubtypes.FirstOrDefault(x => FamilyNameHasSubtype(familyName, x)) ?? string.Empty;

        private static bool IsGridSubtype(string subtype) =>
            GridFamilySubtypes.Any(x => string.Equals(x, subtype, StringComparison.OrdinalIgnoreCase));
    }
}
