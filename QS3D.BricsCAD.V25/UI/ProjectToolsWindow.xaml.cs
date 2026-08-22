using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
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
                    ProjectNameText.Text = "—";
                    FloorText.Text = "—";
                    FamilyCountText.Text = "0";
                    ElementCountText.Text = "0";
                    try { UnitText.Text = CadUnitService.Describe(_document); UnitText.ToolTip = null; }
                    catch (Exception unitError) { UnitText.Text = "BLOCKED"; UnitText.ToolTip = unitError.Message; }
                    SetStatus("Chưa có QS3D project hiện hữu cho bản vẽ này. Project Tools chỉ hiển thị snapshot và không tạo replacement project khi mở/refresh.");
                    return;
                }

                ProjectNameText.Text = string.IsNullOrWhiteSpace(project.Name) ? project.ProjectId : project.Name;
                var activeFloor = project.Floors.FirstOrDefault(x => string.Equals(x.Id, project.ActiveFloorId, StringComparison.OrdinalIgnoreCase));
                FloorText.Text = activeFloor == null
                    ? (string.IsNullOrWhiteSpace(project.ActiveFloorId) ? "—" : project.ActiveFloorId)
                    : activeFloor.Name + " • " + activeFloor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
                FamilyCountText.Text = project.Families.Count.ToString(CultureInfo.InvariantCulture);
                ElementCountText.Text = project.Elements.Count.ToString(CultureInfo.InvariantCulture);
                try { UnitText.Text = CadUnitService.Describe(_document); UnitText.ToolTip = null; }
                catch (Exception unitError) { UnitText.Text = "BLOCKED"; UnitText.ToolTip = unitError.Message; }
                if (ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                    SetStatus("Project snapshot đã đồng bộ.");
                else
                    SetStatus("Đang xem project của “" + DrawingLabel(_document) + "”. Kích hoạt lại đúng bản vẽ này trước khi chạy command.");
            }
            catch (Exception ex) { SetStatus("Đọc Project Tools lỗi: " + ex.Message); }
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
