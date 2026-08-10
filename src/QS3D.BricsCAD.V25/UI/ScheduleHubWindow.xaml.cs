using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using QS3D.Core.Reporting;
using QS3D.Core.Services;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class ScheduleHubWindow : Window
    {
        private readonly Document _document;

        public ScheduleHubWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Loaded += (_, __) => RefreshSnapshot();
            Activated += (_, __) => RefreshSnapshot();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshSnapshot();

        private void OnCommandClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is string command) || string.IsNullOrWhiteSpace(command)) return;
            var normalizedCommand = command.Trim();
            try
            {
                EnsureActive("chạy " + normalizedCommand);
                _document.SendStringToExecute(normalizedCommand + " ", true, false, false);
                SetStatus("Đã gửi lệnh " + normalizedCommand + " sang “" + DrawingLabel(_document) + "”.");
            }
            catch (Exception ex) { SetStatus("Schedule Hub: " + ex.Message); }
        }

        private void RefreshSnapshot()
        {
            try
            {
                Title = "QS3D • Schedule Hub • " + DrawingLabel(_document);
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                {
                    SetStatus("Kích hoạt lại “" + DrawingLabel(_document) + "” để làm mới Schedule snapshot; số đang hiển thị được giữ nguyên.");
                    return;
                }

                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);

                var bqRows = ProjectQuantityReportBuilder.Group(project);
                var finishRows = RoomFinishScheduleBuilder.Build(project);
                var doorRows = DoorOpeningScheduleBuilder.Build(project);
                var curtainRows = CurtainWallScheduleBuilder.Build(project);
                var materialRows = MaterialUsageScheduleBuilder.Build(project);

                ElementCountText.Text = CountBqElements(bqRows).ToString(CultureInfo.InvariantCulture);
                FinishCountText.Text = CountFinishElements(finishRows).ToString(CultureInfo.InvariantCulture);
                DoorCountText.Text = CountDoorElements(doorRows).ToString(CultureInfo.InvariantCulture);
                CurtainCountText.Text = CountCurtainElements(curtainRows).ToString(CultureInfo.InvariantCulture);
                MaterialCountText.Text = materialRows.Select(x => x.MaterialName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString(CultureInfo.InvariantCulture);

                SetStatus("Schedule snapshot đã đồng bộ từ dữ liệu schedule hợp lệ" + (regenerated > 0 ? " • regen " + regenerated + " cấu kiện dirty." : "."));
            }
            catch (Exception ex) { SetStatus("Đọc Schedule Hub lỗi: " + ex.Message); }
        }

        private static int CountBqElements(System.Collections.Generic.IEnumerable<QuantityReportRow> rows)
        {
            var count = 0;
            foreach (var row in rows) count = QuantityReportMath.AddCount(count, row.Count);
            return count;
        }

        private static int CountFinishElements(System.Collections.Generic.IEnumerable<RoomFinishScheduleRow> rows)
        {
            var count = 0;
            foreach (var row in rows) count = QuantityReportMath.AddCount(count, row.Count);
            return count;
        }

        private static int CountDoorElements(System.Collections.Generic.IEnumerable<DoorOpeningScheduleRow> rows)
        {
            var count = 0;
            foreach (var row in rows) count = QuantityReportMath.AddCount(count, row.Count);
            return count;
        }

        private static int CountCurtainElements(System.Collections.Generic.IEnumerable<CurtainWallScheduleRow> rows)
        {
            var count = 0;
            foreach (var row in rows) count = QuantityReportMath.AddCount(count, row.ElementIds.Count);
            return count;
        }

        private void EnsureActive(string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Schedule Hub trước khi " + operation + ".");
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
