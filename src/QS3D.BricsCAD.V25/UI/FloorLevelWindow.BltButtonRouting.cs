using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow
    {
        private static readonly bool BltProjectButtonRoutingRegistered = RegisterBltProjectButtonRouting();
        private Border? _bltProjectPlaceholderSurface;
        private TextBlock? _bltProjectPlaceholderText;

        private static readonly SolidColorBrush BltNavSelectedBrush =
            new SolidColorBrush(Color.FromRgb(36, 36, 36));
        private static readonly SolidColorBrush BltNavAccentBrush =
            new SolidColorBrush(Color.FromRgb(29, 120, 213));
        private static readonly SolidColorBrush BltNavTransparentBrush =
            new SolidColorBrush(Colors.Transparent);

        private static bool RegisterBltProjectButtonRouting()
        {
            // BLT3D keeps all three Project Setup entries inside one bounded surface. Register
            // before instance Click handlers so legacy modeless-window routes cannot leak through.
            EventManager.RegisterClassHandler(
                typeof(Button),
                ButtonBase.ClickEvent,
                new RoutedEventHandler(OnBltProjectButtonRoutedClick));
            return true;
        }

        private static void OnBltProjectButtonRoutedClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(Window.GetWindow(button) is FloorLevelWindow window))
                return;

            var label = NormalizeButtonLabel(button.Content);
            switch (label)
            {
                case "Thông tin dự án":
                    e.Handled = true;
                    window.ShowBltProjectPlaceholder("Thông tin dự án");
                    break;

                case "Cài đặt tầng":
                    e.Handled = true;
                    window.ShowBltFloorSettingsSurface();
                    break;

                case "Thuộc tính dự án":
                    e.Handled = true;
                    window.ShowBltProjectPlaceholder("Thuộc tính dự án");
                    break;
            }
        }

        private void ShowBltProjectPlaceholder(string section)
        {
            EnsureBltProjectPlaceholderSurface();
            if (_bltProjectPlaceholderText != null)
                _bltProjectPlaceholderText.Text = "(Chưa xây dựng — Thông tin dự án / Thuộc tính dự án)";
            if (_bltProjectPlaceholderSurface != null)
                _bltProjectPlaceholderSurface.Visibility = Visibility.Visible;

            ApplyBltProjectNavSelection(section);
            SetBltStatus(section + ": chưa xây dựng trong bản tham chiếu BLT3D.");
        }

        private void ShowBltFloorSettingsSurface()
        {
            if (_bltProjectPlaceholderSurface != null)
                _bltProjectPlaceholderSurface.Visibility = Visibility.Collapsed;

            ApplyBltProjectNavSelection("Cài đặt tầng");

            // The grid remains alive underneath the in-window placeholder. Do not reload it just
            // because the user changes Project Setup sub-tabs: RefreshBltSetup() would overwrite
            // edits that are intentionally still pending until “Áp dụng thay đổi” is pressed.
            SetBltStatus("Cài đặt tầng: chỉnh dữ liệu trong bảng rồi bấm Áp dụng thay đổi.");
        }

        private void OpenDedicatedBltProjectProperties()
        {
            // Keep the canonical handler callable for compatibility, but route to the same
            // in-window BLT3D placeholder instead of opening a second modeless surface.
            ShowBltProjectPlaceholder("Thuộc tính dự án");
        }

        private void EnsureBltProjectPlaceholderSurface()
        {
            if (_bltProjectPlaceholderSurface != null && _bltProjectPlaceholderText != null)
                return;
            if (!(Content is Grid root))
                return;

            var text = new TextBlock
            {
                Text = "(Chưa xây dựng — Thông tin dự án / Thuộc tính dự án)",
                Foreground = new SolidColorBrush(Color.FromRgb(214, 214, 214)),
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(24)
            };

            var surface = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Child = text,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Grid.SetRow(surface, 1);
            Panel.SetZIndex(surface, 100);
            root.Children.Add(surface);
            _bltProjectPlaceholderText = text;
            _bltProjectPlaceholderSurface = surface;
        }

        private void ApplyBltProjectNavSelection(string selectedLabel)
        {
            foreach (var button in FindVisualChildren<Button>(this))
            {
                var label = NormalizeButtonLabel(button.Content);
                if (!IsBltProjectNavLabel(label))
                    continue;

                var selected = string.Equals(label, selectedLabel, StringComparison.CurrentCultureIgnoreCase);
                button.Background = selected ? BltNavSelectedBrush : BltNavTransparentBrush;
                button.BorderBrush = selected ? BltNavAccentBrush : BltNavTransparentBrush;
                button.BorderThickness = selected ? new Thickness(0, 0, 0, 3) : new Thickness(0);

                // The original XAML marks Floor Settings selected with a wrapper Grid and a
                // separate bottom Border. Clear that legacy marker once runtime selection takes over.
                if (button.Parent is Grid wrapper)
                {
                    wrapper.Background = BltNavTransparentBrush;
                    foreach (var marker in wrapper.Children.OfType<Border>())
                    {
                        if (Grid.GetRow(marker) == 1)
                            marker.Background = BltNavTransparentBrush;
                    }
                }
            }
        }

        private static bool IsBltProjectNavLabel(string label)
        {
            return string.Equals(label, "Thông tin dự án", StringComparison.CurrentCultureIgnoreCase) ||
                   string.Equals(label, "Cài đặt tầng", StringComparison.CurrentCultureIgnoreCase) ||
                   string.Equals(label, "Thuộc tính dự án", StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
