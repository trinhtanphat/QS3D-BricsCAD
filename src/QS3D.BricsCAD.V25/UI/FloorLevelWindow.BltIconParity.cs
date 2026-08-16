using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow
    {
        private static readonly bool BltIconParityRegistered = RegisterBltIconParity();
        private bool _bltIconParityApplied;

        private static bool RegisterBltIconParity()
        {
            EventManager.RegisterClassHandler(
                typeof(FloorLevelWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBltIconParityLoaded),
                true);
            return true;
        }

        private static void OnBltIconParityLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FloorLevelWindow window) || window._bltIconParityApplied || window.Dispatcher.HasShutdownStarted)
                return;

            window._bltIconParityApplied = true;
            window.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                {
                    if (!window.IsLoaded || window.Dispatcher.HasShutdownStarted) return;
                    window.ApplyBltIconParity();
                }));
        }

        private void ApplyBltIconParity()
        {
            foreach (var button in FindVisualChildren<Button>(this))
            {
                if (!button.IsVisible) continue;

                var label = NormalizeButtonLabel(button.Content);
                switch (label)
                {
                    case "Thông tin dự án":
                        button.Content = BuildBltNavIconContent(
                            "M3,2.5 L15,2.5 L19,6.5 L19,21.5 L3,21.5 Z M15,2.5 L15,6.5 L19,6.5 M7,10 L15,10 M7,14 L15,14 M7,18 L12,18",
                            "Thông tin\ndự án");
                        button.ToolTip = "Thông tin dự án";
                        break;

                    case "Cài đặt tầng":
                        button.Content = BuildBltNavIconContent(
                            "M3,5 L21,5 M5,10 L19,10 M3,15 L21,15 M5,20 L19,20 M8,3 L8,7 M16,13 L16,17",
                            "Cài đặt\ntầng");
                        button.ToolTip = "Cài đặt tầng";
                        break;

                    case "Thuộc tính dự án":
                        button.Content = BuildBltNavIconContent(
                            "M4,5 L20,5 M4,12 L20,12 M4,19 L20,19 M8,2 L8,8 M16,9 L16,15 M11,16 L11,22",
                            "Thuộc tính\ndự án");
                        button.ToolTip = "Thuộc tính dự án";
                        break;

                    case "Thêm":
                        button.ToolTip = "Thêm vùng";
                        break;
                    case "Xóa":
                        button.ToolTip = "Xóa vùng";
                        break;
                    case "Chèn sàn":
                        button.ToolTip = "Chèn sàn phía trên";
                        break;
                    case "Chèn sàn xuống dưới":
                        button.ToolTip = "Chèn sàn xuống dưới";
                        break;
                    case "Xóa sàn":
                        button.ToolTip = "Xóa sàn đang chọn";
                        break;
                    case "Áp dụng thay đổi":
                        button.ToolTip = "Áp dụng thay đổi cài đặt tầng";
                        break;
                }
            }
        }

        private static FrameworkElement BuildBltNavIconContent(string geometryData, string caption)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconHost = new Grid
            {
                Width = 23,
                Height = 23,
                Margin = new Thickness(0, 0, 0, 3)
            };
            iconHost.Children.Add(new Path
            {
                Data = Geometry.Parse(geometryData),
                Stroke = new SolidColorBrush(Color.FromRgb(239, 239, 239)),
                StrokeThickness = 1.45,
                StrokeStartLineCap = PenLineCap.Square,
                StrokeEndLineCap = PenLineCap.Square,
                StrokeLineJoin = PenLineJoin.Miter,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true
            });
            panel.Children.Add(iconHost);
            panel.Children.Add(new TextBlock
            {
                Text = caption,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(243, 243, 243)),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                LineHeight = 13
            });
            return panel;
        }

        private static string NormalizeButtonLabel(object content)
        {
            if (content is TextBlock textBlock)
                return NormalizeWhitespace(textBlock.Text);

            if (content is DependencyObject root)
            {
                var text = FindVisualChildren<TextBlock>(root)
                    .Select(item => item.Text)
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                if (!string.IsNullOrWhiteSpace(text)) return NormalizeWhitespace(text);
            }

            return NormalizeWhitespace(content?.ToString() ?? string.Empty);
        }

        private static string NormalizeWhitespace(string value) =>
            string.Join(" ", (value ?? string.Empty)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) yield return match;
                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
    }
}
