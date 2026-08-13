using System;
using System.Windows;
using Bricscad.ApplicationServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25.Updates
{
    public sealed class UpdateSettingsCommands
    {
        [CommandMethod("QS3DUPDATEONCLOSE", CommandFlags.Modal)]
        public void ToggleInstallOnClose()
        {
            var enabled = !UpdatePreferences.InstallOnExit;
            if (!UpdatePreferences.TrySetInstallOnExit(enabled, out var error))
            {
                Show(error, MessageBoxImage.Warning);
                return;
            }

            var message = enabled
                ? "Đã bật Update khi đóng. Khi QS3D đã kiểm tra và xác minh được bản mới, lần bạn đóng BricsCAD bình thường tiếp theo sẽ lên lịch cài đặt; updater chờ mọi tiến trình BricsCAD thoát rồi cập nhật và mở lại BricsCAD."
                : "Đã tắt Update khi đóng. QS3D vẫn tự kiểm tra bản mới khi khởi động; bạn có thể dùng QS3DUPDATE hoặc nút Cập nhật QS3D để cài thủ công.";
            Show(message, MessageBoxImage.Information);
        }

        [CommandMethod("QS3DUPDATESTATUS", CommandFlags.Modal)]
        public void ShowUpdatePreferenceStatus()
        {
            var mode = UpdatePreferences.InstallOnExit ? "BẬT" : "TẮT";
            var result = UpdateCoordinator.Instance.LastResult;
            var detail = string.IsNullOrWhiteSpace(result.Message) ? "Chưa có trạng thái cập nhật." : result.Message;
            Show("Update khi đóng: " + mode + "\n\n" + detail, MessageBoxImage.Information);
        }

        private static void Show(string message, MessageBoxImage icon)
        {
            try
            {
                MessageBox.Show(message, "QS3D Update", MessageBoxButton.OK, icon);
            }
            catch
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\nQS3D Update: " + message.Replace("\n", " "));
            }
        }
    }
}
