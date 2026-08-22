using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Export;
using QS3D.Core.Rebar;
using QS3D.Core.Reporting;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RebarScheduleWindow : Window
    {
        private readonly IReadOnlyList<RebarScheduleRow> _rows;
        private readonly Action<RebarScheduleRow>? _locate;
        private readonly string _defaultFileName;
        private readonly Document _document;

        public RebarScheduleWindow(Document document, IReadOnlyList<RebarScheduleRow> rows, Action<RebarScheduleRow>? locate = null, string defaultFileName = "QS3D-BBS.xlsx")
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _locate = locate;
            _defaultFileName = defaultFileName;
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Grid.ItemsSource = _rows;

            var quantity = 0;
            var totalLengthM = 0d;
            var totalWeightKg = 0d;
            foreach (var row in _rows)
            {
                if (row == null) throw new InvalidOperationException("BBS không được chứa dòng null.");
                quantity = QuantityReportMath.AddCount(quantity, row.Quantity);
                totalLengthM = QuantityReportMath.Add(totalLengthM, row.TotalLengthM, "BBS visible total length");
                totalWeightKg = QuantityReportMath.Add(totalWeightKg, row.TotalWeightKg, "BBS visible total weight");
            }
            Totals.Text = "TỔNG: " + quantity + " thanh • " + totalLengthM.ToString("N3") + " m • " + totalWeightKg.ToString("N3") + " kg";
        }

        private void OnLocateClick(object sender, RoutedEventArgs e) => Locate();
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => Locate();

        private void Locate()
        {
            if (_locate == null || !(Grid.SelectedItem is RebarScheduleRow row)) return;
            try
            {
                EnsureActive("định vị BBS");
                _locate(row);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể định vị BBS: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("xuất BBS XLSX");
                if (_rows.Count == 0) throw new InvalidOperationException("BBS hiện chưa có dòng để xuất.");
                var dialog = new SaveFileDialog { Title = "Xuất BBS QS3D", Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = _defaultFileName, AddExtension = true, DefaultExt = ".xlsx", OverwritePrompt = true };
                if (dialog.ShowDialog(this) != true) return;
                XlsxRebarScheduleExporter.Export(dialog.FileName, _rows);
                MessageBox.Show(this, "Đã xuất BBS XLSX.", "QS3D", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void EnsureActive(string operation)
        {
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Bảng BBS này thuộc một DWG khác. Hãy kích hoạt lại đúng bản vẽ trước khi " + operation + ".");
        }
    }
}