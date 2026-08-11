using System;
using System.ComponentModel;
using System.Windows;
using QS3D.Core.Reporting;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantitySettingsWindow
    {
        private QuantityCalculationSettings? _persistedSettingsBaseline;
        private bool _unsavedChangesTrackingInitialized;
        private bool _allowCloseWithoutPrompt;

        private void InitializeUnsavedChangesTracking()
        {
            if (_unsavedChangesTrackingInitialized) return;

            _persistedSettingsBaseline = BuildSettingsFromView();
            Closing += QuantitySettingsWindow_Closing;
            SaveSettingsButton.Click += QuantitySettingsSaveBaseline_Click;
            _unsavedChangesTrackingInitialized = true;
        }

        private void QuantitySettingsSaveBaseline_Click(object sender, RoutedEventArgs e)
        {
            if (_persistentSettingsWriteBlocked) return;

            try
            {
                var current = BuildSettingsFromView();
                var persisted = _store.Load();
                persisted.NormalizeAndValidate();
                if (SettingsEquivalent(current, persisted))
                    _persistedSettingsBaseline = current.Clone();
            }
            catch
            {
                // The existing Save handler owns validation/I/O error reporting.
                // A failed save must never advance the close-time persisted baseline.
            }
        }

        private void QuantitySettingsWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_allowCloseWithoutPrompt) return;

            QuantityCalculationSettings current;
            try
            {
                current = BuildSettingsFromView();
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                ShowError(
                    "Không thể đóng vì cấu hình hiện tại còn giá trị chưa hợp lệ. Hãy sửa giá trị hoặc khôi phục cấu hình hợp lệ trước khi đóng để tránh mất thay đổi ngoài ý muốn.",
                    ex);
                return;
            }

            var baseline = _persistedSettingsBaseline;
            if (baseline == null)
            {
                _persistedSettingsBaseline = current.Clone();
                return;
            }

            if (SettingsEquivalent(current, baseline)) return;

            if (_persistentSettingsWriteBlocked)
            {
                var readOnlyAnswer = MessageBox.Show(
                    this,
                    "Cửa sổ đang ở chế độ CHỈ ĐỌC vì file cấu hình dùng schema mới hơn. Các thay đổi trong cửa sổ không thể lưu đè lên file hiện tại.\n\nChọn OK để đóng và bỏ các thay đổi trong cửa sổ, hoặc Hủy để quay lại (ví dụ Xuất template ra file khác).",
                    "QS3D • Thay đổi chưa lưu trong chế độ chỉ đọc",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);
                if (readOnlyAnswer != MessageBoxResult.OK)
                {
                    e.Cancel = true;
                    return;
                }

                _allowCloseWithoutPrompt = true;
                return;
            }

            var answer = MessageBox.Show(
                this,
                "Cài đặt tính toán có thay đổi chưa được lưu.\n\nCó = Lưu Cài Đặt rồi đóng\nKhông = Bỏ thay đổi và đóng\nHủy = Quay lại cửa sổ",
                "QS3D • Cài đặt chưa lưu",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (answer == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (answer == MessageBoxResult.No)
            {
                _allowCloseWithoutPrompt = true;
                return;
            }

            try
            {
                _store.Save(current);
                _persistedSettingsBaseline = current.Clone();
                _loadedSettings = current.Clone();
                _allowCloseWithoutPrompt = true;
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                ShowError("Không thể lưu cài đặt trước khi đóng. Cửa sổ vẫn được giữ mở.", ex);
            }
        }

        private static bool SettingsEquivalent(QuantityCalculationSettings left, QuantityCalculationSettings right)
        {
            if (left.SchemaVersion != right.SchemaVersion ||
                !left.FormworkTolerance.Equals(right.FormworkTolerance) ||
                !left.BlindingConcreteOffset.Equals(right.BlindingConcreteOffset) ||
                !left.MinSubtractAreaMm2.Equals(right.MinSubtractAreaMm2) ||
                !left.MinFormworkAreaMm2.Equals(right.MinFormworkAreaMm2) ||
                !left.MinConcreteVolumeM3.Equals(right.MinConcreteVolumeM3) ||
                !left.EngulfRelPercent.Equals(right.EngulfRelPercent) ||
                !left.EngulfMinAreaMm2.Equals(right.EngulfMinAreaMm2) ||
                !left.RoomGapFillMm.Equals(right.RoomGapFillMm) ||
                !left.RoomSearchRadiusMm.Equals(right.RoomSearchRadiusMm) ||
                !string.Equals(left.DimColor, right.DimColor, StringComparison.Ordinal) ||
                !left.DimTextHeight.Equals(right.DimTextHeight) ||
                left.CategoryRules.Count != right.CategoryRules.Count ||
                left.IntersectionRules.Count != right.IntersectionRules.Count)
                return false;

            for (var i = 0; i < left.CategoryRules.Count; i++)
            {
                var a = left.CategoryRules[i];
                var b = right.CategoryRules[i];
                if (a.Category != b.Category ||
                    a.ExtractSide != b.ExtractSide ||
                    a.ExtractBottom != b.ExtractBottom ||
                    !a.FaceAngleThresholdDeg.Equals(b.FaceAngleThresholdDeg))
                    return false;
            }

            for (var i = 0; i < left.IntersectionRules.Count; i++)
            {
                var a = left.IntersectionRules[i];
                var b = right.IntersectionRules[i];
                if (a.Source != b.Source ||
                    a.Target != b.Target ||
                    a.SubtractConcrete != b.SubtractConcrete ||
                    a.SubtractSideFormworkByConcrete != b.SubtractSideFormworkByConcrete ||
                    a.SubtractBottomFormworkByConcrete != b.SubtractBottomFormworkByConcrete ||
                    a.SubtractSideFormworkBySideFormwork != b.SubtractSideFormworkBySideFormwork ||
                    a.SubtractBottomFormworkByBottomFormwork != b.SubtractBottomFormworkByBottomFormwork)
                    return false;
            }

            return true;
        }
    }
}
