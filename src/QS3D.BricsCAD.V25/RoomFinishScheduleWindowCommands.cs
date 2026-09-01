using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RoomFinishScheduleWindowCommands
    {
        private static RoomFinishScheduleWindow? _window;
        private static Document? _publishedDocument;
        private static IntPtr _publishedNativeDatabaseIdentity;

        [CommandMethod("QS3DFINISHSCHEDULE", CommandFlags.Modal)]
        public void ShowRoomFinishSchedule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(document, nativeDatabaseIdentity))
                {
                    const string blockedStatus = "HT_Phòng Schedule hiện tại chưa thể đóng an toàn; không mở bản sao thứ hai.";
                    try { document.Editor.WriteMessage("\nQS3DFINISHSCHEDULE: cửa sổ hiện tại chưa đạt terminal Closed; không mở bản sao thứ hai."); } catch { }
                    try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    return;
                }

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    try { PaletteCoordinator.SetStatus("HT_Phòng Schedule hiện có đã được kích hoạt cho đúng bản vẽ."); } catch { }
                    return;
                }

                var window = new RoomFinishScheduleWindow(document);
                window.Closed += (_, __) => ReleasePublishedWindow(window);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded) return;

                _publishedDocument = document;
                _publishedNativeDatabaseIdentity = nativeDatabaseIdentity;
                _window = window;
                PaletteCoordinator.SetStatus("HT_Phòng Schedule: review • filter • XLSX • khóa theo project của bản vẽ.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DFINISHSCHEDULE lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
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

            if (ReferenceEquals(_publishedDocument, requestedDocument)
                && _publishedNativeDatabaseIdentity != IntPtr.Zero
                && _publishedNativeDatabaseIdentity == requestedNativeDatabaseIdentity)
            {
                return true;
            }

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

        private static void ReleasePublishedWindow(RoomFinishScheduleWindow window)
        {
            if (!ReferenceEquals(_window, window)) return;
            _window = null;
            _publishedDocument = null;
            _publishedNativeDatabaseIdentity = IntPtr.Zero;
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("HT_Phòng Schedule requires a BricsCAD document database.");

            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("HT_Phòng Schedule requires a live native BricsCAD database.");
            return identity;
        }
    }
}
