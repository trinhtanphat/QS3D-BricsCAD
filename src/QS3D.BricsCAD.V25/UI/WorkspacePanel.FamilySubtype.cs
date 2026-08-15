using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Keeps the reference Workspace tree subtype-aware without changing the persisted
    /// ProjectFamily schema. Foundation subtypes are resolved from the visible leaf name and
    /// the existing family naming convention (for example "Móng Bè-1").
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly string[] FoundationFamilySubtypes =
        {
            "Bê Tông Lót",
            "Móng Băng",
            "Móng Bè",
            "Dầm Móng",
            "Đài Cọc",
            "Cọc"
        };

        private bool _familySubtypeInteractionsAttached;
        private bool _applyingFamilySubtypeFilter;
        private string _familySubtypeFilter = string.Empty;
        private ListBoxItem? _lastHighlightedFamilyItem;

        private void AttachFamilySubtypeInteractions()
        {
            if (_familySubtypeInteractionsAttached) return;
            _familySubtypeInteractionsAttached = true;

            ModelTree.SelectedItemChanged += OnFamilySubtypeTreeSelectionChanged;
            FamilySearch.TextChanged += OnFamilySubtypeSearchChanged;
            FamilyList.SelectionChanged += OnFamilySubtypeFamilySelectionChanged;
            FamilyList.ItemContainerGenerator.StatusChanged += OnFamilyContainerGeneratorStatusChanged;

            RewireFamilyAddActions();
            RefreshSelectedFamilyHighlight();
        }

        private void RewireFamilyAddActions()
        {
            foreach (var button in FindVisualChildren<Button>(this).Where(IsWorkspaceAddFamilyButton))
            {
                button.Click -= OnAddClick;
                button.Click -= OnFamilyAddModeClick;
                button.Click += OnFamilyAddModeClick;
                button.ToolTip = "Thêm Family theo Tham số hoặc Solid3D";
            }

            var menu = FamilyList.ContextMenu;
            if (menu == null) return;
            foreach (var item in menu.Items.OfType<MenuItem>().Where(x =>
                         string.Equals(x.Header as string, "Nhân bản Family", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(x.Header as string, "Thêm Family…", StringComparison.OrdinalIgnoreCase)))
            {
                item.Click -= OnAddClick;
                item.Click -= OnFamilyAddModeClick;
                item.Click += OnFamilyAddModeClick;
                item.Header = "Thêm Family…";
            }
        }

        private static bool IsWorkspaceAddFamilyButton(Button button)
        {
            var text = button.Content as string;
            return string.Equals(text, "+ Thêm", StringComparison.Ordinal) ||
                   string.Equals(text, "＋  Add", StringComparison.Ordinal) ||
                   string.Equals(text, "Add", StringComparison.Ordinal);
        }

        private void OnFamilyAddModeClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var menu = CreateContextMenu();
            menu.Items.Add(CreateMenuItem("Tham số", OnAddParameterFamilyClick));
            menu.Items.Add(CreateMenuItem("Solid3D", OnAddSolid3dFamilyClick));
            menu.PlacementTarget = sender as UIElement ?? FamilyList;
            menu.Placement = sender is Button ? PlacementMode.Bottom : PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        private void OnAddParameterFamilyClick(object sender, RoutedEventArgs e) =>
            CreateFamilyFromWorkspaceSubtype(launchSolid3D: false);

        private void OnAddSolid3dFamilyClick(object sender, RoutedEventArgs e) =>
            CreateFamilyFromWorkspaceSubtype(launchSolid3D: true);

        private void CreateFamilyFromWorkspaceSubtype(bool launchSolid3D)
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active.");

                var subtype = _familySubtypeFilter;
                var selected = FamilyList.SelectedItem as ProjectFamily;
                if (selected != null && !FamilyMatchesWorkspaceSubtype(selected, subtype)) selected = null;

                var project = selected == null
                    ? ProjectContextCoordinator.GetOrCreate(doc)
                    : ExistingProjectMutationContext.Require(doc, "Thêm Family từ Workspace");
                var basis = selected == null ? null : project.FindFamily(selected.Id);
                if (selected != null && basis == null)
                    throw new InvalidOperationException("Family đang chọn không còn tồn tại trong project hiện tại. Hãy Refresh Workspace.");

                var category = _categoryFilter ?? basis?.Category ?? ElementCategory.Room;
                if (!string.IsNullOrWhiteSpace(subtype)) category = ElementCategory.Foundation;
                var schema = ProjectFamilyQuickSchemaService.GetSchema(category);
                var existingNames = new HashSet<string>(
                    project.Families.Where(x => x.Category == category).Select(x => x.Name),
                    StringComparer.OrdinalIgnoreCase);

                string name;
                if (!string.IsNullOrWhiteSpace(subtype))
                    name = NextSubtypeFamilyName(subtype, existingNames);
                else
                    name = NextWorkspaceFamilyName(basis?.Name ?? schema.DefaultName, existingNames);

                var created = ExecuteAtomic(project, () =>
                {
                    ProjectFamily family;
                    if (basis != null)
                    {
                        family = ProjectFamilyService.Duplicate(
                            project,
                            basis.Id,
                            Guid.NewGuid().ToString("N"),
                            name);
                        AuditTrail.ForProject(project).Record(
                            "family.duplicate",
                            string.Empty,
                            basis.Id + " -> " + family.Id + " • " + family.Name + " • Workspace " +
                            (launchSolid3D ? "Solid3D" : "Tham số"));
                    }
                    else
                    {
                        family = ProjectFamilyService.Create(
                            project,
                            Guid.NewGuid().ToString("N"),
                            name,
                            category);
                        foreach (var property in schema.Properties)
                            family.Properties[property.Key] = property.DefaultValue;
                        AuditTrail.ForProject(project).Record(
                            "family.create",
                            string.Empty,
                            family.Id + " • " + family.Category + " • " + family.Name + " • Workspace " +
                            (launchSolid3D ? "Solid3D" : "Tham số"));
                    }

                    ProjectFamilyActivationService.SetActive(project, family.Id);
                    return family;
                }, launchSolid3D ? "Tạo Family Solid3D từ Workspace" : "Tạo Family tham số từ Workspace");

                RefreshAfterCommit(
                    () =>
                    {
                        RefreshProject();
                        _categoryFilter = category;
                        _familySubtypeFilter = subtype;
                        ApplyFamilySubtypeFilter();
                        var live = _viewModel.Families.FirstOrDefault(x =>
                            string.Equals(x.Id, created.Id, StringComparison.OrdinalIgnoreCase));
                        FamilyList.SelectedItem = live;
                        if (live != null) FamilyList.ScrollIntoView(live);
                        RefreshSelectedFamilyHighlight();
                        if (launchSolid3D) OnView3DClick(this, new RoutedEventArgs());
                    },
                    launchSolid3D
                        ? "Đã tạo Family “" + created.Name + "”; chuyển sang workflow Solid3D native."
                        : "Đã tạo Family tham số “" + created.Name + "”.",
                    launchSolid3D ? "Workspace Family Solid3D" : "Workspace Family parameter");
            }
            catch (Exception ex)
            {
                SetStatus((launchSolid3D ? "Tạo Family Solid3D lỗi: " : "Tạo Family tham số lỗi: ") + ex.Message);
            }
        }

        private static string NextSubtypeFamilyName(string subtype, ISet<string> existingNames)
        {
            var baseName = (subtype ?? string.Empty).Trim();
            if (baseName.Length == 0) baseName = "Family";
            for (var i = 1; i < 10000; i++)
            {
                var candidate = baseName + "-" + i;
                if (!existingNames.Contains(candidate)) return candidate;
            }
            throw new InvalidOperationException("Không thể tạo tên Family duy nhất cho " + baseName + ".");
        }

        private static string NextWorkspaceFamilyName(string sourceName, ISet<string> existingNames)
        {
            var baseName = string.IsNullOrWhiteSpace(sourceName) ? "Family" : sourceName.Trim();
            for (var i = 2; i < 10000; i++)
            {
                var candidate = baseName + "-" + i;
                if (!existingNames.Contains(candidate)) return candidate;
            }
            throw new InvalidOperationException("Không thể tạo tên Family duy nhất.");
        }

        private void OnFamilySubtypeTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(e.NewValue is TreeViewItem item)) return;

            _familySubtypeFilter = ResolveFoundationSubtype(item);
            ApplyFamilySubtypeFilter();

            if (string.IsNullOrWhiteSpace(_familySubtypeFilter)) return;

            var first = FamilyList.Items.Cast<object>().OfType<ProjectFamily>().FirstOrDefault();
            _loadingContext = true;
            try
            {
                FamilyList.SelectedItem = first;
                if (first != null)
                {
                    FamilyList.ScrollIntoView(first);
                    _viewModel.SetActiveFamily(first);
                    _viewModel.ShowFamilyProperties();
                    SetStatus("Nhóm mô hình: " + _familySubtypeFilter + " • " + first.Name);
                }
                else
                {
                    _viewModel.SelectedFamilyName = string.Empty;
                    _viewModel.Properties.Clear();
                    SetStatus("Nhóm mô hình: " + _familySubtypeFilter + " • chưa có Family; bấm Add để chọn Tham số hoặc Solid3D.");
                }
            }
            finally
            {
                _loadingContext = false;
            }
            RefreshSelectedFamilyHighlight();
        }

        private void OnFamilySubtypeSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_familySubtypeFilter))
                ApplyFamilySubtypeFilter();
        }

        private void OnFamilySubtypeFamilySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_applyingFamilySubtypeFilter) return;

            if (FamilyList.SelectedItem is ProjectFamily family && family.Category == ElementCategory.Foundation)
            {
                var inferred = InferFoundationSubtype(family.Name);
                if (_inspection.Count > 0 && inferred.Length > 0 &&
                    !string.Equals(_familySubtypeFilter, inferred, StringComparison.OrdinalIgnoreCase))
                {
                    _familySubtypeFilter = inferred;
                    _categoryFilter = ElementCategory.Foundation;
                    ApplyFamilySubtypeFilter();
                }
            }

            RefreshSelectedFamilyHighlight();
        }

        private void OnFamilyContainerGeneratorStatusChanged(object? sender, EventArgs e)
        {
            if (FamilyList.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                RefreshSelectedFamilyHighlight();
        }

        private void ApplyFamilySubtypeFilter()
        {
            if (_applyingFamilySubtypeFilter) return;
            _applyingFamilySubtypeFilter = true;
            try
            {
                if (string.IsNullOrWhiteSpace(_familySubtypeFilter))
                {
                    ApplyFamilyFilter();
                    return;
                }

                var text = FamilySearch?.Text?.Trim() ?? string.Empty;
                var view = CollectionViewSource.GetDefaultView(FamilyList?.ItemsSource);
                if (view == null) return;
                view.Filter = item =>
                {
                    if (!(item is ProjectFamily family)) return false;
                    if (_categoryFilter.HasValue && family.Category != _categoryFilter.Value) return false;
                    if (!FamilyMatchesWorkspaceSubtype(family, _familySubtypeFilter)) return false;
                    return text.Length == 0 ||
                           family.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                           family.Category.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
                };
                view.Refresh();
            }
            finally
            {
                _applyingFamilySubtypeFilter = false;
            }
        }

        private static string ResolveFoundationSubtype(TreeViewItem item)
        {
            if (!(item.Tag is string tag) ||
                !Enum.TryParse(tag, true, out ElementCategory category) ||
                category != ElementCategory.Foundation)
                return string.Empty;

            var header = (item.Header as string ?? string.Empty).Trim();
            return FoundationFamilySubtypes.FirstOrDefault(x =>
                       string.Equals(x, header, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        }

        private static string InferFoundationSubtype(string familyName)
        {
            return FoundationFamilySubtypes.FirstOrDefault(x => FamilyNameHasSubtype(familyName, x)) ?? string.Empty;
        }

        private static bool FamilyMatchesWorkspaceSubtype(ProjectFamily family, string subtype)
        {
            if (family == null) return false;
            if (string.IsNullOrWhiteSpace(subtype)) return true;
            return family.Category == ElementCategory.Foundation && FamilyNameHasSubtype(family.Name, subtype);
        }

        private static bool FamilyNameHasSubtype(string familyName, string subtype)
        {
            var name = (familyName ?? string.Empty).Trim();
            var prefix = (subtype ?? string.Empty).Trim();
            if (prefix.Length == 0) return true;
            if (string.Equals(name, prefix, StringComparison.OrdinalIgnoreCase)) return true;
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || name.Length <= prefix.Length) return false;
            var separator = name[prefix.Length];
            return separator == '-' || separator == '_' || char.IsWhiteSpace(separator);
        }

        private void RefreshSelectedFamilyHighlight()
        {
            if (_lastHighlightedFamilyItem != null)
            {
                _lastHighlightedFamilyItem.ClearValue(Control.BackgroundProperty);
                _lastHighlightedFamilyItem.ClearValue(Control.ForegroundProperty);
                _lastHighlightedFamilyItem.ClearValue(Control.FontWeightProperty);
                _lastHighlightedFamilyItem.ClearValue(UIElement.OpacityProperty);
                _lastHighlightedFamilyItem = null;
            }

            if (!(FamilyList.SelectedItem is ProjectFamily selected)) return;
            FamilyList.ScrollIntoView(selected);
            FamilyList.UpdateLayout();
            var container = FamilyList.ItemContainerGenerator.ContainerFromItem(selected) as ListBoxItem;
            if (container == null) return;

            var accent = TryFindResource("AccentBrush") as Brush ?? SystemColors.HighlightBrush;
            container.Background = accent;
            container.Foreground = Brushes.White;
            container.FontWeight = FontWeights.SemiBold;
            container.Opacity = 1.0;
            _lastHighlightedFamilyItem = container;
        }
    }
}
