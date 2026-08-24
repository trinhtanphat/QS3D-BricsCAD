using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI.ViewModels;
using QS3D.Core.Reporting;
using QS3D.Core.Services;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private void OnExcelExportClick(object sender, RoutedEventArgs e)
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                _viewModel.Status = "Không có bản vẽ đang hoạt động để xuất Excel.";
                return;
            }

            if (_selectionGeometryFallback)
            {
                _viewModel.Status = "Xuất Excel cần QS3D project semantic; geometry read-only không được dùng làm quantity truth.";
                return;
            }

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
            {
                _viewModel.Status = "Xuất Excel cần một QS3D project hiện hữu.";
                return;
            }

            if (_boundDocument == null || !ReferenceEquals(document, _boundDocument) || !SameProjectIdentity(project))
            {
                _viewModel.Status = "Project/DWG đã thay đổi kể từ lần làm mới. Bấm Làm mới trước khi xuất Excel.";
                return;
            }

            try
            {
                var selectedItems = SelectedScopeItems();
                if (selectedItems.Count == 0)
                {
                    DispatchExistingCommand(
                        "QS3DED2 ",
                        "Xuất Excel: chưa chọn node cây; ED2 sẽ dùng scope chuẩn (Selection/Floor/Zone/All)."
                    );
                    return;
                }

                var currentRows = BuildPreviewRows(project, out _);
                var elementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in selectedItems)
                {
                    if (!_rowSnapshots.TryGetValue(item, out var displayedRow))
                        throw new InvalidOperationException("Node đang chọn không còn thuộc snapshot hiện hành. Hãy bấm Làm mới.");

                    var displayedIds = CanonicalIds(displayedRow.ElementIds);
                    if (displayedIds.Length == 0)
                        throw new InvalidOperationException("Node đang chọn chứa dòng không có ElementId semantic ổn định.");

                    var matches = currentRows
                        .Where(row => row != null && SameElementIdentity(displayedIds, row))
                        .ToList();
                    if (matches.Count != 1 || !SameRow(displayedRow, matches[0]))
                        throw new InvalidOperationException("Node đang chọn đã stale hoặc quantity/provenance đã thay đổi. Hãy bấm Làm mới.");

                    foreach (var id in displayedIds) elementIds.Add(id);
                }

                var handles = SourceHandleResolver.Resolve(project, elementIds)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (handles.Length == 0)
                    throw new InvalidOperationException("Scope cây đang chọn chưa có CAD Handle provenance để tạo Selection export.");

                // Resolve the entire live set before touching PICKFIRST. If any Handle became
                // stale between Quantity Insight refresh and this click, the user's existing
                // selection remains unchanged and ED2 is never dispatched with a partial scope.
                var resolved = Cad.CadHandleService.Resolve(document, handles);
                if (resolved.Count != handles.Length)
                    throw new InvalidOperationException("Một hoặc nhiều CAD Handle của scope đã stale/missing; giữ nguyên selection và từ chối export Selection mơ hồ.");

                document.Editor.SetImpliedSelection(resolved.ToArray());

                var scopeLabel = DescribeSelectedScope();
                DispatchExistingCommand(
                    "QS3DED2 ",
                    "Xuất Excel: đã map node " + scopeLabel + " → " + elementIds.Count + " ElementId / " + handles.Length + " Handle; ED2 mặc định Selection."
                );
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Không thể chuẩn bị scope Xuất Excel: " + ex.Message;
            }
        }

        private void OnExcelTracebackClick(object sender, RoutedEventArgs e)
        {
            DispatchExistingCommand(
                "QS3DEXCELLOCATE ",
                "Truy ngược Excel: chọn workbook và số dòng CHI_TIET; QS3D sẽ kiểm ElementId + Handle + Drawing Fingerprint trước khi locate."
            );
        }

        private void OnCadToExcelClick(object sender, RoutedEventArgs e)
        {
            DispatchExistingCommand(
                "QS3DCADTOEXCEL ",
                "CAD → Excel: chọn đúng một cấu kiện QS3D; workbook Excel đang mở phải được lưu và khớp ElementId + Handle + Drawing Fingerprint."
            );
        }

        private IReadOnlyList<QuantityInsightItemViewModel> SelectedScopeItems()
        {
            var selected = QuantityTree.SelectedItem;
            if (selected is QuantityInsightItemViewModel item)
                return new[] { item };
            if (selected is QuantityInsightNameViewModel name)
                return name.Items.ToArray();
            if (selected is QuantityInsightTypeViewModel type)
                return type.Names.SelectMany(value => value.Items).ToArray();
            if (selected is QuantityInsightFloorViewModel floor)
                return floor.Items.ToArray();
            return Array.Empty<QuantityInsightItemViewModel>();
        }

        private string DescribeSelectedScope()
        {
            var selected = QuantityTree.SelectedItem;
            if (selected is QuantityInsightItemViewModel item) return "Element '" + item.DisplayName + "'";
            if (selected is QuantityInsightNameViewModel name) return "Name/Family '" + name.Name + "'";
            if (selected is QuantityInsightTypeViewModel type) return "Type/Category '" + type.Name + "'";
            if (selected is QuantityInsightFloorViewModel floor) return "Floor '" + floor.Name + "'";
            return "ED2 standard scope";
        }
    }
}
