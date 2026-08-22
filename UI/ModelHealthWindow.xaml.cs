using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class ModelHealthWindow : Window
    {
        private readonly Action<ModelHealthIssue>? _locate;
        private readonly Document _document;

        public ModelHealthWindow(Document document, IReadOnlyList<ModelHealthIssue> issues, Action<ModelHealthIssue>? locate = null)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            _locate = locate;
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
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
                EnsureActive();
                _locate(issue);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không thể định vị Model Health: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EnsureActive()
        {
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Cửa sổ Model Health này thuộc một DWG khác. Hãy kích hoạt lại đúng bản vẽ trước khi định vị.");
        }
    }
}