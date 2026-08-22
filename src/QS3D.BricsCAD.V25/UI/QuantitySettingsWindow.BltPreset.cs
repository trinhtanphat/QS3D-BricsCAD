using System.Windows;
using QS3D.Core.Reporting;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantitySettingsWindow
    {
        public void LoadBltPresetOnOpen()
        {
            Loaded += QuantitySettingsWindow_LoadBltPresetOnOpen;
        }

        private void QuantitySettingsWindow_LoadBltPresetOnOpen(object sender, RoutedEventArgs e)
        {
            Loaded -= QuantitySettingsWindow_LoadBltPresetOnOpen;
            LoadBltPreset(showConfirmation: false);
            SettingsPathText.Text = _store.SettingsPath + "  •  PRESET BLT (chưa lưu)";
        }

        private void LoadBltPreset_Click(object sender, RoutedEventArgs e)
        {
            LoadBltPreset(showConfirmation: true);
        }

        private void LoadBltPreset(bool showConfirmation)
        {
            try
            {
                var preset = QuantityCalculationBltCompatibilityPreset.Create();
                LoadIntoView(preset);

                if (!showConfirmation) return;
                MessageBox.Show(
                    this,
                    "Đã nạp preset BLT tích hợp: " + preset.CategoryRules.Count +
                    " loại cấu kiện và " + preset.IntersectionRules.Count +
                    " luật giao cắt có hướng.\n\n" +
                    "Preset mới chỉ được nạp vào cửa sổ. Nhấn ‘Lưu Cài Đặt’ để áp dụng làm cấu hình theo máy." +
                    (_persistentSettingsWriteBlocked
                        ? "\n\nFile cấu hình chính đang dùng schema mới hơn nên Lưu Cài Đặt vẫn bị khóa. Bạn vẫn có thể Xuất template BLT ra một file khác."
                        : string.Empty),
                    "QS3D • Preset BLT",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show(
                    this,
                    "Không thể nạp preset BLT tích hợp. Cấu hình hiện tại chưa được ghi xuống máy.",
                    "QS3D • Preset BLT",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
