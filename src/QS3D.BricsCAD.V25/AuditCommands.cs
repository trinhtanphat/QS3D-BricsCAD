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
        private static IntPtr _nativeDatabaseIdentity;

        [CommandMethod("QS3DAUDIT", CommandFlags.Modal)]
        public void ShowAuditLog()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!PrepareUnpublishedCandidate())
                {
                    const string blockedStatus = "Nhật ký thay đổi lỗi: cửa sổ chưa publish trước đó chưa thể đóng an toàn.";
                    try { document.Editor.WriteMessage("\nQS3DAUDIT: candidate chưa publish trước đó chưa đạt terminal Closed; không mở thêm cửa sổ."); } catch { }
                    try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    return;
                }

                var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(nativeDatabaseIdentity))
                {
                    const string blockedStatus = "Nhật ký thay đổi đang thuộc bản vẽ khác và chưa thể đóng an toàn.";
                    try { document.Editor.WriteMessage("\nQS3DAUDIT: cửa sổ hiện tại chưa đạt terminal Closed; không mở bản sao thứ hai."); } catch { }
                    try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    return;
                }

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    var reusedStatus = ProjectContextCoordinator.TryGetReadOnly(document, out var existingProject)
                        ? "Đã kích hoạt Nhật ký thay đổi hiện có • " + existingProject.AuditEvents.Count + " sự kiện."
                        : "Đã kích hoạt Nhật ký thay đổi hiện có • chưa có QS3D project hiện hữu; không tạo project mới.";
                    try { PaletteCoordinator.SetStatus(reusedStatus); } catch { }
                    return;
                }

                var hasProject = ProjectContextCoordinator.TryGetReadOnly(document, out var project);
                var candidate = new AuditLogWindow(document);
                candidate.Closed += (_, __) => ReleaseCandidate(candidate);
                _unpublishedCandidate = candidate;
                try
                {
                    Application.ShowModelessWindow(IntPtr.Zero, candidate, true);
                }
                catch (System.Exception)
                {
                    if (!CloseUnpublishedCandidate(candidate))
                    {
                        const string blockedStatus = "Nhật ký thay đổi lỗi: cửa sổ chưa publish không thể đóng an toàn.";
                        try { document.Editor.WriteMessage("\nQS3DAUDIT: candidate chưa publish chưa đạt terminal Closed; không mở thêm cửa sổ."); } catch { }
                        try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                        return;
                    }

                    const string showFailure = "Nhật ký thay đổi lỗi: không thể mở nhật ký thay đổi.";
                    try { document.Editor.WriteMessage("\nQS3DAUDIT error: không thể mở nhật ký thay đổi."); } catch { }
                    try { PaletteCoordinator.SetStatus(showFailure); } catch { }
                    return;
                }

                if (!candidate.IsLoaded)
                {
                    if (!CloseUnpublishedCandidate(candidate))
                    {
                        const string blockedStatus = "Nhật ký thay đổi lỗi: cửa sổ chưa publish không thể đóng an toàn.";
                        try { document.Editor.WriteMessage("\nQS3DAUDIT: candidate chưa publish chưa đạt terminal Closed; không mở thêm cửa sổ."); } catch { }
                        try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    }
                    return;
                }

                if (candidate.IsLoaded)
                {
                    _window = candidate;
                    _nativeDatabaseIdentity = nativeDatabaseIdentity;
                    if (ReferenceEquals(_unpublishedCandidate, candidate))
                        _unpublishedCandidate = null;
                }

                var status = hasProject
                    ? "Đã mở Nhật ký thay đổi • " + project.AuditEvents.Count + " sự kiện."
                    : "Đã mở Nhật ký thay đổi • chưa có QS3D project hiện hữu; không tạo project mới.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
            }
            catch (System.Exception)
            {
                const string status = "Nhật ký thay đổi lỗi: không thể mở nhật ký thay đổi.";
                try { document.Editor.WriteMessage("\nQS3DAUDIT error: không thể mở nhật ký thay đổi."); } catch { }
                try { PaletteCoordinator.SetStatus(status); } catch { }
            }
        }

        private static bool PrepareUnpublishedCandidate()
        {
            var candidate = _unpublishedCandidate;
            if (candidate == null) return true;
            return CloseUnpublishedCandidate(candidate);
        }

        private static bool PreparePublishedWindow(IntPtr requestedNativeDatabaseIdentity)
        {
            var published = _window;
            if (published == null) return true;

            if (!published.IsLoaded)
            {
                ReleaseCandidate(published);
                return true;
            }

            if (_nativeDatabaseIdentity == requestedNativeDatabaseIdentity)
                return true;

            try
            {
                published.Close();
            }
            catch
            {
                return false;
            }

            if (published.IsLoaded)
                return false;

            ReleaseCandidate(published);
            return true;
        }

        private static bool CloseUnpublishedCandidate(AuditLogWindow candidate)
        {
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
    }
}
