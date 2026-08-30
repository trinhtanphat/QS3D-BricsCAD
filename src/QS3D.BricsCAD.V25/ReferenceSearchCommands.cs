using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ReferenceSearchCommands
    {
        private static ReferenceSearchWindow? _window;

        [CommandMethod("QS3DREFSEARCH", CommandFlags.Modal)]
        public void ShowReferenceSearch()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(document, nativeDatabaseIdentity))
                {
                    const string blockedStatus = "Tham khảo thi công hiện tại chưa thể đóng an toàn; không mở bản sao thứ hai.";
                    try { document.Editor.WriteMessage("\nQS3DREFSEARCH: cửa sổ hiện tại chưa đạt terminal Closed; không mở bản sao thứ hai."); } catch { }
                    try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    return;
                }

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    try { PaletteCoordinator.SetStatus("Tham khảo thi công hiện có đã được kích hoạt cho đúng bản vẽ."); } catch { }
                    return;
                }

                var window = new ReferenceSearchWindow(document);
                window.Closed += (_, __) => ReleasePublishedWindow(window);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded) return;

                _window = window;
                PaletteCoordinator.SetStatus("Tham khảo thi công: ảnh • web • video • mua sắm • video ngắn • tin tức.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DREFSEARCH lỗi: " + ex.Message;
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

            if (published.IsBoundTo(requestedDocument, requestedNativeDatabaseIdentity))
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

        private static void ReleasePublishedWindow(ReferenceSearchWindow window)
        {
            if (!ReferenceEquals(_window, window)) return;
            _window = null;
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Reference Search requires a BricsCAD document database.");

            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Reference Search requires a live native BricsCAD database.");
            return identity;
        }
    }
}
