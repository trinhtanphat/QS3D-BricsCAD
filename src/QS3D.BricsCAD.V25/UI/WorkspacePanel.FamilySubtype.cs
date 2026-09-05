using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI.ViewModels;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Workspace-only subtype routing. The persisted ProjectFamily model remains unchanged;
    /// Foundation subtypes are resolved from the tree leaf plus the established family-name prefix.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly string[] FoundationFamilySubtypes =
        {
            "Bê Tông Lót", "Móng Băng", "Móng Bè", "Dầm Móng", "Đài Cọc", "Cọc"
        };

        private const string RoomTopLevelKey = "TopLevel";
        private const string RoomBottomLevelKey = "BottomLevel";
        private const string RoomColorModeKey = "ColorMode";
        private const string RoomTransparencyKey = "TransparencyPercent";
        private const string RoomMarkKey = "Mark";
        private const string RoomCommentKey = "Comment";
        private const string RoomWbsKey = "WBS";
        private const string RoomMaterialKey = "Material";

        private static readonly string[] RoomLevelChoices = { "bottom_level", "top_level" };
        private static readonly string[] RoomColorModeChoices = { "Theo loại (mặc định)", "Tùy chỉnh" };
        private static readonly string[] RoomTransparencyChoices = { "0", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100" };
        private static readonly string[] RoomMaterialChoices = { "Khác" };

        private bool _familySubtypeInteractionsAttached;
        private bool _applyingFamilySubtypeFilter;
        private bool _familyHighlightRefreshPending;
        private string _familySubtypeFilter = string.Empty;

        private void AttachFamilySubtypeInteractions()
        {
            if (_familySubtypeInteractionsAttached) return;
            _familySubtypeInteractionsAttached = true;
            ModelTree.SelectedItemChanged += OnFamilySubtypeTreeSelectionChanged;
            FamilySearch.TextChanged += OnFamilySubtypeSearchChanged;
            FamilyList.SelectionChanged += OnFamilySubtypeFamilySelectionChanged;
            FamilyList.ItemContainerGenerator.StatusChanged += OnFamilyContainerGeneratorStatusChanged;
            FloorCombo.SelectionChanged += OnRoomFloorContextChanged;
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

        private bool IsWorkspaceAddFamilyButton(Button button)
        {
            if (!IsBlt3dFamilyAddButton(button)) return false;
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
            CreateFamilyFromWorkspaceSubtype(false);

        private void OnAddSolid3dFamilyClick(object sender, RoutedEventArgs e) =>
            CreateFamilyFromWorkspaceSubtype(true);

        private void CreateFamilyFromWorkspaceSubtype(bool launchSolid3D)
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active.");

                var subtype = _familySubtypeFilter;
                var selected = FamilyList.SelectedItem as ProjectFamily;
                if (selected != null && !FamilyMatchesWorkspaceSubtype(selected, subtype)) selected = null;

                var category = _categoryFilter ?? selected?.Category ?? ElementCategory.Room;
                if (!string.IsNullOrWhiteSpace(subtype)) category = ElementCategory.Foundation;
                if (launchSolid3D && !Cad.NativeBuildCapability.Supports(category))
                {
                    SetStatus(Cad.NativeBuildCapability.UnsupportedMessage(category));
                    return;
                }

                var project = selected == null
                    ? ProjectContextCoordinator.GetOrCreate(doc)
                    : ExistingProjectMutationContext.Require(doc, "Thêm Family từ Workspace");
                var basis = selected == null ? null : project.FindFamily(selected.Id);
                if (selected != null && basis == null)
                    throw new InvalidOperationException("Family đang chọn không còn tồn tại trong project hiện tại. Hãy Refresh Workspace.");

                var existingNames = new HashSet<string>(
                    project.Families.Where(x => x.Category == category).Select(x => x.Name),
                    StringComparer.OrdinalIgnoreCase);
                var name = !string.IsNullOrWhiteSpace(subtype)
                    ? NextSubtypeFamilyName(subtype, existingNames)
                    : NextWorkspaceFamilyName(basis?.Name ?? category.ToString(), existingNames);

                var created = ExecuteAtomic(project, () =>
                {
                    ProjectFamily family;
                    if (basis != null)
                    {
                        family = ProjectFamilyService.Duplicate(project, basis.Id, Guid.NewGuid().ToString("N"), name);
                        AuditTrail.ForProject(project).Record(
                            "family.duplicate", string.Empty,
                            basis.Id + " -> " + family.Id + " • " + family.Name + " • Workspace " +
                            (launchSolid3D ? "Solid3D" : "Tham số"));
                    }
                    else
                    {
                        family = ProjectFamilyService.Create(project, Guid.NewGuid().ToString("N"), name, category);
                        SeedQuickSchemaDefaults(family);
                        AuditTrail.ForProject(project).Record(
                            "family.create", string.Empty,
                            family.Id + " • " + family.Category + " • " + family.Name + " • Workspace " +
                            (launchSolid3D ? "Solid3D" : "Tham số"));
                    }
                    if (!string.IsNullOrWhiteSpace(subtype))
                        family.Properties[RaftFoundationPropertySet.WorkspaceSubtypeKey] = subtype;
                    SeedRoomFamilyDefaults(family);
                    if (RaftFoundationPropertySet.IsRaftFamily(family))
                        RaftFoundationLevelPlacement.EnsureDefaults(project, family);
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
                        if (live != null && RaftFoundationPropertySet.IsRaftFamily(live))
                            ApplyRaftFoundationPropertyForm(live);
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

        private static void SeedQuickSchemaDefaults(ProjectFamily family)
        {
            var schema = ProjectFamilyQuickSchemaService.GetSchema(family.Category);
            foreach (var pair in schema.DefaultsM)
                family.Properties[pair.Key] = pair.Value.ToString("0.########", CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(schema.DefaultMaterial))
                family.Properties["Material"] = schema.DefaultMaterial;
        }

        private static void SeedRoomFamilyDefaults(ProjectFamily family)
        {
            if (family == null || family.Category != ElementCategory.Room) return;
            SeedRoomDefault(family, RoomTopLevelKey, "bottom_level");
            SeedRoomDefault(family, RoomBottomLevelKey, "bottom_level");
            SeedRoomDefault(family, RoomColorModeKey, "Theo loại (mặc định)");
            SeedRoomDefault(family, RoomTransparencyKey, "70");
            SeedRoomDefault(family, RoomMarkKey, string.Empty);
            SeedRoomDefault(family, RoomCommentKey, string.Empty);
            SeedRoomDefault(family, RoomWbsKey, string.Empty);
            SeedRoomDefault(family, RoomMaterialKey, "Khác");
        }

        private static void SeedRoomDefault(ProjectFamily family, string key, string value)
        {
            if (!family.Properties.ContainsKey(key)) family.Properties[key] = value;
        }

        private void ApplyRoomFamilyPropertyForm(ProjectFamily family)
        {
            if (family == null || family.Category != ElementCategory.Room ||
                !string.Equals(_viewModel.SelectedPropertyScope, WorkspaceViewModel.FamilyScope, StringComparison.Ordinal)) return;

            var familyNameRow = _viewModel.Properties.FirstOrDefault(x =>
                string.Equals(x.Name, "Tên Family", StringComparison.CurrentCultureIgnoreCase));
            _viewModel.Properties.Clear();
            if (familyNameRow != null)
            {
                familyNameRow.Group = "Information";
                _viewModel.Properties.Add(familyNameRow);
            }
            else
            {
                var fallbackName = new PropertyRowViewModel { Group = "Information", Name = "Tên Family", IsReadOnly = true };
                fallbackName.Value = family.Name;
                _viewModel.Properties.Add(fallbackName);
            }
            var categoryRow = new PropertyRowViewModel { Group = "Information", Name = "Loại cấu kiện", IsReadOnly = true };
            categoryRow.Value = "Phòng";
            _viewModel.Properties.Add(categoryRow);
            var floorRow = new PropertyRowViewModel { Group = "Information", Name = "Tầng", IsReadOnly = true };
            floorRow.Value = FloorCombo?.SelectedItem as string ?? string.Empty;
            _viewModel.Properties.Add(floorRow);

            AddRoomFamilyPropertyRow(family, "Cao độ", "Cao độ đầu", RoomTopLevelKey, "bottom_level", RoomLevelChoices);
            AddRoomFamilyPropertyRow(family, "Cao độ", "Cao độ đáy", RoomBottomLevelKey, "bottom_level", RoomLevelChoices);
            AddRoomFamilyPropertyRow(family, "Display", "Màu sắc", RoomColorModeKey, "Theo loại (mặc định)", RoomColorModeChoices);
            AddRoomFamilyPropertyRow(family, "Display", "Độ trong suốt", RoomTransparencyKey, "70", RoomTransparencyChoices, "%");
            AddRoomFamilyPropertyRow(family, "Metadata", "Mark", RoomMarkKey, string.Empty, Array.Empty<string>());
            AddRoomFamilyPropertyRow(family, "Metadata", "Comment", RoomCommentKey, string.Empty, Array.Empty<string>());
            AddRoomFamilyPropertyRow(family, "Metadata", "WBS", RoomWbsKey, string.Empty, Array.Empty<string>());
            AddRoomFamilyPropertyRow(family, "Metadata", "Vật liệu", RoomMaterialKey, "Khác", RoomMaterialChoices);
        }

        private void AddRoomFamilyPropertyRow(
            ProjectFamily family, string group, string label, string key, string fallback,
            IReadOnlyList<string> choices, string unit = "")
        {
            var current = family.Properties.TryGetValue(key, out var stored) ? (stored ?? string.Empty).Trim() : fallback;
            var rowChoices = choices.Concat(new[] { current }).Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.CurrentCultureIgnoreCase).ToArray();
            var row = new PropertyRowViewModel
            {
                Group = group,
                Name = label,
                Unit = unit,
                EditorKind = rowChoices.Length > 0 ? PropertyRowViewModel.ChoiceEditor : PropertyRowViewModel.TextEditor,
                Choices = rowChoices
            };
            row.Value = current;
            row.Apply = value => ApplyRoomFamilyProperty(family, key, value);
            _viewModel.Properties.Add(row);
        }

        private string ApplyRoomFamilyProperty(ProjectFamily family, string key, string value)
        {
            var previous = family.Properties.TryGetValue(key, out var stored)
                ? (stored ?? string.Empty).Trim()
                : RoomDefaultValue(key);
            var next = (value ?? string.Empty).Trim();
            if (string.Equals(key, RoomTransparencyKey, StringComparison.Ordinal))
            {
                if ((!double.TryParse(next, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) &&
                     !double.TryParse(next, NumberStyles.Float, CultureInfo.CurrentCulture, out percent)) ||
                    double.IsNaN(percent) || double.IsInfinity(percent) || percent < 0d || percent > 100d)
                {
                    SetStatus("Độ trong suốt: chỉ nhận giá trị từ 0% đến 100%.");
                    return previous;
                }
                next = percent.ToString("0.##", CultureInfo.InvariantCulture);
            }
            else if ((string.Equals(key, RoomTopLevelKey, StringComparison.Ordinal) ||
                      string.Equals(key, RoomBottomLevelKey, StringComparison.Ordinal) ||
                      string.Equals(key, RoomColorModeKey, StringComparison.Ordinal) ||
                      string.Equals(key, RoomMaterialKey, StringComparison.Ordinal)) && next.Length == 0)
            {
                SetStatus(RoomPropertyLabel(key) + ": không được để trống.");
                return previous;
            }
            if (string.Equals(previous, next, StringComparison.Ordinal)) return previous;
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active.");
                var project = ExistingProjectMutationContext.Require(doc, "Cập nhật thuộc tính Family Phòng");
                var owned = project.FindFamily(family.Id);
                if (owned == null || !ReferenceEquals(owned, family))
                    throw new InvalidOperationException("Family Phòng đang chọn đã stale hoặc không thuộc project hiện tại.");
                var result = ExecuteAtomic(project,
                    () => ProjectFamilyService.SetProperty(project, owned.Id, key, next),
                    "Cập nhật thuộc tính Family Phòng");
                var live = owned.Properties.TryGetValue(key, out var saved) ? saved ?? next : next;
                SetStatus("Đã cập nhật " + RoomPropertyLabel(key) + " • kế thừa " + result.InheritedInstancesUpdated + " cấu kiện" +
                          (result.OverridesPreserved > 0 ? " • giữ " + result.OverridesPreserved + " instance override" : string.Empty));
                return live;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is OverflowException)
            {
                SetStatus("Không thể cập nhật " + RoomPropertyLabel(key) + ": " + ex.Message);
                return previous;
            }
        }

        private static string RoomDefaultValue(string key)
        {
            switch (key)
            {
                case RoomTopLevelKey:
                case RoomBottomLevelKey: return "bottom_level";
                case RoomColorModeKey: return "Theo loại (mặc định)";
                case RoomTransparencyKey: return "70";
                case RoomMaterialKey: return "Khác";
                default: return string.Empty;
            }
        }

        private static string RoomPropertyLabel(string key)
        {
            switch (key)
            {
                case RoomTopLevelKey: return "Cao độ đầu";
                case RoomBottomLevelKey: return "Cao độ đáy";
                case RoomColorModeKey: return "Màu sắc";
                case RoomTransparencyKey: return "Độ trong suốt";
                case RoomMarkKey: return "Mark";
                case RoomCommentKey: return "Comment";
                case RoomWbsKey: return "WBS";
                case RoomMaterialKey: return "Vật liệu";
                default: return key;
            }
        }

        private void OnRoomFloorContextChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingContext) return;
            if (FamilyList.SelectedItem is ProjectFamily family)
            {
                if (RaftFoundationPropertySet.IsRaftFamily(family)) ApplyRaftFoundationPropertyForm(family);
                else if (family.Category == ElementCategory.Room) ApplyRoomFamilyPropertyForm(family);
            }
        }

        private static string NextSubtypeFamilyName(string subtype, ISet<string> existingNames)
        {
            var baseName = string.IsNullOrWhiteSpace(subtype) ? "Family" : subtype.Trim();
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
                    _viewModel.SetActiveFamily(first);
                    _viewModel.ShowFamilyProperties();
                    if (RaftFoundationPropertySet.IsRaftFamily(first)) ApplyRaftFoundationPropertyForm(first);
                    SetStatus("Nhóm mô hình: " + _familySubtypeFilter + " • " + first.Name);
                }
                else
                {
                    _viewModel.SelectedFamilyName = string.Empty;
                    _viewModel.Properties.Clear();
                    SetStatus("Nhóm mô hình: " + _familySubtypeFilter + " • chưa có Family; bấm Add để chọn Tham số hoặc Solid3D.");
                }
            }
            finally { _loadingContext = false; }
            RefreshSelectedFamilyHighlight();
        }

        private void OnFamilySubtypeSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_familySubtypeFilter)) ApplyFamilySubtypeFilter();
        }

        private void OnFamilySubtypeFamilySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_applyingFamilySubtypeFilter) return;
            var family = FamilyList.SelectedItem as ProjectFamily;
            if (family != null && family.Category == ElementCategory.Foundation)
            {
                var inferred = InferFoundationSubtype(family.Name);
                if (_loadingContext && _inspection.Count > 0 && inferred.Length > 0 &&
                    !string.Equals(_familySubtypeFilter, inferred, StringComparison.OrdinalIgnoreCase))
                {
                    _familySubtypeFilter = inferred;
                    _categoryFilter = ElementCategory.Foundation;
                    ApplyFamilySubtypeFilter();
                }
            }
            if (family != null && RaftFoundationPropertySet.IsRaftFamily(family))
                ApplyRaftFoundationPropertyForm(family);
            else if (family != null && family.Category == ElementCategory.Room)
                ApplyRoomFamilyPropertyForm(family);
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
                view.Filter = item => item is ProjectFamily family &&
                    (!_categoryFilter.HasValue || family.Category == _categoryFilter.Value) &&
                    FamilyMatchesWorkspaceSubtype(family, _familySubtypeFilter) &&
                    (text.Length == 0 ||
                     family.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                     family.Category.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
                view.Refresh();
            }
            finally { _applyingFamilySubtypeFilter = false; }
        }

        private static string ResolveFoundationSubtype(TreeViewItem item)
        {
            if (!(item.Tag is string tag) || !Enum.TryParse(tag, true, out ElementCategory category) ||
                category != ElementCategory.Foundation) return string.Empty;
            var header = (item.Header as string ?? string.Empty).Trim();
            return FoundationFamilySubtypes.FirstOrDefault(x =>
                string.Equals(x, header, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        }

        private static string InferFoundationSubtype(string familyName) =>
            FoundationFamilySubtypes.FirstOrDefault(x => FamilyNameHasSubtype(familyName, x)) ?? string.Empty;

        private static bool FamilyMatchesWorkspaceSubtype(ProjectFamily family, string subtype) =>
            string.IsNullOrWhiteSpace(subtype) ||
            (family.Category == ElementCategory.Foundation &&
             (string.Equals(RaftFoundationPropertySet.SubtypeName, subtype, StringComparison.OrdinalIgnoreCase)
                 ? RaftFoundationPropertySet.IsRaftFamily(family)
                 : FamilyNameHasSubtype(family.Name, subtype)));

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
            if (_familyHighlightRefreshPending) return;
            _familyHighlightRefreshPending = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _familyHighlightRefreshPending = false;
                RevealSelectedFamilyAndRefreshHighlight();
            }));
        }

        private void RevealSelectedFamilyAndRefreshHighlight()
        {
            if (!(FamilyList.SelectedItem is ProjectFamily selected)) return;
            if (FamilyList.ItemContainerGenerator.Status == GeneratorStatus.GeneratingContainers) return;
            FamilyList.ScrollIntoView(selected);
        }
    }
}
