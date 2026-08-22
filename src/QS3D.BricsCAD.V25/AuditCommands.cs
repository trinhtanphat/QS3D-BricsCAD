using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class AuditCommands
    {
        private static AuditLogWindow? _window;

        [CommandMethod("QS3DAUDIT", CommandFlags.Modal)]
        public void ShowAuditLog()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (_window != null && _window.IsLoaded) _window.Close();
                var project = ProjectContextCoordinator.GetOrCreate(document);
                _window = new AuditLogWindow(project);
                _window.Closed += (_, __) => _window = null;
                Application.ShowModelessWindow(IntPtr.Zero, _window, true);
                PaletteCoordinator.SetStatus("Đã mở Nhật ký thay đổi • " + project.AuditEvents.Count + " sự kiện.");
            }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DAUDIT error: " + ex.Message);
                PaletteCoordinator.SetStatus("Nhật ký thay đổi lỗi: " + ex.Message);
            }
        }
    }
}
