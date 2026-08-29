using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Revisions;
using QS3D.Core.Services;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RevisionWindow : Window
    {
        private readonly IReadOnlyList<QuantityRevisionRow> _rows;
        private readonly SemanticChangeReview _semanticReview;
        private readonly RevisionSnapshot _afterSnapshot;
        private readonly IntPtr _nativeDatabaseIdentity;
        private readonly bool _canLocate;
        private bool _staleSnapshot;

        public RevisionWindow(Document document, RevisionSnapshot before, RevisionSnapshot after, IReadOnlyList<QuantityRevisionRow> rows, Action<QuantityRevisionRow>? locate = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            _nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
            if (before == null) throw new ArgumentNullException(nameof(before));
            _afterSnapshot = after ?? throw new ArgumentNullException(nameof(after));
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            // The caller-supplied callback historically closed over the command's managed Document
            // wrapper. Keep only whether Locate was enabled; execute the equivalent locate workflow
            // below against a freshly resolved live wrapper for the bound native database.
            _canLocate = locate != null;
            _semanticReview = new SemanticChangeReviewBuilder().Build(before, _afterSnapshot);
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, document);
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

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            if (document.IsDisposed)
                throw new InvalidOperationException("Revision cần một BricsCAD document còn hoạt động.");
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Revision cần BricsCAD document database.");
            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Revision cần native BricsCAD database còn hoạt động.");
            return identity;
        }

        private bool MatchesNativeDatabase(Document document)
        {
            if (document == null || document.IsDisposed) return false;
            try
            {
                var database = document.Database;
                return database != null &&
                       database.UnmanagedObject != IntPtr.Zero &&
                       database.UnmanagedObject == _nativeDatabaseIdentity;
            }
            catch
            {
                return false;
            }
        }

        private bool TryGetBoundActiveDocument(out Document document)
        {
            document = BcadApplication.DocumentManager.MdiActiveDocument!;
            if (document == null || !MatchesNativeDatabase(document))
            {
                document = null!;
                return false;
            }
            return true;
        }

        private Document RequireBoundActiveDocument()
        {
            if (TryGetBoundActiveDocument(out var document)) return document;
            throw new InvalidOperationException("Cửa sổ Revision này thuộc một DWG khác. Hãy kích hoạt lại đúng bản vẽ trước khi định vị.");
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
            if (!_canLocate || !(Grid.SelectedItem is QuantityRevisionRow row)) return;
            Locate(row);
        }

        private void LocateSemantic()
        {
            if (!_canLocate || !(SemanticGrid.SelectedItem is SemanticChangeReviewElement row)) return;
            Locate(new QuantityRevisionRow { ElementId = row.ElementId, Category = row.Category, Change = row.Change });
        }

        private void Locate(QuantityRevisionRow row)
        {
            try
            {
                var document = EnsureActiveAndCurrent();
                LocateCurrentElement(document, row);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể định vị Revision: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private Document EnsureActiveAndCurrent()
        {
            var document = RequireBoundActiveDocument();
            RefreshSnapshotFreshness(document);
            if (_staleSnapshot)
                throw new InvalidOperationException("Snapshot Revision đã cũ vì semantic project của DWG này đã thay đổi. Đóng cửa sổ và chạy lại QS3DREVDIFF trước khi định vị.");
            return document;
        }

        private void RefreshSnapshotFreshness()
        {
            if (_staleSnapshot) return;
            // Switching to another DWG is temporary, not evidence that this snapshot is stale.
            // DocumentBoundWindowLifetime blocks interaction; refresh only when the bound DWG is active.
            if (!TryGetBoundActiveDocument(out var document)) return;
            RefreshSnapshotFreshness(document);
        }

        private void RefreshSnapshotFreshness(Document document)
        {
            if (_staleSnapshot) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject))
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

        private static void LocateCurrentElement(Document document, QuantityRevisionRow row)
        {
            if (string.IsNullOrWhiteSpace(row.ElementId))
                throw new InvalidOperationException("Revision Locate: dòng review không có ElementId hợp lệ.");

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject))
                throw new InvalidOperationException("Revision Locate: QS3D project hiện hành không còn khả dụng. Hãy làm mới bảng review.");
            var element = currentProject.FindElement(row.ElementId)
                ?? throw new InvalidOperationException("Revision Locate: cấu kiện " + row.ElementId + " không còn tồn tại trong project hiện tại. Hãy làm mới bảng review.");
            var handles = SourceHandleResolver.Resolve(currentProject, new[] { element.Id });
            if (handles.Count == 0)
                throw new InvalidOperationException("Revision Locate: cấu kiện " + element.Id + " không còn CAD source handle hợp lệ trong project hiện tại.");

            var count = CadHandleService.Select(document, handles);
            if (count == 0)
                throw new InvalidOperationException("Revision Locate: CAD source của cấu kiện " + element.Id + " không còn tồn tại trong bản vẽ hiện tại.");
            document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
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
