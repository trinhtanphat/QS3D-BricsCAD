using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using Application = Bricscad.ApplicationServices.Application;
using QS3D.BricsCAD.V25.UI.ViewModels;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel : UserControl
    {
        private WorkspaceViewModel _viewModel = new WorkspaceViewModel();
        private IReadOnlyList<EntitySnapshot> _inspection = Array.Empty<EntitySnapshot>();
        private bool _loadingContext;
        private ElementCategory? _categoryFilter;

        public WorkspacePanel()
        {
            InitializeComponent();
            BindViewModel();
            ConfigureWorkspaceInteractions();
            AttachQuickDrawInteractions();
            AttachLayoutPersistence();
            Loaded += (_, __) => RefreshProject();
        }

        private void BindViewModel()
        {
            DataContext = _viewModel;
            var propertyView = CollectionViewSource.GetDefaultView(_viewModel.Properties);
            if (propertyView != null && propertyView.CanGroup)
                propertyView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PropertyRowViewModel.Group)));
        }

        public void ClearProject(string status)
        {
            _loadingContext = true;
            try
            {
                _inspection = Array.Empty<EntitySnapshot>();
                InspectionList.ItemsSource = _inspection;
                SelectionCount.Text = "0 chọn";
                _categoryFilter = null;
                _viewModel = new WorkspaceViewModel();
                BindViewModel();
                ZoneCombo.SelectedIndex = -1;
                FloorCombo.SelectedIndex = -1;
                FamilyList.SelectedItem = null;
                _viewModel.Status = status ?? string.Empty;
            }
            finally { _loadingContext = false; }
        }

        private void ConfigureWorkspaceInteractions()
        {
            PreviewKeyDown += OnWorkspacePreviewKeyDown;
            FamilyList.PreviewMouseRightButtonDown += OnFamilyListPreviewMouseRightButtonDown;
            InspectionList.PreviewMouseRightButtonDown += OnInspectionListPreviewMouseRightButtonDown;

            var familyMenu = CreateContextMenu();
            familyMenu.Items.Add(CreateMenuItem("Nhân bản Family", OnAddClick));
            familyMenu.Items.Add(CreateMenuItem("Xóa Family", OnDeleteClick));
            familyMenu.Items.Add(new Separator());
            familyMenu.Items.Add(CreateMenuItem("Bóc đối tượng CAD đang chọn", OnCaptureSelectedClick));
            familyMenu.Items.Add(CreateMenuItem("Vẽ / Cập nhật 3D", OnView3DClick));
            FamilyList.ContextMenu = familyMenu;

            var inspectionMenu = CreateContextMenu();
            inspectionMenu.Items.Add(CreateMenuItem("Focus", OnFocusSelectedClick));
            inspectionMenu.Items.Add(CreateMenuItem("Cô lập", OnIsolateSelectedClick));
            inspectionMenu.Items.Add(CreateMenuItem("Khôi phục cô lập", OnUnisolateClick));
            inspectionMenu.Items.Add(new Separator());
            inspectionMenu.Items.Add(CreateMenuItem("Định vị / Zoom chọn", OnLocateSelectedClick));
            inspectionMenu.Items.Add(CreateMenuItem("Mặt bằng", OnTopViewClick));
            InspectionList.ContextMenu = inspectionMenu;
        }

        private ContextMenu CreateContextMenu()
        {
            return new ContextMenu
            {
                Background = TryFindResource("Bg2Brush") as Brush,
                Foreground = TryFindResource("TextBrush") as Brush,
                BorderBrush = TryFindResource("BorderStrongBrush") as Brush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(2)
            };
        }

        private MenuItem CreateMenuItem(string header, RoutedEventHandler handler)
        {
            var item = new MenuItem
            {
                Header = header,
                Foreground = TryFindResource("TextBrush") as Brush,
                Background = Brushes.Transparent,
                Padding = new Thickness(8, 4, 12, 4)
            };
            item.Click += handler;
            return item;
        }

        private void OnWorkspacePreviewKeyDown(object sender, KeyEventArgs e)
        {
            var modifiers = Keyboard.Modifiers;
            if (modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                OnSaveClick(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }
            if (modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                FamilySearch.Focus();
                FamilySearch.SelectAll();
                e.Handled = true;
                return;
            }
            if (modifiers == ModifierKeys.Control && e.Key == Key.B)
            {
                OnQuantityClick(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }
            if (modifiers == ModifierKeys.None && e.Key == Key.F5)
            {
                OnRefreshClick(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }
            if (modifiers == ModifierKeys.None && e.Key == Key.Delete && FamilyList.IsKeyboardFocusWithin)
            {
                OnDeleteClick(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void OnFamilyListPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindContainer<ListBoxItem>(FamilyList, e.OriginalSource as DependencyObject);
            if (item != null) item.IsSelected = true;
        }

        private void OnInspectionListPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindContainer<ListViewItem>(InspectionList, e.OriginalSource as DependencyObject);
            if (item != null) item.IsSelected = true;
        }

        private static T? FindContainer<T>(ItemsControl owner, DependencyObject? source) where T : DependencyObject
        {
            if (owner == null || source == null) return null;
            var current = source;
            while (current != null && !ReferenceEquals(current, owner))
            {
                if (current is T typed) return typed;
                current = ParentOf(current);
            }
            return null;
        }

        private static DependencyObject? ParentOf(DependencyObject child)
        {
            if (child is ContentElement content)
                return ContentOperations.GetParent(content) ?? (content as FrameworkContentElement)?.Parent;
            return VisualTreeHelper.GetParent(child);
        }

        public void RefreshProject()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            _loadingContext = true;
            try
            {
                if (!ExistingProjectMutationContext.TryGet(doc, out var project))
                {
                    ClearProject("No QS3D project is available; Workspace remains read-only and non-creating.");
                    return;
                }
                _viewModel.Load(project);
                ZoneCombo.SelectedIndex = _viewModel.ActiveZoneIndex();
                FloorCombo.SelectedIndex = _viewModel.ActiveFloorIndex();
                ApplyFamilyFilter();
                var active = project.Metadata.TryGetValue("ActiveFamilyId", out var id) ? project.FindFamily(id) : null;
                FamilyList.SelectedItem = active ?? FamilyList.Items.Cast<object>().OfType<ProjectFamily>().FirstOrDefault();
            }
            catch (Exception ex)
            {
                ClearProject("Đọc Workspace lỗi: " + ex.Message);
            }
            finally { _loadingContext = false; }
        }

        public void SetStatus(string status) => _viewModel.Status = status ?? string.Empty;

        public void SetInspection(IReadOnlyList<EntitySnapshot> snapshots)
        {
            _inspection = snapshots ?? Array.Empty<EntitySnapshot>();
            InspectionList.ItemsSource = _inspection;
            SelectionCount.Text = _inspection.Count + " chọn";
            try
            {
                SyncFamilyFromSelection();
            }
            catch (Exception ex)
            {
                ClearProject("Selection sync semantic lỗi: " + ex.Message);
            }
        }

        private void SyncFamilyFromSelection()
        {
            if (_inspection.Count != 1)
            {
                _viewModel.SetSelectedElement(null);
                if (_inspection.Count > 1)
                    SetStatus("Selection gồm nhiều đối tượng CAD; inspector giữ scope Family để tránh sửa nhầm Instance.");
                return;
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                _viewModel.SetSelectedElement(null);
                return;
            }

            var handles = new HashSet<string>(_inspection.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
            if (!ProjectContextCoordinator.TryGetReadOnly(doc, out var project))
            {
                _viewModel.SetSelectedElement(null);
                SetStatus("Bản vẽ hiện tại chưa có QS3D project; selection chỉ được inspect CAD.");
                return;
            }
            var matches = project.Elements.Where(x => SemanticReferenceHandles.MatchesSelection(x, handles)).Take(2).ToList();
            if (matches.Count != 1 || string.IsNullOrWhiteSpace(matches[0].FamilyId))
            {
                _viewModel.SetSelectedElement(null);
                if (matches.Count > 1)
                    SetStatus("Selection khớp nhiều cấu kiện semantic; inspector giữ scope Family để tránh sửa nhầm Instance.");
                return;
            }

            var element = matches[0];
            var family = project.FindFamily(element.FamilyId);
            if (family == null)
            {
                _viewModel.SetSelectedElement(null);
                SetStatus("Cấu kiện semantic đang chọn không còn Family hợp lệ; inspector đã về scope Family.");
                return;
            }

            _loadingContext = true;
            try
            {
                _categoryFilter = family.Category;
                ApplyFamilyFilter();
                FamilyList.SelectedItem = family;
                FamilyList.ScrollIntoView(family);
                _viewModel.SetSelectedElement(element);
            }
            finally { _loadingContext = false; }
        }

        private void OnZoneChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingContext) return;
            try { _viewModel.SetActiveZone(ZoneCombo.SelectedItem as string); }
            catch (Exception ex) { SetStatus("Đổi Zone làm việc lỗi: " + ex.Message); }
        }

        private void OnFloorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingContext) return;
            try { _viewModel.SetActiveFloor(FloorCombo.SelectedItem as string); }
            catch (Exception ex) { SetStatus("Đổi tầng làm việc lỗi: " + ex.Message); }
        }

        private void OnModelTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(e.NewValue is TreeViewItem item)) return;
            if (item.Tag is string tag && Enum.TryParse(tag, true, out ElementCategory category))
            {
                _categoryFilter = category;
                ApplyFamilyFilter();
                SetStatus(Cad.NativeBuildCapability.Supports(category)
                    ? "Nhóm mô hình: " + item.Header
                    : "Nhóm mô hình: " + item.Header + " • " + Cad.NativeBuildCapability.UnsupportedMessage(category));
            }
            else
            {
                _categoryFilter = null;
                ApplyFamilyFilter();
            }
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active.");
                var selected = FamilyList.SelectedItem as ProjectFamily;
                var project = selected == null
                    ? ProjectContextCoordinator.GetOrCreate(doc)
                    : ExistingProjectMutationContext.Require(doc, "Nhân bản Family từ Workspace");
                var basis = selected == null ? null : project.FindFamily(selected.Id);
                if (selected != null && basis == null)
                    throw new InvalidOperationException("Family đang chọn không còn tồn tại trong project hiện tại. Hãy Refresh Workspace.");

                var category = basis?.Category ?? _categoryFilter ?? ElementCategory.Room;
                var baseName = basis?.Name ?? category.ToString();
                var n = 2;
                var name = baseName + "-" + n;
                while (project.Families.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))) name = baseName + "-" + (++n);

                var family = ExecuteAtomic(project, () =>
                {
                    ProjectFamily created;
                    if (basis != null)
                    {
                        created = ProjectFamilyService.Duplicate(project, basis.Id, Guid.NewGuid().ToString("N"), name);
                        AuditTrail.ForProject(project).Record("family.duplicate", string.Empty, basis.Id + " -> " + created.Id + " • " + created.Name + " • Workspace");
                    }
                    else
                    {
                        created = ProjectFamilyService.Create(project, Guid.NewGuid().ToString("N"), name, category);
                        AuditTrail.ForProject(project).Record("family.create", string.Empty, created.Id + " • " + created.Category + " • " + created.Name + " • Workspace");
                    }
                    ProjectFamilyActivationService.SetActive(project, created.Id);
                    return created;
                }, "Tạo/Nhân bản Family từ Workspace");

                RefreshAfterCommit(
                    () =>
                    {
                        RefreshProject();
                        FamilyList.SelectedItem = project.FindFamily(family.Id);
                    },
                    basis == null ? "Đã tạo Family “" + family.Name + "”." : "Đã nhân bản Family “" + basis.Name + "” → “" + family.Name + "”.",
                    "Workspace Family create/duplicate");
            }
            catch (Exception ex) { SetStatus("Tạo/Nhân bản Family lỗi: " + ex.Message); }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active.");
                if (!(FamilyList.SelectedItem is ProjectFamily selected)) return;
                var project = ExistingProjectMutationContext.Require(doc, "Xóa Family từ Workspace");
                var family = project.FindFamily(selected.Id)
                    ?? throw new InvalidOperationException("Family đang chọn không còn tồn tại trong project hiện tại. Hãy Refresh Workspace.");
                var used = ProjectFamilyService.ReferenceCount(project, family.Id);
                if (used > 0)
                {
                    SetStatus("Không thể xóa: Family đang được " + used + " cấu kiện sử dụng.");
                    return;
                }

                var deleted = ExecuteAtomic(project, () =>
                {
                    if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId) && string.Equals(activeId, family.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        project.Metadata.Remove("ActiveFamilyId");
                        project.Touch();
                    }
                    var removed = ProjectFamilyService.Delete(project, family.Id);
                    if (removed)
                        AuditTrail.ForProject(project).Record("family.delete", string.Empty, family.Id + " • " + family.Name + " • Workspace");
                    return removed;
                }, "Xóa Family từ Workspace");
                if (!deleted) return;

                RefreshAfterCommit(
                    RefreshProject,
                    "Đã xóa Family “" + family.Name + "”.",
                    "Workspace Family delete");
            }
            catch (Exception ex) { SetStatus("Xóa Family lỗi: " + ex.Message); }
        }

        private void OnCaptureSelectedClick(object sender, RoutedEventArgs e)
        {
            var family = FamilyList.SelectedItem as ProjectFamily;
            var category = _categoryFilter ?? family?.Category;
            if (!category.HasValue) { SetStatus("Chọn một nhóm mô hình hoặc Family trước khi bóc đối tượng CAD."); return; }
            if (family != null && family.Category == category.Value) _viewModel.SetActiveFamily(family);
            var command = CommandFor(category);
            SetStatus("Bóc từ chọn → " + category.Value);
            Send(command);
        }

        private void OnView3DClick(object sender, RoutedEventArgs e)
        {
            var family = FamilyList.SelectedItem as ProjectFamily;
            var category = _categoryFilter ?? family?.Category;
            if (!category.HasValue)
            {
                SetStatus("Chọn một nhóm mô hình hoặc Family có native builder trước khi Vẽ/Cập nhật 3D.");
                return;
            }
            if (!Cad.NativeBuildCapability.Supports(category.Value))
            {
                SetStatus(Cad.NativeBuildCapability.UnsupportedMessage(category.Value));
                return;
            }
            if (family != null && family.Category == category.Value) _viewModel.SetActiveFamily(family);
            var restoredSources = SelectInspectionSemanticSourcesForBuild();
            SetStatus("Vẽ/Cập nhật 3D: " + (family?.Name ?? category.Value.ToString()) + (restoredSources > 0 ? " • source " + restoredSources : string.Empty));
            Send("QS3DBUILD3D");
        }
        private void OnWallJunctionsClick(object sender, RoutedEventArgs e) { SetStatus("Phân tích giao tim tường L / T / X trong selection."); Send("QS3DWALLJUNCTIONS"); }
        private void OnWallSnapPreviewClick(object sender, RoutedEventArgs e) { SetStatus("Xem trước kế hoạch snap đầu mút tường; chưa sửa CAD."); Send("QS3DWALLSNAPPREVIEW"); }
        private void OnWallSnapApplyClick(object sender, RoutedEventArgs e) { SetStatus("Áp dụng wall snap từ preview còn hợp lệ."); Send("QS3DWALLSNAPAPPLY"); }
        private void OnAutoHostClick(object sender, RoutedEventArgs e) { SetStatus("Tự ghép Cửa/Lỗ đang chọn với wall host an toàn; chưa khoét solid."); Send("QS3DAUTOLINKHOSTS"); }
        private void OnViewModel3DClick(object sender, RoutedEventArgs e) => Send("QS3DVIEW3D");
        private void OnOrbitClick(object sender, RoutedEventArgs e) => Send("QS3DORBIT");
        private void OnZoomSelectionClick(object sender, RoutedEventArgs e) => Send("QS3DZOOMSELECTED");
        private void OnTopViewClick(object sender, RoutedEventArgs e) => Send("QS3DVIEWTOP");
        private void OnAddFinishClick(object sender, RoutedEventArgs e) => Send("QS3DFINISH");
        private void OnRemoveFinishClick(object sender, RoutedEventArgs e) => Send("QS3DUNTRACKFINISH");
        private void OnPickRoomClick(object sender, RoutedEventArgs e) => Send("QS3DROOM");
        private void OnQuantityClick(object sender, RoutedEventArgs e) => Send("QS3DBQ");
        private void OnHealthClick(object sender, RoutedEventArgs e) => Send("QS3DHEALTH");
        private void OnSaveClick(object sender, RoutedEventArgs e) => Send("QS3DSAVE");
        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshProject();
                PaletteCoordinator.RefreshCad();
            }
            catch (Exception ex) { SetStatus("Làm mới Workspace/CAD lỗi: " + ex.Message); }
        }
        private void OnLocateSelectedClick(object sender, RoutedEventArgs e) { var count = SelectInspection(); SetStatus("Đã chọn lại " + count + " đối tượng CAD."); if (count > 0) Send("QS3DZOOMSELECTED"); }
        private void OnFocusSelectedClick(object sender, RoutedEventArgs e) { var count = SelectInspection(); if (count <= 0) { SetStatus("Chưa có đối tượng để Focus."); return; } SetStatus("Focus " + count + " đối tượng."); Send("QS3DFOCUS"); }
        private void OnIsolateSelectedClick(object sender, RoutedEventArgs e) { var count = SelectInspection(); if (count <= 0) { SetStatus("Chưa có đối tượng để Cô lập."); return; } SetStatus("Cô lập " + count + " đối tượng."); Send("QS3DISOLATE"); }
        private void OnUnisolateClick(object sender, RoutedEventArgs e) { SetStatus("Khôi phục đối tượng đã cô lập."); Send("QS3DUNISOLATE"); }
        private void OnResetPropertyClick(object sender, RoutedEventArgs e) { if (sender is Button button && button.CommandParameter is PropertyRowViewModel row) row.ResetValue(); }
        private void OnFamilySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingContext) return;
            try
            {
                _viewModel.SetActiveFamily(FamilyList.SelectedItem as ProjectFamily);
                _viewModel.ShowFamilyProperties();
            }
            catch (Exception ex) { SetStatus("Đổi Family active lỗi: " + ex.Message); }
        }
        private void OnFamilySearchChanged(object sender, TextChangedEventArgs e) => ApplyFamilyFilter();

        private int SelectInspection()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || _inspection.Count == 0) return 0;
            return Cad.CadHandleService.Select(doc, _inspection.Select(x => x.Handle));
        }

        private int SelectInspectionSemanticSourcesForBuild()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || _inspection.Count == 0) return 0;
            var handles = new HashSet<string>(_inspection.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
            if (!ProjectContextCoordinator.TryGetReadOnly(doc, out var project)) return 0;
            var sourceHandles = project.Elements
                .Where(x => SemanticReferenceHandles.MatchesSelection(x, handles))
                .SelectMany(x => x.SourceHandles)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (sourceHandles.Count == 0) return 0;
            return Cad.CadHandleService.Select(doc, sourceHandles);
        }

        private void ApplyFamilyFilter()
        {
            var text = FamilySearch?.Text?.Trim() ?? string.Empty; var view = CollectionViewSource.GetDefaultView(FamilyList?.ItemsSource); if (view == null) return;
            view.Filter = item => item is ProjectFamily family && (!_categoryFilter.HasValue || family.Category == _categoryFilter.Value) && (text.Length == 0 || family.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 || family.Category.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0); view.Refresh();
        }

        private static string CommandFor(ElementCategory? category)
        {
            switch (category)
            {
                case ElementCategory.Grid: return "QS3DGRID";
                case ElementCategory.ArchitecturalWall: return "QS3DWALL";
                case ElementCategory.GlassWall: return "QS3DGLASSWALL";
                case ElementCategory.WallPier: return "QS3DWALLPIER";
                case ElementCategory.StructuralWall: return "QS3DSTRUCTWALL";
                case ElementCategory.Room: return "QS3DROOM";
                case ElementCategory.Door: return "QS3DDOOR";
                case ElementCategory.WallOpening: return "QS3DOPENING";
                case ElementCategory.Beam: return "QS3DBEAM";
                case ElementCategory.Slab: return "QS3DSLAB";
                case ElementCategory.Column: return "QS3DCOLUMN";
                case ElementCategory.Foundation: return "QS3DFOUNDATION";
                case ElementCategory.Stair: return "QS3DSTAIR";
                case ElementCategory.Railing: return "QS3DRAILING";
                case ElementCategory.Earthwork: return "QS3DEARTHWORK";
                case ElementCategory.FloorFinish:
                case ElementCategory.Waterproofing:
                case ElementCategory.Skirting:
                case ElementCategory.WallFinish:
                case ElementCategory.CeilingFinish:
                    return "QS3DFINISH";
                default: return "QS3DTAKEOFF";
            }
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
            try { refresh(); }
            catch (Exception refreshError)
            {
                SetStatus(successMessage + " UI sync warning: " + refreshError.Message);
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    try { doc.Editor.WriteMessage("\nQS3D " + context + " đã commit; UI sync warning: " + refreshError.Message); }
                    catch { }
                }
            }
        }

        private void Send(string command)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                SetStatus("Không có bản vẽ BricsCAD đang active.");
                return;
            }
            var normalized = (command ?? string.Empty).Trim();
            if (normalized.Length == 0)
            {
                SetStatus("Command rỗng; không gửi sang BricsCAD.");
                return;
            }
            try { document.SendStringToExecute(normalized + " ", true, false, false); }
            catch (Exception ex) { SetStatus("Không thể gửi lệnh " + normalized + ": " + ex.Message); }
        }
    }
}
