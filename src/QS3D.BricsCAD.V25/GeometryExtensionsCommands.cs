using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GeometryExtensionsCommands
    {
        private static GeometryExtensionsWindow? _published;
        private static GeometryExtensionsWindow? _pending;

        [CommandMethod("QS3DGEOMETRYEXT", CommandFlags.Modal)]
        public void ShowGeometryExtensions()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            GeometryExtensionsWindow? candidate = null;
            try
            {
                var pending = _pending;
                if (pending != null && !TryClosePendingWindow(pending))
                {
                    ReportStatus(document, "Geometry Extensions chưa thể mở lại vì cửa sổ lỗi trước đó chưa đóng hoàn toàn.");
                    return;
                }

                var previous = _published;
                if (previous != null)
                {
                    if (previous.IsLoaded)
                    {
                        try { previous.Activate(); } catch { }
                        ReportStatus(document, "Geometry Extensions đã mở.");
                        return;
                    }

                    ReleasePublishedWindow(previous);
                }

                candidate = new GeometryExtensionsWindow();
                var window = candidate;
                _pending = window;
                window.Closed += (_, __) => ReleaseWindow(window);

                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded)
                    throw new InvalidOperationException("Geometry Extensions host publication did not remain loaded.");

                _published = window;
                ReleasePendingWindow(window);
                candidate = null;
                ReportStatus(document, "Đã mở Geometry Extensions.");
            }
            catch (Exception ex)
            {
                ReportStatus(document, "QS3DGEOMETRYEXT lỗi khi mở cửa sổ.");
                try { document.Editor.WriteMessage("\nQS3DGEOMETRYEXT failed (" + ex.GetType().Name + ")."); } catch { }
            }
            finally
            {
                if (candidate != null)
                    TryClosePendingWindow(candidate);
            }
        }

        private static void ReleaseWindow(GeometryExtensionsWindow window)
        {
            ReleasePublishedWindow(window);
            ReleasePendingWindow(window);
        }

        private static void ReleasePublishedWindow(GeometryExtensionsWindow window)
        {
            if (!ReferenceEquals(_published, window)) return;
            _published = null;
        }

        private static void ReleasePendingWindow(GeometryExtensionsWindow window)
        {
            if (!ReferenceEquals(_pending, window)) return;
            _pending = null;
        }

        private static bool TryClosePendingWindow(GeometryExtensionsWindow window)
        {
            if (!ReferenceEquals(_pending, window)) return true;
            if (ReferenceEquals(_published, window))
            {
                ReleasePendingWindow(window);
                return true;
            }

            if (window.IsLoaded)
            {
                try { window.Close(); } catch (Exception) { }
            }

            if (window.IsLoaded) return false;
            ReleasePendingWindow(window);
            return true;
        }

        private static void ReportStatus(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            try { document.Editor.WriteMessage("\n" + message); } catch { }
        }
    }
}
