using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Navigation;
using QS3D.Core.Persistence;
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Production hosted adapter for the Core Project Browser. The adapter intentionally keeps
    /// only semantic/presentation identity in modeless state. Native ObjectId/Handle values are
    /// resolved from the current canonical project and active DWG at action time.
    /// </summary>
    public partial class WorkspacePanel
    {
        private const int HostedBrowserViewportPageSize = 100;
        private const int HostedBrowserElementPageSize = 100;

        private static readonly bool HostedBrowserClassHandlersRegistered = RegisterHostedBrowserClassHandlers();

        private readonly ProjectBrowserWorkspaceStateStore _hostedBrowserStateStore = new ProjectBrowserWorkspaceStateStore();
        private ProjectBrowserWorkspaceState _hostedBrowserState = new ProjectBrowserWorkspaceState();
        private string _hostedBrowserProjectId = string.Empty;
        private string _hostedBrowserDrawingFingerprint = string.Empty;
        private string _hostedBrowserSelectedNodePath = string.Empty;
        private int _hostedBrowserViewportOffset;
        private int _hostedBrowserElementOffset;
        private bool _hostedBrowserAttached;
        private bool _hostedBrowserUiUpdating;
        private bool _hostedBrowserRefreshQueued;
        private bool _hostedBrowserForceRebindQueued;
        private DependencyPropertyDescriptor? _hostedBrowserInspectionItemsSourceDescriptor;

        private TabControl? _hostedBrowserTabs;
        private TabItem? _hostedBrowserTab;
        private TextBox? _hostedBrowserQuery;
        private ComboBox? _hostedBrowserGrouping;
        private CheckBox? _hostedBrowserDirtyOnly;
        private ListBox? _hostedBrowserNodes;
        private ListBox? _hostedBrowserElements;
        private TextBlock? _hostedBrowserStatus;
        private TextBlock? _hostedBrowserNodePageStatus;
        private TextBlock? _hostedBrowserElementPageStatus;
        private Button? _hostedBrowserPreviousNodes;
        private Button? _hostedBrowserNextNodes;
        private Button? _hostedBrowserPreviousElements;
        private Button? _hostedBrowserNextElements;
        private Button? _hostedBrowserSelectCad;
        private Button? _hostedBrowserZoomCad;
        private Button? _hostedBrowserReset;

        private static bool RegisterHostedBrowserClassHandlers()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnHostedBrowserWorkspaceLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnHostedBrowserWorkspaceUnloaded),
                true);
            return true;
        }

        private static void OnHostedBrowserWorkspaceLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel) panel.AttachHostedProjectBrowser();
        }

        private static void OnHostedBrowserWorkspaceUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel) panel.DetachHostedProjectBrowser();
        }

        private void AttachHostedProjectBrowser()
        {
            EnsureHostedProjectBrowserSurface();
            if (_hostedBrowserAttached) return;

            _hostedBrowserAttached = true;
            DataContextChanged += OnHostedBrowserDataContextChanged;
            _hostedBrowserInspectionItemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(
                ItemsControl.ItemsSourceProperty,
                typeof(ListView));
            _hostedBrowserInspectionItemsSourceDescriptor?.AddValueChanged(
                InspectionList,
                OnHostedBrowserInspectionItemsSourceChanged);
            QueueHostedBrowserRefresh(true);
        }

        private void DetachHostedProjectBrowser()
        {
            if (!_hostedBrowserAttached) return;
            _hostedBrowserAttached = false;
            DataContextChanged -= OnHostedBrowserDataContextChanged;
            _hostedBrowserInspectionItemsSourceDescriptor?.RemoveValueChanged(
                InspectionList,
                OnHostedBrowserInspectionItemsSourceChanged);
            _hostedBrowserInspectionItemsSourceDescriptor = null;
            _hostedBrowserProjectId = string.Empty;
            _hostedBrowserDrawingFingerprint = string.Empty;
            _hostedBrowserRefreshQueued = false;
            _hostedBrowserForceRebindQueued = false;
        }

        private void OnHostedBrowserDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            QueueHostedBrowserRefresh(true);
        }

        private void OnHostedBrowserInspectionItemsSourceChanged(object? sender, EventArgs e)
        {
            if (!_hostedBrowserAttached || _hostedBrowserUiUpdating) return;
            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(SyncHostedBrowserFromCadInspection));
        }

        private void QueueHostedBrowserRefresh(bool forceRebind)
        {
            if (!_hostedBrowserAttached && !IsLoaded) return;
            _hostedBrowserForceRebindQueued |= forceRebind;
            if (_hostedBrowserRefreshQueued) return;
            _hostedBrowserRefreshQueued = true;
            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    _hostedBrowserRefreshQueued = false;
                    var rebind = _hostedBrowserForceRebindQueued;
                    _hostedBrowserForceRebindQueued = false;
                    RefreshHostedProjectBrowser(rebind);
                }));
        }

        private void EnsureHostedProjectBrowserSurface()
        {
            if (_hostedBrowserTabs != null) return;
            if (!(ModelTree.Parent is DockPanel modelDock)) return;

            modelDock.Children.Remove(ModelTree);

            var tabs = new TabControl
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Background = TryFindResource("Bg1Brush") as Brush,
                BorderBrush = TryFindResource("BorderBrush") as Brush,
                BorderThickness = new Thickness(0)
            };
            var modelTab = new TabItem { Header = "Mô hình", Content = ModelTree };
            var browserTab = new TabItem { Header = "Project Browser", Content = CreateHostedProjectBrowserSurface() };
            tabs.Items.Add(modelTab);
            tabs.Items.Add(browserTab);
            tabs.SelectionChanged += OnHostedBrowserTabSelectionChanged;
            modelDock.Children.Add(tabs);

            _hostedBrowserTabs = tabs;
            _hostedBrowserTab = browserTab;
        }

        private FrameworkElement CreateHostedProjectBrowserSurface()
        {
            var root = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(104) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var controls = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
            controls.Children.Add(new TextBlock
            {
                Text = "Tìm semantic",
                FontSize = 9,
                Foreground = TryFindResource("TextMutedBrush") as Brush
            });
            var query = new TextBox { MinHeight = 24, ToolTip = "Tìm theo ID, Family, Category, tầng hoặc Zone; Enter để áp dụng." };
            query.KeyDown += OnHostedBrowserQueryKeyDown;
            controls.Children.Add(query);

            var grouping = new ComboBox
            {
                MinHeight = 24,
                Margin = new Thickness(0, 3, 0, 0),
                ItemsSource = new[]
                {
                    new HostedBrowserGroupingOption(ProjectBrowserGrouping.FloorThenCategory, "Tầng > Category"),
                    new HostedBrowserGroupingOption(ProjectBrowserGrouping.ZoneThenCategory, "Zone > Category"),
                    new HostedBrowserGroupingOption(ProjectBrowserGrouping.Category, "Category")
                },
                DisplayMemberPath = nameof(HostedBrowserGroupingOption.DisplayName),
                SelectedValuePath = nameof(HostedBrowserGroupingOption.Value)
            };
            grouping.SelectionChanged += OnHostedBrowserViewChanged;
            controls.Children.Add(grouping);

            var dirtyOnly = new CheckBox
            {
                Content = "Chỉ cấu kiện dirty",
                Margin = new Thickness(0, 3, 0, 0),
                FontSize = 9
            };
            dirtyOnly.Click += OnHostedBrowserViewChanged;
            controls.Children.Add(dirtyOnly);

            var toolbar = new WrapPanel { Margin = new Thickness(0, 3, 0, 0) };
            var refresh = new Button { Content = "Làm mới", MinHeight = 23, Margin = new Thickness(0, 0, 3, 0) };
            refresh.Click += OnHostedBrowserRefreshClick;
            var reset = new Button { Content = "Reset", MinHeight = 23, ToolTip = "Xóa presentation state Project Browser hiện hành." };
            reset.Click += OnHostedBrowserResetClick;
            toolbar.Children.Add(refresh);
            toolbar.Children.Add(reset);
            controls.Children.Add(toolbar);
            Grid.SetRow(controls, 0);
            root.Children.Add(controls);

            var status = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 9,
                Margin = new Thickness(0, 0, 0, 4),
                Foreground = TryFindResource("TextMutedBrush") as Brush
            };
            Grid.SetRow(status, 1);
            root.Children.Add(status);

            var nodes = new ListBox
            {
                SelectionMode = SelectionMode.Single,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                ToolTip = "Double-click node để mở/đóng; chọn node để xem semantic IDs theo page."
            };
            nodes.SelectionChanged += OnHostedBrowserNodeSelectionChanged;
            nodes.MouseDoubleClick += OnHostedBrowserNodeDoubleClick;
            Grid.SetRow(nodes, 2);
            root.Children.Add(nodes);

            var elements = new ListBox
            {
                SelectionMode = SelectionMode.Extended,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 0),
                ToolTip = "Semantic IDs; double-click để chọn + zoom CAD."
            };
            elements.MouseDoubleClick += OnHostedBrowserElementDoubleClick;
            Grid.SetRow(elements, 3);
            root.Children.Add(elements);

            var footer = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            var nodePager = new DockPanel();
            var prevNodes = new Button { Content = "‹", Width = 27, MinHeight = 22 };
            prevNodes.Click += OnHostedBrowserPreviousNodesClick;
            var nextNodes = new Button { Content = "›", Width = 27, MinHeight = 22 };
            nextNodes.Click += OnHostedBrowserNextNodesClick;
            var nodePage = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 5, 0), FontSize = 9 };
            DockPanel.SetDock(prevNodes, Dock.Left);
            DockPanel.SetDock(nextNodes, Dock.Right);
            nodePager.Children.Add(prevNodes);
            nodePager.Children.Add(nextNodes);
            nodePager.Children.Add(nodePage);
            footer.Children.Add(nodePager);

            var elementPager = new DockPanel { Margin = new Thickness(0, 2, 0, 0) };
            var prevElements = new Button { Content = "‹ id", Width = 38, MinHeight = 22 };
            prevElements.Click += OnHostedBrowserPreviousElementsClick;
            var nextElements = new Button { Content = "id ›", Width = 38, MinHeight = 22 };
            nextElements.Click += OnHostedBrowserNextElementsClick;
            var elementPage = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 5, 0), FontSize = 9 };
            DockPanel.SetDock(prevElements, Dock.Left);
            DockPanel.SetDock(nextElements, Dock.Right);
            elementPager.Children.Add(prevElements);
            elementPager.Children.Add(nextElements);
            elementPager.Children.Add(elementPage);
            footer.Children.Add(elementPager);

            var actions = new WrapPanel { Margin = new Thickness(0, 3, 0, 0) };
            var selectCad = new Button { Content = "Chọn CAD", MinHeight = 23, Margin = new Thickness(0, 0, 3, 0) };
            selectCad.Click += OnHostedBrowserSelectCadClick;
            var zoomCad = new Button { Content = "Zoom", MinHeight = 23 };
            zoomCad.Click += OnHostedBrowserZoomCadClick;
            actions.Children.Add(selectCad);
            actions.Children.Add(zoomCad);
            footer.Children.Add(actions);
            Grid.SetRow(footer, 4);
            root.Children.Add(footer);

            _hostedBrowserQuery = query;
            _hostedBrowserGrouping = grouping;
            _hostedBrowserDirtyOnly = dirtyOnly;
            _hostedBrowserNodes = nodes;
            _hostedBrowserElements = elements;
            _hostedBrowserStatus = status;
            _hostedBrowserNodePageStatus = nodePage;
            _hostedBrowserElementPageStatus = elementPage;
            _hostedBrowserPreviousNodes = prevNodes;
            _hostedBrowserNextNodes = nextNodes;
            _hostedBrowserPreviousElements = prevElements;
            _hostedBrowserNextElements = nextElements;
            _hostedBrowserSelectCad = selectCad;
            _hostedBrowserZoomCad = zoomCad;
            _hostedBrowserReset = reset;
            return root;
        }

        private void OnHostedBrowserTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReferenceEquals(_hostedBrowserTabs?.SelectedItem, _hostedBrowserTab)) QueueHostedBrowserRefresh(true);
        }

        private void OnHostedBrowserRefreshClick(object sender, RoutedEventArgs e)
        {
            RefreshHostedProjectBrowser(true);
            SyncHostedBrowserFromCadInspection();
        }

        private void OnHostedBrowserResetClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetHostedBrowserCurrentProject(true, out var document, out var readOnlyProject, out var error))
                    throw new InvalidOperationException(error);
                var project = ExistingProjectMutationContext.Require(document, "Reset Project Browser presentation state");
                RequireHostedBrowserIdentity(project, readOnlyProject.ProjectId, readOnlyProject.DrawingFingerprint);
                var version = project.ChangeVersion;
                _hostedBrowserStateStore.Clear(project);
                RequireHostedBrowserVersionInvariant(project, version);
                _hostedBrowserState = new ProjectBrowserWorkspaceState();
                _hostedBrowserViewportOffset = 0;
                _hostedBrowserElementOffset = 0;
                _hostedBrowserSelectedNodePath = string.Empty;
                RefreshHostedProjectBrowser(true);
            }
            catch (Exception ex)
            {
                SetHostedBrowserStatus("Reset Project Browser bị từ chối: " + ex.Message);
            }
        }

        private void OnHostedBrowserQueryKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            ApplyHostedBrowserViewFromControls();
            e.Handled = true;
        }

        private void OnHostedBrowserViewChanged(object sender, RoutedEventArgs e)
        {
            if (_hostedBrowserUiUpdating) return;
            ApplyHostedBrowserViewFromControls();
        }

        private void ApplyHostedBrowserViewFromControls()
        {
            if (_hostedBrowserGrouping == null || _hostedBrowserQuery == null || _hostedBrowserDirtyOnly == null) return;
            if (_hostedBrowserUiUpdating) return;

            try
            {
                if (!TryGetHostedBrowserCurrentProject(false, out var document, out var project, out var error))
                    throw new InvalidOperationException(error);
                var grouping = _hostedBrowserGrouping.SelectedValue is ProjectBrowserGrouping value
                    ? value
                    : ProjectBrowserGrouping.FloorThenCategory;
                var state = new ProjectBrowserWorkspaceState(
                    grouping,
                    _hostedBrowserQuery.Text,
                    _hostedBrowserDirtyOnly.IsChecked == true,
                    _hostedBrowserState.Categories,
                    _hostedBrowserState.FloorIds,
                    _hostedBrowserState.ZoneIds,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    string.Empty);
                PersistHostedBrowserState(document, project, state);
                _hostedBrowserState = state;
                _hostedBrowserViewportOffset = 0;
                _hostedBrowserElementOffset = 0;
                _hostedBrowserSelectedNodePath = string.Empty;
                RefreshHostedProjectBrowser(false);
            }
            catch (Exception ex)
            {
                SetHostedBrowserStatus("Project Browser view bị từ chối: " + ex.Message);
            }
        }

        private void RefreshHostedProjectBrowser(bool forceRebind)
        {
            if (_hostedBrowserNodes == null || _hostedBrowserElements == null) return;
            try
            {
                if (!TryGetHostedBrowserCurrentProject(forceRebind, out _, out var project, out var error))
                {
                    ClearHostedProjectBrowser(error);
                    return;
                }

                if (forceRebind ||
                    !string.Equals(_hostedBrowserProjectId, project.ProjectId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(_hostedBrowserDrawingFingerprint, project.DrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    _hostedBrowserState = _hostedBrowserStateStore.Load(project);
                    _hostedBrowserProjectId = project.ProjectId;
                    _hostedBrowserDrawingFingerprint = project.DrawingFingerprint;
                    _hostedBrowserViewportOffset = 0;
                    _hostedBrowserElementOffset = 0;
                    _hostedBrowserSelectedNodePath = string.Empty;
                }

                var version = project.ChangeVersion;
                var plan = ProjectBrowserWorkspaceCoordinator.Build(
                    project,
                    _hostedBrowserState,
                    _hostedBrowserViewportOffset,
                    HostedBrowserViewportPageSize);
                RequireHostedBrowserVersionInvariant(project, version);
                RenderHostedBrowser(project, plan);
            }
            catch (Exception ex)
            {
                ClearHostedProjectBrowser("Project Browser fail-closed: " + ex.Message);
            }
        }

        private void RenderHostedBrowser(ProjectState project, ProjectBrowserWorkspacePlan plan)
        {
            if (_hostedBrowserNodes == null || _hostedBrowserElements == null || _hostedBrowserGrouping == null ||
                _hostedBrowserQuery == null || _hostedBrowserDirtyOnly == null) return;

            _hostedBrowserUiUpdating = true;
            try
            {
                _hostedBrowserGrouping.SelectedValue = _hostedBrowserState.Grouping;
                if (!_hostedBrowserQuery.IsKeyboardFocusWithin) _hostedBrowserQuery.Text = _hostedBrowserState.Query;
                _hostedBrowserDirtyOnly.IsChecked = _hostedBrowserState.DirtyOnly;

                var rows = plan.Viewport.Rows
                    .Select(row => new HostedBrowserNodeRow(row))
                    .ToList();
                _hostedBrowserNodes.ItemsSource = rows;

                var targetPath = _hostedBrowserSelectedNodePath;
                if (targetPath.Length == 0 && plan.Reveal.TargetNodePaths.Count > 0)
                    targetPath = plan.Reveal.TargetNodePaths[0];
                var selectedNode = rows.FirstOrDefault(row => string.Equals(row.Path, targetPath, StringComparison.Ordinal));
                if (selectedNode == null && rows.Count > 0 && targetPath.Length == 0) selectedNode = rows[0];
                _hostedBrowserNodes.SelectedItem = selectedNode;
                if (selectedNode != null) _hostedBrowserNodes.ScrollIntoView(selectedNode);
                _hostedBrowserSelectedNodePath = selectedNode?.Path ?? targetPath;

                RenderHostedBrowserElements(project, plan.Query.Root);
                UpdateHostedBrowserPaging(plan.Viewport);
                SetHostedBrowserEnabled(true);
                SetHostedBrowserStatus(
                    plan.Viewport.TotalVisibleRows + " node hiển thị • " + plan.Query.MatchedElementCount +
                    " semantic match • selection " + plan.Reveal.SelectedElementIds.Count + ".");
            }
            finally
            {
                _hostedBrowserUiUpdating = false;
            }
        }

        private void RenderHostedBrowserElements(ProjectState project, ProjectBrowserNode root)
        {
            if (_hostedBrowserElements == null) return;
            if (_hostedBrowserSelectedNodePath.Length == 0)
            {
                _hostedBrowserElements.ItemsSource = Array.Empty<HostedBrowserElementRow>();
                UpdateHostedBrowserElementPaging(null);
                return;
            }

            var page = ProjectBrowserVirtualizationPlanner.GetElementPage(
                root,
                _hostedBrowserSelectedNodePath,
                _hostedBrowserElementOffset,
                HostedBrowserElementPageSize);
            var rows = page.ElementIds.Select(id => HostedBrowserElementRow.Create(project, id)).ToList();
            _hostedBrowserElements.ItemsSource = rows;

            var selectedIds = new HashSet<string>(_hostedBrowserState.SelectedElementIds, StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.Where(row => selectedIds.Contains(row.ElementId)))
                _hostedBrowserElements.SelectedItems.Add(row);
            if (_hostedBrowserElements.SelectedItems.Count > 0)
                _hostedBrowserElements.ScrollIntoView(_hostedBrowserElements.SelectedItems[0]);
            UpdateHostedBrowserElementPaging(page);
        }

        private void OnHostedBrowserNodeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_hostedBrowserUiUpdating || !(_hostedBrowserNodes?.SelectedItem is HostedBrowserNodeRow row)) return;
            _hostedBrowserSelectedNodePath = row.Path;
            _hostedBrowserElementOffset = 0;
            try
            {
                if (!TryGetHostedBrowserCurrentProject(false, out _, out var project, out var error))
                    throw new InvalidOperationException(error);
                var plan = ProjectBrowserWorkspaceCoordinator.Build(project, _hostedBrowserState, _hostedBrowserViewportOffset, HostedBrowserViewportPageSize);
                _hostedBrowserUiUpdating = true;
                try { RenderHostedBrowserElements(project, plan.Query.Root); }
                finally { _hostedBrowserUiUpdating = false; }
            }
            catch (Exception ex)
            {
                SetHostedBrowserStatus("Project Browser node bị từ chối: " + ex.Message);
            }
        }

        private void OnHostedBrowserNodeDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(_hostedBrowserNodes?.SelectedItem is HostedBrowserNodeRow row) || !row.HasChildren) return;
            try
            {
                if (!TryGetHostedBrowserCurrentProject(false, out var document, out var project, out var error))
                    throw new InvalidOperationException(error);
                var state = ProjectBrowserWorkspaceCoordinator.SetExpanded(project, _hostedBrowserState, row.Path, !row.IsExpanded);
                PersistHostedBrowserState(document, project, state);
                _hostedBrowserState = state;
                _hostedBrowserViewportOffset = 0;
                RefreshHostedProjectBrowser(false);
            }
            catch (Exception ex)
            {
                SetHostedBrowserStatus("Project Browser expand/collapse bị từ chối: " + ex.Message);
            }
        }

        private void OnHostedBrowserElementDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectHostedBrowserCad(true);
        }

        private void OnHostedBrowserSelectCadClick(object sender, RoutedEventArgs e)
        {
            SelectHostedBrowserCad(false);
        }

        private void OnHostedBrowserZoomCadClick(object sender, RoutedEventArgs e)
        {
            SelectHostedBrowserCad(true);
        }

        private void SelectHostedBrowserCad(bool zoom)
        {
            try
            {
                if (!TryGetHostedBrowserCurrentProject(false, out var document, out var project, out var error))
                    throw new InvalidOperationException(error);
                var ids = (_hostedBrowserElements?.SelectedItems.Cast<object>().OfType<HostedBrowserElementRow>()
                               .Select(row => row.ElementId)
                               .ToList() ?? new List<string>());
                if (ids.Count == 0 && _hostedBrowserSelectedNodePath.Length > 0)
                {
                    var plan = ProjectBrowserWorkspaceCoordinator.Build(project, _hostedBrowserState, _hostedBrowserViewportOffset, HostedBrowserViewportPageSize);
                    ids = ProjectBrowserSelectionPlanner.PlanNodeSelection(
                            plan.Query.Root,
                            _hostedBrowserSelectedNodePath,
                            _hostedBrowserElementOffset,
                            HostedBrowserElementPageSize)
                        .ElementIds
                        .ToList();
                }
                if (ids.Count == 0) throw new InvalidOperationException("Project Browser chưa có semantic element để chọn CAD.");

                ResolveAndSelectHostedBrowserCad(document, project, ids, zoom);
            }
            catch (Exception ex)
            {
                SetHostedBrowserStatus("Browser → CAD bị từ chối: " + ex.Message);
            }
        }

        private void ResolveAndSelectHostedBrowserCad(Document document, ProjectState project, IReadOnlyList<string> elementIds, bool zoom)
        {
            RequireHostedBrowserIdentity(project, _hostedBrowserProjectId, _hostedBrowserDrawingFingerprint);
            var ids = elementIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0) throw new InvalidOperationException("Project Browser semantic selection is empty.");

            foreach (var id in ids)
            {
                var element = project.FindElement(id)
                    ?? throw new InvalidOperationException("Semantic element đã bị xóa/stale: " + id + ". Hãy Refresh Project Browser.");
                if (string.IsNullOrWhiteSpace(element.FamilyId) || project.FindFamily(element.FamilyId) == null)
                    throw new InvalidOperationException("Semantic element không còn Family hợp lệ: " + id + ". Hãy Refresh Project Browser.");
            }

            var handles = SourceHandleResolver.Resolve(project, ids)
                .Select(handle => CadHandleService.NormalizeHexHandle(handle)
                    ?? throw new InvalidOperationException("Semantic provenance chứa CAD Handle không hợp lệ."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(handle => handle, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (handles.Count == 0)
                throw new InvalidOperationException("Semantic selection không có CAD provenance có thể Locate.");

            var objectIds = CadHandleService.Resolve(document, handles);
            if (objectIds.Count != handles.Count)
                throw new InvalidOperationException("Không resolve đủ live CAD objects; selection được giữ nguyên để tránh partial Locate.");

            var state = ProjectBrowserWorkspaceCoordinator.ApplySelection(project, _hostedBrowserState, ids, ids[0]);
            document.Editor.SetImpliedSelection(objectIds.ToArray());
            try
            {
                PersistHostedBrowserState(document, project, state);
                _hostedBrowserState = state;
            }
            catch (Exception persistenceError)
            {
                SetHostedBrowserStatus("Đã chọn CAD nhưng không lưu được browser presentation state: " + persistenceError.Message);
            }
            if (zoom) Send("QS3DZOOMSELECTED");
            QueueHostedBrowserRefresh(false);
        }

        private void SyncHostedBrowserFromCadInspection()
        {
            if (!_hostedBrowserAttached || _hostedBrowserNodes == null) return;
            try
            {
                if (!TryGetHostedBrowserCurrentProject(false, out var document, out var project, out var error))
                {
                    RefreshHostedProjectBrowser(true);
                    if (!TryGetHostedBrowserCurrentProject(false, out document, out project, out error))
                        throw new InvalidOperationException(error);
                }

                IReadOnlyList<ProjectElement> elements;
                string selectionError;
                if (_inspection.Count == 0)
                {
                    elements = Array.Empty<ProjectElement>();
                    selectionError = string.Empty;
                }
                else if (!TryResolveSemanticSelection(project, _inspection, out elements, out selectionError))
                {
                    elements = Array.Empty<ProjectElement>();
                }

                var ids = elements.Select(element => element.Id).ToList();
                var state = ProjectBrowserWorkspaceCoordinator.ApplySelection(
                    project,
                    _hostedBrowserState,
                    ids,
                    ids.Count == 0 ? null : ids[0]);
                PersistHostedBrowserState(document, project, state);
                _hostedBrowserState = state;
                _hostedBrowserViewportOffset = 0;
                _hostedBrowserElementOffset = 0;
                _hostedBrowserSelectedNodePath = string.Empty;
                RefreshHostedProjectBrowser(false);
                if (!string.IsNullOrWhiteSpace(selectionError))
                    SetHostedBrowserStatus(selectionError + " Project Browser selection đã được clear fail-closed.");
            }
            catch (Exception ex)
            {
                SetHostedBrowserStatus("CAD → Browser bị từ chối: " + ex.Message);
            }
        }

        private void OnHostedBrowserPreviousNodesClick(object sender, RoutedEventArgs e)
        {
            _hostedBrowserViewportOffset = Math.Max(0, _hostedBrowserViewportOffset - HostedBrowserViewportPageSize);
            RefreshHostedProjectBrowser(false);
        }

        private void OnHostedBrowserNextNodesClick(object sender, RoutedEventArgs e)
        {
            _hostedBrowserViewportOffset += HostedBrowserViewportPageSize;
            RefreshHostedProjectBrowser(false);
        }

        private void OnHostedBrowserPreviousElementsClick(object sender, RoutedEventArgs e)
        {
            _hostedBrowserElementOffset = Math.Max(0, _hostedBrowserElementOffset - HostedBrowserElementPageSize);
            RefreshHostedProjectBrowser(false);
        }

        private void OnHostedBrowserNextElementsClick(object sender, RoutedEventArgs e)
        {
            _hostedBrowserElementOffset += HostedBrowserElementPageSize;
            RefreshHostedProjectBrowser(false);
        }

        private bool TryGetHostedBrowserCurrentProject(
            bool allowRebind,
            out Document document,
            out ProjectState project,
            out string error)
        {
            document = Application.DocumentManager.MdiActiveDocument;
            project = null!;
            error = string.Empty;
            if (document == null)
            {
                error = "Không có bản vẽ BricsCAD active.";
                return false;
            }
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out project))
            {
                error = "Bản vẽ active không có QS3D project khả dụng; Project Browser đã fail-closed.";
                return false;
            }

            if (!allowRebind && _hostedBrowserProjectId.Length > 0)
            {
                if (!string.Equals(project.ProjectId, _hostedBrowserProjectId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(project.DrawingFingerprint, _hostedBrowserDrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    error = "Active DWG/project đã đổi; callback Project Browser cũ không được phép tác động sang bản vẽ mới.";
                    return false;
                }
            }
            return true;
        }

        private void PersistHostedBrowserState(Document document, ProjectState readOnlyProject, ProjectBrowserWorkspaceState state)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (readOnlyProject == null) throw new ArgumentNullException(nameof(readOnlyProject));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("Active DWG changed before Project Browser presentation state could be persisted.");

            var project = ExistingProjectMutationContext.Require(document, "Project Browser presentation state");
            RequireHostedBrowserIdentity(project, readOnlyProject.ProjectId, readOnlyProject.DrawingFingerprint);
            var version = project.ChangeVersion;
            _hostedBrowserStateStore.Save(project, state);
            RequireHostedBrowserVersionInvariant(project, version);
        }

        private static void RequireHostedBrowserIdentity(ProjectState project, string projectId, string drawingFingerprint)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(projectId) ||
                !string.Equals(project.ProjectId, projectId, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(drawingFingerprint) ||
                !string.Equals(project.DrawingFingerprint, drawingFingerprint, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Project Browser project/DWG identity is stale; Refresh required.");
        }

        private static void RequireHostedBrowserVersionInvariant(ProjectState project, long expectedVersion)
        {
            if (project.ChangeVersion != expectedVersion)
                throw new InvalidOperationException("Project Browser presentation-only operation changed semantic ChangeVersion unexpectedly.");
        }

        private void ClearHostedProjectBrowser(string status)
        {
            _hostedBrowserProjectId = string.Empty;
            _hostedBrowserDrawingFingerprint = string.Empty;
            _hostedBrowserState = new ProjectBrowserWorkspaceState();
            _hostedBrowserViewportOffset = 0;
            _hostedBrowserElementOffset = 0;
            _hostedBrowserSelectedNodePath = string.Empty;
            if (_hostedBrowserNodes != null) _hostedBrowserNodes.ItemsSource = Array.Empty<HostedBrowserNodeRow>();
            if (_hostedBrowserElements != null) _hostedBrowserElements.ItemsSource = Array.Empty<HostedBrowserElementRow>();
            if (_hostedBrowserNodePageStatus != null) _hostedBrowserNodePageStatus.Text = "0 node";
            if (_hostedBrowserElementPageStatus != null) _hostedBrowserElementPageStatus.Text = "0 id";
            SetHostedBrowserEnabled(false);
            SetHostedBrowserStatus(status);
        }

        private void SetHostedBrowserEnabled(bool enabled)
        {
            if (_hostedBrowserQuery != null) _hostedBrowserQuery.IsEnabled = enabled;
            if (_hostedBrowserGrouping != null) _hostedBrowserGrouping.IsEnabled = enabled;
            if (_hostedBrowserDirtyOnly != null) _hostedBrowserDirtyOnly.IsEnabled = enabled;
            if (_hostedBrowserNodes != null) _hostedBrowserNodes.IsEnabled = enabled;
            if (_hostedBrowserElements != null) _hostedBrowserElements.IsEnabled = enabled;
            if (_hostedBrowserSelectCad != null) _hostedBrowserSelectCad.IsEnabled = enabled;
            if (_hostedBrowserZoomCad != null) _hostedBrowserZoomCad.IsEnabled = enabled;
            if (_hostedBrowserReset != null) _hostedBrowserReset.IsEnabled = enabled;
        }

        private void UpdateHostedBrowserPaging(ProjectBrowserViewport viewport)
        {
            if (_hostedBrowserNodePageStatus != null)
            {
                var first = viewport.Rows.Count == 0 ? 0 : viewport.Offset + 1;
                var last = viewport.Offset + viewport.Rows.Count;
                _hostedBrowserNodePageStatus.Text = first + "-" + last + " / " + viewport.TotalVisibleRows;
            }
            if (_hostedBrowserPreviousNodes != null) _hostedBrowserPreviousNodes.IsEnabled = viewport.HasPrevious;
            if (_hostedBrowserNextNodes != null) _hostedBrowserNextNodes.IsEnabled = viewport.HasNext;
        }

        private void UpdateHostedBrowserElementPaging(ProjectBrowserElementPage? page)
        {
            if (_hostedBrowserElementPageStatus != null)
            {
                var first = page == null || page.ElementIds.Count == 0 ? 0 : page.Offset + 1;
                var last = page == null ? 0 : page.Offset + page.ElementIds.Count;
                var total = page?.TotalCount ?? 0;
                _hostedBrowserElementPageStatus.Text = first + "-" + last + " / " + total;
            }
            if (_hostedBrowserPreviousElements != null) _hostedBrowserPreviousElements.IsEnabled = page?.HasPrevious == true;
            if (_hostedBrowserNextElements != null) _hostedBrowserNextElements.IsEnabled = page?.HasNext == true;
        }

        private void SetHostedBrowserStatus(string status)
        {
            if (_hostedBrowserStatus != null) _hostedBrowserStatus.Text = status ?? string.Empty;
        }

        private sealed class HostedBrowserGroupingOption
        {
            public HostedBrowserGroupingOption(ProjectBrowserGrouping value, string displayName)
            {
                Value = value;
                DisplayName = displayName ?? string.Empty;
            }

            public ProjectBrowserGrouping Value { get; }
            public string DisplayName { get; }
        }

        private sealed class HostedBrowserNodeRow
        {
            public HostedBrowserNodeRow(ProjectBrowserVisibleRow row)
            {
                Path = row.Path;
                HasChildren = row.HasChildren;
                IsExpanded = row.IsExpanded;
                var indent = new string(' ', Math.Min(12, row.Depth * 2));
                var marker = row.HasChildren ? (row.IsExpanded ? "▼ " : "▶ ") : "• ";
                Display = indent + marker + row.DisplayName + " (" + row.Count + (row.DirtyCount > 0 ? ", dirty " + row.DirtyCount : string.Empty) + ")";
            }

            public string Path { get; }
            public bool HasChildren { get; }
            public bool IsExpanded { get; }
            public string Display { get; }
            public override string ToString() => Display;
        }

        private sealed class HostedBrowserElementRow
        {
            private HostedBrowserElementRow(string elementId, string display)
            {
                ElementId = elementId;
                Display = display;
            }

            public string ElementId { get; }
            public string Display { get; }
            public override string ToString() => Display;

            public static HostedBrowserElementRow Create(ProjectState project, string elementId)
            {
                var element = project.FindElement(elementId)
                    ?? throw new InvalidOperationException("Project Browser element page references missing semantic element: " + elementId + ".");
                var family = string.IsNullOrWhiteSpace(element.FamilyId) ? null : project.FindFamily(element.FamilyId);
                var familyName = family?.Name ?? "(no Family)";
                return new HostedBrowserElementRow(element.Id, element.Category + " • " + familyName + " • " + element.Id);
            }
        }
    }
}
