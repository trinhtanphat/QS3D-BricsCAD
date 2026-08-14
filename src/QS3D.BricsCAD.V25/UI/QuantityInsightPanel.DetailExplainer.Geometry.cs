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
                _quantityGeometryCurrent = QuantityGeometryExplanationService.Build(document, project, ids[0]);
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
            title.Text = "DIỄN GIẢI HÌNH HỌC • BREP EXACT";
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
            var line = new TextBlock
            {
                Text = label + ": " + FormatGeometryValue(value) + " " + unit,
                FontWeight = strong ? FontWeights.SemiBold : FontWeights.Normal,
                Margin = new Thickness(left, 1d, 0d, 1d),
                TextWrapping = TextWrapping.Wrap
            };
            line.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            _quantityGeometryPanel.Children.Add(line);
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
                ToolTip = "Click để chọn cấu kiện đích + nguyên nhân và zoom trong CAD. " + deduction.RegionKey
            };
            button.SetResourceReference(FrameworkElement.StyleProperty, "DenseButton");
            button.Click += OnQuantityGeometryDeductionClick;
            _quantityGeometryPanel.Children.Add(button);
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
                var preview = ProjectStateSnapshot.CreateDetachedCopy(project);
                new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(preview);
                var ids = CanonicalIds(option.Row.ElementIds);
                var matches = ProjectQuantityReportBuilder.Detail(preview, ids)
                    .Where(x => SameElementIdentity(ids, x))
                    .ToList();
                if (matches.Count != 1 || !SameRow(option.Row, matches[0]))
                {
                    _viewModel.Status = "Dữ liệu detail hoặc provenance đã thay đổi; bấm Làm mới trước khi định vị giao.";
                    return;
                }

                var currentRow = matches[0];
                var semanticIds = currentRow.ElementIds
                    .Concat(new[] { deduction.ElementId })
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var handles = SourceHandleResolver.Resolve(project, semanticIds)
                    .Concat(deduction.SourceHandles ?? Array.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var count = Cad.CadHandleService.Select(document, handles);
                if (count <= 0)
                {
                    Cad.CadHandleService.ClearSelection(document);
                    _viewModel.Status = "Không còn CAD handle live cho dòng khấu trừ " + deduction.RegionKey + ".";
                    return;
                }

                if (!global::QS3D.BricsCAD.V25.ViewportCommands.TryZoomSelection(document))
                {
                    _viewModel.Status = "Đã chọn cấu kiện đích + nguyên nhân nhưng chưa thể zoom • " + deduction.RegionKey;
                    return;
                }

                _viewModel.Status = "Đã chọn/zoom cấu kiện đích + nguyên nhân • " + deduction.RegionKey;
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Không thể định vị dòng khấu trừ: " + ex.Message;
            }
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