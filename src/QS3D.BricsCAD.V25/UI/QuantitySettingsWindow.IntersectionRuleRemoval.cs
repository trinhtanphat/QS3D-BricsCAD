using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantitySettingsWindow
    {
        private Button? _deleteSelectedRuleButton;
        private bool _intersectionRuleRemovalInitialized;

        private void InitializeIntersectionRuleRemoval()
        {
            if (_intersectionRuleRemovalInitialized) return;

            var actionPanel = CreateSelectedRuleButton.Parent as Panel;
            if (actionPanel == null)
                throw new InvalidOperationException("Không thể khởi tạo thao tác xóa luật giao cắt trong QS3DSETUP.");

            _deleteSelectedRuleButton = new Button
            {
                Content = "−  Xóa luật A → B",
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 8, 0, 0),
                IsEnabled = false,
                Visibility = Visibility.Collapsed
            };
            _deleteSelectedRuleButton.Click += DeleteSelectedRule_Click;

            var createIndex = actionPanel.Children.IndexOf(CreateSelectedRuleButton);
            actionPanel.Children.Insert(createIndex < 0 ? actionPanel.Children.Count : createIndex + 1, _deleteSelectedRuleButton);

            PrimaryCategoryList.SelectionChanged += IntersectionRuleRemovalStateChanged;
            ReferenceCategoryList.SelectionChanged += IntersectionRuleRemovalStateChanged;
            IntersectionRows.CollectionChanged += IntersectionRuleRemovalRowsChanged;
            _intersectionRuleRemovalInitialized = true;
            UpdateDeleteSelectedRuleButton();
        }

        private void IntersectionRuleRemovalStateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDeleteSelectedRuleButton();
        }

        private void IntersectionRuleRemovalRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateDeleteSelectedRuleButton();
        }

        private void UpdateDeleteSelectedRuleButton()
        {
            var button = _deleteSelectedRuleButton;
            if (button == null) return;

            var source = PrimaryCategoryList.SelectedItem as QuantityCategoryChoice;
            var target = ReferenceCategoryList.SelectedItem as QuantityCategoryChoice;
            var exists = source != null && target != null &&
                IntersectionRows.Any(x => x.SourceCode == source.CategoryCode && x.TargetCode == target.CategoryCode);

            button.Visibility = exists ? Visibility.Visible : Visibility.Collapsed;
            button.IsEnabled = exists && !_persistentSettingsWriteBlocked;
        }

        private void DeleteSelectedRule_Click(object sender, RoutedEventArgs e)
        {
            if (_persistentSettingsWriteBlocked)
            {
                UpdateDeleteSelectedRuleButton();
                return;
            }

            var source = PrimaryCategoryList.SelectedItem as QuantityCategoryChoice;
            var target = ReferenceCategoryList.SelectedItem as QuantityCategoryChoice;
            if (source == null || target == null)
            {
                UpdateDeleteSelectedRuleButton();
                return;
            }

            var selected = IntersectionRows.SingleOrDefault(
                x => x.SourceCode == source.CategoryCode && x.TargetCode == target.CategoryCode);
            if (selected == null)
            {
                UpdateDeleteSelectedRuleButton();
                return;
            }

            var answer = MessageBox.Show(
                this,
                "Xóa luật " + source.DisplayName + " → " + target.DisplayName +
                " khỏi cấu hình đang chỉnh?\n\nChỉ chiều A → B này bị xóa. Luật chiều ngược B → A và các quy tắc loại không bị thay đổi. Thao tác chưa ghi xuống máy cho tới khi bạn nhấn ‘Lưu Cài Đặt’.",
                "QS3D • Xóa luật giao cắt",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            IntersectionRows.Remove(selected);
            RebuildIntersectionBrowser();
            UpdateDeleteSelectedRuleButton();
        }
    }
}
