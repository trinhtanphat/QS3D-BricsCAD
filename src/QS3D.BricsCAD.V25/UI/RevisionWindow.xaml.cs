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
        private readonly RevisionSnapshot _afterSnapshot;
        private readonly Action<QuantityRevisionRow>? _locate;
        private readonly Document _document;
        private bool _staleSnapshot;

        public RevisionWindow(Document document, RevisionSnapshot before, RevisionSnapshot after, IReadOnlyList<QuantityRevisionRow> rows, Action<QuantityRevisionRow>? locate = null)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            if (before == null) throw new ArgumentNullException(nameof(before));
            _afterSnapshot = after ?? throw new ArgumentNullException(nameof(after));
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _locate = locate;
            _semanticReview = new SemanticChangeReviewBuilder().Build(before, _afterSnapshot);
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Activated += (_, __) => RefreshSnapshotFreshness();
            Grid.ItemsSource = _rows;
            SemanticGrid.ItemsSource = _semanticReview.Elements;
            Header.Text = before.Id + " → " + _afterSnapshot.Id + " • " + _semanticReview.Elements.Count + " semantic element change(s)";
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
                EnsureActiveAndCurrent();
                _locate?.Invoke(row);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể định vị Revision: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EnsureActiveAndCurrent()
        {
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Cửa sổ Revision này thuộc một DWG khác. Hãy kích hoạt lại đúng bản vẽ trước khi định vị.");
            RefreshSnapshotFreshness();
            if (_staleSnapshot)
                throw new InvalidOperationException("Snapshot Revision đã cũ vì semantic project của DWG này đã thay đổi. Đóng cửa sổ và chạy lại QS3DREVDIFF trước khi định vị.");
        }

        private void RefreshSnapshotFreshness()
        {
            if (_staleSnapshot) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var currentProject))
                {
                    MarkSnapshotStale("QS3D project hiện hành không còn khả dụng.");
                    return;
                }

                var revisionService = new RevisionService();
                var liveSnapshot = revisionService.Capture(currentProject, "__revision_window_live__");
                if (revisionService.Compare(_afterSnapshot, liveSnapshot).Count == 0) return;
                MarkSnapshotStale("Semantic element/source state đã thay đổi kể từ lúc Revision diff được tạo.");
            }
            catch (Exception ex)
            {
                MarkSnapshotStale("Không thể xác nhận semantic snapshot hiện hành: " + ex.Message);
            }
        }

        private void MarkSnapshotStale(string reason)
        {
            if (_staleSnapshot) return;
            _staleSnapshot = true;
            if (Grid != null) Grid.IsEnabled = false;
            if (SemanticGrid != null) SemanticGrid.IsEnabled = false;
            if (Totals != null)
                Totals.Text = "SNAPSHOT ĐÃ CŨ • " + reason + " Đóng cửa sổ và chạy lại QS3DREVDIFF.";
        }
    }
}
