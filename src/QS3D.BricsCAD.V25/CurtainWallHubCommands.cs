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
            var nativeDatabaseIdentity = IntPtr.Zero;
            try
            {
                nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;

                if (!PreparePublishedWindow(document, nativeDatabaseIdentity))
                {
                    ReportBlocked(document, nativeDatabaseIdentity,
                        "Vách Kính Hub hiện tại chưa thể đóng an toàn; không mở bản sao thứ hai.",
                        "QS3DCURTAIN: cửa sổ hiện tại chưa đạt terminal Closed; không mở bản sao thứ hai.");
                    return;
                }

                // Closing a retained pending/published owner can pump MDI work.
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                        TrySetStatus("Vách Kính Hub hiện có đã được kích hoạt cho đúng bản vẽ.");
                    return;
                }

                candidate = new CurtainWallWindow(document);
                var ownedCandidate = candidate;
                candidate.Closed += (_, __) => ReleaseOwnedWindow(ownedCandidate);
                if (!ReservePendingWindow(candidate, document, nativeDatabaseIdentity))
                {
                    ReportBlocked(document, nativeDatabaseIdentity,
                        "Vách Kính Hub đang được host publish; không mở bản sao thứ hai.",
                        "QS3DCURTAIN: một cửa sổ đang trong giai đoạn publish; không mở bản sao thứ hai.");
                    CloseOwnedCandidateOnFailure(candidate);
                    return;
                }

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    CloseOwnedCandidateOnFailure(candidate);
                    return;
                }

                Application.ShowModelessWindow(IntPtr.Zero, candidate, true);

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    CloseOwnedCandidateOnFailure(candidate);
                    return;
                }

                if (!candidate.IsLoaded)
                {
                    ReleaseOwnedWindow(candidate);
                    return;
                }

                if (!PromotePendingWindow(candidate, document, nativeDatabaseIdentity))
                {
                    if (!CloseOwnedCandidateOnFailure(candidate))
                    {
                        ReportBlocked(document, nativeDatabaseIdentity,
                            "Vách Kính Hub không thể xác nhận owner sau khi host publish; cửa sổ ứng viên vẫn được giữ để đóng an toàn.",
                            "QS3DCURTAIN: publication owner changed; giữ owner ứng viên đến terminal Closed.");
                    }
                    return;
                }

                candidate = null;
                if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                    TrySetStatus("Vách Kính Hub: Family • panel grid • schedule • workflow 3D.");
            }
            catch (System.Exception)
            {
                if (candidate != null)
                    CloseOwnedCandidateOnFailure(candidate);
                ReportFailure(document, nativeDatabaseIdentity);
            }
        }

        private static bool PreparePublishedWindow(Document requestedDocument, IntPtr requestedNativeDatabaseIdentity)
        {
            var pending = _pendingWindow;
            if (pending != null)
            {
                if (!pending.IsLoaded)
                {
                    ReleaseOwnedWindow(pending);
                }
                else
                {
                    try { pending.Close(); }
                    catch { return false; }
                    if (pending.IsLoaded) return false;
                    ReleaseOwnedWindow(pending);
                }
            }

            var published = _window;
            if (published == null) return true;

            if (!published.IsLoaded)
            {
                ReleaseOwnedWindow(published);
                return true;
            }

            if (_nativeDatabaseIdentity == requestedNativeDatabaseIdentity && ReferenceEquals(_document, requestedDocument))
                return true;

            try { published.Close(); }
            catch { return false; }

            if (published.IsLoaded) return false;

            ReleaseOwnedWindow(published);
            return true;
        }

        private static bool ReservePendingWindow(CurtainWallWindow candidate, Document document, IntPtr nativeDatabaseIdentity)
        {
            if (_pendingWindow != null) return false;

            _pendingWindow = candidate;
            _pendingDocument = document;
            _pendingNativeDatabaseIdentity = nativeDatabaseIdentity;
            return true;
        }

        private static bool PromotePendingWindow(CurtainWallWindow candidate, Document document, IntPtr nativeDatabaseIdentity)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity) ||
                !ReferenceEquals(_pendingWindow, candidate) ||
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

        private static bool CloseOwnedCandidateOnFailure(CurtainWallWindow candidate)
        {
            try { if (candidate.IsLoaded) candidate.Close(); } catch { return false; }
            if (candidate.IsLoaded) return false;
            ReleaseOwnedWindow(candidate);
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

        private static void ReportBlocked(Document document, IntPtr nativeDatabaseIdentity, string status, string editorMessage)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            TrySetStatus(status);
            TryWrite(document, "\n" + editorMessage);
        }

        private static void ReportFailure(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
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

        private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (nativeDatabaseIdentity == IntPtr.Zero) return false;
            try
            {
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)) return false;
                var database = document.Database;
                return database != null &&
                       database.UnmanagedObject != IntPtr.Zero &&
                       database.UnmanagedObject == nativeDatabaseIdentity;
            }
            catch
            {
                return false;
            }
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
