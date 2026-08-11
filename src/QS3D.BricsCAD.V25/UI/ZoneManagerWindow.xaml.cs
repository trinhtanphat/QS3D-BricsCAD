using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class ZoneManagerWindow : Window
    {
        private readonly Document _document;
        private ProjectState _boundProject;
        private bool _loading;
        private string _editingId = string.Empty;

        public ZoneManagerWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Loaded += (_, __) => RefreshAll();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshAll();
        private void OnZoneSelectionChanged(object sender, SelectionChangedEventArgs e) { if (_loading) return; LoadEditor(); RefreshLabels(); }
        private void OnNewClick(object sender, RoutedEventArgs e) { _editingId = string.Empty; ZoneList.SelectedItem = null; ZoneNameBox.Text = string.Empty; ZoneNameBox.Focus(); SetStatus("Tạo Zone mới."); }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("lưu Zone");
                var project = ExistingProjectMutationContext.Require(_document, "Lưu Zone");
                var rollback = ProjectStateSnapshot.Capture(project);
                ZoneDefinition zone;
                try
                {
                    if (string.IsNullOrWhiteSpace(_editingId))
                    {
                        zone = ProjectZoneService.Create(project, "zone-" + Guid.NewGuid().ToString("N"), ZoneNameBox.Text);
                        AuditTrail.ForProject(project).Record("zone.create", string.Empty, zone.Id + " • " + zone.Name);
                    }
                    else
                    {
                        var before = project.Zones.FirstOrDefault(x => string.Equals(x.Id, _editingId, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException("Zone đang chỉnh không còn tồn tại trong project hiện tại. Hãy Refresh rồi chọn lại Zone.");
                        var oldName = before.Name;
                        zone = ProjectZoneService.Update(project, before.Id, ZoneNameBox.Text);
                        if (!string.Equals(oldName, zone.Name, StringComparison.Ordinal))
                            AuditTrail.ForProject(project).Record("zone.update", string.Empty, zone.Id + " • " + oldName + " -> " + zone.Name);
                    }
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Lưu Zone");
                    throw;
                }

                _editingId = zone.Id;
                RefreshAfterCommit(
                    () => RefreshAll(zone.Id),
                    "Đã lưu Zone “" + zone.Name + "”.",
                    "Zone save");
            }
            catch (Exception ex) { SetStatus("Lưu Zone lỗi: " + ex.Message); }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("xóa Zone");
                var project = ExistingProjectMutationContext.Require(_document, "Xóa Zone");
                var zone = RequireSelectedZone(project);
                var rollback = ProjectStateSnapshot.Capture(project);
                var deleted = false;
                try
                {
                    deleted = ProjectZoneService.Delete(project, zone.Id);
                    if (deleted)
                        AuditTrail.ForProject(project).Record("zone.delete", string.Empty, zone.Id + " • " + zone.Name);
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Xóa Zone");
                    throw;
                }

                if (!deleted) return;
                _editingId = string.Empty;
                RefreshAfterCommit(
                    () => RefreshAll(),
                    "Đã xóa Zone “" + zone.Name + "”.",
                    "Zone delete");
            }
            catch (Exception ex) { SetStatus("Xóa Zone lỗi: " + ex.Message); }
        }

        private void OnActivateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("đặt Zone active");
                var project = ExistingProjectMutationContext.Require(_document, "Đặt Zone active");
                var zone = RequireSelectedZone(project);
                var previous = project.ActiveZoneId;
                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    ProjectZoneService.SetActive(project, zone.Id);
                    if (!string.Equals(previous, zone.Id, StringComparison.OrdinalIgnoreCase))
                        AuditTrail.ForProject(project).Record("zone.activate", string.Empty, previous + " -> " + zone.Id + " • " + zone.Name);
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Đặt Zone active");
                    throw;
                }

                RefreshAfterCommit(
                    () => RefreshAll(zone.Id),
                    "Zone hoạt động: " + zone.Name + ".",
                    "Zone activate");
            }
            catch (Exception ex) { SetStatus("Đặt Zone active lỗi: " + ex.Message); }
        }

        private void OnAssignClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("gán Zone cho selection");
                if (!(ZoneList.SelectedItem is ZoneDefinition selectedZone))
                    throw new InvalidOperationException("Chọn một Zone trước khi thực hiện thao tác.");
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var previewProject))
                    throw new InvalidOperationException("Gán Zone cho selection cần một QS3D project hiện hữu; thao tác này không tạo project mới.");
                var previewZone = previewProject.Zones.FirstOrDefault(x => string.Equals(x.Id, selectedZone.Id, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Zone đã chọn không còn tồn tại trong project hiện tại. Hãy Refresh và chọn lại.");
                var expectedProjectId = previewProject.ProjectId;
                var previewIds = SemanticSelectionResolver.ResolveImplied(_document, previewProject)
                    .Select(x => x.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (previewIds.Count == 0) throw new InvalidOperationException("Selection hiện tại không resolve được QS3D semantic element.");

                var project = ExistingProjectMutationContext.Require(_document, "Gán Zone cho selection");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("QS3D project đã thay đổi sau khi đọc selection. Không có Zone assignment nào được áp dụng; hãy Refresh và thử lại.");
                var zone = project.Zones.FirstOrDefault(x => string.Equals(x.Id, previewZone.Id, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Zone đã thay đổi hoặc bị xóa khỏi project hiện tại. Hãy Refresh và chọn lại.");
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project)
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();
                var currentIds = elements.Select(x => x.Id)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (!previewIds.SequenceEqual(currentIds, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Selection hoặc semantic ownership đã thay đổi trước khi gán Zone. Không có mutation nào được áp dụng; hãy chọn lại và thử lại.");

                var previous = elements
                    .Where(x => !string.Equals(x.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(x => x.Id, x => x.ZoneId, StringComparer.OrdinalIgnoreCase);

                var rollback = ProjectStateSnapshot.Capture(project);
                int changed;
                try
                {
                    changed = ProjectZoneService.Assign(project, zone.Id, elements);
                    foreach (var element in elements)
                        if (previous.TryGetValue(element.Id, out var oldZone))
                            AuditTrail.ForProject(project).Record("zone.assign", element.Id, oldZone + " -> " + zone.Id + " • semantic only; CAD source position unchanged");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Gán Zone cho selection");
                    throw;
                }

                RefreshAfterCommit(
                    () => RefreshLabels(),
                    "Đã gán “" + zone.Name + "” cho " + changed + "/" + elements.Count + " semantic element.",
                    "Zone assign");
            }
            catch (Exception ex) { SetStatus("Gán Zone lỗi: " + ex.Message); }
        }

        private void OnInspectClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("kiểm tra selection");
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                    throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Zone Manager không tạo replacement project khi chỉ kiểm tra selection.");
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project);
                SelectionCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
                var zones = elements
                    .GroupBy(x => x.ZoneId, StringComparer.OrdinalIgnoreCase)
                    .Select(x => (project.Zones.FirstOrDefault(z => string.Equals(z.Id, x.Key, StringComparison.OrdinalIgnoreCase))?.Name ?? x.Key) + ": " + x.Count())
                    .ToList();
                SetStatus(elements.Count == 0 ? "Selection chưa resolve semantic element." : "Selection: " + string.Join(" • ", zones));
            }
            catch (Exception ex) { SetStatus("Kiểm tra selection lỗi: " + ex.Message); }
        }

        private void RefreshAll(string preferredId = "")
        {
            try
            {
                var previous = string.IsNullOrWhiteSpace(preferredId) ? (ZoneList.SelectedItem as ZoneDefinition)?.Id : preferredId;
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                {
                    _boundProject = null;
                    _loading = true;
                    try { ZoneList.ItemsSource = null; ZoneList.SelectedItem = null; }
                    finally { _loading = false; }
                    _editingId = string.Empty;
                    ZoneNameBox.Text = string.Empty;
                    ActiveZoneText.Text = "—";
                    SelectedZoneText.Text = "—";
                    ReferenceCountText.Text = "0";
                    SelectionCountText.Text = "—";
                    Title = "QS3D • Zone • " + DrawingLabel(_document);
                    SetStatus("Chưa có QS3D project hiện hữu cho bản vẽ này. Zone Manager không tạo replacement project khi chỉ đọc.");
                    return;
                }

                var zones = project.Zones.OrderBy(x => x.Name).ToList();
                _loading = true;
                try
                {
                    ZoneList.ItemsSource = zones;
                    ZoneList.SelectedItem = zones.FirstOrDefault(x => string.Equals(x.Id, previous, StringComparison.OrdinalIgnoreCase))
                        ?? zones.FirstOrDefault(x => string.Equals(x.Id, project.ActiveZoneId, StringComparison.OrdinalIgnoreCase))
                        ?? zones.FirstOrDefault();
                }
                finally { _loading = false; }
                LoadEditor();
                RefreshLabels();
                _boundProject = project;
                Title = "QS3D • Zone • " + DrawingLabel(_document);
                if (zones.Count == 0)
                {
                    _editingId = string.Empty;
                    ZoneNameBox.Text = string.Empty;
                    SetStatus("Project chưa có Zone. Dùng Mới → nhập tên → Lưu.");
                }
            }
            catch (Exception ex) { SetStatus("Đọc Zone lỗi: " + ex.Message); }
        }

        private ZoneDefinition RequireSelectedZone(ProjectState project)
        {
            if (!(ZoneList.SelectedItem is ZoneDefinition selected))
                throw new InvalidOperationException("Chọn một Zone trước khi thực hiện thao tác.");
            return project.Zones.FirstOrDefault(x => string.Equals(x.Id, selected.Id, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Zone đã chọn không còn tồn tại trong project hiện tại. Hãy Refresh và chọn lại.");
        }

        private void LoadEditor()
        {
            if (!(ZoneList.SelectedItem is ZoneDefinition zone))
            {
                _editingId = string.Empty;
                ZoneNameBox.Text = string.Empty;
                return;
            }
            _editingId = zone.Id;
            ZoneNameBox.Text = zone.Name;
        }

        private void RefreshLabels()
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
            {
                ActiveZoneText.Text = "—";
                SelectedZoneText.Text = "—";
                ReferenceCountText.Text = "0";
                SelectionCountText.Text = "—";
                return;
            }

            var active = project.Zones.FirstOrDefault(x => string.Equals(x.Id, project.ActiveZoneId, StringComparison.OrdinalIgnoreCase));
            var selectedId = (ZoneList.SelectedItem as ZoneDefinition)?.Id;
            var selected = project.Zones.FirstOrDefault(x => string.Equals(x.Id, selectedId, StringComparison.OrdinalIgnoreCase));
            ActiveZoneText.Text = active?.Name ?? "—";
            SelectedZoneText.Text = selected?.Name ?? "—";
            ReferenceCountText.Text = selected == null ? "0" : ProjectZoneService.ReferenceCount(project, selected.Id).ToString(CultureInfo.InvariantCulture);
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
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
            catch (Exception refreshError)
            {
                var warning = successMessage + " UI sync warning: " + refreshError.Message;
                try { StatusText.Text = warning; } catch { }
                try { PaletteCoordinator.SetStatus(warning); } catch { }
                try { _document.Editor.WriteMessage("\nQS3D " + context + " đã commit; UI sync warning: " + refreshError.Message); } catch { }
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

        private void EnsureActive(string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Zone Manager trước khi " + operation + ".");
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var currentProject) ||
                _boundProject == null ||
                !ReferenceEquals(currentProject, _boundProject))
                throw new InvalidOperationException("QS3D project đã thay đổi từ lần Refresh gần nhất. Hãy Refresh Zone Manager trước khi " + operation + ".");
        }

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu";
            try { return System.IO.Path.GetFileName(name); }
            catch { return name; }
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
            try { PaletteCoordinator.SetStatus(StatusText.Text); } catch { }
        }
    }
}
