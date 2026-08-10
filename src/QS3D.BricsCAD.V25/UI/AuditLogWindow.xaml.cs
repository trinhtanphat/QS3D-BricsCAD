using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class AuditLogWindow : Window
    {
        private readonly ProjectState _project;
        private IReadOnlyList<AuditEvent> _rows = Array.Empty<AuditEvent>();

        public AuditLogWindow(ProjectState project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            InitializeComponent();
            Reload();
        }

        private void Reload()
        {
            _rows = _project.AuditEvents
                .Where(x => x != null)
                .OrderByDescending(x => x.Utc)
                .ToList();
            ApplyFilter();
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            if (Grid == null || Summary == null) return;
            var query = (SearchBox?.Text ?? string.Empty).Trim();
            var filtered = query.Length == 0
                ? _rows
                : _rows.Where(x => Contains(x.Action, query) || Contains(x.ElementId, query) || Contains(x.Detail, query) || Contains(x.Actor, query) || Contains(x.CorrelationId, query)).ToList();
            Grid.ItemsSource = filtered;
            Summary.Text = filtered.Count + " / " + _rows.Count + " sự kiện";
        }

        private static bool Contains(string value, string query) => (value ?? string.Empty).IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }
}
