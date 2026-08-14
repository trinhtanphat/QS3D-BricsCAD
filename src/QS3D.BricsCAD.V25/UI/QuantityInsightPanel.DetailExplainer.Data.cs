using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI.ViewModels;
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
        private sealed class QuantityInsightDetailOption
        {
            public QuantityInsightDetailOption(QuantityReportRow row, int index)
            {
                Row = row;
                var name = string.IsNullOrWhiteSpace(row.ElementName) ? row.FamilyName : row.ElementName;
                DisplayName = (string.IsNullOrWhiteSpace(name) ? "Cấu kiện" : name.Trim()) + " • #" + index.ToString("N0", CultureInfo.CurrentCulture);
            }
            public QuantityReportRow Row { get; }
            public string DisplayName { get; }
            public override string ToString() => DisplayName;
        }

        private void RefreshQuantityDetail(QuantityInsightItemViewModel item)
        {
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || _boundDocument == null || !ReferenceEquals(document, _boundDocument))
            {
                ClearQuantityDetail("Bảng chi tiết thuộc DWG khác hoặc đã cũ; bấm Làm mới.");
                return;
            }
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project) || !SameProjectIdentity(project))
            {
                ClearQuantityDetail("QS3D project đã thay đổi; bấm Làm mới trước khi xem chi tiết.");
                return;
            }

            try
            {
                var currentRow = ResolveCurrentRow(item, project);
                var preview = ProjectStateSnapshot.CreateDetachedCopy(project);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(preview);
                var details = ProjectQuantityReportBuilder.Detail(preview, currentRow.ElementIds)
                    .OrderBy(x => x.ElementName, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => string.Join("|", x.ElementIds), StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (details.Count == 0)
                {
                    ClearQuantityDetail("Dòng này chưa có detail row canonical để diễn giải.");
                    return;
                }

                var options = details.Select((row, index) => new QuantityInsightDetailOption(row, index + 1)).ToList();
                var firstOption = options.FirstOrDefault();
                if (firstOption == null)
                {
                    ClearQuantityDetail("Dòng này chưa có detail row canonical để diễn giải.");
                    return;
                }

                _quantityDetailOptions = options;
                if (_quantityDetailSelector != null)
                {
                    _quantityDetailSelectionLoading = true;
                    try
                    {
                        _quantityDetailSelector.ItemsSource = options;
                        _quantityDetailSelector.Visibility = options.Count > 1 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                        _quantityDetailSelector.SelectedItem = firstOption;
                    }
                    finally
                    {
                        _quantityDetailSelectionLoading = false;
                    }
                }
                RenderQuantityDetail(firstOption);
                _viewModel.Status = "Chi tiết read-only • " + details.Count.ToString("N0", CultureInfo.CurrentCulture) + " cấu kiện canonical.";
            }
            catch (Exception ex)
            {
                ClearQuantityDetail("Không thể đọc chi tiết: " + ex.Message);
            }
        }
    }
}
