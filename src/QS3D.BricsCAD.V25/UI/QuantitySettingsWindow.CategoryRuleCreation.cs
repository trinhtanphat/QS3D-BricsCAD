using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QS3D.Core.Reporting;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantitySettingsWindow
    {
        private bool _categoryRuleCreationEventsHooked;

        public ObservableCollection<QuantityCategoryChoice> MissingCategoryRuleChoices { get; } =
            new ObservableCollection<QuantityCategoryChoice>();

        private void QuantitySettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_categoryRuleCreationEventsHooked)
            {
                CategoryRows.CollectionChanged += QuantityRuleRows_CollectionChanged;
                IntersectionRows.CollectionChanged += QuantityRuleRows_CollectionChanged;
                _categoryRuleCreationEventsHooked = true;
            }

            InitializeUnsavedChangesTracking();
            InitializeIntersectionRuleRemoval();
            RebuildMissingCategoryRuleChoices();
        }

        private void QuantityRuleRows_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildMissingCategoryRuleChoices();
        }

        private void RebuildMissingCategoryRuleChoices()
        {
            var selectedCode = (MissingCategoryRuleList.SelectedItem as QuantityCategoryChoice)?.CategoryCode;
            var categoryCodes = new HashSet<int>(CategoryRows.Select(x => x.CategoryCode));
            var missingCodes = IntersectionRows
                .SelectMany(x => new[] { x.SourceCode, x.TargetCode })
                .Where(x => !categoryCodes.Contains(x))
                .Distinct()
                .OrderBy(x => QuantityCategoryDisplayName.Resolve(x), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x)
                .ToList();

            MissingCategoryRuleChoices.Clear();
            foreach (var code in missingCodes)
                MissingCategoryRuleChoices.Add(new QuantityCategoryChoice(code));

            var restored = selectedCode.HasValue
                ? MissingCategoryRuleChoices.FirstOrDefault(x => x.CategoryCode == selectedCode.Value)
                : null;
            MissingCategoryRuleList.SelectedItem = restored ?? MissingCategoryRuleChoices.FirstOrDefault();
            MissingCategoryRuleList.IsEnabled = MissingCategoryRuleChoices.Count > 0 && !_persistentSettingsWriteBlocked;
            UpdateCreateCategoryRuleButton();

            MissingCategoryRuleStatusText.Text = MissingCategoryRuleChoices.Count == 0
                ? "Không có mã intersection-only: mọi mã đang dùng trong luật giao cắt đều đã có quy tắc loại."
                : (_persistentSettingsWriteBlocked
                    ? "Có " + MissingCategoryRuleChoices.Count + " mã chưa có quy tắc loại, nhưng cửa sổ đang chỉ đọc vì file cấu hình dùng schema mới hơn."
                    : "Có " + MissingCategoryRuleChoices.Count + " mã chỉ xuất hiện trong luật giao cắt. Chọn một mã để tạo quy tắc loại an toàn rồi chỉnh thông số trước khi lưu.");
        }

        private void MissingCategoryRuleSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCreateCategoryRuleButton();
        }

        private void UpdateCreateCategoryRuleButton()
        {
            if (CreateCategoryRuleButton == null) return;
            CreateCategoryRuleButton.IsEnabled =
                !_persistentSettingsWriteBlocked &&
                MissingCategoryRuleList.SelectedItem is QuantityCategoryChoice;
        }

        private void CreateCategoryRule_Click(object sender, RoutedEventArgs e)
        {
            if (_persistentSettingsWriteBlocked)
            {
                RebuildMissingCategoryRuleChoices();
                return;
            }

            var selected = MissingCategoryRuleList.SelectedItem as QuantityCategoryChoice;
            if (selected == null) return;

            var code = selected.CategoryCode;
            if (CategoryRows.Any(x => x.CategoryCode == code) ||
                !IntersectionRows.Any(x => x.SourceCode == code || x.TargetCode == code))
            {
                RebuildMissingCategoryRuleChoices();
                return;
            }

            var answer = MessageBox.Show(
                this,
                "Tạo quy tắc loại cho " + selected.DisplayName +
                "?\n\nQS3D sẽ thêm một dòng an toàn với Lấy CP Thành = TẮT, Lấy CP Đáy = TẮT và góc ngưỡng 30°. Không có khối lượng cốp pha mới nào được bật chỉ vì thao tác này. Thay đổi chỉ được ghi xuống máy khi bạn nhấn ‘Lưu Cài Đặt’.",
                "QS3D • Tạo quy tắc loại",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            CategoryRows.Add(new QuantityCategoryRuleRow(new QuantityCategoryRuleSetting
            {
                Category = code,
                ExtractSide = false,
                ExtractBottom = false,
                FaceAngleThresholdDeg = 30d
            }));

            RebuildIntersectionBrowser();
        }
    }
}
