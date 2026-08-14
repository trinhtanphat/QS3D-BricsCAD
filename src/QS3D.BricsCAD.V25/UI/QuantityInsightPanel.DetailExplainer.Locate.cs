using System;
using System.Linq;
using System.Windows;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private void OnQuantityDetailLocateClick(object sender, RoutedEventArgs e)
        {
            var option = _quantityDetailSelector?.SelectedItem as QuantityInsightDetailOption;
            if (option == null && _quantityDetailOptions.Count == 1) option = _quantityDetailOptions[0];
            if (option == null) { _viewModel.Status = "Chọn một cấu kiện chi tiết trước khi định vị."; return; }

            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || _boundDocument == null || !ReferenceEquals(document, _boundDocument))
            { _viewModel.Status = "Chi tiết thuộc DWG khác hoặc đã cũ; bấm Làm mới."; return; }
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project) || !SameProjectIdentity(project))
            { _viewModel.Status = "QS3D project đã thay đổi; bấm Làm mới trước khi định vị."; return; }

            try
            {
                var preview = ProjectStateSnapshot.CreateDetachedCopy(project);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(preview);
                var ids = CanonicalIds(option.Row.ElementIds);
                var matches = ProjectQuantityReportBuilder.Detail(preview, ids)
                    .Where(x => SameElementIdentity(ids, x)).ToList();
                if (matches.Count != 1 || !SameRow(option.Row, matches[0]))
                { _viewModel.Status = "Chi tiết đã thay đổi kể từ lần đọc; bấm Làm mới."; return; }

                var handles = SourceHandleResolver.Resolve(project, matches[0].ElementIds);
                if (handles.Count == 0)
                { Cad.CadHandleService.ClearSelection(document); _viewModel.Status = "Cấu kiện này chưa có semantic handle hiện hành để định vị."; return; }
                var count = Cad.CadHandleService.Select(document, handles);
                if (count <= 0)
                { Cad.CadHandleService.ClearSelection(document); _viewModel.Status = "Không còn đối tượng CAD hợp lệ để chọn."; return; }
                if (!global::QS3D.BricsCAD.V25.ViewportCommands.TryZoomSelection(document))
                { _viewModel.Status = "Đã chọn " + count.ToString("N0") + " đối tượng CAD nhưng chưa thể zoom cấu kiện hiện hành."; return; }
                _viewModel.Status = "Định vị chi tiết: đã chọn/zoom " + count.ToString("N0") + " đối tượng CAD.";
            }
            catch (Exception ex)
            { _viewModel.Status = "Không thể định vị chi tiết: " + ex.Message; }
        }
    }
}
