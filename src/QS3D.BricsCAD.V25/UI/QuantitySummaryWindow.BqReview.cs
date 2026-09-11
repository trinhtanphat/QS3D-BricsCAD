using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Review;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantitySummaryWindow
    {
        private bool _bqReviewInitialized;
        private bool _loadingBqReview;

        private void OnBqReviewPanelLoaded(object sender, RoutedEventArgs e)
        {
            if (_bqReviewInitialized) return;
            _bqReviewInitialized = true;
            BqReviewStatusCombo.ItemsSource = StatusOptions();
            QuantityGrid.SelectionChanged += OnBqReviewSelectionChanged;
            UpdateBqReviewPresentation(QuantityGrid.SelectedItem as QuantityReportRow);
        }

        private void OnBqReviewSelectionChanged(object sender, SelectionChangedEventArgs e) =>
            UpdateBqReviewPresentation(QuantityGrid.SelectedItem as QuantityReportRow);

        private void UpdateBqReviewPresentation(QuantityReportRow? row)
        {
            if (!_bqReviewInitialized || BqReviewStateText == null) return;
            _loadingBqReview = true;
            try
            {
                var detail = _detailMode && row != null && row.Count == 1 && row.ElementIds.Count == 1;
                SetBqReviewEnabled(detail);
                if (!detail)
                {
                    ClearBqReviewFields();
                    BqReviewStateText.Text = "Chọn chế độ diễn giải chi tiết và một cấu kiện để review.";
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project) || !SameProjectIdentity(project))
                {
                    ClearBqReviewFields();
                    SetBqReviewEnabled(false);
                    BqReviewStateText.Text = "Project hiện hành không còn khớp với cửa sổ BQ này.";
                    return;
                }

                var ledger = ProjectBqReviewLedger.Open(project).Current;
                var elementId = BqReviewService.ElementId(row!);
                var entry = ledger.Find(elementId);
                var current = entry != null && BqReviewService.IsCurrent(entry, row!);
                LoadEntryFields(entry, current);
                LoadMetricOptions(row!, entry, current);
                BqReviewStateText.Text = entry == null
                    ? "PENDING • chưa có review cho " + elementId
                    : current
                        ? entry.Status.ToString().ToUpperInvariant() + " • review hiện hành • " + entry.ReviewedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
                        : "STALE • số liệu BQ đã thay đổi; phải review lại trước khi adjustment/approval có hiệu lực.";
            }
            catch (Exception)
            {
                ClearBqReviewFields();
                SetBqReviewEnabled(false);
                BqReviewStateText.Text = "Không thể đọc review BQ hiện hành. Đóng bảng BQ và mở lại.";
            }
            finally { _loadingBqReview = false; }
            UpdateAdjustmentPreview();
        }

        private void LoadEntryFields(BqReviewEntry? entry, bool current)
        {
            var status = entry?.Status ?? BqReviewStatus.Pending;
            BqReviewStatusCombo.SelectedItem = StatusOptions().First(x => x.Value == status);
            BqReviewReviewerBox.Text = entry?.Reviewer ?? string.Empty;
            BqReviewNoteBox.Text = entry?.Note ?? string.Empty;
            if (!current)
            {
                BqReviewDeltaBox.Text = string.Empty;
                BqReviewReasonBox.Text = string.Empty;
            }
        }

        private void LoadMetricOptions(QuantityReportRow row, BqReviewEntry? entry, bool current)        {
            var previous = (BqReviewMetricCombo.SelectedItem as MetricOption)?.Value;
            var options = BqReviewService.AvailableMetrics(row).Select(MetricOption.For).ToList();
            BqReviewMetricCombo.ItemsSource = options;
            BqReviewMetricCombo.SelectedItem = previous.HasValue
                ? options.FirstOrDefault(x => x.Value == previous.Value) ?? options.FirstOrDefault()
                : options.FirstOrDefault();

            if (current && entry != null && BqReviewMetricCombo.SelectedItem is MetricOption selected)
            {
                var adjustment = entry.FindAdjustment(selected.Value);
                BqReviewDeltaBox.Text = adjustment == null ? string.Empty : FormatDouble(adjustment.Delta);
                BqReviewReasonBox.Text = adjustment?.Reason ?? string.Empty;
            }
            else
            {
                BqReviewDeltaBox.Text = string.Empty;
                BqReviewReasonBox.Text = string.Empty;
            }
        }

        private void OnBqReviewMetricChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingBqReview) return;
            var row = QuantityGrid.SelectedItem as QuantityReportRow;
            if (!_detailMode || row == null) { UpdateAdjustmentPreview(); return; }

            try
            {
                if (ProjectContextCoordinator.TryGetReadOnly(_document, out var project) && SameProjectIdentity(project))
                {
                    var entry = ProjectBqReviewLedger.Open(project).Current.Find(BqReviewService.ElementId(row));                    if (entry != null && BqReviewService.IsCurrent(entry, row) && BqReviewMetricCombo.SelectedItem is MetricOption metric)
                    {
                        var adjustment = entry.FindAdjustment(metric.Value);
                        BqReviewDeltaBox.Text = adjustment == null ? string.Empty : FormatDouble(adjustment.Delta);
                        BqReviewReasonBox.Text = adjustment?.Reason ?? string.Empty;
                    }
                    else
                    {
                        BqReviewDeltaBox.Text = string.Empty;
                        BqReviewReasonBox.Text = string.Empty;
                    }
                }
            }
            catch
            {
                BqReviewDeltaBox.Text = string.Empty;
                BqReviewReasonBox.Text = string.Empty;
            }
            UpdateAdjustmentPreview();
        }

        private void UpdateAdjustmentPreview()
        {
            if (BqReviewAdjustmentPreviewText == null) return;
            var row = QuantityGrid?.SelectedItem as QuantityReportRow;
            var metric = BqReviewMetricCombo?.SelectedItem as MetricOption;
            if (!_detailMode || row == null || metric == null)
            {
                BqReviewAdjustmentPreviewText.Text = "Không có metric khả dụng.";
                return;
            }
            try
            {
                var measured = BqReviewService.MeasuredValue(row, metric.Value);
                var proposed = measured;
                if (TryParseOptionalDelta(BqReviewDeltaBox.Text, out var delta)) proposed = checked(measured + delta);
                if (double.IsNaN(proposed) || double.IsInfinity(proposed))
                    throw new InvalidOperationException("Adjusted value is not finite.");
                BqReviewAdjustmentPreviewText.Text = "Đo gốc " + FormatDouble(measured) + " → proposal " + FormatDouble(proposed) +
                    " • chỉ có hiệu lực downstream khi review hiện hành ở trạng thái Approved.";
            }
            catch
            {
                BqReviewAdjustmentPreviewText.Text = "Delta/metric chưa hợp lệ; số đo gốc không bị thay đổi.";
            }
        }

        private void OnSaveBqReviewClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureCurrentProject("lưu review BQ");
                var selected = QuantityGrid.SelectedItem as QuantityReportRow
                    ?? throw new InvalidOperationException("Hãy chọn một dòng BQ chi tiết.");
                var elementId = BqReviewService.ElementId(selected);
                var project = ExistingProjectMutationContext.Require(_document, "lưu review BQ");
                EnsureProjectIdentity(project, "lưu review BQ");
                ProjectContextCoordinator.RequireBackingStoreUnchanged(_document, project, "lưu review BQ");
                var expectedVersion = project.ChangeVersion;
                var fresh = FindFreshDetailRow(elementId);
                if (project.ChangeVersion != expectedVersion)
                    throw new InvalidOperationException("Project changed while refreshing the BQ row for review.");
                ProjectContextCoordinator.RequireBackingStoreUnchanged(_document, project, "lưu review BQ");

                var ledger = ProjectBqReviewLedger.Open(project);
                var previous = ledger.Current.Find(elementId);
                var currentPrevious = previous != null && BqReviewService.IsCurrent(previous, fresh);
                var adjustments = currentPrevious
                    ? previous!.Adjustments.ToList()
                    : new List<BqManualAdjustment>();
                ApplyAdjustmentEditor(adjustments);

                var status = (BqReviewStatusCombo.SelectedItem as StatusOption)?.Value
                    ?? throw new InvalidOperationException("Hãy chọn trạng thái review.");
                var note = CanonicalOptional(BqReviewNoteBox.Text);
                var reviewer = CanonicalOptional(BqReviewReviewerBox.Text);
                var entry = BqReviewService.CreateEntry(
                    fresh,
                    status,
                    note,
                    reviewer,
                    DateTime.UtcNow,
                    adjustments);

                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    ledger.Upsert(entry);
                    AuditTrail.ForProject(project).Record(
                        "bq.review.upsert",
                        elementId,
                        "status=" + status + "; adjustments=" + entry.Adjustments.Count.ToString(CultureInfo.InvariantCulture),
                        reviewer);
                    ProjectContextCoordinator.Save(_document);
                }
                catch (Exception saveFailure)
                {
                    RestoreBqReviewAfterFailure(project, rollback, saveFailure);
                    throw;
                }
                UpdateBqReviewPresentation(fresh);
                try { PaletteCoordinator.SetStatus("Đã lưu review BQ cho " + elementId + "."); } catch { }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    this,
                    "Không thể lưu review BQ. Số đo gốc không bị thay đổi; hãy kiểm tra trạng thái, delta/lý do và thử lại.",
                    "QS3D",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                UpdateBqReviewPresentation(QuantityGrid.SelectedItem as QuantityReportRow);
            }
        }

        private void OnClearBqReviewClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureCurrentProject("xóa review BQ");
                var selected = QuantityGrid.SelectedItem as QuantityReportRow
                    ?? throw new InvalidOperationException("Hãy chọn một dòng BQ chi tiết.");
                var elementId = BqReviewService.ElementId(selected);
                var project = ExistingProjectMutationContext.Require(_document, "xóa review BQ");
                EnsureProjectIdentity(project, "xóa review BQ");
                ProjectContextCoordinator.RequireBackingStoreUnchanged(_document, project, "xóa review BQ");
                var ledger = ProjectBqReviewLedger.Open(project);
                if (ledger.Current.Find(elementId) == null)
                {
                    UpdateBqReviewPresentation(selected);
                    return;
                }
                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    if (!ledger.Remove(elementId)) return;
                    AuditTrail.ForProject(project).Record("bq.review.remove", elementId, "BQ review removed.");
                    ProjectContextCoordinator.Save(_document);
                }
                catch (Exception saveFailure)
                {
                    RestoreBqReviewAfterFailure(project, rollback, saveFailure);
                    throw;
                }

                UpdateBqReviewPresentation(selected);
                try { PaletteCoordinator.SetStatus("Đã xóa review BQ cho " + elementId + "."); } catch { }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    this,
                    "Không thể xóa review BQ. Project đã được giữ/khôi phục về trạng thái an toàn.",
                    "QS3D",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                UpdateBqReviewPresentation(QuantityGrid.SelectedItem as QuantityReportRow);
            }
        }

        private QuantityReportRow FindFreshDetailRow(string elementId)
        {
            var rows = RecalculateDetailRows();
            return rows.SingleOrDefault(x => x.Count == 1 && x.ElementIds.Count == 1 &&
                string.Equals(x.ElementIds[0], elementId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Cấu kiện không còn tồn tại trong BQ chi tiết hiện hành.");
        }
        private void ApplyAdjustmentEditor(List<BqManualAdjustment> adjustments)
        {
            var metric = BqReviewMetricCombo.SelectedItem as MetricOption;
            if (metric == null) return;
            adjustments.RemoveAll(x => x.Metric == metric.Value);

            var deltaText = (BqReviewDeltaBox.Text ?? string.Empty).Trim();
            var reason = CanonicalOptional(BqReviewReasonBox.Text);
            if (deltaText.Length == 0)
            {
                if (reason.Length != 0)
                    throw new InvalidOperationException("Lý do adjustment phải để trống khi không có delta.");
                return;
            }

            var delta = ParseFiniteDouble(deltaText, "BQ adjustment delta");
            if (delta == 0d)
                throw new InvalidOperationException("BQ adjustment delta phải khác 0 hoặc để trống để xóa adjustment.");
            if (reason.Length == 0)
                throw new InvalidOperationException("BQ adjustment cần lý do rõ ràng.");
            adjustments.Add(new BqManualAdjustment(metric.Value, delta, reason));
        }

        private void RestoreBqReviewAfterFailure(ProjectState project, ProjectStateSnapshot rollback, Exception saveFailure)
        {
            try { rollback.Restore(project); }
            catch (Exception rollbackFailure)
            {
                ProjectContextCoordinator.Forget(_document);
                throw new InvalidOperationException(
                    "BQ review save failed and rollback could not restore the in-memory project.",
                    new AggregateException(saveFailure, rollbackFailure));
            }
            ProjectContextCoordinator.Forget(_document);
        }
        private void SetBqReviewEnabled(bool enabled)
        {
            BqReviewStatusCombo.IsEnabled = enabled;
            BqReviewReviewerBox.IsEnabled = enabled;
            BqReviewNoteBox.IsEnabled = enabled;
            BqReviewMetricCombo.IsEnabled = enabled;
            BqReviewDeltaBox.IsEnabled = enabled;
            BqReviewReasonBox.IsEnabled = enabled;
            BqReviewPanel.IsEnabled = enabled;
        }

        private void ClearBqReviewFields()
        {
            BqReviewStatusCombo.SelectedIndex = -1;
            BqReviewReviewerBox.Text = string.Empty;
            BqReviewNoteBox.Text = string.Empty;
            BqReviewMetricCombo.ItemsSource = null;
            BqReviewDeltaBox.Text = string.Empty;
            BqReviewReasonBox.Text = string.Empty;
            BqReviewAdjustmentPreviewText.Text = string.Empty;
        }

        private static string CanonicalOptional(string? value)
        {
            value = value ?? string.Empty;
            if (value.Length == 0) return string.Empty;
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Review text must not contain leading or trailing whitespace.");
            return value;
        }

        private static double ParseFiniteDouble(string text, string label)
        {
            double value;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                throw new FormatException(label + " must be a valid number.");            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new FormatException(label + " must be finite.");
            return value == 0d ? 0d : value;
        }

        private static string FormatDouble(double value) =>
            value.ToString("0.############################", CultureInfo.InvariantCulture);

        private static IReadOnlyList<StatusOption> StatusOptions() => new[]
        {
            new StatusOption(BqReviewStatus.Pending, "Pending"),
            new StatusOption(BqReviewStatus.Reviewed, "Reviewed"),
            new StatusOption(BqReviewStatus.Approved, "Approved"),
            new StatusOption(BqReviewStatus.Rejected, "Rejected")
        };

        private sealed class StatusOption
        {
            internal StatusOption(BqReviewStatus value, string label) { Value = value; Label = label; }
            internal BqReviewStatus Value { get; }
            private string Label { get; }
            public override string ToString() => Label;
        }

        private sealed class MetricOption
        {
            internal BqReviewMetric Value { get; private set; }
            private string Label { get; set; } = string.Empty;
            internal static MetricOption For(BqReviewMetric value) => new MetricOption { Value = value, Label = MetricLabel(value) };
            public override string ToString() => Label;
        }

        private static string MetricLabel(BqReviewMetric metric)
        {
            switch (metric)
            {
                case BqReviewMetric.GrossConcreteM3: return "BT gộp (m³)";
                case BqReviewMetric.DeductionM3: return "Trừ giao (m³)";
                case BqReviewMetric.NetConcreteM3: return "BT còn (m³)";
                case BqReviewMetric.FormworkM2: return "Cốp pha (m²)";
                case BqReviewMetric.LengthM: return "Dài (m)";
                case BqReviewMetric.OuterPerimeterM: return "Chu vi ngoài (m)";
                case BqReviewMetric.InnerPerimeterM: return "Chu vi trong (m)";
                case BqReviewMetric.DoorAreaM2: return "DT cửa (m²)";
                case BqReviewMetric.SideAreaM2: return "Thành bên (m²)";
                case BqReviewMetric.BottomAreaM2: return "DT đáy (m²)";
                case BqReviewMetric.TopAreaM2: return "DT đỉnh (m²)";
                case BqReviewMetric.OtherAreaM2: return "DT khác (m²)";
                case BqReviewMetric.WidthM: return "Rộng (m)";
                case BqReviewMetric.HeightM: return "Cao (m)";
                case BqReviewMetric.MassKg: return "Khối lượng (kg)";
                default: return metric.ToString();
            }
        }
    }
}
