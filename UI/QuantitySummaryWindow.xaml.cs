using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantitySummaryWindow : Window
    {
<<<<<<< origin/main
        private IReadOnlyList<QuantityReportRow> _rows;
        private readonly Action<QuantityReportRow>? _locate;
        private readonly Func<IReadOnlyList<QuantityReportRow>>? _recalculate;

        public QuantitySummaryWindow(IReadOnlyList<QuantityReportRow> rows, Action<QuantityReportRow>? locate = null, Func<IReadOnlyList<QuantityReportRow>>? recalculate = null)
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _locate = locate;
            _recalculate = recalculate;
            InitializeComponent();
            ReloadFloors();
            ReloadCategories();
            ApplyFilter();
        }

        private void ReloadFloors(string? preferred = null)
        {
            var floors = new List<string> { "Tất cả" };
            floors.AddRange(_rows.Select(x => x.Floor).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x));
            FloorCombo.ItemsSource = floors;
            FloorCombo.SelectedItem = preferred != null && floors.Any(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase)) ? floors.First(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase)) : "Tất cả";
        }

        private void ReloadCategories(string? preferred = null)
        {
            var categories = new List<string> { "Tất cả" };
            categories.AddRange(_rows.Select(x => x.Category).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x));
            CategoryList.ItemsSource = categories;
            CategoryList.SelectedItem = preferred != null && categories.Any(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase)) ? categories.First(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase)) : "Tất cả";
        }

        private void ApplyFilter()
        {
            if (QuantityGrid == null || TotalsText == null) return;
            var query = SearchBox == null ? string.Empty : (SearchBox.Text ?? string.Empty).Trim();
            var category = CategoryList?.SelectedItem as string ?? "Tất cả";
            var floor = FloorCombo?.SelectedItem as string ?? "Tất cả";
            var filtered = _rows.Where(x =>
                (floor == "Tất cả" || string.Equals(x.Floor, floor, StringComparison.OrdinalIgnoreCase)) &&
                (category == "Tất cả" || string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase)) &&
                (query.Length == 0 || x.FamilyName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Floor.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            QuantityGrid.ItemsSource = filtered;
            var totals = QuantityReportTotals.FromRows(filtered);
            TotalsText.Text = $"TỔNG: {totals.Count:N0} cấu kiện  •  Bê tông {totals.NetConcreteM3:N3} m³  •  Cốp pha {totals.FormworkM2:N3} m²  •  Dài {totals.LengthM:N3} m  •  DT cửa {totals.DoorAreaM2:N3} m²";
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void OnCategoryChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
        private void OnFloorChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void OnRecalculateClick(object sender, RoutedEventArgs e)
        {
            if (_recalculate == null) { ApplyFilter(); return; }
            var floor = FloorCombo.SelectedItem as string;
            var category = CategoryList.SelectedItem as string;
            try
            {
                _rows = _recalculate() ?? Array.Empty<QuantityReportRow>();
                ReloadFloors(floor);
                ReloadCategories(category);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể tính lại khối lượng: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnColumnVisibilityChanged(object sender, RoutedEventArgs e)
        {
            if (QuantityGrid == null || !(sender is CheckBox box) || box.Tag == null) return;
            if (!int.TryParse(box.Tag.ToString(), out var index) || index < 0 || index >= QuantityGrid.Columns.Count) return;
            QuantityGrid.Columns[index].Visibility = box.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
        private void OnLocateClick(object sender, RoutedEventArgs e) => LocateCurrent();
        private void OnQuantityGridDoubleClick(object sender, MouseButtonEventArgs e) => LocateCurrent();
        private void LocateCurrent()
        {
            if (_locate == null || !(QuantityGrid.SelectedItem is QuantityReportRow row)) return;
            try { _locate(row); }
            catch (Exception ex) { MessageBox.Show(this, "Không thể định vị: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Title = "Xuất bảng khối lượng QS3D", Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = "QS3D-Khoi-Luong.xlsx", AddExtension = true, DefaultExt = ".xlsx", OverwritePrompt = true };
            if (dialog.ShowDialog(this) != true) return;
            try
            {
                var visibleRows = (QuantityGrid.ItemsSource as IEnumerable<QuantityReportRow>)?.ToList() ?? _rows.ToList();
                XlsxQuantityExporter.Export(dialog.FileName, visibleRows);
                MessageBox.Show(this, "Đã xuất Excel thành công.", "QS3D", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể xuất Excel: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
=======
        private readonly IReadOnlyList<QuantityReportRow> _rows; private readonly Action<QuantityReportRow>? _locate;
        public QuantitySummaryWindow(IReadOnlyList<QuantityReportRow> rows, Action<QuantityReportRow>? locate = null) { _rows = rows ?? throw new ArgumentNullException(nameof(rows)); _locate = locate; InitializeComponent(); ReloadFloors(); ReloadCategories(); ApplyFilter(); }
        private void ReloadFloors() { var floors = new List<string> { "Tất cả" }; floors.AddRange(_rows.Select(x => x.Floor).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)); FloorCombo.ItemsSource = floors; FloorCombo.SelectedIndex = 0; }
        private void ReloadCategories() { var categories = new List<string> { "Tất cả" }; categories.AddRange(_rows.Select(x => x.Category).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)); CategoryList.ItemsSource = categories; CategoryList.SelectedIndex = 0; }
        private void ApplyFilter() { if (QuantityGrid == null || TotalsText == null) return; var query = SearchBox == null ? string.Empty : (SearchBox.Text ?? string.Empty).Trim(); var category = CategoryList?.SelectedItem as string ?? "Tất cả"; var floor = FloorCombo?.SelectedItem as string ?? "Tất cả"; var filtered = _rows.Where(x => (floor == "Tất cả" || string.Equals(x.Floor, floor, StringComparison.OrdinalIgnoreCase)) && (category == "Tất cả" || string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase)) && (query.Length == 0 || x.FamilyName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Floor.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToList(); QuantityGrid.ItemsSource = filtered; var totals = QuantityReportTotals.FromRows(filtered); TotalsText.Text = $"TỔNG: {totals.Count:N0} cấu kiện • BT {totals.NetConcreteM3:N3} m³ • Cốp pha {totals.FormworkM2:N3} m² • Thép {totals.SteelWeightKg:N3} kg • Dài {totals.LengthM:N3} m"; }
        private void OnSearchChanged(object sender, TextChangedEventArgs e) => ApplyFilter(); private void OnCategoryChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter(); private void OnFloorChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter(); private void OnRecalculateClick(object sender, RoutedEventArgs e) => ApplyFilter();
        private void OnColumnVisibilityChanged(object sender, RoutedEventArgs e) { if (QuantityGrid == null || !(sender is CheckBox box) || box.Tag == null) return; if (!int.TryParse(box.Tag.ToString(), out var index) || index < 0 || index >= QuantityGrid.Columns.Count) return; QuantityGrid.Columns[index].Visibility = box.IsChecked == true ? Visibility.Visible : Visibility.Collapsed; }
        private void OnLocateClick(object sender, RoutedEventArgs e) => LocateCurrent(); private void OnQuantityGridDoubleClick(object sender, MouseButtonEventArgs e) => LocateCurrent(); private void LocateCurrent() { if (_locate == null || !(QuantityGrid.SelectedItem is QuantityReportRow row)) return; try { _locate(row); } catch (Exception ex) { MessageBox.Show(this, "Không thể Locate: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning); } }
        private void OnExportClick(object sender, RoutedEventArgs e) { var dialog = new SaveFileDialog { Title = "Xuất bảng khối lượng QS3D", Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = "QS3D-Khoi-Luong.xlsx", AddExtension = true, DefaultExt = ".xlsx" }; if (dialog.ShowDialog(this) != true) return; try { var visibleRows = (QuantityGrid.ItemsSource as IEnumerable<QuantityReportRow>)?.ToList() ?? _rows.ToList(); XlsxQuantityExporter.Export(dialog.FileName, visibleRows); MessageBox.Show(this, "Đã xuất Excel thành công.", "QS3D", MessageBoxButton.OK, MessageBoxImage.Information); } catch (Exception ex) { MessageBox.Show(this, "Không thể xuất Excel: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Error); } }
>>>>>>> origin/agent/full-domain-20260810
    }
}
