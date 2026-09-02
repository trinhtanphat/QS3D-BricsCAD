using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class AuditLogWindow : Window
    {
        private readonly IntPtr _nativeDatabaseIdentity;
        private readonly string _boundDrawingLabel;
        private IReadOnlyList<AuditEvent> _rows = Array.Empty<AuditEvent>();

        public AuditLogWindow(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            _nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
            _boundDrawingLabel = DrawingLabel(document);
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, document);
            Activated += (_, __) => Reload();
            Reload();
        }

        private void Reload()
        {
            try
            {
                if (!TryResolveBoundDocument(out var document))
                {
                    ClearProjection("Bản vẽ gắn với Audit Log không còn khả dụng; cửa sổ đang chờ lifecycle host đóng an toàn.");
                    Title = "QS3D • Nhật ký thay đổi • " + _boundDrawingLabel;
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    ClearProjection("Chưa có QS3D project hiện hữu; Audit Log không tạo project mới.");
                    Title = "QS3D • Nhật ký thay đổi • " + DrawingLabel(document);
                    return;
                }

                _rows = project.AuditEvents
                    .Where(x => x != null)
                    .OrderByDescending(x => x.Utc)
                    .ToList();
                Title = "QS3D • Nhật ký thay đổi • " + DrawingLabel(document);
                ApplyFilter();
            }
            catch (Exception)
            {
                ClearProjection("Không đọc được Audit Log. Vui lòng thử lại.");
            }
        }

        private bool TryResolveBoundDocument(out Document document)
        {
            document = null!;
            try
            {
                foreach (Document candidate in BcadApplication.DocumentManager)
                {
                    if (candidate == null || candidate.IsDisposed) continue;
                    try
                    {
                        var database = candidate.Database;
                        if (database == null || database.UnmanagedObject == IntPtr.Zero) continue;
                        if (database.UnmanagedObject != _nativeDatabaseIdentity) continue;
                        document = candidate;
                        return true;
                    }
                    catch
                    {
                        // One wrapper can become stale while BricsCAD is changing document state.
                        // Ignore it and continue looking for the live wrapper of the bound database.
                    }
                }
            }
            catch
            {
                document = null!;
            }

            return false;
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Audit Log requires a BricsCAD document database.");

            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Audit Log requires a live native BricsCAD database.");
            return identity;
        }

        private void ClearProjection(string message)
        {
            _rows = Array.Empty<AuditEvent>();
            if (Grid != null) Grid.ItemsSource = _rows;
            if (Summary != null) Summary.Text = message;
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
