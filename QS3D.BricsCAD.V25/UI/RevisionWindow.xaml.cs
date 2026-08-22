using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using QS3D.Core.Revisions;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RevisionWindow : Window
    {
        private readonly IReadOnlyList<QuantityRevisionRow> _rows; private readonly Action<QuantityRevisionRow>? _locate;
<<<<<<< origin/main
        public RevisionWindow(RevisionSnapshot before, RevisionSnapshot after, IReadOnlyList<QuantityRevisionRow> rows, Action<QuantityRevisionRow>? locate = null)
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows)); _locate = locate; InitializeComponent(); Grid.ItemsSource = _rows; Header.Text = before.Id + " → " + after.Id;
            var summary = new QuantityRevisionReport().Summarize(_rows); Totals.Text = _rows.Count + " thay đổi • " + string.Join("  |  ", summary.Take(5).Select(x => x.QuantityName + " Δ " + x.Delta.ToString("0.###")));
        }
        private void OnLocateClick(object sender, RoutedEventArgs e) => Locate();
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => Locate();
        private void Locate() { if (_locate != null && Grid.SelectedItem is QuantityRevisionRow row) _locate(row); }
=======
        public RevisionWindow(RevisionSnapshot before, RevisionSnapshot after, IReadOnlyList<QuantityRevisionRow> rows, Action<QuantityRevisionRow>? locate = null) { _rows = rows ?? throw new ArgumentNullException(nameof(rows)); _locate = locate; InitializeComponent(); Grid.ItemsSource = _rows; Header.Text = before.Id + " → " + after.Id; var summary = new QuantityRevisionReport().Summarize(_rows); Totals.Text = _rows.Count + " thay đổi • " + string.Join("  |  ", summary.Take(5).Select(x => x.QuantityName + " Δ " + x.Delta.ToString("0.###"))); }
        private void OnLocateClick(object sender, RoutedEventArgs e) => Locate(); private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => Locate(); private void Locate() { if (_locate != null && Grid.SelectedItem is QuantityRevisionRow row) _locate(row); }
>>>>>>> origin/agent/full-domain-20260810
    }
}
