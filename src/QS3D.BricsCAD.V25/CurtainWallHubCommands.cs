using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CurtainWallHubCommands
    {
        private static CurtainWallWindow? _window;
        private static Document? _document;
        private static IntPtr _nativeDatabaseIdentity;
        private static CurtainWallWindow? _pendingWindow;
        private static Document? _pendingDocument;
        private static IntPtr _pendingNativeDatabaseIdentity;

        [CommandMethod("QS3DCURTAIN", CommandFlags.Modal)]
        public void ShowCurtainWallHub()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            CurtainWallWindow? candidate = null;
            try
            {
                var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(document, nativeDatabaseIdentity))
                {
                    ReportBlocked(document, "Vách Kính Hub hiện tại chưa thể đóng an toàn; không mở bản sao thứ hai.",
                        "QS3DCURTAIN: cửa sổ hiện tại chưa đạt terminal Closed; không mở bản sao thứ hai.");
                    return;
                }

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    TrySetStatus("Vách Kính Hub hiện có đã được kích hoạt cho đúng bản vẽ.");
                    return;
                }

                candidate = new CurtainWallWindow(document);
                candidate.Closed += (_, __) => ReleaseOwnedWindow(candidate);
                if (!ReservePendingWindow(candidate, document, nativeDatabaseIdentity))
                {
                    ReportBlocked(document, "Vách Kính Hub đang được host publish; không mở bản sao thứ hai.",
                        "QS3DCURTAIN: một cửa sổ đang trong giai đoạn publish; không mở bản sao thứ hai.");
                    TryClose(candidate);
                    return;
                }

                Application.ShowModelessWindow(IntPtr.Zero, candidate, true);
                if (!candidate.IsLoaded)
                {
                    ReleaseOwnedWindow(candidate);
                    return;
                }

                if (!PromotePendingWindow(candidate, document, nativeDatabaseIdentity))
                {
                    ReleaseOwnedWindow(candidate);
                    TryClose(candidate);
                    ReportBlocked(document, "Vách Kính Hub không thể xác nhận owner sau khi host publish; cửa sổ ứng viên đã được hủy.",
                        "QS3DCURTAIN: publication owner changed; ứng viên đã được hủy an toàn.");
                    return;
                }

                candidate = null;
                TrySetStatus("Vách Kính Hub: Family • panel grid • schedule • workflow 3D.");
            }
            catch (System.Exception)
            {
                if (candidate != null)
                {
                    ReleaseOwnedWindow(candidate);
                    TryClose(candidate);
                }
                ReportFailure(document);
            }
        }

        private static bool PreparePublishedWindow(Document requestedDocument, IntPtr requestedNativeDatabaseIdentity)
        {
            var published = _window;
            if (published == null) return true;

            if (!published.IsLoaded)
            {
                ReleaseOwnedWindow(published);
                return true;
            }

            if (_nativeDatabaseIdentity == requestedNativeDatabaseIdentity && ReferenceEquals(_document, requestedDocument))
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

            ReleaseOwnedWindow(published);
            return true;
        }

        private static bool ReservePendingWindow(CurtainWallWindow candidate, Document document, IntPtr nativeDatabaseIdentity)
        {
            if (_pendingWindow != null)
                return false;

            _pendingWindow = candidate;
            _pendingDocument = document;
            _pendingNativeDatabaseIdentity = nativeDatabaseIdentity;
            return true;
        }

        private static bool PromotePendingWindow(CurtainWallWindow candidate, Document document, IntPtr nativeDatabaseIdentity)
        {
            if (!ReferenceEquals(_pendingWindow, candidate) ||
                !ReferenceEquals(_pendingDocument, document) ||
                _pendingNativeDatabaseIdentity != nativeDatabaseIdentity ||
                _window != null)
                return false;

            _pendingWindow = null;
            _pendingDocument = null;
            _pendingNativeDatabaseIdentity = IntPtr.Zero;
            _window = candidate;
            _document = document;
            _nativeDatabaseIdentity = nativeDatabaseIdentity;
            return true;
        }

        private static void ReleaseOwnedWindow(CurtainWallWindow window)
        {
            if (ReferenceEquals(_pendingWindow, window))
            {
                _pendingWindow = null;
                _pendingDocument = null;
                _pendingNativeDatabaseIdentity = IntPtr.Zero;
            }

            if (!ReferenceEquals(_window, window)) return;
            _window = null;
            _document = null;
            _nativeDatabaseIdentity = IntPtr.Zero;
        }

        private static void TryClose(CurtainWallWindow window)
        {
            try { if (window.IsLoaded) window.Close(); } catch { }
        }

        private static void ReportBlocked(Document document, string status, string editorMessage)
        {
            TrySetStatus(status);
            TryWrite(document, "\n" + editorMessage);
        }

        private static void ReportFailure(Document document)
        {
            const string message = "QS3DCURTAIN lỗi: không thể mở Vách Kính Hub; kiểm tra document/CAD state và thử lại.";
            TrySetStatus(message);
            TryWrite(document, "\n" + message);
        }

        private static void TrySetStatus(string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }

        private static void TryWrite(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Vách Kính Hub requires a BricsCAD document database.");

            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Vách Kính Hub requires a live native BricsCAD database.");
            return identity;
        }
    }
}