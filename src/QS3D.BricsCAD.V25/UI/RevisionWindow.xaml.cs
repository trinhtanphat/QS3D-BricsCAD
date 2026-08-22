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
        private readonly SemanticChangeReview _semanticReview;
        private readonly Action<QuantityRevisionRow>? _locate;
        private readonly Document _document;

        public RevisionWindow(Document document, RevisionSnapshot before, RevisionSnapshot after, IReadOnlyList<QuantityRevisionRow> rows, Action<QuantityRevisionRow>? locate = null)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _locate = locate;
            _semanticReview = new SemanticChangeReviewBuilder().Build(before, after);
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Grid.ItemsSource = _rows;
            SemanticGrid.ItemsSource = _semanticReview.Elements;
            Header.Text = before.Id + " → " + after.Id + " • " + _semanticReview.Elements.Count + " semantic element change(s)";
            var quantitySummary = new QuantityRevisionReport().Summarize(_rows);
            var semanticSummary = _semanticReview.Summary;
            Totals.Text =
                "Semantic +" + semanticSummary.AddedElementCount + " / -" + semanticSummary.RemovedElementCount + " / ~" + semanticSummary.ChangedElementCount +
                " • I/P/Q " + semanticSummary.IdentityChangeCount + "/" + semanticSummary.PropertyChangeCount + "/" + semanticSummary.QuantityChangeCount +
                " • source ref ẩn " + semanticSummary.OmittedSourceReferenceChangeCount +
                "  |  Quantity " + _rows.Count + " dòng" +
                (quantitySummary.Count == 0 ? string.Empty : " • " + string.Join("  |  ", quantitySummary.Take(4).Select(x => x.QuantityName + " Δ " + x.Delta.ToString("0.###"))));
            if (_rows.Count == 0 && _semanticReview.HasChanges) Tabs.SelectedIndex = 1;
        }

        private void OnLocateClick(object sender, RoutedEventArgs e) => LocateCurrentTab();
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => LocateQuantity();
        private void OnSemanticGridDoubleClick(object sender, MouseButtonEventArgs e) => LocateSemantic();

        private void LocateCurrentTab()
        {
            if (Tabs.SelectedIndex == 1) LocateSemantic();
            else LocateQuantity();
        }

        private void LocateQuantity()
        {
            if (_locate == null || !(Grid.SelectedItem is QuantityRevisionRow row)) return;
            Locate(row);
        }

        private void LocateSemantic()
        {
            if (_locate == null || !(SemanticGrid.SelectedItem is SemanticChangeReviewElement row)) return;
            Locate(new QuantityRevisionRow { ElementId = row.ElementId, Category = row.Category, Change = row.Change });
        }

        private void Locate(QuantityRevisionRow row)
        {
            try
            {
                EnsureActive();
                _locate?.Invoke(row);
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
