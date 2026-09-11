using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class DoorOpeningScheduleWindowCommands
    {
        private static PublishedWindow? _pending;
        private static PublishedWindow? _published;

        [CommandMethod("QS3DDOORSCHEDULE", CommandFlags.Modal)]
        public void ShowDoorOpeningSchedule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            var nativeDatabaseIdentity = IntPtr.Zero;
            PublishedWindow? owner = null;
            try
            {
                nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;

                if (!PreparePublishedWindow(document, nativeDatabaseIdentity))
                {
                    PublishBlockedStatusIfOwned(document, nativeDatabaseIdentity);
                    return;
                }

                // Closing a prior modeless owner may pump MDI work. Never construct for
                // a document generation that became background during that boundary.
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;

                var published = _published;
                if (published != null)
                {
                    try { published.Window.Activate(); } catch { }
                    if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                    {
                        try { PaletteCoordinator.SetStatus("Door/Opening Schedule hiện có đã được kích hoạt cho đúng bản vẽ."); } catch { }
                    }
                    return;
                }

                var window = new DoorOpeningScheduleWindow(document);
                owner = new PublishedWindow(window, document, nativeDatabaseIdentity);
                var releaseOwner = owner;
                window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);

                // Own the candidate before any host call can pump messages. A failed
                // close/show then leaves a durable reference for the next invocation.
                _pending = owner;

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    ClosePendingOnFailure(owner);
                    return;
                }

                Application.ShowModelessWindow(IntPtr.Zero, window, true);

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    ClosePendingOnFailure(owner);
                    return;
                }

                if (!window.IsLoaded)
                {
                    ReleaseOwnedWindow(owner);
                    return;
                }

                if (!ReferenceEquals(_pending, owner))
                {
                    ClosePendingOnFailure(owner);
                    return;
                }

                _pending = null;
                _published = owner;
                owner = null;

                if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    try { PaletteCoordinator.SetStatus("Door/Opening Schedule: group • host provenance • XLSX • khóa theo project của bản vẽ."); } catch { }
                }
            }
            catch
            {
                if (owner != null) ClosePendingOnFailure(owner);
                PublishFailureStatusIfOwned(document, nativeDatabaseIdentity);
            }
        }

        private static bool PreparePublishedWindow(Document requestedDocument, IntPtr requestedNativeDatabaseIdentity)
        {
            var pending = _pending;
            if (pending != null)
            {
                if (!pending.Window.IsLoaded)
                {
                    ReleaseOwnedWindow(pending);
                }
                else
                {
                    try { pending.Window.Close(); } catch { return false; }
                    if (pending.Window.IsLoaded) return false;
                    ReleaseOwnedWindow(pending);
                }
            }

            var published = _published;
            if (published == null) return true;

            if (!published.Window.IsLoaded)
            {
                ReleaseOwnedWindow(published);
                return true;
            }

            if (published.NativeDatabaseIdentity == requestedNativeDatabaseIdentity &&
                ReferenceEquals(published.Document, requestedDocument))
                return true;

            try { published.Window.Close(); }
            catch { return false; }

            if (published.Window.IsLoaded) return false;

            ReleaseOwnedWindow(published);
            return true;
        }

        private static void ClosePendingOnFailure(PublishedWindow owner)
        {
            try { owner.Window.Close(); } catch { }
            if (!owner.Window.IsLoaded) ReleaseOwnedWindow(owner);
        }

        private static void ReleaseOwnedWindow(PublishedWindow owner)
        {
            if (ReferenceEquals(_pending, owner)) _pending = null;
            if (ReferenceEquals(_published, owner)) _published = null;
        }

        private static void PublishBlockedStatusIfOwned(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            const string blockedStatus = "Door/Opening Schedule hiện tại chưa thể đóng an toàn; không mở bản sao thứ hai.";
            try { document.Editor.WriteMessage("\nQS3DDOORSCHEDULE: cửa sổ hiện tại chưa đạt terminal Closed; không mở bản sao thứ hai."); } catch { }
            try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
        }

        private static void PublishFailureStatusIfOwned(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            const string message = "QS3DDOORSCHEDULE không thể mở Door/Opening Schedule an toàn; trạng thái hiện tại được giữ nguyên.";
            try { PaletteCoordinator.SetStatus(message); } catch { }
            try { document.Editor.WriteMessage("\n" + message); } catch { }
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
                throw new InvalidOperationException("Door/Opening Schedule requires a BricsCAD document database.");

            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Door/Opening Schedule requires a live native BricsCAD database.");
            return identity;
        }

        private sealed class PublishedWindow
        {
            internal PublishedWindow(DoorOpeningScheduleWindow window, Document document, IntPtr nativeDatabaseIdentity)
            {
                Window = window ?? throw new ArgumentNullException(nameof(window));
                Document = document ?? throw new ArgumentNullException(nameof(document));
                NativeDatabaseIdentity = nativeDatabaseIdentity;
            }

            internal DoorOpeningScheduleWindow Window { get; }
            internal Document Document { get; }
            internal IntPtr NativeDatabaseIdentity { get; }
        }
    }
}