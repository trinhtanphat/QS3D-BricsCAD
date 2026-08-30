using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class Rebar3DHubCommands
    {
        private static Rebar3DHubWindow? _window;

        [CommandMethod("QS3DREBARHUB", CommandFlags.Modal)]
        public void ShowRebarHub()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            Rebar3DHubWindow? candidate = null;
            try
            {
                var published = _window;
                if (published != null)
                {
                    if (published.IsLoaded)
                    {
                        try { published.Activate(); } catch { }
                        try { PaletteCoordinator.SetStatus("Rebar 3D Hub hiện có đã được kích hoạt; lệnh vẫn theo drawing đang active."); } catch { }
                        return;
                    }

                    ReleasePublishedWindow(published);
                }

                candidate = new Rebar3DHubWindow();
                var window = candidate;
                window.Closed += (_, __) => ReleasePublishedWindow(window);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded) return;

                _window = window;
                candidate = null;
                PaletteCoordinator.SetStatus("Rebar 3D Hub đã mở; lệnh luôn gửi sang drawing đang active tại thời điểm bấm.");
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DREBARHUB lỗi: " + ex.Message);
                PaletteCoordinator.SetStatus("QS3DREBARHUB lỗi: " + ex.Message);
            }
            finally
            {
                if (candidate != null) TryCloseUnpublishedWindow(candidate);
            }
        }

        private static void ReleasePublishedWindow(Rebar3DHubWindow window)
        {
            if (!ReferenceEquals(_window, window)) return;
            _window = null;
        }

        private static void TryCloseUnpublishedWindow(Rebar3DHubWindow window)
        {
            if (ReferenceEquals(_window, window)) return;
            try { window.Close(); } catch (System.Exception) { }
        }
    }
}
