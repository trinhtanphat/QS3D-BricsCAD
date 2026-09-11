using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class AuditCommands
    {
        private static AuditLogWindow? _window;
        private static AuditLogWindow? _unpublishedCandidate;
        private static AuditLogWindow? _publicationInFlightCandidate;
        private static AuditLogWindow? _cleanupInFlightCandidate;
        private static IntPtr _nativeDatabaseIdentity;
        private static WeakReference<Document>? _publishedDocument;

        [CommandMethod("QS3DAUDIT", CommandFlags.Modal)]
        public void ShowAuditLog()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            var nativeDatabaseIdentity = IntPtr.Zero;
            try
            {
                nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;

                if (!PrepareUnpublishedCandidate())
                {
                    if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                    const string blockedStatus = "Nhật ký thay đổi lỗi: cửa sổ chưa publish trước đó chưa thể đóng an toàn.";
                    try { document.Editor.WriteMessage("\nQS3DAUDIT: candidate chưa publish trước đó chưa đạt terminal Closed; không mở thêm cửa sổ."); } catch { }
                    try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    return;
                }

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                if (!PreparePublishedWindow(document, nativeDatabaseIdentity))
                {
                    if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                    const string blockedStatus = "Nhật ký thay đổi đang thuộc bản vẽ khác và chưa thể đóng an toàn.";
                    try { document.Editor.WriteMessage("\nQS3DAUDIT: cửa sổ hiện tại chưa đạt terminal Closed; không mở bản sao thứ hai."); } catch { }
                    try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    return;
                }

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;

                    var reusedStatus = ProjectContextCoordinator.TryGetReadOnly(document, out var existingProject)
                        ? "Đã kích hoạt Nhật ký thay đổi hiện có • " + existingProject.AuditEvents.Count + " sự kiện."
                        : "Đã kích hoạt Nhật ký thay đổi hiện có • chưa có QS3D project hiện hữu; không tạo project mới.";
                    if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                    try { PaletteCoordinator.SetStatus(reusedStatus); } catch { }
                    return;
                }

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                var candidate = new AuditLogWindow(document);
                candidate.Closed += (_, __) => ReleaseCandidate(candidate);
                _unpublishedCandidate = candidate;

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    CloseUnpublishedCandidate(candidate);
                    return;
                }

                _publicationInFlightCandidate = candidate;
                try
                {
                    Application.ShowModelessWindow(IntPtr.Zero, candidate, true);
                }
                catch (System.Exception)
                {
                    if (!CloseUnpublishedCandidate(candidate))
                    {
                        if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                        const string blockedStatus = "Nhật ký thay đổi lỗi: cửa sổ chưa publish không thể đóng an toàn.";
                        try { document.Editor.WriteMessage("\nQS3DAUDIT: candidate chưa publish chưa đạt terminal Closed; không mở thêm cửa sổ."); } catch { }
                        try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                        return;
                    }

                    if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                    const string showFailure = "Nhật ký thay đổi lỗi: không thể mở nhật ký thay đổi.";
                    try { document.Editor.WriteMessage("\nQS3DAUDIT error: không thể mở nhật ký thay đổi."); } catch { }
                    try { PaletteCoordinator.SetStatus(showFailure); } catch { }
                    return;
                }
                finally
                {
                    if (ReferenceEquals(_publicationInFlightCandidate, candidate))
                        _publicationInFlightCandidate = null;
                }

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    CloseUnpublishedCandidate(candidate);
                    return;
                }

                if (!candidate.IsLoaded)
                {
                    if (!CloseUnpublishedCandidate(candidate))
                    {
                        if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                        const string blockedStatus = "Nhật ký thay đổi lỗi: cửa sổ chưa publish không thể đóng an toàn.";
                        try { document.Editor.WriteMessage("\nQS3DAUDIT: candidate chưa publish chưa đạt terminal Closed; không mở thêm cửa sổ."); } catch { }
                        try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    }
                    return;
                }

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    CloseUnpublishedCandidate(candidate);
                    return;
                }

                _window = candidate;
                _nativeDatabaseIdentity = nativeDatabaseIdentity;
                _publishedDocument = new WeakReference<Document>(document);
                if (ReferenceEquals(_unpublishedCandidate, candidate))
                    _unpublishedCandidate = null;

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                var hasProject = ProjectContextCoordinator.TryGetReadOnly(document, out var project);
                var status = hasProject
                    ? "Đã mở Nhật ký thay đổi • " + project.AuditEvents.Count + " sự kiện."
                    : "Đã mở Nhật ký thay đổi • chưa có QS3D project hiện hữu; không tạo project mới.";
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                try { PaletteCoordinator.SetStatus(status); } catch { }
            }
            catch (System.Exception)
            {
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                const string status = "Nhật ký thay đổi lỗi: không thể mở nhật ký thay đổi.";
                try { document.Editor.WriteMessage("\nQS3DAUDIT error: không thể mở nhật ký thay đổi."); } catch { }
                try { PaletteCoordinator.SetStatus(status); } catch { }
            }
        }

        private static bool PrepareUnpublishedCandidate()
        {
            if (_cleanupInFlightCandidate != null)
                return false;
            if (_publicationInFlightCandidate != null)
                return false;

            var candidate = _unpublishedCandidate;
            if (candidate == null) return true;
            return CloseUnpublishedCandidate(candidate);
        }

        private static bool PreparePublishedWindow(Document requestedDocument, IntPtr requestedNativeDatabaseIdentity)
        {
            var published = _window;
            if (published == null) return true;

            if (!published.IsLoaded)
            {
                ReleaseCandidate(published);
                return true;
            }

            if (_nativeDatabaseIdentity == requestedNativeDatabaseIdentity && PublishedDocumentMatches(requestedDocument))
                return true;

            _cleanupInFlightCandidate = published;
            try
            {
                published.Close();
            }
            catch
            {
                if (!published.IsLoaded)
                {
                    ReleaseCandidate(published);
                    return true;
                }

                return false;
            }
            finally
            {
                if (ReferenceEquals(_cleanupInFlightCandidate, published))
                    _cleanupInFlightCandidate = null;
            }

            if (published.IsLoaded)
                return false;

            ReleaseCandidate(published);
            return true;
        }

        private static bool PublishedDocumentMatches(Document document)
        {
            return document != null &&
                   _publishedDocument != null &&
                   _publishedDocument.TryGetTarget(out var publishedDocument) &&
                   ReferenceEquals(publishedDocument, document);
        }

        private static bool CloseUnpublishedCandidate(AuditLogWindow candidate)
        {
            _cleanupInFlightCandidate = candidate;
            try
            {
                candidate.Close();
            }
            catch
            {
                if (!candidate.IsLoaded)
                {
                    ReleaseCandidate(candidate);
                    return true;
                }

                _unpublishedCandidate = candidate;
                return false;
            }
            finally
            {
                if (ReferenceEquals(_cleanupInFlightCandidate, candidate))
                    _cleanupInFlightCandidate = null;
            }

            if (!candidate.IsLoaded)
            {
                ReleaseCandidate(candidate);
                return true;
            }

            _unpublishedCandidate = candidate;
            return false;
        }

        private static void ReleaseCandidate(AuditLogWindow candidate)
        {
            if (ReferenceEquals(_window, candidate))
            {
                _window = null;
                _nativeDatabaseIdentity = IntPtr.Zero;
                _publishedDocument = null;
            }

            if (ReferenceEquals(_unpublishedCandidate, candidate))
                _unpublishedCandidate = null;
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

        private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (document == null || nativeDatabaseIdentity == IntPtr.Zero) return false;
            try
            {
                var activeDocument = Application.DocumentManager.MdiActiveDocument;
                if (!ReferenceEquals(activeDocument, document)) return false;
                var database = activeDocument.Database;
                return database != null && database.UnmanagedObject != IntPtr.Zero &&
                       database.UnmanagedObject == nativeDatabaseIdentity;
            }
            catch
            {
                return false;
            }
        }
    }
}
