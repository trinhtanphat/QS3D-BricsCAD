using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class AuditLogWindow : Window
    {
        private readonly Document _document;
        private IReadOnlyList<AuditEvent> _rows = Array.Empty<AuditEvent>();

        public AuditLogWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Activated += (_, __) => Reload();
            Reload();
        }

        private void Reload()
        {
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                {
                    _rows = Array.Empty<AuditEvent>();
                    if (Grid != null) Grid.ItemsSource = _rows;
                    if (Summary != null) Summary.Text = "Chưa có QS3D project hiện hữu; Audit Log không tạo project mới.";
                    Title = "QS3D • Nhật ký thay đổi • " + DrawingLabel(_document);
                    return;
                }

                _rows = project.AuditEvents
                    .Where(x => x != null)
                    .OrderByDescending(x => x.Utc)
                    .ToList();
                Title = "QS3D • Nhật ký thay đổi • " + DrawingLabel(_document);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _rows = Array.Empty<AuditEvent>();
                if (Grid != null) Grid.ItemsSource = _rows;
                if (Summary != null) Summary.Text = "Không đọc được audit: " + ex.Message;
            }
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

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu";
            try { return System.IO.Path.GetFileName(name); }
            catch { return name; }
        }
    }
}
