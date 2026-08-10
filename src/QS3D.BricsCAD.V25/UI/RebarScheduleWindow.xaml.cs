using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Export;
using QS3D.Core.Rebar;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RebarScheduleWindow : Window
    {
        private readonly IReadOnlyList<RebarScheduleRow> _rows;
        private readonly Action<RebarScheduleRow>? _locate;
        private readonly string _defaultFileName;
        private readonly Document _document;

        public RebarScheduleWindow(IReadOnlyList<RebarScheduleRow> rows, Action<RebarScheduleRow>? locate = null, string defaultFileName = "QS3D-BBS.xlsx")
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _locate = locate;
            _defaultFileName = defaultFileName;
            _document = BcadApplication.DocumentManager.MdiActiveDocument ?? throw new InvalidOperationException("Không có DWG active khi mở BBS.");
            InitializeComponent();
            Grid.ItemsSource = _rows;
            Totals.Text = "TỔNG: " + _rows.Sum(x => x.Quantity) + " thanh • " + _rows.Sum(x => x.TotalLengthM).ToString("N3") + " m • " + _rows.Sum(x => x.TotalWeightKg).ToString("N3") + " kg";
        }

        private void OnLocateClick(object sender, RoutedEventArgs e) => Locate();
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => Locate();

        private void Locate()
        {
            if (_locate == null || !(Grid.SelectedItem is RebarScheduleRow row)) return;
            try
            {
                EnsureActive();
                _locate(row);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể định vị BBS: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Title = "Xuất BBS QS3D", Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = _defaultFileName, AddExtension = true, DefaultExt = ".xlsx", OverwritePrompt = true };
            if (dialog.ShowDialog(this) != true) return;
            try { XlsxRebarScheduleExporter.Export(dialog.FileName, _rows); MessageBox.Show(this, "Đã xuất BBS XLSX.", "QS3D", MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void EnsureActive()
        {
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Bảng BBS này thuộc một DWG khác. Hãy kích hoạt lại đúng bản vẽ trước khi định vị.");
        }
    }
}
