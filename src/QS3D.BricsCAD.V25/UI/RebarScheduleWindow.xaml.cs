using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RebarScheduleWindow : Window
    {
        private IReadOnlyList<RebarScheduleRow> _rows;
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
            BindRows();
        }

        private void BindRows()
        {
            Grid.ItemsSource = null;
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
                var currentRow = ResolveCurrentRow(row);
                _locate(currentRow);
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
                var dialog = new SaveFileDialog { Title = "Xuất BBS QS3D", Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = _defaultFileName, AddExtension = true, DefaultExt = ".xlsx", OverwritePrompt = true };
                if (dialog.ShowDialog(this) != true) return;

                EnsureActive("xuất BBS XLSX");
                _rows = BuildCurrentRows();
                BindRows();
                if (_rows.Count == 0) throw new InvalidOperationException("BBS hiện chưa có dòng để xuất.");

                XlsxRebarScheduleExporter.Export(dialog.FileName, _rows);
                MessageBox.Show(this, "Đã làm mới dữ liệu read-only hiện hành và xuất BBS XLSX.", "QS3D", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private RebarScheduleRow ResolveCurrentRow(RebarScheduleRow displayedRow)
        {
            var currentRows = BuildCurrentRows();
            var matches = currentRows
                .Where(x => x != null &&
                            string.Equals(x.ElementId, displayedRow.ElementId, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(x.BarMark, displayedRow.BarMark, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException("Dòng BBS đã cũ hoặc không còn định danh duy nhất trong project hiện hành. Đóng bảng và chạy lại QS3DBBSVIEW.");
            if (!SameRow(displayedRow, matches[0]))
                throw new InvalidOperationException("Dòng BBS đã thay đổi kể từ lúc bảng được mở. Đóng bảng và chạy lại QS3DBBSVIEW trước khi định vị.");
            return matches[0];
        }

        private IReadOnlyList<RebarScheduleRow> BuildCurrentRows()
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Đóng bảng BBS và mở lại.");
            var snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
            new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
            return ProjectRebarScheduleBuilder.Build(snapshot);
        }

        private static bool SameRow(RebarScheduleRow left, RebarScheduleRow right)
        {
            return string.Equals(left.ElementId, right.ElementId, StringComparison.Ordinal) &&
                   string.Equals(left.BarMark, right.BarMark, StringComparison.Ordinal) &&
                   string.Equals(left.ShapeCode, right.ShapeCode, StringComparison.Ordinal) &&
                   string.Equals(left.Notation, right.Notation, StringComparison.Ordinal) &&
                   left.DiameterMm.Equals(right.DiameterMm) &&
                   left.Quantity == right.Quantity &&
                   left.CuttingLengthM.Equals(right.CuttingLengthM) &&
                   left.TotalLengthM.Equals(right.TotalLengthM) &&
                   left.UnitWeightKgM.Equals(right.UnitWeightKgM) &&
                   left.NetWeightKg.Equals(right.NetWeightKg) &&
                   left.WastePercent.Equals(right.WastePercent) &&
                   left.TotalWeightKg.Equals(right.TotalWeightKg) &&
                   string.Equals(left.FabricationStatus, right.FabricationStatus, StringComparison.Ordinal) &&
                   string.Equals(left.FabricationStandardCode, right.FabricationStandardCode, StringComparison.Ordinal) &&
                   string.Equals(left.FabricationDetailingRevision, right.FabricationDetailingRevision, StringComparison.Ordinal);
        }

        private void EnsureActive(string operation)
        {
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Bảng BBS này thuộc một DWG khác. Hãy kích hoạt lại đúng bản vẽ trước khi " + operation + ".");
        }
    }
}
