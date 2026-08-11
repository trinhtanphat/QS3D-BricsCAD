using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI.ViewModels;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel : UserControl
    {
        private readonly QuantityInsightViewModel _viewModel = new QuantityInsightViewModel();
        private IReadOnlyList<EntitySnapshot> _selectionSnapshots = Array.Empty<EntitySnapshot>();

        public QuantityInsightPanel()
        {
            InitializeComponent();
            DataContext = _viewModel;
            Loaded += OnLoaded;
        }

        public void RefreshQuantityInsights()
        {
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                ClearQuantityInsights("Không có bản vẽ đang hoạt động.");
                return;
            }

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
            {
                ClearQuantityInsights("Chưa có QS3D project hiện hành. Mở/tạo project để xem khối lượng.");
                return;
            }

            try
            {
                var rows = ProjectQuantityReportBuilder.Group(project);
                var floors = rows
                    .GroupBy(x => DisplayFloor(x.Floor), StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x.Key, StringComparer.CurrentCultureIgnoreCase)
                    .Select(group => new QuantityInsightFloorViewModel(
                        group.Key,
                        group
                            .OrderBy(x => x.Category, StringComparer.CurrentCultureIgnoreCase)
                            .ThenBy(x => x.FamilyName, StringComparer.CurrentCultureIgnoreCase)
                            .ThenBy(x => x.ElementName, StringComparer.CurrentCultureIgnoreCase)
                            .Select(ToInsightItem)))
                    .ToList();

                var totals = QuantityReportTotals.FromRows(rows);
                _viewModel.Replace(floors, totals, rows.Count);
                EmptyHint.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                QuantityTree.Visibility = rows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                _viewModel.Status = rows.Count == 0
                    ? "Project hiện chưa có dòng khối lượng."
                    : "Read-only • dữ liệu từ project QS3D hiện hành • nhấp đúp để định vị.";
                ApplySelectionHighlights(project, false);
            }
            catch (Exception ex)
            {
                ClearQuantityInsights("Không đọc được khối lượng: " + ex.Message);
            }
        }

        public void SetInspectionReadOnly(IReadOnlyList<EntitySnapshot> snapshots, ProjectState? project)
        {
            _selectionSnapshots = snapshots ?? Array.Empty<EntitySnapshot>();
            if (project == null)
            {
                ClearSelectionHighlights();
                return;
            }

            try
            {
                ApplySelectionHighlights(project, true);
            }
            catch
            {
                ClearSelectionHighlights();
            }
        }

        public void ClearQuantityInsights(string status)
        {
            _selectionSnapshots = Array.Empty<EntitySnapshot>();
            _viewModel.Clear(status ?? string.Empty);
            EmptyHint.Visibility = Visibility.Visible;
            QuantityTree.Visibility = Visibility.Collapsed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            RefreshQuantityInsights();
        }

        private static QuantityInsightItemViewModel ToInsightItem(QuantityReportRow row)
        {
            return new QuantityInsightItemViewModel(
                DisplayFloor(row.Floor),
                row.Category,
                row.FamilyName,
                row.ElementName,
                row.Count,
                FormatSummary(row),
                row.ElementIds
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray());
        }

        private static string DisplayFloor(string? floor) =>
            string.IsNullOrWhiteSpace(floor) ? "Chưa gán tầng" : floor.Trim();

        private static string FormatSummary(QuantityReportRow row)
        {
            var parts = new List<string>(3);
            if (Math.Abs(row.NetConcreteM3) > 1e-9) parts.Add(row.NetConcreteM3.ToString("0.###") + " m³");
            if (Math.Abs(row.FormworkM2) > 1e-9) parts.Add(row.FormworkM2.ToString("0.###") + " m²");
            if (Math.Abs(row.LengthM) > 1e-9 && parts.Count < 2) parts.Add(row.LengthM.ToString("0.###") + " m");
            return parts.Count > 0 ? string.Join(" • ", parts) : row.Count.ToString("N0") + " cấu kiện";
        }

        private void ApplySelectionHighlights(ProjectState project, bool updateStatus)
        {
            var selectedHandles = new HashSet<string>(
                _selectionSnapshots
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Handle))
                    .Select(x => x.Handle.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var matchCount = 0;
            foreach (var item in _viewModel.AllItems())
            {
                var handles = item.ElementIds.Count == 0
                    ? Array.Empty<string>()
                    : SourceHandleResolver.Resolve(project, item.ElementIds).ToArray();
                var matched = selectedHandles.Count > 0 && handles.Any(selectedHandles.Contains);
                item.SetSelectionMatch(matched);
                if (matched) matchCount++;
            }

            if (!updateStatus) return;
            if (selectedHandles.Count == 0)
                _viewModel.Status = "CAD selection đã bỏ chọn • khối lượng vẫn ở chế độ read-only.";
            else if (matchCount > 0)
                _viewModel.Status = "Đã highlight " + matchCount.ToString("N0") + " dòng khối lượng liên quan selection CAD.";
            else
                _viewModel.Status = "Selection CAD hiện tại chưa ánh xạ tới dòng khối lượng semantic nào.";
        }

        private void ClearSelectionHighlights()
        {
            foreach (var item in _viewModel.AllItems()) item.SetSelectionMatch(false);
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshQuantityInsights();

        private void OnRegenerateClick(object sender, RoutedEventArgs e) =>
            DispatchExistingCommand("QS3DREGEN ", "Đã gửi lệnh QS3DREGEN; bảng sẽ làm mới sau khi regenerate hoàn tất.");

        private void OnOpenBqClick(object sender, RoutedEventArgs e) =>
            DispatchExistingCommand("QS3DBQ ", "Đã mở workflow BQ đầy đủ.");

        private void OnLocateClick(object sender, RoutedEventArgs e) => LocateSelected();

        private void OnQuantityTreeDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (QuantityTree.SelectedItem is QuantityInsightItemViewModel) LocateSelected();
        }

        private void LocateSelected()
        {
            if (!(QuantityTree.SelectedItem is QuantityInsightItemViewModel item))
            {
                _viewModel.Status = "Chọn một dòng cấu kiện trong cây trước khi định vị.";
                return;
            }

            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                _viewModel.Status = "Không có bản vẽ đang hoạt động để định vị.";
                return;
            }
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
            {
                _viewModel.Status = "QS3D project hiện hành không còn khả dụng; hãy làm mới bảng.";
                return;
            }

            try
            {
                var handles = SourceHandleResolver.Resolve(project, item.ElementIds);
                if (handles.Count == 0)
                {
                    _viewModel.Status = "Dòng này chưa có semantic handle hiện hành để định vị trong CAD.";
                    return;
                }

                var count = Cad.CadHandleService.Select(document, handles);
                _viewModel.Status = "Định vị: đã chọn " + count.ToString("N0") + " đối tượng CAD.";
                if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Không thể định vị: " + ex.Message;
            }
        }

        private void DispatchExistingCommand(string command, string status)
        {
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                _viewModel.Status = "Không có bản vẽ đang hoạt động.";
                return;
            }

            document.SendStringToExecute(command, true, false, false);
            _viewModel.Status = status;
        }
    }
}
