using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class StartCenterWindow : Window
    {
        private const string AllGroups = "Tất cả";
        private const string AllRecentProjects = "Tất cả";
        private const string PinnedRecentProjects = "Đã ghim";
        private const string AvailableRecentProjects = "Sẵn sàng";
        private const string MissingRecentProjects = "Thiếu file";
        private bool _initialized;

        private enum NativeDocumentAction
        {
            NewDrawing,
            OpenDrawing,
            SaveDrawing,
            SaveAsDrawing
        }

        public StartCenterWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => InitializeUi();
            Activated += (_, __) =>
            {
                if (_initialized) RefreshActiveContext(recordRecent: false);
            };
            PreviewKeyDown += OnPreviewKeyDown;
        }

        public void RefreshFromActiveDocument()
        {
            if (!_initialized) return;
            RefreshActiveContext(recordRecent: true);
            RefreshStateLists();
        }

        private void InitializeUi()
        {
            var groups = new List<string> { AllGroups };
            groups.AddRange(StartCenterCommandCatalog.Groups);
            GroupFilter.ItemsSource = groups;
            GroupFilter.SelectedIndex = 0;

            RecentProjectFilter.ItemsSource = new[]
            {
                AllRecentProjects,
                PinnedRecentProjects,
                AvailableRecentProjects,
                MissingRecentProjects
            };
            RecentProjectFilter.SelectedIndex = 0;

            _initialized = true;
            RefreshCommands();
            RefreshStateLists();
            RefreshActiveContext(recordRecent: true);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter && CommandList.IsKeyboardFocusWithin)
            {
                RunSelectedCommand();
                e.Handled = true;
            }
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (_initialized) RefreshCommands();
        }

        private void OnGroupChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initialized) RefreshCommands();
        }

        private void OnRecentProjectSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (_initialized) RefreshRecentProjectsOnly();
        }

        private void OnRecentProjectFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initialized) RefreshRecentProjectsOnly();
        }

        private void RefreshCommands()
        {
            var group = GroupFilter.SelectedItem as string ?? AllGroups;
            var results = StartCenterCommandCatalog.Search(SearchBox.Text, group);
            CommandList.ItemsSource = results;
            CommandCountText.Text = results.Count + " lệnh";
            if (results.Count > 0 && CommandList.SelectedItem == null) CommandList.SelectedIndex = 0;
        }

        private void RefreshStateLists()
        {
            var state = StartCenterUserStateStore.GetSnapshot();
            FavoriteList.ItemsSource = ResolveCommands(state.FavoriteCommands);
            RecentCommandList.ItemsSource = ResolveCommands(state.RecentCommands);
            RefreshRecentProjectsOnly(state);
        }

        private static IReadOnlyList<StartCenterCommandItem> ResolveCommands(IEnumerable<string> commands)
        {
            var result = new List<StartCenterCommandItem>();
            foreach (var command in commands ?? Enumerable.Empty<string>())
                if (StartCenterCommandCatalog.TryGet(command, out var item)) result.Add(item);
            return result.AsReadOnly();
        }

        private void RefreshActiveContext(bool recordRecent)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                DrawingNameText.Text = "Chưa có bản vẽ BricsCAD active";
                DrawingPathText.Text = string.Empty;
                ProjectNameText.Text = "Chưa có project để hiển thị";
                ProjectSummaryText.Text = "Mở hoặc tạo một DWG để bắt đầu.";
                ProjectStatusText.Text = "WAITING FOR DRAWING";
                ProjectStatusText.Foreground = FindBrush("WarningBrush");
                SaveProjectButton.IsEnabled = false;
                return;
            }

            var drawingPath = document.Name ?? string.Empty;
            DrawingNameText.Text = DrawingLabel(document);
            DrawingPathText.Text = IsSavedDrawingPath(drawingPath) ? drawingPath : "Bản vẽ chưa lưu thành DWG trên đĩa.";

            if (recordRecent && IsSavedDrawingPath(drawingPath) && StartCenterUserStateStore.RecordProject(drawingPath))
                RefreshRecentProjectsOnly();

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    ProjectNameText.Text = "Chưa có QS3D project hiện hữu";
                    ProjectSummaryText.Text = "Start Center không tự tạo ProjectState khi chỉ đọc dashboard. Dùng Family / Type hoặc workflow authoring khi cần.";
                    ProjectStatusText.Text = "READ-ONLY • NO EXISTING QSDB";
                    ProjectStatusText.Foreground = FindBrush("MutedBrush");
                    SaveProjectButton.IsEnabled = false;
                    return;
                }

                var floorName = "Chưa chọn";
                if (!string.IsNullOrWhiteSpace(project.ActiveFloorId))
                {
                    var floor = project.FindFloor(project.ActiveFloorId);
                    floorName = floor?.Name ?? "Floor reference không hợp lệ";
                }

                var familyName = "Chưa chọn";
                if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeFamilyId) && !string.IsNullOrWhiteSpace(activeFamilyId))
                {
                    var family = project.FindFamily(activeFamilyId);
                    familyName = family == null ? "Family reference không hợp lệ" : family.Name + " • " + family.Category;
                }

                var pending = ProjectContextCoordinator.HasPendingChanges(document);
                ProjectNameText.Text = project.Name;
                ProjectSummaryText.Text =
                    project.Elements.Count + " đối tượng • " +
                    project.Families.Count + " Family • " +
                    project.Floors.Count + " Level • " +
                    project.Zones.Count + " Zone" +
                    "\nTầng active: " + floorName + " • Family active: " + familyName + " • ChangeVersion " + project.ChangeVersion;
                ProjectStatusText.Text = pending ? "CÓ THAY ĐỔI QS3D CHƯA LƯU" : "QS3D PROJECT KHÔNG CÓ PENDING CHANGE";
                ProjectStatusText.Foreground = FindBrush(pending ? "WarningBrush" : "SuccessBrush");
                SaveProjectButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ProjectNameText.Text = "Không thể đọc QS3D project hiện tại";
                ProjectSummaryText.Text = ex.Message;
                ProjectStatusText.Text = "PROJECT READ FAILED • FAIL CLOSED";
                ProjectStatusText.Foreground = FindBrush("DangerBrush");
                SaveProjectButton.IsEnabled = false;
            }
        }

        private System.Windows.Media.Brush FindBrush(string key)
        {
            return TryFindResource(key) as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.White;
        }

        private void RefreshRecentProjectsOnly()
        {
            RefreshRecentProjectsOnly(StartCenterUserStateStore.GetSnapshot());
        }

        private void RefreshRecentProjectsOnly(StartCenterUserStateSnapshot state)
        {
            IEnumerable<StartCenterRecentProject> projects = state.RecentProjects;
            var query = (RecentProjectSearchBox.Text ?? string.Empty).Trim();
            if (query.Length > 0)
            {
                projects = projects.Where(x =>
                    x.DisplayName.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    x.Path.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var filter = RecentProjectFilter.SelectedItem as string ?? AllRecentProjects;
            if (string.Equals(filter, PinnedRecentProjects, StringComparison.CurrentCultureIgnoreCase))
                projects = projects.Where(x => x.IsPinned);
            else if (string.Equals(filter, AvailableRecentProjects, StringComparison.CurrentCultureIgnoreCase))
                projects = projects.Where(x => x.Exists);
            else if (string.Equals(filter, MissingRecentProjects, StringComparison.CurrentCultureIgnoreCase))
                projects = projects.Where(x => !x.Exists);

            var filtered = projects.ToList();
            RecentProjectList.ItemsSource = filtered;
            RecentProjectCountText.Text = filtered.Count + " / " + state.RecentProjects.Count;
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            RefreshActiveContext(recordRecent: true);
            RefreshCommands();
            RefreshStateLists();
            SetStatus("Đã refresh Start Center từ DWG active hiện tại.");
        }

        private void OnAllowlistedCommandClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            ExecuteAllowlistedCommand(button.Tag as string ?? string.Empty);
        }

        private void OnRunSelectedCommandClick(object sender, RoutedEventArgs e) => RunSelectedCommand();
        private void OnCommandDoubleClick(object sender, MouseButtonEventArgs e) => RunSelectedCommand();

        private void RunSelectedCommand()
        {
            if (CommandList.SelectedItem is StartCenterCommandItem item) ExecuteAllowlistedCommand(item.Command);
        }

        private void OnFavoriteDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FavoriteList.SelectedItem is StartCenterCommandItem item) ExecuteAllowlistedCommand(item.Command);
        }

        private void OnRecentCommandDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RecentCommandList.SelectedItem is StartCenterCommandItem item) ExecuteAllowlistedCommand(item.Command);
        }

        private void ExecuteAllowlistedCommand(string command)
        {
            if (!StartCenterCommandCatalog.TryGet(command, out var item))
            {
                SetStatus("Đã chặn command không nằm trong Start Center allowlist.");
                return;
            }

            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                SetStatus("Chưa có bản vẽ BricsCAD đang active.");
                return;
            }

            try
            {
                document.SendStringToExecute(item.Command + " ", true, false, false);
                StartCenterUserStateStore.RecordCommand(item.Command);
                RefreshStateLists();
                SetStatus("Đã gửi " + item.Command + " sang " + DrawingLabel(document) + ".");
            }
            catch (Exception ex)
            {
                SetStatus("Không thể gửi " + item.Command + ": " + ex.Message);
            }
        }

        private void OnToggleFavoriteClick(object sender, RoutedEventArgs e)
        {
            var item = CommandList.SelectedItem as StartCenterCommandItem ?? FavoriteList.SelectedItem as StartCenterCommandItem;
            if (item == null)
            {
                SetStatus("Chọn một command trước khi ghim.");
                return;
            }

            StartCenterUserStateStore.ToggleFavorite(item.Command);
            RefreshStateLists();
            SetStatus("Đã cập nhật Favorites cho " + item.Command + ".");
        }

        private void OnNewDrawingClick(object sender, RoutedEventArgs e) => ExecuteNativeDocumentAction(NativeDocumentAction.NewDrawing);
        private void OnOpenDrawingClick(object sender, RoutedEventArgs e) => ExecuteNativeDocumentAction(NativeDocumentAction.OpenDrawing);
        private void OnSaveDrawingClick(object sender, RoutedEventArgs e) => ExecuteNativeDocumentAction(NativeDocumentAction.SaveDrawing);
        private void OnSaveAsDrawingClick(object sender, RoutedEventArgs e) => ExecuteNativeDocumentAction(NativeDocumentAction.SaveAsDrawing);

        private void ExecuteNativeDocumentAction(NativeDocumentAction action)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                SetStatus("BricsCAD chưa có active document để nhận file action.");
                return;
            }

            string command;
            string label;
            switch (action)
            {
                case NativeDocumentAction.NewDrawing: command = "_.NEW "; label = "New"; break;
                case NativeDocumentAction.OpenDrawing: command = "_.OPEN "; label = "Open"; break;
                case NativeDocumentAction.SaveDrawing: command = "_.QSAVE "; label = "Save"; break;
                case NativeDocumentAction.SaveAsDrawing: command = "_.SAVEAS "; label = "Save As"; break;
                default: throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported Start Center document action.");
            }

            try
            {
                document.SendStringToExecute(command, true, false, false);
                SetStatus("Đã gửi BricsCAD " + label + " từ Start Center.");
            }
            catch (Exception ex)
            {
                SetStatus("BricsCAD " + label + " lỗi: " + ex.Message);
            }
        }

        private void OnRecentProjectDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelectedRecentProject();
        private void OnOpenRecentProjectClick(object sender, RoutedEventArgs e) => OpenSelectedRecentProject();

        private void OpenSelectedRecentProject()
        {
            if (!(RecentProjectList.SelectedItem is StartCenterRecentProject recent))
            {
                SetStatus("Chọn một DWG trong Recent Projects.");
                return;
            }

            if (!StartCenterUserStateStore.TryNormalizeDwgPath(recent.Path, out var normalized))
            {
                SetStatus("Recent path không còn hợp lệ.");
                return;
            }
            if (!File.Exists(normalized))
            {
                SetStatus("DWG không còn tồn tại tại đường dẫn đã lưu. Bạn có thể bỏ mục này khỏi lịch sử.");
                RefreshRecentProjectsOnly();
                return;
            }

            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                SetStatus("BricsCAD chưa có active document để nhận lệnh OPEN.");
                return;
            }

            try
            {
                document.SendStringToExecute("_.OPEN \"" + normalized + "\" ", true, false, false);
                StartCenterUserStateStore.RecordProject(normalized);
                RefreshRecentProjectsOnly();
                SetStatus("Đã gửi OPEN cho " + System.IO.Path.GetFileName(normalized) + ".");
            }
            catch (Exception ex)
            {
                SetStatus("Không thể mở recent DWG: " + ex.Message);
            }
        }

        private void OnToggleRecentProjectPinClick(object sender, RoutedEventArgs e)
        {
            if (!(RecentProjectList.SelectedItem is StartCenterRecentProject recent))
            {
                SetStatus("Chọn một Recent Project trước khi ghim.");
                return;
            }
            StartCenterUserStateStore.ToggleProjectPinned(recent.Path);
            RefreshRecentProjectsOnly();
            SetStatus("Đã cập nhật trạng thái ghim.");
        }

        private void OnRemoveRecentProjectClick(object sender, RoutedEventArgs e)
        {
            if (!(RecentProjectList.SelectedItem is StartCenterRecentProject recent))
            {
                SetStatus("Chọn một Recent Project trước khi bỏ khỏi lịch sử.");
                return;
            }
            StartCenterUserStateStore.RemoveProject(recent.Path);
            RefreshRecentProjectsOnly();
            SetStatus("Đã bỏ khỏi lịch sử Start Center; file DWG không bị xóa.");
        }

        private void OnClearRecentProjectsClick(object sender, RoutedEventArgs e)
        {
            StartCenterUserStateStore.ClearProjects();
            RefreshRecentProjectsOnly();
            SetStatus("Đã clear lịch sử Recent Projects; không file DWG nào bị xóa.");
        }

        private void SetStatus(string text) => StatusText.Text = (text ?? string.Empty).Trim();
        private static bool IsSavedDrawingPath(string path) => StartCenterUserStateStore.TryNormalizeDwgPath(path, out _);

        private static string DrawingLabel(Document document)
        {
            var name = document?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "bản vẽ chưa lưu";
            try
            {
                var file = System.IO.Path.GetFileName(name);
                return string.IsNullOrWhiteSpace(file) ? name : file;
            }
            catch { return name; }
        }
    }
}
