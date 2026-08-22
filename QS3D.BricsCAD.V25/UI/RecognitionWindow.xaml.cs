using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using QS3D.Core.Recognition;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RecognitionWindow : Window
    {
        private readonly IReadOnlyList<RecognitionResult> _rows; private readonly Action<RecognitionResult>? _apply; private readonly Action<RecognitionResult>? _locate;
<<<<<<< origin/main
        public RecognitionWindow(IReadOnlyList<RecognitionResult> rows, Action<RecognitionResult>? apply = null, Action<RecognitionResult>? locate = null)
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows)); _apply = apply; _locate = locate; InitializeComponent(); Grid.ItemsSource = _rows; RefreshStatus(0, 0);
        }
=======
        public RecognitionWindow(IReadOnlyList<RecognitionResult> rows, Action<RecognitionResult>? apply = null, Action<RecognitionResult>? locate = null) { _rows = rows ?? throw new ArgumentNullException(nameof(rows)); _apply = apply; _locate = locate; InitializeComponent(); Grid.ItemsSource = _rows; RefreshStatus(0); }
>>>>>>> origin/agent/full-domain-20260810
        private void OnLocateClick(object sender, RoutedEventArgs e) { if (_locate != null && Grid.SelectedItem is RecognitionResult row) _locate(row); }
        private void OnApplyClick(object sender, RoutedEventArgs e) => Apply(Grid.SelectedItems.Cast<RecognitionResult>().ToList());
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) { if (Grid.SelectedItem is RecognitionResult row) Apply(new[] { row }); }
        private void OnApplyConfidentClick(object sender, RoutedEventArgs e) => Apply(_rows.Where(x => x.TopCandidate != null && x.Confidence >= .92d && x.Margin >= .15d).ToList());
<<<<<<< origin/main
        private void Apply(IEnumerable<RecognitionResult> rows)
        {
            if (_apply == null) return; var applied = 0; var failed = 0;
            foreach (var row in rows)
            {
                if (row.TopCandidate == null) continue;
                try { _apply(row); applied++; } catch { failed++; }
            }
            RefreshStatus(applied, failed);
        }
        private void RefreshStatus(int applied, int failed)
        {
            var review = _rows.Count(x => x.RequiresReview);
            Status.Text = _rows.Count + " đối tượng • " + review + " cần review" + (applied > 0 ? " • vừa áp dụng " + applied : string.Empty) + (failed > 0 ? " • lỗi/xung đột " + failed : string.Empty);
        }
=======
        private void Apply(IEnumerable<RecognitionResult> rows) { if (_apply == null) return; var count = 0; foreach (var row in rows) { if (row.TopCandidate == null) continue; try { _apply(row); count++; } catch { } } RefreshStatus(count); }
        private void RefreshStatus(int applied) { var review = _rows.Count(x => x.RequiresReview); Status.Text = _rows.Count + " đối tượng • " + review + " cần review" + (applied > 0 ? " • vừa áp dụng " + applied : string.Empty); }
>>>>>>> origin/agent/full-domain-20260810
    }
}
