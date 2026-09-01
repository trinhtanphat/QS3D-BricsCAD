using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class WallQuantityCommands
    {
        private static WallQuantityWindow? _window;
        private static Document? _document;
        private static IntPtr _nativeDatabaseIdentity;

        [CommandMethod("QS3DWALLQTY", CommandFlags.Modal)]
        public void ShowWallQuantity()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(document, nativeDatabaseIdentity))
                {
                    const string blockedStatus = "Khối lượng Tường hiện tại chưa thể đóng an toàn; không mở bản sao thứ hai.";
                    try { document.Editor.WriteMessage("\nQS3DWALLQTY: cửa sổ hiện tại chưa đạt terminal Closed; không mở bản sao thứ hai."); } catch { }
                    try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    return;
                }

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    try { PaletteCoordinator.SetStatus("Khối lượng Tường hiện có đã được kích hoạt cho đúng bản vẽ."); } catch { }
                    return;
                }

                var window = new WallQuantityWindow(document);
                window.Closed += (_, __) => ReleasePublishedWindow(window);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded) return;

                _window = window;
                _document = document;
                _nativeDatabaseIdentity = nativeDatabaseIdentity;
                PaletteCoordinator.SetStatus("Khối lượng Tường: danh sách • thuộc tính • chi tiết • XLSX • read-only.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DWALLQTY lỗi: " + ex.Message;
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

        private static void ReleasePublishedWindow(WallQuantityWindow window)
        {
            if (!ReferenceEquals(_window, window)) return;
            _window = null;
            _document = null;
            _nativeDatabaseIdentity = IntPtr.Zero;
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Wall Quantity requires a BricsCAD document database.");

            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Wall Quantity requires a live native BricsCAD database.");
            return identity;
        }
    }
}