using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow : Window
    {
        private enum VerticalLevelMutation
        {
            AssignBottom,
            AssignTop,
            Clear
        }

        private readonly Document _document;
        private ProjectState? _boundProject;
        private string _editingFloorId = string.Empty;
        private bool _loading;

        public FloorLevelWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Loaded += (_, __) => RefreshAll();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshAll();

        private void OnFloorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            LoadEditorFromSelection();
            RefreshLabels();
        }

        private void OnNewFloorClick(object sender, RoutedEventArgs e)
        {
            _editingFloorId = string.Empty;
            _loading = true;
            try { FloorList.SelectedItem = null; }
            finally { _loading = false; }
            FloorNameBox.Text = string.Empty;
            var nextElevation = ProjectContextCoordinator.TryGetReadOnly(_document, out var project) && project.Floors.Count > 0
                ? project.Floors.Max(x => x.ElevationM) + 3.6d
                : 0d;
            FloorElevationBox.Text = nextElevation.ToString("0.###", CultureInfo.InvariantCulture);
            ReferenceCountText.Text = "0";
            FloorNameBox.Focus();
            SetStatus("Tạo tầng mới. Nhập tên và cao độ rồi bấm Lưu.");
        }

        private void OnSaveFloorClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var project = RequireBoundProjectForMutation("lưu tầng", "Lưu Floor/Level");
                var name = (FloorNameBox.Text ?? string.Empty).Trim();
                var elevation = ParseElevation(FloorElevationBox.Text);
                var rollback = ProjectStateSnapshot.Capture(project);
                FloorDefinition floor;
                try
                {
                    if (string.IsNullOrWhiteSpace(_editingFloorId))
                    {
                        floor = ProjectFloorService.Create(project, "floor-" + Guid.NewGuid().ToString("N"), name, elevation);
                        AuditTrail.ForProject(project).Record("floor.create", string.Empty, floor.Id + " • " + floor.Name + " • " + floor.ElevationM.ToString("R", CultureInfo.InvariantCulture) + "m");
                    }
                    else
                    {
                        var existing = project.Floors.FirstOrDefault(x => string.Equals(x.Id, _editingFloorId, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException("Tầng đang chỉnh không còn tồn tại trong project hiện tại. Hãy Refresh rồi chọn lại tầng.");
                        var before = existing.Name + "@" + existing.ElevationM.ToString("R", CultureInfo.InvariantCulture);
                        floor = ProjectFloorService.Update(project, existing.Id, name, elevation);
                        var after = floor.Name + "@" + floor.ElevationM.ToString("R", CultureInfo.InvariantCulture);
                        if (!string.Equals(before, after, StringComparison.Ordinal))
                            AuditTrail.ForProject(project).Record("floor.update", string.Empty, floor.Id + " • " + before + " -> " + after);
                    }
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Lưu Floor/Level");
                    throw;
                }

                _editingFloorId = floor.Id;
                RefreshAfterCommit(
                    () => RefreshAll(floor.Id),
                    "Đã lưu tầng “" + floor.Name + "” • " + floor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m.",
                    "Floor/Level save");
            }
            catch (Exception) { ReportFailure("Lưu tầng"); }
        }

        private void OnDeleteFloorClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var project = RequireBoundProjectForMutation("xóa tầng", "Xóa Floor/Level");
                var floor = RequireSelectedFloor(project);
                var rollback = ProjectStateSnapshot.Capture(project);
                var deleted = false;
                try
                {
                    deleted = ProjectFloorService.Delete(project, floor.Id);
                    if (deleted)
                        AuditTrail.ForProject(project).Record("floor.delete", string.Empty, floor.Id + " • " + floor.Name);
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Xóa Floor/Level");
                    throw;
                }

                if (!deleted) return;
                _editingFloorId = string.Empty;
                RefreshAfterCommit(
                    () => RefreshAll(),
                    "Đã xóa tầng “" + floor.Name + "”.",
                    "Floor/Level delete");
            }
            catch (Exception) { ReportFailure("Xóa tầng"); }
        }

        private void OnActivateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var project = RequireBoundProjectForMutation("đặt tầng hoạt động", "Đặt Floor/Level active");
                var floor = RequireSelectedFloor(project);
                var previous = project.ActiveFloorId;
                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    var beforeVersion = project.ChangeVersion;
                    ProjectFloorService.SetActive(project, floor.Id);
                    if (project.ChangeVersion != beforeVersion)
                        AuditTrail.ForProject(project).Record("floor.activate", string.Empty, previous + " -> " + floor.Id + " • " + floor.Name + " • " + floor.ElevationM.ToString("R", CultureInfo.InvariantCulture) + "m");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Đặt Floor/Level active");
                    throw;
                }

                RefreshAfterCommit(
                    () => RefreshAll(floor.Id),
                    "Tầng hoạt động: " + floor.Name + " • " + floor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m.",
                    "Floor/Level activate");
            }
            catch (Exception) { ReportFailure("Đặt tầng active"); }
        }

        private void OnAssignClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var previewProject = RequireBoundProjectForRead("gán tầng cho selection");
                if (!(FloorList.SelectedItem is FloorDefinition selectedFloor))
                    throw new InvalidOperationException("Chọn một tầng trước khi thực hiện thao tác.");
                var previewFloor = previewProject.Floors.FirstOrDefault(x => string.Equals(x.Id, selectedFloor.Id, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Tầng đã chọn không còn tồn tại trong project hiện tại. Hãy Refresh và chọn lại.");
                var expectedProjectId = previewProject.ProjectId;
                var previewIds = SemanticSelectionResolver.ResolveImplied(_document, previewProject)
                    .Select(x => x.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (previewIds.Count == 0) throw new InvalidOperationException("Selection hiện tại không resolve được QS3D semantic element.");

                var project = ExistingProjectMutationContext.Require(_document, "Gán Floor/Level cho selection");
                if (!ReferenceEquals(project, _boundProject) || !ReferenceEquals(project, previewProject) ||
                    !string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("QS3D project đã thay đổi sau khi đọc selection. Không có Floor/Level assignment nào được áp dụng; hãy Refresh và thử lại.");
                var floor = project.Floors.FirstOrDefault(x => string.Equals(x.Id, previewFloor.Id, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Tầng đã thay đổi hoặc bị xóa khỏi project hiện tại. Hãy Refresh và chọn lại.");
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project)
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();
                var currentIds = elements.Select(x => x.Id)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (!previewIds.SequenceEqual(currentIds, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Selection hoặc semantic ownership đã thay đổi trước khi gán Floor/Level. Không có mutation nào được áp dụng; hãy chọn lại và thử lại.");

                var previous = elements.ToDictionary(
                    element => element.Id,
                    element => element.FloorId,
                    StringComparer.OrdinalIgnoreCase);
                var rollback = ProjectStateSnapshot.Capture(project);
                int changed;
                try
                {
                    changed = ProjectFloorService.Assign(project, floor.Id, elements);
                    foreach (var element in elements)
                        if (previous.TryGetValue(element.Id, out var oldFloor) &&
                            !string.Equals(oldFloor, element.FloorId, StringComparison.Ordinal))
                            AuditTrail.ForProject(project).Record("floor.assign", element.Id, oldFloor + " -> " + floor.Id + " • semantic only; CAD source position unchanged");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Gán Floor/Level cho selection");
                    throw;
                }

                RefreshAfterCommit(
                    () =>
                    {
                        SelectionCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
                        RefreshLabels();
                    },
                    "Đã gán “" + floor.Name + "” cho " + changed + "/" + elements.Count + " semantic element. Generated output liên quan đã stale; CAD source không bị Move.",
                    "Floor/Level assign");
            }
            catch (Exception) { ReportFailure("Gán tầng"); }
        }

        private void OnAssignBottomLevelClick(object sender, RoutedEventArgs e) =>
            ApplyVerticalLevelMutation(VerticalLevelMutation.AssignBottom);

        private void OnAssignTopLevelClick(object sender, RoutedEventArgs e) =>
            ApplyVerticalLevelMutation(VerticalLevelMutation.AssignTop);

        private void OnClearVerticalLevelsClick(object sender, RoutedEventArgs e) =>
            ApplyVerticalLevelMutation(VerticalLevelMutation.Clear);

        private void ApplyVerticalLevelMutation(VerticalLevelMutation mutation)
        {
            var operation = mutation == VerticalLevelMutation.AssignBottom
                ? "Gán Level đáy"
                : mutation == VerticalLevelMutation.AssignTop ? "Gán Level đỉnh" : "Xóa Level đứng";
            try
            {
                var previewProject = RequireBoundProjectForRead(operation.ToLowerInvariant());
                var selectedFloorId = string.Empty;
                if (mutation != VerticalLevelMutation.Clear)
                {
                    if (!(FloorList.SelectedItem is FloorDefinition selectedFloor))
                        throw new InvalidOperationException("Chọn một Level trước khi gán.");
                    selectedFloorId = selectedFloor.Id;
                }

                if (selectedFloorId.Length > 0 && previewProject.FindFloor(selectedFloorId) == null)
                    throw new InvalidOperationException("Level đã chọn không còn tồn tại. Hãy Refresh và chọn lại.");
                var expectedProjectId = previewProject.ProjectId;
                var previewIds = SemanticSelectionResolver.ResolveImplied(_document, previewProject)
                    .Select(x => x.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (previewIds.Count == 0)
                    throw new InvalidOperationException("Selection hiện tại không resolve được QS3D semantic element.");

                var project = ExistingProjectMutationContext.Require(_document, operation);
                if (!ReferenceEquals(project, _boundProject) || !ReferenceEquals(project, previewProject) ||
                    !string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("QS3D project đã thay đổi sau khi đọc selection; không có Level mutation nào được áp dụng.");
                var floor = selectedFloorId.Length == 0
                    ? null
                    : project.FindFloor(selectedFloorId) ?? throw new InvalidOperationException("Level đã chọn vừa bị xóa hoặc thay đổi.");
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project)
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var currentIds = elements.Select(x => x.Id).ToList();
                if (!previewIds.SequenceEqual(currentIds, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Selection hoặc semantic ownership đã thay đổi; hãy chọn lại và thử lại.");
                if (mutation != VerticalLevelMutation.Clear)
                {
                    foreach (var element in elements)
                        LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, operation);
                }

                var before = elements.Select(x => new { Element = x, Token = VerticalLevelToken(x) }).ToList();
                var rollback = ProjectStateSnapshot.Capture(project);
                int changed;
                try
                {
                    switch (mutation)
                    {
                        case VerticalLevelMutation.AssignBottom:
                            changed = ProjectFloorService.AssignBottomLevel(project, floor!.Id, elements);
                            break;
                        case VerticalLevelMutation.AssignTop:
                            changed = ProjectFloorService.AssignTopLevel(project, floor!.Id, elements);
                            break;
                        default:
                            changed = ProjectFloorService.ClearVerticalLevels(project, elements);
                            break;
                    }

                    foreach (var item in before.Where(x => !string.Equals(x.Token, VerticalLevelToken(x.Element), StringComparison.Ordinal)))
                        AuditTrail.ForProject(project).Record(
                            mutation == VerticalLevelMutation.AssignBottom ? "level.bottom.assign" :
                            mutation == VerticalLevelMutation.AssignTop ? "level.top.assign" : "level.vertical.clear",
                            item.Element.Id,
                            item.Token + " -> " + VerticalLevelToken(item.Element) + " • source CAD unchanged; native outputs stale");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, operation);
                    throw;
                }

                var levelLabel = floor == null
                    ? "legacy source-relative placement"
                    : floor.Name + " • " + floor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
                RefreshAfterCommit(
                    () =>
                    {
                        SelectionCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
                        RefreshLabels();
                    },
                    operation + ": " + changed + "/" + elements.Count + " element • " + levelLabel + ". Rebuild output stale; source CAD không bị Move.",
                    operation);
            }
            catch (Exception) { ReportFailure(operation); }
        }

        private void OnInspectSelectionClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureBoundDrawingIsActive("kiểm tra selection");
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                    throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Level Picker không tạo replacement project khi chỉ kiểm tra selection.");
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project).ToList();
                SelectionCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
                var floors = elements
                    .GroupBy(x => x.FloorId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .Select(group => FloorLabel(project, group.Key) + ": " + group.Count())
                    .ToList();
                SetStatus(elements.Count == 0 ? "Selection chưa resolve được semantic element." : "Selection: " + string.Join(" • ", floors));
            }
            catch (Exception) { ReportFailure("Kiểm tra selection"); }
        }

        private void RefreshAll(string preferredFloorId = "")
        {
            try
            {
                var previous = string.IsNullOrWhiteSpace(preferredFloorId)
                    ? (FloorList.SelectedItem as FloorDefinition)?.Id
                    : preferredFloorId;
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                {
                    _boundProject = null;
                    _loading = true;
                    try { FloorList.ItemsSource = null; FloorList.SelectedItem = null; }
                    finally { _loading = false; }
                    _editingFloorId = string.Empty;
                    FloorNameBox.Text = string.Empty;
                    FloorElevationBox.Text = "0";
                    ActiveFloorText.Text = "—";
                    SelectedFloorText.Text = "—";
                    ReferenceCountText.Text = "0";
                    SelectionCountText.Text = "—";
                    Title = "QS3D • Level Picker • " + DrawingLabel(_document);
                    SetStatus("Chưa có QS3D project hiện hữu cho bản vẽ này. Level Picker không tạo replacement project khi chỉ đọc.");
                    return;
                }

                var floors = project.Floors
                    .OrderBy(x => x.ElevationM)
                    .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                _loading = true;
                try
                {
                    FloorList.ItemsSource = floors;
                    FloorList.SelectedItem = floors.FirstOrDefault(x => string.Equals(x.Id, previous, StringComparison.OrdinalIgnoreCase))
                        ?? floors.FirstOrDefault(x => string.Equals(x.Id, project.ActiveFloorId, StringComparison.OrdinalIgnoreCase))
                        ?? floors.FirstOrDefault();
                }
                finally { _loading = false; }
                LoadEditorFromSelection();
                RefreshLabels();
                _boundProject = project;
                Title = "QS3D • Level Picker • " + DrawingLabel(_document);
                if (floors.Count == 0)
                {
                    _editingFloorId = string.Empty;
                    FloorNameBox.Text = string.Empty;
                    FloorElevationBox.Text = "0";
                    SetStatus("Project chưa có tầng. Dùng Mới → nhập tên/cao độ → Lưu.");
                }
            }
            catch (Exception)
            {
                _boundProject = null;
                ReportFailure("Đọc Floor/Level");
            }
        }

        private FloorDefinition RequireSelectedFloor(ProjectState project)
        {
            if (!(FloorList.SelectedItem is FloorDefinition selected))
                throw new InvalidOperationException("Chọn một tầng trước khi thực hiện thao tác.");
            return project.Floors.FirstOrDefault(x => string.Equals(x.Id, selected.Id, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Tầng đã chọn không còn tồn tại trong project hiện tại. Hãy Refresh và chọn lại.");
        }

        private void LoadEditorFromSelection()
        {
            if (!(FloorList.SelectedItem is FloorDefinition floor))
            {
                _editingFloorId = string.Empty;
                FloorNameBox.Text = string.Empty;
                FloorElevationBox.Text = string.Empty;
                return;
            }
            _editingFloorId = floor.Id;
            FloorNameBox.Text = floor.Name;
            FloorElevationBox.Text = floor.ElevationM.ToString("R", CultureInfo.InvariantCulture);
        }

        private void RefreshLabels()
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
            {
                ActiveFloorText.Text = "—";
                SelectedFloorText.Text = "—";
                ReferenceCountText.Text = "0";
                SelectionCountText.Text = "—";
                return;
            }

            var active = project.Floors.FirstOrDefault(x => string.Equals(x.Id, project.ActiveFloorId, StringComparison.OrdinalIgnoreCase));
            var selectedId = (FloorList.SelectedItem as FloorDefinition)?.Id;
            var selected = project.Floors.FirstOrDefault(x => string.Equals(x.Id, selectedId, StringComparison.OrdinalIgnoreCase));
            ActiveFloorText.Text = active == null ? "—" : active.Name + " • " + active.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            SelectedFloorText.Text = selected == null ? "—" : selected.Name + " • " + selected.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            ReferenceCountText.Text = selected == null
                ? "0"
                : ProjectFloorService.ReferenceCount(project, selected.Id).ToString(CultureInfo.InvariantCulture);
            if (!ReferenceEquals(Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument, _document))
            {
                SelectionCountText.Text = "—";
                return;
            }
            try { SelectionCountText.Text = SemanticSelectionResolver.ResolveImplied(_document, project).Count.ToString(CultureInfo.InvariantCulture); }
            catch { SelectionCountText.Text = "!"; }
        }

        private void RefreshAfterCommit(Action refresh, string successMessage, string context)
        {
            SetStatus(successMessage);
            try
            {
                refresh();
                PaletteCoordinator.RefreshProject();
            }
            catch (Exception)
            {
                ReportPostCommitWarning(successMessage, context);
            }
        }

        private static void RestoreOrThrow(ProjectState project, ProjectStateSnapshot rollback, Exception operationError, string operation)
        {
            try
            {
                rollback.Restore(project);
            }
            catch (Exception restoreError)
            {
                throw new InvalidOperationException(
                    operation + " thất bại và rollback project cũng không hoàn tất.",
                    new AggregateException(operationError, restoreError));
            }
        }

        private ProjectState RequireBoundProjectForRead(string operation)
        {
            EnsureBoundDrawingIsActive(operation);
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var currentProject) ||
                _boundProject == null ||
                !ReferenceEquals(currentProject, _boundProject))
                throw new InvalidOperationException("QS3D project đã thay đổi từ lần Refresh gần nhất. Hãy Refresh Level Picker trước khi " + operation + ".");
            return currentProject;
        }

        private ProjectState RequireBoundProjectForMutation(string operation, string mutationContext)
        {
            var currentProject = RequireBoundProjectForRead(operation);
            var project = ExistingProjectMutationContext.Require(_document, mutationContext);
            if (!ReferenceEquals(project, currentProject) || !ReferenceEquals(project, _boundProject))
                throw new InvalidOperationException("QS3D project đã thay đổi trước khi " + operation + ". Không có thay đổi nào được áp dụng; hãy Refresh Level Picker và thử lại.");
            return project;
        }

        private void EnsureBoundDrawingIsActive(string operation)
        {
            if (!ReferenceEquals(Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Level Picker trước khi " + operation + ".");
        }

        private static double ParseElevation(string raw)
        {
            var text = (raw ?? string.Empty).Trim();
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                throw new InvalidOperationException("Cao độ không phải số hợp lệ.");
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Cao độ phải hữu hạn.");
            return value;
        }

        private static string VerticalLevelToken(ProjectElement element)
        {
            string Value(string key) => element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;
            return "bottom=" + Value(ProjectFloorService.BottomLevelIdKey) + "@" + Value(ProjectFloorService.BottomLevelOffsetKey) +
                   ";top=" + Value(ProjectFloorService.TopLevelIdKey) + "@" + Value(ProjectFloorService.TopLevelOffsetKey);
        }

        private static string FloorLabel(ProjectState project, string floorId)
        {
            var floor = project.Floors.FirstOrDefault(x => string.Equals(x.Id, floorId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            return floor == null ? (string.IsNullOrWhiteSpace(floorId) ? "Chưa gán" : floorId) : floor.Name;
        }

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu";
            try { return System.IO.Path.GetFileName(name); }
            catch { return name; }
        }

        private void ReportFailure(string operation)
        {
            SetStatus(
                operation + " không hoàn tất. Không có thay đổi chưa xác nhận nào được giữ lại; hãy Refresh Level Picker và thử lại.");
        }

        private void ReportPostCommitWarning(string successMessage, string context)
        {
            var warning = successMessage + " " + context + " đã commit; đồng bộ UI chưa hoàn tất. Hãy Refresh Level Picker.";
            try { StatusText.Text = warning; } catch { }
            try { PaletteCoordinator.SetStatus(warning); } catch { }
            try { _document.Editor.WriteMessage("\nQS3D " + warning); } catch { }
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
            try { PaletteCoordinator.SetStatus(StatusText.Text); } catch { }
        }
    }
}
