using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using QS3D.Core.Recognition;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RecognitionWindow : Window
    {
        private readonly IReadOnlyList<RecognitionResult> _rows;
        private readonly Func<IReadOnlyList<RecognitionResult>, bool, int>? _apply;
        private readonly Action<RecognitionResult>? _locate;
        private readonly Document _document;

        public RecognitionWindow(Document document, IReadOnlyList<RecognitionResult> rows, Func<IReadOnlyList<RecognitionResult>, bool, int>? apply = null, Action<RecognitionResult>? locate = null)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _apply = apply;
            _locate = locate;
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Grid.ItemsSource = _rows;
            RefreshStatus(0, 0, null);
        }

        private void OnLocateClick(object sender, RoutedEventArgs e)
        {
            if (_locate == null || !(Grid.SelectedItem is RecognitionResult row)) return;
            try
            {
                EnsureActiveDocument();
                _locate(row);
                RefreshStatus(0, 0, null);
            }
            catch (Exception ex)
            {
                RefreshStatus(0, 1, "Locate: " + ex.Message);
            }
        }

        private void OnApplyClick(object sender, RoutedEventArgs e) => Apply(Grid.SelectedItems.Cast<RecognitionResult>().ToList(), requireLiveConfidence: false);
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) { if (Grid.SelectedItem is RecognitionResult row) Apply(new[] { row }, requireLiveConfidence: false); }
        private void OnApplyConfidentClick(object sender, RoutedEventArgs e) =>
            Apply(
                _rows.Where(x => x.TopCandidate != null && x.Confidence >= .92d && x.Margin >= .15d && x.IsCaptureReady).ToList(),
                requireLiveConfidence: true);

        private void Apply(IEnumerable<RecognitionResult> rows, bool requireLiveConfidence)
        {
            if (_apply == null) return;
            IReadOnlyList<RecognitionResult> batch;
            try
            {
                EnsureActiveDocument();
                batch = rows.Where(x => x != null && x.TopCandidate != null).ToList().AsReadOnly();
            }
            catch (Exception ex)
            {
                RefreshStatus(0, 1, ex.Message);
                return;
            }
            if (batch.Count == 0) return;

            try
            {
                var applied = _apply(batch, requireLiveConfidence);
                RefreshStatus(applied, 0, null);
            }
            catch (Exception ex)
            {
                RefreshStatus(0, batch.Count, "Apply batch: " + ex.Message);
            }
        }

        private void EnsureActiveDocument()
        {
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Cửa sổ Recognition thuộc một DWG khác. Hãy quay lại DWG đã mở cửa sổ này hoặc đóng và mở lại Recognition trong DWG hiện tại.");
        }

        private void RefreshStatus(int applied, int failed, string? error)
        {
            var review = _rows.Count(x => x.RequiresReview);
            Status.Text = _rows.Count + " đối tượng • " + review + " cần review" +
                          (applied > 0 ? " • vừa áp dụng " + applied : string.Empty) +
                          (failed > 0 ? " • lỗi/xung đột " + failed : string.Empty) +
                          (!string.IsNullOrWhiteSpace(error) ? " • " + error : string.Empty);
        }
    }
}
