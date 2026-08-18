using System;
using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    internal sealed class UiLanguageWindow : Window
    {
        private readonly ComboBox _language;

        internal UiLanguageWindow()
        {
            Title = UiLocalization.T("Ngôn ngữ");
            Width = 460;
            Height = 230;
            MinWidth = 420;
            MinHeight = 220;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;

            var layout = new Grid { Margin = new Thickness(20) };
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var heading = new TextBlock
            {
                Text = UiLocalization.T("Chọn ngôn ngữ giao diện"),
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12),
            };
            Grid.SetRow(heading, 0);
            layout.Children.Add(heading);

            _language = new ComboBox
            {
                MinWidth = 280,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 10),
            };

            string currentCode = UiLocalization.CurrentLanguageCode;
            foreach (UiLocalization.LanguageOption option in UiLocalization.SupportedLanguages)
            {
                var item = new ComboBoxItem
                {
                    Content = option.DisplayName,
                    Tag = option.Code,
                };
                _language.Items.Add(item);
                if (string.Equals(option.Code, currentCode, StringComparison.OrdinalIgnoreCase))
                {
                    _language.SelectedItem = item;
                }
            }

            if (_language.SelectedIndex < 0)
            {
                _language.SelectedIndex = 0;
            }

            Grid.SetRow(_language, 1);
            layout.Children.Add(_language);

            var note = new TextBlock
            {
                Text = UiLocalization.T("Ngôn ngữ được lưu cho các lần mở QS3D tiếp theo."),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.72,
                Margin = new Thickness(0, 0, 0, 16),
            };
            Grid.SetRow(note, 2);
            layout.Children.Add(note);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            var apply = new Button
            {
                Content = UiLocalization.T("Áp dụng"),
                MinWidth = 96,
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true,
            };
            apply.Click += ApplyClick;
            buttons.Children.Add(apply);

            var close = new Button
            {
                Content = UiLocalization.T("Đóng"),
                MinWidth = 96,
                Padding = new Thickness(14, 6, 14, 6),
            };
            close.Click += (sender, args) => Close();
            buttons.Children.Add(close);

            Grid.SetRow(buttons, 4);
            layout.Children.Add(buttons);

            Content = layout;
            Loaded += (sender, args) => _language.Focus();
        }

        private void ApplyClick(object sender, RoutedEventArgs e)
        {
            if (!(_language.SelectedItem is ComboBoxItem item)
                || !(item.Tag is string languageCode))
            {
                return;
            }

            UiLocalization.SetLanguage(languageCode);
            Close();
        }
    }
}
