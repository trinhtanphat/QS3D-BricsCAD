using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class Rebar3DHubCommands
    {
        private static Rebar3DHubWindow? _pending;
        private static Rebar3DHubWindow? _published;

        [CommandMethod("QS3DREBARHUB", CommandFlags.Modal)]
        public void ShowRebarHub()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            Rebar3DHubWindow? candidate = null;
            try
            {
                var pending = _pending;
                if (pending != null)
                    CloseOwnerBeforeReplacement(pending, "pending");

                var published = _published;
                if (published != null)
                {
                    if (published.IsLoaded)
                    {
                        try { published.Activate(); } catch { }
                        try { PaletteCoordinator.SetStatus("Rebar 3D Hub hiện có đã được kích hoạt; lệnh vẫn theo drawing đang active."); } catch { }
                        return;
                    }

                    CloseOwnerBeforeReplacement(published, "published");
                }

                var window = new Rebar3DHubWindow();
                candidate = window;
                window.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_pending, window)) _pending = null;
                    if (ReferenceEquals(_published, window)) _published = null;
                };

                _pending = window;
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded)
                    throw new InvalidOperationException("Rebar 3D Hub did not remain loaded after host publication.");
                if (!ReferenceEquals(_pending, window))
                    throw new InvalidOperationException("Rebar 3D Hub publication ownership changed unexpectedly.");

                _pending = null;
                _published = window;
                candidate = null;
                try { PaletteCoordinator.SetStatus("Rebar 3D Hub đã mở; lệnh luôn gửi sang drawing đang active tại thời điểm bấm."); } catch { }
            }
            catch (Exception ex)
            {
                if (candidate != null && ReferenceEquals(_pending, candidate))
                {
                    try { candidate.Close(); } catch { }
                }

                var message = "QS3DREBARHUB không thể mở Rebar 3D Hub (" + ex.GetType().Name + ").";
                try { document.Editor.WriteMessage("\n" + message); } catch { }
                try { PaletteCoordinator.SetStatus(message); } catch { }
            }
        }

        private static void CloseOwnerBeforeReplacement(Rebar3DHubWindow window, string state)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            if (!window.IsLoaded && string.Equals(state, "published", StringComparison.Ordinal))
            {
                if (ReferenceEquals(_published, window)) _published = null;
                return;
            }

            try
            {
                window.Close();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Rebar 3D Hub " + state + " cleanup failed; replacement was refused.",
                    ex);
            }

            if (window.IsLoaded || ReferenceEquals(_pending, window) || ReferenceEquals(_published, window))
                throw new InvalidOperationException(
                    "Rebar 3D Hub " + state + " owner did not reach terminal close; replacement was refused.");
        }
    }
}
