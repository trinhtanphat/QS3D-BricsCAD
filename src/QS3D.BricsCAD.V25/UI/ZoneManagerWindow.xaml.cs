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

namespace QS3D.BricsCAD.V25.UI
{
    public partial class ZoneManagerWindow : Window
    {
        private readonly Document _document;
        private bool _loading;
        private string _editingId = string.Empty;

        public ZoneManagerWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
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
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                ZoneDefinition zone;
                if (string.IsNullOrWhiteSpace(_editingId))
                {
                    zone = ProjectZoneService.Create(project, "zone-" + Guid.NewGuid().ToString("N"), ZoneNameBox.Text);
                    AuditTrail.ForProject(project).Record("zone.create", string.Empty, zone.Id + " • " + zone.Name);
                }
                else
                {
                    var before = project.Zones.FirstOrDefault(x => string.Equals(x.Id, _editingId, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException("Zone đang chỉnh không còn tồn tại.");
                    var oldName = before.Name;
                    zone = ProjectZoneService.Update(project, before.Id, ZoneNameBox.Text);
                    if (!string.Equals(oldName, zone.Name, StringComparison.Ordinal)) AuditTrail.ForProject(project).Record("zone.update", string.Empty, zone.Id + " • " + oldName + " -> " + zone.Name);
                }
                PaletteCoordinator.RefreshProject(); RefreshAll(zone.Id); SetStatus("Đã lưu Zone “" + zone.Name + "”.");
            }
            catch (Exception ex) { SetStatus("Lưu Zone lỗi: " + ex.Message); }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("xóa Zone");
                if (!(ZoneList.SelectedItem is ZoneDefinition zone)) throw new InvalidOperationException("Chọn một Zone trước khi xóa.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                if (!ProjectZoneService.Delete(project, zone.Id)) return;
                AuditTrail.ForProject(project).Record("zone.delete", string.Empty, zone.Id + " • " + zone.Name);
                _editingId = string.Empty; PaletteCoordinator.RefreshProject(); RefreshAll(); SetStatus("Đã xóa Zone “" + zone.Name + "”.");
            }
            catch (Exception ex) { SetStatus("Xóa Zone lỗi: " + ex.Message); }
        }

        private void OnActivateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("đặt Zone active");
                if (!(ZoneList.SelectedItem is ZoneDefinition zone)) throw new InvalidOperationException("Chọn một Zone trước khi đặt active.");
                var project = ProjectContextCoordinator.GetOrCreate(_document); var previous = project.ActiveZoneId;
                ProjectZoneService.SetActive(project, zone.Id);
                if (!string.Equals(previous, zone.Id, StringComparison.OrdinalIgnoreCase)) AuditTrail.ForProject(project).Record("zone.activate", string.Empty, previous + " -> " + zone.Id + " • " + zone.Name);
                PaletteCoordinator.RefreshProject(); RefreshLabels(); SetStatus("Zone hoạt động: " + zone.Name + ".");
            }
            catch (Exception ex) { SetStatus("Đặt Zone active lỗi: " + ex.Message); }
        }

        private void OnAssignClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("gán Zone cho selection");
                if (!(ZoneList.SelectedItem is ZoneDefinition zone)) throw new InvalidOperationException("Chọn một Zone trước khi gán.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project).ToList();
                if (elements.Count == 0) throw new InvalidOperationException("Selection hiện tại không resolve được QS3D semantic element.");
                var previous = elements.Where(x => !string.Equals(x.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase)).ToDictionary(x => x.Id, x => x.ZoneId, StringComparer.OrdinalIgnoreCase);
                var changed = ProjectZoneService.Assign(project, zone.Id, elements);
                foreach (var element in elements) if (previous.TryGetValue(element.Id, out var oldZone)) AuditTrail.ForProject(project).Record("zone.assign", element.Id, oldZone + " -> " + zone.Id + " • semantic only; CAD source position unchanged");
                PaletteCoordinator.RefreshProject(); RefreshLabels(); SetStatus("Đã gán “" + zone.Name + "” cho " + changed + "/" + elements.Count + " semantic element.");
            }
            catch (Exception ex) { SetStatus("Gán Zone lỗi: " + ex.Message); }
        }

        private void OnInspectClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("kiểm tra selection");
                var project = ProjectContextCoordinator.GetOrCreate(_document); var elements = SemanticSelectionResolver.ResolveImplied(_document, project);
                SelectionCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
                var zones = elements.GroupBy(x => x.ZoneId, StringComparer.OrdinalIgnoreCase).Select(x => (project.Zones.FirstOrDefault(z => string.Equals(z.Id, x.Key, StringComparison.OrdinalIgnoreCase))?.Name ?? x.Key) + ": " + x.Count()).ToList();
                SetStatus(elements.Count == 0 ? "Selection chưa resolve semantic element." : "Selection: " + string.Join(" • ", zones));
            }
            catch (Exception ex) { SetStatus("Kiểm tra selection lỗi: " + ex.Message); }
        }

        private void RefreshAll(string preferredId = "")
        {
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(_document); var previous = string.IsNullOrWhiteSpace(preferredId) ? (ZoneList.SelectedItem as ZoneDefinition)?.Id : preferredId;
                var zones = project.Zones.OrderBy(x => x.Name).ToList(); _loading = true;
                try { ZoneList.ItemsSource = zones; ZoneList.SelectedItem = zones.FirstOrDefault(x => string.Equals(x.Id, previous, StringComparison.OrdinalIgnoreCase)) ?? zones.FirstOrDefault(x => string.Equals(x.Id, project.ActiveZoneId, StringComparison.OrdinalIgnoreCase)) ?? zones.FirstOrDefault(); }
                finally { _loading = false; }
                LoadEditor(); RefreshLabels(); Title = "QS3D • Zone • " + DrawingLabel(_document);
                if (zones.Count == 0) { _editingId = string.Empty; ZoneNameBox.Text = string.Empty; SetStatus("Project chưa có Zone. Dùng Mới → nhập tên → Lưu."); }
            }
            catch (Exception ex) { SetStatus("Đọc Zone lỗi: " + ex.Message); }
        }

        private void LoadEditor() { if (!(ZoneList.SelectedItem is ZoneDefinition zone)) return; _editingId = zone.Id; ZoneNameBox.Text = zone.Name; }
        private void RefreshLabels()
        {
            var project = ProjectContextCoordinator.GetOrCreate(_document); var active = project.Zones.FirstOrDefault(x => string.Equals(x.Id, project.ActiveZoneId, StringComparison.OrdinalIgnoreCase)); var selected = ZoneList.SelectedItem as ZoneDefinition;
            ActiveZoneText.Text = active?.Name ?? "—"; SelectedZoneText.Text = selected?.Name ?? "—"; ReferenceCountText.Text = selected == null ? "0" : ProjectZoneService.ReferenceCount(project, selected.Id).ToString(CultureInfo.InvariantCulture);
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)) { SelectionCountText.Text = "—"; return; }
            try { SelectionCountText.Text = SemanticSelectionResolver.ResolveImplied(_document, project).Count.ToString(CultureInfo.InvariantCulture); } catch { SelectionCountText.Text = "!"; }
        }
        private void EnsureActive(string operation) { if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)) throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Zone Manager trước khi " + operation + "."); }
        private static string DrawingLabel(Document document) { var name = document.Name ?? string.Empty; if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu"; try { return System.IO.Path.GetFileName(name); } catch { return name; } }
        private void SetStatus(string text) { StatusText.Text = text ?? string.Empty; PaletteCoordinator.SetStatus(StatusText.Text); }
    }
}
