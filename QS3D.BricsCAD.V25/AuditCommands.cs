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
                var hasProject = ProjectContextCoordinator.TryGetReadOnly(document, out var project);
                _window = new AuditLogWindow(document);
                _window.Closed += (_, __) => _window = null;
                Application.ShowModelessWindow(IntPtr.Zero, _window, true);
                var status = hasProject
                    ? "Đã mở Nhật ký thay đổi • " + project.AuditEvents.Count + " sự kiện."
                    : "Đã mở Nhật ký thay đổi • chưa có QS3D project hiện hữu; không tạo project mới.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
            }
            catch (System.Exception)
            {
                const string status = "Nhật ký thay đổi lỗi: không thể mở nhật ký thay đổi.";
                try { document.Editor.WriteMessage("\nQS3DAUDIT error: không thể mở nhật ký thay đổi."); } catch { }
                try { PaletteCoordinator.SetStatus(status); } catch { }
            }
        }
    }
}
