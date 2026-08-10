using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using QS3D.Core.Revisions;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RevisionWindow : Window
    {
        private readonly IReadOnlyList<QuantityRevisionRow> _rows;
        private readonly Action<QuantityRevisionRow>? _locate;
        private readonly Document _document;

        public RevisionWindow(Document document, RevisionSnapshot before, RevisionSnapshot after, IReadOnlyList<QuantityRevisionRow> rows, Action<QuantityRevisionRow>? locate = null)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _locate = locate;
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Grid.ItemsSource = _rows;
            Header.Text = before.Id + " → " + after.Id;
            var summary = new QuantityRevisionReport().Summarize(_rows);
            Totals.Text = _rows.Count + " thay đổi • " + string.Join("  |  ", summary.Take(5).Select(x => x.QuantityName + " Δ " + x.Delta.ToString("0.###")));
        }

        private void OnLocateClick(object sender, RoutedEventArgs e) => Locate();
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => Locate();

        private void Locate()
        {
            if (_locate == null || !(Grid.SelectedItem is QuantityRevisionRow row)) return;
            try
            {
                EnsureActive();
                _locate(row);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể định vị Revision: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EnsureActive()
        {
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Cửa sổ Revision này thuộc một DWG khác. Hãy kích hoạt lại đúng bản vẽ trước khi định vị.");
        }
    }
}