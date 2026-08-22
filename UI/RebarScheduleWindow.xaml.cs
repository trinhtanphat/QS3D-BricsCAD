using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RebarScheduleWindow : Window
    {
<<<<<<< origin/main
        private readonly IReadOnlyList<RebarScheduleRow> _rows; private readonly Action<RebarScheduleRow>? _locate; private readonly string _defaultFileName;
        public RebarScheduleWindow(IReadOnlyList<RebarScheduleRow> rows, Action<RebarScheduleRow>? locate = null, string defaultFileName = "QS3D-BBS.xlsx")
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows)); _locate = locate; _defaultFileName = defaultFileName; InitializeComponent(); Grid.ItemsSource = _rows;
            Totals.Text = "TỔNG: " + _rows.Sum(x => x.Quantity) + " thanh • " + _rows.Sum(x => x.TotalLengthM).ToString("N3") + " m • " + _rows.Sum(x => x.TotalWeightKg).ToString("N3") + " kg";
        }
        private void OnLocateClick(object sender, RoutedEventArgs e) => Locate();
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => Locate();
        private void Locate() { if (_locate != null && Grid.SelectedItem is RebarScheduleRow row) _locate(row); }
        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Title = "Xuất BBS QS3D", Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = _defaultFileName, AddExtension = true, DefaultExt = ".xlsx", OverwritePrompt = true };
            if (dialog.ShowDialog(this) != true) return;
            try { XlsxRebarScheduleExporter.Export(dialog.FileName, _rows); MessageBox.Show(this, "Đã xuất BBS XLSX.", "QS3D", MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
=======
        private readonly IReadOnlyList<RebarScheduleRow> _rows; private readonly Action<RebarScheduleRow>? _locate;
        public RebarScheduleWindow(IReadOnlyList<RebarScheduleRow> rows, Action<RebarScheduleRow>? locate = null) { _rows = rows ?? throw new ArgumentNullException(nameof(rows)); _locate = locate; InitializeComponent(); Grid.ItemsSource = _rows; Totals.Text = "TỔNG: " + _rows.Sum(x => x.Quantity) + " thanh • " + _rows.Sum(x => x.TotalLengthM).ToString("N3") + " m • " + _rows.Sum(x => x.TotalWeightKg).ToString("N3") + " kg"; }
        private void OnLocateClick(object sender, RoutedEventArgs e) => Locate(); private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => Locate();
        private void Locate() { if (_locate != null && Grid.SelectedItem is RebarScheduleRow row) _locate(row); }
        private void OnExportClick(object sender, RoutedEventArgs e) { var dialog = new SaveFileDialog { Title = "Xuất BBS QS3D", Filter = "CSV UTF-8 (*.csv)|*.csv", FileName = "QS3D-BBS.csv", AddExtension = true, DefaultExt = ".csv" }; if (dialog.ShowDialog(this) != true) return; try { RebarCsvExporter.Export(dialog.FileName, _rows); MessageBox.Show(this, "Đã xuất BBS CSV.", "QS3D", MessageBoxButton.OK, MessageBoxImage.Information); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Error); } }
>>>>>>> origin/agent/full-domain-20260810
    }
}
