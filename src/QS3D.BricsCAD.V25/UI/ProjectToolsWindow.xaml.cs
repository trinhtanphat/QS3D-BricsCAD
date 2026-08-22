using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class ProjectToolsWindow : Window
    {
        private readonly Document _document;

        public ProjectToolsWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Loaded += (_, __) => RefreshSnapshot();
            Activated += (_, __) => RefreshSnapshot();
        }

        private void OnCommandClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is string command) || string.IsNullOrWhiteSpace(command)) return;
            var normalizedCommand = command.Trim();
            try
            {
                EnsureBoundDrawingIsActive("chạy " + normalizedCommand);
                _document.SendStringToExecute(normalizedCommand + " ", true, false, false);
                SetStatus("Đã gửi lệnh " + normalizedCommand + " sang “" + DrawingLabel(_document) + "”.");
            }
            catch (Exception ex) { SetStatus("Project Tools: " + ex.Message); }
        }

        private void RefreshSnapshot()
        {
            try
            {
                Title = "QS3D • Thiết lập dự án • " + DrawingLabel(_document);
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                {
                    ClearProjectSnapshot();
                    try { UnitText.Text = CadUnitService.Describe(_document); UnitText.ToolTip = null; }
                    catch (Exception unitError) { UnitText.Text = "BLOCKED"; UnitText.ToolTip = unitError.Message; }
                    ReadinessText.Text = "Chưa có QS3D project hiện hữu. Snapshot giữ nguyên read-only và không bootstrap project mới.";
                    ReadinessBadgeText.Text = "NO PROJECT";
                    SetStatus("Chưa có QS3D project hiện hữu cho bản vẽ này. Project Tools chỉ hiển thị snapshot và không tạo replacement project khi mở/refresh.");
                    return;
                }

                ProjectNameText.Text = string.IsNullOrWhiteSpace(project.Name) ? project.ProjectId : project.Name;

                var activeZone = project.Zones.FirstOrDefault(x =>
                    x != null && string.Equals(x.Id, project.ActiveZoneId, StringComparison.OrdinalIgnoreCase));
                ZoneText.Text = activeZone == null
                    ? (string.IsNullOrWhiteSpace(project.ActiveZoneId) ? "—" : project.ActiveZoneId + " • thiếu định nghĩa")
                    : activeZone.Name;

                var activeFloor = project.Floors.FirstOrDefault(x =>
                    x != null && string.Equals(x.Id, project.ActiveFloorId, StringComparison.OrdinalIgnoreCase));
                FloorText.Text = activeFloor == null
                    ? (string.IsNullOrWhiteSpace(project.ActiveFloorId) ? "—" : project.ActiveFloorId + " • thiếu định nghĩa")
                    : activeFloor.Name + " • " + activeFloor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";

                ZoneCountText.Text = project.Zones.Count.ToString(CultureInfo.InvariantCulture);
                FloorCountText.Text = project.Floors.Count.ToString(CultureInfo.InvariantCulture);
                FamilyCountText.Text = project.Families.Count.ToString(CultureInfo.InvariantCulture);
                ElementCountText.Text = project.Elements.Count.ToString(CultureInfo.InvariantCulture);

                var dirtyCount = project.Elements.Count(x => x != null && x.Dirty != ElementDirtyFlags.None);
                var geometryDirtyCount = project.Elements.Count(x => x != null && (x.Dirty & ElementDirtyFlags.Geometry) != 0);
                var quantityDirtyCount = project.Elements.Count(x => x != null && (x.Dirty & ElementDirtyFlags.Quantity) != 0);
                DirtyCountText.Text = dirtyCount.ToString(CultureInfo.InvariantCulture);
                GeometryDirtyCountText.Text = geometryDirtyCount.ToString(CultureInfo.InvariantCulture);
                QuantityDirtyCountText.Text = quantityDirtyCount.ToString(CultureInfo.InvariantCulture);
                ChangeVersionText.Text = project.ChangeVersion.ToString(CultureInfo.InvariantCulture);
                UpdatedText.Text = project.UpdatedUtc.ToUniversalTime().ToString("dd/MM/yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture);
                ReadinessBadgeText.Text = dirtyCount == 0 ? "CLEAN" : dirtyCount.ToString(CultureInfo.InvariantCulture) + " DIRTY";
                ReadinessText.Text = dirtyCount == 0
                    ? "Semantic state hiện không có cấu kiện dirty. Dùng Health để kiểm tra sâu trước khi phát hành."
                    : "Có " + dirtyCount.ToString(CultureInfo.InvariantCulture) + " cấu kiện dirty; xem Geometry/Quantity bên dưới rồi dùng Regenerate hoặc Health theo workflow hiện hữu.";

                try { UnitText.Text = CadUnitService.Describe(_document); UnitText.ToolTip = null; }
                catch (Exception unitError) { UnitText.Text = "BLOCKED"; UnitText.ToolTip = unitError.Message; }

                if (ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                    SetStatus("Project readiness snapshot đã đồng bộ read-only.");
                else
                    SetStatus("Đang xem project của “" + DrawingLabel(_document) + "”. Kích hoạt lại đúng bản vẽ này trước khi chạy command.");
            }
            catch (Exception ex) { SetStatus("Đọc Project Tools lỗi: " + ex.Message); }
        }

        private void ClearProjectSnapshot()
        {
            ProjectNameText.Text = "—";
            ZoneText.Text = "—";
            FloorText.Text = "—";
            UnitText.Text = "—";
            UnitText.ToolTip = null;
            ZoneCountText.Text = "0";
            FloorCountText.Text = "0";
            FamilyCountText.Text = "0";
            ElementCountText.Text = "0";
            DirtyCountText.Text = "0";
            GeometryDirtyCountText.Text = "0";
            QuantityDirtyCountText.Text = "0";
            ChangeVersionText.Text = "0";
            UpdatedText.Text = "—";
            ReadinessBadgeText.Text = "READ-ONLY";
            ReadinessText.Text = "Đang đọc trạng thái project…";
        }

        private void EnsureBoundDrawingIsActive(string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Project Tools trước khi " + operation + ".");
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
