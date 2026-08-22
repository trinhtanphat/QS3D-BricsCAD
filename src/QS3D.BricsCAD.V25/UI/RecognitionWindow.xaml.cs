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
        private readonly Action<RecognitionResult>? _apply;
        private readonly Action<RecognitionResult>? _locate;
        private readonly Document? _document;

        public RecognitionWindow(IReadOnlyList<RecognitionResult> rows, Action<RecognitionResult>? apply = null, Action<RecognitionResult>? locate = null)
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _apply = apply;
            _locate = locate;
            _document = BcadApplication.DocumentManager.MdiActiveDocument;
            InitializeComponent();
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

        private void OnApplyClick(object sender, RoutedEventArgs e) => Apply(Grid.SelectedItems.Cast<RecognitionResult>().ToList());
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) { if (Grid.SelectedItem is RecognitionResult row) Apply(new[] { row }); }
        private void OnApplyConfidentClick(object sender, RoutedEventArgs e) => Apply(_rows.Where(x => x.TopCandidate != null && x.Confidence >= .92d && x.Margin >= .15d).ToList());

        private void Apply(IEnumerable<RecognitionResult> rows)
        {
            if (_apply == null) return;
            try
            {
                EnsureActiveDocument();
            }
            catch (Exception ex)
            {
                RefreshStatus(0, 1, ex.Message);
                return;
            }

            var applied = 0;
            var failed = 0;
            string? firstError = null;
            foreach (var row in rows)
            {
                if (row.TopCandidate == null) continue;
                try
                {
                    _apply(row);
                    applied++;
                }
                catch (Exception ex)
                {
                    failed++;
                    if (firstError == null) firstError = ex.Message;
                }
            }
            RefreshStatus(applied, failed, firstError);
        }

        private void EnsureActiveDocument()
        {
            if (_document == null || !ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document))
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
