using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ScheduleHubCommands
    {
        private static ScheduleHubWindow? _window;
        private static Document? _document;
        private static IntPtr _nativeDatabaseIdentity;

        [CommandMethod("QS3DSCHEDULES", CommandFlags.Modal)]
        public void ShowScheduleHub()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(document, nativeDatabaseIdentity))
                {
                    const string blockedStatus = "Schedule Hub hiện tại chưa thể đóng an toàn; không mở bản sao thứ hai.";
                    try { document.Editor.WriteMessage("\nQS3DSCHEDULES: cửa sổ hiện tại chưa đạt terminal Closed; không mở bản sao thứ hai."); } catch { }
                    try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    return;
                }

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    try { PaletteCoordinator.SetStatus("Schedule Hub hiện có đã được kích hoạt cho đúng bản vẽ."); } catch { }
                    return;
                }

                var candidate = new ScheduleHubWindow(document);
                candidate.Closed += (_, __) => ReleasePublishedWindow(candidate);
                Application.ShowModelessWindow(IntPtr.Zero, candidate, true);
                if (!candidate.IsLoaded) return;

                _window = candidate;
                _document = document;
                _nativeDatabaseIdentity = nativeDatabaseIdentity;
                PaletteCoordinator.SetStatus("Schedule Hub: BQ • vật liệu • curtain • cửa/lỗ • cốt thép • khóa theo bản vẽ.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DSCHEDULES lỗi: " + ex.Message;
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

        private static void ReleasePublishedWindow(ScheduleHubWindow candidate)
        {
            if (!ReferenceEquals(_window, candidate)) return;
            _window = null;
            _document = null;
            _nativeDatabaseIdentity = IntPtr.Zero;
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Schedule Hub requires a BricsCAD document database.");

            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Schedule Hub requires a live native BricsCAD database.");
            return identity;
        }
    }
}