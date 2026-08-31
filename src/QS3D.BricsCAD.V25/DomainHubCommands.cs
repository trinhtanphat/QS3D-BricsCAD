using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class DomainHubCommands
    {
        private static DomainHubWindow? _published;
        private static DomainHubWindow? _pending;

        [CommandMethod("QS3DDOMAIN", CommandFlags.Modal)]
        public void ShowDomainHub()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            DomainHubWindow? candidate = null;
            try
            {
                var pending = _pending;
                if (pending != null && !TryClosePendingWindow(pending))
                {
                    ReportStatus(document, "Domain Hub chưa thể mở lại vì cửa sổ lỗi trước đó chưa đóng hoàn toàn.");
                    return;
                }

                var previous = _published;
                if (previous != null)
                {
                    if (previous.IsLoaded)
                    {
                        try { previous.Activate(); } catch { }
                        ReportStatus(document, "Domain Hub đã mở.");
                        return;
                    }

                    ReleasePublishedWindow(previous);
                }

                candidate = new DomainHubWindow();
                var window = candidate;
                _pending = window;
                window.Closed += (_, __) => ReleaseWindow(window);

                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded)
                    throw new InvalidOperationException("Domain Hub host publication did not remain loaded.");

                _published = window;
                ReleasePendingWindow(window);
                candidate = null;
                ReportStatus(document, "Đã mở Domain Hub.");
            }
            catch (Exception ex)
            {
                ReportStatus(document, "QS3DDOMAIN lỗi khi mở cửa sổ.");
                try { document.Editor.WriteMessage("\nQS3DDOMAIN failed (" + ex.GetType().Name + ")."); } catch { }
            }
            finally
            {
                if (candidate != null)
                    TryClosePendingWindow(candidate);
            }
        }

        private static void ReleaseWindow(DomainHubWindow window)
        {
            ReleasePublishedWindow(window);
            ReleasePendingWindow(window);
        }

        private static void ReleasePublishedWindow(DomainHubWindow window)
        {
            if (!ReferenceEquals(_published, window)) return;
            _published = null;
        }

        private static void ReleasePendingWindow(DomainHubWindow window)
        {
            if (!ReferenceEquals(_pending, window)) return;
            _pending = null;
        }

        private static bool TryClosePendingWindow(DomainHubWindow window)
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
