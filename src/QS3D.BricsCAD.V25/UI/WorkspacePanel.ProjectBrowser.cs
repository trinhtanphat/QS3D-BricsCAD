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
using QS3D.Core.Navigation;
using QS3D.Core.Persistence;
using QS3D.Core.Services;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Hosted V25 adapter for the Core Project Browser. Modeless state retains only semantic and
    /// presentation identity. Native CAD identity is resolved against the exact active DWG at the
    /// instant a Browser -> CAD action commits.
    /// </summary>
    public partial class WorkspacePanel
    {
        private const int BrowserNodePageSize = 100;
        private const int BrowserElementPageSize = 100;

        private static readonly bool BrowserClassHandlersRegistered = RegisterBrowserClassHandlers();

        private readonly ProjectBrowserWorkspaceStateStore _browserStateStore = new ProjectBrowserWorkspaceStateStore();
        private ProjectBrowserWorkspaceState _browserState = new ProjectBrowserWorkspaceState();
        private string _browserProjectId = string.Empty;
        private string _browserDrawingFingerprint = string.Empty;
        private string _browserInspectionProjectId = string.Empty;
        private string _browserInspectionDrawingFingerprint = string.Empty;
        private string _browserNodePath = string.Empty;
        private int _browserNodeOffset;
        private int _browserElementOffset;
        private bool _browserAttached;
        private bool _browserUpdating;
        private bool _browserRefreshQueued;
        private bool _browserForceRebindQueued;
        private long _browserAttachmentGeneration;
        private DependencyPropertyDescriptor? _browserInspectionSourceDescriptor;

        private TabControl? _browserTabs;
        private TabItem? _browserTab;
        private TextBox? _browserQuery;
        private ComboBox? _browserGrouping;
        private CheckBox? _browserDirtyOnly;
        private ListBox? _browserNodes;
        private ListBox? _browserElements;
        private TextBlock? _browserStatus;
        private TextBlock? _browserNodePage;
        private TextBlock? _browserElementPage;
        private Button? _browserNodePrev;
        private Button? _browserNodeNext;
        private Button? _browserElementPrev;
        private Button? _browserElementNext;
        private Button? _browserSelectCad;
        private Button? _browserZoomCad;
        private Button? _browserReset;

        private static bool RegisterBrowserClassHandlers()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBrowserWorkspaceLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnBrowserWorkspaceUnloaded),
                true);
            return true;
        }

        private static void OnBrowserWorkspaceLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel) panel.AttachProjectBrowser();
        }

        private static void OnBrowserWorkspaceUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel) panel.DetachProjectBrowser();
        }

        private void AttachProjectBrowser()
        {
            if (!BrowserClassHandlersRegistered)
                throw new InvalidOperationException("Project Browser class handlers were not registered.");
            EnsureProjectBrowserSurface();
            if (_browserAttached) return;
            _browserAttachmentGeneration++;
            _browserAttached = true;
            DataContextChanged += OnBrowserDataContextChanged;
            _browserInspectionSourceDescriptor = DependencyPropertyDescriptor.FromProperty(
                ItemsControl.ItemsSourceProperty,
                typeof(ListView));
            _browserInspectionSourceDescriptor?.AddValueChanged(InspectionList, OnBrowserInspectionSourceChanged);
            CaptureBrowserInspectionIdentity();
            QueueBrowserRefresh(true);
        }

        private void DetachProjectBrowser()
        {
            if (!_browserAttached) return;
            _browserAttachmentGeneration++;
            _browserAttached = false;
            DataContextChanged -= OnBrowserDataContextChanged;
            _browserInspectionSourceDescriptor?.RemoveValueChanged(InspectionList, OnBrowserInspectionSourceChanged);
            _browserInspectionSourceDescriptor = null;
            _browserProjectId = string.Empty;
            _browserDrawingFingerprint = string.Empty;
            _browserInspectionProjectId = string.Empty;
            _browserInspectionDrawingFingerprint = string.Empty;
            _browserRefreshQueued = false;
            _browserForceRebindQueued = false;
        }

        private bool IsCurrentBrowserAttachment(long generation)
        {
            return _browserAttached && IsLoaded && generation == _browserAttachmentGeneration;
        }

        private void OnBrowserDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The inspection list can still contain the old document while modeless Workspace is
            // being rebound. Do not let that stale snapshot acquire the new document identity.
            _browserInspectionProjectId = string.Empty;
            _browserInspectionDrawingFingerprint = string.Empty;
            QueueBrowserRefresh(true);
        }

        private void OnBrowserInspectionSourceChanged(object? sender, EventArgs e)
        {
            if (!_browserAttached || _browserUpdating) return;
            CaptureBrowserInspectionIdentity();
            var generation = _browserAttachmentGeneration;
            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    if (!IsCurrentBrowserAttachment(generation)) return;
                    SyncProjectBrowserFromCad();
                }));
        }

        private void CaptureBrowserInspectionIdentity()
        {
            _browserInspectionProjectId = string.Empty;
            _browserInspectionDrawingFingerprint = string.Empty;
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null || !ProjectContextCoordinator.TryGetReadOnly(document, out var project)) return;
            _browserInspectionProjectId = project.ProjectId;
            _browserInspectionDrawingFingerprint = project.DrawingFingerprint;
        }

        private void QueueBrowserRefresh(bool forceRebind)
        {
            if (!_browserAttached || !IsLoaded) return;
            _browserForceRebindQueued |= forceRebind;
            if (_browserRefreshQueued) return;
            _browserRefreshQueued = true;
            var generation = _browserAttachmentGeneration;
            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    if (!IsCurrentBrowserAttachment(generation)) return;
                    _browserRefreshQueued = false;
                    var force = _browserForceRebindQueued;
                    _browserForceRebindQueued = false;
                    RefreshProjectBrowser(force);
                }));
        }

        private void EnsureProjectBrowserSurface()
        {
            if (_browserTabs != null || !(ModelTree.Parent is DockPanel modelDock)) return;
            modelDock.Children.Remove(ModelTree);
            var tabs = new TabControl
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Background = TryFindResource("Bg1Brush") as Brush,
                BorderBrush = TryFindResource("BorderBrush") as Brush,
                BorderThickness = new Thickness(0)
            };
            tabs.Items.Add(new TabItem { Header = "Mô hình", Content = ModelTree });
            var browserTab = new TabItem { Header = "Project Browser", Content = CreateProjectBrowserSurface() };
            tabs.Items.Add(browserTab);
            tabs.SelectionChanged += OnBrowserTabSelectionChanged;
            modelDock.Children.Add(tabs);
            _browserTabs = tabs;
            _browserTab = browserTab;
        }

        private FrameworkElement CreateProjectBrowserSurface()
        {
            var root = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(108) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var controls = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
            controls.Children.Add(new TextBlock { Text = "Tìm semantic", FontSize = 9 });
            var query = new TextBox { MinHeight = 23, ToolTip = "Tìm ID, Family, Category, tầng hoặc Zone; Enter để áp dụng." };
            query.KeyDown += OnBrowserQueryKeyDown;
            controls.Children.Add(query);

            var grouping = new ComboBox
            {
                MinHeight = 23,
                Margin = new Thickness(0, 3, 0, 0),
                ItemsSource = new[]
                {
                    new BrowserGroupingOption(ProjectBrowserGrouping.FloorThenCategory, "Tầng > Category"),
                    new BrowserGroupingOption(ProjectBrowserGrouping.ZoneThenCategory, "Zone > Category"),
                    new BrowserGroupingOption(ProjectBrowserGrouping.Category, "Category")
                },
                DisplayMemberPath = nameof(BrowserGroupingOption.DisplayName),
                SelectedValuePath = nameof(BrowserGroupingOption.Value)
            };
            grouping.SelectionChanged += OnBrowserGroupingChanged;
            controls.Children.Add(grouping);

            var dirtyOnly = new CheckBox { Content = "Chỉ cấu kiện dirty", FontSize = 9, Margin = new Thickness(0, 3, 0, 0) };
            dirtyOnly.Click += OnBrowserDirtyOnlyChanged;
            controls.Children.Add(dirtyOnly);

            var controlsRow = new WrapPanel { Margin = new Thickness(0, 3, 0, 0) };
            var refresh = new Button { Content = "Làm mới", MinHeight = 22, Margin = new Thickness(0, 0, 3, 0) };
            refresh.Click += OnBrowserRefreshClick;
            var reset = new Button { Content = "Reset", MinHeight = 22, ToolTip = "Xóa presentation state Project Browser." };
            reset.Click += OnBrowserResetClick;
            controlsRow.Children.Add(refresh);
            controlsRow.Children.Add(reset);
            controls.Children.Add(controlsRow);
            Grid.SetRow(controls, 0);
            root.Children.Add(controls);

            var status = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 9, Margin = new Thickness(0, 0, 0, 4) };
            Grid.SetRow(status, 1);
            root.Children.Add(status);

            var nodes = new ListBox
            {
                SelectionMode = SelectionMode.Single,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                ToolTip = "Chọn node để xem semantic IDs; double-click để mở/đóng node."
            };
            nodes.SelectionChanged += OnBrowserNodeSelectionChanged;
            nodes.MouseDoubleClick += OnBrowserNodeDoubleClick;
            Grid.SetRow(nodes, 2);
            root.Children.Add(nodes);

            var elements = new ListBox
            {
                SelectionMode = SelectionMode.Extended,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 0),
                ToolTip = "Semantic IDs; double-click để chọn + zoom CAD."
            };
            elements.MouseDoubleClick += OnBrowserElementDoubleClick;
            Grid.SetRow(elements, 3);
            root.Children.Add(elements);

            var footer = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            var nodePager = BuildBrowserPager("‹", "›", out var nodePrev, out var nodeNext, out var nodePage);
            nodePrev.Click += OnBrowserPreviousNodesClick;
            nodeNext.Click += OnBrowserNextNodesClick;
            footer.Children.Add(nodePager);
            var elementPager = BuildBrowserPager("‹ id", "id ›", out var elementPrev, out var elementNext, out var elementPage);
            elementPager.Margin = new Thickness(0, 2, 0, 0);
            elementPrev.Click += OnBrowserPreviousElementsClick;
            elementNext.Click += OnBrowserNextElementsClick;
            footer.Children.Add(elementPager);

            var actions = new WrapPanel { Margin = new Thickness(0, 3, 0, 0) };
            var selectCad = new Button { Content = "Chọn CAD", MinHeight = 22, Margin = new Thickness(0, 0, 3, 0) };
            selectCad.Click += OnBrowserSelectCadClick;
            var zoomCad = new Button { Content = "Zoom", MinHeight = 22 };
            zoomCad.Click += OnBrowserZoomCadClick;
            actions.Children.Add(selectCad);
            actions.Children.Add(zoomCad);
            footer.Children.Add(actions);
            Grid.SetRow(footer, 4);
            root.Children.Add(footer);

            _browserQuery = query;
            _browserGrouping = grouping;
            _browserDirtyOnly = dirtyOnly;
            _browserNodes = nodes;
            _browserElements = elements;
            _browserStatus = status;
            _browserNodePage = nodePage;
            _browserElementPage = elementPage;
            _browserNodePrev = nodePrev;
            _browserNodeNext = nodeNext;
            _browserElementPrev = elementPrev;
            _browserElementNext = elementNext;
            _browserSelectCad = selectCad;
            _browserZoomCad = zoomCad;
            _browserReset = reset;
            return root;
        }

        private static DockPanel BuildBrowserPager(
            string previousLabel,
            string nextLabel,
            out Button previous,
            out Button next,
            out TextBlock status)
        {
            var panel = new DockPanel();
            previous = new Button { Content = previousLabel, Width = 38, MinHeight = 21 };
            next = new Button { Content = nextLabel, Width = 38, MinHeight = 21 };
            status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 5, 0), FontSize = 9 };
            DockPanel.SetDock(previous, Dock.Left);
            DockPanel.SetDock(next, Dock.Right);
            panel.Children.Add(previous);
            panel.Children.Add(next);
            panel.Children.Add(status);
            return panel;
        }

        private void OnBrowserTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, _browserTabs)) return;
            if (ReferenceEquals(_browserTabs?.SelectedItem, _browserTab)) QueueBrowserRefresh(true);
        }

        private void OnBrowserRefreshClick(object sender, RoutedEventArgs e)
        {
            RefreshProjectBrowser(true);
            CaptureBrowserInspectionIdentity();
            SyncProjectBrowserFromCad();
        }

        private void OnBrowserResetClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryCurrentBrowserProject(true, out var document, out var expectedProject, out var error))
                    throw new InvalidOperationException(error);
                var project = RequireCanonicalBrowserMutationProject(document, expectedProject, "Reset Project Browser presentation state");
                var version = project.ChangeVersion;
                _browserStateStore.Clear(project);
                RequireBrowserVersionInvariant(project, version);
                _browserState = new ProjectBrowserWorkspaceState();
                _browserNodePath = string.Empty;
                _browserNodeOffset = 0;
                _browserElementOffset = 0;
                RefreshProjectBrowser(true);
            }
            catch (Exception ex)
            {
                SetBrowserStatus("Reset Project Browser bị từ chối: " + ex.Message);
            }
        }

        private void OnBrowserQueryKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            ApplyBrowserView();
            e.Handled = true;
        }

        private void OnBrowserGroupingChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_browserUpdating) ApplyBrowserView();
        }

        private void OnBrowserDirtyOnlyChanged(object sender, RoutedEventArgs e)
        {
            if (!_browserUpdating) ApplyBrowserView();
        }

        private void ApplyBrowserView()
        {
            if (_browserQuery == null || _browserGrouping == null || _browserDirtyOnly == null || _browserUpdating) return;
            try
            {
                if (!TryCurrentBrowserProject(false, out var document, out var project, out var error))
                    throw new InvalidOperationException(error);
                var grouping = _browserGrouping.SelectedValue is ProjectBrowserGrouping selected
                    ? selected
                    : ProjectBrowserGrouping.FloorThenCategory;
                var state = new ProjectBrowserWorkspaceState(
                    grouping,
                    _browserQuery.Text,
                    _browserDirtyOnly.IsChecked == true,
                    _browserState.Categories,
                    _browserState.FloorIds,
                    _browserState.ZoneIds,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    string.Empty);
                PersistBrowserState(document, project, state);
                _browserState = state;
                _browserNodePath = string.Empty;
                _browserNodeOffset = 0;
                _browserElementOffset = 0;
                RefreshProjectBrowser(false);
            }
            catch (Exception ex)
            {
                SetBrowserStatus("Project Browser view bị từ chối: " + ex.Message);
            }
        }

        private void RefreshProjectBrowser(bool forceRebind, bool revealPrimarySelection = false)
        {
            if (_browserNodes == null || _browserElements == null) return;
            try
            {
                if (!TryCurrentBrowserProject(forceRebind, out _, out var project, out var error))
                {
                    ClearProjectBrowser(error);
                    return;
                }
                if (forceRebind ||
                    !string.Equals(project.ProjectId, _browserProjectId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(project.DrawingFingerprint, _browserDrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    _browserState = _browserStateStore.Load(project);
                    _browserProjectId = project.ProjectId;
                    _browserDrawingFingerprint = project.DrawingFingerprint;
                    _browserNodePath = string.Empty;
                    _browserNodeOffset = 0;
                    _browserElementOffset = 0;
                }
                var version = project.ChangeVersion;
                var plan = ProjectBrowserWorkspaceCoordinator.Build(
                    project,
                    _browserState,
                    _browserNodeOffset,
                    BrowserNodePageSize,
                    revealPrimarySelection);
                RequireBrowserVersionInvariant(project, version);
                RenderProjectBrowser(project, plan);
            }
            catch (Exception ex)
            {
                ClearProjectBrowser("Project Browser fail-closed: " + ex.Message);
            }
        }

        private void RenderProjectBrowser(ProjectState project, ProjectBrowserWorkspacePlan plan)
        {
            if (_browserNodes == null || _browserElements == null || _browserGrouping == null ||
                _browserQuery == null || _browserDirtyOnly == null) return;
            _browserUpdating = true;
            try
            {
                _browserGrouping.SelectedValue = _browserState.Grouping;
                if (!_browserQuery.IsKeyboardFocusWithin) _browserQuery.Text = _browserState.Query;
                _browserDirtyOnly.IsChecked = _browserState.DirtyOnly;
                var nodeRows = plan.Viewport.Rows.Select(row => new BrowserNodeRow(row)).ToList();
                _browserNodes.ItemsSource = nodeRows;
                _browserNodeOffset = plan.Viewport.Offset;
                var targetPath = _browserNodePath;
                if (plan.PrimaryTargetNodePath.Length > 0) targetPath = plan.PrimaryTargetNodePath;
                var selectedNode = nodeRows.FirstOrDefault(row => string.Equals(row.Path, targetPath, StringComparison.Ordinal));
                if (selectedNode == null && targetPath.Length == 0 && nodeRows.Count > 0) selectedNode = nodeRows[0];
                _browserNodes.SelectedItem = selectedNode;
                if (selectedNode != null) _browserNodes.ScrollIntoView(selectedNode);
                _browserNodePath = selectedNode?.Path ?? targetPath;
                RenderProjectBrowserElements(project, plan.Query.Root);
                UpdateBrowserNodePaging(plan.Viewport);
                SetBrowserEnabled(true);
                SetBrowserStatus(
                    plan.Viewport.TotalVisibleRows + " node • " + plan.Query.MatchedCount +
                    " / " + plan.Query.TotalCount + " semantic • selection " + plan.Reveal.SelectedElementIds.Count + ".");
            }
            finally { _browserUpdating = false; }
        }

        private void RenderProjectBrowserElements(ProjectState project, ProjectBrowserNode root)
        {
            if (_browserElements == null) return;
            if (_browserNodePath.Length == 0)
            {
                _browserElements.ItemsSource = Array.Empty<BrowserElementRow>();
                UpdateBrowserElementPaging(null);
                return;
            }
            var page = ProjectBrowserVirtualizationPlanner.GetElementPage(root, _browserNodePath, _browserElementOffset, BrowserElementPageSize);
            var rows = page.ElementIds.Select(id => BrowserElementRow.Create(project, id)).ToList();
            _browserElements.ItemsSource = rows;
            var selectedIds = new HashSet<string>(_browserState.SelectedElementIds, StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.Where(row => selectedIds.Contains(row.ElementId))) _browserElements.SelectedItems.Add(row);
            if (_browserElements.SelectedItems.Count > 0) _browserElements.ScrollIntoView(_browserElements.SelectedItems[0]);
            UpdateBrowserElementPaging(page);
        }

        private void OnBrowserNodeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_browserUpdating || !(_browserNodes?.SelectedItem is BrowserNodeRow row)) return;
            _browserNodePath = row.Path;
            _browserElementOffset = 0;
            try
            {
                if (!TryCurrentBrowserProject(false, out _, out var project, out var error)) throw new InvalidOperationException(error);
                var plan = ProjectBrowserWorkspaceCoordinator.Build(project, _browserState, _browserNodeOffset, BrowserNodePageSize);
                _browserUpdating = true;
                try { RenderProjectBrowserElements(project, plan.Query.Root); }
                finally { _browserUpdating = false; }
            }
            catch (Exception ex) { SetBrowserStatus("Project Browser node bị từ chối: " + ex.Message); }
        }

        private void OnBrowserNodeDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(_browserNodes?.SelectedItem is BrowserNodeRow row) || !row.HasChildren) return;
            try
            {
                if (!TryCurrentBrowserProject(false, out var document, out var project, out var error)) throw new InvalidOperationException(error);
                var state = ProjectBrowserWorkspaceCoordinator.SetExpanded(project, _browserState, row.Path, !row.IsExpanded);
                PersistBrowserState(document, project, state);
                _browserState = state;
                _browserNodeOffset = 0;
                RefreshProjectBrowser(false);
            }
            catch (Exception ex) { SetBrowserStatus("Project Browser expand/collapse bị từ chối: " + ex.Message); }
        }

        private void OnBrowserElementDoubleClick(object sender, MouseButtonEventArgs e) => SelectBrowserCad(true);
        private void OnBrowserSelectCadClick(object sender, RoutedEventArgs e) => SelectBrowserCad(false);
        private void OnBrowserZoomCadClick(object sender, RoutedEventArgs e) => SelectBrowserCad(true);

        private void SelectBrowserCad(bool zoom)
        {
            try
            {
                if (!TryCurrentBrowserProject(false, out var document, out var project, out var error)) throw new InvalidOperationException(error);
                var ids = _browserElements?.SelectedItems.Cast<object>().OfType<BrowserElementRow>().Select(row => row.ElementId).ToList()
                          ?? new List<string>();
                if (ids.Count == 0 && _browserNodePath.Length > 0)
                {
                    var plan = ProjectBrowserWorkspaceCoordinator.Build(project, _browserState, _browserNodeOffset, BrowserNodePageSize);
                    ids = ProjectBrowserSelectionPlanner.PlanNodeSelection(
                            plan.Query.Root,
                            _browserNodePath,
                            _browserElementOffset,
                            BrowserElementPageSize)
                        .ElementIds.ToList();
                }
                if (ids.Count == 0) throw new InvalidOperationException("Project Browser chưa có semantic element để chọn CAD.");
                ResolveAndSelectBrowserCad(document, project, ids, zoom);
            }
            catch (Exception ex) { SetBrowserStatus("Browser → CAD bị từ chối: " + ex.Message); }
        }

        private void ResolveAndSelectBrowserCad(Document document, ProjectState project, IReadOnlyList<string> elementIds, bool zoom)
        {
            RequireBrowserIdentity(project, _browserProjectId, _browserDrawingFingerprint);
            var sourceVersion = project.ChangeVersion;
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
                if (!string.IsNullOrWhiteSpace(element.FamilyId) && project.FindFamily(element.FamilyId) == null)
                    throw new InvalidOperationException("Semantic element tham chiếu Family đã bị xóa/missing: " + id + ". Hãy Refresh Project Browser.");
            }

            var handles = SourceHandleResolver.Resolve(project, ids)
                .Select(handle => CadHandleService.NormalizeHexHandle(handle)
                    ?? throw new InvalidOperationException("Semantic provenance chứa CAD Handle không hợp lệ."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(handle => handle, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (handles.Count == 0) throw new InvalidOperationException("Semantic selection không có CAD provenance có thể Locate.");
            var objectIds = CadHandleService.Resolve(document, handles);
            if (objectIds.Count != handles.Count)
                throw new InvalidOperationException("Không resolve đủ live CAD objects; PICKFIRST được giữ nguyên để tránh partial Locate.");

            var state = ProjectBrowserWorkspaceCoordinator.ApplySelection(project, _browserState, ids, ids[0]);
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("Active DWG changed before Browser → CAD selection commit; PICKFIRST was not changed.");
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject) || !ReferenceEquals(currentProject, project))
                throw new InvalidOperationException("Project Browser canonical project instance changed before CAD selection; PICKFIRST was not changed.");
            RequireBrowserIdentity(currentProject, _browserProjectId, _browserDrawingFingerprint);
            if (project.ChangeVersion != sourceVersion)
                throw new InvalidOperationException("Project changed before Browser → CAD selection commit; PICKFIRST was not changed.");

            document.Editor.SetImpliedSelection(objectIds.ToArray());
            try
            {
                PersistBrowserState(document, project, state);
                _browserState = state;
            }
            catch (Exception persistenceError)
            {
                SetBrowserStatus("Đã chọn CAD nhưng không lưu được browser presentation state: " + persistenceError.Message);
            }
            if (zoom)
            {
                if (ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)) Send("QS3DZOOMSELECTED");
                else SetBrowserStatus("CAD selection đã commit trên DWG nguồn; active DWG đổi trước Zoom nên Zoom bị bỏ qua.");
            }
            QueueBrowserRefresh(false);
        }

        private void SyncProjectBrowserFromCad()
        {
            if (!_browserAttached || _browserNodes == null) return;
            try
            {
                if (!TryCurrentBrowserProject(false, out var document, out var project, out var error))
                {
                    RefreshProjectBrowser(true);
                    if (!TryCurrentBrowserProject(false, out document, out project, out error)) throw new InvalidOperationException(error);
                }
                if (string.IsNullOrWhiteSpace(_browserInspectionProjectId) ||
                    !string.Equals(project.ProjectId, _browserInspectionProjectId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(project.DrawingFingerprint, _browserInspectionDrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("CAD inspection belongs to a stale/other DWG; callback ignored until current selection refresh.");

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
                var state = ProjectBrowserWorkspaceCoordinator.ApplySelection(project, _browserState, ids, ids.Count == 0 ? null : ids[0]);
                PersistBrowserState(document, project, state);
                _browserState = state;
                if (ids.Count > 0)
                {
                    _browserNodePath = string.Empty;
                    _browserElementOffset = 0;
                }
                RefreshProjectBrowser(false, ids.Count > 0);
                if (!string.IsNullOrWhiteSpace(selectionError))
                    SetBrowserStatus(selectionError + " Project Browser selection đã clear fail-closed.");
            }
            catch (Exception ex) { SetBrowserStatus("CAD → Browser bị từ chối: " + ex.Message); }
        }

        private void OnBrowserPreviousNodesClick(object sender, RoutedEventArgs e)
        {
            _browserNodeOffset = Math.Max(0, _browserNodeOffset - BrowserNodePageSize);
            RefreshProjectBrowser(false);
        }

        private void OnBrowserNextNodesClick(object sender, RoutedEventArgs e)
        {
            _browserNodeOffset += BrowserNodePageSize;
            RefreshProjectBrowser(false);
        }

        private void OnBrowserPreviousElementsClick(object sender, RoutedEventArgs e)
        {
            _browserElementOffset = Math.Max(0, _browserElementOffset - BrowserElementPageSize);
            RefreshProjectBrowser(false);
        }

        private void OnBrowserNextElementsClick(object sender, RoutedEventArgs e)
        {
            _browserElementOffset += BrowserElementPageSize;
            RefreshProjectBrowser(false);
        }

        private bool TryCurrentBrowserProject(
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
            if (!allowRebind && _browserProjectId.Length == 0)
            {
                error = "Project Browser chưa bind canonical project; Refresh required.";
                return false;
            }
            if (!allowRebind &&
                (!string.Equals(project.ProjectId, _browserProjectId, StringComparison.OrdinalIgnoreCase) ||
                 !string.Equals(project.DrawingFingerprint, _browserDrawingFingerprint, StringComparison.OrdinalIgnoreCase)))
            {
                error = "Active DWG/project đã đổi; callback Project Browser cũ không được phép tác động sang bản vẽ mới.";
                return false;
            }
            return true;
        }

        private void PersistBrowserState(Document document, ProjectState expectedProject, ProjectBrowserWorkspaceState state)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("Active DWG changed before Project Browser state could be persisted.");
            var project = RequireCanonicalBrowserMutationProject(document, expectedProject, "Project Browser presentation state");
            var version = project.ChangeVersion;
            _browserStateStore.Save(project, state);
            RequireBrowserVersionInvariant(project, version);
        }

        private ProjectState RequireCanonicalBrowserMutationProject(Document document, ProjectState expectedProject, string context)
        {
            var project = ExistingProjectMutationContext.Require(document, context);
            if (!ReferenceEquals(project, expectedProject))
                throw new InvalidOperationException("Project Browser canonical project instance changed; Refresh required.");
            RequireBrowserIdentity(project, expectedProject.ProjectId, expectedProject.DrawingFingerprint);
            return project;
        }

        private static void RequireBrowserIdentity(ProjectState project, string projectId, string drawingFingerprint)
        {
            if (string.IsNullOrWhiteSpace(projectId) ||
                !string.Equals(project.ProjectId, projectId, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(drawingFingerprint) ||
                !string.Equals(project.DrawingFingerprint, drawingFingerprint, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Project Browser project/DWG identity is stale; Refresh required.");
        }

        private static void RequireBrowserVersionInvariant(ProjectState project, long expectedVersion)
        {
            if (project.ChangeVersion != expectedVersion)
                throw new InvalidOperationException("Project Browser presentation-only operation changed semantic ChangeVersion unexpectedly.");
        }

        private void ClearProjectBrowser(string status)
        {
            _browserProjectId = string.Empty;
            _browserDrawingFingerprint = string.Empty;
            _browserState = new ProjectBrowserWorkspaceState();
            _browserNodePath = string.Empty;
            _browserNodeOffset = 0;
            _browserElementOffset = 0;
            if (_browserNodes != null) _browserNodes.ItemsSource = Array.Empty<BrowserNodeRow>();
            if (_browserElements != null) _browserElements.ItemsSource = Array.Empty<BrowserElementRow>();
            if (_browserNodePage != null) _browserNodePage.Text = "0 node";
            if (_browserElementPage != null) _browserElementPage.Text = "0 id";
            SetBrowserEnabled(false);
            SetBrowserStatus(status);
        }

        private void SetBrowserEnabled(bool enabled)
        {
            if (_browserQuery != null) _browserQuery.IsEnabled = enabled;
            if (_browserGrouping != null) _browserGrouping.IsEnabled = enabled;
            if (_browserDirtyOnly != null) _browserDirtyOnly.IsEnabled = enabled;
            if (_browserNodes != null) _browserNodes.IsEnabled = enabled;
            if (_browserElements != null) _browserElements.IsEnabled = enabled;
            if (_browserSelectCad != null) _browserSelectCad.IsEnabled = enabled;
            if (_browserZoomCad != null) _browserZoomCad.IsEnabled = enabled;
            if (_browserReset != null) _browserReset.IsEnabled = true;
        }

        private void UpdateBrowserNodePaging(ProjectBrowserViewport viewport)
        {
            if (_browserNodePage != null)
            {
                var first = viewport.Rows.Count == 0 ? 0 : viewport.Offset + 1;
                _browserNodePage.Text = first + "-" + (viewport.Offset + viewport.Rows.Count) + " / " + viewport.TotalVisibleRows;
            }
            if (_browserNodePrev != null) _browserNodePrev.IsEnabled = viewport.HasPrevious;
            if (_browserNodeNext != null) _browserNodeNext.IsEnabled = viewport.HasNext;
        }

        private void UpdateBrowserElementPaging(ProjectBrowserElementPage? page)
        {
            if (_browserElementPage != null)
            {
                var first = page == null || page.ElementIds.Count == 0 ? 0 : page.Offset + 1;
                var last = page == null ? 0 : page.Offset + page.ElementIds.Count;
                _browserElementPage.Text = first + "-" + last + " / " + (page?.TotalCount ?? 0);
            }
            if (_browserElementPrev != null) _browserElementPrev.IsEnabled = page?.HasPrevious == true;
            if (_browserElementNext != null) _browserElementNext.IsEnabled = page?.HasNext == true;
        }

        private void SetBrowserStatus(string status)
        {
            if (_browserStatus != null) _browserStatus.Text = status ?? string.Empty;
        }

        private sealed class BrowserGroupingOption
        {
            public BrowserGroupingOption(ProjectBrowserGrouping value, string displayName)
            {
                Value = value;
                DisplayName = displayName;
            }

            public ProjectBrowserGrouping Value { get; }
            public string DisplayName { get; }
        }

        private sealed class BrowserNodeRow
        {
            public BrowserNodeRow(ProjectBrowserVisibleRow row)
            {
                Path = row.Path;
                HasChildren = row.HasChildren;
                IsExpanded = row.IsExpanded;
                var marker = row.HasChildren ? (row.IsExpanded ? "▼ " : "▶ ") : "• ";
                Display = new string(' ', Math.Min(12, row.Depth * 2)) + marker + row.DisplayName +
                          " (" + row.Count + (row.DirtyCount > 0 ? ", dirty " + row.DirtyCount : string.Empty) + ")";
            }

            public string Path { get; }
            public bool HasChildren { get; }
            public bool IsExpanded { get; }
            public string Display { get; }
            public override string ToString() => Display;
        }

        private sealed class BrowserElementRow
        {
            private BrowserElementRow(string elementId, string display)
            {
                ElementId = elementId;
                Display = display;
            }

            public string ElementId { get; }
            public string Display { get; }
            public override string ToString() => Display;

            public static BrowserElementRow Create(ProjectState project, string elementId)
            {
                var element = project.FindElement(elementId)
                    ?? throw new InvalidOperationException("Project Browser page references missing semantic element: " + elementId + ".");
                var family = string.IsNullOrWhiteSpace(element.FamilyId) ? null : project.FindFamily(element.FamilyId);
                return new BrowserElementRow(
                    element.Id,
                    element.Category + " • " + (family?.Name ?? "(no Family)") + " • " + element.Id);
            }
        }
    }
}
