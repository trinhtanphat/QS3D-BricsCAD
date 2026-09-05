using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Room-specific owner-reference contract for the BLT3D Workspace.
    ///
    /// The generic Family surface still uses the Tham số/Solid3D chooser. Room is deliberately
    /// different: + Add creates the next room immediately, while the existing XAML room/finish
    /// surface is restored as a third docked column instead of being retired by the compact shell.
    /// </summary>
    public partial class WorkspacePanel
    {
        private const double Blt3dRoomWorkspaceMinWidth = 710d;
        private static readonly bool Blt3dRoomWorkspacePaneRegistered = RegisterBlt3dRoomWorkspacePane();

        private bool _blt3dRoomWorkspaceHooksApplied;
        private bool _blt3dRoomWorkspaceLayoutQueued;
        private bool _blt3dRoomWorkspaceLayoutApplying;
        private bool _blt3dRoomWorkspacePaneStyled;

        private static bool RegisterBlt3dRoomWorkspacePane()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBlt3dRoomWorkspaceLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnBlt3dRoomWorkspaceUnloaded),
                true);
            return true;
        }

        private static void OnBlt3dRoomWorkspaceLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !Blt3dRoomWorkspacePaneRegistered) return;

            // Double-hop at the lowest dispatcher priority so the room contract wins after both
            // the compact/reference Loaded passes and the final BLT3D SystemIdle layout pass.
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.SystemIdle,
                new Action(() => panel.Dispatcher.BeginInvoke(
                    DispatcherPriority.SystemIdle,
                    new Action(panel.ApplyBlt3dRoomWorkspaceContract))));
        }

        private static void OnBlt3dRoomWorkspaceUnloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel)) return;
            panel.UnwireBlt3dRoomWorkspaceHooks();
        }

        private void ApplyBlt3dRoomWorkspaceContract()
        {
            if (!IsLoaded) return;
            EnsureBlt3dRoomWorkspaceHooks();
            RewireBlt3dRoomAwareAddActions();

            if (!IsBlt3dRoomWorkspace()) return;
            HideBlt3dFamilyModeChooser();
            ApplyBlt3dRoomPanePresentation();
            ApplyBlt3dRoomWorkspaceLayout();
        }

        private void EnsureBlt3dRoomWorkspaceHooks()
        {
            if (_blt3dRoomWorkspaceHooksApplied) return;
            ModelTree.SelectedItemChanged += OnBlt3dRoomWorkspaceTreeSelectionChanged;
            FamilyList.SelectionChanged += OnBlt3dRoomWorkspaceFamilySelectionChanged;
            WorkspaceOverflow.LayoutUpdated += OnBlt3dRoomWorkspaceLayoutUpdated;
            _blt3dRoomWorkspaceHooksApplied = true;
        }

        private void UnwireBlt3dRoomWorkspaceHooks()
        {
            if (!_blt3dRoomWorkspaceHooksApplied) return;
            try { ModelTree.SelectedItemChanged -= OnBlt3dRoomWorkspaceTreeSelectionChanged; } catch { }
            try { FamilyList.SelectionChanged -= OnBlt3dRoomWorkspaceFamilySelectionChanged; } catch { }
            try { WorkspaceOverflow.LayoutUpdated -= OnBlt3dRoomWorkspaceLayoutUpdated; } catch { }
            _blt3dRoomWorkspaceHooksApplied = false;
            _blt3dRoomWorkspaceLayoutQueued = false;
            _blt3dRoomWorkspaceLayoutApplying = false;
        }

        private bool IsBlt3dRoomWorkspace()
        {
            if (_categoryFilter == ElementCategory.Room) return true;
            if (!(ModelTree.SelectedItem is TreeViewItem item) || !(item.Tag is string tag)) return false;
            return Enum.TryParse(tag, true, out ElementCategory category) && category == ElementCategory.Room;
        }

        private void RewireBlt3dRoomAwareAddActions()
        {
            // Deliberately reuse the generic Family Add predicate. Do not match the room-pane
            // "+ Thêm" finish button: that control must remain wired to OnAddFinishClick.
            foreach (var button in RoomPaneDescendants<Button>(this).Where(IsBlt3dFamilyAddButton))
            {
                button.Click -= OnAddClick;
                button.Click -= OnFamilyAddModeClick;
                button.Click -= OnGridAwareFamilyAddModeClick;
                button.Click -= OnBlt3dFamilyAddClick;
                button.Click -= OnBlt3dRoomAwareAddClick;
                button.Click += OnBlt3dRoomAwareAddClick;
                button.ToolTip = IsBlt3dRoomWorkspace()
                    ? "Tạo Phòng mới trực tiếp"
                    : "Add Family — chọn Tham số hoặc Solid3D";
            }

            var menu = FamilyList.ContextMenu;
            if (menu == null) return;
            foreach (var item in menu.Items.OfType<MenuItem>().Where(item =>
                         string.Equals(item.Header as string, "Nhân bản Family", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.Header as string, "Thêm Family…", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.Header as string, "Thêm Phòng", StringComparison.OrdinalIgnoreCase)))
            {
                item.Click -= OnAddClick;
                item.Click -= OnFamilyAddModeClick;
                item.Click -= OnGridAwareFamilyAddModeClick;
                item.Click -= OnBlt3dFamilyAddClick;
                item.Click -= OnBlt3dRoomAwareAddClick;
                item.Click += OnBlt3dRoomAwareAddClick;
                item.Header = IsBlt3dRoomWorkspace() ? "Thêm Phòng" : "Thêm Family…";
            }
        }

        private void OnBlt3dRoomAwareAddClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (!IsBlt3dRoomWorkspace())
            {
                if (IsGridSubtype(_familySubtypeFilter))
                {
                    CreateGridFamilyFromWorkspaceSubtype(false);
                    return;
                }
                OnBlt3dFamilyAddClick(sender, e);
                return;
            }

            HideBlt3dFamilyModeChooser();
            CreateRoomFromWorkspace();
        }

        private void CreateRoomFromWorkspace()
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active.");

                var selected = FamilyList.SelectedItem as ProjectFamily;
                if (selected != null && selected.Category != ElementCategory.Room) selected = null;

                var project = selected == null
                    ? ProjectContextCoordinator.GetOrCreate(doc)
                    : ExistingProjectMutationContext.Require(doc, "Thêm Phòng từ Workspace");
                var basis = selected == null ? null : project.FindFamily(selected.Id);
                if (selected != null && basis == null)
                    throw new InvalidOperationException("Phòng đang chọn không còn tồn tại trong project hiện tại. Hãy Refresh Workspace.");

                var existingNames = new HashSet<string>(
                    project.Families.Where(x => x.Category == ElementCategory.Room).Select(x => x.Name),
                    StringComparer.OrdinalIgnoreCase);
                var name = NextRoomWorkspaceFamilyName(existingNames);

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
                            basis.Id + " -> " + family.Id + " • " + family.Name + " • Workspace Room direct Add");
                    }
                    else
                    {
                        family = ProjectFamilyService.Create(
                            project,
                            Guid.NewGuid().ToString("N"),
                            name,
                            ElementCategory.Room);
                        SeedQuickSchemaDefaults(family);
                        AuditTrail.ForProject(project).Record(
                            "family.create",
                            string.Empty,
                            family.Id + " • Room • " + family.Name + " • Workspace Room direct Add");
                    }

                    SeedRoomFamilyDefaults(family);
                    ProjectFamilyActivationService.SetActive(project, family.Id);
                    return family;
                }, "Tạo Phòng trực tiếp từ Workspace");

                RefreshAfterCommit(
                    () =>
                    {
                        RefreshProject();
                        _categoryFilter = ElementCategory.Room;
                        _familySubtypeFilter = string.Empty;
                        ApplyFamilySubtypeFilter();
                        var live = _viewModel.Families.FirstOrDefault(x =>
                            string.Equals(x.Id, created.Id, StringComparison.OrdinalIgnoreCase));
                        FamilyList.SelectedItem = live;
                        RefreshSelectedFamilyHighlight();
                        ApplyBlt3dRoomPanePresentation();
                        QueueBlt3dRoomWorkspaceLayoutRepair();
                    },
                    "Đã tạo Phòng “" + created.Name + "”.",
                    "Workspace Room direct Add");
            }
            catch (Exception ex)
            {
                SetStatus("Tạo Phòng lỗi: " + ex.Message);
            }
        }

        private static string NextRoomWorkspaceFamilyName(ISet<string> existingNames)
        {
            for (var index = 1; index < 10000; index++)
            {
                var candidate = "Phòng-" + index;
                if (!existingNames.Contains(candidate)) return candidate;
            }

            throw new InvalidOperationException("Không thể tạo tên Phòng duy nhất.");
        }

        private void OnBlt3dRoomWorkspaceTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            HideBlt3dFamilyModeChooser();
            RewireBlt3dRoomAwareAddActions();
            if (IsBlt3dRoomWorkspace())
            {
                ApplyBlt3dRoomPanePresentation();
                QueueBlt3dRoomWorkspaceLayoutRepair();
            }
            else
            {
                ApplyBlt3dFiveZoneRuntimeLayout();
            }
        }

        private void OnBlt3dRoomWorkspaceFamilySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsBlt3dRoomWorkspace()) return;
            ApplyBlt3dRoomPanePresentation();
        }

        private void OnBlt3dRoomWorkspaceLayoutUpdated(object? sender, EventArgs e)
        {
            if (_blt3dRoomWorkspaceLayoutApplying || !IsLoaded || !IsBlt3dRoomWorkspace()) return;
            if (NeedsBlt3dRoomWorkspaceLayoutRepair())
                QueueBlt3dRoomWorkspaceLayoutRepair();
        }

        private void QueueBlt3dRoomWorkspaceLayoutRepair()
        {
            if (_blt3dRoomWorkspaceLayoutQueued || _blt3dRoomWorkspaceLayoutApplying || !IsLoaded) return;
            _blt3dRoomWorkspaceLayoutQueued = true;
            Dispatcher.BeginInvoke(
                DispatcherPriority.SystemIdle,
                new Action(() =>
                {
                    _blt3dRoomWorkspaceLayoutQueued = false;
                    if (!IsLoaded || !IsBlt3dRoomWorkspace()) return;
                    ApplyBlt3dRoomWorkspaceLayout();
                }));
        }

        private bool NeedsBlt3dRoomWorkspaceLayoutRepair()
        {
            var workspace = FindBlt3dRoomWorkspaceGrid();
            if (workspace == null || workspace.ColumnDefinitions.Count != 5) return false;
            var roomPane = FindBlt3dRoomDetailPane(workspace);
            var splitter = workspace.Children
                .OfType<GridSplitter>()
                .FirstOrDefault(child => Grid.GetColumn(child) == 3);
            var columns = workspace.ColumnDefinitions;
            return roomPane == null ||
                   roomPane.Visibility != Visibility.Visible ||
                   splitter == null ||
                   splitter.Visibility != Visibility.Visible ||
                   columns[3].MaxWidth <= 0d ||
                   columns[4].MaxWidth <= 0d ||
                   columns[4].Width.Value <= 0d;
        }

        private void ApplyBlt3dRoomWorkspaceLayout()
        {
            if (_blt3dRoomWorkspaceLayoutApplying || !IsBlt3dRoomWorkspace()) return;
            var root = WorkspaceContentRoot;
            var workspace = FindBlt3dRoomWorkspaceGrid();
            if (root == null || workspace == null) return;

            var modelPane = workspace.Children
                .OfType<Border>()
                .FirstOrDefault(child => Grid.GetColumn(child) == 0);
            var familyPane = workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(child => IsVisualDescendant(child, FamilyList));
            var leftSplitter = workspace.Children
                .OfType<GridSplitter>()
                .FirstOrDefault(child => Grid.GetColumn(child) == 1);
            var rightSplitter = workspace.Children
                .OfType<GridSplitter>()
                .FirstOrDefault(child => Grid.GetColumn(child) == 3);
            var roomPane = FindBlt3dRoomDetailPane(workspace);
            if (modelPane == null || familyPane == null || leftSplitter == null || rightSplitter == null || roomPane == null)
                return;

            _blt3dRoomWorkspaceLayoutApplying = true;
            try
            {
                BindingOperations.ClearBinding(root, FrameworkElement.WidthProperty);
                root.Width = double.NaN;
                root.MinWidth = Blt3dRoomWorkspaceMinWidth;
                root.HorizontalAlignment = HorizontalAlignment.Stretch;

                WorkspaceOverflow.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                workspace.MinWidth = Blt3dRoomWorkspaceMinWidth;
                workspace.HorizontalAlignment = HorizontalAlignment.Stretch;

                var columns = workspace.ColumnDefinitions;
                columns[0].MinWidth = 160;
                columns[0].MaxWidth = double.PositiveInfinity;
                columns[0].Width = new GridLength(22, GridUnitType.Star);
                columns[1].MinWidth = 4;
                columns[1].MaxWidth = 4;
                columns[1].Width = new GridLength(4);
                columns[2].MinWidth = 260;
                columns[2].MaxWidth = double.PositiveInfinity;
                columns[2].Width = new GridLength(38, GridUnitType.Star);
                columns[3].MinWidth = 4;
                columns[3].MaxWidth = 4;
                columns[3].Width = new GridLength(4);
                columns[4].MinWidth = 260;
                columns[4].MaxWidth = double.PositiveInfinity;
                columns[4].Width = new GridLength(40, GridUnitType.Star);

                foreach (UIElement child in workspace.Children)
                {
                    child.Visibility = ReferenceEquals(child, modelPane) ||
                                       ReferenceEquals(child, familyPane) ||
                                       ReferenceEquals(child, leftSplitter) ||
                                       ReferenceEquals(child, rightSplitter) ||
                                       ReferenceEquals(child, roomPane)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }

                Grid.SetColumn(modelPane, 0);
                Grid.SetRow(modelPane, 0);
                Grid.SetColumn(leftSplitter, 1);
                Grid.SetRow(leftSplitter, 0);
                Grid.SetColumn(familyPane, 2);
                Grid.SetRow(familyPane, 0);
                Grid.SetColumn(rightSplitter, 3);
                Grid.SetRow(rightSplitter, 0);
                Grid.SetColumn(roomPane, 4);
                Grid.SetRow(roomPane, 0);

                rightSplitter.Width = 4;
                rightSplitter.Height = double.NaN;
                rightSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                rightSplitter.VerticalAlignment = VerticalAlignment.Stretch;
                rightSplitter.ResizeDirection = GridResizeDirection.Columns;
                rightSplitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                roomPane.Visibility = Visibility.Visible;
                roomPane.Opacity = 1d;
            }
            finally
            {
                _blt3dRoomWorkspaceLayoutApplying = false;
            }
        }

        private Grid? FindBlt3dRoomWorkspaceGrid()
        {
            var root = WorkspaceContentRoot;
            if (root == null) return null;
            return root.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate =>
                    Grid.GetRow(candidate) == 1 &&
                    candidate.ColumnDefinitions.Count == 5);
        }

        private static Grid? FindBlt3dRoomDetailPane(Grid workspace)
        {
            return workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(child => Grid.GetColumn(child) == 4);
        }

        private void ApplyBlt3dRoomPanePresentation()
        {
            var workspace = FindBlt3dRoomWorkspaceGrid();
            var roomPane = workspace == null ? null : FindBlt3dRoomDetailPane(workspace);
            if (roomPane == null) return;

            if (!_blt3dRoomWorkspacePaneStyled)
            {
                var title = FindRoomPaneText(roomPane, "HT_PHÒNG");
                if (title != null)
                {
                    BindingOperations.SetBinding(
                        title,
                        TextBlock.TextProperty,
                        new Binding("SelectedFamilyName") { Mode = BindingMode.OneWay, FallbackValue = "Phòng" });
                }

                var subtitle = FindRoomPaneText(roomPane, "Hoàn thiện theo phòng");
                if (subtitle != null) subtitle.Visibility = Visibility.Collapsed;

                if (SelectionCount != null)
                {
                    SelectionCount.Visibility = Visibility.Collapsed;
                    var badge = FindRoomPaneAncestor<Border>(SelectionCount);
                    if (badge != null) badge.Visibility = Visibility.Collapsed;
                }

                var remove = FindRoomPaneButton(roomPane, "Bỏ HT");
                if (remove != null) remove.Content = "Bỏ";

                var createFinish = FindRoomPaneButton(roomPane, "Chọn phòng");
                if (createFinish != null)
                {
                    createFinish.Click -= OnPickRoomClick;
                    createFinish.Click -= OnAddFinishClick;
                    createFinish.Click += OnAddFinishClick;
                    createFinish.Content = "Tạo hoàn thiện";
                    createFinish.ToolTip = "Tạo/cập nhật hoàn thiện cho Phòng đang chọn";
                }

                var finishTree = RoomPaneDescendants<TreeView>(roomPane).FirstOrDefault();
                if (finishTree != null && !finishTree.Items.OfType<TreeViewItem>().Any(item =>
                        string.Equals(item.Header as string, "Trát Trần", StringComparison.OrdinalIgnoreCase)))
                {
                    finishTree.Items.Add(new TreeViewItem
                    {
                        Header = "Trát Trần",
                        Tag = ElementCategory.CeilingFinish.ToString()
                    });
                }

                var inspectionTitle = FindRoomPaneText(roomPane, "ĐỐI TƯỢNG ĐANG CHỌN");
                if (inspectionTitle != null) inspectionTitle.Text = "Thuộc tính";

                var inspectionSubtitle = FindRoomPaneText(roomPane, "Handle • loại • layer • kích thước");
                if (inspectionSubtitle != null) inspectionSubtitle.Visibility = Visibility.Collapsed;

                var inspectionScope = FindRoomPaneText(roomPane, "CAD + SEMANTIC");
                if (inspectionScope != null)
                {
                    inspectionScope.Text = "Chưa chọn";
                    inspectionScope.FontWeight = FontWeights.Normal;
                    inspectionScope.Opacity = 0.7d;
                }

                var focus = FindRoomPaneButton(roomPane, "Focus");
                var toolbar = focus == null ? null : FindRoomPaneAncestor<Border>(focus);
                if (toolbar != null) toolbar.Visibility = Visibility.Collapsed;
                InspectionList.Visibility = Visibility.Collapsed;
                _blt3dRoomWorkspacePaneStyled = true;
            }
        }

        private static TextBlock? FindRoomPaneText(DependencyObject root, string text)
        {
            return RoomPaneDescendants<TextBlock>(root)
                .FirstOrDefault(candidate => string.Equals(candidate.Text, text, StringComparison.Ordinal));
        }

        private static Button? FindRoomPaneButton(DependencyObject root, string content)
        {
            return RoomPaneDescendants<Button>(root)
                .FirstOrDefault(candidate => string.Equals(candidate.Content as string, content, StringComparison.Ordinal));
        }

        private static T? FindRoomPaneAncestor<T>(DependencyObject? child) where T : DependencyObject
        {
            var current = child;
            while (current != null)
            {
                current = VisualTreeHelper.GetParent(current);
                if (current is T typed) return typed;
            }
            return null;
        }

        private static IEnumerable<T> RoomPaneDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < count; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T typed) yield return typed;
                foreach (var nested in RoomPaneDescendants<T>(child)) yield return nested;
            }
        }
    }
}
