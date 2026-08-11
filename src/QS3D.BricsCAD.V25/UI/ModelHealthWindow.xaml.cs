using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class ModelHealthWindow : Window
    {
        private readonly Action<ModelHealthIssue>? _locate;
        private readonly Document _document;
        private readonly IReadOnlyList<ModelHealthIssue> _issues;
        private readonly string _projectIdAtOpen;
        private readonly DateTime _updatedUtcAtOpen;
        private readonly long _changeVersionAtOpen;
        private readonly string _drawingFingerprintAtOpen;
        private bool _staleSnapshot;

        public ModelHealthWindow(Document document, IReadOnlyList<ModelHealthIssue> issues, Action<ModelHealthIssue>? locate = null)
            : this(document, ResolveProjectAtOpen(document), issues, locate)
        {
        }

        internal ModelHealthWindow(
            Document document,
            ProjectState projectAtOpen,
            IReadOnlyList<ModelHealthIssue> issues,
            Action<ModelHealthIssue>? locate = null)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            if (projectAtOpen == null) throw new ArgumentNullException(nameof(projectAtOpen));
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            _issues = issues.ToList();
            _locate = locate;
            _projectIdAtOpen = projectAtOpen.ProjectId;
            _updatedUtcAtOpen = projectAtOpen.UpdatedUtc;
            _changeVersionAtOpen = projectAtOpen.ChangeVersion;
            _drawingFingerprintAtOpen = projectAtOpen.DrawingFingerprint ?? string.Empty;
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Activated += (_, __) => RefreshSnapshotFreshness();
            UpdateTotalSummary();
            ApplyFilter();
        }

        private void OnLocateClick(object sender, RoutedEventArgs e) => Locate();
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => Locate();
        private void OnFilterChanged(object sender, RoutedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            if (IssueGrid == null || SearchBox == null || SeverityCombo == null || VisibleCountText == null) return;

            var query = (SearchBox.Text ?? string.Empty).Trim();
            var severity = (SeverityCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "All";
            var filtered = _issues
                .Where(issue => MatchesSeverity(issue, severity) && MatchesSearch(issue, query))
                .ToList();

            IssueGrid.ItemsSource = filtered;
            VisibleCountText.Text = filtered.Count + " / " + _issues.Count;
        }

        private static bool MatchesSeverity(ModelHealthIssue issue, string severity)
        {
            return string.Equals(severity, "All", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(issue.Severity.ToString(), severity, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesSearch(ModelHealthIssue issue, string query)
        {
            if (query.Length == 0) return true;
            return ContainsIgnoreCase(issue.Code, query) ||
                   ContainsIgnoreCase(issue.ElementId, query) ||
                   ContainsIgnoreCase(issue.Message, query);
        }

        private static bool ContainsIgnoreCase(string? value, string query)
        {
            if (value == null || value.Length == 0) return false;
            return value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void UpdateTotalSummary()
        {
            SummaryText.Text = _issues.Count(x => x.Severity == HealthSeverity.Error) + " lỗi • " +
                               _issues.Count(x => x.Severity == HealthSeverity.Warning) + " cảnh báo • " +
                               _issues.Count(x => x.Severity == HealthSeverity.Info) + " thông tin";
        }

        private void Locate()
        {
            if (_locate == null || !(IssueGrid.SelectedItem is ModelHealthIssue issue)) return;
            try
            {
                EnsureActiveAndCurrent();
                _locate(issue);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể định vị Model Health: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EnsureActiveAndCurrent()
        {
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Cửa sổ Model Health này thuộc một DWG khác. Hãy kích hoạt lại đúng bản vẽ trước khi định vị.");
            RefreshSnapshotFreshness();
            if (_staleSnapshot)
                throw new InvalidOperationException("Snapshot Model Health đã cũ vì semantic project của DWG này đã thay đổi/reload. Đóng cửa sổ và chạy lại QS3DHEALTH hoặc QS3DHEALTHALL.");
        }

        private void RefreshSnapshotFreshness()
        {
            if (_staleSnapshot) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var current))
                {
                    MarkSnapshotStale("QS3D project hiện hành không còn khả dụng.");
                    return;
                }

                if (MatchesSnapshot(current)) return;
                MarkSnapshotStale("Semantic project đã thay đổi hoặc được reload kể từ lúc Health được tạo.");
            }
            catch (Exception ex)
            {
                MarkSnapshotStale("Không thể xác nhận project hiện hành: " + ex.Message);
            }
        }

        private bool MatchesSnapshot(ProjectState current)
        {
            return string.Equals(current.ProjectId, _projectIdAtOpen, StringComparison.Ordinal) &&
                   current.UpdatedUtc == _updatedUtcAtOpen &&
                   current.ChangeVersion == _changeVersionAtOpen &&
                   string.Equals(current.DrawingFingerprint ?? string.Empty, _drawingFingerprintAtOpen, StringComparison.OrdinalIgnoreCase);
        }

        private static ProjectState ResolveProjectAtOpen(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (ProjectContextCoordinator.TryGetReadOnly(document, out var project)) return project;
            throw new InvalidOperationException("Model Health cần một QS3D project hiện hữu; cửa sổ kiểm tra không tạo project mới.");
        }

        private void MarkSnapshotStale(string reason)
        {
            if (_staleSnapshot) return;
            _staleSnapshot = true;
            if (IssueGrid != null) IssueGrid.IsEnabled = false;
            if (SearchBox != null) SearchBox.IsEnabled = false;
            if (SeverityCombo != null) SeverityCombo.IsEnabled = false;
            if (VisibleCountText != null) VisibleCountText.Text = "STALE";
            if (SummaryText != null)
                SummaryText.Text = "SNAPSHOT ĐÃ CŨ • " + reason + " Đóng cửa sổ và chạy lại Health.";
        }
    }
}
