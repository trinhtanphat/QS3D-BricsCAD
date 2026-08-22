using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using QS3D.Core.Export;
using QS3D.Core.Reporting;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private void OnQuantityEvidenceExportClick(object sender, RoutedEventArgs e)
        {
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || _boundDocument == null || !ReferenceEquals(document, _boundDocument))
            {
                _viewModel.Status = "Không thể xuất evidence: DWG hiện hành đã thay đổi.";
                return;
            }
            if (_selectionGeometryFallback)
            {
                _viewModel.Status = "Xuất evidence cần QS3D semantic element; selection CAD thô không có quantity-rule provenance.";
                return;
            }
            if (!Services.ProjectContextCoordinator.TryGetReadOnly(document, out var project) || !SameProjectIdentity(project))
            {
                _viewModel.Status = "Không thể xuất evidence: QS3D project đã thay đổi; hãy bấm Làm mới.";
                return;
            }

            try
            {
                var option = _quantityDetailSelector?.SelectedItem as QuantityInsightDetailOption;
                if (option == null && _quantityDetailOptions.Count == 1) option = _quantityDetailOptions[0];
                if (option == null && QuantityTree.SelectedItem is ViewModels.QuantityInsightItemViewModel selectedItem)
                {
                    RefreshQuantityDetail(selectedItem);
                    option = _quantityDetailSelector?.SelectedItem as QuantityInsightDetailOption;
                    if (option == null && _quantityDetailOptions.Count == 1) option = _quantityDetailOptions[0];
                }
                if (option == null)
                {
                    _viewModel.Status = "Chọn một Element trong cây Floor / Type / Name / Element trước khi xuất evidence.";
                    return;
                }

                if (_quantityGeometryCurrent == null) RefreshQuantityGeometry(option);
                if (!TryRevalidateQuantityGeometry(document, project, option, out var freshGeometry, out var elementIds, out var error))
                {
                    _viewModel.Status = error;
                    return;
                }
                if (freshGeometry == null || elementIds.Length != 1)
                {
                    _viewModel.Status = "Evidence hình học không còn duy nhất; hãy bấm Làm mới trước khi xuất.";
                    return;
                }

                var evidence = QuantityGeometryEvidenceAdapter.Create(freshGeometry);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất QS3D Quantity Review evidence",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = "QS3D-Quantity-Review-" + SafeEvidenceFileName(freshGeometry.ElementId) + ".xlsx",
                    AddExtension = true,
                    DefaultExt = ".xlsx",
                    OverwritePrompt = true
                };
                if (dialog.ShowDialog() != true) return;

                XlsxQuantityEvidenceExporter.Export(dialog.FileName, evidence.Explanations);
                _viewModel.Status = "Đã xuất đúng evidence graph đang review • concrete " + ShortEvidenceId(evidence.Concrete.EvidenceId) +
                                    " • formwork " + ShortEvidenceId(evidence.Formwork.EvidenceId) + ".";
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException) && !(ex is StackOverflowException) && !(ex is AccessViolationException))
            {
                _viewModel.Status = "Không thể xuất Quantity Review evidence: " + ex.Message;
            }
        }

        private static string SafeEvidenceFileName(string value)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "Element" : value.Trim();
            foreach (var invalid in Path.GetInvalidFileNameChars()) text = text.Replace(invalid, '-');
            return text.Length > 80 ? text.Substring(0, 80) : text;
        }

        private static string ShortEvidenceId(string value)
        {
            var text = value ?? string.Empty;
            return text.Length <= 12 ? text : text.Substring(0, 12);
        }
    }
}
