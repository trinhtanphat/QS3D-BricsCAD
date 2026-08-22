using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CoordinationManagerCommands
    {
        private static CoordinationManagerWindow? _window;

        [CommandMethod("QS3DCOORDINATIONMANAGER", CommandFlags.Modal)]
        public void ShowCoordinationManager()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Coordination Manager cần một QS3D project hiện hữu; thao tác đọc không tạo project thay thế.");

                if (_window != null && _window.IsLoaded) _window.Close();
                _window = new CoordinationManagerWindow(document, project.ProjectId, project.DrawingFingerprint);
                _window.Closed += (_, __) => _window = null;
                Application.ShowModelessWindow(IntPtr.Zero, _window, true);
                try { PaletteCoordinator.SetStatus("Đã mở Coordination Manager cho project hiện hành."); } catch { }
            }
            catch (Exception ex)
            {
                try { document.Editor.WriteMessage("\nQS3D Coordination Manager: " + ex.Message); } catch { }
                try { PaletteCoordinator.SetStatus("Coordination Manager: " + ex.Message); } catch { }
            }
        }
    }
}
