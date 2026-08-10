using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
        private readonly ProjectState _projectAtOpen;
        private bool _staleSnapshot;

        public ModelHealthWindow(Document document, IReadOnlyList<ModelHealthIssue> issues, Action<ModelHealthIssue>? locate = null)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            _locate = locate;
            _projectAtOpen = ProjectContextCoordinator.GetOrCreate(_document);
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Activated += (_, __) => RefreshSnapshotFreshness();
            IssueGrid.ItemsSource = issues;
            SummaryText.Text = issues.Count(x => x.Severity == HealthSeverity.Error) + " lỗi • " + issues.Count(x => x.Severity == HealthSeverity.Warning) + " cảnh báo • " + issues.Count(x => x.Severity == HealthSeverity.Info) + " thông tin";
        }

        private void OnLocateClick(object sender, RoutedEventArgs e) => Locate();
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => Locate();

        private void Locate()
        {
            if (_locate == null || !(IssueGrid.SelectedItem is ModelHealthIssue issue) || string.IsNullOrWhiteSpace(issue.ElementId)) return;
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
                throw new InvalidOperationException("Snapshot Model Health đã cũ vì project của DWG này đã được reload/thay thế. Đóng cửa sổ và chạy lại QS3DHEALTH hoặc QS3DHEALTHALL.");
        }

        private void RefreshSnapshotFreshness()
        {
            if (_staleSnapshot) return;
            try
            {
                var current = ProjectContextCoordinator.GetOrCreate(_document);
                if (ReferenceEquals(current, _projectAtOpen)) return;
                MarkSnapshotStale("Project của DWG đã được reload/thay thế.");
            }
            catch (Exception ex)
            {
                MarkSnapshotStale("Không thể xác nhận project hiện hành: " + ex.Message);
            }
        }

        private void MarkSnapshotStale(string reason)
        {
            if (_staleSnapshot) return;
            _staleSnapshot = true;
            if (IssueGrid != null) IssueGrid.IsEnabled = false;
            if (SummaryText != null)
                SummaryText.Text = "SNAPSHOT ĐÃ CŨ • " + reason + " Đóng cửa sổ và chạy lại Health.";
        }
    }
}
