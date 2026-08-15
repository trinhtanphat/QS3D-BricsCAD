using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QS3D.BricsCAD.V25.Reporting;
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
        private StackPanel? _quantityGeometryPanel;
        private ScrollViewer? _quantityGeometryScroll;
        private QuantityGeometryExplanation? _quantityGeometryCurrent;
        private string _quantityGeometryError = string.Empty;

        private QuantityGeometryExplanation? RefreshQuantityGeometry(QuantityInsightDetailOption option)
        {
            _quantityGeometryCurrent = null;
            _quantityGeometryError = string.Empty;
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || _boundDocument == null || !ReferenceEquals(document, _boundDocument))
            {
                _quantityGeometryError = "DWG đã đổi; bấm Làm mới trước khi tính hình học.";
                return null;
            }
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project) || !SameProjectIdentity(project))
            {
                _quantityGeometryError = "QS3D project đã thay đổi; bấm Làm mới trước khi tính hình học.";
                return null;
            }

            var ids = option.Row.ElementIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (ids.Length != 1)
            {
                _quantityGeometryError = "Diễn giải hình học cần đúng một cấu kiện canonical; dòng hiện có " + ids.Length.ToString("N0", CultureInfo.CurrentCulture) + " ElementId.";
                return null;
            }

            try
            {
                var geometryProject = PrepareQuantityGeometrySnapshot(document, project, ids, out var geometryError);
                if (geometryProject == null)
                {
                    _quantityGeometryError = string.IsNullOrWhiteSpace(geometryError)
                        ? "Không thể tạo snapshot Solid3d live an toàn."
                        : geometryError;
                    return null;
                }
                _quantityGeometryCurrent = QuantityGeometryExplanationService.Build(document, geometryProject, ids[0]);
                return _quantityGeometryCurrent;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException) && !(ex is StackOverflowException) && !(ex is AccessViolationException))
            {
                _quantityGeometryError = ex.Message;
                return null;
            }
        }

        private void RenderQuantityGeometry(QuantityGeometryExplanation? geometry)
        {
            EnsureQuantityGeometryPanel();
            if (_quantityGeometryPanel == null || _quantityGeometryScroll == null) return;
            _quantityGeometryPanel.Children.Clear();
            _quantityGeometryScroll.Visibility = Visibility.Visible;

            var title = CaptionText();
            title.Text = geometry == null
                ? "DIỄN GIẢI HÌNH HỌC"
                : "DIỄN GIẢI HÌNH HỌC • BREP EXACT";
            title.FontWeight = FontWeights.SemiBold;
            _quantityGeometryPanel.Children.Add(title);

            if (geometry == null)
            {
                var unavailable = CaptionText(true);
                unavailable.Text = "Chưa có diễn giải hình học: " + (string.IsNullOrWhiteSpace(_quantityGeometryError) ? "không có Solid3d live khả dụng." : _quantityGeometryError);
                _quantityGeometryPanel.Children.Add(unavailable);
                return;
            }

            var concreteEquation = new TextBlock
            {
                Text = "Bê tông: " + FormatGeometryValue(geometry.GrossVolume) + " - " +
                       FormatGeometryValue(geometry.DeductionVolume) + " = " +
                       FormatGeometryValue(geometry.NetVolume) + " m³",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0d, 3d, 0d, 2d),
                TextWrapping = TextWrapping.Wrap
            };
            concreteEquation.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush");
            _quantityGeometryPanel.Children.Add(concreteEquation);

            if (geometry.IsDirty)
            {
                var stale = CaptionText(true);
                stale.Text = "Semantic state đang dirty; các số dưới đây vừa được tính lại trực tiếp từ Solid3d live.";
                _quantityGeometryPanel.Children.Add(stale);
            }

            AddQuantityGeometryHeading("THỂ TÍCH • GỘP - TRỪ = CÒN");
            AddQuantityGeometryValue("V gộp", geometry.GrossVolume, "m³", true);
            foreach (var deduction in geometry.VolumeDeductions)
                AddQuantityGeometryDeductionButton("Trừ giao", deduction, deduction.Volume, "m³");
            AddQuantityGeometryValue("V còn", geometry.NetVolume, "m³", true);

            AddQuantityGeometryHeading("VÁN KHUÔN THEO MẶT • GỘP - TRỪ = CÒN");
            foreach (var face in geometry.FormworkFaces
                .OrderBy(x => FaceSort(x.FaceType))
                .ThenBy(x => x.FaceId, StringComparer.OrdinalIgnoreCase))
            {
                var faceTitle = new TextBlock
                {
                    Text = face.FaceId + " • " + face.FaceType,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(8d, 5d, 0d, 1d),
                    TextWrapping = TextWrapping.Wrap
                };
                faceTitle.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
                _quantityGeometryPanel.Children.Add(faceTitle);
                AddQuantityGeometryValue("S gộp", face.GrossArea, "m²", false, 12d);
                foreach (var deduction in face.Deductions)
                    AddQuantityGeometryDeductionButton("Trừ", deduction, deduction.Area, "m²", 12d);
                AddQuantityGeometryValue("S còn", face.NetArea, "m²", true, 12d);
            }

            var totals = CaptionText(true);
            totals.Text = "Ván khuôn: S gộp " + FormatGeometryValue(geometry.GrossFormworkArea) + " m² - " +
                          FormatGeometryValue(geometry.DeductionFormworkArea) + " m² = S còn " +
                          FormatGeometryValue(geometry.NetFormworkArea) + " m²";
            totals.Margin = new Thickness(0d, 6d, 0d, 0d);
            _quantityGeometryPanel.Children.Add(totals);

            var fingerprint = CaptionText(true);
            fingerprint.Text = "Fingerprint: " + ShortFingerprint(geometry.GeometryFingerprint) +
                               " • dependencies: " + geometry.Dependencies.Count.ToString("N0", CultureInfo.CurrentCulture) +
                               " • diagnostics: " + geometry.Diagnostics.Count.ToString("N0", CultureInfo.CurrentCulture);
            _quantityGeometryPanel.Children.Add(fingerprint);
            foreach (var diagnostic in geometry.Diagnostics.Take(3))
            {
                var line = CaptionText(true);
                line.Text = "• " + diagnostic;
                _quantityGeometryPanel.Children.Add(line);
            }
        }

        private void EnsureQuantityGeometryPanel()
        {
            if (_quantityGeometryPanel != null || _quantityDetailBody == null) return;
            _quantityGeometryPanel = new StackPanel();
            _quantityGeometryScroll = new ScrollViewer
            {
                Content = _quantityGeometryPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0d, 6d, 0d, 0d)
            };
            var insertAt = Math.Min(2, _quantityDetailBody.Children.Count);
            _quantityDetailBody.Children.Insert(insertAt, _quantityGeometryScroll);
        }

        private void AddQuantityGeometryHeading(string text)
        {
            if (_quantityGeometryPanel == null) return;
            var heading = CaptionText();
            heading.Text = text;
            heading.FontWeight = FontWeights.SemiBold;
            heading.Margin = new Thickness(0d, 6d, 0d, 2d);
            _quantityGeometryPanel.Children.Add(heading);
        }

        private void AddQuantityGeometryValue(string label, double value, string unit, bool strong, double left = 0d)
        {
            if (_quantityGeometryPanel == null) return;
            var button = new Button
            {
                Content = label + ": " + FormatGeometryValue(value) + " " + unit,
                Tag = _quantityGeometryCurrent?.ElementId ?? string.Empty,
                FontWeight = strong ? FontWeights.SemiBold : FontWeights.Normal,
                Margin = new Thickness(left, 1d, 0d, 1d),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                ToolTip = "Click để chọn/highlight Solid3d live của cấu kiện đích và zoom trong CAD."
            };
            button.SetResourceReference(FrameworkElement.StyleProperty, "DenseButton");
            button.Click += OnQuantityGeometryTargetClick;
            _quantityGeometryPanel.Children.Add(button);
        }

        private void AddQuantityGeometryDeductionButton(string prefix, QuantityGeometryDeduction deduction, double value, string unit, double left = 4d)
        {
            if (_quantityGeometryPanel == null) return;
            var cause = string.IsNullOrWhiteSpace(deduction.ElementName) ? deduction.ElementId : deduction.ElementName;
            var button = new Button
            {
                Content = prefix + " - " + cause + ": -" + FormatGeometryValue(value) + " " + unit,
                Tag = deduction,
                Margin = new Thickness(left, 1d, 0d, 1d),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                ToolTip = "Click để chọn/highlight cấu kiện đích + nguyên nhân và zoom trong CAD. " + deduction.RegionKey
            };
            button.SetResourceReference(FrameworkElement.StyleProperty, "DenseButton");
            button.Click += OnQuantityGeometryDeductionClick;
            _quantityGeometryPanel.Children.Add(button);
        }

        private void OnQuantityGeometryTargetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string elementId && !string.IsNullOrWhiteSpace(elementId))
                LocateQuantityGeometryTarget(elementId);
        }

        private void LocateQuantityGeometryTarget(string expectedElementId)
        {
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || _boundDocument == null || !ReferenceEquals(document, _boundDocument))
            {
                _viewModel.Status = "Không thể định vị hình học: DWG hiện hành đã thay đổi.";
                return;
            }
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project) || !SameProjectIdentity(project))
            {
                _viewModel.Status = "Không thể định vị hình học: QS3D project đã thay đổi; hãy bấm Làm mới.";
                return;
            }

            var option = _quantityDetailSelector?.SelectedItem as QuantityInsightDetailOption;
            if (option == null && _quantityDetailOptions.Count == 1) option = _quantityDetailOptions[0];
            if (option == null)
            {
                _viewModel.Status = "Không còn cấu kiện detail hiện hành để định vị hình học.";
                return;
            }

            try
            {
                if (!TryRevalidateQuantityGeometry(document, project, option, out var freshGeometry, out var elementIds, out var error))
                {
                    _viewModel.Status = error;
                    return;
                }
                if (freshGeometry == null || elementIds.Length != 1 ||
                    !string.Equals(freshGeometry.ElementId, expectedElementId, StringComparison.OrdinalIgnoreCase))
                {
                    _viewModel.Status = "Provenance hình học đã thay đổi; bấm Làm mới trước khi định vị.";
                    return;
                }

                var handles = ResolveQuantityPreferredLiveHandles(document, project, new[] { freshGeometry.ElementId }, out var resolutionError);
                if (handles.Count == 0)
                {
                    Cad.CadHandleService.ClearSelection(document);
                    _viewModel.Status = string.IsNullOrWhiteSpace(resolutionError)
                        ? "Không còn Solid3d/CAD handle live cho cấu kiện đích."
                        : "Không thể định vị hình học: " + resolutionError;
                    return;
                }

                var count = Cad.CadHandleService.Select(document, handles);
                if (count <= 0)
                {
                    Cad.CadHandleService.ClearSelection(document);
                    _viewModel.Status = "Không còn đối tượng CAD live hợp lệ cho cấu kiện đích.";
                    return;
                }
                if (!global::QS3D.BricsCAD.V25.ViewportCommands.TryZoomSelection(document))
                {
                    _viewModel.Status = "Đã chọn/highlight " + count.ToString("N0", CultureInfo.CurrentCulture) + " đối tượng live nhưng chưa thể zoom.";
                    return;
                }

                _viewModel.Status = "Đã chọn/highlight/zoom " + count.ToString("N0", CultureInfo.CurrentCulture) + " đối tượng live của " + freshGeometry.ElementId + ".";
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Không thể định vị hình học: " + ex.Message;
            }
        }

        private void OnQuantityGeometryDeductionClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QuantityGeometryDeduction deduction)
                LocateQuantityGeometryDeduction(deduction);
        }

        private void LocateQuantityGeometryDeduction(QuantityGeometryDeduction deduction)
        {
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || _boundDocument == null || !ReferenceEquals(document, _boundDocument))
            {
                _viewModel.Status = "Không thể định vị giao: DWG hiện hành đã thay đổi.";
                return;
            }
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project) || !SameProjectIdentity(project))
            {
                _viewModel.Status = "Không thể định vị giao: QS3D project đã thay đổi; hãy bấm Làm mới.";
                return;
            }

            var option = _quantityDetailSelector?.SelectedItem as QuantityInsightDetailOption;
            if (option == null && _quantityDetailOptions.Count == 1) option = _quantityDetailOptions[0];
            if (option == null)
            {
                _viewModel.Status = "Không còn cấu kiện detail hiện hành để định vị giao.";
                return;
            }

            try
            {
                if (!TryRevalidateQuantityGeometry(document, project, option, out var freshGeometry, out var elementIds, out var error))
                {
                    _viewModel.Status = error;
                    return;
                }
                if (freshGeometry == null || elementIds.Length != 1)
                {
                    _viewModel.Status = "Provenance hình học đã thay đổi; bấm Làm mới trước khi định vị giao.";
                    return;
                }

                var currentDeductions = freshGeometry.VolumeDeductions
                    .Concat(freshGeometry.FormworkFaces.SelectMany(x => x.Deductions))
                    .Where(x => string.Equals(x.RegionKey, deduction.RegionKey, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (currentDeductions.Count != 1)
                {
                    _viewModel.Status = "Dòng khấu trừ đã thay đổi hoặc không còn duy nhất; bấm Làm mới trước khi định vị giao.";
                    return;
                }

                var currentDeduction = currentDeductions[0];
                var semanticIds = elementIds
                    .Concat(new[] { currentDeduction.ElementId })
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var handles = ResolveQuantityPreferredLiveHandles(document, project, semanticIds, out var resolutionError);
                if (handles.Count == 0)
                {
                    Cad.CadHandleService.ClearSelection(document);
                    _viewModel.Status = string.IsNullOrWhiteSpace(resolutionError)
                        ? "Không còn CAD handle live cho dòng khấu trừ " + currentDeduction.RegionKey + "."
                        : "Không thể định vị dòng khấu trừ: " + resolutionError;
                    return;
                }

                var count = Cad.CadHandleService.Select(document, handles);
                if (count <= 0)
                {
                    Cad.CadHandleService.ClearSelection(document);
                    _viewModel.Status = "Không còn đối tượng CAD live hợp lệ cho dòng khấu trừ " + currentDeduction.RegionKey + ".";
                    return;
                }
                if (!global::QS3D.BricsCAD.V25.ViewportCommands.TryZoomSelection(document))
                {
                    _viewModel.Status = "Đã chọn/highlight cấu kiện đích + nguyên nhân nhưng chưa thể zoom • " + currentDeduction.RegionKey;
                    return;
                }

                _viewModel.Status = "Đã chọn/highlight/zoom cấu kiện đích + nguyên nhân • " + currentDeduction.RegionKey;
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Không thể định vị dòng khấu trừ: " + ex.Message;
            }
        }

        private bool TryRevalidateQuantityGeometry(
            Bricscad.ApplicationServices.Document document,
            ProjectState project,
            QuantityInsightDetailOption option,
            out QuantityGeometryExplanation? geometry,
            out string[] elementIds,
            out string error)
        {
            geometry = null;
            elementIds = Array.Empty<string>();
            error = string.Empty;

            var preview = ProjectStateSnapshot.CreateDetachedCopy(project);
            new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(preview);
            elementIds = CanonicalIds(option.Row.ElementIds).ToArray();
            if (elementIds.Length != 1)
            {
                error = "Diễn giải hình học cần đúng một cấu kiện canonical; bấm Làm mới dữ liệu detail.";
                return false;
            }

            var matches = ProjectQuantityReportBuilder.Detail(preview, elementIds)
                .Where(x => SameElementIdentity(elementIds, x))
                .ToList();
            if (matches.Count != 1 || !SameRow(option.Row, matches[0]))
            {
                error = "Dữ liệu detail hoặc provenance đã thay đổi; bấm Làm mới trước khi định vị.";
                return false;
            }

            var geometryProject = PrepareQuantityGeometrySnapshot(document, project, elementIds, out var geometryError);
            if (geometryProject == null)
            {
                error = string.IsNullOrWhiteSpace(geometryError)
                    ? "Không thể tạo snapshot Solid3d live an toàn; bấm Làm mới."
                    : geometryError;
                return false;
            }

            var fresh = QuantityGeometryExplanationService.Build(document, geometryProject, elementIds[0]);
            if (_quantityGeometryCurrent == null ||
                !string.Equals(fresh.GeometryFingerprint, _quantityGeometryCurrent.GeometryFingerprint, StringComparison.Ordinal))
            {
                error = "Solid3d/BREP đã thay đổi kể từ lần đọc; bấm Làm mới trước khi định vị.";
                return false;
            }

            geometry = fresh;
            return true;
        }

        private static int FaceSort(string faceType)
        {
            if (string.Equals(faceType, "Bottom", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(faceType, "Side", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(faceType, "End", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(faceType, "Top", StringComparison.OrdinalIgnoreCase)) return 3;
            return 4;
        }

        private static string FormatGeometryValue(double value) =>
            value.ToString("0.######", CultureInfo.CurrentCulture);

        private static string ShortFingerprint(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "—";
            var text = value.Trim();
            return text.Length <= 12 ? text : text.Substring(0, 12);
        }
    }
}
