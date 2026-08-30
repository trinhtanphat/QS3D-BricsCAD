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

        [CommandMethod("QS3DCURTAIN", CommandFlags.Modal)]
        public void ShowCurtainWallHub()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(document, nativeDatabaseIdentity))
                {
                    const string blockedStatus = "Vách Kính Hub hiện tại chưa thể đóng an toàn; không mở bản sao thứ hai.";
                    try { PaletteCoordinator.SetStatus(blockedStatus); } catch { }
                    try { document.Editor.WriteMessage("\nQS3DCURTAIN: cửa sổ hiện tại chưa đạt terminal Closed; không mở bản sao thứ hai."); } catch { }
                    return;
                }

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    try { PaletteCoordinator.SetStatus("Vách Kính Hub hiện có đã được kích hoạt cho đúng bản vẽ."); } catch { }
                    return;
                }

                var window = new CurtainWallWindow(document);
                window.Closed += (_, __) => ReleasePublishedWindow(window);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded) return;

                _window = window;
                _document = document;
                _nativeDatabaseIdentity = nativeDatabaseIdentity;
                PaletteCoordinator.SetStatus("Vách Kính Hub: Family • panel grid • schedule • workflow 3D.");
            }
            catch (System.Exception ex)
            {
                PaletteCoordinator.SetStatus("QS3DCURTAIN lỗi: " + ex.Message);
                document.Editor.WriteMessage("\nQS3DCURTAIN lỗi: " + ex.Message);
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

        private static void ReleasePublishedWindow(CurtainWallWindow window)
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
                throw new InvalidOperationException("Vách Kính Hub requires a BricsCAD document database.");

            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Vách Kính Hub requires a live native BricsCAD database.");
            return identity;
        }
    }
}