using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using QS3D.Core.Diagnostics;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class ModelHealthWindow : Window
    {
        private readonly Action<ModelHealthIssue>? _locate;
        public ModelHealthWindow(IReadOnlyList<ModelHealthIssue> issues, Action<ModelHealthIssue>? locate = null)
        {
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            _locate = locate; InitializeComponent(); IssueGrid.ItemsSource = issues;
            SummaryText.Text = issues.Count(x => x.Severity == HealthSeverity.Error) + " lỗi • " + issues.Count(x => x.Severity == HealthSeverity.Warning) + " cảnh báo • " + issues.Count(x => x.Severity == HealthSeverity.Info) + " thông tin";
        }
        private void OnLocateClick(object sender, RoutedEventArgs e) => Locate();
        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e) => Locate();
        private void Locate() { if (_locate != null && IssueGrid.SelectedItem is ModelHealthIssue issue && !string.IsNullOrWhiteSpace(issue.ElementId)) _locate(issue); }
    }
}
