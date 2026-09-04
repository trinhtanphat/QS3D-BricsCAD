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
        private readonly WeakReference<Document> _lifecycleDocument;
        private readonly IntPtr _nativeDatabaseIdentity;
        private readonly string _boundProjectId;
        private readonly string _boundDrawingFingerprint;
        private readonly string _boundDrawingLabel;
        private IReadOnlyList<AuditEvent> _rows = Array.Empty<AuditEvent>();

        public AuditLogWindow(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            _lifecycleDocument = new WeakReference<Document>(document);
            _nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
            CaptureBoundProjectAffinity(document, out _boundProjectId, out _boundDrawingFingerprint);
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
            _lifecycleDocument.TryGetTarget(out var lifecycleDocument);

            try
            {
                foreach (Document candidate in BcadApplication.DocumentManager)
                {
                    if (candidate == null || candidate.IsDisposed) continue;
                    try
                    {
                        var database = candidate.Database;
                        if (database == null || database.UnmanagedObject == IntPtr.Zero) continue;

                        // Managed reference identity is the strongest proof while the original wrapper
                        // remains live. Native identity is only a wrapper-drift candidate filter: a
                        // recycled database address must never be enough to rebind Audit Log data.
                        if (lifecycleDocument != null && ReferenceEquals(candidate, lifecycleDocument))
                        {
                            if (database.UnmanagedObject != _nativeDatabaseIdentity) continue;
                            if (HasBoundProjectContext && !MatchesBoundProjectAffinity(candidate)) continue;
                            document = candidate;
                            return true;
                        }

                        if (database.UnmanagedObject != _nativeDatabaseIdentity) continue;
                        if (!MatchesBoundProjectAffinity(candidate)) continue;
                        document = candidate;
                        return true;
                    }
                    catch
                    {
                        // One wrapper can become stale while BricsCAD is changing document state.
                        // Ignore it and continue looking for a live wrapper that proves both native
                        // candidate identity and the immutable project/drawing affinity captured here.
                    }
                }
            }
            catch
            {
                document = null!;
            }

            return false;
        }

        private bool HasBoundProjectContext =>
            !string.IsNullOrWhiteSpace(_boundProjectId) ||
            !string.IsNullOrWhiteSpace(_boundDrawingFingerprint);

        private bool MatchesBoundProjectAffinity(Document candidate)
        {
            // A window opened before a QS3D project exists has no semantic token with which to prove
            // wrapper drift. A partially captured token is not equivalent to no project: it must fail
            // closed because the original semantic identity can no longer be proven completely.
            if (string.IsNullOrWhiteSpace(_boundProjectId) ||
                string.IsNullOrWhiteSpace(_boundDrawingFingerprint))
                return false;

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(candidate, out var project)) return false;
                return string.Equals(project.ProjectId ?? string.Empty, _boundProjectId, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(project.DrawingFingerprint ?? string.Empty, _boundDrawingFingerprint, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void CaptureBoundProjectAffinity(
            Document document,
            out string projectId,
            out string drawingFingerprint)
        {
            projectId = string.Empty;
            drawingFingerprint = string.Empty;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project)) return;
                projectId = (project.ProjectId ?? string.Empty).Trim();
                drawingFingerprint = (project.DrawingFingerprint ?? string.Empty).Trim();
            }
            catch
            {
                // Audit Log remains read-only and can still bind to the exact managed wrapper. A
                // missing semantic token intentionally disables native-pointer wrapper drift.
                projectId = string.Empty;
                drawingFingerprint = string.Empty;
            }
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
