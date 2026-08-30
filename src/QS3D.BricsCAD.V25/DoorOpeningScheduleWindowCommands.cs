using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class DoorOpeningScheduleWindowCommands
    {
        private static DoorOpeningScheduleWindow? _window;
        private static Document? _document;
        private static IntPtr _nativeDatabaseIdentity;

        [CommandMethod("QS3DDOORSCHEDULE", CommandFlags.Modal)]
        public void ShowDoorOpeningSchedule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            DoorOpeningScheduleWindow? candidate = null;
            try
            {
                var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(document, nativeDatabaseIdentity))
                {
                    const string blockedStatus = "Door/Opening Schedule hiện tại chưa thể đóng an toàn; không mở bản sao thứ hai.";
                    try { document.Editor.WriteMessage("\nQS3DDOORSCHEDULE: cửa sổ hiện tại chưa đạt terminal Closed; không mở bản sao thứ hai."); } catch { }
                    try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    return;
                }

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    try { PaletteCoordinator.SetStatus("Door/Opening Schedule hiện có đã được kích hoạt cho đúng bản vẽ."); } catch { }
                    return;
                }

                candidate = new DoorOpeningScheduleWindow(document);
                var window = candidate;
                window.Closed += (_, __) => ReleasePublishedWindow(window);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded) return;

                _window = window;
                _document = document;
                _nativeDatabaseIdentity = nativeDatabaseIdentity;
                candidate = null;
                PaletteCoordinator.SetStatus("Door/Opening Schedule: group • host provenance • XLSX • khóa theo project của bản vẽ.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DDOORSCHEDULE lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
            finally
            {
                if (candidate != null) TryCloseUnpublishedWindow(candidate);
            }
        }

        private static bool PreparePublishedWindow(Document requestedDocument, IntPtr requestedNativeDatabaseIdentity)
        {
            var published = _window;
            if (published == null) return true;

            if (!published.IsLoaded)
            {
                ReleasePublishedWindow(published);
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

            ReleasePublishedWindow(published);
            return true;
        }

        private static void ReleasePublishedWindow(DoorOpeningScheduleWindow window)
        {
            if (!ReferenceEquals(_window, window)) return;
            _window = null;
            _document = null;
            _nativeDatabaseIdentity = IntPtr.Zero;
        }

        private static void TryCloseUnpublishedWindow(DoorOpeningScheduleWindow window)
        {
            if (ReferenceEquals(_window, window)) return;
            try { window.Close(); } catch (System.Exception) { }
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
    }
}
